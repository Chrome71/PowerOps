using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// M30: R-12 (one stderr note per link skipped inside a found solution) and A-7 (diff cleans the folder names it prints).
public class OutputHygieneCommandTests : IDisposable
{
    private static readonly string Seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-hygiene-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    private static string[] Lines(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();

    private string Copy(string source, string name)
    {
        var target = Path.Combine(_temp, name);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }

        return target;
    }

    // The observed case: Other/Customizations.xml is a link to a file that holds a connection reference.
    private string LinkedCustomizations()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Creating links needs privileges on Windows.");
        var folder = Directory.CreateDirectory(Path.Combine(_temp, "linked", "Other")).Parent!.FullName;
        File.WriteAllText(Path.Combine(folder, "Other", "Solution.xml"), """
            <ImportExportXml><SolutionManifest>
              <UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>
              <Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher>
            </SolutionManifest></ImportExportXml>
            """);
        var real = Path.Combine(_temp, "outside.xml");
        File.WriteAllText(real, """<ImportExportXml><connectionreferences><connectionreference connectionreferencelogicalname="demo_x"><connectorid>/providers/Microsoft.PowerApps/apis/shared_x</connectorid></connectionreference></connectionreferences></ImportExportXml>""");
        File.CreateSymbolicLink(Path.Combine(folder, "Other", "Customizations.xml"), real);
        return folder;
    }

    [Fact]
    public void Analyze_exits_0_with_one_note_naming_the_linked_file()
    {
        var folder = LinkedCustomizations();

        var (code, _, err) = Run("analyze", folder);

        Assert.Equal(0, code);
        var note = Assert.Single(Lines(err));
        Assert.StartsWith("note: ", note);
        Assert.Contains(folder.Replace('\\', '/') + "/Other/Customizations.xml", note);
    }

    [Fact]
    public void Analyze_prints_no_note_for_a_folder_without_links()
    {
        var (code, _, err) = Run("analyze", Copy(Path.Combine(Seed, "v1"), "plain"));

        Assert.Equal(0, code);
        Assert.Equal("", err);
    }

    [Fact]
    public void A_link_passed_by_the_discovery_walk_gets_no_note()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Creating links needs privileges on Windows.");
        var root = Directory.CreateDirectory(Path.Combine(_temp, "root")).FullName;
        var solution = Copy(Path.Combine(Seed, "v1"), "elsewhere");
        Directory.CreateSymbolicLink(Path.Combine(root, "link"), solution);

        var (code, _, err) = Run("analyze", root);

        Assert.Equal(2, code);
        Assert.Equal("error: no solutions found under \"" + root + "\". To analyze a YAML layout, point at the folder that holds solutions/.", Lines(err).Single());
    }

    [Fact]
    public void Diff_and_test_print_the_note_too()
    {
        var linked = LinkedCustomizations();
        var other = Copy(Path.Combine(Seed, "v2"), "v2");

        var diff = Run("diff", other, linked);
        var test = Run("test", linked, "--tests", Path.Combine(Golden.RepoRoot, "tests", "fixtures", "flowtests", "seed"));

        Assert.Contains(Lines(diff.Err), l => l.StartsWith("note: ", StringComparison.Ordinal) && l.Contains("Other/Customizations.xml"));
        Assert.Single(Lines(diff.Err));
        Assert.Contains(Lines(test.Err), l => l.StartsWith("note: ", StringComparison.Ordinal) && l.Contains("Other/Customizations.xml"));
    }

    [Fact]
    public void Diff_cleans_the_folder_names_it_prints_so_a_name_cannot_add_a_log_line()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "A folder name cannot hold a line break on Windows.");
        var root = Path.Combine(_temp, "two");
        Copy(Path.Combine(Seed, "v1"), Path.Combine("two", "a\n::error::forged"));
        Copy(Path.Combine(Seed, "v2"), Path.Combine("two", "b"));

        var (code, _, err) = Run("diff", root, Path.Combine(Seed, "v2"));

        Assert.Equal(2, code);
        var line = Assert.Single(Lines(err));
        Assert.StartsWith("error: ", line);
        Assert.Contains("a ::error::forged", line);
    }

    // M28: the refusal names the target, and a link name is as hostile as a solution's.
    [Fact]
    public void A_refused_out_with_a_line_break_in_its_name_prints_one_clean_error_line()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "A line break is not valid in a Windows file name.");
        var victim = Path.Combine(_temp, "victim.txt");
        File.WriteAllText(victim, "victim");
        var link = Path.Combine(_temp, "x\n::error::bad.json");
        Links.Create(link, victim);

        var (code, _, err) = Run("analyze", Path.Combine(Seed, "v1"), "--out", link);

        Assert.Equal(2, code);
        var line = Assert.Single(Lines(err));
        Assert.StartsWith("error: ", line);
        Assert.DoesNotContain(Lines(err), l => l.StartsWith("::"));
        Assert.Equal("victim", File.ReadAllText(victim));
    }

    // M33 S-1: a value from the input must not add a line or an escape sequence to a text report or an error line.
    private const string Hostile = "demo\n::warning::FORGED\u001b[31mRED";

    private static void AssertClean(string text)
    {
        Assert.DoesNotContain('\u001b', text);
        Assert.DoesNotContain(Lines(text), l => l.StartsWith("::", StringComparison.Ordinal));
    }

    [Fact]
    public void Settings_validate_prints_a_hostile_logical_name_on_one_clean_line()
    {
        var settings = Path.Combine(_temp, "hostile.settings.json");
        File.WriteAllText(settings, new System.Text.Json.Nodes.JsonObject
        {
            ["EnvironmentVariables"] = new System.Text.Json.Nodes.JsonArray(),
            ["ConnectionReferences"] = new System.Text.Json.Nodes.JsonArray(new System.Text.Json.Nodes.JsonObject
            {
                ["LogicalName"] = Hostile,
                ["ConnectionId"] = "fake-connection-id",
                ["ConnectorId"] = "/providers/Microsoft.PowerApps/apis/shared_example",
            }),
        }.ToJsonString());

        var (code, output, _) = Run("settings", "validate", Path.Combine(Seed, "v1"), "--settings", settings);

        Assert.Equal(1, code);
        Assert.Contains("FORGED", output);
        AssertClean(output);
    }

    [Fact]
    public void Analyze_prints_a_path_with_a_line_break_and_an_escape_on_one_clean_error_line()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "A line break is not valid in a Windows file name.");

        var (code, _, err) = Run("analyze", Path.Combine(_temp, Hostile));

        Assert.Equal(2, code);
        Assert.StartsWith("error: path not found: ", Assert.Single(Lines(err)));
        AssertClean(err);
    }

    // M33 F-3: the path is quoted, so `.` does not read as the end of the sentence.
    [Fact]
    public void Analyze_and_test_quote_the_folder_in_the_no_solutions_error()
    {
        var empty = Directory.CreateDirectory(Path.Combine(_temp, "empty")).FullName;
        var tests = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "flowtests", "seed");

        var analyze = Run("analyze", empty);
        var test = Run("test", empty, "--tests", tests);

        Assert.Equal(2, analyze.Code);
        Assert.Equal($"error: no solutions found under \"{empty}\". To analyze a YAML layout, point at the folder that holds solutions/.", Lines(analyze.Err).Single());
        Assert.Equal(2, test.Code);
        Assert.Equal($"error: no solutions found under \"{empty}\". To test a YAML layout, point at the folder that holds solutions/.", Lines(test.Err).Single());
    }
}