using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;

using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Rules;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

public class SarifReportTests
{
    // The official schema is draft-04, which JsonSchema.Net does not support. It uses no draft-04-only
    // keywords, so evaluate it as draft-07 (the vendored file stays untouched).
    private static readonly JsonSchema Schema = LoadSchema();

    private static JsonSchema LoadSchema()
    {
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "schemas", "sarif-schema-2.1.0.json")))!.AsObject();
        node["$schema"] = "http://json-schema.org/draft-07/schema#";
        return JsonSchema.FromText(node.ToJsonString());
    }

    private static string Sarif(string path, params Finding[] findings)
    {
        var w = new StringWriter();
        SarifReport.Write(new AnalysisResult([new SolutionResult(path, B.Empty(), findings)]), w, "1.2.3");
        return w.ToString();
    }

    private static bool IsValid(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Schema.Evaluate(doc.RootElement).IsValid;
    }

    [Fact]
    public void Fixture_report_is_valid_sarif_and_an_incomplete_log_is_not()
    {
        var w = new StringWriter();
        SarifReport.Write(AnalysisResult.Run([Path.Combine(Golden.RepoRoot, "tests", "fixtures")], Golden.RepoRoot), w, "1.2.3");
        var json = w.ToString();
        Assert.True(IsValid(json));

        var broken = JsonNode.Parse(json)!.AsObject();
        broken.Remove("version");
        Assert.False(IsValid(broken.ToJsonString()));
    }

    [Fact]
    public void Folder_solution_points_at_the_file_with_escaped_segments()
    {
        var json = Sarif("my sol/ö", new Finding("PO001", Severity.Error, "m \"q\"", "Other/a b.xml", "c"));

        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0];
        var loc = r.GetProperty("locations")[0];
        Assert.Equal("my%20sol/%C3%B6/Other/a%20b.xml", loc.GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal(1, loc.GetProperty("physicalLocation").GetProperty("region").GetProperty("startLine").GetInt32());
        Assert.False(loc.TryGetProperty("logicalLocations", out _));
        Assert.Equal("error", r.GetProperty("level").GetString());
        Assert.Equal(0, r.GetProperty("ruleIndex").GetInt32());
        Assert.Equal("m \"q\"", r.GetProperty("message").GetProperty("text").GetString());
    }

    [Fact]
    public void Zip_solution_points_at_the_zip_and_names_the_inner_file()
    {
        var json = Sarif("x/S.ZIP", new Finding("PO001", Severity.Error, "m", "customizations.xml", null));

        using var doc = JsonDocument.Parse(json);
        var loc = doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0].GetProperty("locations")[0];
        Assert.Equal("x/S.ZIP", loc.GetProperty("physicalLocation").GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal("customizations.xml", loc.GetProperty("logicalLocations")[0].GetProperty("fullyQualifiedName").GetString());
    }

    [Fact]
    public void Fingerprint_ignores_file_and_layout()
    {
        static string Fp(string path, string file)
        {
            using var doc = JsonDocument.Parse(Sarif(path, new Finding("PO001", Severity.Error, "m", file, "c")));
            return doc.RootElement.GetProperty("runs")[0].GetProperty("results")[0].GetProperty("partialFingerprints").GetProperty("poFinding/v1").GetString()!;
        }

        Assert.Equal(Fp("a.zip", "customizations.xml"), Fp("a", "Other/Customizations.xml"));
        Assert.Matches("^[0-9a-f]{64}$", Fp("a", "f"));
    }
}