using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO027McpToolWithoutGenerativeOrchestration : IRule, INotChecked
{
    public string Id => "PO027";

    public Severity Severity => Severity.Error;

    public string Title => "MCP server on an agent with generative orchestration off";

    public string Fix => "Turn on generative orchestration for the agent, or remove the MCP server.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var agent in Agents.Placed(solution).Where(a => a.Configuration!.GenerativeOrchestration == false))
        {
            foreach (var tool in Agents.Tools(agent).Where(t => t.Mcp))
            {
                yield return new Finding(Id, Severity, $"Agent '{agent.SchemaName}' has MCP server '{tool.SchemaName}', but its generative orchestration is off, which MCP needs.", tool.File, tool.SchemaName);
            }
        }
    }

    public IEnumerable<string> NotChecked(Solution solution) =>
        Agents.HarnessNotes(solution)
            .Concat(Agents.Placed(solution).SelectMany(a => a.UnreadFiles.Select(f =>
                $"PO027 not checked for agent '{a.SchemaName}': '{f}' is valid YAML whose keys PowerOps can't map, so the tool it may hold isn't read, and PO030 and PO031 don't check it either.")))
            .Concat(
                Agents.Placed(solution)
                    .Where(a => a.Configuration!.GenerativeOrchestration is null && a.Tools.Any(t => t.Mcp))
                    .Select(a => $"PO027 not checked for agent '{a.SchemaName}': its configuration.json doesn't say whether generative orchestration is on."));
}