using PowerOps.Core.Flows;
using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

internal static class RuleHelpers
{
    /// <summary>Modern cloud flows (category 5) with clientdata, parsed once per call. Invalid JSON throws InvalidDataException.</summary>
    public static IEnumerable<(Workflow Workflow, FlowDefinition Flow)> CloudFlows(Solution s) =>
        s.Workflows
            .Where(w => w.Category == 5 && w.ClientData is not null)
            .Select(w => (w, FlowDefinition.Parse(w.ClientData!)));

    /// <summary>True when a MissingDependency names this schema name or id as required: another solution provides it.</summary>
    public static bool IsDeclared(Solution s, string value) =>
        s.MissingDependencies.Any(d =>
            string.Equals(d.Required.SchemaName, value, StringComparison.OrdinalIgnoreCase)
            || string.Equals(d.Required.Id, value, StringComparison.OrdinalIgnoreCase));
}