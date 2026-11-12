using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Core.Model;
using PowerOps.Core.Rules;

namespace PowerOps.Core.Output;

/// <summary>What <c>powerops inventory</c> reports, per solution in the order given. <paramref name="ShowValues"/> is true when names were not redacted and literal hosts are kept.</summary>
public sealed record Inventory(IReadOnlyList<InventorySolution> Solutions, bool ShowValues);

/// <summary>One solution's inventory; every list is present, empty when none.</summary>
public sealed record InventorySolution(
    string Path,
    string UniqueName,
    IReadOnlyList<InventoryDriver> LicenceDrivers,
    IReadOnlyList<InventoryNotClassified> NotClassified,
    IReadOnlyList<InventoryConnector> Connectors,
    IReadOnlyList<InventoryCustomConnector> CustomConnectors,
    IReadOnlyList<InventoryHttpAction> HttpActions,
    IReadOnlyList<InventoryRequestTrigger> RequestTriggers,
    IReadOnlyList<InventoryChildFlow> ChildFlows,
    IReadOnlyList<string> NotRead);

/// <summary>A licence driver of the closed set: what it needs, its Learn sources, the date they were checked and what fires it.</summary>
public sealed record InventoryDriver(string Id, string Need, IReadOnlyList<string> Sources, string Verified, IReadOnlyList<string> Where);

public sealed record InventoryNotClassified(string Id, string Note);

public sealed record InventoryConnector(string Id, IReadOnlyList<string> References, IReadOnlyList<InventoryUser> Users);

/// <summary><paramref name="Kind"/> is <c>flow</c>, <c>canvasApp</c>, <c>customPage</c>, <c>codeApp</c> or <c>agentTool</c>.</summary>
public sealed record InventoryUser(string Kind, string Name);

public sealed record InventoryCustomConnector(string Name, bool InSolution);

public sealed record InventoryHttpAction(string Flow, string Action, string Type, string Endpoint, string? Host);

public sealed record InventoryRequestTrigger(string Flow, string Trigger, string? Kind);

/// <summary><paramref name="Child"/> is the called flow's name, null when the solution doesn't hold it.</summary>
public sealed record InventoryChildFlow(string Flow, string? Child);

/// <summary>The sections of <c>powerops inventory</c> (M107) and its text, JSON and Markdown, with "\n" line endings on every OS.</summary>
public static partial class InventoryReport
{
    public const string NotClassifiedNote = "tier not classified: check the connector's reference page";

    public const string Footer = "Drivers come from a closed set with sources; a solution with no driver listed may still need a premium licence.";

    public const string YamlNotRead = "canvas apps, model-driven apps and agents are not read in the YAML layout, so dataverse-in-canvas-app, model-driven-app and their users are not reported";

    private const string Verified = "2026-11-12";
    private const string Designation = "https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation";
    private const string Types = "https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types";
    private const string AppsFaq = "https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq";
    private const string DesktopFlow = "shared_uiflow";
    private const string Dataverse = "shared_commondataserviceforapps";

    /// <summary>The closed set of licence drivers, in report order; <c>Where</c> is empty here. A driver outside it needs a new milestone (plan M107, Data row 4).</summary>
    public static IReadOnlyList<InventoryDriver> Drivers { get; } =
    [
        new("http-action", "A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow.",
            [Designation, "https://learn.microsoft.com/training/modules/http-connectors/2-webhook", Types], Verified, []),
        new("custom-connector", "Custom connectors are premium: an app's users need Power Apps Premium or per app, and a flow needs Power Automate Premium or a Process licence.",
            [Designation, Types], Verified, []),
        new("dataverse-in-canvas-app", "A canvas app that uses Dataverse needs Power Apps Premium or per app for its users.", [AppsFaq], Verified, []),
        new("model-driven-app", "A model-driven app is premium: its users need Power Apps Premium or per app.", [AppsFaq], Verified, []),
        new("desktop-flow-call", "Running a desktop flow from a cloud flow needs Power Automate Premium for the connection's user; an unattended run also needs a Process licence or the unattended RPA add-on.",
            ["https://learn.microsoft.com/connectors/uiflow/", "https://learn.microsoft.com/troubleshoot/power-platform/power-automate/desktop-flows/desktop-flow-run-time-license-check", Types], Verified, []),
    ];

