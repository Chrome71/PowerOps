using PowerOps.Core.Deploy;
using PowerOps.Core.Output;

namespace PowerOps.Core.Tests.Output;

public class DeployApplyReportTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static DeployApplyResult Result(
        string status, bool applied = true, string? message = null, AppliedSolution[]? solutions = null, AppliedFlow[]? flows = null) =>
        new("1.2.3", applied, "Contoso Test", Hash, status, message, solutions ?? [], flows ?? []);

    private static AppliedSolution Sol(int order, string name, string action, string import, int? exit = null) =>
        new(order, name, name + ".zip", action, import, exit);

    private static string Text(DeployApplyResult r)
    {
        var w = new StringWriter();
        DeployApplyReport.WriteText(r, w);
        return w.ToString();
    }

    private static string Json(DeployApplyResult r)
    {
        var w = new StringWriter();
        DeployApplyReport.WriteJson(r, w);
        return w.ToString();
    }

    // ---- JSON ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Json_has_a_stable_property_order_nulls_and_a_trailing_newline_with_unix_endings()
    {
        var json = Json(Result(ApplyStatus.Imported, true, null, [Sol(1, "A", "install", ImportStatus.Ok)], [new("A", "Notify", "on", null)]));

        Assert.Equal(
            "{\n  \"schemaVersion\": 1,\n  \"tool\": \"1.2.3\",\n  \"applied\": true,\n  \"target\": \"Contoso Test\",\n  \"hash\": \"" + Hash + "\",\n  \"status\": \"imported\",\n  \"message\": null,\n" +
            "  \"solutions\": [\n    {\n      \"order\": 1,\n      \"solution\": \"A\",\n      \"zip\": \"A.zip\",\n      \"action\": \"install\",\n      \"import\": \"ok\",\n      \"pacExitCode\": null\n    }\n  ],\n" +
            "  \"flows\": [\n    {\n      \"solution\": \"A\",\n      \"name\": \"Notify\",\n      \"expectedState\": \"on\",\n      \"stateAfter\": null\n    }\n  ]\n}\n",
            json);
    }

    // ---- Text -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Text_of_a_dry_run_counts_the_imports_it_would_make()
    {
        var text = Text(Result(ApplyStatus.Planned, false, null, [Sol(1, "A", "install", ImportStatus.Planned), Sol(2, "B", "skip", ImportStatus.Skipped), Sol(3, "C", "upgrade", ImportStatus.Planned)], [new("A", "Notify", "on", null)]));

        Assert.Equal(
            $"Target: Contoso Test\nPlan hash: {Hash}\n1 A (A.zip): install, planned\n2 B (B.zip): skip, skipped\n3 C (C.zip): upgrade, planned\nflow A/Notify: expected on, now not read\ndry run: would import 2 solutions\n",
            text);
        Assert.EndsWith("would import 1 solution\n", Text(Result(ApplyStatus.Planned, false, null, [Sol(1, "A", "install", ImportStatus.Planned)])));
    }

    [Fact]
    public void Text_of_an_applied_run_says_applied_and_flows_as_expected_do_not_add_a_warning()
    {
        var text = Text(Result(ApplyStatus.Imported, true, null, [Sol(1, "A", "install", ImportStatus.Ok)], [new("A", "Notify", "off", "off")]));

        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\n1 A (A.zip): install, ok\nflow A/Notify: expected off, now off\napplied\n", text);
    }

    [Fact]
    public void Text_of_an_applied_run_counts_flows_not_in_their_expected_state()
    {
        var text = Text(Result(ApplyStatus.Imported, true, null, [Sol(1, "A", "install", ImportStatus.Ok)], [new("A", "N1", "on", "off"), new("A", "N2", "on", "on")]));

        Assert.EndsWith("applied, but 1 flow is not in its expected state\n", text);
        Assert.EndsWith("applied, but 2 flows are not in their expected state\n",
            Text(Result(ApplyStatus.Imported, true, null, [], [new("A", "N1", "on", "off"), new("A", "N2", "on", "missing")])));
    }

    [Fact]
    public void Text_of_an_applied_run_with_a_message_puts_it_on_the_summary_line()
    {
        var text = Text(Result(ApplyStatus.Imported, true, "The imports succeeded. The flows could not be read.", [Sol(1, "A", "install", ImportStatus.Ok)], [new("A", "Notify", "on", null)]));

        Assert.EndsWith("flow A/Notify: expected on, now not read\napplied: The imports succeeded. The flows could not be read.\n", text);
    }

    [Fact]
    public void Text_of_a_refusal_and_a_failure_ends_with_the_message()
    {
        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\nrefused: a blocked plan is never applied\n", Text(Result(ApplyStatus.Refused, false, "a blocked plan is never applied")));
        Assert.EndsWith("2 B (B.zip): upgrade, failed\n3 C (C.zip): install, not-run\nfailed: No rollback. Imported: A.\n",
            Text(Result(ApplyStatus.Failed, true, "No rollback. Imported: A.", [Sol(1, "A", "install", ImportStatus.Ok), Sol(2, "B", "upgrade", ImportStatus.Failed, 1), Sol(3, "C", "install", ImportStatus.NotRun)])));
        Assert.EndsWith("\nrefused\n", Text(Result(ApplyStatus.Refused, false)));
    }

    [Fact]
    public void Text_cleans_every_name_and_the_message()
    {
        var evil = "x\n::error::boom";
        var r = new DeployApplyResult("1.2.3", true, evil, Hash, ApplyStatus.Failed, "m\r\n::error::m", [new(1, evil, evil + ".zip", "install", ImportStatus.Failed, 1)], [new(evil, evil, "on", evil)]);

        var text = Text(r);

        Assert.DoesNotContain('\r', text);
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.DoesNotMatch(@"^\s*::", l));
        Assert.Contains("Target: x ::error::boom", text);
        Assert.Contains("failed: m  ::error::m", text);
        Assert.Equal(5, text.Count(c => c == '\n'));
    }
}