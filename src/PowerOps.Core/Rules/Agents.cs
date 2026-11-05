using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

/// <summary>Which agents PO027 to PO031 read: those on the standard harness (M48 decisions Q4).</summary>
internal static class Agents
{
    /// <summary>A configuration.json that is a JSON object whose recognizer kind is absent, GenerativeAIRecognizer or VirtualAgentsRecognizer (the documented default); any other agent is not read.</summary>
    public static bool InStandardHarness(Agent agent) => agent.Configuration is { Recognizer: null or "GenerativeAIRecognizer" or "VirtualAgentsRecognizer" };

    /// <summary>The agents the rules read, in ordinal order of their file.</summary>
    public static IEnumerable<Agent> Placed(Solution solution) =>
        solution.Agents.Where(InStandardHarness).OrderBy(a => a.File, StringComparer.Ordinal);

    /// <summary>The `TaskDialog` tools in ordinal order of their file; the `McpTool` tools of the GitHub Copilot harness's layout are not read (M51 decisions Q7).</summary>
    public static IEnumerable<AgentTool> Tools(Agent agent) => agent.Tools.Where(t => t.Kind == "TaskDialog").OrderBy(t => t.File, StringComparer.Ordinal);

    /// <summary>The instructions of the agent's Custom GPT component, or null when it has none; those from configuration.json are not read (M51 decisions Q7).</summary>
    public static string? Instructions(Agent agent) => agent.InstructionsFromConfiguration ? null : agent.Instructions;

    /// <summary>One note per agent that is not read, naming all five rules.</summary>
    public static IEnumerable<string> HarnessNotes(Solution solution) =>
        solution.Agents.Where(a => !InStandardHarness(a)).OrderBy(a => a.File, StringComparer.Ordinal).Select(a => a.Configuration is null
            ? $"PO027 to PO031 not checked for agent '{a.SchemaName}': its configuration.json is missing or isn't a JSON object."
            : $"PO027 to PO031 not checked for agent '{a.SchemaName}': its recognizer is '{a.Configuration.Recognizer}', not GenerativeAIRecognizer or VirtualAgentsRecognizer, so it may not run on the standard harness that these rules read.");
}