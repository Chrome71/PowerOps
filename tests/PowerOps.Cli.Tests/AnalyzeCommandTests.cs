using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class AnalyzeCommandTests : IDisposable
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-cli-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    [Fact]
    public void Fixtures_fail_on_warning_exits_one()
    {
        var (code, output, _) = Run("analyze", Fixtures, "--fail-on", "warning");

        Assert.Equal(1, code);
        Assert.Contains("warning PO", output);
    }

    [Fact]
    public void Fixtures_fail_on_none_exits_zero() => Assert.Equal(0, Run("analyze", Fixtures, "--fail-on", "none").Code);

    [Fact]
    public void Clean_zip_exits_zero()
    {
        var zip = Path.Combine(Fixtures, "zips", "ColumnLevelSecuritySampleSolution.zip");

        var (code, output, _) = Run("analyze", zip, "--fail-on", "warning");

        Assert.Equal(0, code);
        Assert.Contains("no findings", output);
    }

    [Fact]
    public void Coe_auditlogs_has_no_errors_so_default_exits_zero_but_warning_exits_one()
    {
        var coe = Path.Combine(Fixtures, "coe-auditlogs");

        Assert.Equal(0, Run("analyze", coe).Code);
        Assert.Equal(1, Run("analyze", coe, "--fail-on", "warning").Code);
    }

    [Fact]
    public void Error_finding_exits_one_by_default_and_zero_with_fail_on_none()
    {
        // PO002: a required environment variable with no default and no value.
        Directory.CreateDirectory(Path.Combine(_temp, "Other"));
        File.WriteAllText(Path.Combine(_temp, "Other", "Solution.xml"), """
            <ImportExportXml><SolutionManifest>
              <UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>
              <Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher>
            </SolutionManifest></ImportExportXml>
            """);
        var envDir = Path.Combine(_temp, "environmentvariabledefinitions", "demo_A");
        Directory.CreateDirectory(envDir);
        File.WriteAllText(
            Path.Combine(envDir, "environmentvariabledefinition.xml"),
            """<environmentvariabledefinition schemaname="demo_A"><isrequired>1</isrequired></environmentvariabledefinition>""");

        var (code, output, _) = Run("analyze", _temp);

        Assert.Equal(1, code);
        Assert.Contains("error PO002", output);
        Assert.Equal(0, Run("analyze", _temp, "--fail-on", "none").Code);
    }

    [Fact]
    public void Missing_path_exits_two()
    {
        var (code, _, err) = Run("analyze", Path.Combine(_temp, "nope"));

        Assert.Equal(2, code);
        Assert.Contains("path not found", err);
    }

    [Fact]
    public void Bad_format_exits_two()
    {
        var (code, _, err) = Run("analyze", Fixtures, "--format", "xml");

        Assert.Equal(2, code);
        Assert.NotEmpty(err);
    }

    [Fact]
    public void Empty_folder_exits_two()
    {
        var (code, _, err) = Run("analyze", _temp);

        Assert.Equal(2, code);
        Assert.Contains("no solutions found", err);
    }

    [Fact]
    public void Malformed_solution_xml_exits_three()
    {
        Directory.CreateDirectory(Path.Combine(_temp, "Other"));
        File.WriteAllText(Path.Combine(_temp, "Other", "Solution.xml"), "<not xml");

        var (code, _, err) = Run("analyze", _temp);

        Assert.Equal(3, code);
        Assert.StartsWith("error: ", err);
        Assert.DoesNotContain("   at ", err);
    }

    [Fact]
    public void Json_output_parses_and_has_nine_solutions()
    {
        var (code, output, _) = Run("analyze", Fixtures, "--format", "json", "--fail-on", "none");

        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(output);
        Assert.Equal(9, doc.RootElement.GetProperty("summary").GetProperty("solutions").GetInt32());
    }

    [Fact]
    public void Out_writes_file_and_leaves_stdout_empty()
    {
        var file = Path.Combine(_temp, "report.json");

        var (code, output, _) = Run("analyze", Fixtures, "--format", "json", "--out", file, "--fail-on", "none");

        Assert.Equal(0, code);
        Assert.Equal("", output);
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        Assert.Equal(9, doc.RootElement.GetProperty("solutions").GetArrayLength());
    }

    [Fact]
    public void Out_to_unwritable_path_exits_three()
    {
        var (code, _, err) = Run("analyze", Fixtures, "--out", Path.Combine(_temp, "no-such-dir", "r.txt"), "--fail-on", "none");

        Assert.Equal(3, code);
        Assert.StartsWith("error: ", err);
    }
}