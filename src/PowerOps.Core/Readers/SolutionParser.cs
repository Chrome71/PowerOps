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
            paths.TryAdd(p, p);
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
            .Concat((customizations?.Element("CanvasApps")?.Elements("CanvasApp") ?? []).Select(e => (Text: e.ToString(SaveOptions.DisableFormatting), Element: (XElement?)e)))
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
            .Select(m => ReadCanvasApp(m.Element!))
            .OfType<CanvasAppMeta>()
            .ToList();

        var appModules = FolderFiles(paths.Values, AppModulePath())
            .Select(p => ReadAppModule(LoadRoot(files, p, "AppModule"), p))
            .Concat((customizations?.Element("AppModules")?.Elements("AppModule") ?? []).Select(e => ReadAppModule(e, customizationsPath!)))
            .ToList();
        var siteMaps = FolderFiles(paths.Values, SiteMapPath())
            .Select(p => ReadSiteMap(LoadRoot(files, p, "AppModuleSiteMap"), p))
            .Concat((customizations?.Element("AppModuleSiteMaps")?.Elements("AppModuleSiteMap") ?? []).Select(e => ReadSiteMap(e, customizationsPath!)))
            .ToList();

        var powerPageComponents = paths.Values
            .Where(p => PowerPageComponentPath().IsMatch(p))
            .Select(p => ReadPowerPageComponent(LoadXml(files, p), p))
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
        };
    }

    // `pac solution unpack` writes AppModule.xml, or only AppModule_managed.xml for a managed zip; a folder holding both (unpacked as Both) is read once, from the unmanaged file.
    private static IEnumerable<string> FolderFiles(IEnumerable<string> paths, Regex pattern) =>
        paths.Select(p => (Path: p, Match: pattern.Match(p)))
            .Where(x => x.Match.Success)
            .GroupBy(x => x.Match.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x.Match.Groups[2].Success).ThenBy(x => x.Path, StringComparer.Ordinal).First().Path)
            .Order(StringComparer.Ordinal);

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

    private static CanvasAppMeta? ReadCanvasApp(XElement app) =>
        Text(app, "Name") is { } name
            ? new CanvasAppMeta(name, int.TryParse(Text(app, "CanvasAppType"), out var type) ? type : null, ReadConnections(Text(app, "ConnectionReferences")))
            : null;

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
            jsonPath is null ? null : files.ReadText(jsonPath));
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
    private static (string Text, XElement? Element) CheckedMeta(SolutionFiles files, string path)
    {
        var text = files.ReadText(path);
        try
        {
            return (text, ParseXml(text, path, files.Budget).Root);
        }
        catch (XmlException)
        {
            return (text, null);
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

    [GeneratedRegex(@"^CanvasApps/[^/]+\.meta\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex CanvasMetaPath();

    [GeneratedRegex(@"^CanvasApps/[^/]+\.msapp$", RegexOptions.IgnoreCase)]
    private static partial Regex MsappPath();

    [GeneratedRegex(@"^AppModules/([^/]+)/AppModule(_managed)?\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex AppModulePath();

    [GeneratedRegex(@"^AppModuleSiteMaps/([^/]+)/AppModuleSiteMap(_managed)?\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex SiteMapPath();

    [GeneratedRegex(@"^environmentvariabledefinitions/[^/]+/environmentvariabledefinition\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvVarDefinitionPath();

    [GeneratedRegex(@"^powerpagecomponents/[^/]+/powerpagecomponent\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex PowerPageComponentPath();
}