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

    // Plugin code, classic workflows and agent tools can write any table; PowerOps reads none of them for tables, so a finding there could be wrong.
    public IEnumerable<string> NotChecked(Solution solution) =>
        Unreadable(solution) is { } reason
            ? Left(solution).Select(x => $"PO034 not checked for role '{x.Role.Name}': {x.Tables.Count} of the tables it can create or write are used by no component PowerOps reads, and the solution holds {reason}, whose table use isn't read.")
            : [];

    private static string? Unreadable(Solution s) =>
        s.RootComponents.Any(c => c.Type == "91") ? "a plugin assembly"
        : s.CanvasApps.Any(c => c.Tables is null) ? "a canvas app with no readable CdsDependencies"
        : s.Workflows.Any(w => w.ClientData is null) ? "a workflow with no readable definition"
        : s.Agents.Count > 0 ? "a Copilot Studio agent"
        : null;

    // Per custom role with known privileges: the tables it can Create or Write that nothing in the solution uses, sorted ordinal ignoring case, with the verbs named.
    private static IEnumerable<(Role Role, List<(string Table, string Verbs)> Tables)> Left(Solution s)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UserEntityUISettings" }; // Learn lists Create, Read and Write on it among the minimum privileges.
        used.UnionWith(s.RootComponents.Where(c => c.Type == "1" && c.SchemaName is not null).Select(c => c.SchemaName!));
        used.UnionWith(s.MissingDependencies.Where(d => d.Required.Type == "1" && d.Required.SchemaName is not null).Select(d => d.Required.SchemaName!));
        used.UnionWith(s.CanvasApps.SelectMany(c => c.Tables ?? []));

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

            // shortcut: a case-insensitive substring of the flow's clientdata, which can only hide a finding, never add one; upgrade to reading the flow's table names if a missed finding is reported.
            var left = tables.Values.Where(t => !s.Workflows.Any(w => w.ClientData?.Contains(t.Table, StringComparison.OrdinalIgnoreCase) == true))
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