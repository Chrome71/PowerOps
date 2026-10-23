using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Model;
using PowerOps.Core.Settings;
using PowerOps.Core.Tests.Rules;

namespace PowerOps.Core.Tests.Deploy;

public class DeployApplyTests
{
    private const string H1 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string H2 = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210";
    private static readonly Guid G1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid G2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid G3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static PlanInputs Inputs(ZipInput[]? zips = null, DependencyInput[]? deps = null) => new(zips ?? [], deps ?? []);

    private static ZipInput Zip(string name = "a.zip", string zip = H1, string? settings = null, string? settingsHash = null, string snapshot = H1) =>
        new(name, zip, settings, settingsHash, snapshot);

    // ---- ChangedInput ------------------------------------------------------------------------------------------

    [Fact]
    public void Equal_inputs_change_nothing()
    {
        var inputs = Inputs([Zip(), Zip("b.zip", settings: "b.settings.json", settingsHash: H2)], [new DependencyInput("dep", "1.0.0.0"), new DependencyInput("gone", null)]);

        Assert.Null(DeployApply.ChangedInput(inputs, inputs with { }));
        Assert.Equal("the target environment, or the plan file's hash, differs", DeployApply.InputsEqualMessage);
    }

    [Theory]
    [MemberData(nameof(Changes))]
    public void The_first_difference_is_named_in_the_plans_order(string expected, PlanInputs reviewed, PlanInputs now) =>
        Assert.Equal(expected, DeployApply.ChangedInput(reviewed, now));

    public static TheoryData<string, PlanInputs, PlanInputs> Changes() => new()
    {
        { "the zip b.zip is not in the reviewed plan", Inputs([Zip()]), Inputs([Zip(), Zip("b.zip")]) },
        { "the zip b.zip of the reviewed plan is gone", Inputs([Zip(), Zip("b.zip")]), Inputs([Zip()]) },
        { "the zip a.zip changed since the plan", Inputs([Zip()]), Inputs([Zip(zip: H2)]) },
        { "the settings file of a.zip was added since the plan", Inputs([Zip()]), Inputs([Zip(settings: "s.json", settingsHash: H2)]) },
        { "the settings file of a.zip was removed since the plan", Inputs([Zip(settings: "s.json", settingsHash: H2)]), Inputs([Zip()]) },
        { "the settings file of a.zip changed since the plan", Inputs([Zip(settings: "s.json", settingsHash: H2)]), Inputs([Zip(settings: "s.json", settingsHash: H1)]) },
        { "the settings file of a.zip changed since the plan", Inputs([Zip(settings: "s.json", settingsHash: H2)]), Inputs([Zip(settings: "t.json", settingsHash: H2)]) },
        { "the target's state of the solution in a.zip changed since the plan", Inputs([Zip()]), Inputs([Zip(snapshot: H2)]) },
        { "the installed version of the dependency dep changed since the plan", Inputs(deps: [new DependencyInput("dep", "1.0.0.0")]), Inputs(deps: [new DependencyInput("dep", "1.0.0.1")]) },
        { "the installed version of the dependency dep changed since the plan", Inputs(deps: [new DependencyInput("dep", null)]), Inputs(deps: [new DependencyInput("dep", "1.0.0.1")]) },
        { "the dependency dep is not in the reviewed plan", Inputs(), Inputs(deps: [new DependencyInput("dep", null)]) },
        { "the dependency dep of the reviewed plan is no longer read", Inputs(deps: [new DependencyInput("dep", null)]), Inputs() },

        // The order: zips, then settings files, then the target's state, then dependencies.
        {
            "the zip b.zip changed since the plan",
            Inputs([Zip(), Zip("b.zip")], [new DependencyInput("dep", "1")]),
            Inputs([Zip(settings: "s", settingsHash: H2, snapshot: H2), Zip("b.zip", zip: H2)], [new DependencyInput("dep", "2")])
        },
        {
            "the settings file of b.zip was added since the plan",
            Inputs([Zip(), Zip("b.zip")], [new DependencyInput("dep", "1")]),
            Inputs([Zip(snapshot: H2), Zip("b.zip", settings: "s", settingsHash: H2)], [new DependencyInput("dep", "2")])
        },
        {
            "the target's state of the solution in a.zip changed since the plan",
            Inputs([Zip()], [new DependencyInput("dep", "1")]),
            Inputs([Zip(snapshot: H2)], [new DependencyInput("dep", "2")])
        },
    };

    // ---- Imports / FlowIds / FlowsAfter ------------------------------------------------------------------------

    private static DeployZip DZip(string name, bool managed = true, string? settings = null, params Workflow[] flows) =>
        new(name + ".zip", H1, B.Empty() with { UniqueName = name, Managed = managed, Workflows = flows }, settings, settings is null ? null : H2, settings is null ? null : "{}");

    private static PlannedSolution Planned(int order, DeployZip zip, string action, params PlannedFlow[] flows) =>
        new(order, zip.Solution.UniqueName, zip.File, zip.SettingsFile, "1.0.0.0", zip.Solution.Managed, action, null, [], flows);

    private static DeployPlanResult Plan(params PlannedSolution[] solutions) =>
        new("Test", H1, false, Inputs(), solutions, []);

    [Fact]
    public void Imports_follow_the_plan_order_and_skip_nothing_but_skips()
    {
        var a = DZip("A", managed: true, settings: "A.settings.json");
        var b = DZip("B", managed: true);
        var c = DZip("C", managed: false);
        var d = DZip("D", managed: true);
        var e = DZip("E", managed: true, settings: "E.settings.json");
        var plan = Plan(Planned(1, d, PlanAction.Skip), Planned(2, a, PlanAction.Install), Planned(3, b, PlanAction.Upgrade), Planned(4, c, PlanAction.Upgrade), Planned(5, e, PlanAction.Upgrade));

        var imports = DeployApply.Imports(plan, [a, b, c, d, e]);

        Assert.Equal(
            [
                new ImportStep(2, "A", "A.zip", "A.settings.json", false),
                new ImportStep(3, "B", "B.zip", null, true),
                new ImportStep(4, "C", "C.zip", null, false),
                new ImportStep(5, "E", "E.zip", "E.settings.json", true),
            ],
            imports);
    }

