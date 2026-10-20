using System.Globalization;
using System.Text.RegularExpressions;

using PowerOps.Core.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Rules;

public partial class RuleDocsTests
{
    [GeneratedRegex(@"^Sources: .*\]\(https://[^)]+\).* verified: (\d{4}-\d{2}-\d{2})$", RegexOptions.Multiline)]
    private static partial Regex SourcesLine();

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

    [Fact]
    public void Every_rule_doc_has_a_sources_line_with_a_link_and_a_past_verified_date()
    {
        foreach (var rule in Analyzer.Rules)
        {
            var text = File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "rules", rule.Id + ".md")).Replace("\r\n", "\n");

            var match = SourcesLine().Match(text);

            Assert.True(match.Success, $"docs/rules/{rule.Id}.md has no 'Sources: [title](https://...). verified: yyyy-MM-dd' line");
            Assert.True(DateOnly.TryParseExact(match.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date), $"{rule.Id}: bad date");
            Assert.True(date <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), $"{rule.Id}: verified date {date} is in the future");
        }
    }
}