using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Runs action/install.sh, action/plan.sh, action/apply.sh, action/check-apply.sh, action/diff.sh, action/comment.sh, action/test.sh, action/powerops.sh, action/analyze.sh, action/annotate.sh and action/summary.sh with bash, against stub `gh`, `dotnet` and `curl` programs
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
    public void A_plan_already_in_the_job_summary_is_not_appended_again()
    {
        var run = RunComment(list: "[]", ghExit: "1", planInSummary: true);

        Assert.Equal(0, run.Code);
        Assert.Contains("::warning::", run.Out);
        Assert.False(File.Exists(run.Summary));
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
        Assert.Equal($"deploy plan {run.Folder} --target Contoso Test --save {run.Folder}/powerops-plan.json --format markdown --out {run.Comment} --planned-by action", run.DotnetCalls.Single().Replace("powerops.dll ", ""));
        Assert.True(File.Exists(Path.Combine(run.Temp, "powerops-plan-in-summary")));
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
    public void Plan_logs_powerops_warning_and_note_lines_with_its_error_lines_and_hides_forwarded_text()
    {
        const string Warning = "warning: a.zip: docs/plans/m: security-solution.md is blocked\n";
        const string Note = "note: a.zip: 1 ready report under docs/plans reviewed this solution, each in an input that changed since the review; none was compared with the zip\n";
        var run = RunPlan(exit: 0, save: false, stderr: $"{Warning}{Note}  | tenant 0000 at https://contoso.crm.dynamics.com\n  | note: forwarded\nerror: --target: pac failed\nForwarded: tenant 0000\n");

        Assert.Equal(0, run.Code);
        Assert.Equal($"{Warning}{Note}error: --target: pac failed\n", run.Out);
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

        Assert.Equal("deploy apply plan/powerops-plan.json --target Contoso Test --github-run contoso/widgets run 7 attempt 2 --require-reviews", dry.DotnetCalls.Single().Replace("powerops.dll ", ""));
        Assert.Equal("deploy apply plan/powerops-plan.json --target Contoso Test --github-run contoso/widgets run 7 attempt 2 --require-reviews --apply", real.DotnetCalls.Single().Replace("powerops.dll ", ""));
    }

    [Fact]
    public void Apply_passes_the_run_attempt_and_environment_as_the_receipt()
    {
        var run = RunApply(exit: 0, environment: "test");

        Assert.Equal("deploy apply plan/powerops-plan.json --target Contoso Test --github-run contoso/widgets run 7 attempt 2, environment test --require-reviews --apply", run.DotnetCalls.Single().Replace("powerops.dll ", ""));
    }

    [Fact]
    public void Apply_leaves_out_require_reviews_only_for_the_exact_value_false()
    {
        var off = RunApply(exit: 0, requireReviews: "false");

        Assert.Equal("deploy apply plan/powerops-plan.json --target Contoso Test --github-run contoso/widgets run 7 attempt 2 --apply", off.DotnetCalls.Single().Replace("powerops.dll ", ""));
    }

    [Theory]
    [InlineData("FALSE")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("true")]
    public void Apply_keeps_require_reviews_for_every_other_value_and_when_unset(string? value)
    {
        var run = RunApply(exit: 0, requireReviews: value);

        Assert.Contains(" --github-run contoso/widgets run 7 attempt 2 --require-reviews --apply", run.DotnetCalls.Single());
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
    public void Apply_logs_only_error_and_warning_lines_and_not_forwarded_pac_lines()
    {
        var run = RunApply(exit: 3, stderr: "  | error: x\n  | warning: w\nerror: y\nwarning: a.zip: no review covers it\n  | tenant 0000 at https://contoso.crm.dynamics.com\n");

        Assert.Equal("the report\nerror: y\nwarning: a.zip: no review covers it\n", run.Out);
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

    [Fact]
    public void Apply_refuses_without_a_target()
    {
        var run = RunApply(exit: 0, target: "");

        Assert.Equal(0, run.Code);
        Assert.StartsWith("::error::", run.Out);
        Assert.Contains("target is required", run.Out);
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

    [Theory]
    [InlineData("", "false", "powerops.dll analyze sol dir --fail-on warning --format sarif --out a b.sarif")]
    [InlineData("base line.json", "true", "powerops.dll analyze sol dir --fail-on warning --baseline base line.json --redact --format sarif --out a b.sarif")]
    public void Analyze_builds_the_arguments_from_the_environment_and_passes_the_exit_code(string baseline, string redact, string call)
    {
        Tools.Require("bash");
        var temp = NewDir("analyze");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "");

        var run = Run("analyze.sh", temp, temp, new() { ["PO_DLL"] = "powerops.dll", ["PO_PATH"] = "sol dir", ["PO_BASELINE"] = baseline, ["PO_REDACT"] = redact, ["STUB_EXIT"] = "1" }, stdin: null, args: ["warning", "--format", "sarif", "--out", "a b.sarif"]);

        Assert.Equal(1, run.Code);
        Assert.Equal(call, Assert.Single(run.DotnetCalls));
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
    public void Test_report_over_the_summary_limit_is_cut_with_the_last_line()
    {
        var report = "## Flow tests\n\n" + string.Concat(Enumerable.Range(0, 30_000).Select(i => $"| row {i} | {new string('x', 40)} |\n"));
        Assert.True(report.Length > 1_048_576);

        var run = RunTest(exit: 0, markdown: report);

        Assert.Equal(0, run.Code);
        var written = File.ReadAllBytes(run.Summary);
        Assert.True(written.Length <= 1_048_576, $"{written.Length} bytes");
        Assert.Matches(@"\n_The report was cut to fit the 1 MiB job summary limit: \d+ of \d+ bytes are shown\._\n$", File.ReadAllText(run.Summary));
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

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    public void Diff_adds_redact_only_when_PO_REDACT_is_true(string redact, bool expected)
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 1, redact: redact);

        Assert.Equal(0, run.Code);
        Assert.Equal(expected, run.DotnetCalls.Single().EndsWith(" --redact", StringComparison.Ordinal));
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
    public void Diff_removes_a_plan_marker_left_by_an_earlier_use_of_the_action_in_the_job()
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 1, staleMarker: true);

        Assert.Equal(0, run.Code);
        Assert.False(File.Exists(Path.Combine(run.Temp, "powerops-plan-in-summary")));
    }

    [Fact]
    public void Diff_refuses_a_checkout_that_is_not_the_merge_commit()
    {
        Tools.Require("bash", "git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 1, sha: new string('a', 40));

        Assert.Equal(0, run.Code);
        Assert.Equal("## PowerOps diff\n\nThe diff of the base and head solution could not run (checkout is not the merge commit, exit code 1). See the job log.\n", File.ReadAllText(run.Comment));
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

    // ---- check-apply.sh ----

    private const string RequestToken = "REQUEST-TOKEN-SENTINEL";
    private const string Signature = "SIGNATURE-SENTINEL";
    private const string Refusal = "::error::path must be the folder that holds plan-file, so that the gate analyzes the zips that are imported; nothing was imported\n";

    private static string Base64Url(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // A JWT-shaped token whose payload is the given JSON object plus a filler field, so that its unpadded base64url holds a '-' and a '_' (the two characters that differ from base64).
    private static string Jwt(string payload)
    {
        for (var shift = 0; shift < 6; shift++)
        {
            var encoded = Base64Url(payload.TrimEnd('}') + $",\"x\":\"{new string('a', shift)}>>>???\"}}");
            if (encoded.Contains('-') && encoded.Contains('_'))
            {
                Assert.DoesNotContain('=', encoded);
                return "eyJhbGciOiJSUzI1NiJ9." + encoded + "." + Signature;
            }
        }

        throw new InvalidOperationException("no filler gives both characters");
    }

    private static string TokenResponse(string payload) => JsonSerializer.Serialize(new { value = Jwt(payload) });

    private static void AssertNoSecret(Result run)
    {
        foreach (var text in new[] { run.Out, run.Err })
        {
            Assert.DoesNotContain(RequestToken, text);
            Assert.DoesNotContain(Signature, text);
            Assert.DoesNotContain("eyJ", text);
        }
    }

    [Fact]
    public void Check_passes_an_equal_folder_in_every_spelling_and_for_a_dry_run_asks_for_no_token()
    {
        var runs = new[]
        {
            RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "false", token: false),
            RunCheck(path: null, planFile: "deploy/powerops-plan.json", apply: "false", token: false, pathSuffix: "/"),
            RunCheck(path: "./deploy/", planFile: "deploy/powerops-plan.json", apply: "false", token: false),
            RunCheck(path: ".", planFile: "powerops-plan.json", apply: "false", token: false),
        };

        foreach (var run in runs)
        {
            Assert.Equal(0, run.Code);
            Assert.Equal("", run.Out);
            Assert.Equal("", run.Output);
            Assert.Empty(run.CurlCalls);
        }
    }

    [Theory]
    [InlineData("other", "deploy/powerops-plan.json", "true")]
    [InlineData("missing", "deploy/powerops-plan.json", "true")]
    [InlineData("zip", "deploy/powerops-plan.json", "true")]
    [InlineData("empty", "deploy/powerops-plan.json", "true")]
    [InlineData("parent", "deploy/powerops-plan.json", "true")]
    [InlineData("dot-with-the-plan-elsewhere", "deploy/powerops-plan.json", "false")]
    [InlineData("deploy-with-the-plan-in-the-root", "powerops-plan.json", "false")]
    [InlineData("dotdot-in-path", "deploy/powerops-plan.json", "false")]
    [InlineData("deploy", "deploy/../deploy/powerops-plan.json", "false")]
    [InlineData("deploy", "deploy/nope.json", "false")]
    [InlineData("deploy", "deploy", "false")]
    [InlineData("deploy", "", "false")]
    public void Check_refuses_a_path_that_is_not_the_folder_of_an_existing_plan_file_and_asks_for_no_token(string kind, string planFile, string apply)
    {
        var path = kind switch
        {
            "other" => "other",
            "missing" => "nope",
            "zip" => "deploy/Sol.zip",
            "empty" => "",
            "parent" => ".",
            "dot-with-the-plan-elsewhere" => ".",
            "dotdot-in-path" => "deploy/../deploy",
            _ => "deploy",
        };

        var run = RunCheck(path: path, planFile: planFile, apply: apply);

        Assert.Equal(2, run.Code);
        Assert.Equal(Refusal, run.Out);
        Assert.Empty(run.CurlCalls);
        Assert.Equal("", run.Output);
        AssertNoSecret(run);
    }

    [Fact]
    public void Check_reads_a_backslash_spelling_only_on_windows()
    {
        var windows = RunCheck(path: "deploy", planFile: @"deploy\powerops-plan.json", apply: "false", token: false, os: "Windows_NT");
        var other = RunCheck(path: "deploy", planFile: @"deploy\powerops-plan.json", apply: "false", token: false);
        var windowsPath = RunCheck(path: @".\deploy", planFile: @"deploy\powerops-plan.json", apply: "false", token: false, os: "Windows_NT");
        var windowsDotDot = RunCheck(path: @"deploy\..\deploy", planFile: @"deploy\powerops-plan.json", apply: "false", token: false, os: "Windows_NT");

        Assert.Equal((0, ""), (windows.Code, windows.Out));
        Assert.Equal((0, ""), (windowsPath.Code, windowsPath.Out));
        Assert.Equal((2, Refusal), (other.Code, other.Out));
        Assert.Equal((2, Refusal), (windowsDotDot.Code, windowsDotDot.Out));
    }

    [Fact]
    public void Check_for_apply_true_asks_for_the_token_with_its_own_audience_and_writes_the_environment()
    {
        var run = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "true", response: TokenResponse("""{"environment":"test","sub":"repo:contoso/widgets:environment:test"}"""));

        Assert.Equal(0, run.Code);
        Assert.Equal("environment=test\n", run.Output);
        var call = Assert.Single(run.CurlCalls);
        Assert.Contains("https://token.example.test/token?api-version=2.0&audience=powerops-environment-check", call);
        Assert.Contains($"Authorization: bearer {RequestToken}", call);
        AssertNoSecret(run);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("a_b-c~?>")]
    [InlineData("Test é")]
    public void Check_decodes_payloads_with_each_amount_of_base64_padding(string name)
    {
        var run = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "true", response: TokenResponse($$"""{"environment":"{{name}}"}"""));

        Assert.Equal(0, run.Code);
        Assert.Equal($"environment={name}\n", run.Output);
    }

    [Fact]
    public void The_fixture_payloads_cover_all_three_padding_lengths()
    {
        var lengths = new[] { "a", "ab", "abc" }.Select(n => Jwt($$"""{"environment":"{{n}}"}""").Split('.')[1].Length % 4).Distinct().ToList();

        Assert.Equal(3, lengths.Count);
    }

    [Theory]
    [InlineData("no-variables")]
    [InlineData("curl-fails")]
    [InlineData("empty-response")]
    [InlineData("not-json")]
    [InlineData("no-value")]
    [InlineData("not-a-jwt")]
    [InlineData("no-claim")]
    [InlineData("empty-claim")]
    [InlineData("line-break")]
    [InlineData("carriage-return")]
    [InlineData("zero-width-space")]
    [InlineData("line-separator")]
    [InlineData("number-claim")]
    public void Check_refuses_apply_true_without_a_job_environment_and_writes_no_output(string kind)
    {
        var response = kind switch
        {
            "no-claim" => TokenResponse("""{"sub":"repo:contoso/widgets:ref:refs/heads/main"}"""),
            "empty-claim" => TokenResponse("""{"environment":""}"""),
            "line-break" => TokenResponse("""{"environment":"te\nst"}"""),
            "carriage-return" => TokenResponse("""{"environment":"test\r"}"""),
            "zero-width-space" => TokenResponse("""{"environment":"te\u200bst"}"""),
            "line-separator" => TokenResponse("""{"environment":"te\u2028st"}"""),
            "number-claim" => TokenResponse("""{"environment":5}"""),
            "empty-response" => "",
            "not-json" => "<html>Signature SENTINEL</html>",
            "no-value" => """{"count":1}""",
            "not-a-jwt" => """{"value":"abc"}""",
            _ => TokenResponse("""{"environment":"test"}"""),
        };

        var run = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "true", response: response, token: kind != "no-variables", curlExit: kind == "curl-fails" ? "22" : "0");

        Assert.Equal(2, run.Code);
        Assert.StartsWith("::error::apply: 'true' ", run.Out);
        Assert.Single(run.Out.TrimEnd().Split('\n'));
        Assert.EndsWith("nothing was imported\n", run.Out);
        Assert.Equal("", run.Output);
        Assert.Equal(kind == "no-variables" ? 0 : 1, run.CurlCalls.Length);
        AssertNoSecret(run);
    }

    [Fact]
    public void Check_names_what_is_missing_in_each_refusal()
    {
        var noToken = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "true", token: false);
        var noEnvironment = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "true", response: TokenResponse("""{"sub":"x"}"""));
        var curlFails = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: "true", curlExit: "22");

        Assert.Equal("::error::apply: 'true' needs permissions: id-token: write, in a job that declares a GitHub environment (jobs.<job_id>.environment); nothing was imported\n", noToken.Out);
        Assert.Equal("::error::apply: 'true' runs only in a job that declares a GitHub environment (jobs.<job_id>.environment), with required reviewers; nothing was imported\n", noEnvironment.Out);
        Assert.Equal("::error::apply: 'true' could not get the job's OIDC token to read its environment; nothing was imported\n", curlFails.Out);
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("")]
    public void Check_treats_any_other_apply_value_as_a_dry_run(string apply)
    {
        var run = RunCheck(path: "deploy", planFile: "deploy/powerops-plan.json", apply: apply, token: false);

        Assert.Equal(0, run.Code);
        Assert.Empty(run.CurlCalls);
        Assert.Equal("", run.Output);
    }

    // The stub curl logs its arguments and prints the canned response; CURL_EXIT makes it fail.
    private const string CurlStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/curl.log"
        if [ "${CURL_EXIT:-0}" != "0" ]; then echo "curl: (22) failed REQUEST-TOKEN-SENTINEL" >&2; exit "$CURL_EXIT"; fi
        printf '%s' "$(cat "$STUB_DIR/curl-response.json")"
        """;

    // Runs from a workspace that holds deploy/ (a plan file and a zip), other/ and a plan file in its root. A null path is the absolute path of deploy plus pathSuffix.
    private Result RunCheck(string? path, string planFile, string apply, string response = "", bool token = true, string curlExit = "0", string pathSuffix = "", string os = "")
    {
        Tools.Require("bash", "jq");
        var temp = NewDir("check");
        WriteStub(temp, "curl", CurlStub);
        File.WriteAllText(Path.Combine(temp, "curl-response.json"), response);
        var work = Directory.CreateDirectory(Path.Combine(temp, "work")).FullName;
        Directory.CreateDirectory(Path.Combine(work, "deploy"));
        Directory.CreateDirectory(Path.Combine(work, "other"));
        File.WriteAllText(Path.Combine(work, "powerops-plan.json"), "{}");
        File.WriteAllText(Path.Combine(work, "deploy", "powerops-plan.json"), "{}");
        File.WriteAllText(Path.Combine(work, "deploy", "Sol.zip"), "x");
        var output = Path.Combine(temp, "output.txt");
        var run = Run("check-apply.sh", temp, work, new()
        {
            ["PO_PATH"] = path ?? Path.Combine(work, "deploy") + pathSuffix,
            ["PO_PLAN_FILE"] = planFile,
            ["PO_APPLY"] = apply,
            ["GITHUB_OUTPUT"] = output,
            ["OS"] = os,
            // Set to empty, not left out: a test run inside an Actions job inherits the job's own values.
            ["ACTIONS_ID_TOKEN_REQUEST_URL"] = token ? "https://token.example.test/token?api-version=2.0" : "",
            ["ACTIONS_ID_TOKEN_REQUEST_TOKEN"] = token ? RequestToken : "",
            ["CURL_EXIT"] = curlExit,
        });
        return run with { Output = ReadOrEmpty(output) };
    }

    // ---- install.sh ----

    private const string Release = "1.2.3";

    private static readonly string ReleaseUrl = $"https://github.com/Chrome71/PowerOps/releases/download/PowerOps-v{Release}/PowerOps.{Release}.nupkg";

    private const string SourceDll = "src/PowerOps.Cli/bin/Release/net10.0/powerops.dll";

    private static readonly string Commit40 = new('a', 40);

    private static readonly (string Name, string Text)[] OneDll = [("tools/net10.0/any/powerops.dll", "the dll"), ("tools/net10.0/any/DotnetToolSettings.xml", "<x/>")];

    // The stub curl logs its arguments and copies the canned package to --output; CURL_EXIT makes it fail.
    private const string InstallCurlStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/curl.log"
        if [ "${CURL_EXIT:-0}" != "0" ]; then echo "curl: (22) failed" >&2; exit "$CURL_EXIT"; fi
        while [ $# -gt 0 ]; do
          if [ "$1" = "--output" ]; then cp "$STUB_DIR/package.nupkg" "$2"; fi
          shift
        done
        """;

    // Runs install.sh in a folder named `folderName` (GITHUB_ACTION_PATH unless `actionPath` is given). The canned package is a zip of `files`;
    // a sha of "match" is its real SHA-256.
    private Result RunInstall(string release, string sha, string folderName = "action", string? actionPath = null, string curlExit = "0", params (string Name, string Text)[] files)
    {
        Tools.Require("bash", "sha256sum", "unzip");
        var temp = NewDir("install");
        WriteStub(temp, "dotnet", DotnetStub);
        WriteStub(temp, "curl", InstallCurlStub);
        var package = Path.Combine(temp, "package.nupkg");
        using (var zip = ZipFile.Open(package, ZipArchiveMode.Create))
        {
            foreach (var (name, text) in files)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(text);
            }
        }

        var folder = Directory.CreateDirectory(Path.Combine(temp, folderName)).FullName;
        var output = Path.Combine(temp, "output.txt");
        var run = Run("install.sh", temp, folder, new()
        {
            ["PO_RELEASE"] = release,
            ["PO_RELEASE_SHA256"] = sha == "match" ? Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(package))) : sha,
            ["GITHUB_ACTION_PATH"] = actionPath ?? folder,
            ["RUNNER_TEMP"] = Path.Combine(temp, "runner-temp"),
            ["GITHUB_OUTPUT"] = output,
            ["CURL_EXIT"] = curlExit,
        });
        return run with { Folder = folder, Output = ReadOrEmpty(output) };
    }

    [Fact]
    public void Install_without_a_pin_builds_from_source_in_locked_mode_and_downloads_nothing()
    {
        var run = RunInstall("", "");

        Assert.Equal(0, run.Code);
        Assert.Equal(["build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true"], run.DotnetCalls);
        Assert.Empty(run.CurlCalls);
        Assert.Equal($"dll={run.Folder}/{SourceDll}\n", run.Output);
    }

    [Theory]
    [InlineData("a", true)] // the folder is named after the commit it was pinned to
    [InlineData("b", true)]
    [InlineData("main", false)]
    [InlineData("PowerOps", false)]
    [InlineData("v1.2.3", false)]
    [InlineData("39", false)] // not 40 hex digits
    [InlineData("41", false)]
    [InlineData("upper", false)]
    public void Install_passes_the_action_folders_commit_to_the_source_build_only_when_it_is_named_with_40_lowercase_hex_digits(string kind, bool revision)
    {
        var name = kind switch
        {
            "a" => Commit40,
            "b" => new string('0', 20) + new string('f', 20),
            "39" => Commit40[1..],
            "41" => Commit40 + "a",
            "upper" => Commit40.ToUpperInvariant(),
            _ => kind,
        };

        var run = RunInstall("", "", name);

        Assert.Equal(0, run.Code);
        Assert.Equal(revision ? $"build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true -p:SourceRevisionId={name}" : "build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true", Assert.Single(run.DotnetCalls));
        Assert.Empty(run.CurlCalls);
    }

    [Theory]
    [InlineData(@"D:\a\_actions\Chrome71\PowerOps\", true)] // a Windows runner's path, with the folder name after a backslash
    [InlineData(@"/home/runner/work/_actions/Chrome71/PowerOps/", true)]
    [InlineData(@"D:\a\PowerOps\PowerOps", false)] // uses: ./ is a checkout
    public void Install_reads_the_last_segment_of_the_action_path_split_on_slash_or_backslash(string prefix, bool revision)
    {
        var path = revision ? prefix + Commit40 : prefix;

        var run = RunInstall("", "", actionPath: path);

        Assert.Equal(0, run.Code);
        Assert.Equal(revision ? $"build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true -p:SourceRevisionId={Commit40}" : "build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true", Assert.Single(run.DotnetCalls));
        Assert.Equal($"dll={path}/{SourceDll}\n", run.Output);
    }

    [Fact]
    public void Install_with_a_pin_downloads_the_release_checks_its_sha256_and_unpacks_it()
    {
        var run = RunInstall(Release, "match", files: OneDll);

        Assert.Equal(0, run.Code);
        Assert.Empty(run.DotnetCalls);
        var call = Assert.Single(run.CurlCalls);
        Assert.Contains("--fail", call);
        Assert.Contains("--proto =https --proto-redir =https", call);
        Assert.EndsWith(" " + ReleaseUrl, call);
        var folder = Path.Combine(run.Temp, "runner-temp", "powerops-" + Release);
        Assert.Equal($"dll={folder}/package/tools/net10.0/any/powerops.dll\n", run.Output);
        Assert.Equal("the dll", File.ReadAllText(Path.Combine(folder, "package", "tools", "net10.0", "any", "powerops.dll")));
    }

    [Fact]
    public void Install_refuses_a_package_with_another_sha256_and_names_both_hashes_before_unpacking_it()
    {
        var other = new string('a', 64);

        var run = RunInstall(Release, other, files: OneDll);

        Assert.Equal(1, run.Code);
        Assert.Equal("", run.Output);
        Assert.StartsWith("::error::", run.Out);
        Assert.Contains(other, run.Out);
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(run.Temp, "package.nupkg"))));
        Assert.Contains(actual, run.Out);
        Assert.Single(run.CurlCalls);
        Assert.False(Directory.Exists(Path.Combine(run.Temp, "runner-temp", "powerops-" + Release, "package")));
    }

    [Theory]
    [InlineData("download-fails")]
    [InlineData("no-dll")]
    [InlineData("no-tools-folder")]
    [InlineData("two-dlls")]
    public void Install_refuses_a_failed_download_or_a_package_without_exactly_one_dll_and_writes_no_output(string kind)
    {
        var run = kind switch
        {
            "download-fails" => RunInstall(Release, "match", curlExit: "22", files: OneDll),
            "no-dll" => RunInstall(Release, "match", files: [("tools/net10.0/any/other.dll", "x")]),
            "no-tools-folder" => RunInstall(Release, "match", files: [("lib/powerops.dll", "x")]),
            _ => RunInstall(Release, "match", files: [("tools/net10.0/any/powerops.dll", "a"), ("tools/net9.0/any/powerops.dll", "b")]),
        };

        Assert.Equal(1, run.Code);
        Assert.Equal("", run.Output);
        Assert.StartsWith("::error::", run.Out);
        Assert.Empty(run.DotnetCalls);
        Assert.Single(run.CurlCalls);
    }

    [Theory]
    [InlineData("1.2.3", "")] // half a pin
    [InlineData("", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("1.2", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // a version that is not major.minor.patch
    [InlineData("v1.2.3", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("1.2.3", "aaaa")] // a hash that is not 64 lowercase hex digits
    [InlineData("1.2.3", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("1.2.3", "gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void Install_refuses_a_malformed_pin_before_any_download(string release, string sha)
    {
        var run = RunInstall(release, sha, files: OneDll);

        Assert.Equal(1, run.Code);
        Assert.Equal("", run.Output);
        Assert.StartsWith("::error::", run.Out);
        Assert.Empty(run.CurlCalls);
        Assert.Empty(run.DotnetCalls);
    }

    // ---- helpers ----

    private sealed record Result(int Code, string Out, string Folder, string Temp, string Comment, string Summary, string Output, string Posted)
    {
        public string Err { get; init; } = "";

        public string[] GhCalls => ReadLines(Path.Combine(Temp, "gh.log"));

        public string[] DotnetCalls => ReadLines(Path.Combine(Temp, "dotnet.log"));

        public string[] CurlCalls => ReadLines(Path.Combine(Temp, "curl.log"));
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

    private Result RunComment(string list, string? markdown = "## PowerOps diff\n\nbody\n", string fork = "false", string ghExit = "0", string ghFailOn = "", bool planInSummary = false)
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
        if (planInSummary)
        {
            File.WriteAllText(Path.Combine(temp, "powerops-plan-in-summary"), "");
        }

        var run = Run("comment.sh", temp, temp, new()
        {
            ["RUNNER_TEMP"] = temp,
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
            ["RUNNER_TEMP"] = temp,
            ["GITHUB_OUTPUT"] = output,
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["RUNNER_DEBUG"] = debug ? "1" : "",
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["STUB_SAVE"] = save ? "1" : "0",
        });
        return run with { Folder = folder, Comment = comment, Summary = summary, Output = ReadOrEmpty(output) };
    }

    private Result RunApply(int exit, string? stderr = null, bool debug = false, string apply = "true", string target = "Contoso Test", string? headRepo = "contoso/widgets", string eventName = "pull_request", string report = "the report\n", string environment = "", string? requireReviews = null)
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
            ["PO_ENVIRONMENT"] = environment,
            ["GITHUB_RUN_ID"] = "7",
            ["GITHUB_RUN_ATTEMPT"] = "2",
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

        if (requireReviews is not null)
        {
            env["PO_REQUIRE_REVIEWS"] = requireReviews; // null leaves it unset
        }

        var run = Run("apply.sh", temp, temp, env);
        return run with { Summary = summary, Output = ReadOrEmpty(output) };
    }

    private Result RunTest(int exit, string tests = "flowtests", string json = TestJson, bool noOut = false, string? jsonExit = null, string? stderr = null, string markdown = "## Flow tests\n\nthe report\n")
    {
        Tools.Require("bash", "jq");
        var temp = NewDir("test");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), markdown);
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

    private Result RunDiff(string workDir, int exit, string? stderr = null, string? redact = null, string? sha = null, bool staleMarker = false)
    {
        Tools.Require("bash", "git");
        var temp = NewDir("diff");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "## PowerOps diff\n\nthe diff\n");
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        if (staleMarker)
        {
            File.WriteAllText(Path.Combine(temp, "powerops-plan-in-summary"), "");
        }

        var comment = Path.Combine(temp, "comment.md");
        var run = Run("diff.sh", temp, workDir, new()
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_PATH"] = "sol",
            ["PO_COMMENT"] = comment,
            ["RUNNER_TEMP"] = temp,
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["PO_REDACT"] = redact ?? "",
            ["GITHUB_SHA"] = sha ?? Head(workDir),
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

    private static string Head(string workDir)
    {
        var start = new ProcessStartInfo("git", ["rev-parse", "HEAD"]) { WorkingDirectory = workDir, RedirectStandardOutput = true, RedirectStandardError = true };
        return Execute(start).Out.Trim();
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