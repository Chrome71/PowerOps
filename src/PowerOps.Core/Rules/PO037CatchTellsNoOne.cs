using PowerOps.Core.Flows;
using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO037CatchTellsNoOne : IRule
{
    private static readonly HashSet<string> OutwardTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OpenApiConnection", "OpenApiConnectionWebhook", "ApiConnection", "ApiConnectionWebhook", "Http", "HttpWebhook", "Workflow",
    };

    public string Id => "PO037";

    public Severity Severity => Severity.Warning;

    public string Title => "Flow catches a failure, tells no one and ends as succeeded";

    public string Fix => "End the failure path with a Terminate action set to Failed, or send the failure to someone, so a failed run is noticed.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var (w, flow) in RuleHelpers.CloudFlows(solution))
        {
            if (FirstSilentHandler(flow.Actions) is { } handler)
            {
                yield return new Finding(Id, Severity, $"Flow '{w.Name}' has a failure handler '{handler}' that tells no one, so the run ends as succeeded.", w.File, w.Name);
            }
        }
    }

    private static bool Has(FlowRunAfter r, string status) => r.Statuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    /// <summary>The first handler in walk order when no action on the failure path, nor inside one, reaches anyone; null when there is no handler or the run can still end otherwise than succeeded.</summary>
    private static string? FirstSilentHandler(IReadOnlyList<FlowAction> actions)
    {
        var handlers = Enumerable.Range(0, actions.Count).Where(i => actions[i].RunAfter.Any(r => Has(r, "Failed") || Has(r, "TimedOut"))).ToList();
        if (handlers.Count == 0 || actions.Any(a =>
            string.Equals(a.Type, "Response", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(a.Type, "Terminate", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(a.RunStatus, "Failed", StringComparison.OrdinalIgnoreCase) || string.Equals(a.RunStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)))))
        {
            return null;
        }

        // outward[k] counts the outward actions among the first k, so a Descendants slice is checked in O(1).
        var outward = new int[actions.Count + 1];
        var next = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var i = 0; i < actions.Count; i++)
        {
            outward[i + 1] = outward[i] + (actions[i].Type is { } t && OutwardTypes.Contains(t) ? 1 : 0);
            foreach (var r in actions[i].RunAfter.Where(r => Has(r, "Succeeded")))
            {
                if (!next.TryGetValue(r.Action, out var list))
                {
                    next[r.Action] = list = [];
                }

                list.Add(i);
            }
        }

        // runAfter names an action by name, so a name repeated in two containers is followed once, from either.
        var expanded = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<int>(handlers);
        while (queue.TryDequeue(out var i))
        {
            if (outward[i + 1 + actions[i].Descendants] > outward[i])
            {
                return null;
            }

            if (expanded.Add(actions[i].Name) && next.TryGetValue(actions[i].Name, out var after))
            {
                after.ForEach(queue.Enqueue);
            }
        }

        return actions[handlers[0]].Name;
    }
}