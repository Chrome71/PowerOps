using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO015CreateTriggerWithFilterColumns : IRule
{
    public string Id => "PO015";

    public Severity Severity => Severity.Error;

    public string Title => "Dataverse row-created trigger has filter columns";

    public string Fix => "Clear the trigger's filter columns, or change its change type to Create or Update.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var (w, flow) in RuleHelpers.CloudFlows(solution))
        {
            foreach (var t in flow.Triggers)
            {
                // Message 1 is Create only; the other types were not tested, so they stay quiet.
                if (t.OperationId == "SubscribeWebhookTrigger"
                    && t.Message == 1
                    && !string.IsNullOrWhiteSpace(t.FilteringAttributes))
                {
                    yield return new Finding(Id, Severity, $"Flow '{w.Name}' has filter columns on a create-only trigger of '{t.Entity}'; in observed runs such a flow never started.", w.File, t.Name);
                }
            }
        }
    }
}