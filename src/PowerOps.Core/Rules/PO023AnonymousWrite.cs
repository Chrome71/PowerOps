using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO023AnonymousWrite : IRule, INotChecked
{
    public string Id => "PO023";

    public Severity Severity => Severity.Warning;

    public string Title => "Power Pages table permission lets anonymous users create, write or delete";

    public string Fix => "Remove create, write and delete from the anonymous role's table permission, or grant them to a role that requires sign-in.";

    public IEnumerable<Finding> Evaluate(Solution solution) => Check(solution).Findings;

    public IEnumerable<string> NotChecked(Solution solution) => Check(solution).Notes;

    private (List<Finding> Findings, List<string> Notes) Check(Solution solution)
    {
        var findings = new List<Finding>();
        var notes = new List<string>();
        var all = PowerPages.ById(solution, PowerPages.TablePermission);
        var roles = PowerPages.ById(solution, PowerPages.WebRole);
        var chains = new PowerPages.Chains(all);
        foreach (var p in PowerPages.Permissions(solution))
        {
            string[] granted = [.. new[] { ("create", PowerPages.Flag(p, "create")), ("write", PowerPages.Flag(p, "write")), ("delete", PowerPages.Flag(p, "delete")) }
                .Where(x => x.Item2).Select(x => x.Item1)];
            if (granted.Length == 0)
            {
                continue;
            }

            var effective = chains.Of(p);
            if (effective.Off)
            {
                continue; // an inactive ancestor: the permission is not in effect
            }

            if (!effective.Resolved)
            {
                notes.Add($"PO023 not checked for table permission '{p.Name}': its parent, or a permission above it, is not found in the solution.");
            }

            foreach (var id in effective.Roles)
            {
                if (!roles.TryGetValue(id, out var role))
                {
                    notes.Add($"PO023 not checked for table permission '{p.Name}': web role '{id}' is not in the solution.");
                }
                else if (role.Active && PowerPages.Flag(role, "anonymoususersrole"))
                {
                    findings.Add(new Finding(
                        Id,
                        Severity,
                        $"Table permission '{p.Name}' on '{PowerPages.Table(p)}' lets the anonymous role '{PowerPages.RoleName(id, roles)}' {List(granted)}, so anyone can without signing in; confirm it before Production.",
                        p.File,
                        p.Name));
                }
            }
        }

        return (findings, notes);
    }

    private static string List(string[] items) => items.Length == 1 ? items[0] : $"{string.Join(", ", items[..^1])} and {items[^1]}";
}