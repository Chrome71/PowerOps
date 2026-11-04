using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO028AgentInstructionsLength : IRule, INotChecked
{
    private const int MaxLength = 8000;

    public string Id => "PO028";

    public Severity Severity => Severity.Warning;

    public string Title => "Agent instructions are empty or longer than 8,000 characters";

    public string Fix => "Write the agent's instructions, in at most 8,000 characters.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var agent in Agents.Placed(solution).Where(a => a.Instructions is not null))
        {
            // A YAML block scalar keeps trailing line breaks the person didn't type, so they aren't counted. Characters are UTF-16 code units, which differ from code points only outside the Basic Multilingual Plane.
            var length = agent.Instructions!.TrimEnd('\r', '\n').Length;
            var message = string.IsNullOrWhiteSpace(agent.Instructions) ? (agent.Configuration!.GenerativeOrchestration == true ? $"Agent '{agent.SchemaName}' has no instructions." : null)
                : length > MaxLength ? $"Agent '{agent.SchemaName}' has instructions of {length} characters, more than the 8,000 that Copilot Studio allows."
                : null;
            if (message is not null)
            {
                yield return new Finding(Id, Severity, message, agent.InstructionsFile ?? agent.File, agent.SchemaName);
            }
        }
    }

    public IEnumerable<string> NotChecked(Solution solution) =>
        Agents.Placed(solution).Where(a => a.Instructions is null).Select(a => $"PO028 not checked for agent '{a.SchemaName}': it has no single Custom GPT component whose instructions are text.");
}