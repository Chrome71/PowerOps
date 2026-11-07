using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO033RoleWithoutAppBaseline : IRule, INotChecked
{
    public string Id => "PO033";

    public Severity Severity => Severity.Warning;

    public string Title => "Custom security role has no privilege to open a model-driven app";

    public string Fix => "Copy the role from App Opener, or give its users App Opener as well, and test the app as a user who holds only those roles.";

    // Learn ties the minimum privileges to opening a model-driven app, so a solution without one (roles for flows, canvas apps or app users) is not checked (decisions Q10).
    public IEnumerable<Finding> Evaluate(Solution solution) =>
        SecurityRoles.Custom(solution).Where(r => solution.AppModules.Count > 0 && r.Privileges is { } p && HasTable(p) && !p.Any(x => SecurityRoles.AppOpening.Contains(x, StringComparer.OrdinalIgnoreCase)))
            .Select(r => new Finding(Id, Severity, $"Role '{r.Name}' has table privileges but none of prvReadAppModule, prvReadUser and prvReadUserQuery, so a user who holds only this role may not be able to open a model-driven app.", r.File, r.Name));

    // The one note for PO033 and PO034: both read the privileges, and PO033 only where the solution holds a model-driven app.
    public IEnumerable<string> NotChecked(Solution solution) =>
        SecurityRoles.Custom(solution).Where(r => r.Privileges is null)
            .Select(r => $"{(solution.AppModules.Count > 0 ? "PO033 and PO034" : "PO034")} not checked for role '{r.Name}': its file has no RolePrivileges element, so its privileges are not known.");

    private static bool HasTable(IEnumerable<string> privileges) => privileges.Any(p => SecurityRoles.Table(p, SecurityRoles.Verbs) is not null);
}