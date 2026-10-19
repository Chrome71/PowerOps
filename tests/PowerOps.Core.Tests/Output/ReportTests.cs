using System.Text.Json;

using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Rules;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

public class ReportTests
{
    private static AnalysisResult Fixtures() =>
        AnalysisResult.Run([Path.Combine(Golden.RepoRoot, "tests", "fixtures")], Golden.RepoRoot);

    private static string Text(AnalysisResult r)
    {
        var w = new StringWriter();
        TextReport.Write(r, w);
        return w.ToString();
    }

    private static string Json(AnalysisResult r)
    {
        var w = new StringWriter();
        JsonReport.Write(r, w);
        return w.ToString();
    }

    private static AnalysisResult One(IReadOnlyList<Finding> findings, params MissingDependency[] deps) =>
        new([new SolutionResult("a/b", B.Empty() with { MissingDependencies = deps }, findings)]);

    private static MissingDependency Dep(string? solution) =>
        new(new DependencyNode("1", "x", null, null, solution), new DependencyNode("1", "y", null, null, null));

    [Fact]
    public void Text_report_matches_golden() => Golden.Verify(Text(Fixtures()), "analyze-fixtures.txt");

    [Fact]
    public void Json_report_matches_golden() => Golden.Verify(Json(Fixtures()), "analyze-fixtures.json");

    [Fact]
    public void Text_without_findings_says_so_and_uses_singular_counts()
    {
        Assert.Equal(
            "a/b: Demo 1.0.0.0 (publisher demopub, prefix demo)\n  no findings\n1 solution, 0 errors, 0 warnings\n",
            Text(One([])));
    }

    [Fact]
    public void Text_lists_requires_sorted_and_distinct_and_pluralises()
    {
        var findings = new[]
        {
            new Finding("PO001", Severity.Error, "m1.", "f1", "c"),
            new Finding("PO005", Severity.Warning, "m2.", "f2", null),
        };
        var text = Text(One(findings, Dep("Zed (1)"), Dep("Alpha (2)"), Dep("Zed (1)"), Dep(null), Dep("")));

        Assert.Equal(
            "a/b: Demo 1.0.0.0 (publisher demopub, prefix demo)\n  requires Alpha (2)\n  requires Zed (1)\n" +
            "  error PO001 f1: m1.\n  warning PO005 f2: m2.\n1 solution, 1 error, 1 warning\n",
            text);
    }

    [Fact]
    public void Json_uses_camel_case_lower_case_enums_and_no_carriage_returns()
    {
        var json = Json(One([new Finding("PO001", Severity.Error, "It's 'x'.", "f", null)], Dep("S (1)")));

        Assert.DoesNotContain('\r', json);
        Assert.Contains("'x'", json);
        using var doc = JsonDocument.Parse(json);
        var sol = doc.RootElement.GetProperty("solutions")[0];
        Assert.Equal("a/b", sol.GetProperty("path").GetString());
        Assert.Equal("S (1)", sol.GetProperty("requires")[0].GetString());
        var f = sol.GetProperty("findings")[0];
        Assert.Equal("error", f.GetProperty("severity").GetString());
        Assert.Equal(JsonValueKind.Null, f.GetProperty("component").ValueKind);
        Assert.Equal(1, doc.RootElement.GetProperty("summary").GetProperty("errors").GetInt32());
    }
}