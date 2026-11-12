using PowerOps.Core.Flows;
using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO042SecretWithoutSecureData : IRule
{
    private const string DataverseApi = "/shared_commondataserviceforapps";

    private const string KeyVaultApi = "/shared_keyvault";

    public string Id => "PO042";

    public Severity Severity => Severity.Warning;

    public string Title => "Flow reads or sends a secret without secure inputs or outputs";

    public string Fix => "Turn on secure outputs on every action that reads a secret, and secure inputs on every action that sends one.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        RuleHelpers.CloudFlows(solution).SelectMany(x => x.Flow.Actions.Select(a => Message(x.Workflow.Name, a) is { } message
            ? new Finding(Id, Severity, message, x.Workflow.File, a.Name)
            : null).OfType<Finding>());

    private string? Message(string flow, FlowAction a)
    {
        if (IsSecretRead(a) && !a.SecureOutputs)
        {
            return $"Flow '{flow}' action '{a.Name}' reads a secret without secure outputs, so the secret shows in the run history.";
        }

        return a.Type is { } t && (t.Equals("Http", StringComparison.OrdinalIgnoreCase) || t.Equals("HttpWebhook", StringComparison.OrdinalIgnoreCase)) && a.Authentication && !a.SecureInputs
            ? $"Flow '{flow}' action '{a.Name}' has an authentication block and its secure inputs are off."
            : null;
    }

    private static bool IsSecretRead(FlowAction a) =>
        (EndsWith(a.ApiId, DataverseApi)
            && (Is(a.OperationId, "PerformUnboundAction") || Is(a.OperationId, "PerformUnboundActionWithOrganization"))
            && Is(a.UnboundAction, "RetrieveEnvironmentVariableSecretValue"))
        || (EndsWith(a.ApiId, KeyVaultApi) && (Is(a.OperationId, "GetSecret") || Is(a.OperationId, "GetSecretVersion")));

    private static bool EndsWith(string? value, string suffix) => value?.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) == true;

    private static bool Is(string? value, string expected) => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}