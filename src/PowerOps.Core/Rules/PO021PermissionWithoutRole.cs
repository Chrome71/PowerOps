using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO021PermissionWithoutRole : IRule
{
    public string Id => "PO021";

    public Severity Severity => Severity.Warning;

    public string Title => "Power Pages table permission has no web role";

    public string Fix => "Associate the table permission with the web roles that need it, or delete it.";

    // A child takes its parent's roles, so only a permission with no parent is judged by its own.
    public IEnumerable<Finding> Evaluate(Solution solution) =>
        PowerPages.Permissions(solution)
            .Where(p => PowerPages.ParentId(p) is null && PowerPages.OwnRoles(p).Count == 0)
            .Select(p => new Finding(
                Id,
                Severity,
                $"Table permission '{p.Name}' on '{PowerPages.Table(p)}' has no web role, so it gives no access; add a role or remove it before Production.",
                p.File,
                p.Name));
}