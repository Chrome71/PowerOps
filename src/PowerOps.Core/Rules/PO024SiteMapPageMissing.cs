using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO024SiteMapPageMissing : IRule, INotChecked
{
    public string Id => "PO024";

    public Severity Severity => Severity.Error;

    public string Title => "Site map opens a custom page that is not in the solution";

    public string Fix => "Add the custom page to the solution, or add the solution that holds it as a dependency.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        solution.CanvasApps.Count == 0
            ? []
            : MissingPages(solution).Select(m => new Finding(
                Id,
                Severity,
                $"Site map '{m.Map.UniqueName}' opens custom page '{m.Page}', which is not in the solution or its dependencies.",
                m.Map.File,
                m.Page));

    // A trimmed export without CanvasApps/ would give false Errors, so with no canvas metadata a missing page is a note (decision 49).
    public IEnumerable<string> NotChecked(Solution solution)
    {
        var declared = RuleHelpers.DeclaredNames(solution);
        return (solution.CanvasApps.Count == 0
            ? MissingPages(solution).Select(m => $"PO024 not checked for site map '{m.Map.UniqueName}': custom page '{m.Page}' is not in the solution, and the solution holds no canvas apps to check it against.")
            : [])
        .Concat(solution.SiteMaps.SelectMany(m => m.GenPageIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(id => !declared.Contains(id))
            .Select(id => $"PO024 not checked for site map '{m.UniqueName}': generative page '{id}' is not checked, because uxagentprojects/ is not read.")));
    }

    private static IEnumerable<(SiteMap Map, string Page)> MissingPages(Solution solution)
    {
        var held = solution.CanvasApps.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        held.UnionWith(RuleHelpers.DeclaredNames(solution));
        // A custom page (component type 300) that the solution lists as a root component is held without its metadata.
        held.UnionWith(solution.RootComponents.Where(r => r.Type == "300" && r.SchemaName is not null).Select(r => r.SchemaName!));
        return solution.SiteMaps.OrderBy(m => m.File, StringComparer.Ordinal).SelectMany(m => m.Pages
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(p => !held.Contains(p))
            .Select(p => (m, p)));
    }
}