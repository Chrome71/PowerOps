using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO031AgentToolCount : IRule, INotChecked
{
    private const int MaxTools = 30;

    public string Id => "PO031";

    public Severity Severity => Severity.Warning;

    public string Title => "Agent has more than 30 tools";

    public string Fix => "Remove tools the agent doesn't need.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        Agents.Placed(solution).Select(a => (Agent: a, Count: Count(a))).Where(x => x.Count > MaxTools)
            .Select(x => new Finding(Id, Severity, $"Agent '{x.Agent.SchemaName}' has {x.Count} tools, more than 30; Learn advises 25 to 30 at most.", x.Agent.File, x.Agent.SchemaName));

    // A connected agent is added as an agent, not a tool, and Learn's 25 to 30 is about tools (M48 decisions Q5).
    public IEnumerable<string> NotChecked(Solution solution) =>
        Agents.Placed(solution).Where(a => Count(a) <= MaxTools && a.Tools.Any(t => t.Mcp))
            .Select(a => $"PO031 not checked for agent '{a.SchemaName}': it has at least one MCP server, and the exports read here don't list the tools an MCP server publishes.");

    private static int Count(Agent agent) => agent.Tools.Count(t => t.Action != "InvokeConnectedAgentTaskAction");
}