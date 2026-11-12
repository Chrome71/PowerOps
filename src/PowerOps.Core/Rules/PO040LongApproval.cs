using System.Xml;

using PowerOps.Core.Flows;
using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO040LongApproval : IRule, INotChecked
{
    private static readonly HashSet<string> WaitOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "StartAndWaitForAnApproval", "WaitForAnApproval", "StartAndWaitForATextSuggestionApproval", "approvalSubscribe", "approvalSubscribeV2",
    };

    public string Id => "PO040";

    public Severity Severity => Severity.Warning;

    public string Title => "Approval can wait as long as a run can last";

    public string Fix => "Set the approval's timeout under 30 days and handle its expiry, or create the approval in one flow and act on the response in a second.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        from x in Timed(solution)
        where x.Timeout is { } t && t >= TimeSpan.FromDays(30)
        select new Finding(Id, Severity, $"Flow '{x.Flow}' approval '{x.Action}' has a timeout of {x.Timeout!.Value.Days} days; a run lasts at most 30 days.", x.File, x.Action);

    public IEnumerable<string> NotChecked(Solution solution) =>
        from x in Timed(solution)
        where x.Timeout is null
        select $"PO040 not checked for flow '{x.Flow}': approval '{x.Action}' has a timeout this rule can't read as a duration.";

    /// <summary>Every approvals wait action with a timeout, and the timeout read as a duration (null when it does not read).</summary>
    private static IEnumerable<(string Flow, string File, string Action, TimeSpan? Timeout)> Timed(Solution solution) =>
        from x in RuleHelpers.CloudFlows(solution)
        from a in x.Flow.Actions
        where a.Timeout is not null && IsWait(a)
        select (x.Workflow.Name, x.Workflow.File, a.Name, Read(a.Timeout!));

    private static bool IsWait(FlowAction a) =>
        a.ApiId is { } api && api.EndsWith("/shared_approvals", StringComparison.OrdinalIgnoreCase) && a.OperationId is { } op && WaitOperations.Contains(op);

    private static TimeSpan? Read(string timeout)
    {
        try
        {
            return XmlConvert.ToTimeSpan(timeout);
        }
        catch (Exception e) when (e is FormatException or OverflowException)
        {
            return null;
        }
    }
}