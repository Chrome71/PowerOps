using PowerOps.Core.Diff;
using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO034RoleWritesUnusedTable : IRule, INotChecked
{
    public string Id => "PO034";

    public Severity Severity => Severity.Warning;

    public string Title => "Security role can create or write a table that no component in the solution uses";

    public string Fix => "Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        var reason = Unreadable(solution);
        return reason is not null ? [] : Left(solution).SelectMany(x => x.Tables.Select(t =>
            new Finding(Id, Severity, $"Role '{x.Role.Name}' has {t.Verbs} on '{t.Table}', which no component in the solution uses, so it may grant more than the solution needs.", x.Role.File, x.Role.Name)));
    }

    // Plugin code, classic workflows and agent parts it can't place can write any table, so a finding there could be wrong. A business rule counts for its primary table and a Dataverse tool for the table it names (M74).
    public IEnumerable<string> NotChecked(Solution solution) =>
        Unreadable(solution) is { } reason
            ? Left(solution).Select(x => $"PO034 not checked for role '{x.Role.Name}': {x.Tables.Count} of the tables it can create or write are used by no component PowerOps reads, and the solution holds {reason}, whose table use isn't read.")
            : [];

    private static string? Unreadable(Solution s) =>
        s.RootComponents.Any(c => c.Type == "91") ? "a plugin assembly"
        : s.CanvasApps.Any(c => c.Tables is null) ? "a canvas app with no readable CdsDependencies"
        : s.Workflows.Any(w => w.ClientData is null && !IsBusinessRule(w)) ? "a workflow with no readable definition"
        : AgentBlocker(s);

    private static bool IsBusinessRule(Workflow w) => w is { Category: 2, PrimaryEntity: not null };

    // The first agent, in order of its bot.xml, that has a part whose table use isn't read: an unread file, another component, a topic with an action node, then a tool.
    private static string? AgentBlocker(Solution s)
    {
        foreach (var a in s.Agents.OrderBy(a => a.File, StringComparer.Ordinal))
        {
            var at = $"agent '{a.SchemaName}'";
            if (a.UnreadFiles.FirstOrDefault() is { } unread)
            {
                return $"{at} file '{unread}' (YAML keys PowerOps can't map)";
            }

            if (a.OtherComponents.FirstOrDefault() is { } other)
            {
                return $"{at} file '{other.File}' ({(other.Kind is null ? "a component with no kind" : $"a component of kind '{Cut(other.Kind)}'")})";
            }

            if (a.ActionDialogs.FirstOrDefault() is { } dialog)
            {
                return $"{at} file '{dialog.File}' (a topic or child agent with a '{Cut(dialog.Kind!)}' node)";
            }

            foreach (var t in a.Tools.OrderBy(t => t.File, StringComparer.Ordinal))
            {
                if (ToolBlocker(s, t) is { } why)
                {
                    return $"{at} tool '{t.SchemaName}' ({why})";
                }
            }
        }

        return null;
    }

    private static string? ToolBlocker(Solution s, AgentTool t) =>
        t.Kind == "McpTool" || t.Mcp ? "an MCP tool"
        : t.Action == "InvokeConnectorTaskAction" ? t.Table switch
        {
            null => "it names no table",
            { EntitySet: null } => "its entityName is not a literal entity set name",
            { Name: null } => $"its entity set '{Cut(t.Table.EntitySet!)}' names no table in the solution",
            _ => null,
        }
        : t.Action == "InvokeFlowTaskAction" ? (t.FlowId is not null && s.Workflows.Any(w => w.Id == t.FlowId) ? null : "its flow is not in the solution")
        : t.Action is null ? "it has no action kind"
        : $"its action is '{Cut(t.Action)}'";

    private static string Cut(string value) => SolutionDiff.Truncate(value, 100);

    // Per custom role with known privileges: the tables it can Create or Write that nothing in the solution uses, sorted ordinal ignoring case, with the verbs named.
    private static IEnumerable<(Role Role, List<(string Table, string Verbs)> Tables)> Left(Solution s)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UserEntityUISettings" }; // Learn lists Create, Read and Write on it among the minimum privileges.
        used.UnionWith(s.RootComponents.Where(c => c.Type == "1" && c.SchemaName is not null).Select(c => c.SchemaName!));
        used.UnionWith(s.MissingDependencies.Where(d => d.Required.Type == "1" && d.Required.SchemaName is not null).Select(d => d.Required.SchemaName!));
        used.UnionWith(s.CanvasApps.SelectMany(c => c.Tables ?? []));
        used.UnionWith(s.Workflows.Where(IsBusinessRule).Select(w => w.PrimaryEntity!));
        used.UnionWith(s.Agents.SelectMany(a => a.Tools).Select(t => t.Table?.Name).OfType<string>()); // a guard for a table file whose root component is missing; in an export that table is already used

        // Whether a table appears in some flow, found once per table and shared by every role.
        var inFlow = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool InFlow(string table)
        {
            if (!inFlow.TryGetValue(table, out var found))
            {
                // shortcut: a case-insensitive substring of the flow's clientdata, which can only hide a finding, never add one; upgrade to reading the flow's table names if a missed finding is reported.
                inFlow[table] = found = s.Workflows.Any(w => w.ClientData?.Contains(table, StringComparison.OrdinalIgnoreCase) == true);
            }

            return found;
        }

        foreach (var role in SecurityRoles.Custom(s).Where(r => r.Privileges is not null))
        {
            var tables = new Dictionary<string, (string Table, bool Create, bool Write)>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in role.Privileges!)
            {
                var create = SecurityRoles.Table(p, "Create");
                if ((create ?? SecurityRoles.Table(p, "Write")) is not { } table || used.Contains(table))
                {
                    continue;
                }

                var (first, c, w) = tables.GetValueOrDefault(table, (table, false, false));
                tables[table] = (first, c || create is not null, w || create is null);
            }

            var left = tables.Values.Where(t => !InFlow(t.Table))
                .OrderBy(t => t.Table, StringComparer.OrdinalIgnoreCase)
                .Select(t => (t.Table, t.Create && t.Write ? "Create and Write" : t.Create ? "Create" : "Write"))
                .ToList();
            if (left.Count > 0)
            {
                yield return (role, left);
            }
        }
    }
}