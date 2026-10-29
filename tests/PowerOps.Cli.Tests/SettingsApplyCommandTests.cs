using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class SettingsApplyCommandTests : IDisposable
{
    private const string EnvironmentId = "00000000-0000-0000-0000-00000000000c";
    private const string SeedFlowId = "4c8dd007-dfcb-f111-a05c-0022480697a8";

    private static readonly string Valid = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "settings", "powerops-seed-v1.valid.json");
    private static readonly string Problems = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "settings", "powerops-seed-v1.problems.json");

    private readonly string _dir = Directory.CreateTempSubdirectory("powerops-apply-").FullName;

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string Sample(string name) => File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", name));

    private static PacResult Ok(string stdout = "") => new(true, false, 0, stdout);

    // The seed v1 zip; with flowOn false, its flow's export StateCode is 0.
    private string SeedZip(bool flowOn = true)
    {
        var zip = Path.Combine(_dir, flowOn ? "seed.zip" : "seed-off.zip");
        if (File.Exists(zip))
        {
            return zip;
        }

        var folder = Path.Combine(_dir, flowOn ? "on" : "off");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1"), "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(folder, Path.GetRelativePath(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1"), file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            var text = File.ReadAllText(file);
            File.WriteAllText(copy, flowOn ? text : text.Replace("<StateCode>1</StateCode>", "<StateCode>0</StateCode>"));
        }

        SolutionZip.Create(folder, zip);
        return zip;
    }

    private sealed record Outcome(int Code, string Out, string Err, List<string> Calls, List<TimeSpan> Timeouts);

    private static Outcome Run(Func<string, PacResult> pac, params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var calls = new List<string>();
        var timeouts = new List<TimeSpan>();
        var code = Program.Run(args, o, e, (a, t) =>
        {
            calls.Add(string.Join(" ", a));
            timeouts.Add(t);
            return pac(calls[^1]);
        });
        return new(code, o.ToString(), e.ToString(), calls, timeouts);
    }

    // A healthy pac: list-cloud-flows prints `before` until the import, then `after`.
    private static Func<string, PacResult> Pac(string before = "list-cloud-flows-missing.json", string after = "list-cloud-flows-on.json", Func<PacResult>? import = null)
    {
        var imported = false;
        return call =>
        {
            if (call == "org who")
            {
                return Ok(Sample("org-who.txt"));
            }

            if (call.StartsWith("solution import", StringComparison.Ordinal))
            {
                imported = true;
                return import?.Invoke() ?? Ok("Solution Imported successfully.");
            }

            Assert.StartsWith("power-automate list-cloud-flows", call);
            return Ok(Sample(imported ? after : before));
        };
    }

    private static bool Imported(Outcome r) => r.Calls.Any(c => c.StartsWith("solution import", StringComparison.Ordinal));

    [Fact]
    public void Dry_run_reads_the_target_and_each_flow_and_never_imports()
    {
        var zip = SeedZip();

        var r = Run(Pac(), "settings", "apply", zip, "--settings", Valid);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.False(Imported(r));
        Assert.Equal(["org who", $"power-automate list-cloud-flows --environment {EnvironmentId} --workflow-id {SeedFlowId} --json"], r.Calls);
        Assert.All(r.Timeouts, t => Assert.Equal(TimeSpan.FromSeconds(30), t));
        Assert.StartsWith("environment: Contoso Dev\nmanaged: false\n", r.Out);
        Assert.Contains("import: planned\nflows: planned\nflow Stamp work item status: new, expected on\ndry run: nothing imported\n", r.Out);
    }

    [Fact]
    public void Dry_run_reports_an_existing_flow_and_a_new_flow_that_was_off_at_export()
    {
        Assert.Contains("flow Stamp work item status: exists, off\n", Run(Pac(before: "list-cloud-flows-off.json"), "settings", "apply", SeedZip(), "--settings", Valid).Out);
        Assert.Contains("flow Stamp work item status: new, expected off\n", Run(Pac(), "settings", "apply", SeedZip(flowOn: false), "--settings", Valid).Out);
    }

    [Fact]
    public void Validate_problems_stop_before_any_pac_call()
    {
        var r = Run(Pac(), "settings", "apply", SeedZip(), "--settings", Problems);

        Assert.Equal(1, r.Code);
        Assert.Empty(r.Calls);
        Assert.Contains("validate: failed\ntarget: skipped\nplan: skipped\nimport: skipped\nflows: skipped\n4 problems\n", r.Out);
    }

    public static TheoryData<PacResult, string> TargetFailures => new()
    {
        { new PacResult(false, false, 0, ""), "pac was not found on PATH" },
        { new PacResult(true, false, 1, "Error: no profile"), "`pac org who` failed (exit code 1). Sign in with pac auth create" },
        { new PacResult(true, true, 0, ""), "`pac org who` timed out after 30 s. Sign in with pac auth create" },
        { new PacResult(true, false, 0, "Connected as admin@contoso.com\n"), "`pac org who` printed no environment name or id." },
    };

    [Theory]
    [MemberData(nameof(TargetFailures))]
    public void A_target_failure_exits_3_without_importing(PacResult who, string message)
    {
        var r = Run(_ => who, "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(3, r.Code);
        Assert.Equal(["org who"], r.Calls);
        Assert.Contains("validate: ok\ntarget: failed\nplan: skipped\nimport: skipped\nflows: skipped\n" + message, r.Out);
        Assert.DoesNotContain("environment:", r.Out);
    }

    [Fact]
    public void A_wrong_target_name_exits_2_without_importing()
    {
        var r = Run(Pac(), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "contoso dev");

        Assert.Equal(2, r.Code);
        Assert.Equal(["org who"], r.Calls);
        Assert.Contains("target: failed\n", r.Out);
        Assert.EndsWith("pac's current environment is \"Contoso Dev\", not the --target \"contoso dev\"; nothing was imported.\n", r.Out);
    }

    [Fact]
    public void Apply_with_every_flow_on_exits_0_and_pins_the_environment_on_every_later_call()
    {
        var zip = SeedZip();

        var r = Run(Pac(), "settings", "apply", zip, "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Equal(
            [
                "org who",
                $"power-automate list-cloud-flows --environment {EnvironmentId} --workflow-id {SeedFlowId} --json",
                $"solution import --environment {EnvironmentId} --path {zip} --settings-file {Valid}",
                $"power-automate list-cloud-flows --environment {EnvironmentId} --workflow-id {SeedFlowId} --json",
            ],
            r.Calls);
        Assert.Equal([30, 30, 3600, 30], r.Timeouts.Select(t => t.TotalSeconds));
        Assert.StartsWith("environment: Contoso Dev\nmanaged: false\n", r.Out);
        Assert.EndsWith("import: ok\nflows: ok\nflow Stamp work item status: on\nimported, all flows on\n", r.Out);
    }

    [Theory]
    [InlineData("list-cloud-flows-off.json", "off")]
    public void A_flow_not_on_after_the_import_exits_1(string after, string state)
    {
        var r = Run(Pac(after: after), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(1, r.Code);
        Assert.EndsWith($"flows: failed\nflow Stamp work item status: {state}\nimported, 1 of 1 flows not on\n", r.Out);
    }

    [Fact]
    public void A_failed_import_exits_3_and_sends_pac_output_to_stderr_only()
    {
        var r = Run(Pac(import: () => new PacResult(true, false, 1, "pac out text\n", "pac err text\n")), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev", "--format", "json");

        Assert.Equal(3, r.Code);
        Assert.Equal("environment: Contoso Dev\n  | pac out text\n  | pac err text\n", r.Err.ReplaceLineEndings("\n"));
        Assert.DoesNotContain("pac out text", r.Out);
        Assert.DoesNotContain("pac err text", r.Out);
        var root = JsonDocument.Parse(r.Out).RootElement;
        Assert.Equal(1, root.GetProperty("pacExitCode").GetInt32());
        Assert.Equal("failed", root.GetProperty("steps")[3].GetProperty("status").GetString());
        Assert.Equal("skipped", root.GetProperty("steps")[4].GetProperty("status").GetString());
        Assert.Equal(3, r.Calls.Count); // org who, one read, the import, and no read after it
    }

    [Fact]
    public void A_failed_imports_pac_text_is_prefixed_so_no_line_is_a_workflow_command()
    {
        var r = Run(Pac(import: () => new PacResult(true, false, 1, "::error::from pac\n", "::set-output name=x::y\r\nerror: fake\n")), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(3, r.Code);
        var lines = r.Err.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).Where(l => l != "environment: Contoso Dev").ToList();
        Assert.Contains("  | ::error::from pac", lines);
        Assert.Contains("  | ::set-output name=x::y", lines);
        Assert.Contains("  | error: fake", lines);
        Assert.All(lines, l => Assert.StartsWith("  | ", l));
    }

    [Fact]
    public void The_environment_id_appears_in_no_output_of_powerops()
    {
        foreach (var import in new Func<PacResult>[] { () => Ok(), () => new PacResult(true, false, 1, "", "") })
        {
            foreach (var format in new[] { "text", "json" })
            {
                var r = Run(Pac(import: import), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev", "--format", format);

                Assert.DoesNotContain(EnvironmentId, r.Out);
                Assert.DoesNotContain(EnvironmentId, r.Err);
                Assert.Contains("<environment id>", r.Out);
            }
        }
    }

    [Fact]
    public void An_import_timeout_exits_3_and_says_it_may_still_be_running()
    {
        var r = Run(Pac(import: () => new PacResult(true, true, 0, "")), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Err);
        Assert.Contains("import: failed\nflows: skipped\n", r.Out);
        Assert.Contains("It may still be running in the environment", r.Out);
    }

    [Fact]
    public void In_json_mode_the_target_line_is_on_stderr_before_the_import_starts()
    {
        var o = new StringWriter();
        var e = new StringWriter();
        string? errAtImport = null;
        var pac = Pac();
        var code = Program.Run(["settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev", "--format", "json"], o, e, (a, _) =>
        {
            var call = string.Join(" ", a);
            if (call.StartsWith("solution import", StringComparison.Ordinal))
            {
                errAtImport = e.ToString();
                Assert.Equal("", o.ToString());
            }

            return pac(call);
        });

        Assert.Equal(0, code);
        Assert.Equal("environment: Contoso Dev\n", errAtImport);
        Assert.Equal("Contoso Dev", JsonDocument.Parse(o.ToString()).RootElement.GetProperty("environment").GetString());
    }

    [Fact]
    public void In_text_mode_the_target_line_is_the_first_output_before_the_import_starts()
    {
        var o = new StringWriter();
        string? outAtImport = null;
        var pac = Pac();
        Program.Run(["settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev"], o, new StringWriter(), (a, _) =>
        {
            var call = string.Join(" ", a);
            if (call.StartsWith("solution import", StringComparison.Ordinal))
            {
                outAtImport = o.ToString();
            }

            return pac(call);
        });

        Assert.Equal("environment: Contoso Dev\n", outAtImport);
        Assert.Single(o.ToString().Split('\n'), l => l.StartsWith("environment:", StringComparison.Ordinal));
    }

    [Fact]
    public void A_solution_without_cloud_flows_imports_and_exits_0()
    {
        var settings = Path.Combine(_dir, "empty.json");
        File.WriteAllText(settings, "{}");
        var zip = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "zips", "ColumnLevelSecuritySampleSolution.zip");

        var r = Run(Pac(), "settings", "apply", zip, "--settings", settings, "--apply", "--target", "Contoso Dev");

        Assert.Equal(0, r.Code);
        Assert.Equal(["org who", $"solution import --environment {EnvironmentId} --path {zip} --settings-file {settings}"], r.Calls);
        Assert.EndsWith("flows: ok\nimported, no cloud flows\n", r.Out);
    }

    [Fact]
    public void Paths_are_printed_as_typed()
    {
        var zip = Path.GetRelativePath(Directory.GetCurrentDirectory(), SeedZip());

        var r = Run(Pac(), "settings", "apply", zip, "--settings", Valid);

        Assert.Equal(0, r.Code);
        Assert.Contains($"command: pac solution import --environment \"<environment id>\" --path {Quoted(zip)} --settings-file {Quoted(Valid)}\n", r.Out);
    }

    // The text report quotes an argument that holds a space, as a checkout path may.
    private static string Quoted(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;

    // Goldens hold stable paths: the temp zip becomes seed.zip and the settings file its repo-relative path.
    private static string Stable(string output, string zip, string format)
    {
        foreach (var (path, stable) in new[] { (zip, "seed.zip"), (Valid, "tests/fixtures/settings/powerops-seed-v1.valid.json") })
        {
            output = format == "json" ? output.Replace(path.Replace("\\", "\\\\"), stable) : output.Replace(Quoted(path), stable);
        }

        return output;
    }

    [Theory]
    [InlineData("text", "settings-apply-dry-run.txt")]
    [InlineData("json", "settings-apply-dry-run.json")]
    public void Dry_run_matches_golden(string format, string golden)
    {
        var zip = SeedZip();

        Golden.Verify(Stable(Run(Pac(), "settings", "apply", zip, "--settings", Valid, "--format", format).Out, zip, format), golden);
    }

    [Fact]
    public void Apply_matches_golden()
    {
        var zip = SeedZip();

        Golden.Verify(Stable(Run(Pac(), "settings", "apply", zip, "--settings", Valid, "--apply", "--target", "Contoso Dev", "--format", "json").Out, zip, "json"), "settings-apply.json");
    }

    [Fact]
    public void A_failed_import_matches_golden()
    {
        var zip = SeedZip();
        var r = Run(Pac(import: () => new PacResult(true, false, 1, "", "")), "settings", "apply", zip, "--settings", Valid, "--apply", "--target", "Contoso Dev", "--format", "json");

        Golden.Verify(Stable(r.Out, zip, "json"), "settings-apply-import-failed.json");
    }

    [Theory]
    [InlineData("--apply")]
    [InlineData("--target", "Contoso Dev")]
    public void Apply_and_target_go_together_and_are_checked_before_validate(params string[] options)
    {
        var r = Run(Pac(), ["settings", "apply", SeedZip(), "--settings", Problems, .. options]);

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.Empty(r.Calls);
        Assert.StartsWith("error: ", r.Err);
    }

    [Fact]
    public void A_folder_or_a_missing_path_is_a_usage_error()
    {
        Assert.Equal(2, Run(Pac(), "settings", "apply", Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1"), "--settings", Valid).Code);
        Assert.Equal(2, Run(Pac(), "settings", "apply", Path.Combine(_dir, "nope.zip"), "--settings", Valid).Code);
        Assert.Equal(2, Run(Pac(), "settings", "apply", SeedZip(), "--settings", Path.Combine(_dir, "nope.json")).Code);
    }

    [Fact]
    public void An_unreadable_settings_file_or_zip_exits_3()
    {
        var bad = Path.Combine(_dir, "bad.json");
        File.WriteAllText(bad, "not json");
        var notZip = Path.Combine(_dir, "not.zip");
        File.WriteAllText(notZip, "not a zip");

        var settings = Run(Pac(), "settings", "apply", SeedZip(), "--settings", bad);
        var zip = Run(Pac(), "settings", "apply", notZip, "--settings", Valid);

        Assert.Equal(3, settings.Code);
        Assert.StartsWith($"error: {bad}: ", settings.Err);
        Assert.Equal(3, zip.Code);
        Assert.Empty(settings.Calls.Concat(zip.Calls));
    }

    [Fact]
    public void Unreadable_list_cloud_flows_output_exits_3()
    {
        var r = Run(Pac(before: "list-cloud-flows-bad-id.txt"), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(3, r.Code);
        Assert.False(Imported(r));
        Assert.Contains("plan: failed\nimport: skipped\n", r.Out);
    }

    [Fact]
    public void A_failed_read_after_the_import_exits_3_and_says_the_import_succeeded()
    {
        var r = Run(Pac(after: "list-cloud-flows-bad-id.txt"), "settings", "apply", SeedZip(), "--settings", Valid, "--apply", "--target", "Contoso Dev");

        Assert.Equal(3, r.Code);
        Assert.Contains("import: ok\nflows: failed\n", r.Out);
        Assert.Contains("The import succeeded. Reading a cloud flow's state", r.Out);
    }
}