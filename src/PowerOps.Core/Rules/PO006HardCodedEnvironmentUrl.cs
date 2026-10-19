using System.Text.RegularExpressions;

using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed partial class PO006HardCodedEnvironmentUrl : IRule
{
    public string Id => "PO006";

    public Severity Severity => Severity.Warning;

    public string Title => "Flow contains a hard-coded environment or SharePoint site URL";

    [GeneratedRegex(@"https?://[a-z0-9-]+\.crm\d*\.dynamics\.com|https?://[a-z0-9-]+\.sharepoint\.com/sites/", RegexOptions.IgnoreCase)]
    private static partial Regex EnvUrl();

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var (w, flow) in RuleHelpers.CloudFlows(solution))
        {
            var urls = flow.StringLiterals
                .SelectMany(l => EnvUrl().Matches(l.Value).Select(m => m.Value))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var url in urls)
            {
                yield return new Finding(Id, Severity, $"Flow '{w.Name}' hard-codes the URL '{url}'.", w.File, url);
            }
        }
    }
}