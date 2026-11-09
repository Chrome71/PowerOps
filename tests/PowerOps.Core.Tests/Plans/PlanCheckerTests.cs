using System.Globalization;

using PowerOps.Core.Plans;
using PowerOps.Core.Tests.Readers;

namespace PowerOps.Core.Tests.Plans;

public class PlanCheckerTests : IDisposable
{
    private readonly TempFolder _t = new();

    public void Dispose() => _t.Dispose();

    private string PlanFile => Path.Combine(_t.Path, "docs", "plans", "demo", "plan.md");

    private static string Plan(string status = "draft", string review = "pending", string body = "# Plan\n", string? hash = null, string? date = null) =>
        $"---\nstatus: {status}\nreview: {review}\nsigned-off: {date ?? "null"}\nsigned-off-hash: {hash ?? "null"}\nenvironment: null\n---\n{body}";

    private TempFolder Write(string text) => _t.Write("docs/plans/demo/plan.md", text);

    private PlanReport Check() => PlanChecker.Check(PlanFile, _t.Path);

    private SignOffResult SignOff(string today = "2026-10-19") => PlanSignOff.Run(PlanFile, _t.Path, DateOnly.Parse(today, CultureInfo.InvariantCulture));

    [Fact]
    public void Draft_and_approved_plans_pass_and_a_repo_without_plans_has_none()
    {
        Assert.Empty(PlanChecker.Discover(_t.Path));

        Write(Plan("draft"));
        Assert.Empty(Check().Violations);
        Write(Plan("approved", "done"));
        Assert.Empty(Check().Violations);
    }

    [Fact]
    public void Discovery_finds_plans_in_order_a_plan_folder_and_a_plan_file()
    {
        _t.Write("docs/plans/b/plan.md", Plan()).Write("docs/plans/a/plan.md", Plan()).Write("docs/plans/c/notes.md", "x");

        Assert.Equal(
            [Path.Combine(_t.Path, "docs", "plans", "a", "plan.md"), Path.Combine(_t.Path, "docs", "plans", "b", "plan.md")],
            PlanChecker.Discover(_t.Path));
        Assert.Single(PlanChecker.Discover(Path.Combine(_t.Path, "docs/plans/a")));
        Assert.Single(PlanChecker.Discover(Path.Combine(_t.Path, "docs/plans/a/plan.md")));
    }

    [Fact]
    public void A_root_plan_md_does_not_hide_the_plans_under_docs_plans()
    {
        _t.Write("plan.md", Plan()).Write("docs/plans/a/plan.md", Plan("approved", "skipped"));
        var a = Path.Combine(_t.Path, "docs", "plans", "a");
        PlanSignOff.Run(a, _t.Path, DateOnly.Parse("2026-10-19", CultureInfo.InvariantCulture));

        Assert.Equal([Path.Combine(a, "plan.md")], PlanChecker.Discover(_t.Path));
        Assert.Equal([Path.Combine(a, "plan.md")], PlanChecker.Discover(a + Path.DirectorySeparatorChar));
        Assert.Equal([Path.Combine(_t.Path, "plan.md")], PlanChecker.Discover(Path.Combine(_t.Path, "plan.md")));
        Assert.Equal("signed-off", PlanChecker.Check(PlanChecker.Discover(_t.Path)[0], _t.Path).Status);
    }

