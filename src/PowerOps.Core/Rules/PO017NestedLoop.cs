using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO017NestedLoop : IRule
{
    public string Id => "PO017";

    public Severity Severity => Severity.Warning;

    public string Title => "Cloud flow runs a loop inside a loop";

    public string Fix => "Replace the inner loop with one query, filter or bulk action over the whole set, or fetch the related rows with the outer query's expand option and use one loop.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var (w, flow) in RuleHelpers.CloudFlows(solution))
        {
            foreach (var n in flow.NestedLoops)
            {
                yield return new Finding(Id, Severity, $"Flow '{w.Name}' runs loop '{n.Inner}' inside loop '{n.Outer}'.", w.File, n.Inner);
            }
        }
    }
}