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
    public void A_review_plan_hash_after_the_fourth_line_still_counts_but_a_verify_one_does_not()
    {
        Write(Plan());
        Report("review.md", $"a\nb\nc\nd\ne\nplan-hash: {Other}\n");
        Report("verify.md", $"state: ready\nb\nc\nd\nplan-hash: {Other}\n");

        var reports = Check();

        Assert.Equal([("review.md", null, true), ("verify.md", "ready", null)], reports.Reports.Select(Row));
        Assert.Equal(["review.md was written for a different version of the plan (stale review)"], reports.Warnings);
    }

    [Fact]
    public void A_report_reads_state_and_plan_hash_from_its_first_four_lines_only()
    {
        Write(Plan());
        Report("security-solution.md", $"a\nb\nc\nplan-hash: {Hash}\nstate: ready\n");
        Report("verify.md", "a\nb\nc\nd\nstate: ready\n");

        var reports = Check();

        Assert.Equal([("security-solution.md", null, false), ("verify.md", null, null)], reports.Reports.Select(Row));
        Assert.Empty(reports.Warnings);
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
}