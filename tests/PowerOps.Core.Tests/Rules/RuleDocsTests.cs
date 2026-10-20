using PowerOps.Core.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Rules;

public class RuleDocsTests
{
    [Fact]
    public void Every_rule_has_a_docs_page()
    {
        foreach (var rule in Analyzer.Rules)
        {
            Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, "docs", "rules", rule.Id + ".md")), $"docs/rules/{rule.Id}.md is missing");
        }
    }

    [Fact]
    public void Every_rule_has_a_one_line_plain_fix_without_urls()
    {
        foreach (var rule in Analyzer.Rules)
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Fix), $"{rule.Id} has no fix");
            Assert.DoesNotContain('\n', rule.Fix);
            Assert.DoesNotContain("http", rule.Fix, StringComparison.OrdinalIgnoreCase);
        }
    }
}