using PowerOps.Core.Deploy;
using PowerOps.Core.Output;

namespace PowerOps.Core.Tests.Output;

public class DeployApplyReportTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static DeployApplyResult Result(
        string status, bool applied = true, string? message = null, AppliedSolution[]? solutions = null, AppliedFlow[]? flows = null,
        string? appliedAt = null, string executor = ApplyExecutor.Cli, Approval? approval = null, AppliedAgent[]? agents = null) =>
        new("1.2.3", applied, "Contoso Test", Hash, status, message, appliedAt, executor, approval, solutions ?? [], flows ?? [], agents ?? []);

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

    // ---- Text -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Text_of_a_dry_run_counts_the_imports_it_would_make()
    {
        var text = Text(Result(ApplyStatus.Planned, false, null, [Sol(1, "A", "install", ImportStatus.Planned), Sol(2, "B", "skip", ImportStatus.Skipped), Sol(3, "C", "upgrade", ImportStatus.Planned)], [new("A", "Notify", "on", null)]));

        Assert.Equal(
            $"Target: Contoso Test\nPlan hash: {Hash}\nExecutor: cli\n1 A (A.zip): install, planned\n2 B (B.zip): skip, skipped\n3 C (C.zip): upgrade, planned\nflow A/Notify: expected on, now not read\ndry run: would import 2 solutions\n",
            text);
        Assert.EndsWith("would import 1 solution\n", Text(Result(ApplyStatus.Planned, false, null, [Sol(1, "A", "install", ImportStatus.Planned)])));
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
        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\nExecutor: cli\nrefused: a blocked plan is never applied\n", Text(Result(ApplyStatus.Refused, false, "a blocked plan is never applied")));
        Assert.EndsWith("2 B (B.zip): upgrade, failed\n3 C (C.zip): install, not-run\nfailed: No rollback. Imported: A.\n",
            Text(Result(ApplyStatus.Failed, true, "No rollback. Imported: A.", [Sol(1, "A", "install", ImportStatus.Ok), Sol(2, "B", "upgrade", ImportStatus.Failed, 1), Sol(3, "C", "install", ImportStatus.NotRun)])));
        Assert.EndsWith("\nrefused\n", Text(Result(ApplyStatus.Refused, false)));
    }

    [Fact]
    public void Text_cleans_every_name_and_the_message()
    {
        var evil = "x\n::error::boom";
        var r = new DeployApplyResult("1.2.3", true, evil, Hash, ApplyStatus.Failed, "m\r\n::error::m", "2026-11-04T10:00:00Z", ApplyExecutor.Action, new Approval(ApprovalMode.Action, "run 7\n::error::x"), [new(1, evil, evil + ".zip", "install", ImportStatus.Failed, 1)], [new(evil, evil, "on", evil)], []);

        var text = Text(r);

        Assert.DoesNotContain('\r', text);
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.DoesNotMatch(@"^\s*::", l));
        Assert.Contains("Target: x ::error::boom", text);
        Assert.Contains("failed: m  ::error::m", text);
        Assert.Contains("Approval: action, receipt run 7 ::error::x\n", text);
        Assert.Equal(8, text.Count(c => c == '\n'));
    }

    [Fact]
    public void Json_writes_the_time_the_executor_and_the_approval_object_after_the_message()
    {
        var json = Json(Result(ApplyStatus.Imported, true, "m", appliedAt: "2026-11-04T10:00:00Z", executor: ApplyExecutor.Action, approval: new Approval(ApprovalMode.Action, "run 7 attempt 1")));
        var form = Json(Result(ApplyStatus.Imported, true, appliedAt: "2026-11-04T10:00:00Z", executor: ApplyExecutor.Mcp, approval: new Approval(ApprovalMode.Form, null)));

        Assert.Contains("  \"message\": \"m\",\n  \"appliedAt\": \"2026-11-04T10:00:00Z\",\n  \"executor\": \"action\",\n  \"approval\": {\n    \"mode\": \"action\",\n    \"receipt\": \"run 7 attempt 1\"\n  },\n  \"solutions\"", json);
        Assert.Contains("  \"executor\": \"mcp\",\n  \"approval\": {\n    \"mode\": \"form\",\n    \"receipt\": null\n  },\n", form);
    }

    [Fact]
    public void Text_of_a_dry_run_has_the_executor_line_only()
    {
        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\nExecutor: cli\ndry run: would import 0 solutions\n", Text(Result(ApplyStatus.Planned, false)));
    }

    [Theory]
    [InlineData(ApplyExecutor.Cli, ApprovalMode.Cli, null, "Executor: cli\nApproval: cli\n")]
    [InlineData(ApplyExecutor.Mcp, ApprovalMode.Form, null, "Executor: mcp\nApproval: form\n")]
    [InlineData(ApplyExecutor.Action, ApprovalMode.Action, "run 7 attempt 2, environment test", "Executor: action\nApproval: action, receipt run 7 attempt 2, environment test\n")]
    public void Text_of_an_apply_has_the_executor_the_approval_and_the_time_after_the_hash(string executor, string mode, string? receipt, string lines)
    {
        var text = Text(Result(ApplyStatus.Imported, true, appliedAt: "2026-11-04T10:00:00Z", executor: executor, approval: new Approval(mode, receipt)));

        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\n{lines}Applied at: 2026-11-04T10:00:00Z\napplied\n", text);
    }

    [Fact]
    public void Text_of_a_refusal_with_an_approval_has_no_time()
    {
        var text = Text(Result(ApplyStatus.Refused, true, "no", approval: new Approval(ApprovalMode.Cli, null)));

        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\nExecutor: cli\nApproval: cli\nrefused: no\n", text);
    }

    [Fact]
    public void Text_has_one_line_per_agent_after_the_flow_lines_in_each_state()
    {
        var text = Text(Result(
            ApplyStatus.Imported, true, solutions: [Sol(1, "A", "install", ImportStatus.Ok)], flows: [new("A", "Notify", "on", "on")],
            agents: [new("A", "a_One", AgentState.Found, true), new("A", "a_Two", AgentState.Found, false), new("A", "a_Three", AgentState.Missing, null), new("A", "a_Four", AgentState.NotRead, null)]));

        Assert.EndsWith(
            "flow A/Notify: expected on, now on\nagent A/a_One: found, publishedon set\nagent A/a_Two: found, publishedon empty\nagent A/a_Three: missing\nagent A/a_Four: not read\napplied\n",
            text);
        Assert.EndsWith(
            "agent A/a_One: in the import; the import may leave it published\ndry run: would import 0 solutions\n",
            Text(Result(ApplyStatus.Planned, false, agents: [new("A", "a_One", null, null)])));
    }

    [Fact]
    public void Text_cleans_an_agent_name_and_its_solution()
    {
        var text = Text(Result(ApplyStatus.Imported, true, agents: [new("S\n::error::s", "n\n::error::n", AgentState.Missing, null)]));

        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.DoesNotMatch(@"^\s*::", l));
        Assert.Contains("agent S ::error::s/n ::error::n: missing\n", text);
    }

    [Fact]
    public void Json_writes_the_agents_after_the_flows_with_the_solution_name_state_and_publishedOn_in_that_order()
    {
        var json = Json(Result(ApplyStatus.Imported, true, flows: [new("A", "Notify", "on", "on")], agents: [new("A", "a_One", AgentState.Found, true), new("A", "a_Two", AgentState.Missing, null)]));

        Assert.Contains(
            "  \"flows\": [\n    {\n      \"solution\": \"A\",\n      \"name\": \"Notify\",\n      \"expectedState\": \"on\",\n      \"stateAfter\": \"on\"\n    }\n  ],\n  \"agents\": [\n    {\n      \"solution\": \"A\",\n      \"name\": \"a_One\",\n      \"state\": \"found\",\n      \"publishedOn\": true\n    },\n    {\n      \"solution\": \"A\",\n      \"name\": \"a_Two\",\n      \"state\": \"missing\",\n      \"publishedOn\": null\n    }\n  ]\n}\n",
            json);
        Assert.Contains("\"agents\": []\n}\n", Json(Result(ApplyStatus.Refused, false)));
    }
}