using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO026CodeAppDependencyMissing : IRule, INotChecked
{
    public string Id => "PO026";

    public Severity Severity => Severity.Warning;

    public string Title => "Code app depends on a table or connector that is not in the solution";

    public string Fix => "Add the table or connector to the solution, or add the solution that holds it as a dependency.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        var tables = Held(solution, "1");
        var connectors = Held(solution, "372");
        foreach (var app in CodeApps(solution).Where(a => a.Tables is not null))
        {
            // A table without a customization prefix (no underscore) is not checked: standard tables are part of the system solution, which every environment is expected to have.
            foreach (var t in app.Tables!.Where(t => t.Contains('_') && !tables.Contains(t)))
            {
                yield return Finding(app, "table", t);
            }

            foreach (var c in app.Connectors!.Where(c => !connectors.Contains(c)))
            {
                yield return Finding(app, "connector", c);
            }
        }
    }

    public IEnumerable<string> NotChecked(Solution solution) =>
        CodeApps(solution).SelectMany(a =>
            a.Tables is null ? [$"PO026 not checked for code app '{a.Name}': its CdsDependencies is missing or not in a shape PowerOps reads."]
            // At most five types, so a crafted file can't print one line of megabytes.
            : a.OtherTypes is { Count: > 0 } other ? [$"PO026 not checked for code app '{a.Name}': its CdsDependencies has entries of other component types ({string.Join(", ", other.Take(5))}{(other.Count > 5 ? $", and {other.Count - 5} more" : "")}), which are not checked."]
            : Array.Empty<string>());

    private Finding Finding(CanvasAppMeta app, string kind, string name) =>
        new(Id, Severity, $"Code app '{app.Name}' depends on {kind} '{name}', which is not in the solution or its dependencies.", app.File, name);

    private static IEnumerable<CanvasAppMeta> CodeApps(Solution solution) =>
        solution.CanvasApps.Where(a => a.Type == 4).OrderBy(a => a.File, StringComparer.Ordinal).ThenBy(a => a.Name, StringComparer.Ordinal);

    // Only a root component of the same type holds the name; a declared dependency holds it whatever its type.
    private static HashSet<string> Held(Solution solution, string type)
    {
        var held = RuleHelpers.DeclaredNames(solution);
        held.UnionWith(solution.RootComponents.Where(r => r.Type == type && r.SchemaName is not null).Select(r => r.SchemaName!));
        return held;
    }
}