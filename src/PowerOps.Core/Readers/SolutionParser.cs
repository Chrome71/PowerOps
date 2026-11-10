using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>The file-access seam: relative forward-slash paths plus a way to read one as text, and, for the layouts that hold `.msapp` files, a way to open one as raw bytes.</summary>
internal sealed record SolutionFiles(
    IReadOnlyCollection<string> Paths,
    Func<string, string> ReadText,
    string ManifestFile,
    string CustomizationsFile,
    ReadBudget Budget,
    Func<string, Stream>? OpenRead = null);

/// <summary>Parses the files of an unpacked or exported solution; folder and zip differ only in <see cref="SolutionFiles"/>.</summary>
internal static partial class SolutionParser
{
    /// <summary>Reads a stream as text, enforcing the per-file cap on the bytes actually read (declared sizes can lie) and the per-solution budget.</summary>
    public static string ReadCapped(Stream source, string name, ReadBudget budget)
    {
        using var reader = new StreamReader(new MemoryStream(ReadCappedBytes(source, name, budget)), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>The bytes of a stream under the same caps as <see cref="ReadCapped"/>; <paramref name="cap"/> replaces the per-file cap (an <c>.msapp</c> is a zip, so it gets the zip cap).</summary>
    public static byte[] ReadCappedBytes(Stream source, string name, ReadBudget budget, long? cap = null)
    {
        var limit = cap ?? budget.MaxFileBytes;
        using var stream = source;
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int n;
        while ((n = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + n > limit)
            {
                throw new InvalidDataException($"{name}: larger than {limit / (1024 * 1024)} MB.");
            }

            budget.Consume(n, name);
            buffer.Write(chunk, 0, n);
        }

        return buffer.ToArray();
    }

    public static Solution Parse(SolutionFiles files)
    {
        // Many references can point at one entry (zip-bomb amplification), so each file is read once.
        var cache = new Dictionary<string, string>(StringComparer.Ordinal);
        var read = files.ReadText;
        files = files with { ReadText = p => cache.TryGetValue(p, out var text) ? text : cache[p] = read(p) };

        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in files.Paths.OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!paths.TryAdd(p, p))
            {
                // The file system may keep both names (Linux), but the import treats them as one.
                throw new InvalidDataException($"{paths[p]}: {p} is listed more than once (names are compared without case).");
            }
        }

        if (!paths.TryGetValue(files.ManifestFile, out var manifestPath))
        {
            throw new InvalidDataException($"{files.ManifestFile} not found.");
        }

        var manifest = LoadXml(files, manifestPath).Element("SolutionManifest")
            ?? throw new InvalidDataException($"{manifestPath}: missing SolutionManifest element.");
        var publisher = manifest.Element("Publisher");

        var customizations = paths.TryGetValue(files.CustomizationsFile, out var customizationsPath)
            ? LoadXml(files, customizationsPath)
            : null;

        var workflows = new List<Workflow>();
        foreach (var p in paths.Values.Where(p => p.StartsWith("Workflows/", StringComparison.OrdinalIgnoreCase)
            && p.EndsWith(".data.xml", StringComparison.OrdinalIgnoreCase)))
        {
            workflows.Add(ReadWorkflow(files, paths, LoadXml(files, p), p));
        }

        foreach (var w in customizations?.Element("Workflows")?.Elements("Workflow") ?? [])
        {
            workflows.Add(ReadWorkflow(files, paths, w, customizationsPath!));
        }

        var canvasMeta = paths.Values
            .Where(p => CanvasMetaPath().IsMatch(p))
            .Select(p => CheckedMeta(files, p))
            .Concat((customizations?.Element("CanvasApps")?.Elements("CanvasApp") ?? []).Select(e => (Text: e.ToString(SaveOptions.DisableFormatting), Element: (XElement?)e, File: customizationsPath!)))
            .ToList();
        var (canvasFormulas, canvasWithoutSource) = ReadMsapps(files, paths, canvasMeta.Select(m => m.Element));

        var agentConnectionReferences = paths.TryGetValue("Assets/botcomponent_connectionreferenceset.xml", out var agentPath)
            ? LoadXml(files, agentPath).Elements("botcomponent_connectionreference")
                .Select(e => Opt((string?)e.Attribute("connectionreferenceid.connectionreferencelogicalname")))
                .OfType<string>()
                .ToList()
            : [];

        var canvasApps = canvasMeta
            .Where(m => m.Element is { Name.LocalName: "CanvasApp" })
            .Select(m => ReadCanvasApp(m.Element!, m.File))
            .OfType<CanvasAppMeta>()
            .ToList();

        var appModules = FolderFiles(paths.Values, AppModulePath())
            .Select(p => ReadAppModule(LoadRoot(files, p, "AppModule"), p))
            .Concat((customizations?.Element("AppModules")?.Elements("AppModule") ?? []).Select(e => ReadAppModule(e, customizationsPath!)))
            .ToList();
        var roles = paths.Values
            .Where(p => RolePath().IsMatch(p))
            .Order(StringComparer.Ordinal)
            .Select(p => ReadRole(LoadRoot(files, p, "Role"), p))
            .Concat((customizations?.Element("Roles")?.Elements("Role") ?? []).Select(e => ReadRole(e, customizationsPath!)))
            .DistinctBy(r => r.Id) // a role once, from the first file that holds it (`X.xml` before `X_managed.xml`, then customizations.xml)
            .ToList();
        var siteMaps = FolderFiles(paths.Values, SiteMapPath())
            .Select(p => ReadSiteMap(LoadRoot(files, p, "AppModuleSiteMap"), p))
            .Concat((customizations?.Element("AppModuleSiteMaps")?.Elements("AppModuleSiteMap") ?? []).Select(e => ReadSiteMap(e, customizationsPath!)))
            .ToList();

        var powerPageComponents = paths.Values
            .Where(p => PowerPageComponentPath().IsMatch(p))
            .Select(p => ReadPowerPageComponent(LoadXml(files, p), p))
            .ToList();

        // Table files are read only when an agent tool names a literal entity set (M74) or a relationship has a role (M79), and then once.
        var entitySets = new Lazy<Dictionary<string, string>>(() => ReadEntitySets(files, paths, customizations));
        var entitySet = EntitySetOfTable(entitySets);
        var navigationProperties = paths.Values
            .Where(p => RelationshipPath().IsMatch(p))
            .Order(StringComparer.Ordinal)
            .SelectMany(p => LoadXml(files, p).Elements("EntityRelationship"))
            .Concat(customizations?.Element("EntityRelationships")?.Elements("EntityRelationship") ?? [])
            .SelectMany(r => NavigationProperties(r, entitySet))
            .ToList();

        return new Solution(
            UniqueName: Text(manifest, "UniqueName") ?? "",
            Version: Text(manifest, "Version") ?? "",
            Managed: Text(manifest, "Managed") == "1",
            Publisher: new Publisher(
                publisher is null ? "" : Text(publisher, "UniqueName") ?? "",
                publisher is null ? "" : Text(publisher, "CustomizationPrefix") ?? ""),
            RootComponents: ReadRootComponents(manifest.Element("RootComponents")),
            MissingDependencies: ReadMissingDependencies(manifest.Element("MissingDependencies")),
            ConnectionReferences: (customizations?.Element("connectionreferences")?.Elements("connectionreference") ?? [])
                .Select(e => ReadConnectionReference(e, customizationsPath!))
                .ToList(),
            EnvironmentVariables: paths.Values
                .Where(p => EnvVarDefinitionPath().IsMatch(p))
                .Select(p => ReadEnvironmentVariable(files, paths, p))
                .ToList(),
            Workflows: workflows,
            CanvasAppTexts: canvasMeta.Select(m => m.Text).ToList(),
            ManifestFile: manifestPath,
            AgentConnectionReferences: agentConnectionReferences)
        {
            PowerPageComponents = powerPageComponents,
            CanvasFormulas = canvasFormulas,
            CanvasAppsWithoutSource = canvasWithoutSource,
            AppModules = appModules,
            SiteMaps = siteMaps,
            CanvasApps = canvasApps,
            Agents = ReadAgents(files, paths, entitySets),
            Roles = roles,
            NavigationProperties = navigationProperties,
        };
    }

