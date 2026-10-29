using System.Diagnostics;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Runs action/plan.sh, action/apply.sh, action/diff.sh, action/comment.sh, action/test.sh, action/powerops.sh, action/annotate.sh and action/summary.sh with bash, against stub `gh` and `dotnet` programs
// that come first on PATH. Skipped on Windows and where bash (or jq or git, which a test needs) is not found.
public sealed class ActionScriptTests : IDisposable
{
    private const string Marker = "<!-- powerops -->";

    private static readonly string Scripts = Path.Combine(Golden.RepoRoot, "action");

    private readonly string _root = Directory.CreateTempSubdirectory("powerops-action-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    // ---- comment.sh ----

    [Fact]
    public void Comment_is_created_when_none_has_the_marker()
    {
        var run = RunComment(list: """[{"id":1,"user":{"login":"someone"},"body":"hello"}]""");

        Assert.Equal(0, run.Code);
        var calls = run.GhCalls;
        Assert.Contains("api --paginate repos/contoso/widgets/issues/7/comments --jq", calls[0]);
        Assert.Equal(2, calls.Length);
        Assert.StartsWith("api repos/contoso/widgets/issues/7/comments -F body=@", calls[1]);
        Assert.DoesNotContain("PATCH", calls[1]);
        Assert.Equal(Marker + "\n## PowerOps diff\n\nbody\n", run.Posted);
        Assert.False(File.Exists(run.Summary));
    }

    [Fact]
    public void Comment_by_the_bot_with_the_marker_is_updated_and_the_others_are_ignored()
    {
        var list = $$"""
            [
              {"id":1,"user":{"login":"someone"},"body":"{{Marker}}\nnot the bot"},
              {"id":2,"user":{"login":"github-actions[bot]"},"body":"a comment without the marker"},
              {"id":3,"user":{"login":"github-actions[bot]"},"body":null},
              {"id":4,"user":{"login":"github-actions[bot]"},"body":"{{Marker}}\nold"},
              {"id":5,"user":{"login":"github-actions[bot]"},"body":"{{Marker}}\nolder"}
            ]
            """;

        var run = RunComment(list);

        Assert.Equal(0, run.Code);
        Assert.Equal(2, run.GhCalls.Length);
        Assert.StartsWith("api -X PATCH repos/contoso/widgets/issues/comments/4 -F body=@", run.GhCalls[1]);
        Assert.StartsWith(Marker + "\n", run.Posted);
    }

    [Fact]
    public void Comment_on_a_fork_goes_to_the_job_summary_only()
    {
        var run = RunComment(list: "[]", fork: "true");

        Assert.Equal(0, run.Code);
        Assert.Empty(run.GhCalls);
        Assert.Equal("## PowerOps diff\n\nbody\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void Oversize_comment_is_replaced_by_a_note_and_the_summary_has_the_report()
    {
        var markdown = "## PowerOps diff\n\n" + new string('x', 70000) + "\n";

        var run = RunComment(list: "[]", markdown: markdown);

        Assert.Equal(0, run.Code);
        Assert.Equal(Marker + "\nThe PowerOps report is too long for a comment; it is in the job summary of this run.\n", run.Posted);
        Assert.Equal(markdown, File.ReadAllText(run.Summary));
    }

    [Fact]
    public void A_failing_gh_gives_a_warning_and_the_job_summary_and_does_not_fail()
    {
        var run = RunComment(list: "[]", ghExit: "1");

        Assert.Equal(0, run.Code);
        Assert.Contains("::warning::", run.Out);
        Assert.Equal("## PowerOps diff\n\nbody\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void A_failing_update_gives_a_warning_and_one_copy_in_the_job_summary()
    {
        var run = RunComment(list: """[{"id":9,"user":{"login":"github-actions[bot]"},"body":"<!-- powerops -->\nold"}]""", ghFailOn: "PATCH");

        Assert.Equal(0, run.Code);
        Assert.Contains("::warning::", run.Out);
        Assert.Equal("## PowerOps diff\n\nbody\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void Nothing_happens_without_a_comment_file()
    {
        var run = RunComment(list: "[]", markdown: null);

        Assert.Equal(0, run.Code);
        Assert.Empty(run.GhCalls);
        Assert.False(File.Exists(run.Summary));
        Assert.Equal("", run.Out);
    }

    // ---- plan.sh ----

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Plan_with_exit_0_or_1_writes_the_summary_and_the_outputs(int exit)
    {
        var run = RunPlan(exit: exit, save: true);

        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\nplan-file={run.Folder}/powerops-plan.json\n", run.Output);
        Assert.Equal("## PowerOps deploy plan\n\nthe plan\n", File.ReadAllText(run.Summary));
        Assert.Equal("## PowerOps deploy plan\n\nthe plan\n", File.ReadAllText(run.Comment));
        Assert.Equal($"deploy plan {run.Folder} --target Contoso Test --save {run.Folder}/powerops-plan.json --format markdown --out {run.Comment}", run.DotnetCalls.Single().Replace("powerops.dll ", ""));
    }

    [Fact]
    public void Plan_output_names_the_plan_file_only_when_it_exists()
    {
        var run = RunPlan(exit: 1, save: false);

        Assert.Equal("exit-code=1\n", run.Output);
        Assert.Equal("## PowerOps deploy plan\n\nthe plan\n", File.ReadAllText(run.Summary));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Plan_with_exit_2_or_3_writes_the_fixed_text_and_logs_only_error_lines(int exit)
    {
        var run = RunPlan(exit: exit, save: false, stderr: "error: --target: pac failed\nForwarded: tenant 0000 at https://contoso.crm.dynamics.com\n");

        var fixedText = $"## PowerOps deploy plan\n\nThe plan could not run (exit code {exit}). See the job log.\n";
        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\n", run.Output);
        Assert.Equal(fixedText, File.ReadAllText(run.Comment));
        Assert.Equal(fixedText, File.ReadAllText(run.Summary));
        Assert.Equal("error: --target: pac failed\n", run.Out);
    }

    [Fact]
    public void Plan_in_debug_mode_prints_the_forwarded_text()
    {
        var run = RunPlan(exit: 3, save: false, stderr: "error: x\nForwarded: tenant 0000\n", debug: true);

        Assert.Equal("error: x\nForwarded: tenant 0000\n", run.Out);
    }

    [Fact]
    public void Plan_with_an_empty_folder_exits_2_without_running_powerops()
    {
        var run = RunPlan(exit: 0, save: false, folder: "");

        Assert.Equal(0, run.Code);
        Assert.Equal("::error::deploy-folder is required when target is set\n", run.Out);
        Assert.Equal("exit-code=2\n", run.Output);
        Assert.Empty(run.DotnetCalls);
        Assert.Contains("(exit code 2)", File.ReadAllText(run.Comment));
    }

    // ---- apply.sh ----

    [Fact]
    public void Apply_without_the_flag_is_a_dry_run_and_with_true_adds_apply()
    {
        var dry = RunApply(exit: 0, apply: "false");
        var real = RunApply(exit: 0, apply: "true");

        Assert.Equal("deploy apply plan/powerops-plan.json --target Contoso Test", dry.DotnetCalls.Single().Replace("powerops.dll ", ""));
        Assert.Equal("deploy apply plan/powerops-plan.json --target Contoso Test --apply", real.DotnetCalls.Single().Replace("powerops.dll ", ""));
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("")]
    public void Apply_other_values_than_true_do_not_add_apply(string value)
    {
        var run = RunApply(exit: 0, apply: value);

        Assert.DoesNotContain("--apply", run.DotnetCalls.Single());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Apply_records_the_exit_code_and_exits_0(int exit)
    {
        var run = RunApply(exit: exit);

        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\n", run.Output);
        Assert.Equal("the report\n", run.Out);
    }

    [Fact]
    public void Apply_logs_only_error_lines_and_not_forwarded_pac_lines()
    {
        var run = RunApply(exit: 3, stderr: "  | error: x\nerror: y\n  | tenant 0000 at https://contoso.crm.dynamics.com\n");

        Assert.Equal("the report\nerror: y\n", run.Out);
    }

    [Fact]
    public void Apply_in_debug_mode_prints_the_whole_stderr()
    {
        var run = RunApply(exit: 3, stderr: "  | error: x\nerror: y\n", debug: true);

        Assert.Equal("the report\n  | error: x\nerror: y\n", run.Out);
    }

    [Fact]
    public void Apply_puts_the_report_in_the_summary_in_a_fence_longer_than_any_run_inside_it()
    {
        var run = RunApply(exit: 0, report: "applied\n```` four\n``` three\n");

        Assert.Equal("## PowerOps deploy apply\n\n`````\napplied\n```` four\n``` three\n`````\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void Apply_summary_uses_a_three_backtick_fence_when_the_report_has_none()
    {
        var run = RunApply(exit: 0, report: "applied");

        Assert.Equal("## PowerOps deploy apply\n\n```\napplied\n```\n", File.ReadAllText(run.Summary));
    }

    [Theory]
    [InlineData("", "contoso/widgets", "pull_request", "target is required")]
    [InlineData("Contoso Test", "fork/widgets", "pull_request", "a fork pull request")]
    [InlineData("Contoso Test", "contoso/widgets", "pull_request_target", "pull_request_target")]
    public void Apply_refuses_without_a_target_on_a_fork_and_on_pull_request_target(string target, string headRepo, string eventName, string text)
    {
        var run = RunApply(exit: 0, target: target, headRepo: headRepo, eventName: eventName);

        Assert.Equal(0, run.Code);
        Assert.StartsWith("::error::", run.Out);
        Assert.Contains(text, run.Out);
        Assert.Equal("exit-code=2\n", run.Output);
        Assert.Empty(run.DotnetCalls);
    }

    [Theory]
    [InlineData("push", "")]
    [InlineData("workflow_dispatch", "")]
    [InlineData("merge_group", "")]
    [InlineData("push", "contoso/widgets")]
    [InlineData("pull_request", "contoso/widgets")]
    public void Apply_runs_on_push_workflow_dispatch_merge_group_and_a_same_repository_pull_request(string eventName, string headRepo)
    {
        var run = RunApply(exit: 0, headRepo: headRepo, eventName: eventName);

        Assert.Equal(0, run.Code);
        Assert.Equal("exit-code=0\n", run.Output);
        Assert.Single(run.DotnetCalls);
    }

    [Theory]
    [InlineData("workflow_run", "contoso/widgets")]
    [InlineData("issue_comment", "contoso/widgets")]
    [InlineData("pull_request_review_comment", "contoso/widgets")]
    [InlineData("pull_request_target", "contoso/widgets")]
    [InlineData("release", "contoso/widgets")]
    [InlineData("repository_dispatch", "contoso/widgets")]
    [InlineData("pull_request", "fork/widgets")]
    [InlineData("pull_request", "")]
    [InlineData("pull_request", null)]
    [InlineData("schedule", "contoso/widgets")]
    [InlineData("", "contoso/widgets")]
    public void Apply_refuses_every_other_event(string eventName, string? headRepo)
    {
        var run = RunApply(exit: 0, headRepo: headRepo, eventName: eventName);

        Assert.Equal(0, run.Code);
        Assert.StartsWith("::error::plan-file is supported only on push, workflow_dispatch, merge_group and same-repository pull_request events", run.Out);
        Assert.Equal("exit-code=2\n", run.Output);
        Assert.Empty(run.DotnetCalls);
    }

    // ---- powerops.sh, annotate.sh and summary.sh ----

    [Fact]
    public void Powerops_passes_stdout_and_the_exit_code_and_logs_only_error_and_note_lines_of_stderr()
    {
        Tools.Require("bash");
        var temp = NewDir("powerops");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "stdout.txt"), "the report\n");
        File.WriteAllText(Path.Combine(temp, "stderr.txt"), "error: a\n::error::forged\nnote: b\n  ::add-mask::z\nwarning: c\n");

        var run = Run("powerops.sh", temp, temp, new() { ["PO_DLL"] = "powerops.dll", ["STUB_EXIT"] = "3" }, stdin: null, args: ["analyze", "sol"]);

        Assert.Equal(3, run.Code);
        Assert.Equal("the report\n", run.Out);
        Assert.Equal("error: a\nnote: b\n", run.Err);
        Assert.Equal("powerops.dll analyze sol", Assert.Single(run.DotnetCalls));
    }

    [Fact]
    public void Powerops_names_an_exit_of_3_or_more_that_printed_no_error_line()
    {
        Tools.Require("bash");
        var temp = NewDir("powerops");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "stderr.txt"), "You must install .NET to run this application.\n");

        var run = Run("powerops.sh", temp, temp, new() { ["PO_DLL"] = "powerops.dll", ["STUB_EXIT"] = "3" }, stdin: null, args: ["analyze", "sol"]);

        Assert.Equal(3, run.Code);
        Assert.Equal("error: powerops exited with code 3\n", run.Err);
    }

    [Theory]
    [InlineData("error: a\n", 3)]
    [InlineData("note: b\nerror: a\n", 134)]
    [InlineData("warning: c\n", 1)]
    [InlineData("warning: c\n", 2)]
    [InlineData("", 0)]
    public void Powerops_adds_no_line_when_stderr_has_an_error_line_or_the_exit_is_below_3(string stderr, int exit)
    {
        Tools.Require("bash");
        var temp = NewDir("powerops");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);

        var run = Run("powerops.sh", temp, temp, new() { ["PO_DLL"] = "powerops.dll", ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture) }, stdin: null, args: ["analyze", "sol"]);

        Assert.Equal(exit, run.Code);
        Assert.Equal(string.Concat(stderr.Split('\n').Where(l => l.StartsWith("error:", StringComparison.Ordinal) || l.StartsWith("note:", StringComparison.Ordinal)).Select(l => l + "\n")), run.Err);
    }

    private const string AnnotateJson = """
        {
          "solutions": [
            { "path": ".", "findings": [
              { "ruleId": "PO011", "severity": "warning", "file": "Workflows/a,b:c.json", "message": "sets ::error:: 100% off\nsecond line\r\nthird" } ] },
            { "path": "sol", "findings": [ { "ruleId": "PO002", "severity": "error", "file": "Other/Customizations.xml", "message": "m" } ] },
            { "path": "dist/Demo.zip", "findings": [ { "ruleId": "PO003", "severity": "error", "file": "Workflows/f.json", "message": "z" } ] }
          ]
        }
        """;

    [Fact]
    public void Annotate_escapes_percent_line_breaks_and_the_property_separators()
    {
        Tools.Require("bash", "jq");
        var temp = NewDir("annotate");

        var run = Run("annotate.sh", temp, temp, [], stdin: AnnotateJson);

        Assert.Equal(0, run.Code);
        Assert.Equal(
            "::warning file=Workflows/a%2Cb%3Ac.json,title=PO011::sets ::error:: 100%25 off%0Asecond line%0D%0Athird\n" +
            "::error file=sol/Other/Customizations.xml,title=PO002::m\n" +
            "::error file=dist/Demo.zip,title=PO003::z\n",
            run.Out);
    }

    [Fact]
    public void Summary_under_the_limit_is_appended_unchanged()
    {
        Tools.Require("bash");
        var temp = NewDir("summary");
        var summary = Path.Combine(temp, "summary.md");
        var report = "## PowerOps analyze\n\n" + new string('x', 1_048_576 - 22) + "\n"; // exactly 1 MiB
        Assert.Equal(1_048_576, report.Length);

        var run = Run("summary.sh", temp, temp, new() { ["GITHUB_STEP_SUMMARY"] = summary }, stdin: report);

        Assert.Equal(0, run.Code);
        Assert.Equal(report, File.ReadAllText(summary));
    }

    [Fact]
    public void Summary_over_the_limit_is_cut_at_a_line_end_with_a_last_line_that_says_how_much()
    {
        Tools.Require("bash");
        var temp = NewDir("summary");
        var summary = Path.Combine(temp, "summary.md");
        var report = "## PowerOps analyze\n\n" + string.Concat(Enumerable.Range(0, 30_000).Select(i => $"| row {i} | {new string('é', 20)} |\n"));
        var total = System.Text.Encoding.UTF8.GetByteCount(report);
        Assert.True(total > 1_048_576);

        var run = Run("summary.sh", temp, temp, new() { ["GITHUB_STEP_SUMMARY"] = summary }, stdin: report);

        Assert.Equal(0, run.Code);
        var written = File.ReadAllBytes(summary);
        Assert.True(written.Length <= 1_048_576, $"{written.Length} bytes");
        var text = System.Text.Encoding.UTF8.GetString(written);
        var lines = text.Split('\n');
        var kept = text[..text.LastIndexOf("\n\n_The report", StringComparison.Ordinal)];
        var shown = System.Text.Encoding.UTF8.GetByteCount(kept) + 1;
        Assert.Equal($"_The report was cut to fit the 1 MiB job summary limit: {shown} of {total} bytes are shown._", lines[^2]);
        Assert.StartsWith(report[..20], text, StringComparison.Ordinal);
        Assert.EndsWith(" |\n\n_The report was cut to fit the 1 MiB job summary limit: " + $"{shown} of {total} bytes are shown._\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Summary_with_one_line_over_the_limit_keeps_the_start_of_it_as_valid_UTF8()
    {
        Tools.Require("bash");
        var temp = NewDir("summary");
        var summary = Path.Combine(temp, "summary.md");
        var report = new string('\u20AC', 349_525) + "ab"; // 3 bytes each: 1 MiB + 1 byte on one line, and the cut falls inside a character
        var total = System.Text.Encoding.UTF8.GetByteCount(report);
        Assert.Equal(1_048_577, total);

        var run = Run("summary.sh", temp, temp, new() { ["GITHUB_STEP_SUMMARY"] = summary }, stdin: report);

        Assert.Equal(0, run.Code);
        var written = File.ReadAllBytes(summary);
        Assert.True(written.Length <= 1_048_576, $"{written.Length} bytes");
        var text = new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(written);
        var kept = text[..text.IndexOf("\n_The report", StringComparison.Ordinal)];
        Assert.True(kept.Length > 300_000, $"{kept.Length} characters kept");
        Assert.All(kept, c => Assert.Equal('\u20AC', c));
        Assert.EndsWith($"\n_The report was cut to fit the 1 MiB job summary limit: {System.Text.Encoding.UTF8.GetByteCount(kept)} of {total} bytes are shown._\n", text, StringComparison.Ordinal);
    }

    // ---- test.sh ----

    // A passed test; a failed one in a sub folder whose name needs escaping, with a passed assertion, a failed one whose actual
    // is null and one with no actual; an unmocked one whose message has a line break.
    private const string TestJson = """
        {
          "schemaVersion": 1,
          "tests": [
            { "file": "a.flowtest.json", "flow": "Flow A", "result": "passed", "runStatus": "Succeeded", "assertions": [] },
            { "file": "sub/b,1:%.flowtest.json", "flow": "Flow B", "result": "failed", "runStatus": "Succeeded", "assertions": [
              { "index": 0, "description": "run status", "passed": false, "expected": "Failed", "actual": "Succeeded" },
              { "index": 1, "description": "status of X", "passed": true, "expected": "Succeeded" },
              { "index": 2, "description": "variable v", "passed": false, "expected": 1, "actual": null },
              { "index": 3, "description": "inputs of Y", "passed": false, "expected": 1, "message": "did not run" }
            ] },
            { "file": "c.flowtest.json", "flow": "Flow C", "result": "unmocked", "message": "Action 'Get':\nit needs a mock", "assertions": [] }
          ],
          "summary": { "passed": 1, "failed": 1, "unmocked": 1, "unsupported": 0 }
        }
        """;

    [Fact]
    public void Test_with_exit_0_writes_the_report_to_the_summary_and_no_annotation()
    {
        var run = RunTest(exit: 0);

        Assert.Equal(0, run.Code);
        Assert.Equal("exit-code=0\n", run.Output);
        Assert.Equal("## Flow tests\n\nthe report\n", File.ReadAllText(run.Summary));
        Assert.Equal("", run.Out);
        Assert.Matches(@"^test sol --tests flowtests --format markdown --out \S+$", Assert.Single(run.DotnetCalls).Replace("powerops.dll ", ""));
    }

    [Fact]
    public void Test_with_exit_1_annotates_each_test_file_that_did_not_pass()
    {
        var run = RunTest(exit: 1);

        Assert.Equal(0, run.Code);
        Assert.Equal("exit-code=1\n", run.Output);
        Assert.Equal("## Flow tests\n\nthe report\n", File.ReadAllText(run.Summary));
        Assert.Equal(2, run.DotnetCalls.Length);
        Assert.Matches(@"^test sol --tests flowtests --format json --out \S+$", run.DotnetCalls[1].Replace("powerops.dll ", ""));
        Assert.Equal(
            "::error file=flowtests/sub/b%2C1%3A%25.flowtest.json,title=Flow test failed::Flow B: assertion 0, run status: expected \"Failed\", got \"Succeeded\"; " +
            "assertion 2, variable v: expected 1, got null; assertion 3, inputs of Y: did not run\n" +
            "::error file=flowtests/c.flowtest.json,title=Flow test unmocked::Flow C: Action 'Get':%0Ait needs a mock\n",
            run.Out);
    }

    [Theory]
    [InlineData(".", "sub/b")]
    [InlineData("./", "sub/b")]
    [InlineData("./flowtests/", "flowtests/sub/b")]
    [InlineData("tests/flow tests/", "tests/flow tests/sub/b")]
    [InlineData("WORKSPACE/flowtests", "flowtests/sub/b")]
    [InlineData("WORKSPACE", "sub/b")]
    [InlineData("tests\\flows\\", "tests/flows/sub/b")]
    public void Test_annotation_paths_start_at_the_repository_root(string tests, string path)
    {
        var run = RunTest(exit: 1, tests: tests);

        Assert.StartsWith($"::error file={path}%2C1%3A%25.flowtest.json,", run.Out);
        Assert.Equal(2, run.DotnetCalls.Length);
    }

    [Fact]
    public void Test_annotation_values_are_cut_at_300_characters()
    {
        var json = """{"tests":[{"file":"a.flowtest.json","flow":"F","result":"failed","assertions":[{"index":0,"description":"d","passed":false,"expected":"LONG","actual":"a"}]}]}"""
            .Replace("LONG", new string('e', 400));

        var run = RunTest(exit: 1, json: json);

        Assert.Equal($"::error file=flowtests/a.flowtest.json,title=Flow test failed::F: assertion 0, d: expected \"{new string('e', 299)}…, got \"a\"\n", run.Out);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(137)]
    public void Test_with_another_exit_writes_the_fixed_text_and_runs_nothing_else(int exit)
    {
        var run = RunTest(exit: exit);

        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\n", run.Output);
        Assert.Equal($"## Flow tests\n\nThe tests could not run (exit code {exit}). See the job log.\n", File.ReadAllText(run.Summary));
        Assert.Single(run.DotnetCalls);
        Assert.Equal("", run.Out);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Test_without_a_report_file_gives_a_warning_and_exits_0(int exit)
    {
        var run = RunTest(exit: exit, noOut: true);

        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\n", run.Output);
        Assert.StartsWith("::warning::", run.Out);
        Assert.DoesNotContain("::error", run.Out);
    }

    [Theory]
    [InlineData("", "0")] // exit 1 as expected, but no JSON report written
    [InlineData(TestJson, "3")]
    public void Test_with_a_json_run_that_gives_no_report_gives_a_warning_and_exits_0(string json, string jsonExit)
    {
        var run = RunTest(exit: 1, json: json, jsonExit: jsonExit == "0" ? null : jsonExit);

        Assert.Equal(0, run.Code);
        Assert.Equal("exit-code=1\n", run.Output);
        Assert.Equal("## Flow tests\n\nthe report\n", File.ReadAllText(run.Summary));
        Assert.Equal("::warning::the annotations could not be written; the job summary has the report\n", run.Out);
    }

    [Fact]
    public void Test_logs_only_error_lines_of_stderr_so_a_test_file_cannot_add_a_workflow_command()
    {
        var run = RunTest(exit: 3, stderr: "error: a\n::error::y\n  ::add-mask::z\nmore\n");

        Assert.Equal(0, run.Code);
        Assert.Equal("error: a\n", run.Out);
    }

    [Fact]
    public void Test_logs_the_note_lines_of_stderr_too()
    {
        var run = RunTest(exit: 0, stderr: "note: sol/Other/x.xml: a link\n::error::y\n");

        Assert.Equal("note: sol/Other/x.xml: a link\n", run.Out);
    }

    [Fact]
    public void Test_with_a_json_report_that_does_not_parse_gives_a_warning_and_exits_0()
    {
        var run = RunTest(exit: 1, json: "not json");

        Assert.Equal(0, run.Code);
        Assert.Equal("exit-code=1\n", run.Output);
        Assert.Equal("## Flow tests\n\nthe report\n", File.ReadAllText(run.Summary));
        Assert.Equal("::warning::the annotations could not be written; the job summary has the report\n", run.Out);
    }

    // ---- diff.sh ----

    [Fact]
    public void Diff_compares_the_first_parent_with_the_work_tree()
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 1);

        Assert.Equal(0, run.Code);
        var baseTree = Assert.Single(Directory.GetDirectories(run.Temp, "powerops-base.*"));
        Assert.Equal($"diff {baseTree}/sol sol --format markdown --out {run.Comment}", run.DotnetCalls.Single().Replace("powerops.dll ", ""));
        // The worktree holds the first parent (v1); the work tree holds the merge (v2).
        var seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed");
        Assert.Equal(File.ReadAllText(Path.Combine(seed, "v1", "Other", "Solution.xml")), File.ReadAllText(Path.Combine(baseTree, "sol", "Other", "Solution.xml")));
        Assert.Equal(File.ReadAllText(Path.Combine(seed, "v2", "Other", "Solution.xml")), File.ReadAllText(Path.Combine(clone, "sol", "Other", "Solution.xml")));
        Assert.Equal("## PowerOps diff\n\nthe diff\n", File.ReadAllText(run.Comment));
    }

    [Fact]
    public void Diff_without_a_parent_writes_the_fixed_text()
    {
        Tools.Require("bash", "git");
        var repo = Path.Combine(_root, "single");
        Git(_root, "init", "-q", "-b", "main", repo);
        File.WriteAllText(Path.Combine(repo, "a.txt"), "a");
        Commit(repo, "A");

        var run = RunDiff(repo, exit: 0);

        Assert.Equal(0, run.Code);
        Assert.Equal("## PowerOps diff\n\nThe diff of the base and head solution could not run (no base commit, exit code 1). See the job log.\n", File.ReadAllText(run.Comment));
        Assert.Empty(run.DotnetCalls);
    }

    [Fact]
    public void Diff_with_a_failed_fetch_writes_the_fixed_text()
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();
        Git(clone, "remote", "set-url", "origin", "file://" + Path.Combine(_root, "missing"));

        var run = RunDiff(clone, exit: 0);

        Assert.Equal(0, run.Code);
        Assert.StartsWith("## PowerOps diff\n\nThe diff of the base and head solution could not run (fetch, exit code ", File.ReadAllText(run.Comment));
        Assert.Empty(run.DotnetCalls);
    }

    [Fact]
    public void Diff_with_exit_3_writes_the_fixed_text_without_the_tool_text()
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 3, stderr: "secret tool text");

        Assert.Equal(0, run.Code);
        Assert.Equal("## PowerOps diff\n\nThe diff of the base and head solution could not run (diff, exit code 3). See the job log.\n", File.ReadAllText(run.Comment));
        Assert.DoesNotContain("secret", File.ReadAllText(run.Comment));
    }

    [Fact]
    public void Diff_logs_only_error_and_note_lines_of_stderr_so_a_name_cannot_add_a_workflow_command()
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 1, stderr: "note: sol/Other/x.xml: a link\n::error::forged\n  ::add-mask::z\nerror: b\n");

        Assert.Equal(0, run.Code);
        var logged = run.Out.Split('\n').Where(l => l.StartsWith("note:", StringComparison.Ordinal) || l.StartsWith("error:", StringComparison.Ordinal) || l.Contains("::", StringComparison.Ordinal)).ToArray();
        Assert.Equal(["note: sol/Other/x.xml: a link", "error: b"], logged);
    }

    // ---- helpers ----

    private sealed record Result(int Code, string Out, string Folder, string Temp, string Comment, string Summary, string Output, string Posted)
    {
        public string Err { get; init; } = "";

        public string[] GhCalls => ReadLines(Path.Combine(Temp, "gh.log"));

        public string[] DotnetCalls => ReadLines(Path.Combine(Temp, "dotnet.log"));
    }

    private static string[] ReadLines(string path) => File.Exists(path) ? File.ReadAllLines(path) : [];

    // The stub gh logs its arguments and, for a call with --paginate, runs the real --jq filter over the canned list.
    // GH_FAIL_ON makes a call fail when its arguments contain that text; GH_EXIT makes every call fail.
    private const string GhStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/gh.log"
        if [ "${GH_EXIT:-0}" != "0" ]; then echo "gh: failed" >&2; exit "$GH_EXIT"; fi
        if [ -n "${GH_FAIL_ON:-}" ] && [[ "$*" == *"$GH_FAIL_ON"* ]]; then echo "gh: failed" >&2; exit 1; fi
        filter=
        paginate=0
        for ((i = 1; i <= $#; i++)); do
          arg="${!i}"
          if [ "$arg" = "--paginate" ]; then paginate=1; fi
          if [ "$arg" = "--jq" ]; then j=$((i + 1)); filter="${!j}"; fi
          case "$arg" in body=@*) cp "${arg#body=@}" "$STUB_DIR/posted.md";; esac
        done
        if [ "$paginate" = 1 ]; then jq -r "$filter" < "$STUB_DIR/list.json"; fi
        """;

    // The stub dotnet logs its arguments, writes the canned Markdown (or, for --format json, the canned report.json when there is one)
    // to --out, an empty plan to --save and a canned stdout. STUB_NO_OUT=1 writes no --out file; STUB_JSON_EXIT sets the exit code of a
    // --format json call.
    private const string DotnetStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/dotnet.log"
        out=
        save=
        format=
        while [ $# -gt 0 ]; do
          case "$1" in --out) out="$2";; --save) save="$2";; --format) format="$2";; esac
          shift
        done
        if [ -n "$out" ] && [ "${STUB_NO_OUT:-0}" != "1" ]; then
          if [ "$format" = "json" ] && [ -f "$STUB_DIR/report.json" ]; then cp "$STUB_DIR/report.json" "$out"; else cp "$STUB_DIR/markdown.md" "$out"; fi
        fi
        if [ -n "$save" ] && [ "${STUB_SAVE:-0}" = "1" ]; then echo '{}' > "$save"; fi
        if [ -f "$STUB_DIR/stdout.txt" ]; then cat "$STUB_DIR/stdout.txt"; fi
        if [ -f "$STUB_DIR/stderr.txt" ]; then cat "$STUB_DIR/stderr.txt" >&2; fi
        if [ "$format" = "json" ] && [ -n "${STUB_JSON_EXIT:-}" ]; then exit "$STUB_JSON_EXIT"; fi
        exit "${STUB_EXIT:-0}"
        """;

    private Result RunComment(string list, string? markdown = "## PowerOps diff\n\nbody\n", string fork = "false", string ghExit = "0", string ghFailOn = "")
    {
        Tools.Require("bash", "jq");
        var temp = NewDir("comment");
        WriteStub(temp, "gh", GhStub);
        File.WriteAllText(Path.Combine(temp, "list.json"), list);
        var comment = Path.Combine(temp, "comment.md");
        if (markdown is not null)
        {
            File.WriteAllText(comment, markdown);
        }

        var summary = Path.Combine(temp, "summary.md");
        var run = Run("comment.sh", temp, temp, new()
        {
            ["PO_COMMENT"] = comment,
            ["PO_FORK"] = fork,
            ["PO_PR"] = "7",
            ["GITHUB_REPOSITORY"] = "contoso/widgets",
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["GH_TOKEN"] = "fake",
            ["GH_EXIT"] = ghExit,
            ["GH_FAIL_ON"] = ghFailOn,
        });
        return run with { Posted = ReadOrEmpty(Path.Combine(temp, "posted.md")), Summary = summary, Comment = comment };
    }

    private Result RunPlan(int exit, bool save, string? stderr = null, bool debug = false, string? folder = null)
    {
        Tools.Require("bash");
        var temp = NewDir("plan");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "## PowerOps deploy plan\n\nthe plan\n");
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        folder ??= Directory.CreateDirectory(Path.Combine(temp, "deploy")).FullName;
        var comment = Path.Combine(temp, "comment.md");
        var summary = Path.Combine(temp, "summary.md");
        var output = Path.Combine(temp, "output.txt");
        var run = Run("plan.sh", temp, temp, new()
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_FOLDER"] = folder,
            ["PO_TARGET"] = "Contoso Test",
            ["PO_COMMENT"] = comment,
            ["GITHUB_OUTPUT"] = output,
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["RUNNER_DEBUG"] = debug ? "1" : "",
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["STUB_SAVE"] = save ? "1" : "0",
        });
        return run with { Folder = folder, Comment = comment, Summary = summary, Output = ReadOrEmpty(output) };
    }

