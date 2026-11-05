using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

using PowerOps.Core.Deploy;
using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Core.Diff;

public enum ChangeKind { Added, Removed, Changed }

/// <summary>One difference. <paramref name="Key"/> is the identity used to match components (never a file path); <paramref name="Name"/> is a display name when the key is an id.</summary>
public sealed record Change(ChangeKind Kind, string Component, string Key, string? Name, string? Detail, IReadOnlyList<string> Risks);

public sealed record DiffResult(Solution Old, Solution New, IReadOnlyList<Change> Changes)
{
    /// <summary>What the two solutions hold that the diff means to compare but couldn't read, one sentence each; empty when it read all of it.</summary>
    public IReadOnlyList<string> NotCompared { get; init; } = [];
}

/// <summary>Stable ids of the risk labels on a <see cref="Change"/>.</summary>
public static class Risk
{
    public const string RemovedComponent = "removed-component";
    public const string FlowState = "flow-state";
    public const string ConnectionReference = "connection-reference";
    public const string EnvironmentVariable = "environment-variable";
    public const string AgentTool = "agent-tool";
    public const string AgentInstructions = "agent-instructions";
    public const string SameVersion = "same-version";

    public static string Describe(string id) => id switch
    {
        RemovedComponent => "A managed upgrade deletes components that are no longer in the new version.",
        FlowState => "A flow was turned on or off.",
        ConnectionReference => "A connection reference was added or removed.",
        EnvironmentVariable => "An environment variable was added or removed.",
        AgentTool => "An agent tool was added or removed.",
        AgentInstructions => "An agent's instructions changed.",
        SameVersion => "The two solutions differ but carry the same version number, so importing the new one is not an upgrade: deploy plan skips it; raise the version.",
        _ => id,
    };
}

/// <summary>Compares two <see cref="Solution"/> models. Every component kind is one block in <see cref="Compare"/>; to diff a new kind, add a block there.</summary>
public static class SolutionDiff
{
    private const string Arrow = " → ";