    // `pac solution unpack` writes AppModule.xml, or only AppModule_managed.xml for a managed zip; a folder holding both (unpacked as Both) is read once, from the unmanaged file.
    private static IEnumerable<string> FolderFiles(IEnumerable<string> paths, Regex pattern) =>
        paths.Select(p => (Path: p, Match: pattern.Match(p)))
            .Where(x => x.Match.Success)
            .GroupBy(x => x.Match.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x.Match.Groups[2].Success).ThenBy(x => x.Path, StringComparer.Ordinal).First().Path)
            .Order(StringComparer.Ordinal);

    private sealed class AgentParts(string name, string file, int? mode, AgentConfiguration? configuration, string? configurationFile)
    {
        public string? ConfigurationFile { get; } = configurationFile;

        public string Name { get; } = name;

        public string File { get; } = file;

        public int? Mode { get; } = mode;

        public AgentConfiguration? Configuration { get; } = configuration;

        public List<AgentTool> Tools { get; } = [];

        public List<(XElement? Yaml, string File)> Gpts { get; } = [];

        public List<string> Unread { get; } = [];

        public List<AgentComponent> Other { get; } = [];

        public List<AgentComponent> Dialogs { get; } = [];

        public List<AgentCapability> Capabilities { get; } = [];
    }

    /// <summary>Topics and child agents are type-9 components that are not tools; `TaskDialog` is a tool.</summary>
    private static bool KnownKind(string? kind) => kind is "TaskDialog" or "AdaptiveDialog" or "AgentDialog";

    /// <summary>
    /// Reads each `bots/&lt;folder&gt;/bot.xml` and the `botcomponents/` that name it as their `parentbotid` (M48 plan, Data row 2). Malformed XML, JSON or YAML is refused with the path; a shape the reader doesn't know gives null fields.
    /// Only a type-15 (Custom GPT) and a type-9 (Topic (V2)) component of a read agent has its `data` read, and a type-9 `data` only when its text holds `TaskDialog` or its first column-0 `kind:` line is `McpTool` or `IQCapability`, so topics add no refusals.
    /// An agent with no Custom GPT component takes its instructions from `configuration.json` (M51 plan, Data rows 1 to 4).
    /// </summary>
    private static List<Agent> ReadAgents(SolutionFiles files, Dictionary<string, string> paths, Lazy<Dictionary<string, string>> tables)
    {
        var agents = new List<AgentParts>();
        var byName = new Dictionary<string, AgentParts>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths.Values.Where(p => BotPath().IsMatch(p)).Order(StringComparer.Ordinal))
        {
            var folder = BotPath().Match(path).Groups[1].Value;
            var bot = LoadRoot(files, path, "bot");
            var agent = new AgentParts(
                Opt((string?)bot.Attribute("schemaname")) ?? folder,
                path,
                int.TryParse(Text(bot, "authenticationmode"), out var mode) ? mode : null,
                paths.TryGetValue($"bots/{folder}/configuration.json", out var config) ? ReadAgentConfiguration(files.ReadText(config), config) : null,
                config);
            agents.Add(agent);
            byName.TryAdd(agent.Name, agent);
        }

        foreach (var path in paths.Values.Where(p => BotComponentPath().IsMatch(p)).Order(StringComparer.Ordinal))
        {
            var folder = BotComponentPath().Match(path).Groups[1].Value;
            var component = LoadRoot(files, path, "botcomponent");
            if (!int.TryParse(Text(component, "componenttype"), out var type) || type is not (9 or 15)
                || component.Element("parentbotid") is not { } parentElement || Text(parentElement, "schemaname") is not { } parent || !byName.TryGetValue(parent, out var agent)
                || !paths.TryGetValue($"botcomponents/{folder}/data", out var data))
            {
                continue;
            }

            var text = files.ReadText(data);
            // Read from its first `kind:` line, not parsed, so topics add no refusals (M50 decisions Q3).
            var lineKind = type == 9 && TopKind().Match(text) is { Success: true } m ? Opt(m.Groups[1].Value.Trim().Trim('"', '\'').Trim()) : null;
            var newKind = lineKind is "McpTool" or "IQCapability";
            if (lineKind is "AdaptiveDialog" or "AgentDialog" && ActionNode().Match(text) is { Success: true } node)
            {
                agent.Dialogs.Add(new AgentComponent(node.Groups[1].Value, data));
            }

            if (type == 9 && !text.Contains("TaskDialog", StringComparison.Ordinal) && !newKind)
            {
                if (!KnownKind(lineKind))
                {
                    agent.Other.Add(new AgentComponent(lineKind, data));
                }

                continue;
            }

            var yaml = ReadAgentYaml(files, data, text);
            if (type == 15)
            {
                agent.Gpts.Add((yaml, data));
            }
            else if (yaml is null)
            {
                if (newKind)
                {
                    agent.Other.Add(new AgentComponent(lineKind, data));
                }
                else
                {
                    agent.Unread.Add(data); // may be a tool, but its keys can't be mapped
                }
            }
            else
            {
                var kind = YamlText(yaml, "kind");
                var schema = Opt((string?)component.Attribute("schemaname")) ?? folder;
                if (kind == "TaskDialog")
                {
                    agent.Tools.Add(new AgentTool(
                        schema,
                        YamlText(yaml, "action", "kind"),
                        YamlText(yaml, "action", "operationDetails", "kind") == "ModelContextProtocolMetadata",
                        YamlText(yaml, "action", "connectionProperties", "mode"),
                        data,
                        Table: ReadToolTable(yaml, tables),
                        FlowId: NormalizeId(YamlText(yaml, "action", "flowId"))));
                }
                else if (kind == "McpTool")
                {
                    agent.Tools.Add(new AgentTool(schema, null, true, YamlText(yaml, "authMode"), data, YamlText(yaml, "connectionReference"), YamlText(yaml, "operationId"), "McpTool"));
                }
                else if (kind == "IQCapability" && YamlText(yaml, "capability") is { } capability && YamlText(yaml, "access") is { } access)
                {
                    agent.Capabilities.Add(new AgentCapability(schema, capability, access, data));
                }
                else if (!KnownKind(kind))
                {
                    agent.Other.Add(new AgentComponent(kind, data));
                }
            }
        }

        return agents.Select(a =>
        {
            var gpt = a.Gpts.Count == 1 ? a.Gpts[0] : default;
            var instructions = gpt.Yaml?.Elements("instructions").ToList();
            var text = instructions is null ? null : instructions.Count == 0 ? "" : instructions is [{ HasElements: false } only] ? only.Value : null;
            var configured = a.Gpts.Count == 0 ? a.Configuration?.Instructions : null;
            return new Agent(
                a.Name,
                a.File,
                a.Mode,
                a.Configuration,
                text ?? configured,
                a.Gpts.Count == 0 ? (configured is null ? null : a.ConfigurationFile) : gpt.File,
                a.Tools,
                [.. a.Unread.Order(StringComparer.Ordinal)])
            {
                OtherComponents = [.. a.Other.OrderBy(c => c.File, StringComparer.Ordinal)],
                ActionDialogs = [.. a.Dialogs.OrderBy(c => c.File, StringComparer.Ordinal)],
                Capabilities = [.. a.Capabilities.OrderBy(c => c.File, StringComparer.Ordinal)],
                InstructionsFromConfiguration = configured is not null,
                InstructionsConflict = text is not null && a.Configuration is { } c && (c.InstructionsUnread || c.Instructions is not null && Normalize(c.Instructions) != Normalize(text)),
            };
        }).ToList();

        // A YAML block scalar keeps a line break the maker didn't type, and JSON may write line breaks differently.
        static string Normalize(string value) => value.Replace("\r\n", "\n").TrimEnd('\n');
    }

    // The item of the top-level `inputs` list named `entityName`; null when there is none.
    private static ToolTable? ReadToolTable(XElement yaml, Lazy<Dictionary<string, string>> tables)
    {
        var item = yaml.Elements("inputs").FirstOrDefault(i => YamlText(i, "propertyName") == "entityName");
        if (item is null)
        {
            return null;
        }

        var set = YamlText(item, "kind") == "ManualTaskInput" && YamlText(item, "value") is { } value && !value.StartsWith('=') ? value : null;
        return new ToolTable(set, set is not null && tables.Value.TryGetValue(set, out var name) ? name : null);
    }

    // Each `Entities/<table>/Entity.xml` in ordinal path order, then the customizations file's `Entities/Entity`.
    private static Dictionary<string, string> ReadEntitySets(SolutionFiles files, Dictionary<string, string> paths, XElement? customizations) =>
        EntitySets(paths.Values.Where(p => EntityPath().IsMatch(p)).Order(StringComparer.Ordinal).Select(p => LoadXml(files, p))
            .Concat(customizations?.Element("Entities")?.Elements("Entity") ?? []));

    /// <summary>Entity set name to table name, ignoring case, from <c>Entity</c> elements; the first table to claim a set wins.</summary>
    internal static Dictionary<string, string> EntitySets(IEnumerable<XElement> tables)
    {
        var sets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in tables.SelectMany(t => t.Element("EntityInfo")?.Elements("entity") ?? []))
        {
            if (Text(entity, "EntitySetName") is { } set && Opt((string?)entity.Attribute("Name")) is { } name)
            {
                sets.TryAdd(set, name);
            }
        }

        return sets;
    }

    /// <summary>A table's entity set from <paramref name="sets"/>, ignoring case; the first entity set for a table wins. The inverse map is built on the first call.</summary>
    internal static Func<string, string?> EntitySetOfTable(Lazy<Dictionary<string, string>> sets)
    {
        var byTable = new Lazy<Dictionary<string, string>>(() =>
        {
            var tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (set, table) in sets.Value)
            {
                tables.TryAdd(table, set);
            }

            return tables;
        });
        return table => byTable.Value.GetValueOrDefault(table);
    }

    /// <summary>
    /// The navigation properties of a one-to-many <c>EntityRelationship</c>: each <c>EntityRelationshipRole</c> with a <c>NavigationPropertyName</c> gives one, on <c>ReferencingEntityName</c> for role type 1
    /// and <c>ReferencedEntityName</c> for type 0 (observed in exports, M79). Another role type, a role with no name or table, and a relationship of another type give none.
    /// <paramref name="entitySet"/> is called only for a row found.
    /// </summary>
    internal static IEnumerable<NavigationProperty> NavigationProperties(XElement relationship, Func<string, string?> entitySet)
    {
        if (Text(relationship, "EntityRelationshipType") != "OneToMany")
        {
            yield break;
        }

        foreach (var role in relationship.Element("EntityRelationshipRoles")?.Elements("EntityRelationshipRole") ?? [])
        {
            var table = Text(role, "RelationshipRoleType") switch
            {
                "1" => Text(relationship, "ReferencingEntityName"),
                "0" => Text(relationship, "ReferencedEntityName"),
                _ => null,
            };
            if (table is not null && Text(role, "NavigationPropertyName") is { } name)
            {
                yield return new NavigationProperty(name, table, entitySet(table));
            }
        }
    }

    private static XElement? ReadAgentYaml(SolutionFiles files, string path, string text)
    {
        try
        {
            return YamlXml.Read(text, n => files.Budget.CountNode(path, n), attributes: false);
        }
        // Valid YAML the converter can't map is an unknown shape, not a refusal (M48 decisions Q6).
        catch (YamlShapeException)
        {
            return null;
        }
        // The node budget's message already starts with the path.
        catch (InvalidDataException e) when (!e.Message.StartsWith(path + ": ", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    // The text of a scalar below the top level; null when a key on the way is missing or the value is a mapping.
    private static string? YamlText(XElement yaml, params string[] keys)
    {
        XElement? e = yaml;
        foreach (var key in keys)
        {
            e = e?.Element(key);
        }

        return e is { HasElements: false } ? Opt(e.Value.Trim()) : null;
    }

    private static AgentConfiguration? ReadAgentConfiguration(string text, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // A recognizer that is there but names no readable kind is "": not GenerativeAIRecognizer, so the agent isn't placed in the standard harness.
            var recognizer = !root.TryGetProperty("recognizer", out var r) ? null
                : r.ValueKind == JsonValueKind.Object && r.TryGetProperty("$kind", out var kind) ? JsonString(kind) ?? ""
                : "";
            bool? generative = root.TryGetProperty("settings", out var settings) && settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty("GenerativeActionsEnabled", out var on)
                && on.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? on.ValueKind == JsonValueKind.True
                : null;
            var (instructions, unread) = ReadSegments(root);
            return new AgentConfiguration(recognizer, generative, instructions, unread);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    // `agentSettings.instructions.segments` of the new authoring UI: only StaticSegment items with text are read, so a reference can't pass as the prompt's text (M51 decisions Q2).
    private static (string? Text, bool Unread) ReadSegments(JsonElement root)
    {
        if (!root.TryGetProperty("agentSettings", out var settings) || settings.ValueKind != JsonValueKind.Object || !settings.TryGetProperty("instructions", out var node))
        {
            return (null, false);
        }

        if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("segments", out var segments) || segments.ValueKind != JsonValueKind.Array)
        {
            return (null, true);
        }

        var values = new List<string>();
        foreach (var item in segments.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("$kind", out var kind) || JsonString(kind) != "StaticSegment"
                || !item.TryGetProperty("value", out var value) || JsonString(value) is not { } text)
            {
                return (null, true);
            }

            values.Add(text);
        }

        return (string.Concat(values), false);
    }

    // A string that is not valid UTF-16 text throws InvalidOperationException from GetString.
    private static string? JsonString(JsonElement e)
    {
        try
        {
            return e.ValueKind == JsonValueKind.String ? e.GetString() : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static XElement LoadRoot(SolutionFiles files, string path, string name)
    {
        var root = LoadXml(files, path);
        return root.Name.LocalName == name
            ? root
            : throw new InvalidDataException($"{path}: the root element is '{root.Name.LocalName}', not '{name}'.");
    }

    private static List<string> Trimmed(IEnumerable<string?>? values) =>
        (values ?? []).Select(v => v?.Trim()).Where(v => !string.IsNullOrEmpty(v)).ToList()!;

    private static AppModule ReadAppModule(XElement e, string file) => new(
        Text(e, "UniqueName") ?? throw new InvalidDataException($"{file}: missing UniqueName element."),
        Trimmed(e.Element("AppModuleComponents")?.Elements("AppModuleComponent")
            .Where(c => ((string?)c.Attribute("type"))?.Trim() == "62")
            .Select(c => (string?)c.Attribute("schemaName"))),
        Trimmed(e.Element("appelements")?.Elements("appelement").Select(a => a.Element("canvasappid")?.Element("name")?.Value)),
        file)
    {
        Roles = (e.Element("AppModuleRoleMaps")?.Elements("Role") ?? []).Select(r => NormalizeId((string?)r.Attribute("id"))).OfType<string>().ToList(),
    };

    private static Role ReadRole(XElement e, string file) => new(
        NormalizeId((string?)e.Attribute("id")) ?? throw new InvalidDataException($"{file}: missing id attribute."),
        Opt(((string?)e.Attribute("name"))?.Trim()) ?? throw new InvalidDataException($"{file}: missing name attribute."),
        e.Element("RolePrivileges") is { } privileges ? Trimmed(privileges.Elements("RolePrivilege").Select(p => (string?)p.Attribute("name"))) : null,
        file);

    // The platform types a SubArea as GenPageId, then Entity, then Page (decisions Q4); names are exact, as XML is case-sensitive.
    private static SiteMap ReadSiteMap(XElement e, string file)
    {
        var pages = new List<string>();
        var genPageIds = new List<string>();
        foreach (var area in e.Element("SiteMap")?.Descendants("SubArea") ?? [])
        {
            if (Opt((string?)area.Attribute("GenPageId")) is { } gen)
            {
                if (NormalizeId(gen) is { } id)
                {
                    genPageIds.Add(id);
                }
            }
            else if (Opt((string?)area.Attribute("Entity")) is null && Opt(((string?)area.Attribute("Page"))?.Trim()) is { } page)
            {
                pages.Add(page);
            }
        }

        return new SiteMap(Text(e, "SiteMapUniqueName") ?? throw new InvalidDataException($"{file}: missing SiteMapUniqueName element."), pages, genPageIds, file);
    }

    private static CanvasAppMeta? ReadCanvasApp(XElement app, string file)
    {
        if (Text(app, "Name") is not { } name)
        {
            return null;
        }

        var (tables, connectors, otherTypes) = ReadDependencies(Text(app, "CdsDependencies"));
        return new CanvasAppMeta(name, int.TryParse(Text(app, "CanvasAppType"), out var type) ? type : null, ReadConnections(Text(app, "ConnectionReferences")), file, tables, connectors, otherTypes);
    }

    // All three lists are null when the text is not in the observed shape (M47 plan, Data row 2): a check that sees part of an app says nothing. Nothing is refused, as for ReadConnections.
    // DatabaseReferences is not read: a real export lists an entity set there that no solution can hold.
    private static (IReadOnlyList<string>? Tables, IReadOnlyList<string>? Connectors, IReadOnlyList<int>? OtherTypes) ReadDependencies(string? text)
    {
        if (text is null)
        {
            return (null, null, null);
        }

        var tables = new List<string>();
        var connectors = new List<string>();
        var otherTypes = new List<int>();
        try
        {
            if (!ReadCdsDependencies(text, tables, connectors, otherTypes))
            {
                return (null, null, null);
            }
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            // A name that is not valid UTF-16 text throws InvalidOperationException from GetString.
            return (null, null, null);
        }

        return (tables.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), connectors.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), otherTypes.Distinct().ToList());
    }

    // Type 1 is a table, 372 a connector and 29 a workflow, which is skipped; any other type is listed in otherTypes and its entry not read further.
    private static bool ReadCdsDependencies(string text, List<string> tables, List<string> connectors, List<int> otherTypes)
    {
        using var doc = JsonDocument.Parse(text);
        if (doc.RootElement.ValueKind != JsonValueKind.Object
            || !doc.RootElement.TryGetProperty("cdsdependencies", out var entries)
            || entries.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object
                || !entry.TryGetProperty("componenttype", out var t)
                || t.ValueKind != JsonValueKind.Number
                || !t.TryGetInt32(out var type))
            {
                return false;
            }

            if (type is not (1 or 372 or 29))
            {
                otherTypes.Add(type);
            }
            else if (type != 29)
            {
                if (entry.TryGetProperty("logicalname", out var n) && n.ValueKind == JsonValueKind.String && Opt(n.GetString()!.Trim()) is { } name)
                {
                    (type == 1 ? tables : connectors).Add(name);
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Null when the text is not a JSON object: before M46 nothing parsed it, so every command that loads a solution must still load it (decisions Q9).
    private static Dictionary<string, string>? ReadConnections(string? text)
    {
        var connections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (text is null)
        {
            return connections;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var entry in doc.RootElement.EnumerateObject())
            {
                var key = entry.Name.Trim();
                if (key.Length > 0)
                {
                    var id = entry.Value.ValueKind == JsonValueKind.Object && entry.Value.TryGetProperty("id", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";
                    connections.TryAdd(key, id);
                }
            }

            return connections;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            // An id that is not valid UTF-16 text throws InvalidOperationException from GetString.
            return null;
        }
    }

    private static PowerPageComponent ReadPowerPageComponent(XElement e, string path)
    {
        if (e.Name.LocalName != "powerpagecomponent")
        {
            throw new InvalidDataException($"{path}: the root element is '{e.Name.LocalName}', not 'powerpagecomponent'.");
        }

        var id = NormalizeId((string?)e.Attribute("powerpagecomponentid"))
            ?? throw new InvalidDataException($"{path}: missing powerpagecomponentid attribute.");
        if (!int.TryParse(Text(e, "powerpagecomponenttype"), out var type))
        {
            throw new InvalidDataException($"{path}: powerpagecomponenttype is missing or not an integer.");
        }

        var name = Text(e, "name") ?? "";
        // Only the three types the rules read have their content parsed; any other type's content may be anything.
        var content = EmptyContent;
        if (type is 9 or 11 or 18)
        {
            content = ReadContent(e.Element("content")?.Value, path);
            CheckContentKinds(content, type, name, path);
        }

        return new PowerPageComponent(
            id,
            type,
            name,
            NormalizeId(e.Element("powerpagesiteid")?.Element("powerpagesiteid")?.Value),
            Text(e, "statecode") is null or "0",
            content,
            path);
    }

    private static readonly JsonElement EmptyContent = JsonDocument.Parse("{}").RootElement.Clone();

    private static JsonElement ReadContent(string? text, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text, new JsonDocumentOptions { AllowDuplicateProperties = false });
            return doc.RootElement.ValueKind == JsonValueKind.Object
                ? doc.RootElement.Clone()
                : throw new InvalidDataException($"{path}: content is not a JSON object.");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{path}: content is not valid JSON: {e.Message}", e);
        }
    }

    // The rules read these keys without checks of their own, so a kind they can't use is refused here, where the path is known.
    private static void CheckContentKinds(JsonElement content, int type, string name, string path)
    {
        switch (type)
        {
            case 9 when name.StartsWith("Webapi/", StringComparison.OrdinalIgnoreCase):
                Expect(content, "value", JsonValueKind.String, "a string", path);
                break;
            case 11:
                Expect(content, "anonymoususersrole", JsonValueKind.True, "a boolean", path);
                break;
            case 18:
                Expect(content, "adx_entitypermission_webrole", JsonValueKind.Array, "an array of strings", path);
                Expect(content, "create", JsonValueKind.True, "a boolean", path);
                Expect(content, "write", JsonValueKind.True, "a boolean", path);
                Expect(content, "delete", JsonValueKind.True, "a boolean", path);
                break;
        }
    }

    private static void Expect(JsonElement content, string key, JsonValueKind kind, string described, string path)
    {
        if (!content.TryGetProperty(key, out var v) || v.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        var ok = kind switch
        {
            JsonValueKind.True => v.ValueKind is JsonValueKind.True or JsonValueKind.False,
            JsonValueKind.Array => v.ValueKind == JsonValueKind.Array && v.EnumerateArray().All(x => x.ValueKind == JsonValueKind.String),
            _ => v.ValueKind == kind,
        };
        if (!ok)
        {
            throw new InvalidDataException($"{path}: content key '{key}' is not {described}.");
        }
    }

    internal static List<RootComponent> ReadRootComponents(XElement? rootComponents) =>
        (rootComponents?.Elements("RootComponent") ?? [])
            .Select(e => new RootComponent((string?)e.Attribute("type") ?? "", Opt((string?)e.Attribute("schemaName")), NormalizeId((string?)e.Attribute("id"))))
            .ToList();

    internal static List<MissingDependency> ReadMissingDependencies(XElement? missingDependencies) =>
        (missingDependencies?.Elements("MissingDependency") ?? [])
            .Select(e => new MissingDependency(
                ReadNode(e.Element("Required") ?? new XElement("Required")),
                ReadNode(e.Element("Dependent") ?? new XElement("Dependent"))))
            .ToList();

    internal static ConnectionReference ReadConnectionReference(XElement e, string file) => new(
        (string?)e.Attribute("connectionreferencelogicalname") ?? "",
        Text(e, "connectorid"),
        file);

    internal static Workflow ReadWorkflow(SolutionFiles files, Dictionary<string, string> paths, XElement e, string definedIn)
    {
        var jsonName = Opt(Text(e, "JsonFileName")?.Replace('\\', '/').TrimStart('/'));
        var jsonPath = jsonName is not null && paths.TryGetValue(jsonName, out var actual) ? actual : null;
        var category = int.TryParse(Text(e, "Category"), out var c) ? c : 0;
        if (category == 5 && jsonName is not null && jsonPath is null)
        {
            throw new InvalidDataException($"{definedIn}: cloud flow '{(string?)e.Attribute("Name")}' declares {jsonName}, which is missing.");
        }

        return new Workflow(
            NormalizeId((string?)e.Attribute("WorkflowId")) ?? "",
            (string?)e.Attribute("Name") ?? "",
            category,
            int.TryParse(Text(e, "StateCode"), out var state) ? state : null,
            jsonPath ?? jsonName ?? definedIn,
            jsonPath is null ? null : files.ReadText(jsonPath),
            Text(e, "PrimaryEntity") is { } entity && !entity.Equals("none", StringComparison.OrdinalIgnoreCase) ? entity : null);
    }

    private static EnvironmentVariable ReadEnvironmentVariable(SolutionFiles files, Dictionary<string, string> paths, string path)
    {
        var def = LoadXml(files, path);
        var valuesName = path[..path.LastIndexOf('/')] + "/environmentvariablevalues.json";
        string? current = null;
        if (paths.TryGetValue(valuesName, out var valuesPath))
        {
            current = ReadCurrentValue(files.ReadText(valuesPath), valuesPath);
        }

        return ReadEnvironmentVariable(def, current, path, valuesPath);
    }

    internal static EnvironmentVariable ReadEnvironmentVariable(XElement def, string? current, string path, string? valuesPath)
    {
        return new EnvironmentVariable(
            (string?)def.Attribute("schemaname") ?? "",
            Text(def, "type"),
            Text(def, "isrequired") is "1" or "true",
            Text(def, "defaultvalue"),
            current,
            path,
            valuesPath);
    }

    private static string? ReadCurrentValue(string json, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("environmentvariablevalues", out var values)
                && values.ValueKind == JsonValueKind.Object
                && values.TryGetProperty("environmentvariablevalue", out var value))
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    value = value.EnumerateArray().FirstOrDefault();
                }

                if (value.ValueKind == JsonValueKind.Object
                    && value.TryGetProperty("value", out var v)
                    && v.ValueKind == JsonValueKind.String)
                {
                    return v.GetString();
                }
            }

            return null;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    private static DependencyNode ReadNode(XElement e) => new(
        (string?)e.Attribute("type") ?? "",
        // Some nodes are keyed by `id.schemaname` / `id.connectionreferencelogicalname` instead of schemaName.
        Opt((string?)e.Attribute("schemaName"))
            ?? Opt(e.Attributes().FirstOrDefault(a => a.Name.LocalName.StartsWith("id.", StringComparison.Ordinal))?.Value),
        NormalizeId((string?)e.Attribute("id")),
        Opt((string?)e.Attribute("displayName")),
        Opt((string?)e.Attribute("solution")));

    internal const int MaxXmlDepth = 200;

    private static XElement LoadXml(SolutionFiles files, string path)
    {
        try
        {
            return ParseXml(files.ReadText(path), path, files.Budget).Root!;
        }
        catch (XmlException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    /// <summary>
    /// Canvas metadata stays text, and malformed XML in it is tolerated (SolutionDiff reads it later; the element is then null); too deep or too large is refused here,
    /// where the file's path is known. One parse gives both, so the file's nodes count against the budget once.
    /// </summary>
    private static (string Text, XElement? Element, string File) CheckedMeta(SolutionFiles files, string path)
    {
        var text = files.ReadText(path);
        try
        {
            return (text, ParseXml(text, path, files.Budget).Root, path);
        }
        catch (XmlException)
        {
            return (text, null, path);
        }
    }

    /// <summary>
    /// Reads each `CanvasApps/&lt;name&gt;.msapp` (a zip) in ordinal order. The app's name comes from the metadata whose <c>DocumentUri</c> names the file, else from the file name.
    /// An app whose package holds no source is listed instead of read. A <c>DocumentUri</c> that names no listed <c>.msapp</c> is not read and not noted: the file is never opened.
    /// </summary>
    private static (List<CanvasFormula> Formulas, List<string> WithoutSource) ReadMsapps(SolutionFiles files, Dictionary<string, string> paths, IEnumerable<XElement?> metadata)
    {
        var apps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in metadata.OfType<XElement>().Where(e => e.Name.LocalName == "CanvasApp"))
        {
            if (Text(app, "DocumentUri")?.Replace('\\', '/').TrimStart('/') is not { } uri)
            {
                continue;
            }

            if ((Text(app, "DisplayName") ?? Text(app, "Name")) is { } name)
            {
                apps.TryAdd(uri, name);
            }
        }

        var formulas = new List<CanvasFormula>();
        var withoutSource = new List<string>();
        if (files.OpenRead is { } open)
        {
            foreach (var msapp in paths.Values.Where(p => MsappPath().IsMatch(p)).Order(StringComparer.Ordinal))
            {
                var fileName = msapp[(msapp.LastIndexOf('/') + 1)..];
                var name = apps.GetValueOrDefault(msapp)
                    ?? (fileName.EndsWith("_DocumentUri.msapp", StringComparison.OrdinalIgnoreCase) ? fileName[..^"_DocumentUri.msapp".Length] : fileName[..^".msapp".Length]);
                if (CanvasSource.ReadMsapp(ReadCappedBytes(open(msapp), msapp, files.Budget, files.Budget.MaxZipBytes), msapp, name, files.Budget) is { } read)
                {
                    formulas.AddRange(read);
                }
                else
                {
                    withoutSource.Add(msapp);
                }
            }
        }

        return (formulas, withoutSource);
    }

    /// <summary>
    /// The one place XML text becomes an XDocument: DTDs are prohibited (solutions are untrusted), and nesting deeper than <see cref="MaxXmlDepth"/>
    /// or more nodes than the budget has left are refused first.
    /// </summary>
    /// <exception cref="XmlException">The text is not well-formed XML.</exception>
    /// <exception cref="InvalidDataException">The text is too deep or has too many nodes; the message starts with <paramref name="path"/>.</exception>
    internal static XDocument ParseXml(string text, string path, ReadBudget budget, bool ignoreWhitespace = false)
    {
        ScanXml(text, path, budget);
        using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, IgnoreWhitespace = ignoreWhitespace });
        return XDocument.Load(reader);
    }

    // XDocument.Load is quadratic in nesting depth and costs memory per node, so a cheap forward-only pass refuses hostile input first.
    private static void ScanXml(string text, string path, ReadBudget budget)
    {
        using var scan = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
        while (scan.Read())
        {
            if (scan.Depth > MaxXmlDepth)
            {
                throw new InvalidDataException($"{path}: XML is nested deeper than {MaxXmlDepth} levels.");
            }

            // Attributes are not nodes to Read(), but XDocument.Load keeps each one.
            budget.CountNode(path, 1 + scan.AttributeCount);
        }
    }

    internal static string? Text(XElement e, string child) => Opt(e.Element(child)?.Value.Trim());

    internal static string? Opt(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    internal static string? NormalizeId(string? id) => Opt(id?.Trim().Trim('{', '}').ToLowerInvariant());

    // A `#` after white space starts a YAML comment, which is not part of the kind.
    [GeneratedRegex(@"^kind:(.*?)(?:\s#.*)?\r?$", RegexOptions.Multiline)]
    private static partial Regex TopKind();

    // A topic or child agent node that can reach a table: an `Invoke…` node (a flow, connector, skill or AI model) or an HTTP request, in block or flow style; a match inside a message can only add a note.
    [GeneratedRegex("""(?<![\w.])kind:[ \t]*["']?(Invoke[A-Za-z]*|HttpRequestAction)\b""")]
    private static partial Regex ActionNode();

    [GeneratedRegex(@"^Entities/[^/]+/Entity\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex EntityPath();

    [GeneratedRegex(@"^Other/Relationships/[^/]+\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex RelationshipPath();

    [GeneratedRegex(@"^CanvasApps/[^/]+\.meta\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex CanvasMetaPath();

    [GeneratedRegex(@"^CanvasApps/[^/]+\.msapp$", RegexOptions.IgnoreCase)]
    private static partial Regex MsappPath();

    [GeneratedRegex(@"^AppModules/([^/]+)/AppModule(_managed)?\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex AppModulePath();

    [GeneratedRegex(@"^Roles/[^/]+\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex RolePath();

    [GeneratedRegex(@"^AppModuleSiteMaps/([^/]+)/AppModuleSiteMap(_managed)?\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex SiteMapPath();

    [GeneratedRegex(@"^environmentvariabledefinitions/[^/]+/environmentvariabledefinition\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvVarDefinitionPath();

    [GeneratedRegex(@"^powerpagecomponents/[^/]+/powerpagecomponent\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex PowerPageComponentPath();

    [GeneratedRegex(@"^bots/([^/]+)/bot\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex BotPath();

    [GeneratedRegex(@"^botcomponents/([^/]+)/botcomponent\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex BotComponentPath();
}