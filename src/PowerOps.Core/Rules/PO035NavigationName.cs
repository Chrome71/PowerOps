using System.Text.RegularExpressions;

using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

/// <summary>
/// A Dataverse action names a navigation property, in <c>$expand</c> or an <c>item/&lt;name&gt;@odata.bind</c> key, that matches one of its table's exported navigation properties only when case is ignored (M79).
/// An action whose entity set has no exported navigation property, a <c>$expand</c> other than a comma list of plain names, and a name that matches nothing even ignoring case are not checked.
/// </summary>
public sealed partial class PO035NavigationName : IRule
{
    public string Id => "PO035";

    public Severity Severity => Severity.Warning;

    public string Title => "Flow names a navigation property in the wrong case";

    public string Fix => "Write the navigation property name exactly as the table's relationship defines it, in the same case.";

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex PlainName();

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var (workflow, flow) in RuleHelpers.CloudFlows(solution))
        {
            foreach (var action in flow.DataverseActions)
            {
                var exported = action.EntitySet is { } set
                    ? solution.NavigationProperties.Where(n => string.Equals(n.EntitySet, set, StringComparison.OrdinalIgnoreCase)).ToList()
                    : [];
                if (exported.Count == 0)
                {
                    continue;
                }

                var expand = action.Expand?.Split(',').Select(n => n.Trim()).ToList() ?? [];
                var names = (expand.All(n => PlainName().IsMatch(n)) ? expand : []).Concat(action.Binds);
                foreach (var used in names.Distinct(StringComparer.Ordinal))
                {
                    if (exported.Any(n => n.Name == used)
                        || exported.FirstOrDefault(n => string.Equals(n.Name, used, StringComparison.OrdinalIgnoreCase)) is not { } match)
                    {
                        continue;
                    }

                    yield return new Finding(
                        Id,
                        Severity,
                        $"Flow '{workflow.Name}' action '{action.Name}' names '{used}' on '{action.EntitySet}', but the navigation property of table '{match.Table}' is '{match.Name}'; the Web API matches navigation property names by case.",
                        workflow.File,
                        action.Name);
                }
            }
        }
    }
}