    private Result RunApply(int exit, string? stderr = null, bool debug = false, string apply = "true", string target = "Contoso Test", string? headRepo = "contoso/widgets", string eventName = "pull_request", string report = "the report\n")
    {
        Tools.Require("bash");
        var temp = NewDir("apply");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "stdout.txt"), report);
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        var summary = Path.Combine(temp, "summary.md");
        var output = Path.Combine(temp, "output.txt");
        var env = new Dictionary<string, string>
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_PLAN_FILE"] = "plan/powerops-plan.json",
            ["PO_TARGET"] = target,
            ["PO_APPLY"] = apply,
            ["PO_EVENT"] = eventName,
            ["GITHUB_REPOSITORY"] = "contoso/widgets",
            ["GITHUB_OUTPUT"] = output,
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["RUNNER_DEBUG"] = debug ? "1" : "",
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        if (headRepo is not null)
        {
            env["PO_HEAD_REPO"] = headRepo; // null leaves it unset, as for an event without a pull request
        }

        var run = Run("apply.sh", temp, temp, env);
        return run with { Summary = summary, Output = ReadOrEmpty(output) };
    }

    private Result RunTest(int exit, string tests = "flowtests", string json = TestJson, bool noOut = false, string? jsonExit = null, string? stderr = null)
    {
        Tools.Require("bash", "jq");
        var temp = NewDir("test");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "## Flow tests\n\nthe report\n");
        File.WriteAllText(Path.Combine(temp, "report.json"), json);
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        var summary = Path.Combine(temp, "summary.md");
        var output = Path.Combine(temp, "output.txt");
        var run = Run("test.sh", temp, temp, new()
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_PATH"] = "sol",
            ["PO_TESTS"] = tests.Replace("WORKSPACE", temp),
            ["GITHUB_OUTPUT"] = output,
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["GITHUB_WORKSPACE"] = temp,
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["STUB_NO_OUT"] = noOut ? "1" : "0",
            ["STUB_JSON_EXIT"] = jsonExit ?? "",
        });
        return run with { Summary = summary, Output = ReadOrEmpty(output) };
    }

    private Result RunDiff(string workDir, int exit, string? stderr = null)
    {
        Tools.Require("bash", "git");
        var temp = NewDir("diff");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "## PowerOps diff\n\nthe diff\n");
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        var comment = Path.Combine(temp, "comment.md");
        var run = Run("diff.sh", temp, workDir, new()
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_PATH"] = "sol",
            ["PO_COMMENT"] = comment,
            ["RUNNER_TEMP"] = temp,
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        return run with { Comment = comment };
    }

    // A repository with base commit A (solution v1), a branch commit B (v2) and a --no-ff merge M on top of A,
    // cloned shallow, as actions/checkout does for a pull_request merge commit.
    private string MakeMergeClone()
    {
        var seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed");
        var repo = Path.Combine(_root, "repo");
        Git(_root, "init", "-q", "-b", "main", repo);
        CopyDirectory(Path.Combine(seed, "v1"), Path.Combine(repo, "sol"));
        Commit(repo, "A");
        Git(repo, "checkout", "-q", "-b", "feature");
        Directory.Delete(Path.Combine(repo, "sol"), recursive: true);
        CopyDirectory(Path.Combine(seed, "v2"), Path.Combine(repo, "sol"));
        Commit(repo, "B");
        Git(repo, "checkout", "-q", "main");
        Git(repo, "merge", "-q", "--no-ff", "-m", "M", "feature");
        Git(repo, "config", "uploadpack.allowAnySHA1InWant", "true");

        var clone = Path.Combine(_root, "clone");
        Git(_root, "clone", "-q", "--depth", "1", "--no-local", "file://" + repo, clone);
        return clone;
    }

    private static void Commit(string repo, string message)
    {
        Git(repo, "add", "-A");
        Git(repo, "commit", "-q", "-m", message);
    }

    private static void Git(string workDir, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = workDir, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "commit.gpgsign=false" }.Concat(args))
        {
            start.ArgumentList.Add(arg);
        }

        var result = Execute(start);
        Assert.True(result.Code == 0, $"git {string.Join(' ', args)}: {result.Out}{result.Err}");
    }

    private static void CopyDirectory(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from))
        {
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.GetDirectories(from))
        {
            CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
        }
    }

    private string NewDir(string name) => Directory.CreateDirectory(Path.Combine(_root, name + "-" + Guid.NewGuid().ToString("N")[..8])).FullName;

    private static void WriteStub(string temp, string name, string body)
    {
        var bin = Directory.CreateDirectory(Path.Combine(temp, "bin")).FullName;
        var path = Path.Combine(bin, name);
        File.WriteAllText(path, body.Replace("\r\n", "\n") + "\n");
        var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
        if (posix)
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static string ReadOrEmpty(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

    private Result Run(string script, string temp, string workDir, Dictionary<string, string> env, string? stdin = null, string[]? args = null)
    {
        var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = workDir, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = stdin is not null };
        start.ArgumentList.Add(Path.Combine(Scripts, script));
        foreach (var arg in args ?? [])
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment["PATH"] = Path.Combine(temp, "bin") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        start.Environment["STUB_DIR"] = temp;
        foreach (var (key, value) in env)
        {
            start.Environment[key] = value;
        }

        var result = Execute(start, stdin);
        return new Result(result.Code, result.Out, "", temp, "", "", "", "") { Err = result.Err };
    }

    private static (int Code, string Out, string Err) Execute(ProcessStartInfo start, string? stdin = null)
    {
        using var process = Process.Start(start)!;
        if (stdin is not null)
        {
            // The input can be larger than the pipe buffer, so write it while the output is read.
            var write = Task.Run(() =>
            {
                process.StandardInput.Write(stdin);
                process.StandardInput.Close();
            });
            var stdoutEarly = process.StandardOutput.ReadToEndAsync();
            var stderrEarly = process.StandardError.ReadToEndAsync();
            write.GetAwaiter().GetResult();
            if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException(start.FileName + " did not finish.");
            }

            return (process.ExitCode, stdoutEarly.GetAwaiter().GetResult(), stderrEarly.GetAwaiter().GetResult());
        }

        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(start.FileName + " did not finish.");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }
}