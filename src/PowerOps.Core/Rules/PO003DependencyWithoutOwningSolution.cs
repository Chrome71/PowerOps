using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO003DependencyWithoutOwningSolution : IRule
{
    public string Id => "PO003";

    public Severity Severity => Severity.Error;

    public string Title => "Missing dependency has no owning solution";

    public string Fix => "Add the missing component to the solution, or import the solution that provides it first.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        solution.MissingDependencies
            .Where(d => string.IsNullOrEmpty(d.Required.Solution))
            .GroupBy(d => (d.Required.Type, Name: d.Required.SchemaName ?? d.Required.Id), new KeyComparer())
            .Select(g => new Finding(
                Id,
                Severity,
                $"Missing dependency '{g.Key.Name}' has no owning solution; the import will fail.",
                solution.ManifestFile,
                g.Key.Name));

    private sealed class KeyComparer : IEqualityComparer<(string Type, string? Name)>
    {
        public bool Equals((string Type, string? Name) x, (string Type, string? Name) y) =>
            string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Type, string? Name) k) =>
            HashCode.Combine(k.Type.ToLowerInvariant(), k.Name?.ToLowerInvariant());
    }
}