using PowerOps.Core.Plans;
using PowerOps.Core.Tests.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Plans;

public class PlanStatusReportTests
{
    private static string Plan(string status, string review, string body) =>
        $"---\nstatus: {status}\nreview: {review}\nsigned-off: null\nsigned-off-hash: null\nenvironment: null\n---\n{body}";

    [Fact]
    public void Json_matches_golden()
    {
        using var t = new TempFolder();
        t.Write("docs/plans/clean/plan.md", Plan("draft", "pending", "# Plan\n\n## Open questions\n- one\n"));
        t.Write("docs/plans/trouble/plan.md", Plan("approved", "done", "# Plan\n\nsee https://contoso.example/x\n"));
        t.Write("docs/plans/trouble/review.md", $"plan-hash: {new string('a', 64)}\n");

        var plans = PlanChecker.Discover(t.Path).Select(f => PlanChecker.Check(f, t.Path)).ToList();
        var w = new StringWriter();
        PlanStatusReport.WriteJson(plans, w);

        Assert.Equal(1, plans[0].Violations.Count + plans[1].Violations.Count);
        Assert.Single(plans[1].Warnings);
        Golden.Verify(w.ToString(), "plan-status.json");
    }
}