    [Fact]
    public void A_managed_install_is_not_staged_and_a_plan_of_skips_has_no_imports()
    {
        var a = DZip("A");

        Assert.Equal([new ImportStep(1, "A", "A.zip", null, false)], DeployApply.Imports(Plan(Planned(1, a, PlanAction.Install)), [a]));
        Assert.Empty(DeployApply.Imports(Plan(Planned(1, a, PlanAction.Skip)), [a]));
    }

    [Fact]
    public void Imports_of_a_plan_whose_zip_is_not_loaded_are_unreadable()
    {
        var a = DZip("A");

        var e = Assert.Throws<InvalidDataException>(() => DeployApply.Imports(Plan(Planned(1, a, PlanAction.Install)), []));

        Assert.Contains("A.zip", e.Message);
    }

    private static PlannedFlow Flow(string name, string expected, string? before = "off") => new(name, "on", before is not null, before, expected);

    [Fact]
    public void Two_flows_with_the_same_name_get_their_own_ids_and_states()
    {
        var a = DZip("A", flows: [B.Flow(G2.ToString(), "Same"), B.Flow(G1.ToString(), "Same"), B.Flow(G3.ToString(), "Other", category: 6)]);
        var plan = Plan(Planned(1, a, PlanAction.Upgrade, Flow("Same", "off"), Flow("Same", "on")));

        var flows = DeployApply.FlowIds(plan, [a]);

        // Cloud flows sort by name, then id: so G1 is the first "Same" and G2 the second.
        Assert.Equal([G1, G2], flows.Select(f => f.Id));
        Assert.Equal(["off", "on"], flows.Select(f => f.Flow.ExpectedState));

        var after = DeployApply.FlowsAfter(flows, [new Snapshot("Test", "A", true, "1.0.0.0", true, true, [], [], [], [new SnapshotFlow(G2, "Same", "on"), new SnapshotFlow(G1, "Same", "off")])]);

        Assert.True(after.AsExpected);
        Assert.Equal([new AppliedFlow("A", "Same", "off", "off"), new AppliedFlow("A", "Same", "on", "on")], after.Flows);
    }

    [Fact]
    public void A_skipped_solution_has_no_flows_and_flows_pair_per_solution()
    {
        var a = DZip("A", flows: [B.Flow(G1.ToString(), "One")]);
        var b = DZip("B", flows: [B.Flow(G2.ToString(), "Two")]);
        var plan = Plan(Planned(1, a, PlanAction.Skip), Planned(2, b, PlanAction.Install, Flow("Two", "on", null)));

        var flows = DeployApply.FlowIds(plan, [a, b]);

        Assert.Equal([("B", G2)], flows.Select(f => (f.Solution, f.Id)));
    }

    [Fact]
    public void A_plan_with_another_number_of_flows_than_the_zip_is_unreadable()
    {
        var a = DZip("A", flows: [B.Flow(G1.ToString(), "One")]);

        Assert.Throws<InvalidDataException>(() => DeployApply.FlowIds(Plan(Planned(1, a, PlanAction.Install, Flow("One", "on"), Flow("Two", "on"))), [a]));
    }

    [Fact]
    public void A_flow_expected_off_that_is_off_is_as_expected_and_a_flow_that_is_missing_or_other_is_not()
    {
        var a = DZip("A", flows: [B.Flow(G1.ToString(), "A1"), B.Flow(G2.ToString(), "A2"), B.Flow(G3.ToString(), "A3")]);
        var flows = DeployApply.FlowIds(Plan(Planned(1, a, PlanAction.Upgrade, Flow("A1", "off"), Flow("A2", "on"), Flow("A3", "on"))), [a]);

        var after = DeployApply.FlowsAfter(flows, [new Snapshot("Test", "a", true, "1.0.0.0", true, true, [], [], [], [new SnapshotFlow(G1, "A1", "off"), new SnapshotFlow(G2, "A2", "suspended")])]);

        Assert.False(after.AsExpected);
        Assert.Equal(["off", "suspended", FlowState.Missing], after.Flows.Select(f => f.StateAfter));
    }

    [Fact]
    public void A_flow_of_a_solution_the_second_read_does_not_hold_is_missing()
    {
        var a = DZip("A", flows: [B.Flow(G1.ToString(), "A1")]);
        var flows = DeployApply.FlowIds(Plan(Planned(1, a, PlanAction.Install, Flow("A1", "on", null))), [a]);

        var after = DeployApply.FlowsAfter(flows, []);

        Assert.False(after.AsExpected);
        Assert.Equal(FlowState.Missing, Assert.Single(after.Flows).StateAfter);
    }

    [Fact]
    public void No_planned_flows_are_as_expected()
    {
        Assert.True(DeployApply.FlowsAfter([], []).AsExpected);
    }

    [Fact]
    public void The_status_names_are_the_reports_words()
    {
        Assert.Equal(["refused", "planned", "imported", "failed"], [ApplyStatus.Refused, ApplyStatus.Planned, ApplyStatus.Imported, ApplyStatus.Failed]);
        Assert.Equal(["ok", "failed", "not-run", "planned", "skipped"], [ImportStatus.Ok, ImportStatus.Failed, ImportStatus.NotRun, ImportStatus.Planned, ImportStatus.Skipped]);
    }
}