using PowerOps.Core.Model;
using PowerOps.Core.Readers;

namespace PowerOps.Core.Rules;

public sealed class PO032AppSharedWithAdminsOnly : IRule, INotChecked
{
    public string Id => "PO032";

    public Severity Severity => Severity.Warning;

    public string Title => "Model-driven app is shared only with admin roles";

    public string Fix => "Share the app with the security roles its users hold, and remove System Administrator and System Customizer from it unless they need the app.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        var inSolution = RoleIds(solution);
        return Apps(solution).Where(a => Classify(a, inSolution) is (_, 0, 0))
            .Select(a => new Finding(Id, Severity, $"App '{a.UniqueName}' is shared only with System Administrator or System Customizer, so users without an admin role may not be able to open it.", a.File, a.UniqueName));
    }

    // An id that is neither a documented template nor a role in the solution may be a custom role of another solution, or Environment Maker, which has no template.
    public IEnumerable<string> NotChecked(Solution solution)
    {
        var inSolution = RoleIds(solution);
        return Apps(solution).Select(a => (App: a, Kind: Classify(a, inSolution)))
            .Where(x => x.Kind is (_, 0, > 0))
            .Select(x => $"PO032 not checked for app '{x.App.UniqueName}': {x.Kind.Unknown} of its role map ids are neither a documented role template nor a role in the solution.");
    }

    private static IEnumerable<AppModule> Apps(Solution solution) =>
        solution.AppModules.Where(a => a.Roles.Count > 0).OrderBy(a => a.File, StringComparer.Ordinal).ThenBy(a => a.UniqueName, StringComparer.Ordinal);

    private static HashSet<string> RoleIds(Solution solution) => new(solution.Roles.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);

    // Admin: any admin template id; Others: ids of another template or a role in the solution (these make the app open to non-admins); Unknown: the rest.
    private static (bool Admin, int Others, int Unknown) Classify(AppModule app, HashSet<string> inSolution)
    {
        var admin = false;
        var others = 0;
        var unknown = 0;
        foreach (var id in app.Roles.Select(r => SolutionParser.NormalizeId(r) ?? r).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (SecurityRoles.IsAdmin(id))
            {
                admin = true;
            }
            else if (SecurityRoles.IsTemplate(id) || inSolution.Contains(id))
            {
                others++;
            }
            else
            {
                unknown++;
            }
        }

        return (admin, others, unknown);
    }
}