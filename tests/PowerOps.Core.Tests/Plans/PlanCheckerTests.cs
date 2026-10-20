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
            [Path.Combine(_t.Path, "docs/plans/a/plan.md"), Path.Combine(_t.Path, "docs/plans/b/plan.md")],
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