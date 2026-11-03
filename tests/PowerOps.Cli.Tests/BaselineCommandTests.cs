using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class BaselineCommandTests : IDisposable
{
    private static readonly string V2 = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v2");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-cli-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    private string BaselineFile => Path.Combine(_temp, "baseline.json");

    [Fact]
    public void Baseline_then_analyze_with_it_reports_nothing_and_exits_zero()
    {
        Assert.Equal(0, Run("baseline", V2, "--out", BaselineFile).Code);

        var (code, output, _) = Run("analyze", V2, "--baseline", BaselineFile, "--fail-on", "warning");

        Assert.Equal(0, code);
        Assert.Contains("no findings", output);
        Assert.Contains("0 errors, 0 warnings, 2 baselined", output);
    }

    [Fact]
    public void Baseline_file_is_sorted_utf8_without_bom_and_unix_newlines()
    {
        Run("baseline", V2, "--out", BaselineFile);

        var bytes = File.ReadAllBytes(BaselineFile);
        Assert.NotEqual(0xEF, bytes[0]);
        var text = File.ReadAllText(BaselineFile);
        Assert.EndsWith("}\n", text);
        Assert.DoesNotContain('\r', text);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(["PO005", "PO006"], doc.RootElement.GetProperty("findings").EnumerateArray().Select(f => f.GetProperty("ruleId").GetString()));
    }

    [Fact]
    public void Json_summary_always_has_baselined_and_sarif_omits_the_findings()
    {
        Run("baseline", V2, "--out", BaselineFile);

        using var plain = JsonDocument.Parse(Run("analyze", V2, "--format", "json").Out);
        using var json = JsonDocument.Parse(Run("analyze", V2, "--format", "json", "--baseline", BaselineFile).Out);
        using var sarif = JsonDocument.Parse(Run("analyze", V2, "--format", "sarif", "--baseline", BaselineFile).Out);

        Assert.Equal(0, plain.RootElement.GetProperty("summary").GetProperty("baselined").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("summary").GetProperty("baselined").GetInt32());
        Assert.Equal(0, sarif.RootElement.GetProperty("runs")[0].GetProperty("results").GetArrayLength());
    }

    [Fact]
    public void A_finding_missing_from_the_baseline_still_counts()
    {
        File.WriteAllText(BaselineFile, """{"version": 1, "findings": []}""");

        var (code, output, _) = Run("analyze", V2, "--baseline", BaselineFile, "--fail-on", "warning");

        Assert.Equal(1, code);
        Assert.Contains("warning PO005", output);
        Assert.DoesNotContain("baselined", output);
    }

    [Fact]
    public void Baseline_from_a_zip_suppresses_the_unpacked_findings()
    {
        var zip = Path.Combine(_temp, "v2.zip");
        SolutionZip.Create(V2, zip);
        Assert.Equal(0, Run("baseline", zip, "--out", BaselineFile).Code);

        var (code, output, _) = Run("analyze", V2, "--baseline", BaselineFile, "--fail-on", "warning");

        Assert.Equal(0, code);
        Assert.Contains("2 baselined", output);
    }

    [Fact]
    public void Missing_baseline_file_exits_two()
    {
        var (code, _, err) = Run("analyze", V2, "--baseline", Path.Combine(_temp, "nope.json"));

        Assert.Equal(2, code);
        Assert.StartsWith("error: ", err);
    }

    [Fact]
    public void Invalid_baseline_file_exits_two()
    {
        File.WriteAllText(BaselineFile, "not json");

        var (code, _, err) = Run("analyze", V2, "--baseline", BaselineFile);

        Assert.Equal(2, code);
        Assert.StartsWith("error: ", err);
    }

    [Fact]
    public void Baseline_command_reports_usage_errors_like_analyze()
    {
        Assert.Equal(2, Run("baseline", Path.Combine(_temp, "nope"), "--out", BaselineFile).Code);
        Assert.Equal(2, Run("baseline", _temp, "--out", BaselineFile).Code);
        Assert.False(File.Exists(BaselineFile));
    }

    [Fact]
    public void Baseline_prints_the_not_checked_note_on_stderr()
    {
        var folder = Path.Combine(_temp, "viewfields");
        Directory.CreateDirectory(Path.Combine(folder, "Other"));
        File.WriteAllText(Path.Combine(folder, "Other", "Solution.xml"), """
            <ImportExportXml><SolutionManifest>
              <UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>
              <Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher>
            </SolutionManifest></ImportExportXml>
            """);
        foreach (var (id, name, value) in new[] { ("00000000-0000-0000-0000-0000000000d1", "Webapi/demo_item/enabled", "true"), ("00000000-0000-0000-0000-0000000000d2", "Webapi/demo_item/UseFieldsFromView", "True") })
        {
            Directory.CreateDirectory(Path.Combine(folder, "powerpagecomponents", id));
            File.WriteAllText(Path.Combine(folder, "powerpagecomponents", id, "powerpagecomponent.xml"), $$"""
                <powerpagecomponent powerpagecomponentid="{{id}}">
                  <content>{"value":"{{value}}"}</content>
                  <name>{{name}}</name>
                  <powerpagecomponenttype>9</powerpagecomponenttype>
                </powerpagecomponent>
                """);
        }

        var (code, _, err) = Run("baseline", folder, "--out", BaselineFile);

        Assert.Equal(0, code);
        var line = Assert.Single(err.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Assert.StartsWith("note: ", line);
        Assert.EndsWith(": PO020 not checked for table 'demo_item': UseFieldsFromView is True, and the view that holds the columns is not read.", line);
    }

    [Fact]
    public void Baseline_prints_the_canvas_note_for_an_msapp_with_no_Src()
    {
        var folder = Path.Combine(_temp, "no-src");
        CopyDirectory(V2, folder);
        SolutionZip.Msapp(Path.Combine(folder, "CanvasApps", "pops_workitems_2af60_DocumentUri.msapp"), ("Header.json", "{}"));

        var (code, _, err) = Run("baseline", folder, "--out", BaselineFile);

        Assert.Equal(0, code);
        var line = Assert.Single(err.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Assert.EndsWith(": PO014 and PO018 not checked for canvas app 'CanvasApps/pops_workitems_2af60_DocumentUri.msapp': no Src/*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).", line);
    }

    private static void CopyDirectory(string from, string to)
    {
        foreach (var dir in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, dir)));
        }

        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(to, Path.GetRelativePath(from, file)))!);
            File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)));
        }
    }
}