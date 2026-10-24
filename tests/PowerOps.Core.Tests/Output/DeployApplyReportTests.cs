using System.Text.Json.Nodes;

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

    private static void SameJson(string expected, DeployApplyResult r) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(Json(r))), Json(r));

    private static string Doc(string applied, string status, string message, string solutions = "[]", string flows = "[]") =>
        $$"""{"schemaVersion":1,"tool":"1.2.3","applied":{{applied}},"target":"Contoso Test","hash":"{{Hash}}","status":"{{status}}","message":{{message}},"solutions":{{solutions}},"flows":{{flows}}}""";

    private static string S(int order, string name, string action, string import, string exit = "null") =>
        $$"""{"order":{{order}},"solution":"{{name}}","zip":"{{name}}.zip","action":"{{action}}","import":"{{import}}","pacExitCode":{{exit}}}""";

    private static string F(string solution, string name, string expected, string after) =>
        $$"""{"solution":"{{solution}}","name":"{{name}}","expectedState":"{{expected}}","stateAfter":{{after}}}""";

    // ---- JSON: one test per row of the case table ---------------------------------------------------------------

    [Fact]
    public void Json_refused_before_the_re_plan_has_empty_lists() =>
        SameJson(Doc("false", "refused", "\"a blocked plan is never applied\""),
            Result(ApplyStatus.Refused, false, "a blocked plan is never applied"));

    [Fact]
    public void Json_refused_after_the_re_plan_lists_the_new_plans_solutions_as_not_run() =>
        SameJson(
            Doc("true", "refused", "\"the plan is blocked now\"", $"[{S(1, "A", "install", "not-run")},{S(2, "B", "skip", "not-run")}]"),
            Result(ApplyStatus.Refused, true, "the plan is blocked now", [Sol(1, "A", "install", ImportStatus.NotRun), Sol(2, "B", "skip", ImportStatus.NotRun)]));

    [Fact]
    public void Json_of_a_dry_run_has_planned_and_skipped_solutions_and_flows_without_a_state_after() =>
        SameJson(
            Doc("false", "planned", "null", $"[{S(1, "A", "install", "planned")},{S(2, "B", "skip", "skipped")}]", $"[{F("A", "Notify", "on", "null")}]"),
            Result(ApplyStatus.Planned, false, null, [Sol(1, "A", "install", ImportStatus.Planned), Sol(2, "B", "skip", ImportStatus.Skipped)], [new("A", "Notify", "on", null)]));

    [Fact]
    public void Json_imported_with_only_skips_has_no_flows() =>
        SameJson(
            Doc("true", "imported", "null", $"[{S(1, "B", "skip", "skipped")}]"),
            Result(ApplyStatus.Imported, true, null, [Sol(1, "B", "skip", ImportStatus.Skipped)]));

    [Fact]
    public void Json_imported_has_ok_and_skipped_solutions_and_the_states_after() =>
        SameJson(
            Doc("true", "imported", "null", $"[{S(1, "A", "upgrade", "ok")},{S(2, "B", "skip", "skipped")}]", $"[{F("A", "Notify", "off", "\"off\"")}]"),
            Result(ApplyStatus.Imported, true, null, [Sol(1, "A", "upgrade", ImportStatus.Ok), Sol(2, "B", "skip", ImportStatus.Skipped)], [new("A", "Notify", "off", "off")]));

    [Fact]
    public void Json_imported_with_a_flow_not_as_expected_shows_both_states() =>
        SameJson(
            Doc("true", "imported", "null", $"[{S(1, "A", "install", "ok")}]", $"[{F("A", "Notify", "on", "\"off\"")}]"),
            Result(ApplyStatus.Imported, true, null, [Sol(1, "A", "install", ImportStatus.Ok)], [new("A", "Notify", "on", "off")]));

    [Fact]
    public void Json_imported_with_the_second_read_failed_has_no_state_after_and_the_message() =>
        SameJson(
            Doc("true", "imported", "\"The imports succeeded. The flows could not be read.\"", $"[{S(1, "A", "install", "ok")}]", $"[{F("A", "Notify", "on", "null")}]"),
            Result(ApplyStatus.Imported, true, "The imports succeeded. The flows could not be read.", [Sol(1, "A", "install", ImportStatus.Ok)], [new("A", "Notify", "on", null)]));

    [Fact]
    public void Json_failed_has_ok_then_failed_with_the_pac_exit_code_then_not_run() =>
        SameJson(
            Doc("true", "failed", "\"No rollback. Imported: A.\"", $"[{S(1, "A", "install", "ok")},{S(2, "B", "upgrade", "failed", "1")},{S(3, "C", "install", "not-run")}]"),
            Result(ApplyStatus.Failed, true, "No rollback. Imported: A.", [Sol(1, "A", "install", ImportStatus.Ok), Sol(2, "B", "upgrade", ImportStatus.Failed, 1), Sol(3, "C", "install", ImportStatus.NotRun)]));

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