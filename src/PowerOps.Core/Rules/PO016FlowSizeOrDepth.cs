using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO016FlowSizeOrDepth : IRule
{
    // The review heuristic of the CAT OverFlow skill, not a product limit.
    private const int MaxActions = 50;
    private const int MaxDepth = 4;

    public string Id => "PO016";

    public Severity Severity => Severity.Warning;

    public string Title => "Cloud flow has more than 50 actions or more than 4 levels of nesting";

    public string Fix => "Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var (w, flow) in RuleHelpers.CloudFlows(solution))
        {
            if (flow.ActionCount > MaxActions || flow.Depth > MaxDepth)
            {
                yield return new Finding(Id, Severity, $"Flow '{w.Name}' has {flow.ActionCount} actions and a nesting depth of {flow.Depth}.", w.File, w.Name);
            }
        }
    }
}