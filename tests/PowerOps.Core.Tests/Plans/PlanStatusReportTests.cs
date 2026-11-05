using PowerOps.Core.Plans;
using PowerOps.Core.Tests.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Plans;

public class PlanStatusReportTests
{
    private static string Plan(string status, string review, string body, string extra = "") =>
        $"---\nstatus: {status}\nreview: {review}\nsigned-off: null\nsigned-off-hash: null\nenvironment: null\n{extra}---\n{body}";

    [Fact]
    public void Json_matches_golden()
    {
        using var t = new TempFolder();
        t.Write("docs/plans/clean/plan.md", Plan("draft", "pending", "# Plan\n\n## Open questions\n- one\n"));
        t.Write("docs/plans/clean/writes.md", "| # | Cleanup |\n|---|---|\n| 1 | open: a draft app module |\n| 2 | none |\n");
        t.Write("docs/plans/clean-v2/plan.md", Plan("draft", "pending", "# Plan\n", "depth: deep\nsupersedes: clean\n"));
        t.Write("docs/plans/trouble/plan.md", Plan("approved", "done", "# Plan\n\nsee https://contoso.example/x\n"));
        t.Write("docs/plans/trouble/review.md", $"plan-hash: {new string('a', 64)}\n");

        var plans = PlanChecker.Discover(t.Path).Select(f => PlanChecker.Check(f, t.Path)).ToList();
        var w = new StringWriter();
        PlanStatusReport.WriteJson(plans, w);

        Assert.Equal(1, plans.Sum(p => p.Violations.Count));
        Assert.Single(plans[2].Warnings);
        Golden.Verify(w.ToString(), "plan-status.json");
    }

    [Fact]
    public void Text_prints_one_line_per_report_under_its_plan_with_null_shown_plainly_and_values_cleaned()
    {
        using var t = new TempFolder();
        t.Write("docs/plans/demo/plan.md", Plan("draft", "pending", "# Plan\n"));
        t.Write("docs/plans/demo/review.md", "findings\n");
        t.Write("docs/plans/demo/verify.md", $"state: ready\nplan-hash: {PlanChecker.Check(Path.Combine(t.Path, "docs", "plans", "demo", "plan.md"), t.Path).BodyHash}\n");
        t.Write("docs/plans/demo/security-solution.md", "state: ok\u2028::error::forged\u001b[31m\n");

        var w = new StringWriter();
        PlanStatusReport.WriteText([PlanChecker.Check(Path.Combine(t.Path, "docs", "plans", "demo", "plan.md"), t.Path)], w);

        Assert.Equal(
            "docs/plans/demo: draft, review pending, 0 open questions\n"
            + "  report review.md: state null, stale null\n"
            + "  report security-solution.md: state 'ok ::error::forged [31m', stale null\n"
            + "  report verify.md: state 'ready', stale false\n"
            + "  warning: review.md has no plan-hash line\n"
            + "  warning: security-solution.md has an unknown state 'ok ::error::forged [31m' (expected ready, blocked, failed or dropped)\n"
            + "1 plan, 0 violations, 2 warnings\n",
            w.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void Text_lists_the_journal_after_the_reports_and_says_when_it_was_not_read()
    {
        using var t = new TempFolder();
        t.Write("docs/plans/a/plan.md", Plan("draft", "pending", "# Plan\n"));
        t.Write("docs/plans/a/review.md", "findings\n");
        t.Write("docs/plans/a/writes.md", "| # | Cleanup |\n|---|---|\n| 1 | open: a draft\u2028::error::x |\n");
        t.Write("docs/plans/b/plan.md", Plan("draft", "pending", "# Plan\n"));
        t.Write("docs/plans/b/writes.md", "\0");
        t.Write("docs/plans/c/plan.md", Plan("draft", "pending", "# Plan\n"));
        t.Write("docs/plans/c/writes.md", "| # | Cleanup |\n|---|---|\n| 1 | none |\n| 2 | done: the person |\n");

        var w = new StringWriter();
        PlanStatusReport.WriteText([.. PlanChecker.Discover(t.Path).Select(f => PlanChecker.Check(f, t.Path))], w);

        Assert.Equal(
            "docs/plans/a: draft, review pending, 0 open questions\n"
            + "  report review.md: state null, stale null\n"
            + "  writes writes.md: 1 entry, 1 open cleanup\n"
            + "  warning: review.md has no plan-hash line\n"
            + "docs/plans/b: draft, review pending, 0 open questions\n"
            + "  writes writes.md: not read\n"
            + "  warning: docs/plans/b/writes.md: a binary file, not read\n"
            + "docs/plans/c: draft, review pending, 0 open questions\n"
            + "  writes writes.md: 2 entries, 0 open cleanups\n"
            + "3 plans, 0 violations, 2 warnings\n",
            w.ToString().Replace("\r\n", "\n"));
    }
}