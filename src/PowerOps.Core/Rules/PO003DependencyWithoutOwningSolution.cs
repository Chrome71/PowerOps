using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO003DependencyWithoutOwningSolution : IRule
{
    public string Id => "PO003";

    public Severity Severity => Severity.Error;

    public string Title => "Missing dependency has no owning solution";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        solution.MissingDependencies
            .Where(d => string.IsNullOrEmpty(d.Required.Solution))
            .Select(d =>
            {
                var name = d.Required.SchemaName ?? d.Required.Id;
                return new Finding(Id, Severity, $"Required component '{name}' (type {d.Required.Type}) has no owning solution.", solution.ManifestFile, name);
            });
}