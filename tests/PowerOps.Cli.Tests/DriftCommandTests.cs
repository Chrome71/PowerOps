using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class DriftCommandTests : IDisposable
{
    private const string Url = "https://contoso-test.crm.dynamics.com/";

    private static readonly string DevFile = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-dev.json");
    private static readonly string TestFile = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-test.json");

    private readonly string _dir = Directory.CreateTempSubdirectory("powerops-drift-").FullName;

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static Snapshot Dev => SnapshotFile.Load(DevFile);

    private static Snapshot Test => SnapshotFile.Load(TestFile);

    // Dev's content under the name "Contoso Test": equal to Dev as far as drift goes.
    private static Snapshot Twin => Dev with { Environment = "Contoso Test" };

    private sealed record Outcome(int Code, string Out, string Err, List<string> Calls);

    private static Outcome Run(IDictionary<string, Snapshot>? live, params string[] args)
    {
        var (o, e, calls) = (new StringWriter(), new StringWriter(), new List<string>());
        var code = Program.Run(
            args,
            o,
            e,
            (_, _) => throw new Xunit.Sdk.XunitException("pac must not be called"),
            live is null
                ? null
                : (solution, value) =>
                {
                    calls.Add($"{solution}|{value}");
                    return live.TryGetValue(value, out var s) ? s : throw new DriftReadException("the read failed");
                });
        return new Outcome(code, Lf(o), Lf(e), calls);
    }

    // WriteLine uses the platform line ending; the expected strings use \n.
    private static string Lf(StringWriter w) => w.ToString().ReplaceLineEndings("\n");

    private static string[] Args(params string[] envs) =>
        ["drift", "--solution", "PowerOpsSeed", .. envs.SelectMany(e => new[] { "--env", e })];

    private string Write(string name, string text)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, text);
        return path;
    }

    private static Dictionary<string, Snapshot> Pair(Snapshot a, Snapshot b) => new() { ["a"] = a, ["b"] = b };

    private static IEnumerable<(string Name, Func<Snapshot, Snapshot> Change, string Expected)> Changes()
    {
        yield return ("version", s => s with { Version = "2.0.0.0" }, "version PowerOpsSeed: 1.0.0.0 -> 2.0.0.0");
    }

    public static IEnumerable<object[]> ChangeNames() => Changes().Select(c => new object[] { c.Name });

    [Fact]
    public void Equal_environments_exit_0()
    {
        var r = Run(Pair(Dev, Twin), [.. Args("a", "b"), "--format", "json"]);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Equal(["PowerOpsSeed|a", "PowerOpsSeed|b"], r.Calls);
        Assert.Equal(0, Run(Pair(Dev, Twin), Args("a", "b")).Code);
        Assert.EndsWith("no drift\n", Run(Pair(Dev, Twin), Args("a", "b")).Out);
    }

    [Theory]
    [MemberData(nameof(ChangeNames))]
    public void A_difference_exits_1_and_is_named(string name)
    {
        var (_, change, expected) = Changes().Single(c => c.Name == name);

        var r = Run(Pair(Dev, change(Twin)), Args("a", "b"));

        Assert.Equal(1, r.Code);
        Assert.Contains(expected, r.Out);
    }

    [Fact]
    public void The_captured_dev_and_the_edited_test_give_the_text_json_and_markdown_goldens()
    {
        foreach (var (format, golden) in new[] { ("text", "drift-seed.txt"), ("json", "drift-seed.json"), ("markdown", "drift-seed.md") })
        {
            var r = RunLive(Capture.Fakes("edited"), [.. Args(Capture.DevEnv, Capture.TestEnv), "--format", format]);
            Assert.Equal(1, r.Code);
            Assert.Equal("", r.Err);
            Golden.Verify(r.Out, golden);
        }
    }

    [Fact]
    public void The_captured_dev_and_the_clean_test_are_no_drift_and_match_the_none_golden()
    {
        var r = RunLive(Capture.Fakes("clean"), [.. Args(Capture.DevEnv, Capture.TestEnv), "--format", "json"]);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Golden.Verify(r.Out, "drift-none.json");
    }

    [Fact]
    public void Seed_files_compare_offline_with_drift()
    {
        var r = Run(null, "drift", "--solution", "PowerOpsSeed", "--env", DevFile, "--env", TestFile);

        Assert.Equal(1, r.Code);
        Assert.Equal("", r.Err);
        Assert.Contains("unmanaged-layer Notify on new work item", r.Out);
    }

    public static IEnumerable<object[]> UsageErrors()
    {
        yield return new object[] { new[] { "drift", "--solution", "PowerOpsSeed", "--env", "a" } };
        yield return new object[] { new[] { "drift", "--env", "a", "--env", "b" } };
        yield return new object[] { new[] { "drift", "--solution", "bad name", "--env", "a", "--env", "b" } };
        yield return new object[] { new[] { "drift", "--solution", "Sol\n", "--env", "a", "--env", "b" } };
        yield return new object[] { new[] { "drift", "--solution", "PowerOpsSeed", "--env", "a", "--env", "missing/Nope.JSON" } };
    }

    [Theory]
    [MemberData(nameof(UsageErrors))]
    public void Usage_errors_exit_2_before_any_live_call(string[] args)
    {
        var r = Run(Pair(Dev, Twin), args);

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
        Assert.StartsWith("error: ", r.Err);
        Assert.DoesNotContain("Nope", r.Err);
        Assert.DoesNotContain("Sol", r.Err);
    }

    [Fact]
    public void A_missing_snapshot_file_is_named_by_position()
    {
        var r = Run(Pair(Dev, Twin), Args("a", Path.Combine(_dir, "nope.json")));

        Assert.Equal(2, r.Code);
        Assert.Equal("error: --env 2: no snapshot file at that path\n", r.Err);
    }

    private const string NotAFileOrEnvironment = "error: --env 2: not a snapshot file or an environment; a value with a path separator must be a URL or an existing file\n";

    [Theory]
    [InlineData("bad/emptydir")]
    [InlineData("bad\\emptydir")]
    [InlineData("./x")]
    [InlineData("<existing folder>")]
    public void A_value_with_a_path_separator_that_is_not_a_url_or_an_existing_file_exits_2_and_reads_no_tenant(string value)
    {
        var env = value == "<existing folder>" ? _dir : value;

        var r = Run(Pair(Dev, Twin), Args("a", env));

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
        Assert.Equal(NotAFileOrEnvironment, r.Err);
    }

    [Fact]
    public void An_existing_file_without_a_json_name_is_not_refused_for_its_path()
    {
        var file = Write("snapshot.txt", SnapshotFile.Write(Dev));

        var r = Run(null, Args(file, DevFile));

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void Save_on_a_file_or_with_only_snapshot_inputs_exits_2()
    {
        var file = Write("file.txt", "x");

        var onFile = Run(Pair(Dev, Twin), [.. Args("a", "b"), "--save", file]);
        var onlySnapshots = Run(Pair(Dev, Twin), [.. Args(DevFile, TestFile), "--save", Path.Combine(_dir, "out")]);

        Assert.Equal(2, onFile.Code);
        Assert.Equal(2, onlySnapshots.Code);
        Assert.Empty(onFile.Calls);
        Assert.False(Directory.Exists(Path.Combine(_dir, "out")));
    }

    [Fact]
    public void A_snapshot_for_another_solution_exits_2_without_a_live_call()
    {
        var other = Write("other.json", SnapshotFile.Write(Dev with { Solution = "Other" }));

        var r = Run(Pair(Dev, Twin), Args(other, "b"));

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
        Assert.Equal("error: --env 1: the snapshot is for another solution\n", r.Err);
    }

    [Fact]
    public void A_solution_name_in_another_case_is_the_same_solution()
    {
        var lower = Write("lower.json", SnapshotFile.Write(Dev with { Solution = "POWEROPSSEED" }));

        Assert.Equal(0, Run(null, Args(lower, DevFile)).Code);
    }

    [Fact]
    public void A_snapshot_with_a_wrong_schemaVersion_exits_3_without_a_live_call()
    {
        var bad = Write("bad.json", SnapshotFile.Write(Dev).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));

        var r = Run(Pair(Dev, Twin), Args("a", bad));

        Assert.Equal(3, r.Code);
        Assert.Empty(r.Calls);
        Assert.StartsWith("error: --env 2: ", r.Err);
        Assert.DoesNotContain(bad, r.Err);
        Assert.Equal("", r.Out);
    }

    [Fact]
    public void A_usage_error_and_an_unreadable_snapshot_in_one_run_exit_2()
    {
        var bad = Write("bad.json", "{}");

        var r = Run(Pair(Dev, Twin), "drift", "--solution", "bad name", "--env", bad, "--env", "b");

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
    }

    [Fact]
    public void A_failed_live_read_exits_3_with_no_report_and_no_files()
    {
        var save = Path.Combine(_dir, "saved");

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev }, [.. Args("a", Url), "--save", save]);

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Out);
        Assert.Equal("error: --env 2: the read failed\n", r.Err);
        Assert.False(Directory.Exists(save));
        Assert.DoesNotContain(Url, r.Err);
    }

    [Fact]
    public void Without_an_injected_reader_a_live_env_goes_through_the_default_reader_and_pac()
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        var pacCalls = new List<string>();

        var code = Program.Run(Args(DevFile, "contoso-test"), o, e, (a, _) =>
        {
            pacCalls.Add(string.Join(' ', a));
            return new PacResult(false, false, 0, "");
        });

        Assert.Equal(3, code);
        Assert.Equal("", o.ToString());
        Assert.Equal("error: --env 2: pac was not found on PATH. Install pac (dotnet tool install --global Microsoft.PowerApps.CLI.Tool) and add the dotnet tools folder to PATH.\n", Lf(e));
        Assert.Equal(["org who --environment contoso-test"], pacCalls);
    }

    [Fact]
    public void A_solution_installed_nowhere_exits_2()
    {
        var none = new Snapshot("Contoso Dev", "PowerOpsSeed", false, null, null, true, [], [], [], []);

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = none, ["b"] = none with { Environment = "Contoso Test" } }, Args("a", "b"));

        Assert.Equal(2, r.Code);
        Assert.Equal("error: solution `PowerOpsSeed` is not installed in any environment\n", r.Err);
        Assert.Equal("", r.Out);
    }

    [Fact]
    public void A_url_env_reaches_the_live_reader_unchanged_and_is_printed_nowhere()
    {
        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev, [Url] = Twin }, Args("a", Url));

        Assert.Equal(0, r.Code);
        Assert.Equal(["PowerOpsSeed|a", $"PowerOpsSeed|{Url}"], r.Calls);
        Assert.DoesNotContain("crm.dynamics.com", r.Out + r.Err);
    }

    [Fact]
    public void Save_writes_one_file_per_live_env_and_none_for_snapshot_inputs()
    {
        var save = Path.Combine(_dir, "saved");

        var r = Run(new Dictionary<string, Snapshot> { ["b"] = Test }, [.. Args(DevFile, "b"), "--save", save]);

        Assert.Equal(1, r.Code);
        Assert.Equal([Path.Combine(save, "PowerOpsSeed.Contoso Test.json")], Directory.GetFiles(save));
        Assert.Equal(SnapshotFile.Write(Test), File.ReadAllText(Path.Combine(save, "PowerOpsSeed.Contoso Test.json")));
    }

    [Fact]
    public void Save_overwrites_and_replaces_characters_outside_the_safe_set()
    {
        var save = Path.Combine(_dir, "saved");
        Directory.CreateDirectory(save);
        File.WriteAllText(Path.Combine(save, "PowerOpsSeed.Contoso-Dev- 1_x.json"), "old");

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev with { Environment = "Contoso/Dev: 1_x" }, ["b"] = Twin }, [.. Args("a", "b"), "--save", save]);

        Assert.Equal(0, r.Code);
        Assert.Equal(SnapshotFile.Write(Dev with { Environment = "Contoso/Dev: 1_x" }), File.ReadAllText(Path.Combine(save, "PowerOpsSeed.Contoso-Dev- 1_x.json")));
        Assert.Equal(2, Directory.GetFiles(save).Length);
    }

    [Fact]
    public void Save_of_the_captured_dev_matches_the_snapshot_golden()
    {
        var save = Path.Combine(_dir, "saved");

        var r = RunLive(Capture.Fakes("edited"), [.. Args(Capture.DevEnv, Capture.TestEnv), "--save", save]);

        Assert.Equal(1, r.Code);
        Golden.Verify(File.ReadAllText(Path.Combine(save, "PowerOpsSeed.Contoso Dev.json")).ReplaceLineEndings("\n"), "snapshot-seed.json");
    }

    [Fact]
    public void Two_live_envs_with_the_same_file_name_exit_2_and_write_nothing()
    {
        var save = Path.Combine(_dir, "saved");

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev, ["b"] = Dev with { Environment = "contoso dev" } }, [.. Args("a", "b"), "--save", save]);

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.False(Directory.Exists(save));
    }

    [Fact]
    public void A_saved_pair_compared_offline_gives_the_same_text_and_exit_code_as_the_live_run()
    {
        var save = Path.Combine(_dir, "saved");
        var liveRun = Run(Pair(Dev, Test), [.. Args("a", "b"), "--save", save]);

        var offline = Run(null, Args(Path.Combine(save, "PowerOpsSeed.Contoso Dev.json"), Path.Combine(save, "PowerOpsSeed.Contoso Test.json")));

        Assert.Equal(1, liveRun.Code);
        Assert.Equal(liveRun.Code, offline.Code);
        Assert.Equal(liveRun.Out, offline.Out);
    }

    [Fact]
    public void Out_writes_the_report_to_the_file_and_nothing_to_stdout()
    {
        var file = Path.Combine(_dir, "report.md");

        var r = Run(null, [.. Args(DevFile, TestFile), "--format", "markdown", "--out", file]);

        Assert.Equal(1, r.Code);
        Assert.Equal("", r.Out);
        Assert.StartsWith("## PowerOps drift", File.ReadAllText(file));
        Assert.False(File.ReadAllBytes(file).AsSpan().StartsWith("﻿"u8.ToArray()));
    }

    // ---- live runs through DriftLiveReader with fake pac, az and web (no network) ----

    private const string DevEnv = "ENV-VALUE-DEV";
    private const string TestEnv = "ENV-VALUE-TEST";

    private static Outcome RunLive(LiveFakes fakes, params string[] args)
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        var code = Program.Run(args, o, e, (_, _) => throw new Xunit.Sdk.XunitException("the default pac must not be called"), fakes.Reader.Read);
        return new Outcome(code, Lf(o), Lf(e), []);
    }

    private static LiveFakes FlowOffInTest() => new()
    {
        WebFn = url =>
        {
            var body = LiveFakes.Sample(url);
            return LiveFakes.Ok(url.Host.Contains("test") && LiveFakes.EntitySet(url) == "workflows" ? body.Replace("\"statecode\":1", "\"statecode\":0") : body);
        },
    };

    [Fact]
    public void A_live_dev_and_a_live_test_from_the_samples_show_the_managed_difference_as_information_and_exit_0()
    {
        var fakes = new LiveFakes();

        var r = RunLive(fakes, Args(DevEnv, TestEnv));

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Contains("(info)", r.Out);
        Assert.EndsWith("no drift\n", r.Out);
        Assert.Equal([["org", "who", "--environment", DevEnv], ["org", "who", "--environment", TestEnv]], fakes.PacCalls);
    }

    [Fact]
    public void A_url_env_reaches_pac_as_the_environment_and_is_printed_nowhere()
    {
        var fakes = new LiveFakes();

        var r = RunLive(fakes, Args(DevEnv, Url));

        Assert.Equal(0, r.Code);
        Assert.Equal(Url, fakes.PacCalls[1][^1]);
        Assert.DoesNotContain("crm.dynamics.com", r.Out + r.Err);
    }

    [Fact]
    public void A_flow_that_is_off_in_the_second_live_environment_exits_1()
    {
        var fakes = FlowOffInTest();

        var r = RunLive(fakes, Args(DevEnv, TestEnv));

        Assert.Equal(1, r.Code);
        Assert.Contains("flow-state Notify on new work item: on -> off", r.Out);
    }

    [Fact]
    public void A_live_run_saved_and_compared_offline_gives_the_same_text_and_exit_code()
    {
        var save = Path.Combine(_dir, "saved");
        var fakes = FlowOffInTest();
        var live = RunLive(fakes, [.. Args(DevEnv, TestEnv), "--save", save]);

        var offline = Run(null, Args(Path.Combine(save, "PowerOpsSeed.Contoso Dev.json"), Path.Combine(save, "PowerOpsSeed.Contoso Test.json")));

        Assert.Equal(1, live.Code);
        Assert.Equal((live.Code, live.Out), (offline.Code, offline.Out));
    }

    // Each scenario breaks the second environment (the first one reads fine, so a save would have something to write).
    private static readonly Dictionary<string, Action<LiveFakes>> Failures = new()
    {
        ["pac exit 1"] = f => OnTestPac(f, new PacResult(true, false, 1, "pac says no\n", "pac stderr\n")),
        ["az exit 1 with the token on stdout"] = f => f.AzFn = a => a[3].Contains("test") ? new PacResult(true, false, 1, LiveFakes.Token, "ERROR: run az login\n") : new PacResult(true, false, 0, LiveFakes.Token),
        ["web 400"] = f => OnTestWeb(f, new WebResult(400, LiveFakes.SampleFile("error-400.json"), false)),
        ["web 403"] = f => OnTestWeb(f, new WebResult(403, """{"error":{"code":"1","message":"No access"}}""", false)),
        ["layer read fails"] = f =>
        {
            var good = f.WebFn;
            f.WebFn = u => u.Host.Contains("test") && LiveFakes.EntitySet(u) == "msdyn_componentlayers" ? new WebResult(403, "", false) : good(u);
        },
    };

    private static void OnTestPac(LiveFakes f, PacResult broken)
    {
        var good = f.PacFn;
        f.PacFn = a => a[^1].Contains("TEST") ? broken : good(a);
    }

    private static void OnTestWeb(LiveFakes f, WebResult broken)
    {
        var good = f.WebFn;
        f.WebFn = u => u.Host.Contains("test") ? broken : good(u);
    }

    public static IEnumerable<object[]> FailureNames() => Failures.Keys.Select(k => new object[] { k });

    private static void AssertNoSecrets(string text)
    {
        Assert.DoesNotContain(LiveFakes.Token, text);
        Assert.DoesNotContain(DevEnv, text);
        Assert.DoesNotContain(TestEnv, text);
        Assert.DoesNotContain("crm.dynamics.com", text);
    }

    [Theory]
    [MemberData(nameof(FailureNames))]
    public void A_failed_live_read_exits_3_with_no_report_no_files_and_no_secret_anywhere(string name)
    {
        var (save, report) = (Path.Combine(_dir, "saved"), Path.Combine(_dir, "report.txt"));
        var fakes = new LiveFakes();
        Failures[name](fakes);

        var r = RunLive(fakes, [.. Args(DevEnv, TestEnv), "--save", save, "--out", report]);

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Out);
        Assert.False(Directory.Exists(save));
        Assert.False(File.Exists(report));
        Assert.StartsWith("error: --env 2: ", r.Err);
        AssertNoSecrets(r.Err);
    }

    [Fact]
    public void An_environment_url_on_another_host_exits_2_before_any_token_or_web_call()
    {
        var fakes = new LiveFakes();
        fakes.PacFn = _ => new PacResult(true, false, 0, "Friendly Name:  Evil\nOrg URL:  https://localhost/\n");

        var r = RunLive(fakes, Args(DevEnv, TestEnv));

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.Equal("error: --env 1: `pac org who` named an environment URL that is not on a documented Dataverse host (*.dynamics.com, *.crm.microsoftdynamics.us, *.crm.appsplatform.us, *.crm.dynamics.cn); no token was requested.\n", r.Err);
        Assert.Empty(fakes.AzCalls);
        Assert.Empty(fakes.WebCalls);
        Assert.DoesNotContain("localhost", r.Out + r.Err);
    }

    [Fact]
    public void Forwarded_text_goes_to_stderr_after_the_error_line_with_every_line_prefixed_and_no_doubled_line_break()
    {
        var fakes = new LiveFakes();
        Failures["pac exit 1"](fakes);

        var r = RunLive(fakes, Args(DevEnv, TestEnv));

        Assert.Equal("error: --env 2: `pac org who` failed (exit code 1). Sign in with pac auth create, and check that the account can read that environment.\n  | pac says no\n  | pac stderr\n", r.Err);
        Assert.DoesNotContain("\n\n", r.Err);
        Assert.Equal("", r.Out);
    }

    [Fact]
    public void A_web_error_message_is_forwarded_to_stderr_only_with_a_prefix_and_ends_with_a_line_break()
    {
        var fakes = new LiveFakes();
        Failures["web 400"](fakes);

        var r = RunLive(fakes, Args(DevEnv, TestEnv));

        Assert.Equal("error: --env 2: the Web API read of `solution` failed (HTTP 400).\n  | Entity 'nosuchtable' With Id = 00000000-0000-0000-0000-000000000000 Does Not Exist\n", r.Err);
        Assert.Equal("", r.Out);
    }

    [Fact]
    public void A_successful_run_leaves_no_token_env_value_or_url_in_stdout_the_report_stderr_or_saved_files()
    {
        foreach (var format in new[] { "text", "json", "markdown" })
        {
            var save = Path.Combine(_dir, "saved-" + format);
            var report = Path.Combine(_dir, "report-" + format);
            var fakes = new LiveFakes();

            var shown = RunLive(fakes, [.. Args(DevEnv, TestEnv), "--save", save, "--format", format]);
            var written = RunLive(fakes, [.. Args(DevEnv, TestEnv), "--format", format, "--out", report]);

            Assert.Equal((0, 0), (shown.Code, written.Code));
            AssertNoSecrets(shown.Out + shown.Err + written.Out + written.Err + File.ReadAllText(report));
            foreach (var file in Directory.GetFiles(save))
            {
                AssertNoSecrets(File.ReadAllText(file));
            }

            Assert.Equal(2, Directory.GetFiles(save).Length);
        }
    }
}