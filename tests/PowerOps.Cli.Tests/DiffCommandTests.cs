using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class DiffCommandTests : IDisposable
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private static readonly string V1 = Path.Combine(Fixtures, "powerops-seed", "v1");
    private static readonly string V2 = Path.Combine(Fixtures, "powerops-seed", "v2");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-cli-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    [Fact]
    public void Same_solution_exits_zero_with_no_changes()
    {
        var (code, output, _) = Run("diff", V1, V1);

        Assert.Equal(0, code);
        Assert.Contains("no changes", output);
    }

    [Fact]
    public void Unpacked_folder_and_its_exported_zip_exit_zero()
    {
        var zip = Path.Combine(_temp, "seed.zip");
        SolutionZip.Create(V1, zip);

        Assert.Equal(0, Run("diff", V1, zip).Code);
    }

    [Fact]
    public void Differences_exit_one_and_match_the_text_golden()
    {
        var (code, output, err) = Run("diff", V1, V2);

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Golden.Verify(output, "diff-seed.txt");
    }

    [Fact]
    public void Json_matches_golden() => Golden.Verify(Run("diff", V1, V2, "--format", "json").Out, "diff-seed.json");

    [Fact]
    public void Markdown_matches_golden() => Golden.Verify(Run("diff", V1, V2, "--format", "markdown").Out, "diff-seed.md");

    [Fact]
    public void Out_writes_the_report_to_a_file_and_still_exits_one()
    {
        var file = Path.Combine(_temp, "diff.json");

        var (code, output, _) = Run("diff", V1, V2, "--format", "json", "--out", file);

        Assert.Equal(1, code);
        Assert.Equal("", output);
        Assert.Contains("\"schemaVersion\": 1", File.ReadAllText(file));
    }

    [Fact]
    public void Missing_path_bad_format_and_missing_argument_exit_two()
    {
        var (code, _, err) = Run("diff", V1, Path.Combine(_temp, "nope.zip"));

        Assert.Equal(2, code);
        Assert.Contains("path not found", err);
        Assert.Equal(2, Run("diff", V1, V2, "--format", "sarif").Code);
        Assert.Equal(2, Run("diff", V1).Code);
    }

    [Fact]
    public void Yaml_layout_root_folders_are_diffed_with_the_canvas_app_named()
    {
        var yaml = Path.Combine(Fixtures, "powerops-seed-yaml");

        var (code, output, err) = Run("diff", Path.Combine(yaml, "v1"), Path.Combine(yaml, "v2"));

        Assert.Equal(1, code);
        Assert.Equal("", err);
        // Unlike powerops-seed, the YAML v2 canvas app was edited (a button added) and republished.
        Assert.Equal(
            [
                "  added connectionReference pops_SpareDataverse [risk: connection-reference]",
                "  changed workflow Stamp work item status (4c8dd007-dfcb-f111-a05c-0022480697a8): definition changed",
                "  changed canvasApp pops_workitems_2af60: definition changed; republished (AppVersion 2026-10-19T17:09:47.0000000Z → 2026-10-20T04:41:27.0000000Z)",
            ],
            output.Split('\n').Where(l => l.StartsWith("  ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Folder_with_several_solutions_exits_two()
    {
        var (code, _, err) = Run("diff", Path.Combine(Fixtures, "powerops-seed-yaml"), V1);

        Assert.Equal(2, code);
        Assert.Contains("holds 2 solutions", err);
    }

    [Fact]
    public void Unreadable_input_exits_three_and_names_the_file()
    {
        var bad = Path.Combine(_temp, "bad.zip");
        File.WriteAllText(bad, "not a zip");

        var (code, _, err) = Run("diff", V1, bad);

        Assert.Equal(3, code);
        Assert.StartsWith($"error: {bad}", err);
    }
}