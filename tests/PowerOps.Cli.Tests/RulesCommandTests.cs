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
    public void Format_json_equals_json_and_the_golden()
    {
        var (code, output, err) = Run("rules", "--format", "json");

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Assert.Equal(Run("rules", "--json").Out, output);
        Golden.Verify(output, "rules.json");
    }

    [Fact]
    public void Format_text_equals_the_default_text()
    {
        var (code, output, _) = Run("rules", "--format", "text");

        Assert.Equal(0, code);
        Assert.Equal(Run("rules").Out, output);
    }

    [Fact]
    public void Json_wins_over_format_text()
    {
        var (code, output, _) = Run("rules", "--json", "--format", "text");

        Assert.Equal(0, code);
        Assert.Equal(Run("rules", "--json").Out, output);
    }

    [Fact]
    public void An_unknown_format_is_a_usage_error()
    {
        var (code, output, err) = Run("rules", "--format", "xml");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("Argument 'xml' not recognized. Must be one of:  'text'  'json'", err);
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
        var docs = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "docs", "rules"), "*.md").Where(f => Path.GetFileName(f) != "README.md").Select(f => Path.GetFileNameWithoutExtension(f)).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(docs, ids);
    }
}