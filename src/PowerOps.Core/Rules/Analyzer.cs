using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public static class Analyzer
{
    public static IReadOnlyList<IRule> Rules { get; } = new IRule[]
    {
        new PO001UndefinedConnectionReference(),
        new PO002RequiredEnvironmentVariableWithoutValue(),
        new PO003DependencyWithoutOwningSolution(),
        new PO004UndefinedChildFlow(),
        new PO005UnusedConnectionReference(),
        new PO006HardCodedEnvironmentUrl(),
        new PO007EnvironmentSpecificDefaultValue(),
        new PO008ShippedCurrentValue(),
        new PO009ForeignPrefix(),
        new PO010DefaultPublisher(),
        new PO011FlowOff(),
    }.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();

    /// <summary>All findings, sorted by file, rule id, component, message (ordinal).</summary>
    public static IReadOnlyList<Finding> Run(Solution solution) =>
        Rules.SelectMany(r => r.Evaluate(solution))
            .OrderBy(f => f.File, StringComparer.Ordinal)
            .ThenBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.Component, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToList();
}