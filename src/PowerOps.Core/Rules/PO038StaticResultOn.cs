using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO038StaticResultOn : IRule
{
    public string Id => "PO038";

    public Severity Severity => Severity.Warning;

    public string Title => "Flow action has its static result turned on";

    public string Fix => "Turn off the action's static result before you export, so the action runs instead of returning test output.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        RuleHelpers.CloudFlows(solution).SelectMany(x => x.Flow.Actions.Where(a => a.StaticResult)
            .Select(a => new Finding(Id, Severity, $"Flow '{x.Workflow.Name}' action '{a.Name}' returns its static result instead of running.", x.Workflow.File, a.Name)));
}