    /// <summary>With <paramref name="redact"/> (M61) an environment variable's default value is reported as added, removed or changed, and URLs in keys, names, details and not-compared lines are replaced.</summary>
    public static DiffResult Compare(Solution old, Solution @new, bool redact = false)
    {
        var changes = new List<Change>();

        changes.AddRange(Fields("solution", "uniqueName", old.UniqueName, @new.UniqueName));
        changes.AddRange(Fields("solution", "version", old.Version, @new.Version));
        changes.AddRange(Fields("solution", "managed", old.Managed ? "managed" : "unmanaged", @new.Managed ? "managed" : "unmanaged"));
        changes.AddRange(Fields("solution", "publisher", Pub(old.Publisher), Pub(@new.Publisher)));

        changes.AddRange(Keyed("rootComponent", old.RootComponents, @new.RootComponents,
            r => $"{r.Type}:{r.SchemaName ?? r.Id}", _ => null, null, (_, _) => null));

        changes.AddRange(Keyed("missingDependency", old.MissingDependencies, @new.MissingDependencies,
            d => $"{Node(d.Required)} required by {Node(d.Dependent)}", _ => null, null, (_, _) => null,
            removedDeletes: false)); // a missing dependency going away is an improvement, not a deletion

        changes.AddRange(Keyed("connectionReference", old.ConnectionReferences, @new.ConnectionReferences,
            c => c.LogicalName, _ => null, Risk.ConnectionReference,
            (a, b) => Differences(("connectorId", a.ConnectorId, b.ConnectorId))));

        changes.AddRange(Keyed("environmentVariable", old.EnvironmentVariables, @new.EnvironmentVariables,
            v => v.SchemaName, _ => null, Risk.EnvironmentVariable,
            (a, b) => Join(
                redact
                    ? Join(
                        Differences(("type", a.Type, b.Type), ("required", a.IsRequired.ToString().ToLowerInvariant(), b.IsRequired.ToString().ToLowerInvariant())),
                        Presence("defaultValue", a.DefaultValue, b.DefaultValue))
                    : Differences(
                        ("type", a.Type, b.Type),
                        ("required", a.IsRequired.ToString().ToLowerInvariant(), b.IsRequired.ToString().ToLowerInvariant()),
                        ("defaultValue", a.DefaultValue, b.DefaultValue)),
                // Current values are environment-specific and may be sensitive, so they are never printed.
                Presence("currentValue", a.CurrentValue, b.CurrentValue))));

        changes.AddRange(Keyed("workflow", old.Workflows, @new.Workflows,
            w => w.Id.Length > 0 ? w.Id : w.Name, w => w.Name, null,
            (a, b) => Join(
                Differences(("name", a.Name, b.Name), ("category", a.Category.ToString(), b.Category.ToString()), ("state", State(a.StateCode), State(b.StateCode))),
                SameJson(a.ClientData, b.ClientData) ? null : "definition changed"),
            (a, b) => a.StateCode != b.StateCode && (a.Category == 5 || b.Category == 5) ? Risk.FlowState : null));

        // Canvas apps have no model field for a name, so the name is read from the meta XML. An edit changes the .msapp and
        // AppVersion but not necessarily anything else in the meta, so AppVersion is reported (as a republish), not ignored.
        changes.AddRange(Keyed("canvasApp", old.CanvasAppTexts.Select(CanvasApp), @new.CanvasAppTexts.Select(CanvasApp),
            a => a.Name, _ => null, null,
            (a, b) => Join(
                a.Text == b.Text ? null : "definition changed",
                a.Version == b.Version ? null : $"republished (AppVersion {Show(a.Version)}{Arrow}{Show(b.Version)})")));

        // Instructions can run to 8,000 characters, so a change says so and never shows the text. Work IQ workloads are compared inside the change and carry no risk label (M51 decisions Q5).
        changes.AddRange(Keyed("agent", old.Agents, @new.Agents,
            a => a.SchemaName, _ => null, null,
            (a, b) => Join(a.Instructions == b.Instructions ? null : "instructions changed", Workloads(a, b)),
            (a, b) => a.Instructions == b.Instructions ? null : Risk.AgentInstructions));

        changes.AddRange(Keyed("agentTool", old.Agents.SelectMany(a => a.Tools), @new.Agents.SelectMany(a => a.Tools),
            t => t.SchemaName, _ => null, Risk.AgentTool,
            (a, b) => Differences(("action", a.Action, b.Action), ("mcp", a.Mcp.ToString().ToLowerInvariant(), b.Mcp.ToString().ToLowerInvariant()), ("mode", a.Mode, b.Mode), ("connectionReference", a.ConnectionReference, b.ConnectionReference), ("operationId", a.OperationId, b.OperationId))));

        // deploy plan skips a zip whose version equals the installed one and matches it by unique name, so the same test decides here (M53).
        if (changes.Count > 0 && string.Equals(old.UniqueName, @new.UniqueName, StringComparison.OrdinalIgnoreCase) && DeployPlan.EqualVersions(old.Version, @new.Version))
        {
            changes = [.. changes.Select(c => c with { Risks = [.. c.Risks, Risk.SameVersion] })];
        }

        var notCompared = NotCompared(old, @new);
        if (redact)
        {
            changes = [.. changes.Select(c => c with { Key = Redaction.Text(c.Key), Name = c.Name is null ? null : Redaction.Text(c.Name), Detail = c.Detail is null ? null : Redaction.Text(c.Detail) })];
            notCompared = [.. notCompared.Select(Redaction.Text)];
        }

        return new DiffResult(old, @new, changes) { NotCompared = notCompared };
    }

    /// <summary>"field added", "field removed" or "field changed" for a value that is never printed; null when it is the same.</summary>
    private static string? Presence(string field, string? old, string? @new) =>
        old == @new ? null : old is null ? $"{field} added" : @new is null ? $"{field} removed" : $"{field} changed";