    // NonBacktracking, as Redaction's URL pattern: linear time on a long name.
    [GeneratedRegex(@"[a-z0-9._%+-]+@[a-z0-9-]+(?:\.[a-z0-9-]+)+", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex Address();

    [GeneratedRegex(@"\{?[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\}?|[0-9a-f]{32,}", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex Id();

    /// <summary>Builds the sections of each solution, in the order given; names are redacted (Data row 3) unless <paramref name="showValues"/>, which also keeps literal hosts. The flows are parsed through <see cref="RuleHelpers.CloudFlows"/>.</summary>
    public static Inventory Build(IEnumerable<LoadedSolution> solutions, bool showValues = false) =>
        new([.. solutions.Select(s => One(s, showValues))], showValues);

    /// <summary>URLs, GUIDs (hyphenated, with or without braces), runs of 32 or more hex digits and email addresses replaced.</summary>
    internal static string Redact(string text) => Id().Replace(Address().Replace(Redaction.Text(text), "[redacted address]"), "[id]");

    private static InventorySolution One(LoadedSolution loaded, bool show)
    {
        var s = loaded.Solution;
        string N(string text) => show ? TextReport.Clean(text) : Redact(TextReport.Clean(text));
        IReadOnlyList<string> Names(IEnumerable<string> raw) =>
            [.. raw.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Select(N)];

        var flows = RuleHelpers.CloudFlows(s).ToList();
        var notRead = new List<string>();
        if (loaded.FromYaml)
        {
            notRead.Add(YamlNotRead);
        }

        var apps = new List<CanvasAppMeta>();
        foreach (var app in s.CanvasApps)
        {
            if (app.Connections is null || app.Tables is null)
            {
                notRead.Add($"canvas app '{N(app.Name)}': its connections or tables can't be read, so its connectors and drivers are not reported");
            }
            else
            {
                apps.Add(app);
            }
        }

        // Custom connectors: distinct ignoring case (the first spelling kept), in the solution when any source says so.
        var custom = s.RootComponents.Where(c => c.Type == "372").Select(c => (Name: c.SchemaName, InSolution: true))
            .Concat(s.MissingDependencies.Where(d => d.Required.Type == "372").Select(d => (Name: d.Required.SchemaName, InSolution: false)))
            .Concat(apps.SelectMany(a => a.Connectors ?? []).Select(n => (Name: (string?)n, InSolution: false)))
            .Select(c => (Name: string.IsNullOrEmpty(c.Name) ? "(no schema name)" : c.Name, c.InSolution, Known: !string.IsNullOrEmpty(c.Name)))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Name: g.First().Name, InSolution: g.Any(c => c.InSolution), g.First().Known))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customIds = custom.Where(c => c.Known).Select(c => c.Name.ToLowerInvariant()).ToList();

        // Data row 2a: the name itself, or the observed `shared_<name with - for _>-5f<suffix>` form; the longest name wins.
        string? ConnectorId(string? raw)
        {
            var id = Segment(raw);
            if (id is null)
            {
                return null;
            }

            string? best = null;
            foreach (var name in customIds)
            {
                if ((id == name || id.StartsWith("shared_" + name.Replace('_', '-') + "-5f", StringComparison.Ordinal)) && (best is null || name.Length > best.Length))
                {
                    best = name;
                }
            }

            return best ?? id;
        }

        var uses = new List<(string Id, string? Reference, InventoryUser? User)>();
        foreach (var r in s.ConnectionReferences)
        {
            if (ConnectorId(r.ConnectorId) is { } id)
            {
                uses.Add((id, NullIfEmpty(r.LogicalName), null));
            }
        }

        foreach (var (w, f) in flows)
        {
            foreach (var r in f.ConnectionReferences)
            {
                if (ConnectorId(r.ApiName) is { } id)
                {
                    uses.Add((id, NullIfEmpty(r.LogicalName), new InventoryUser("flow", w.Name)));
                }
            }
        }

        foreach (var app in apps)
        {
            var kind = app.Type switch { 2 => "customPage", 4 => "codeApp", _ => "canvasApp" };
            foreach (var value in app.Connections!.Values)
            {
                if (ConnectorId(value) is { } id)
                {
                    uses.Add((id, null, new InventoryUser(kind, app.Name)));
                }
            }
        }

        foreach (var tool in s.Agents.SelectMany(a => a.Tools).Where(t => t.Kind == "McpTool" && t.ConnectionReference is not null))
        {
            var reference = s.ConnectionReferences.FirstOrDefault(r => string.Equals(r.LogicalName, tool.ConnectionReference, StringComparison.OrdinalIgnoreCase));
            if (ConnectorId(reference?.ConnectorId) is { } id)
            {
                uses.Add((id, null, new InventoryUser("agentTool", tool.SchemaName)));
            }
        }

        var byId = uses.GroupBy(u => u.Id, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        var connectors = byId.Select(g => new InventoryConnector(
            N(g.Key),
            Names(g.Select(u => u.Reference).OfType<string>()),
            [.. g.Select(u => u.User).OfType<InventoryUser>().Distinct()
                .OrderBy(u => u.Kind, StringComparer.Ordinal).ThenBy(u => u.Name, StringComparer.OrdinalIgnoreCase).ThenBy(u => u.Name, StringComparer.Ordinal)
                .Select(u => u with { Name = N(u.Name) })])).ToList();
        var notClassified = byId.Where(g => g.Key != DesktopFlow && !customIds.Contains(g.Key))
            .Select(g => new InventoryNotClassified(N(g.Key), NotClassifiedNote)).ToList();

        var http = new List<InventoryHttpAction>();
        var desktop = new List<string>();
        var triggers = new List<InventoryRequestTrigger>();
        var children = new List<InventoryChildFlow>();
        foreach (var (w, f) in flows)
        {
            foreach (var a in f.Actions)
            {
                if (a.Endpoint is { } endpoint)
                {
                    http.Add(new InventoryHttpAction(N(w.Name), N(a.Name), TextReport.Clean(a.Type), endpoint, show ? a.Host : null));
                }

                if (a.ApiId?.EndsWith("/" + DesktopFlow, StringComparison.OrdinalIgnoreCase) == true
                    && (string.Equals(a.OperationId, "RunUIFlow_V2", StringComparison.OrdinalIgnoreCase) || string.Equals(a.OperationId, "ExecuteDesktopUIFlow", StringComparison.OrdinalIgnoreCase)))
                {
                    desktop.Add($"flow '{N(w.Name)}' action '{N(a.Name)}'");
                }
            }

            foreach (var t in f.Triggers.Where(t => string.Equals(t.Type, "Request", StringComparison.OrdinalIgnoreCase)))
            {
                triggers.Add(new InventoryRequestTrigger(N(w.Name), N(t.Name), t.Kind is null ? null : N(t.Kind)));
            }

            foreach (var child in f.ChildFlowIds)
            {
                var id = child.Trim().Trim('{', '}');
                var called = s.Workflows.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                children.Add(new InventoryChildFlow(N(w.Name), called is null ? null : N(called.Name)));
            }
        }

        IReadOnlyList<string>[] where =
        [
            [.. http.Select(h => $"flow '{h.Flow}' action '{h.Action}'")],
            [.. custom.Select(c => N(c.Name))],
            Names(apps.Where(a => a.Type is not (2 or 4) && (a.Tables!.Count > 0 || a.Connections!.Values.Any(v => Segment(v) == Dataverse))).Select(a => a.Name)),
            Names(s.AppModules.Select(m => m.UniqueName)),
            desktop,
        ];

        return new InventorySolution(
            loaded.Path,
            N(s.UniqueName),
            [.. Drivers.Select((d, i) => d with { Where = where[i] }).Where(d => d.Where.Count > 0)],
            notClassified,
            connectors,
            [.. custom.Select(c => new InventoryCustomConnector(N(c.Name), c.InSolution))],
            http,
            triggers,
            children,
            notRead);
    }

    /// <summary>The last <c>/</c>-segment of a connector id or api name, in lower case; null when there is none.</summary>
    private static string? Segment(string? raw) =>
        string.IsNullOrEmpty(raw) ? null : NullIfEmpty(raw[(raw.LastIndexOf('/') + 1)..].ToLowerInvariant());

    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

    private sealed record Doc(int SchemaVersion, IReadOnlyList<InventorySolution> Solutions);

    public static void WriteJson(TextWriter writer, Inventory inventory) =>
        writer.Write(JsonSerializer.Serialize(new Doc(JsonOutput.SchemaVersion, inventory.Solutions), ManifestReport.Options) + "\n");

    public static void WriteText(TextWriter writer, Inventory inventory)
    {
        var sb = new StringBuilder();
        foreach (var s in inventory.Solutions)
        {
            sb.Append($"{TextReport.Clean(s.Path)} ({s.UniqueName})\n");
            Section(sb, "licenceDrivers", s.LicenceDrivers, d => $"{d.Id}: {d.Need} where: {string.Join("; ", d.Where)} sources: {string.Join(' ', d.Sources)}");
            Section(sb, "notClassified", s.NotClassified, n => $"{n.Id}: {n.Note}");
            Section(sb, "connectors", s.Connectors, c => $"{c.Id}: references {List(c.References)}; users {List(c.Users.Select(User))}");
            Section(sb, "customConnectors", s.CustomConnectors, c => $"{c.Name} ({(c.InSolution ? "in solution" : "not in solution")})");
            Section(sb, "httpActions", s.HttpActions, h => $"flow '{h.Flow}' action '{h.Action}' {h.Type} {h.Endpoint}" + Host(h, inventory.ShowValues) switch
            {
                null => "",
                "not shown" => " host not shown (--show-values)",
                var host => " host " + host,
            });
            Section(sb, "requestTriggers", s.RequestTriggers, t => $"flow '{t.Flow}' trigger '{t.Trigger}' kind {t.Kind ?? "none"}");
            Section(sb, "childFlows", s.ChildFlows, c => $"flow '{c.Flow}' calls " + (c.Child is null ? "a flow not in this solution" : $"'{c.Child}'"));
            Section(sb, "notRead", s.NotRead, n => n);
        }

        writer.Write(sb.Append(Footer).Append('\n').ToString());

        static void Section<T>(StringBuilder sb, string name, IReadOnlyList<T> items, Func<T, string> line)
        {
            sb.Append($"  {name} ({items.Count}):\n");
            foreach (var text in items.Count == 0 ? ["none"] : items.Select(line))
            {
                sb.Append($"    {text}\n");
            }
        }

        static string List(IEnumerable<string> items) => string.Join(", ", items.DefaultIfEmpty("none"));
    }

    public static void WriteMarkdown(TextWriter writer, Inventory inventory)
    {
        var sb = new StringBuilder();
        foreach (var s in inventory.Solutions)
        {
            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append($"## {Escape(s.Path)} ({Escape(s.UniqueName)})\n");
            Table(sb, "licenceDrivers", ["Driver", "Needs", "Where", "Sources"], s.LicenceDrivers, d => [d.Id, Escape(d.Need), Escape(string.Join("; ", d.Where)), string.Join(" ", d.Sources)]);
            Table(sb, "notClassified", ["Connector", "Note"], s.NotClassified, n => [Escape(n.Id), Escape(n.Note)]);
            Table(sb, "connectors", ["Connector", "References", "Users"], s.Connectors, c =>
                [Escape(c.Id), Escape(string.Join(", ", c.References.DefaultIfEmpty("none"))), Escape(string.Join(", ", c.Users.Select(User).DefaultIfEmpty("none")))]);
            Table(sb, "customConnectors", ["Name", "In solution"], s.CustomConnectors, c => [Escape(c.Name), c.InSolution ? "yes" : "no"]);
            Table(sb, "httpActions", ["Flow", "Action", "Type", "Endpoint", "Host"], s.HttpActions, h =>
                [Escape(h.Flow), Escape(h.Action), Escape(h.Type), h.Endpoint, Escape(Host(h, inventory.ShowValues))]);
            Table(sb, "requestTriggers", ["Flow", "Trigger", "Kind"], s.RequestTriggers, t => [Escape(t.Flow), Escape(t.Trigger), Escape(t.Kind)]);
            Table(sb, "childFlows", ["Flow", "Calls"], s.ChildFlows, c => [Escape(c.Flow), c.Child is null ? "a flow not in this solution" : Escape(c.Child)]);
            Table(sb, "notRead", ["Not read"], s.NotRead, n => [Escape(n)]);
        }

        writer.Write(sb.Append('\n').Append(Footer).Append('\n').ToString());

        static void Table<T>(StringBuilder sb, string name, string[] header, IReadOnlyList<T> items, Func<T, string[]> cells)
        {
            sb.Append($"\n### {name}\n\n");
            if (items.Count == 0)
            {
                sb.Append("None.\n");
                return;
            }

            sb.Append($"| {string.Join(" | ", header)} |\n|{string.Concat(header.Select(_ => "---|"))}\n");
            foreach (var item in items)
            {
                sb.Append($"| {string.Join(" | ", cells(item))} |\n");
            }
        }

        static string Escape(string? text) => MarkdownReport.Escape(text);
    }

    /// <summary>One <c>note:</c> line on stderr per <c>notRead</c> sentence.</summary>
    public static void WriteNotes(TextWriter error, Inventory inventory)
    {
        foreach (var s in inventory.Solutions)
        {
            foreach (var line in s.NotRead)
            {
                error.WriteLine($"note: {TextReport.Clean(s.Path)}: {line}");
            }
        }
    }

    private static string User(InventoryUser u) => $"{u.Kind} '{u.Name}'";

    /// <summary>The host, "not shown" for a literal endpoint whose host is held back, else null.</summary>
    private static string? Host(InventoryHttpAction h, bool show) => h.Host ?? (!show && h.Endpoint == "literal" ? "not shown" : null);
}