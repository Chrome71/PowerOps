using System.Text.Json;

using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Rules;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

public class BaselineTests : IDisposable
{
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-baseline-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static AnalysisResult One(params Finding[] findings) =>
        new([new SolutionResult("a/b", B.Empty(), findings)]);

    private string Save(AnalysisResult result)
    {
        var w = new StringWriter();
        Baseline.Write(result, w);
        var path = Path.Combine(_temp, "baseline.json");
        File.WriteAllText(path, w.ToString());
        return path;
    }

    private string WriteRaw(string content)
    {
        var path = Path.Combine(_temp, "raw.json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Write_sorts_collapses_duplicates_and_uses_unix_newlines()
    {
        var w = new StringWriter();
        Baseline.Write(
            One(
                new Finding("PO006", Severity.Warning, "b.", "f2", "c"),
                new Finding("PO005", Severity.Warning, "z.", "f1", null),
                new Finding("PO006", Severity.Warning, "b.", "other-file", "c"),
                new Finding("PO005", Severity.Warning, "a.", "f1", null)),
            w);

        var text = w.ToString();

        Assert.EndsWith("}\n", text);
        Assert.DoesNotContain('\r', text);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());
        var rows = doc.RootElement.GetProperty("findings").EnumerateArray()
            .Select(f => (f.GetProperty("ruleId").GetString(), f.GetProperty("message").GetString())).ToList();
        Assert.Equal([("PO005", "a."), ("PO005", "z."), ("PO006", "b.")], rows);
        Assert.Equal("Demo", doc.RootElement.GetProperty("findings")[0].GetProperty("solution").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("findings")[0].GetProperty("component").ValueKind);
    }

    [Fact]
    public void Apply_drops_baselined_findings_counts_them_and_ignores_the_file()
    {
        var known = new Finding("PO005", Severity.Warning, "known.", "zip-layout.xml", "c");
        var baseline = Baseline.Load(Save(One(known)));
        var result = One(known with { File = "Other/Customizations.xml" }, new Finding("PO001", Severity.Error, "new.", "f", "c"));

        var applied = baseline.Apply(result);

        Assert.Equal(1, applied.Baselined);
        Assert.Equal(1, applied.Errors);
        Assert.Equal(0, applied.Warnings);
        Assert.Equal("new.", Assert.Single(applied.Solutions[0].Findings).Message);
    }

    [Fact]
    public void Apply_matches_on_solution_name()
    {
        var finding = new Finding("PO005", Severity.Warning, "m.", "f", "c");
        var baseline = Baseline.Load(Save(One(finding)));
        var other = new AnalysisResult([new SolutionResult("x", B.Empty() with { UniqueName = "Other" }, [finding])]);

        Assert.Equal(0, baseline.Apply(other).Baselined);
    }

    [Fact]
    public void Baseline_from_a_zip_suppresses_the_unpacked_findings_of_the_same_solution()
    {
        var dir = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v2");
        var zip = Path.Combine(_temp, "v2.zip");
        SolutionZip.Create(dir, zip);
        var baseline = Baseline.Load(Save(AnalysisResult.Run([zip], _temp)));

        var applied = baseline.Apply(AnalysisResult.Run([dir], _temp));

        Assert.True(applied.Baselined > 0);
        Assert.All(applied.Solutions, s => Assert.Empty(s.Findings));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"version": 2, "findings": []}""")]
    [InlineData("""{"version": "1", "findings": []}""")]
    [InlineData("""{"version": 1}""")]
    [InlineData("""{"version": 1, "findings": {}}""")]
    [InlineData("""{"version": 1, "findings": [1]}""")]
    [InlineData("""{"version": 1, "findings": [{"solution": "S", "ruleId": "PO001"}]}""")]
    [InlineData("""{"version": 1, "findings": [{"solution": "S", "ruleId": 1, "message": "m"}]}""")]
    [InlineData("""{"version": 1, "findings": [{"solution": "S", "ruleId": "PO001", "message": "m", "component": 5}]}""")]
    public void Load_rejects_anything_that_is_not_a_version_1_baseline(string content)
    {
        Assert.Throws<InvalidDataException>(() => Baseline.Load(WriteRaw(content)));
    }

    [Fact]
    public void Load_accepts_a_finding_without_component()
    {
        var baseline = Baseline.Load(WriteRaw("""{"version": 1, "findings": [{"solution": "Demo", "ruleId": "PO005", "message": "m."}]}"""));

        Assert.Equal(1, baseline.Apply(One(new Finding("PO005", Severity.Warning, "m.", "f", null))).Baselined);
    }
}