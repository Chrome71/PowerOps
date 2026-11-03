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
    public void Folder_below_a_yaml_root_exits_two_and_says_where_to_point()
    {
        var (code, _, err) = Run("analyze", Path.Combine(Fixtures, "powerops-seed-yaml", "v2", "solutions"));

        Assert.Equal(2, code);
        Assert.Contains("To analyze a YAML layout, point at the folder that holds solutions/.", err);
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
    public void Malformed_flow_json_exits_three_and_names_the_flow_file()
    {
        Directory.CreateDirectory(Path.Combine(_temp, "Other"));
        File.WriteAllText(Path.Combine(_temp, "Other", "Solution.xml"), """
            <ImportExportXml><SolutionManifest>
              <UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>
              <Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher>
            </SolutionManifest></ImportExportXml>
            """);
        Directory.CreateDirectory(Path.Combine(_temp, "Workflows"));
        File.WriteAllText(Path.Combine(_temp, "Workflows", "Broken.json.data.xml"), """
            <Workflow WorkflowId="{AA-11}" Name="Broken">
              <JsonFileName>/Workflows/Broken.json</JsonFileName><Category>5</Category>
            </Workflow>
            """);
        File.WriteAllText(Path.Combine(_temp, "Workflows", "Broken.json"), """{"properties": {""");

        var (code, _, err) = Run("analyze", _temp);

        Assert.Equal(3, code);
        Assert.Contains("Workflows/Broken.json", err);
    }

    [Fact]
    public void Same_solution_with_and_without_trailing_slash_counts_once()
    {
        var solution = Path.Combine(Fixtures, "coe-auditlogs");

        var (code, output, _) = Run("analyze", solution, solution + Path.DirectorySeparatorChar, "--format", "json", "--fail-on", "none");

        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(output);
        Assert.Equal(1, doc.RootElement.GetProperty("summary").GetProperty("solutions").GetInt32());
    }

    [Fact]
    public void Json_output_parses_and_has_twelve_solutions()
    {
        var (code, output, _) = Run("analyze", Fixtures, "--format", "json", "--fail-on", "none");

        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(output);
        Assert.Equal(12, doc.RootElement.GetProperty("summary").GetProperty("solutions").GetInt32());
    }

    [Fact]
    public void Markdown_output_has_heading_and_summary()
    {
        var (code, output, _) = Run("analyze", Fixtures, "--format", "markdown", "--fail-on", "none");

        Assert.Equal(0, code);
        Assert.StartsWith("## PowerOps analyze\n\n**12 solutions, ", output);
    }

    [Fact]
    public void Sarif_output_parses_and_carries_the_tool_version()
    {
        var (code, output, _) = Run("analyze", Fixtures, "--format", "sarif", "--fail-on", "none");

        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(output);
        var driver = doc.RootElement.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver");
        Assert.Equal("PowerOps", driver.GetProperty("name").GetString());
        Assert.False(string.IsNullOrEmpty(driver.GetProperty("version").GetString()));
    }

    [Fact]
    public void Out_writes_file_and_leaves_stdout_empty()
    {
        var file = Path.Combine(_temp, "report.json");

        var (code, output, _) = Run("analyze", Fixtures, "--format", "json", "--out", file, "--fail-on", "none");

        Assert.Equal(0, code);
        Assert.Equal("", output);
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        Assert.Equal(12, doc.RootElement.GetProperty("solutions").GetArrayLength());
    }

    [Fact]
    public void Out_to_unwritable_path_exits_three()
    {
        var (code, _, err) = Run("analyze", Fixtures, "--out", Path.Combine(_temp, "no-such-dir", "r.txt"), "--fail-on", "none");

        // The notes (alm-sample's canvas note among them) come first and the error last.
        Assert.Equal(3, code);
        Assert.StartsWith("error: ", err.TrimEnd().Split('\n')[^1]);
    }

    private static readonly string CorePortal = Path.Combine(Fixtures, "core-portal");
    private const string AnonymousRoleId = "c29ed002-f074-474f-9361-16b9a46850d6";
    private const string PermissionId = "00000000-0000-0000-0000-0000000000c1";
    private static readonly string PermissionFile = $"powerpagecomponents/{PermissionId}/powerpagecomponent.xml";

    private static void CopyDirectory(string from, string to)
    {
        foreach (var dir in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, dir)));
        }

        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)));
        }
    }

    private static JsonElement[] Findings(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return [.. doc.RootElement.GetProperty("solutions").EnumerateArray().SelectMany(s => s.GetProperty("findings").EnumerateArray().Select(f => f.Clone()))];
    }

    [Fact]
    public void Core_portal_folder_and_zip_exit_zero_with_no_finding()
    {
        var zip = Path.Combine(_temp, "core-portal.zip");
        SolutionZip.Create(CorePortal, zip);

        foreach (var path in new[] { CorePortal, zip })
        {
            var (code, output, err) = Run("analyze", path, "--fail-on", "warning");

            Assert.Equal(0, code);
            Assert.Contains("no findings", output);
            Assert.DoesNotContain("note:", err);
        }
    }

    [Fact]
    public void An_anonymous_create_permission_gives_the_same_finding_from_a_folder_and_a_zip()
    {
        var copy = Path.Combine(_temp, "copy");
        CopyDirectory(CorePortal, copy);
        var dir = Path.Combine(copy, "powerpagecomponents", PermissionId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "powerpagecomponent.xml"), $$"""
            <powerpagecomponent powerpagecomponentid="{{PermissionId}}">
              <content>{"entitylogicalname":"demo_item","adx_entitypermission_webrole":["{{AnonymousRoleId}}"],"create":true,"write":false,"delete":false,"read":true,"parententitypermission":null}</content>
              <name>Item access</name>
              <powerpagecomponenttype>18</powerpagecomponenttype>
              <statecode>0</statecode>
            </powerpagecomponent>
            """);
        var zip = Path.Combine(_temp, "copy.zip");
        SolutionZip.Create(copy, zip);

        var fromFolder = Findings(Run("analyze", copy, "--format", "json", "--fail-on", "none").Out);
        var fromZip = Findings(Run("analyze", zip, "--format", "json", "--fail-on", "none").Out);

        var finding = Assert.Single(fromFolder);
        Assert.Equal("PO023", finding.GetProperty("ruleId").GetString());
        Assert.Equal(PermissionFile, finding.GetProperty("file").GetString());
        Assert.Equal("Table permission 'Item access' on 'demo_item' lets the anonymous role 'Anonymous Users' create, so anyone can without signing in; confirm it before Production.", finding.GetProperty("message").GetString());
        Assert.Equal(finding.GetRawText(), Assert.Single(fromZip).GetRawText());
    }

    [Fact]
    public void UseFieldsFromView_prints_a_note_on_stderr_and_exits_zero()
    {
        Directory.CreateDirectory(Path.Combine(_temp, "Other"));
        File.WriteAllText(Path.Combine(_temp, "Other", "Solution.xml"), """
            <ImportExportXml><SolutionManifest>
              <UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>
              <Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher>
            </SolutionManifest></ImportExportXml>
            """);
        foreach (var (id, name, value) in new[] { ("00000000-0000-0000-0000-0000000000d1", "Webapi/demo_item/enabled", "true"), ("00000000-0000-0000-0000-0000000000d2", "Webapi/demo_item/UseFieldsFromView", "True") })
        {
            Directory.CreateDirectory(Path.Combine(_temp, "powerpagecomponents", id));
            File.WriteAllText(Path.Combine(_temp, "powerpagecomponents", id, "powerpagecomponent.xml"), $$"""
                <powerpagecomponent powerpagecomponentid="{{id}}">
                  <content>{"value":"{{value}}"}</content>
                  <name>{{name}}</name>
                  <powerpagecomponenttype>9</powerpagecomponenttype>
                </powerpagecomponent>
                """);
        }

        var (code, output, err) = Run("analyze", _temp, "--fail-on", "warning");

        Assert.Equal(0, code);
        Assert.Contains("no findings", output);
        var note = Assert.Single(err.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Assert.StartsWith("note: ", note);
        Assert.EndsWith(": PO020 not checked for table 'demo_item': UseFieldsFromView is True, and the view that holds the columns is not read.", note);
    }

    private const string SeedMsapp = "CanvasApps/pops_workitems_2af60_DocumentUri.msapp";
    private static readonly string SeedYaml = Path.Combine(Fixtures, "powerops-seed-yaml", "v2");

    /// <summary>A copy of the seed solution (XML layout) whose `.msapp`, which the fixture had to drop, is rebuilt in code from the three `Src` files of the YAML fixture, with the backslash names `pac` writes.</summary>
    private string SeedWithMsapp(string name, string? screen2 = null, bool withSource = true)
    {
        var copy = Path.Combine(_temp, name);
        CopyDirectory(Path.Combine(Fixtures, "powerops-seed", "v2"), copy);
        var src = Path.Combine(SeedYaml, "canvasapps", "pops_workitems_2af60", "pops_workitems_2af60_DocumentUri", "Src");
        var entries = Directory.EnumerateFiles(src, "*.pa.yaml").Order(StringComparer.Ordinal).Select(f => ("Src\\" + Path.GetFileName(f), File.ReadAllText(f))).ToList();
        if (screen2 is not null)
        {
            entries.Add(("Src\\Screen2.pa.yaml", $"Screens:\n  Screen2:\n    Properties:\n      Fill: =Color.Red\n    Children:\n      - Button2:\n          Control: Button@2.2.0\n          Properties:\n            OnSelect: {screen2}\n"));
        }

        SolutionZip.Msapp(Path.Combine(copy, "CanvasApps", "pops_workitems_2af60_DocumentUri.msapp"), withSource ? [.. entries] : [("Header.json", "{}"), ("Controls\\1.json", "{}")]);
        return copy;
    }

    private static JsonElement[] RuleFindings(string path, string ruleId) =>
        [.. Findings(Run("analyze", path, "--format", "json", "--fail-on", "none").Out).Where(f => f.GetProperty("ruleId").GetString() == ruleId)];

    [Fact]
    public void An_msapp_gives_the_PO014_finding_of_the_yaml_layout_from_a_folder_and_a_zip()
    {
        var folder = SeedWithMsapp("with-source");
        var zip = Path.Combine(_temp, "with-source.zip");
        SolutionZip.Create(folder, zip);
        var expected = Assert.Single(RuleFindings(SeedYaml, "PO014"));

        foreach (var path in new[] { folder, zip })
        {
            var found = Assert.Single(RuleFindings(path, "PO014"));

            Assert.Equal(expected.GetProperty("message").GetString(), found.GetProperty("message").GetString());
            Assert.Equal(expected.GetProperty("component").GetString(), found.GetProperty("component").GetString());
            Assert.Equal(SeedMsapp, found.GetProperty("file").GetString());
            Assert.DoesNotContain("note:", Run("analyze", path, "--fail-on", "none").Err);
        }
    }

    [Fact]
    public void A_forall_around_patch_in_an_msapp_gives_one_PO018_finding_from_a_folder_and_a_zip()
    {
        var folder = SeedWithMsapp("forall", screen2: "=ForAll(colItems, Patch(Source, {a:1}))");
        var zip = Path.Combine(_temp, "forall.zip");
        SolutionZip.Create(folder, zip);

        foreach (var path in new[] { folder, zip })
        {
            var found = Assert.Single(RuleFindings(path, "PO018"));

            Assert.Equal("Canvas app 'Work Items' calls Patch inside ForAll in Button2.OnSelect, which is less efficient than passing the ForAll result to one Patch.", found.GetProperty("message").GetString());
            Assert.Equal("Button2.OnSelect", found.GetProperty("component").GetString());
            Assert.Equal(SeedMsapp, found.GetProperty("file").GetString());
        }
    }

    [Fact]
    public void An_msapp_without_Src_prints_the_note_on_stderr_gives_no_finding_and_exits_zero()
    {
        var folder = SeedWithMsapp("no-src", withSource: false);

        var (code, output, err) = Run("analyze", folder);

        Assert.Equal(0, code);
        Assert.DoesNotContain("PO014", output);
        var note = Assert.Single(err.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Assert.StartsWith("note: ", note);
        Assert.EndsWith($": PO014 and PO018 not checked for canvas app '{SeedMsapp}': no Src/*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).", note);
    }

    [Fact]
    public void An_msapp_the_solution_names_but_does_not_hold_is_not_read_and_gives_no_note()
    {
        var (code, _, err) = Run("analyze", Path.Combine(Fixtures, "powerops-seed", "v2"));

        Assert.Equal(0, code);
        Assert.Equal("", err);
    }

    [Fact]
    public void A_hostile_msapp_exits_three_naming_it()
    {
        var folder = SeedWithMsapp("hostile");
        File.WriteAllText(Path.Combine(folder, "CanvasApps", "pops_workitems_2af60_DocumentUri.msapp"), "PK but not a zip");

        var (code, _, err) = Run("analyze", folder);

        Assert.Equal(3, code);
        Assert.StartsWith("error: ", err);
        Assert.Contains(SeedMsapp, err);
    }
}