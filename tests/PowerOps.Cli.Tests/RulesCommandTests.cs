using System.Text.Json;

using PowerOps.Core.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class RulesCommandTests
{
    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    [Fact]
    public void Json_lists_every_rule_in_order_and_matches_golden()
    {
        var (code, output, err) = Run("rules", "--json");

        Assert.Equal(0, code);
        Assert.Equal("", err);
        using var doc = JsonDocument.Parse(output);
        Assert.Equal(Analyzer.Rules.Select(r => r.Id), doc.RootElement.GetProperty("rules").EnumerateArray().Select(r => r.GetProperty("id").GetString()));
        Golden.Verify(output, "rules.json");
    }

    [Fact]
    public void Text_lists_every_rule_in_order_and_matches_golden()
    {
        var (code, output, _) = Run("rules");

        Assert.Equal(0, code);
        Assert.Equal(Analyzer.Rules.Select(r => r.Id), output.Split('\n').Where(l => l.StartsWith("PO", StringComparison.Ordinal)).Select(l => l[..5]));
        Golden.Verify(output, "rules.txt");
    }

    [Fact]
    public void Rule_ids_equal_the_docs_rules_file_names()
    {
        var (_, output, _) = Run("rules", "--json");
        using var doc = JsonDocument.Parse(output);
        var ids = doc.RootElement.GetProperty("rules").EnumerateArray().Select(r => r.GetProperty("id").GetString()!).Order(StringComparer.Ordinal).ToList();
        var docs = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "docs", "rules"), "*.md").Select(f => Path.GetFileNameWithoutExtension(f)).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(docs, ids);
    }
}