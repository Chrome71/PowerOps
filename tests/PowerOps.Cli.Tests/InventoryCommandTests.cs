using System.Text.Json.Nodes;

using PowerOps.Core.Output;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class InventoryCommandTests : IDisposable
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private static readonly string Alm = Path.Combine(Fixtures, "alm-makers");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-inventory-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString().Replace("\r\n", "\n"), e.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void A_solution_gives_a_report_and_exit_0()
    {
        var (code, output, err) = Run("inventory", Alm);

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Assert.Contains("  licenceDrivers (1):\n    http-action: ", output, StringComparison.Ordinal);
        Assert.EndsWith(InventoryReport.Footer + "\n", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("markdown")]
    public void Every_fixture_exits_0_never_1_and_the_yaml_layout_is_noted_on_stderr(string format)
    {
        var (code, output, err) = Run("inventory", Fixtures, "--format", format);

        Assert.Equal(0, code);
        Assert.NotEqual("", output);
        Assert.Contains("powerops-seed-yaml/v1: " + InventoryReport.YamlNotRead + "\n", err, StringComparison.Ordinal);
        if (format == "json")
        {
            Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load("inventory.schema.json"), JsonNode.Parse(output)!));
        }
    }

    [Fact]
    public void A_missing_path_exits_2()
    {
        var missing = Path.Combine(_temp, "nope");

        var (code, output, err) = Run("inventory", missing);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: path not found: {missing}\n", err);
    }

    [Fact]
    public void A_folder_with_no_solution_exits_2()
    {
        var (code, _, err) = Run("inventory", _temp);

        Assert.Equal(2, code);
        Assert.StartsWith($"error: no solutions found under \"{_temp}\".", err, StringComparison.Ordinal);
    }

    [Fact]
    public void Out_onto_a_named_input_is_refused_before_any_load()
    {
        // Not a valid zip: had it been loaded, the exit would be 3.
        var zip = Path.Combine(_temp, "bad.zip");
        File.WriteAllText(zip, "not a zip");

        var (code, output, err) = Run("inventory", zip, "--out", zip);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: {zip}: {SafeWrite.InputReason}\n", err);
        Assert.Equal("not a zip", File.ReadAllText(zip));
        Assert.Equal(3, Run("inventory", zip).Code);
    }

    [Fact]
    public void Out_to_a_new_file_writes_the_report_and_prints_nothing()
    {
        var file = Path.Combine(_temp, "inventory.md");

        var (code, output, _) = Run("inventory", Alm, "--format", "markdown", "--out", file);

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Equal(Run("inventory", Alm, "--format", "markdown").Out, File.ReadAllText(file));
    }

    [Fact]
    public void An_unreadable_flow_exits_3_naming_the_solution_then_the_flow_file()
    {
        Directory.CreateDirectory(Path.Combine(_temp, "Other"));
        Directory.CreateDirectory(Path.Combine(_temp, "Workflows"));
        File.WriteAllText(Path.Combine(_temp, "Other", "Solution.xml"), """
            <ImportExportXml><SolutionManifest><UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>
            <Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher></SolutionManifest></ImportExportXml>
            """);
        File.WriteAllText(Path.Combine(_temp, "Workflows", "Flow-AA.json.data.xml"), """
            <Workflow WorkflowId="{AA-11}" Name="My flow"><JsonFileName>/Workflows/Flow-AA.json</JsonFileName><Category>5</Category><StateCode>0</StateCode></Workflow>
            """);
        File.WriteAllText(Path.Combine(_temp, "Workflows", "Flow-AA.json"), "[1]");

        var (code, output, err) = Run("inventory", _temp);

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.StartsWith($"error: {_temp}: Workflows/Flow-AA.json: ", err, StringComparison.Ordinal);
        Assert.Equal(Run("analyze", _temp).Err, err);
    }

    [Fact]
    public void Stderr_is_redacted_unless_values_are_shown()
    {
        const string url = "https://contoso.example/x";

        Assert.Equal($"error: path not found: {Redaction.Placeholder}\n", Run("inventory", url).Err);
        Assert.Equal($"error: path not found: {url}\n", Run("inventory", url, "--show-values").Err);
    }
}