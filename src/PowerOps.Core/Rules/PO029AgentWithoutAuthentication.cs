using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO029AgentWithoutAuthentication : IRule, INotChecked
{
    public string Id => "PO029";

    public Severity Severity => Severity.Warning;

    public string Title => "Agent authentication is None";

    public string Fix => "Set the agent's authentication to Authenticate with Microsoft or Authenticate manually.";

    // 1 is None in Learn's bot table; 0 (Unspecified) is the column's default and is not judged (M48 decisions Q8).
    public IEnumerable<Finding> Evaluate(Solution solution) =>
        Agents.Placed(solution).Where(a => a.AuthenticationMode == 1)
            .Select(a => new Finding(Id, Severity, $"Agent '{a.SchemaName}' has authentication None, so anyone with its link can chat with it.", a.File, a.SchemaName));

    public IEnumerable<string> NotChecked(Solution solution) =>
        Agents.Placed(solution).Where(a => a.AuthenticationMode is null).Select(a => $"PO029 not checked for agent '{a.SchemaName}': its bot.xml has no authenticationmode.");
}