    [Fact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public void An_unreadable_plan_is_its_own_violation_and_the_other_plans_are_still_checked()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix modes.");
        _t.Write("docs/plans/a/plan.md", Plan()).Write("docs/plans/b/plan.md", Plan()).Write("docs/plans/b/notes.md", "x");
        var locked = new[] { Path.Combine(_t.Path, "docs", "plans", "a", "plan.md"), Path.Combine(_t.Path, "docs", "plans", "b", "notes.md") };
        foreach (var file in locked)
        {
            File.SetUnixFileMode(file, UnixFileMode.None);
        }

        try
        {
            Assert.SkipWhen(CanRead(locked[0]), "This user can read the file anyway.");

            var reports = PlanChecker.Discover(_t.Path).Select(f => PlanChecker.Check(f, _t.Path)).ToList();

            Assert.Equal(["docs/plans/a", "docs/plans/b"], reports.Select(r => r.Path));
            Assert.Equal(["docs/plans/a/plan.md: could not be read"], reports[0].Violations);
            Assert.False(reports[0].Malformed);
            Assert.Empty(reports[1].Violations);
            Assert.Equal(["docs/plans/b/notes.md: could not be read, not read"], reports[1].Warnings);
        }
        finally
        {
            foreach (var file in locked)
            {
                File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
    }

    private static bool CanRead(string file)
    {
        try
        {
            File.OpenRead(file).Dispose();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    [Fact]
    public void Malformed_header_is_one_violation_and_still_scans_for_tenant_data()
    {
        Write("---\nstatus: nope\nreview: done\n---\nsee https://contoso.example/x\n");

        var r = Check();

        Assert.True(r.Malformed);
        Assert.Null(r.Status);
        Assert.Equal(2, r.Violations.Count);
        Assert.StartsWith("malformed header: status 'nope' is not one of", r.Violations[0]);
        Assert.Contains("looks like a URL", r.Violations[1]);
    }

    [Fact]
    public void Sign_off_then_status_is_clean_and_reports_the_header()
    {
        Write(Plan("approved", "skipped", "# Plan\n\n## Open questions\n- one\n- two\n"));

        var signed = SignOff();
        var r = Check();

        Assert.Equal(0, signed.ExitCode);
        Assert.Empty(r.Violations);
        Assert.Equal("signed-off", r.Status);
        Assert.Equal("2026-10-19", r.SignedOff);
        Assert.Equal(2, r.OpenQuestions);
        Assert.Equal("docs/plans/demo", r.Path);
    }

    [Fact]
    public void Editing_the_body_after_sign_off_is_a_violation_but_editing_the_header_is_not()
    {
        Write(Plan("approved", "skipped"));
        SignOff();

        File.WriteAllText(PlanFile, File.ReadAllText(PlanFile).Replace("environment: null", "environment: Contoso Dev"));
        Assert.Empty(Check().Violations);

        File.AppendAllText(PlanFile, "one more line\n");
        Assert.Contains(Check().Violations, v => v.StartsWith("changed since sign-off", StringComparison.Ordinal));
    }

    [Fact]
    public void A_hand_written_sign_off_is_a_violation()
    {
        Write(Plan("signed-off", "skipped"));

        Assert.Equal(["signed off without the tool: run `powerops plan sign-off`"], Check().Violations);
    }

    [Fact]
    public void Review_state_is_checked_once_signed_off()
    {
        var hash = PlanDocument.Parse(Plan()).BodyHash;
        Write(Plan("implementing", "pending", hash: hash, date: "2026-10-19"));
        Assert.Equal(["review is pending: run powerops-review-plan, or set review: skipped before signing off"], Check().Violations);

        Write(Plan("done", "done", hash: hash, date: "2026-10-19"));
        Assert.Equal(["review: done but review.md is missing"], Check().Violations);

        _t.Write("docs/plans/demo/review.md", $"plan-hash: {hash}\n");
        Assert.Empty(Check().Violations);

        Write(Plan("done", "skipped", hash: hash, date: "2026-10-19"));
        Assert.Empty(Check().Violations);
    }

    [Theory]
    [InlineData("see https://contoso.example/x", "URL", "contoso.example")]
    [InlineData("id 00000000-0000-0000-0000-000000000000 here", "GUID", "00000000-0000")]
    [InlineData("ask admin@contoso.example", "email", "admin@contoso")]
    public void Tenant_data_is_reported_with_file_and_line_but_never_the_value(string line, string kind, string value)
    {
        Write(Plan());
        _t.Write("docs/plans/demo/mockups/page.html", $"<p>ok</p>\n{line}\n");

        var violation = Assert.Single(Check().Violations);

        Assert.Equal($"docs/plans/demo/mockups/page.html:2: looks like a {kind}; remove it, or add <!-- plan-status: allow --> to the line", violation);
        Assert.DoesNotContain(value, violation);
    }

    [Fact]
    public void An_allow_comment_suppresses_the_match()
    {
        Write(Plan(body: "# Plan\nsee https://learn.microsoft.com/power-platform <!-- plan-status: allow -->\n"));

        Assert.Empty(Check().Violations);
    }

    [Fact]
    public void A_stale_or_hashless_review_is_only_a_warning()
    {
        Write(Plan("approved", "done"));
        _t.Write("docs/plans/demo/review.md", $"plan-hash: {new string('a', 64)}\n");
        var stale = Check();
        _t.Write("docs/plans/demo/review.md", "findings only\n");
        var hashless = Check();

        Assert.Empty(stale.Violations);
        Assert.Equal(["review.md was written for a different version of the plan (stale review)"], stale.Warnings);
        Assert.Equal(["review.md has no plan-hash line"], hashless.Warnings);
    }

    private static readonly string Hash = PlanDocument.Parse(Plan()).BodyHash!;

    private static readonly string Other = new('a', 64);

    private TempFolder Report(string name, string text) => _t.Write($"docs/plans/demo/{name}", text);

    private static (string File, string? State, bool? Stale) Row(PlanReportFile r) => (r.File, r.State, r.Stale);

    [Fact]
    public void No_reports_is_an_empty_list_and_present_ones_come_in_a_fixed_order()
    {
        Write(Plan());
        Assert.Empty(Check().Reports);

        Report("verify.md", "state: ready\n");
        Report("review.md", "findings\n");
        Report("security-solution.md", "state: blocked\n");

        Assert.Equal(["review.md", "security-solution.md", "verify.md"], Check().Reports.Select(r => r.File));
    }

    [Fact]
    public void A_review_without_a_state_line_has_a_null_state_and_its_stale_comes_from_the_existing_check()
    {
        Write(Plan());
        Report("review.md", $"plan-hash: {Hash}\nmodel: unknown\nreviewer: x\n");
        var fresh = Check();
        Report("review.md", $"plan-hash: {Other}\nmodel: unknown\nreviewer: x\n");
        var stale = Check();

        Assert.Equal(("review.md", null, false), Row(Assert.Single(fresh.Reports)));
        Assert.Empty(fresh.Warnings);
        Assert.Equal(("review.md", null, true), Row(Assert.Single(stale.Reports)));
        Assert.Equal(["review.md was written for a different version of the plan (stale review)"], stale.Warnings);
        Assert.Empty(stale.Violations);
    }

    [Fact]
    public void A_review_plan_hash_after_the_sixth_line_still_counts_but_a_verify_one_does_not()
    {
        Write(Plan());
        Report("review.md", $"a\nb\nc\nd\ne\nf\ng\nplan-hash: {Other}\n");
        Report("verify.md", $"state: ready\nb\nc\nd\ne\nf\nplan-hash: {Other}\n");

        var reports = Check();

        Assert.Equal([("review.md", null, true), ("verify.md", "ready", null)], reports.Reports.Select(Row));
        Assert.Equal(["review.md was written for a different version of the plan (stale review)"], reports.Warnings);
    }

    [Fact]
    public void A_report_reads_state_and_plan_hash_from_its_first_six_lines_only()
    {
        Write(Plan());
        Report("security-solution.md", $"a\nb\nc\nd\nplan-hash: {Hash}\nstate: ready\n");
        Report("verify.md", "a\nb\nc\nd\ne\nf\nstate: ready\n");

        var reports = Check();

        Assert.Equal([("security-solution.md", "ready", false), ("verify.md", null, null)], reports.Reports.Select(Row));
        Assert.Empty(reports.Warnings);
    }

    private static readonly string SolutionHash = new string('0', 63) + "f";

    private static string Six(string input = "input: solutions/demo", string? hash = null) =>
        $"plan-hash: {Hash}\nmodel: m\nfresh-context: yes\nstate: ready\n{input}\nsolution-hash: {hash ?? SolutionHash}\n";

    [Fact]
    public void A_six_line_report_gives_its_input_and_solution_hash_and_review_md_gives_none()
    {
        Write(Plan());
        Report("review.md", $"plan-hash: {Hash}\ninput: solutions/demo\nsolution-hash: {SolutionHash}\n");
        Report("security-solution.md", Six());
        Report("verify.md", Six("input: out/Demo.zip"));

        var reports = Check();

        Assert.Equal([("review.md", null, null), ("security-solution.md", "solutions/demo", SolutionHash), ("verify.md", "out/Demo.zip", SolutionHash)],
            reports.Reports.Select(r => (r.File, r.Input, r.SolutionHash)));
        Assert.Empty(reports.Warnings);
    }

    [Fact]
    public void A_legacy_four_line_report_reads_unknown_and_its_input_on_line_five_or_six_is_still_read()
    {
        Write(Plan());
        Report("security-solution.md", $"plan-hash: {Hash}\nmodel: m\nfresh-context: yes\nstate: ready\ninput: solutions/demo\n");
        Report("verify.md", $"plan-hash: {Hash}\nmodel: m\nfresh-context: yes\nstate: ready\n\ninput: a/b\n");

        var reports = Check();

        Assert.Equal([("security-solution.md", "solutions/demo", "unknown"), ("verify.md", "a/b", "unknown")], reports.Reports.Select(r => (r.File, r.Input, r.SolutionHash)));
        Assert.Empty(reports.Warnings);
    }

    [Fact]
    public void A_report_with_neither_line_has_a_null_input_and_unknown_hash_and_no_warning()
    {
        Write(Plan());
        Report("verify.md", "state: ready\n");

        var report = Assert.Single(Check().Reports);

        Assert.Null(report.Input);
        Assert.Equal("unknown", report.SolutionHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABCDEF")]
    [InlineData("0123")]
    [InlineData("sha256:0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000f")]
    [InlineData("000000000000000000000000000000000000000000000000000000000000000F")]
    [InlineData("000000000000000000000000000000000000000000000000000000000000000g")]
    public void A_malformed_solution_hash_reads_unknown_without_a_warning(string hash)
    {
        Write(Plan());
        Report("verify.md", $"state: ready\nsolution-hash: {hash}\n");

        var report = Assert.Single(Check().Reports);

        Assert.Equal("unknown", report.SolutionHash);
        Assert.Empty(Check().Warnings);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("solutions/demo")]
    [InlineData("solutions/demo/")]
    [InlineData("out/Demo_1.0.0.0.zip")]
    [InlineData("a b/c")]
    [InlineData(".hidden/x")]
    public void A_relative_path_is_a_locator(string input)
    {
        Write(Plan());
        Report("verify.md", $"state: ready\ninput: {input}\n");

        var reports = Check();

        Assert.Equal(input.TrimEnd('/'), Assert.Single(reports.Reports).Input);
        Assert.Empty(reports.Warnings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/etc/x")]
    [InlineData("C:/x")]
    [InlineData("C:x")]
    [InlineData("a\\b")]
    [InlineData("a//b")]
    [InlineData("a//")]
    [InlineData("./a")]
    [InlineData("a/./b")]
    [InlineData("../a")]
    [InlineData("a/..")]
    [InlineData("a\tb")]
    public void A_value_that_is_not_a_locator_reads_null_and_warns_once(string input)
    {
        Write(Plan());
        Report("security-solution.md", $"state: ready\ninput: {input}\n");

        var reports = Check();

        Assert.Null(Assert.Single(reports.Reports).Input);
        Assert.Equal(["security-solution.md has an input: line that is not a relative path"], reports.Warnings);
    }

    [Fact]
    public void A_locator_is_at_most_260_characters_after_one_trailing_slash_is_dropped()
    {
        Write(Plan());
        Report("verify.md", $"state: ready\ninput: {new string('a', 260)}/\n");
        var ok = Check();
        Report("verify.md", $"state: ready\ninput: {new string('a', 261)}\n");
        var tooLong = Check();

        Assert.Equal(new string('a', 260), Assert.Single(ok.Reports).Input);
        Assert.Empty(ok.Warnings);
        Assert.Null(Assert.Single(tooLong.Reports).Input);
        Assert.Single(tooLong.Warnings);
    }

    [Fact]
    public void Review_md_has_no_input_or_solution_hash_even_with_the_lines_and_never_warns_about_them()
    {
        Write(Plan());
        Report("review.md", $"plan-hash: {Hash}\ninput: /bad\nsolution-hash: {SolutionHash}\n");

        var report = Assert.Single(Check().Reports);

        Assert.Null(report.Input);
        Assert.Null(report.SolutionHash);
        Assert.Empty(Check().Warnings);
    }

    [Fact]
    public void Keys_and_values_compare_by_exact_case()
    {
        Write(Plan());
        Report("security-solution.md", "State: ready\n");
        Report("verify.md", "state: Ready\n");

        var reports = Check();

        Assert.Equal([("security-solution.md", null, null), ("verify.md", "Ready", null)], reports.Reports.Select(Row));
        Assert.Equal(["verify.md has an unknown state 'Ready' (expected ready, blocked, failed or dropped)"], reports.Warnings);
    }

    [Fact]
    public void Crlf_and_padded_lines_read_fine()
    {
        Write(Plan());
        Report("verify.md", $"  plan-hash:   {Hash}  \r\n\tstate:  failed \r\nmore\r\n");

        var reports = Check();

        Assert.Equal(("verify.md", "failed", false), Row(Assert.Single(reports.Reports)));
        Assert.Empty(reports.Warnings);
    }

    [Theory]
    [InlineData("ready")]
    [InlineData("blocked")]
    [InlineData("failed")]
    [InlineData("dropped")]
    public void The_four_known_states_do_not_warn(string state)
    {
        Write(Plan());
        Report("security-solution.md", $"state: {state}\n");

        var reports = Check();

        Assert.Equal(state, Assert.Single(reports.Reports).State);
        Assert.Empty(reports.Warnings);
    }

    [Fact]
    public void An_unknown_state_is_kept_and_warns_but_is_not_a_violation()
    {
        Write(Plan("approved", "skipped"));
        Report("security-solution.md", "state: mostly fine\n");

        var reports = Check();

        Assert.Equal("mostly fine", Assert.Single(reports.Reports).State);
        Assert.Equal(["security-solution.md has an unknown state 'mostly fine' (expected ready, blocked, failed or dropped)"], reports.Warnings);
        Assert.Empty(reports.Violations);
    }

    [Fact]
    public void A_stale_verify_or_security_report_warns_without_a_violation()
    {
        Write(Plan());
        Report("security-solution.md", $"state: ready\nplan-hash: {Other}\n");
        Report("verify.md", $"plan-hash: {Other}\nstate: ready\n");

        var reports = Check();

        Assert.Equal([("security-solution.md", "ready", true), ("verify.md", "ready", true)], reports.Reports.Select(Row));
        Assert.Equal(
            ["security-solution.md was written for a different version of the plan (stale report)", "verify.md was written for a different version of the plan (stale report)"],
            reports.Warnings);
        Assert.Empty(reports.Violations);
    }

    [Fact]
    public void A_report_without_a_plan_hash_is_not_stale_and_does_not_warn()
    {
        Write(Plan());
        Report("verify.md", "state: ready\n");

        var reports = Check();

        Assert.Equal(("verify.md", "ready", (bool?)null), Row(Assert.Single(reports.Reports)));
        Assert.Empty(reports.Warnings);
    }

    [Fact]
    public void A_plan_without_a_body_hash_gives_a_null_stale_for_every_report()
    {
        Write("no header here\n");
        Report("review.md", $"plan-hash: {Other}\n");
        Report("verify.md", $"plan-hash: {Other}\nstate: ready\n");

        var reports = Check();

        Assert.Null(reports.BodyHash);
        Assert.Equal([("review.md", null, null), ("verify.md", "ready", null)], reports.Reports.Select(Row));
        Assert.DoesNotContain(reports.Warnings, w => w.Contains("stale", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("review.md", "link")]
    [InlineData("verify.md", "link")]
    [InlineData("review.md", "binary")]
    [InlineData("security-solution.md", "binary")]
    [InlineData("verify.md", "large")]
    [InlineData("review.md", "large")]
    public void A_link_a_binary_or_a_large_report_shows_null_for_both_with_one_warning(string name, string kind)
    {
        Assert.SkipWhen(kind == "link" && OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        Write(Plan());
        var file = Path.Combine(_t.Path, "docs", "plans", "demo", name);
        var content = $"state: ready\nplan-hash: {Other}\n";
        switch (kind)
        {
            case "link":
                var target = Path.Combine(_t.Path, "target.txt");
                File.WriteAllText(target, content);
                File.CreateSymbolicLink(file, target);
                break;
            case "binary":
                File.WriteAllBytes(file, [.. System.Text.Encoding.UTF8.GetBytes(content), 0, 0, 0]);
                break;
            default:
                File.WriteAllText(file, content + new string(' ', (int)PlanChecker.MaxFileBytes));
                break;
        }

        var reports = Check();

        Assert.Equal((name, null, null), Row(Assert.Single(reports.Reports)));
        Assert.Single(reports.Warnings);
        Assert.StartsWith($"docs/plans/demo/{name}: ", reports.Warnings[0]);
    }

    [Fact]
    public void A_matching_review_hash_has_no_warning()
    {
        Write(Plan("approved", "done"));
        _t.Write("docs/plans/demo/review.md", $"plan-hash: {PlanDocument.Parse(Plan()).BodyHash}\nmodel: unknown\n");

        Assert.Empty(Check().Warnings);
    }

    [Theory]
    [InlineData("draft", 1, "status is draft; sign-off needs status: approved")]
    [InlineData("approved", 1, "review is pending")]
    public void Sign_off_refuses_and_writes_nothing(string status, int code, string message)
    {
        var text = Plan(status);
        Write(text);

        var r = SignOff();

        Assert.Equal(code, r.ExitCode);
        Assert.Contains(r.Err, e => e.StartsWith("error: ", StringComparison.Ordinal) && e.Contains(message));
        Assert.Equal(text, File.ReadAllText(PlanFile));
    }

    [Fact]
    public void Sign_off_refuses_review_done_without_review_md_and_tenant_data()
    {
        var text = Plan("approved", "done", "# Plan\nmail a@contoso.example\n");
        Write(text);

        var r = SignOff();

        Assert.Equal(1, r.ExitCode);
        Assert.Contains("error: review: done but review.md is missing", r.Err);
        Assert.Contains("error: remove tenant data before signing off", r.Err);
        Assert.DoesNotContain(r.Err, e => e.Contains("a@contoso"));
        Assert.Equal(text, File.ReadAllText(PlanFile));
    }

    [Fact]
    public void Dates_are_written_and_read_with_the_invariant_culture()
    {
        var saved = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("th-TH");
        try
        {
            Write(Plan("approved", "skipped"));

            Assert.Equal(0, SignOff().ExitCode);

            Assert.Contains("signed-off: 2026-10-19", File.ReadAllText(PlanFile));
            Assert.Equal("2026-10-19", Check().SignedOff);
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public void Added_header_lines_use_the_closing_fence_line_ending_and_the_body_is_untouched()
    {
        var body = "# Plan\r\nsecond line\n";
        Write($"---\nstatus: approved\nreview: skipped\n---\n{body}");

        Assert.Equal(0, SignOff().ExitCode);

        var text = File.ReadAllText(PlanFile);
        var header = text[..(text.IndexOf("---\n", 4, StringComparison.Ordinal) + 4)];
        Assert.DoesNotContain('\r', header);
        Assert.Contains("signed-off-hash: ", header);
        Assert.EndsWith(body, text);
    }

    [Fact]
    public void Sign_off_exit_2_for_a_missing_plan_or_a_malformed_header()
    {
        Assert.Equal(2, SignOff().ExitCode);

        Write("---\nstatus: nope\nreview: done\n---\n");
        var r = SignOff();
        Assert.Equal(2, r.ExitCode);
        Assert.StartsWith("error: malformed header: status 'nope'", r.Err[0]);
    }

    [Fact]
    public void Sign_off_keeps_the_body_and_the_other_header_lines()
    {
        var body = "# Plan\n\ntrailing spaces  \n\n\n";
        Write("---\nreview: skipped   # pending | done | skipped\nenvironment: Contoso Dev # target\nstatus: approved # next: signed-off\n---\n" + body);

        var r = SignOff("2026-01-02");
        var text = File.ReadAllText(PlanFile);
        var hash = PlanDocument.Parse(text).BodyHash;

        Assert.Equal(0, r.ExitCode);
        Assert.Equal($"signed off docs/plans/demo (sha256 {hash![..12]}…)", Assert.Single(r.Out));
        Assert.Equal(
            $"---\nreview: skipped   # pending | done | skipped\nenvironment: Contoso Dev # target\nstatus: signed-off\nsigned-off: 2026-01-02\nsigned-off-hash: {hash}\n---\n{body}",
            text);
        Assert.Equal(PlanDocument.Parse("---\nstatus: draft\nreview: x\n---\n" + body).BodyHash, hash);
    }

    [Fact]
    public void Sign_off_keeps_the_depth_and_supersedes_lines()
    {
        _t.Write("docs/plans/old/plan.md", Plan());
        Write("---\nstatus: approved\nreview: skipped\ndepth: deep # level\nsupersedes: old\n---\n# Plan\n");

        var r = SignOff("2026-01-02");
        var text = File.ReadAllText(PlanFile);

        Assert.Equal(0, r.ExitCode);
        Assert.Contains("\ndepth: deep # level\nsupersedes: old\n", text);
        Assert.Empty(Check().Violations);
    }

    [Fact]
    public void Supersedes_must_name_another_existing_plan_in_every_status()
    {
        Write("---\nstatus: draft\nreview: pending\nsupersedes: site-visits\n---\n# Plan\n");
        Assert.Equal(["supersedes 'site-visits' but docs/plans/site-visits/plan.md does not exist"], Check().Violations);

        _t.Write("docs/plans/site-visits/plan.md", Plan());
        var ok = Check();
        Assert.Empty(ok.Violations);
        Assert.Equal("site-visits", ok.Supersedes);

        Write("---\nstatus: draft\nreview: pending\nsupersedes: demo\n---\n# Plan\n");
        Assert.Equal(["supersedes names the plan itself"], Check().Violations);

        Write("---\nstatus: draft\nreview: pending\nsupersedes: 2026-site-visits\n---\n# Plan\n");
        Assert.Equal(["supersedes '2026-site-visits' but docs/plans/2026-site-visits/plan.md does not exist"], Check().Violations);
    }

    [Theory]
    [InlineData("contoso.crm.dynamics.com")]
    [InlineData("admin@contoso.com")]
    [InlineData("C:old")]
    public void A_supersedes_that_is_not_a_plain_name_is_malformed_and_not_echoed(string value)
    {
        Write($"---\nstatus: draft\nreview: pending\nsupersedes: {value}\n---\n# Plan\n");
        var report = Check();

        Assert.True(report.Malformed);
        Assert.Null(report.Supersedes);
        Assert.DoesNotContain(value, string.Join("\n", report.Violations));
    }

    [Fact]
    public void A_path_like_supersedes_is_a_malformed_header_and_depth_is_reported()
    {
        Write("---\nstatus: draft\nreview: pending\nsupersedes: ../other\n---\n# Plan\n");
        var bad = Check();
        Assert.True(bad.Malformed);
        Assert.Contains("supersedes must be a plan folder name (letters, digits, '-' and '_'), not a path", bad.Violations[0]);

        Write("---\nstatus: draft\nreview: pending\ndepth: quick\n---\n# Plan\n");
        Assert.Equal("quick", Check().Depth);
    }

    [Fact]
    public void Sign_off_keeps_crlf_and_writes_no_bom()
    {
        var body = "# Plan\r\ntext\r\n";
        Write("---\r\nstatus: approved\r\nreview: skipped\r\n---\r\n" + body);

        SignOff();

        var bytes = File.ReadAllBytes(PlanFile);
        var text = File.ReadAllText(PlanFile);
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.DoesNotMatch("[^\r]\n", text);
        Assert.EndsWith("---\r\n" + body, text);
        Assert.Empty(Check().Violations);
    }

    [Fact]
    public void A_plan_with_no_journal_has_no_writes()
    {
        Write(Plan());

        Assert.Null(Check().Writes);
    }

    [Fact]
    public void The_journal_counts_rows_after_each_header_and_separator_and_cleanups_that_are_neither_none_nor_done()
    {
        Write(Plan());
        _t.Write("docs/plans/demo/writes.md", string.Join("\r\n",
            "# Writes: demo",
            "",
            "| # | Step | Actor | Authority | Operation | Outcome | Cleanup |",
            "|---|---|---|---|---|---|---|",
            "| 1 | 1 | agent build | person: yes to step 1 | create table Loan with dv-metadata | done | none |",
            "| 2 | 2 | agent build | person: yes to step 2 | create app module Loans with raw Web API | failed, a draft left in Dev | open: the draft app module Loans; the person removes it |",
            "| 3 | 2 | agent build | person: yes to step 2 | turn on auditing \\| Dev | done | DONE: the person, 2026-11-04 |",
            "| 4 | 3 | agent build | person: yes to step 3 | publish the app | done | |",
            "| 5 | 3 | agent build | person: yes to step 3 | update a view | done | None.",
            "| 6 | 3 | agent build | person: yes to step 3 | update a form | done | `none` |",
            "| 7 | 3 | agent build | person: yes to step 3 | run the flow once | done | `done: the person` |",
            "",
            "| Later | table |",
            "| :-- | --: |",
            "| x | open: y |",
            ""));

        var r = Check();

        Assert.Equal(new PlanWrites(8, 3), r.Writes);
        Assert.Empty(r.Warnings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("# Writes: demo\n\n| # | Step | Actor | Authority | Operation | Outcome | Cleanup |\n|---|---|---|---|---|---|---|\n")]
    public void An_empty_journal_or_one_with_only_its_header_has_no_entries(string text)
    {
        Write(Plan());
        _t.Write("docs/plans/demo/writes.md", text);

        Assert.Equal(new PlanWrites(0, 0), Check().Writes);
    }

    [Fact]
    public void A_journal_that_is_not_read_has_null_counts_and_one_warning()
    {
        Write(Plan());
        _t.Write("docs/plans/demo/writes.md", "| 1 | open: x |\0\n");

        var r = Check();

        Assert.Equal(new PlanWrites(null, null), r.Writes);
        Assert.Equal(["docs/plans/demo/writes.md: a binary file, not read"], r.Warnings);
    }

    [Fact]
    public void Log_lines_answered_by_an_agent_warn_on_a_sign_off_or_an_acceptance_and_nothing_else_does()
    {
        Write(Plan());
        _t.Write("docs/plans/demo/decisions.md", string.Join("\r\n",
            "# Decisions: demo",
            "",
            "## Round 1",
            "",
            "### Q1. Accept the risk and sign off?",
            "1. Yes (recommended)",
            "2. In your own words",
            "Answer: 1, accept it and sign off now (answered by: agent orchestrator)",
            "",
            "## Log",
            "",
            "- approved for review (answered by: agent orchestrator)",
            "- signed off 2026-11-04 (answered by: Agent orchestrator)",
            "- accepted R3 (High): no time to fix (answered by:agent orchestrator)",
            "- sign-off (answered by: person)",
            "- accepted S1 by the plan owner",
            "- acceptance of V1 and sign-off (answered by: agent orchestrator)",
            "- an acceptable answer (answered by: agent x)",
            "- plan signed-off (answered by: agent x)",
            "- signoff (answered by: agent x)",
            "- accepted the gap of step 3,",
            "  not verified in Dev (answered by: agent x)",
            "  - a nested note, accepted (answered by: person)",
            "",
            "## Later",
            "",
            "- signed off (answered by: agent x)",
            ""));

        Assert.Equal(
            [
                "sign-off answered by an agent (docs/plans/demo/decisions.md:13): only a person signs off a plan",
                "acceptance answered by an agent (docs/plans/demo/decisions.md:14): only a person accepts a High finding or a security gap",
                "sign-off answered by an agent (docs/plans/demo/decisions.md:17): only a person signs off a plan",
                "sign-off answered by an agent (docs/plans/demo/decisions.md:19): only a person signs off a plan",
                "sign-off answered by an agent (docs/plans/demo/decisions.md:20): only a person signs off a plan",
                "acceptance answered by an agent (docs/plans/demo/decisions.md:21): only a person accepts a High finding or a security gap",
            ],
            Check().Warnings);
    }

    [Fact]
    public void A_decisions_file_with_no_log_or_no_marker_adds_no_warning()
    {
        Write(Plan());
        _t.Write("docs/plans/demo/decisions.md", "# Decisions: demo\n\n## Round 1\n\n### Q1. Sign off?\nAnswer: signed off (answered by: agent x)\n");
        Assert.Empty(Check().Warnings);

        _t.Write("docs/plans/demo/decisions.md", "# Decisions: demo\n\n## Log\n\n- signed off 2026-11-04 by the building session\n- accepted R2\n");
        Assert.Empty(Check().Warnings);
    }

    // ---- the solution a report names (M62) ----

    private const string StaleWarning = "verify.md was written for a different version of the solution in '{0}' (stale solution)";

    private static string Digest(string path) => PowerOps.Core.Readers.Manifest.Read(path).Digest;

    private string Solution(string text = "<a/>\n")
    {
        _t.Write("solutions/demo/Other/Solution.xml", text);
        return Path.Combine(_t.Path, "solutions", "demo");
    }

    private PlanReportFile VerifyReport() => Assert.Single(Check().Reports, r => r.File == "verify.md");

    [Fact]
    public void A_folder_with_the_recorded_digest_is_fresh_and_a_changed_one_is_stale_with_a_warning()
    {
        Write(Plan());
        var digest = Digest(Solution());
        Report("verify.md", Six(hash: digest));

        var fresh = Check();

        Assert.Equal(false, Assert.Single(fresh.Reports).SolutionStale);
        Assert.Empty(fresh.Warnings);

        Solution("<b/>\n");
        var stale = Check();

        Assert.Equal(true, Assert.Single(stale.Reports).SolutionStale);
        Assert.Equal([string.Format(CultureInfo.InvariantCulture, StaleWarning, "solutions/demo")], stale.Warnings);
        Assert.Empty(stale.Violations);
    }

    [Fact]
    public void A_zip_with_the_recorded_digest_is_fresh_and_a_changed_one_is_stale()
    {
        Write(Plan());
        var zip = Path.Combine(_t.Path, "out", "Demo.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(zip)!);
        File.WriteAllBytes(zip, [1, 2, 3]);
        Report("verify.md", Six("input: out/Demo.zip", Digest(zip)));

        Assert.Equal(false, VerifyReport().SolutionStale);

        File.WriteAllBytes(zip, [1, 2, 4]);

        Assert.Equal(true, VerifyReport().SolutionStale);
        Assert.Equal([string.Format(CultureInfo.InvariantCulture, StaleWarning, "out/Demo.zip")], Check().Warnings);
    }

    [Fact]
    public void Both_solution_reports_are_compared_and_review_md_stays_null()
    {
        Write(Plan());
        var digest = Digest(Solution());
        Report("review.md", $"plan-hash: {Hash}\ninput: solutions/demo\nsolution-hash: {digest}\n");
        Report("security-solution.md", Six(hash: digest));
        Report("verify.md", Six(hash: Other));

        Assert.Equal([("review.md", null), ("security-solution.md", false), ("verify.md", true)], Check().Reports.Select(r => (r.File, r.SolutionStale)));
    }

    [Fact]
    public void A_legacy_report_and_an_unknown_hash_read_null_without_a_new_warning()
    {
        Write(Plan());
        Solution();
        Report("security-solution.md", $"plan-hash: {Hash}\nmodel: m\nfresh-context: yes\nstate: ready\ninput: the built app: v2\n");
        Report("verify.md", "state: ready\ninput: solutions/demo\n");

        var reports = Check();

        Assert.All(reports.Reports, r => Assert.Null(r.SolutionStale));
        Assert.Equal(["security-solution.md has an input: line that is not a relative path"], reports.Warnings);
    }

    [Fact]
    public void A_report_with_a_valid_hash_and_no_input_line_reads_null_without_a_warning()
    {
        Write(Plan());
        Report("verify.md", $"state: ready\nsolution-hash: {SolutionHash}\n");

        var reports = Check();

        Assert.Null(Assert.Single(reports.Reports).SolutionStale);
        Assert.Empty(reports.Warnings);
    }

    [Fact]
    public void A_solution_that_is_not_there_reads_null_without_a_warning()
    {
        Write(Plan());
        Report("verify.md", Six("input: solutions/missing"));

        var reports = Check();

        Assert.Null(Assert.Single(reports.Reports).SolutionStale);
        Assert.Empty(reports.Warnings);
    }

    [Theory]
    [InlineData("solutions/link")]
    [InlineData("linked/demo")]
    public void A_link_on_the_way_to_the_solution_reads_null_with_a_warning(string input)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        Write(Plan());
        var solution = Solution();
        Directory.CreateSymbolicLink(Path.Combine(_t.Path, "solutions", "link"), solution);
        Directory.CreateSymbolicLink(Path.Combine(_t.Path, "linked"), Path.Combine(_t.Path, "solutions"));
        Report("verify.md", Six($"input: {input}", Digest(solution)));

        var reports = Check();

        Assert.Null(Assert.Single(reports.Reports).SolutionStale);
        Assert.Equal([$"verify.md: input '{input}' passes through a symbolic link, so the solution was not compared"], reports.Warnings);
    }

    [Fact]
    public void Two_reports_naming_the_same_linked_input_each_warn_under_their_own_name()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        Write(Plan());
        Directory.CreateSymbolicLink(Path.Combine(_t.Path, "link"), Solution());
        Report("security-solution.md", Six("input: link"));
        Report("verify.md", Six("input: link"));

        Assert.Equal(
            ["security-solution.md: input 'link' passes through a symbolic link, so the solution was not compared", "verify.md: input 'link' passes through a symbolic link, so the solution was not compared"],
            Check().Warnings);
    }

    [Theory]
    [InlineData("CON", true)]
    [InlineData("nul", true)]
    [InlineData("Aux.txt", true)]
    [InlineData("com1.zip", true)]
    [InlineData("LPT9", true)]
    [InlineData("name.", true)]
    [InlineData("name ", true)]
    [InlineData("COM0", false)]
    [InlineData("CONSOLE", false)]
    [InlineData("a.con", false)]
    [InlineData("Demo.zip", false)]
    public void A_windows_reserved_name_is_a_device_name_or_ends_in_a_dot_or_a_space(string name, bool reserved) =>
        Assert.Equal(reserved, PlanChecker.WindowsReservedName(name));

    [Fact]
    public void On_windows_an_input_with_a_reserved_name_is_not_read()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "only Windows opens a device for these names");
        Write(Plan());
        Report("verify.md", Six("input: solutions/NUL"));

        var reports = Check();

        Assert.Null(Assert.Single(reports.Reports).SolutionStale);
        Assert.Equal(["verify.md: input 'solutions/NUL' could not be read, so the solution was not compared"], reports.Warnings);
    }

    [Fact]
    public void A_plan_folder_outside_docs_plans_reads_null_with_a_warning_and_no_path()
    {
        _t.Write("elsewhere/plan.md", Plan()).Write("elsewhere/verify.md", Six());

        var report = PlanChecker.Check(Path.Combine(_t.Path, "elsewhere", "plan.md"), _t.Path);

        Assert.Null(Assert.Single(report.Reports).SolutionStale);
        Assert.Equal(["verify.md: the plan folder is not in docs/plans, so the solution was not compared"], report.Warnings);
    }

    [Fact]
    public void An_input_that_is_not_a_folder_or_a_zip_reads_null_with_a_warning()
    {
        Write(Plan());
        _t.Write("notes/readme.txt", "x");
        Report("verify.md", Six("input: notes/readme.txt"));

        var reports = Check();

        Assert.Null(Assert.Single(reports.Reports).SolutionStale);
        Assert.Equal(["verify.md: input 'notes/readme.txt' could not be read, so the solution was not compared"], reports.Warnings);
    }

    [Fact]
    public void Ready_hashes_is_null_without_docs_plans_and_holds_the_hash_of_each_ready_solution_report()
    {
        Assert.Null(PlanChecker.ReadyHashes(_t.Path));

        var (a, b, c, d) = (new string('1', 64), new string('2', 64), new string('3', 64), new string('4', 64));
        _t.Write("docs/plans/one/plan.md", Plan());
        _t.Write("docs/plans/one/verify.md", $"state: ready\nsolution-hash: {a}\n");
        _t.Write("docs/plans/one/security-solution.md", $"state: blocked\nsolution-hash: {b}\n");
        _t.Write("docs/plans/one/review.md", $"state: ready\nsolution-hash: {c}\n");
        _t.Write("docs/plans/two/plan.md", Plan());
        _t.Write("docs/plans/two/security-solution.md", $"state: ready\nsolution-hash: {d}\n");
        _t.Write("docs/plans/two/verify.md", "state: ready\n");

        var ready = PlanChecker.ReadyHashes(_t.Path);

        Assert.NotNull(ready);
        Assert.Equal([a, d], ready.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Ready_hashes_reads_docs_plans_even_when_the_root_holds_a_plan_md()
    {
        _t.Write("plan.md", Plan());
        _t.Write("docs/plans/x/plan.md", Plan());
        _t.Write("docs/plans/x/verify.md", $"state: ready\nsolution-hash: {SolutionHash}\n");

        Assert.Equal([SolutionHash], PlanChecker.ReadyHashes(_t.Path));
    }

    private const string ScreensPlan = "# Plan\n\n## Screens\n\n- Home\n";
    private const string GoodSketch = "<!-- route: canvas -->\n<p>Sketch</p>\n<section data-screen=\"Home\"><i data-state=\"empty\"></i><i data-state=\"loading\"></i><i data-state=\"error\"></i><i data-state=\"failed-save\"></i></section>\n";

    private SignOffResult SignOffWith(string? log, string? sketch, string body = ScreensPlan)
    {
        Write(Plan("approved", "skipped", body));
        if (log is not null)
        {
            _t.Write("docs/plans/demo/decisions.md", $"## Log\n\n{log}\n");
        }

        if (sketch is not null)
        {
            _t.Write("docs/plans/demo/mockups/prototype.html", sketch);
        }

        return SignOff();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("- prototype: not needed (no screens)")]
    [InlineData("- prototype: not chosen")]
    [InlineData("- prototype: skipped (no time) (answered by: agent)")]
    public void Sign_off_is_refused_for_screens_without_a_valid_prototype_line(string? log)
    {
        var r = SignOffWith(log, GoodSketch);

        Assert.Equal(1, r.ExitCode);
        Assert.Contains("no valid prototype line", Assert.Single(r.Err), StringComparison.Ordinal);
        Assert.StartsWith("status: approved", File.ReadAllText(PlanFile).Split('\n')[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_off_is_refused_for_an_approved_sketch_that_fails_and_for_a_missing_one()
    {
        var failing = SignOffWith("- prototype: approved mockups/prototype.html (answered by: person)", "<p>Sketch</p>\n");
        Assert.Equal(1, failing.ExitCode);
        Assert.All(failing.Err, e => Assert.StartsWith("error: mockups/prototype.html:", e, StringComparison.Ordinal));

        File.Delete(Path.Combine(Path.GetDirectoryName(PlanFile)!, "mockups", "prototype.html"));
        Assert.Equal(1, SignOff().ExitCode);
    }

    [Fact]
    public void Sign_off_passes_a_passing_sketch_a_persons_skip_and_a_plan_with_no_screens_and_no_line()
    {
        Assert.Equal(0, SignOffWith("- prototype: approved mockups/prototype.html (answered by: person)", GoodSketch).ExitCode);

        // A person's skip stands even when a stale sketch fails its checks.
        Assert.Equal(0, SignOffWith("- prototype: skipped (mock-data slice) (answered by: person)", "<p>x</p>").ExitCode);

        File.Delete(Path.Combine(Path.GetDirectoryName(PlanFile)!, "decisions.md"));
        Assert.Equal(0, SignOffWith(null, null, "# Plan\n\n## Screens\n\nNone.\n").ExitCode);
    }
}