    // One part per Work IQ workload that differs, matched by schema name like every keyed match here; a side that is missing (or that the reader couldn't read) shows as "(none)".
    private static string? Workloads(Agent a, Agent b)
    {
        static Dictionary<string, AgentCapability> Index(Agent x) =>
            x.Capabilities.GroupBy(c => c.SchemaName, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        static string Side(AgentCapability? c) => Show(c is null ? null : $"{c.Capability} {c.Access}");

        var (x, y) = (Index(a), Index(b));
        var parts = x.Keys.Union(y.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
            .Select(k => (Name: x.TryGetValue(k, out var before) ? before.SchemaName : y[k].SchemaName, Before: before, After: y.GetValueOrDefault(k)))
            .Where(w => w.Before is null || w.After is null || w.Before.Capability != w.After.Capability || w.Before.Access != w.After.Access)
            .Select(w => $"IQCapability {Truncate(w.Name, 100)}: {Side(w.Before)}{Arrow}{Side(w.After)}");
        return Join([.. parts]);
    }

    // The agents whose instructions or possible tools the reader didn't read, so a change to them can't show above (M50 decisions Q1, Q11).
    // Agents are matched like the `agent` block (ignoring case, the first of a duplicate name); a file in both versions counts once, with the old version's kind.
    private static List<string> NotCompared(Solution old, Solution @new)
    {
        var notes = new List<string>();
        foreach (var name in old.Agents.Concat(@new.Agents).Select(a => a.SchemaName).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
        {
            var (a, b) = (Find(old, name), Find(@new, name));
            var shown = Truncate(name, 100);
            if (a is not null && b is not null && (a.Instructions is null || b.Instructions is null))
            {
                var which = a.Instructions is null && b.Instructions is null ? "neither version has" : a.Instructions is null ? "the old version has no" : "the new version has no";
                notes.Add($"agent '{shown}': instructions not compared: {which} instructions PowerOps can read (the text of a single Custom GPT component, or of configuration.json's segments when the agent has no Custom GPT component)");
            }

            if (a is not null && b is not null && (a.InstructionsConflict || b.InstructionsConflict))
            {
                var where = a.InstructionsConflict && b.InstructionsConflict ? "both versions" : a.InstructionsConflict ? "the old version" : "the new version";
                notes.Add($"agent '{shown}': configuration.json instructions not compared: in {where} they differ from the Custom GPT component's or can't be read, and only the component's are compared");
            }

            var kinds = new[] { a, b }.OfType<Agent>()
                .SelectMany(x => x.OtherComponents.Select(c => (c.File, Kind: c.Kind is null ? "with no kind" : $"'{Truncate(c.Kind, 100)}'"))
                    .Concat(x.UnreadFiles.Select(f => (File: f, Kind: "'TaskDialog' with keys PowerOps can't map"))))
                .DistinctBy(c => c.File, StringComparer.OrdinalIgnoreCase)
                .GroupBy(c => c.Kind)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (g.Key, Count: g.Count()))
                .ToList();
            if (kinds.Count > 0)
            {
                var n = kinds.Sum(k => k.Count);
                notes.Add($"agent '{shown}': {n} component{(n == 1 ? "" : "s")} not read as tools, so a change to {(n == 1 ? "it" : "them")} is not compared: {string.Join(", ", kinds.Select(k => $"{k.Count} {k.Key}"))}");
            }
        }

        return notes;

        static Agent? Find(Solution s, string name) => s.Agents.FirstOrDefault(a => string.Equals(a.SchemaName, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string Pub(Publisher p) => $"{p.UniqueName} ({p.Prefix})";

    private static string Node(DependencyNode n) => $"{n.Type}:{n.SchemaName ?? n.Id}";

    private static string State(int? code) => code switch { 1 => "on", 0 => "off", null => "unknown", _ => code.ToString()! };

    private static IEnumerable<Change> Fields(string component, string key, string? old, string? @new) =>
        old == @new ? [] : [new Change(ChangeKind.Changed, component, key, null, Show(old) + Arrow + Show(@new), [])];

    /// <summary>"field: old → new" for each field that differs, joined with "; "; null when none differ.</summary>
    private static string? Differences(params (string Field, string? Old, string? New)[] fields)
    {
        var parts = fields.Where(f => f.Old != f.New).Select(f => $"{f.Field}: {Show(f.Old)}{Arrow}{Show(f.New)}").ToList();
        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    private static string Show(string? value) =>
        value is null ? "(none)" : Truncate(value, 100);

    /// <summary>Cuts to <paramref name="max"/> chars plus "…" without splitting a surrogate pair.</summary>
    internal static string Truncate(string value, int max)
    {
        if (value.Length <= max)
        {
            return value;
        }

        return value[..(char.IsHighSurrogate(value[max - 1]) ? max - 1 : max)] + "…";
    }

    private static IEnumerable<Change> Keyed<T>(
        string component,
        IEnumerable<T> before,
        IEnumerable<T> after,
        Func<T, string> key,
        Func<T, string?> name,
        string? addRemoveRisk,
        Func<T, T, string?> compare,
        Func<T, T, string?>? changeRisk = null,
        bool removedDeletes = true)
    {
        // Identity is the key only. Duplicates keep the first; keys are matched case-insensitively.
        var a = before.GroupBy(key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var b = after.GroupBy(key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var addRemove = addRemoveRisk is null ? [] : new[] { addRemoveRisk };

        foreach (var k in a.Keys.Union(b.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var inOld = a.TryGetValue(k, out var x);
            var inNew = b.TryGetValue(k, out var y);
            if (!inNew)
            {
                yield return new Change(ChangeKind.Removed, component, key(x!), name(x!), null, removedDeletes ? [Risk.RemovedComponent, .. addRemove] : addRemove);
            }
            else if (!inOld)
            {
                yield return new Change(ChangeKind.Added, component, key(y!), name(y!), null, addRemove);
            }
            else if (compare(x!, y!) is { } detail)
            {
                yield return new Change(ChangeKind.Changed, component, key(y!), name(y!), detail, changeRisk?.Invoke(x!, y!) is { } risk ? [risk] : []);
            }
        }
    }

    private static string? Join(params string?[] parts) => parts.Where(p => p is not null).ToList() is { Count: > 0 } l ? string.Join("; ", l) : null;

    private static bool SameJson(string? a, string? b)
    {
        if (a == b)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        try
        {
            // DeepEquals ignores property order and whitespace; array order still counts. operationMetadataId is a designer-generated
            // GUID that can change on a re-save without a real edit, so it is dropped first, but only from an action's or trigger's metadata.
            return JsonNode.DeepEquals(WithoutOperationMetadataIds(JsonNode.Parse(a)), WithoutOperationMetadataIds(JsonNode.Parse(b)));
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException)
        {
            return false; // not JSON (or duplicate keys, or a key with a lone surrogate): the raw text differs
        }
    }

    // Only properties.definition's triggers and actions are cleaned; the same holders FlowDefinition.WalkActions walks, to any depth.
    private static JsonNode? WithoutOperationMetadataIds(JsonNode? root)
    {
        if (root is JsonObject r && r["properties"] is JsonObject p && p["definition"] is JsonObject definition)
        {
            if (definition["triggers"] is JsonObject triggers)
            {
                foreach (var t in triggers.Select(x => x.Value).OfType<JsonObject>().ToList())
                {
                    (t["metadata"] as JsonObject)?.Remove("operationMetadataId");
                }
            }

            StripActions(definition);
        }

        return root;
    }

    private static void StripActions(JsonNode? holder)
    {
        if (holder is not JsonObject h || h["actions"] is not JsonObject actions)
        {
            return;
        }

        foreach (var a in actions.Select(x => x.Value).OfType<JsonObject>().ToList())
        {
            (a["metadata"] as JsonObject)?.Remove("operationMetadataId");
            StripActions(a);
            StripActions(a["else"]);
            StripActions(a["default"]);
            if (a["cases"] is JsonObject cases)
            {
                foreach (var c in cases.Select(x => x.Value).ToList())
                {
                    StripActions(c);
                }
            }
        }
    }

    /// <summary>The app name, the AppVersion timestamp, and the meta XML without it (parsed, so insignificant whitespace does not count).</summary>
    private static (string Name, string? Version, string Text) CanvasApp(string text)
    {
        try
        {
            // The loader already refused deep or huge metadata with its path; this check is for a Solution built by hand.
            var root = SolutionParser.ParseXml(text, "Canvas app metadata", new ReadBudget(), ignoreWhitespace: true).Root!;
            var name = root.Element("Name")?.Value.Trim() ?? "";
            var version = root.Element("AppVersion")?.Value.Trim();
            root.Element("AppVersion")?.Remove();
            // Where xmlns:xsi is declared differs between a meta file and customizations.xml; the prefixes are regenerated the same way on both sides.
            root.DescendantsAndSelf().Attributes().Where(a => a.IsNamespaceDeclaration).Remove();
            return (name.Length > 0 ? name : "(unnamed)", version, root.ToString(SaveOptions.DisableFormatting));
        }
        catch (XmlException)
        {
            return ("(unnamed)", null, text);
        }
    }
}