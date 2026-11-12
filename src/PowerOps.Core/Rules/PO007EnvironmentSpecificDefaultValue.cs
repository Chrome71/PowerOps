using System.Text.RegularExpressions;

using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed partial class PO007EnvironmentSpecificDefaultValue : IRule
{
    public string Id => "PO007";

    public Severity Severity => Severity.Warning;

    public string Title => "Environment variable default value looks environment-specific";

    public string Fix => "Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere.";

    [GeneratedRegex(@"https?://[^\s/]+", RegexOptions.IgnoreCase)]
    private static partial Regex Url();

    [GeneratedRegex(@"^\{?[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\}?$", RegexOptions.IgnoreCase)]
    private static partial Regex Guid();

    // NonBacktracking: the backtracking engine took quadratic time on a long value of dots (R-3).
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.NonBacktracking)]
    private static partial Regex Email();

    // The trimmed value is classified; null for an empty value or one that matches no pattern.
    private static string? Classify(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null
            : Url().IsMatch(value) ? "a URL"
            : Guid().IsMatch(value) ? "a GUID"
            : Email().IsMatch(value) ? "an email address"
            : null;
    }

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var v in solution.EnvironmentVariables)
        {
            if (Classify(v.DefaultValue) is { } kind)
            {
                yield return new Finding(Id, Severity, $"Environment variable '{v.SchemaName}' has a default value that looks like {kind}.", v.File, v.SchemaName);
            }
        }

        // A default held in a flow is left to the definition's finding when it is the definition's own. The finding never holds the value.
        foreach (var (w, flow) in RuleHelpers.CloudFlows(solution))
        {
            foreach (var d in flow.EnvironmentVariableDefaults)
            {
                if (Classify(d.Value) is { } kind
                    && !solution.EnvironmentVariables.Any(v => v.SchemaName.Equals(d.SchemaName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(v.DefaultValue?.Trim(), d.Value.Trim(), StringComparison.Ordinal)))
                {
                    yield return new Finding(Id, Severity, $"Flow '{w.Name}' holds a default for environment variable '{d.SchemaName}' that looks like {kind}.", w.File, d.SchemaName);
                }
            }
        }
    }
}