using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO022ChildPermissionRoles : IRule, INotChecked
{
    public string Id => "PO022";

    public Severity Severity => Severity.Warning;

    public string Title => "Power Pages child table permission is not in effect through its parent";

    public string Fix => "Give the parent table permission the child's web roles or remove them from the child, and keep the parent active.";

    public IEnumerable<Finding> Evaluate(Solution solution) => Check(solution).Findings;

    public IEnumerable<string> NotChecked(Solution solution) => Check(solution).Notes;

    private (List<Finding> Findings, List<string> Notes) Check(Solution solution)
    {
        var findings = new List<Finding>();
        var notes = new List<string>();
        var all = PowerPages.ById(solution, PowerPages.TablePermission);
        var roles = PowerPages.ById(solution, PowerPages.WebRole);
        var chains = new PowerPages.Chains(all);
        foreach (var child in PowerPages.Permissions(solution).Where(p => PowerPages.ParentId(p) is not null))
        {
            if (!all.TryGetValue(PowerPages.ParentId(child)!, out var parent))
            {
                notes.Add($"PO022 not checked for table permission '{child.Name}': its parent, or a permission above it, is not found in the solution.");
                continue;
            }

            if (!parent.Active)
            {
                findings.Add(new Finding(Id, Severity, $"Table permission '{child.Name}' has the inactive parent '{parent.Name}', so it is not in effect; fix it before Production.", child.File, child.Name));
                continue;
            }

            var above = chains.Of(parent);
            if (above.Off)
            {
                continue; // an ancestor above the parent is inactive; the parent gets its own finding
            }

            if (!above.Resolved)
            {
                notes.Add($"PO022 not checked for table permission '{child.Name}': its parent, or a permission above it, is not found in the solution.");
                continue;
            }

            findings.AddRange(PowerPages.OwnRoles(child).Where(r => !above.Roles.Contains(r)).Select(r => new Finding(
                Id,
                Severity,
                $"Table permission '{child.Name}' has web role '{PowerPages.RoleName(r, roles)}', which its parent '{parent.Name}' doesn't have; fix the roles before Production.",
                child.File,
                child.Name)));
        }

        return (findings, notes);
    }
}