using System.Text;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Model;
using PowerOps.Core.Settings;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Deploy;

public class DeployPlanTests
{
    private static readonly Guid TargetId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid FlowId = Guid.Parse("4c8dd007-dfcb-f111-a05c-0022480697a8");
    private static readonly Guid G1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid G2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid G3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static string Hash(string text) => DeployPlan.Sha256Hex(Encoding.UTF8.GetBytes(text));

    private static Solution Sol(string name, string version = "1.0.0.0", bool managed = true, params MissingDependency[] deps) =>
        B.Empty() with { UniqueName = name, Version = version, Managed = managed, MissingDependencies = deps };

    private static DeployZip Zip(Solution s, string? settings = null) =>
        new(s.UniqueName + ".zip", Hash("zip " + s.UniqueName), s,
            settings is null ? null : s.UniqueName + ".settings.json", settings is null ? null : Hash(settings), settings);

    /// <summary>A snapshot of <paramref name="name"/> installed in the target at <paramref name="version"/>; not installed when the version is null.</summary>
    private static Snapshot Snap(
        string name, string? version = "1.0.0.0", bool managed = true, bool layersRead = true,
        SnapshotComponent[]? components = null, SnapshotVariable[]? variables = null, SnapshotReference[]? references = null, SnapshotFlow[]? flows = null) =>
        version is null
            ? new("Test", name, false, null, null, true, [], [], [], [])
            : new("Test", name, true, version, managed, layersRead, components ?? [], variables ?? [], references ?? [], flows ?? []);

    private static DeployPlanResult Plan(DeployZip[] zips, Snapshot[]? snaps = null, Dictionary<string, string?>? deps = null) =>
        DeployPlan.Build(zips, Target(zips, snaps, deps));

    private static DeployTarget Target(DeployZip[] zips, Snapshot[]? snaps = null, Dictionary<string, string?>? deps = null)
    {
        var given = snaps ?? [];
        return new("Test", TargetId,
            [.. given, .. zips.Where(z => !given.Any(s => s.Solution == z.Solution.UniqueName)).Select(z => Snap(z.Solution.UniqueName, null))],
            deps ?? new Dictionary<string, string?>());
    }

    private static DeployPlanResult Plan(Solution s, Snapshot? snap = null, string? settings = null) =>
        Plan([Zip(s, settings)], snap is null ? null : [snap]);

    private static PlanItem[] Of(DeployPlanResult r, string kind) => r.Items.Where(i => i.Kind == kind).ToArray();

    private static PlanItem One(DeployPlanResult r, string kind) => Assert.Single(Of(r, kind));

    private static MissingDependency Dep(string? owner, string schema = "x_comp") => B.Dep("1", schema, null, owner);

    // ---- names -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Name (1.0.2.77)", "Name")]
    [InlineData("  Name  (1.0)  ", "Name")]
    [InlineData("Name", "Name")]
    [InlineData("Name (", "Name (")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void RequiredSolution_removes_the_version_suffix(string? attribute, string? expected) =>
        Assert.Equal(expected, DeployPlan.RequiredSolution(attribute));

    [Fact]
    public void TargetDependencies_are_valid_names_outside_the_zips_distinct_and_sorted()
    {
        var a = Sol("A", deps: [Dep("Zed (1.0)"), Dep("b_dep (1.0)"), Dep("B_DEP (2.0)"), Dep("A (1.0)"), Dep(null), Dep("Bad-Name (1.0)"), Dep("Other (1.0)")]);
        var other = Sol("other");

        Assert.Equal(["b_dep", "Zed"], DeployPlan.TargetDependencies([a, other]));
    }

    // ---- order -------------------------------------------------------------------------------------------------

    private static string[] Order(DeployPlanResult r) => r.Solutions.Select(s => s.Solution).ToArray();

    [Fact]
    public void Order_puts_dependencies_first_and_breaks_ties_by_name_ignoring_case()
    {
        var a = Sol("A", deps: Dep("C (1.0)"));
        var b = Sol("b");
        var c = Sol("C");

        var r = Plan([Zip(a), Zip(c), Zip(b)]);

        Assert.Equal(["b", "C", "A"], Order(r));
        Assert.Equal([1, 2, 3], r.Solutions.Select(s => s.Order));
        Assert.Equal(["C"], r.Solutions[2].DependsOn);
        Assert.Empty(Of(r, PlanKind.Cycle));
    }

    [Fact]
    public void A_cycle_and_what_depends_on_it_come_last_in_name_order_each_with_a_cycle_item()
    {
        var a = Sol("A", deps: Dep("B (1.0)"));
        var b = Sol("B", deps: Dep("A (1.0)"));
        var c = Sol("C", deps: Dep("A (1.0)"));
        var d = Sol("D");

        var r = Plan([Zip(c), Zip(b), Zip(d), Zip(a)]);

        Assert.Equal(["D", "A", "B", "C"], Order(r));
        var cycle = Of(r, PlanKind.Cycle);
        Assert.Equal(["A", "B", "C"], cycle.Select(i => i.Solution));
        Assert.All(cycle, i => Assert.Equal((PlanSeverity.Block, true), (i.Severity, i.Message.Contains("A, B, C"))));
        Assert.True(r.Blocked);
    }

    [Fact]
    public void Two_zips_that_depend_on_each_other_give_one_cycle_item_each_and_no_missing_dependency()
    {
        var r = Plan([Zip(Sol("A", deps: Dep("B (1.0)"))), Zip(Sol("B", deps: Dep("A (1.0)")))]);

        Assert.Equal(["A", "B"], Of(r, PlanKind.Cycle).Select(i => i.Solution));
        Assert.Empty(Of(r, PlanKind.MissingDependency));
    }

    // ---- dependencies ------------------------------------------------------------------------------------------

    [Fact]
    public void A_dependency_installed_in_the_target_or_in_an_earlier_zip_is_met()
    {
        var app = Sol("App", deps: [Dep("Core (1.0)"), Dep("Lib (1.0)")]);
        var core = Sol("Core");

        var r = Plan([Zip(app), Zip(core)], deps: new() { ["Lib"] = "2.0.0.0" });

        Assert.Empty(Of(r, PlanKind.MissingDependency));
        Assert.False(r.Blocked);
    }

    [Fact]
    public void A_dependency_installed_in_the_target_as_a_folder_zip_is_met_even_when_later_in_the_order()
    {
        var a = Sol("A", deps: Dep("B (1.0)"));
        var b = Sol("B", deps: Dep("A (1.0)"));

        var r = Plan([Zip(a), Zip(b)], [Snap("B")]);

        Assert.Empty(Of(r, PlanKind.MissingDependency));
        Assert.Equal(2, Of(r, PlanKind.Cycle).Length);
    }

    [Fact]
    public void A_dependency_in_neither_place_blocks_and_names_the_dependency()
    {
        var r = Plan([Zip(Sol("App", deps: Dep("Lib (1.0)")))], deps: new() { ["Lib"] = null });

        var item = One(r, PlanKind.MissingDependency);
        Assert.Equal(("App", PlanSeverity.Block, "Lib"), (item.Solution, item.Severity, item.Subject));
        Assert.True(r.Blocked);
    }

    [Fact]
    public void A_required_component_with_no_solution_or_a_bad_name_is_a_warning_with_the_component_as_subject()
    {
        var r = Plan(Sol("App", deps: [B.Dep("1", "x_table", null, null), B.Dep("29", null, "abc", "Bad-Name (1.0)"), B.Dep("29", null, "abc", "Bad-Name (1.0)")]));

        Assert.Equal(["1:x_table", "29:abc"], Of(r, PlanKind.DependencyNotChecked).Select(i => i.Subject));
        Assert.All(Of(r, PlanKind.DependencyNotChecked), i => Assert.Equal(PlanSeverity.Warning, i.Severity));
        Assert.Empty(Of(r, PlanKind.MissingDependency));
        Assert.False(r.Blocked);
        Assert.Empty(r.Solutions[0].DependsOn);
    }

    [Fact]
    public void A_dependency_on_itself_is_ignored()
    {
        var r = Plan(Sol("App", deps: Dep("app (1.0)")));

        Assert.Empty(r.Items);
        Assert.Empty(r.Solutions[0].DependsOn);
    }

    // ---- actions -----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null, "1.0.0.0", PlanAction.Install)]
    [InlineData("1.0.0.0", "1.0.0.1", PlanAction.Upgrade)]
    [InlineData("1.9.0.0", "1.10.0.0", PlanAction.Upgrade)]
    [InlineData("1.0.0.0", "1.0.0.0", PlanAction.Skip)]
    [InlineData("2.0.0.0", "1.99.0.0", PlanAction.Skip)]
    public void The_action_follows_the_versions_compared_part_by_part(string? installed, string version, string action)
    {
        var r = Plan(Sol("App", version), Snap("App", installed));

        var s = Assert.Single(r.Solutions);
        Assert.Equal(action, s.Action);
        Assert.Equal(version, s.Version);
        Assert.Equal(installed is null ? null : new InstalledState(installed, true, true), s.Installed);
    }

    [Fact]
    public void A_solution_carries_its_zip_settings_file_and_managed_flag()
    {
        var r = Plan(Sol("App", managed: false), settings: "{}");

        var s = Assert.Single(r.Solutions);
        Assert.Equal(("App.zip", "App.settings.json", false), (s.Zip, s.Settings, s.Managed));
    }

    [Fact]
    public void A_skip_checks_no_settings_dependencies_or_flows_but_an_upgrade_does()
    {
        var withProblems = Sol("App", "2.0.0.0") with
        {
            MissingDependencies = [Dep("Lib (1.0)"), B.Dep("1", "x", null, null)],
            ConnectionReferences = [B.Ref("demo_ref")],
            Workflows = [B.Flow(FlowId.ToString())],
        };

        var skipped = Plan(withProblems with { Version = "0.9.0.0" }, Snap("App"));
        var upgraded = Plan(withProblems, Snap("App"));

        Assert.Empty(skipped.Items);
        Assert.Empty(skipped.Solutions[0].Flows);
        Assert.False(skipped.Blocked);
        Assert.Equal(
            [PlanKind.MissingDependency, PlanKind.DependencyNotChecked, PlanKind.Settings],
            upgraded.Items.Select(i => i.Kind));
        Assert.Single(upgraded.Solutions[0].Flows);
        Assert.True(upgraded.Blocked);
    }

    // ---- managed conflict --------------------------------------------------------------------------------------

    [Theory]
    [InlineData(true, false, "2.0.0.0")]
    [InlineData(false, true, "2.0.0.0")]
    [InlineData(true, false, "1.0.0.0")]
    [InlineData(false, true, "0.5.0.0")]
    public void Managed_over_unmanaged_and_the_other_way_round_blocks_for_every_action(bool zipManaged, bool installedManaged, string version)
    {
        var r = Plan(Sol("App", version, zipManaged), Snap("App", managed: installedManaged));

        var item = One(r, PlanKind.ManagedConflict);
        Assert.Equal(("App", PlanSeverity.Block), (item.Solution, item.Severity));
        Assert.True(r.Blocked);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Equal_managed_flags_and_a_fresh_install_are_quiet(bool zipManaged, bool installedManaged)
    {
        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0", zipManaged), Snap("App", managed: installedManaged)), PlanKind.ManagedConflict));
        Assert.Empty(Of(Plan(Sol("App", managed: zipManaged)), PlanKind.ManagedConflict));
    }

    // ---- bad input ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.0.0.0.0")]
    [InlineData("1.0.0.x")]
    [InlineData("1.0.0.-1")]
    [InlineData(" 1.0.0.0")]
    public void A_zip_version_that_is_not_four_numeric_parts_throws_naming_the_zip(string version)
    {
        var e = Assert.Throws<InvalidDataException>(() => Plan(Sol("App", version)));

        Assert.Contains("App.zip", e.Message);
    }

    [Fact]
    public void A_target_version_that_is_not_four_numeric_parts_throws_naming_the_zip()
    {
        var e = Assert.Throws<InvalidDataException>(() => Plan(Sol("App"), Snap("App", "1.0")));

        Assert.Contains("App.zip", e.Message);
        Assert.Throws<InvalidDataException>(() => Plan(Sol("App"), Snap("App", "1.0.0.0") with { Version = null }));
    }

    [Theory]
    [InlineData("Bad-Name")]
    [InlineData("Bad Name")]
    [InlineData("")]
    public void A_unique_name_that_fails_the_pattern_throws_naming_the_zip(string name)
    {
        var s = Sol(name);
        var zip = new DeployZip("Some.zip", "0", s, null, null, null);

        var e = Assert.Throws<InvalidDataException>(() => DeployPlan.Build([zip], new DeployTarget("Test", TargetId, [], new Dictionary<string, string?>())));

        Assert.Contains("Some.zip", e.Message);
    }

    [Fact]
    public void A_cloud_flow_id_that_is_not_a_guid_throws_but_another_category_is_not_checked()
    {
        var e = Assert.Throws<InvalidDataException>(() => Plan(Sol("App") with { Workflows = [B.Flow("not-a-guid")] }));
        Assert.Contains("App.zip", e.Message);
        Assert.DoesNotContain("not-a-guid", e.Message);

        Plan(Sol("App") with { Workflows = [B.Flow("not-a-guid", category: 0)] });
    }

    // ---- deletes -----------------------------------------------------------------------------------------------

    private static Snapshot Installed(params SnapshotComponent[] components) => Snap("App", components: components);

    [Fact]
    public void A_managed_upgrade_deletes_a_flow_a_variable_and_a_reference_the_zip_no_longer_has()
    {
        var snap = Snap("App",
            components: [new(29, FlowId, null), new(29, G1, null), new(380, G2, null), new(61, G3, null)],
            variables: [new("demo_Gone", null), new("demo_Kept", null)],
            references: [new(G3, "demo_gone_ref", "c", true), new(G2, "demo_kept_ref", "c", true)],
            flows: [new(FlowId, "Notify", "on")]);
        var zip = Sol("App", "2.0.0.0") with
        {
            Workflows = [B.Flow(G2.ToString(), "Other", category: 0)],
            EnvironmentVariables = [B.Env("DEMO_KEPT")],
            ConnectionReferences = [B.Ref("DEMO_KEPT_REF")],
        };

        var items = Of(Plan(zip, snap), PlanKind.Deletes);

        Assert.Equal(["29 00000000-0000-0000-0000-000000000001", "Notify", "demo_Gone", "demo_gone_ref"], items.Select(i => i.Subject));
        Assert.All(items, i => Assert.Equal((PlanSeverity.Warning, "App"), (i.Severity, i.Solution)));
    }

    [Fact]
    public void Deletes_are_quiet_for_an_unmanaged_zip_a_skip_an_install_and_matching_names()
    {
        var snap = Snap("App", managed: false, components: [new(29, FlowId, null)], variables: [new("demo_Gone", null)], flows: [new(FlowId, "Notify", "on")]);
        var managedSnap = snap with { Managed = true };

        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0", managed: false), snap), PlanKind.Deletes));
        Assert.Empty(Of(Plan(Sol("App", "1.0.0.0"), managedSnap), PlanKind.Deletes));
        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0")), PlanKind.Deletes));
        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0") with
        {
            Workflows = [B.Flow(FlowId.ToString().ToUpperInvariant())],
            EnvironmentVariables = [B.Env("DEMO_GONE")],
        }, managedSnap), PlanKind.Deletes));
    }

    [Fact]
    public void More_tables_or_canvas_apps_in_the_target_than_root_components_in_the_zip_is_one_item_with_the_difference()
    {
        var snap = Installed(new SnapshotComponent(1, G1, null), new(1, G2, null), new(1, G3, null), new(300, G1, null), new(300, G2, null));
        var zip = Sol("App", "2.0.0.0") with { RootComponents = [new("1", "demo_a", null), new("300", "demo_app", null), new("300", "demo_app2", null)] };

        var items = Of(Plan(zip, snap), PlanKind.Deletes);

        Assert.Equal(["2 table(s)"], items.Select(i => i.Subject));
        Assert.Contains("data", items[0].Message);

        var canvas = Of(Plan(zip with { RootComponents = [new("1", "a", null), new("1", "b", null), new("1", "c", null), new("300", "x", null)] }, snap), PlanKind.Deletes);
        Assert.Equal(["1 canvas app(s)"], canvas.Select(i => i.Subject));
        Assert.DoesNotContain("table", canvas[0].Message);
        Assert.Contains("table", items[0].Message);
    }

    [Fact]
    public void Equal_or_lower_counts_of_tables_and_canvas_apps_are_quiet()
    {
        var snap = Installed(new SnapshotComponent(1, G1, null), new(300, G1, null));
        var zip = Sol("App", "2.0.0.0") with { RootComponents = [new("1", "a", null), new("1", "b", null), new("300", "x", null)] };

        Assert.Empty(Of(Plan(zip, snap), PlanKind.Deletes));
    }

    // ---- components-not-checked --------------------------------------------------------------------------------

    [Fact]
    public void Components_outside_the_checked_set_give_an_info_item_with_their_count_and_types()
    {
        var snap = Snap("App",
            components: [new(61, G1, null), new(61, G2, null), new(2, G3, null), new(1, G1, null), new(300, G2, null), new(29, G2, null), new(380, G3, null), new(381, G3, null), new(20, G1, null)],
            references: [new(G1, "demo_ref", "c", true)]);

        var item = One(Plan(Sol("App", "2.0.0.0") with { RootComponents = [new("1", "a", null), new("300", "b", null)] }, snap), PlanKind.ComponentsNotChecked);

        // The components with G1 as id are the reference's, whatever their type; the two left are of types 61 and 2.
        Assert.Equal((PlanSeverity.Info, "2"), (item.Severity, item.Subject));
        Assert.Contains("2, 61", item.Message);
    }

    [Fact]
    public void The_checked_set_alone_and_the_other_actions_give_no_components_not_checked()
    {
        var checkedOnly = Snap("App", components: [new(1, G1, null), new(300, G2, null), new(29, G3, null), new(380, G3, null), new(381, G3, null), new(2, G2, null)],
            references: [new(G2, "demo_ref", "c", true)]);
        var other = Installed(new SnapshotComponent(61, G1, null));
        var zip = Sol("App", "2.0.0.0") with { RootComponents = [new("1", "a", null), new("300", "b", null)] };

        Assert.Empty(Of(Plan(zip, checkedOnly), PlanKind.ComponentsNotChecked));
        Assert.Empty(Of(Plan(zip with { Managed = false }, other with { Managed = false }), PlanKind.ComponentsNotChecked));
        Assert.Empty(Of(Plan(zip with { Version = "1.0.0.0" }, other), PlanKind.ComponentsNotChecked));
        Assert.Empty(Of(Plan(zip), PlanKind.ComponentsNotChecked));
    }

    // ---- layers ------------------------------------------------------------------------------------------------

    [Fact]
    public void An_unmanaged_layer_is_named_by_its_flow_or_its_component_and_only_for_a_managed_target_upgrade()
    {
        var snap = Snap("App", components: [new(29, FlowId, true), new(61, G1, true), new(61, G2, false), new(61, G3, null)], flows: [new(FlowId, "Notify", "on")]);

        var r = Plan(Sol("App", "2.0.0.0"), snap);

        Assert.Equal(["61 00000000-0000-0000-0000-000000000001", "Notify"], Of(r, PlanKind.UnmanagedLayer).Select(i => i.Subject));
        Assert.All(Of(r, PlanKind.UnmanagedLayer), i => Assert.Equal(PlanSeverity.Warning, i.Severity));
        Assert.Empty(Of(r, PlanKind.LayersNotRead));
    }

    [Fact]
    public void An_unmanaged_layer_is_quiet_for_an_unmanaged_target_and_for_a_skip()
    {
        var snap = Snap("App", components: [new(61, G1, true)], managed: false);

        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0", managed: false), snap), PlanKind.UnmanagedLayer));
        Assert.Empty(Of(Plan(Sol("App", "1.0.0.0"), snap with { Managed = true }), PlanKind.UnmanagedLayer));
    }

    [Fact]
    public void Layers_not_read_is_information_for_a_managed_target_upgrade_only()
    {
        var snap = Snap("App", layersRead: false);

        var item = One(Plan(Sol("App", "2.0.0.0"), snap), PlanKind.LayersNotRead);
        Assert.Equal((PlanSeverity.Info, "App"), (item.Severity, item.Subject));
        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0"), Snap("App")), PlanKind.LayersNotRead));
        Assert.Empty(Of(Plan(Sol("App", "1.0.0.0"), snap), PlanKind.LayersNotRead));
        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0", managed: false), snap with { Managed = false }), PlanKind.LayersNotRead));
        Assert.Empty(Of(Plan(Sol("App", "2.0.0.0")), PlanKind.LayersNotRead));
        Assert.False(Plan(Sol("App", "2.0.0.0"), snap).Blocked);
    }

    // ---- settings ----------------------------------------------------------------------------------------------

    private static Solution WithRef() => Sol("App") with
    {
        ConnectionReferences = [B.Ref("demo_ref")],
        EnvironmentVariables = [B.Env("demo_Var")],
    };

    [Fact]
    public void A_settings_file_with_problems_gives_one_blocking_item_per_problem()
    {
        var r = Plan(WithRef(), settings: """{"ConnectionReferences":[{"LogicalName":"demo_ref","ConnectionId":""}]}""");

        Assert.Equal(["demo_Var", "demo_ref"], Of(r, PlanKind.Settings).Select(i => i.Subject));
        Assert.All(Of(r, PlanKind.Settings), i => Assert.Equal(PlanSeverity.Block, i.Severity));
        Assert.Contains(DeploymentSettings.ConnectionReference, Of(r, PlanKind.Settings)[1].Message);
        Assert.True(r.Blocked);
    }

    [Fact]
    public void A_missing_settings_file_is_checked_as_an_empty_one()
    {
        var r = Plan(WithRef());

        Assert.Equal(["demo_Var", "demo_ref"], Of(r, PlanKind.Settings).Select(i => i.Subject));
        Assert.Null(r.Solutions[0].Settings);
        Assert.True(r.Blocked);
    }

    [Fact]
    public void A_complete_settings_file_and_a_solution_that_needs_none_are_quiet()
    {
        var ok = """{"EnvironmentVariables":[{"SchemaName":"demo_Var","Value":"x"}],"ConnectionReferences":[{"LogicalName":"demo_ref","ConnectionId":"c"}]}""";

        Assert.Empty(Of(Plan(WithRef(), settings: ok), PlanKind.Settings));
        Assert.Empty(Plan(Sol("App")).Items);
    }

    [Fact]
    public void A_settings_text_that_is_not_json_throws_naming_the_settings_file()
    {
        var e = Assert.Throws<InvalidDataException>(() => Plan(Sol("App"), settings: "nope"));

        Assert.StartsWith("App.settings.json", e.Message);
    }

    // ---- flows -------------------------------------------------------------------------------------------------

    [Fact]
    public void A_new_flow_comes_back_on_only_if_it_was_on_and_an_existing_flow_keeps_its_state()
    {
        var other = Guid.Parse("00000000-0000-0000-0000-0000000000f2");
        var zip = Sol("App", "2.0.0.0") with
        {
            Workflows = [B.Flow(FlowId.ToString(), "Existing", 1), B.Flow(other.ToString(), "Fresh", 1), B.Flow("00000000-0000-0000-0000-0000000000f3", "Draft", 0), B.Flow("00000000-0000-0000-0000-0000000000f4", "Plain", category: 0)],
        };
        var snap = Snap("App", flows: [new(FlowId, "Existing", "off")]);

        var flows = Plan(zip, snap).Solutions[0].Flows;

        Assert.Equal(
            [
                new PlannedFlow("Draft", "off", false, null, "off"),
                new PlannedFlow("Existing", "on", true, "off", "off"),
                new PlannedFlow("Fresh", "on", false, null, "on"),
            ],
            flows);
        Assert.Equal([new PlannedFlow("Existing", "on", true, "off", "off")], Plan(Sol("App", "2.0.0.0") with { Workflows = [B.Flow(FlowId.ToString(), "Existing", 1)] }, snap).Solutions[0].Flows);
    }

    [Fact]
    public void An_install_plans_every_flow_as_new()
    {
        var flows = Plan(Sol("App") with { Workflows = [B.Flow(FlowId.ToString(), "F", 2)] }).Solutions[0].Flows;

        Assert.Equal([new PlannedFlow("F", "suspended", false, null, "off")], flows);
    }

    // ---- same version differs ----------------------------------------------------------------------------------

    private const string ZipHas = "The zip has this {0} and the target's solution does not, and the plan skips the zip because the versions are equal.";
    private const string TargetHas = "The target's solution has this {0} and the zip does not, and the plan skips the zip because the versions are equal.";

    private static (string Subject, string Message)[] Differences(DeployPlanResult r) =>
        [.. Of(r, PlanKind.SameVersionDiffers).Select(i => (i.Subject, i.Message))];

    [Fact]
    public void A_flow_only_the_zip_has_or_only_the_target_has_is_an_information_item_for_a_same_version_skip()
    {
        var zipOnly = Plan(Sol("App") with { Workflows = [B.Flow(FlowId.ToString(), "Notify")] }, Snap("App"));
        var targetOnly = Plan(Sol("App"), Snap("App", flows: [new(FlowId, "Notify", "on")]));

        var item = One(zipOnly, PlanKind.SameVersionDiffers);
        Assert.Equal(("App", PlanSeverity.Info, "Notify", string.Format(ZipHas, "cloud flow")), (item.Solution, item.Severity, item.Subject, item.Message));
        Assert.Equal([("Notify", string.Format(TargetHas, "cloud flow"))], Differences(targetOnly));
        Assert.Equal(PlanAction.Skip, zipOnly.Solutions[0].Action);
    }

    [Fact]
    public void A_variable_only_one_side_has_gives_an_item_for_that_side()
    {
        var zipOnly = Plan(Sol("App") with { EnvironmentVariables = [B.Env("demo_Zip")] }, Snap("App"));
        var targetOnly = Plan(Sol("App"), Snap("App", variables: [new("demo_Target", null)]));

        Assert.Equal([("demo_Zip", string.Format(ZipHas, "environment variable"))], Differences(zipOnly));
        Assert.Equal([("demo_Target", string.Format(TargetHas, "environment variable"))], Differences(targetOnly));
    }

    [Fact]
    public void A_connection_reference_only_one_side_has_gives_an_item_for_that_side()
    {
        var zipOnly = Plan(Sol("App") with { ConnectionReferences = [B.Ref("demo_zip")] }, Snap("App"));
        var targetOnly = Plan(Sol("App"), Snap("App", references: [new(G1, "demo_target", "c", true)]));

        Assert.Equal([("demo_zip", string.Format(ZipHas, "connection reference"))], Differences(zipOnly));
        Assert.Equal([("demo_target", string.Format(TargetHas, "connection reference"))], Differences(targetOnly));
    }

    [Fact]
    public void Names_that_differ_only_by_case_and_a_flow_id_in_upper_case_give_no_item()
    {
        var zip = Sol("App") with
        {
            Workflows = [B.Flow(FlowId.ToString().ToUpperInvariant(), "Notify")],
            EnvironmentVariables = [B.Env("DEMO_VAR")],
            ConnectionReferences = [B.Ref("DEMO_REF")],
        };
        var snap = Snap("App", variables: [new("demo_var", null)], references: [new(G1, "demo_ref", "c", true)], flows: [new(FlowId, "Notify", "on")]);

        Assert.Empty(Plan(zip, snap).Items);
    }

    [Fact]
    public void A_flow_re_created_under_the_same_name_gives_the_zips_item_then_the_targets()
    {
        var zip = Sol("App") with { Workflows = [B.Flow(FlowId.ToString(), "Notify")] };
        var snap = Snap("App", flows: [new(G1, "Notify", "on")]);

        Assert.Equal([("Notify", string.Format(ZipHas, "cloud flow")), ("Notify", string.Format(TargetHas, "cloud flow"))], Differences(Plan(zip, snap)));
    }

    [Fact]
    public void Items_sort_after_the_other_kinds_and_keep_flows_variables_references_order_for_a_tie()
    {
        var zip = Sol("App", managed: true) with
        {
            Workflows = [B.Flow(FlowId.ToString(), "Same")],
            EnvironmentVariables = [B.Env("same")],
            ConnectionReferences = [B.Ref("same")],
        };
        var snap = Snap("App", managed: false, variables: [new("other", null)], references: [new(G1, "other", "c", true)], flows: [new(G2, "Same", "on")]);

        var r = Plan(zip, snap);

        Assert.Equal(
            [PlanKind.ManagedConflict, .. Enumerable.Repeat(PlanKind.SameVersionDiffers, 6)],
            r.Items.Select(i => i.Kind));
        // Subjects sort ordinally; a tie keeps the order the items were added in.
        Assert.Equal(
            [
                ("Same", string.Format(ZipHas, "cloud flow")), ("Same", string.Format(TargetHas, "cloud flow")),
                ("other", string.Format(TargetHas, "environment variable")), ("other", string.Format(TargetHas, "connection reference")),
                ("same", string.Format(ZipHas, "environment variable")), ("same", string.Format(ZipHas, "connection reference")),
            ],
            Differences(r));
    }

    [Fact]
    public void Equal_contents_a_lower_version_an_upgrade_and_an_install_give_no_item()
    {
        var zip = Sol("App") with { EnvironmentVariables = [B.Env("demo_Zip")] };
        var snap = Snap("App", variables: [new("demo_Target", null)]);

        Assert.Empty(Of(Plan(Sol("App"), Snap("App")), PlanKind.SameVersionDiffers));
        Assert.Empty(Of(Plan(zip with { Version = "0.9.0.0" }, snap), PlanKind.SameVersionDiffers));
        Assert.Empty(Of(Plan(zip with { Version = "2.0.0.0", Managed = true }, snap), PlanKind.SameVersionDiffers));
        Assert.Empty(Of(Plan(zip), PlanKind.SameVersionDiffers));
    }

    [Fact]
    public void Equal_numeric_versions_in_another_spelling_count_as_the_same_version()
    {
        var r = Plan(Sol("App", "1.0.0.00") with { EnvironmentVariables = [B.Env("demo_Zip")] }, Snap("App", "1.0.0.0"));

        Assert.Equal(PlanAction.Skip, r.Solutions[0].Action);
        Assert.Single(Of(r, PlanKind.SameVersionDiffers));
    }

    [Fact]
    public void A_classic_workflow_in_the_zip_gives_no_item()
    {
        var r = Plan(Sol("App") with { Workflows = [B.Flow(FlowId.ToString(), "Classic", category: 0)] }, Snap("App"));

        Assert.Empty(r.Items);
    }

    [Fact]
    public void A_same_version_skip_gives_only_these_items_with_no_settings_dependency_or_flow_output()
    {
        var zip = Sol("App", deps: Dep("Lib (1.0)")) with
        {
            ConnectionReferences = [B.Ref("demo_ref")],
            Workflows = [B.Flow(FlowId.ToString())],
        };

        var r = Plan(zip, Snap("App"));

        Assert.NotEmpty(r.Items);
        Assert.All(r.Items, i => Assert.Equal(PlanKind.SameVersionDiffers, i.Kind));
        Assert.Empty(r.Solutions[0].Flows);
        Assert.False(r.Blocked);
    }

    [Fact]
    public void The_new_item_changes_neither_the_hash_text_nor_the_hash()
    {
        DeployZip[] zips = [Zip(Sol("App") with { EnvironmentVariables = [B.Env("demo_Zip")] })];
        var target = Target(zips, [Snap("App")]);

        var r = DeployPlan.Build(zips, target);

        Assert.NotEmpty(Of(r, PlanKind.SameVersionDiffers));
        var text = DeployPlan.HashText(zips, target);
        Assert.Equal(Hash(text), r.Hash);
        Assert.DoesNotContain("same-version-differs", text);
    }

    // ---- items order -------------------------------------------------------------------------------------------

    [Fact]
    public void Kinds_are_in_the_documented_order()
    {
        Assert.Equal(
            ["cycle", "missing-dependency", "dependency-not-checked", "managed-conflict", "settings", "deletes", "unmanaged-layer", "layers-not-read", "components-not-checked", "same-version-differs"],
            PlanKind.Order);
        Assert.Equal(["install", "upgrade", "skip"], new[] { PlanAction.Install, PlanAction.Upgrade, PlanAction.Skip });
        Assert.Equal(["block", "warning", "info"], new[] { PlanSeverity.Block, PlanSeverity.Warning, PlanSeverity.Info });
    }

    [Fact]
    public void Items_are_sorted_by_plan_order_then_kind_then_subject()
    {
        var a = Sol("A", deps: Dep("Lib (1.0)")) with { ConnectionReferences = [B.Ref("demo_b"), B.Ref("demo_a")] };
        var b = Sol("B", deps: Dep("A (1.0)")) with { ConnectionReferences = [B.Ref("demo_c")] };

        var r = Plan([Zip(b), Zip(a)], deps: new() { ["Lib"] = null });

        Assert.Equal(
            [("A", "missing-dependency", "Lib"), ("A", "settings", "demo_a"), ("A", "settings", "demo_b"), ("B", "settings", "demo_c")],
            r.Items.Select(i => (i.Solution, i.Kind, i.Subject)));
    }

    [Fact]
    public void A_clean_plan_does_not_block_and_names_the_target_by_its_friendly_name()
    {
        var r = Plan(Sol("App"));

        Assert.False(r.Blocked);
        Assert.Equal("Test", r.Target);
    }

    // ---- messages ----------------------------------------------------------------------------------------------

    [Fact]
    public void No_message_or_subject_holds_a_variable_value_a_connection_id_or_the_environment_id()
    {
        const string secret = "s3cr3t-value";
        var zip = Sol("App") with { ConnectionReferences = [B.Ref("demo_ref")], EnvironmentVariables = [B.Env("demo_Var", cur: secret)] };
        var settings = $$"""{"EnvironmentVariables":[{"SchemaName":"demo_Var","Value":"{{secret}}"},{"SchemaName":"gone","Value":"{{secret}}"}],"ConnectionReferences":[{"LogicalName":"demo_ref","ConnectionId":"{{secret}}-conn"}]}""";

        var r = Plan(zip, settings: settings);

        Assert.NotEmpty(r.Items);
        Assert.DoesNotContain(r.Items, i => (i.Message + i.Subject).Contains(secret) || (i.Message + i.Subject).Contains(TargetId.ToString()));
    }

    // ---- hash --------------------------------------------------------------------------------------------------

    private static DeployZip HashZip(string name, string zipHash, string? settingsHash) =>
        new(name + ".zip", zipHash, Sol(name), settingsHash is null ? null : name + ".settings.json", settingsHash, settingsHash is null ? null : "{}");

    private static readonly string H1 = new string('1', 64);
    private static readonly string H2 = new string('2', 64);
    private static readonly string H3 = new string('3', 64);

    private static DeployTarget SmallTarget(params DeployZip[] zips) =>
        new("Test", TargetId, [.. zips.Select(z => Snap(z.Solution.UniqueName, null))], new Dictionary<string, string?> { ["Zed"] = null, ["Other"] = "1.2.3.4" });

    [Fact]
    public void The_hash_text_for_a_fixed_input_is_pinned()
    {
        DeployZip[] zips = [HashZip("B", H2, H3), HashZip("A", H1, null)];
        // The SHA-256 of the not-installed snapshot files of A and B.
        const string snapA = "d4e9e2a14c5313dcf5ef5be27809e9c686b5285cee335736922309ff79fcb1c2";
        const string snapB = "fdc637e93703248e4d1b4617d7a5313ef95348d2ef5bc81a48fc215cd96be7c2";

        var text = DeployPlan.HashText(zips, SmallTarget(zips));

        Assert.Equal(
            "powerops deploy plan 1\n"
            + "target aaaaaaaa-0000-0000-0000-000000000001\n"
            + $"zip A.zip {H1}\n"
            + "settings none\n"
            + $"snapshot {snapA}\n"
            + $"zip B.zip {H2}\n"
            + $"settings B.settings.json {H3}\n"
            + $"snapshot {snapB}\n"
            + "dependency other 1.2.3.4\n"
            + "dependency zed none\n",
            text);
        Assert.Equal(DeployPlan.Sha256Hex(Encoding.UTF8.GetBytes(text)), DeployPlan.Build(zips, SmallTarget(zips)).Hash);
        Assert.Matches("^[0-9a-f]{64}$", DeployPlan.Build(zips, SmallTarget(zips)).Hash);
    }

    [Fact]
    public void Sha256_of_known_bytes_is_lowercase_hex()
    {
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", DeployPlan.Sha256Hex([]));
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", DeployPlan.Sha256Hex("abc"u8));
    }

    [Fact]
    public void The_same_inputs_give_the_same_hash_in_any_zip_order_and_inputs_list_what_was_hashed()
    {
        DeployZip[] zips = [HashZip("A", H1, null), HashZip("B", H2, H3)];
        var target = SmallTarget(zips);

        var first = DeployPlan.Build(zips, target);
        var second = DeployPlan.Build([zips[1], zips[0]], target);

        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal(first.Inputs.Zips, second.Inputs.Zips);
        Assert.Equal(
            [
                new ZipInput("A.zip", H1, null, null, Hash(SnapshotFile.Write(Snap("A", null)))),
                new ZipInput("B.zip", H2, "B.settings.json", H3, Hash(SnapshotFile.Write(Snap("B", null)))),
            ],
            first.Inputs.Zips);
        Assert.Equal([new DependencyInput("other", "1.2.3.4"), new DependencyInput("zed", null)], first.Inputs.Dependencies);
    }

    public static TheoryData<string> Changes() => new()
    {
        "zip", "settings", "no-settings", "target", "dependency", "dependency-gone", "file-name", "new-dependency",
        "environment", "version", "managed", "layers", "component", "variable", "reference", "flow",
    };

    [Theory]
    [MemberData(nameof(Changes))]
    public void Changing_any_one_input_changes_the_hash(string change)
    {
        var baseline = new[] { HashZip("A", H1, H2) };
        var snap = Snap("A", "1.0.0.0", components: [new(1, G1, null)], variables: [new("v", "h")], references: [new(G1, "r", "c", true)], flows: [new(G1, "F", "on")]);
        var target = new DeployTarget("Test", TargetId, [snap], new Dictionary<string, string?> { ["Dep"] = "1.0.0.0" });
        var before = DeployPlan.Build(baseline, target).Hash;

        var zips = baseline;
        var changed = target;
        var one = snap;
        switch (change)
        {
            case "zip": zips = [baseline[0] with { Sha256 = H3 }]; break;
            case "settings": zips = [baseline[0] with { SettingsSha256 = H3 }]; break;
            case "no-settings": zips = [baseline[0] with { SettingsFile = null, SettingsSha256 = null }]; break;
            case "file-name": zips = [baseline[0] with { File = "A2.zip" }]; break;
            case "target": changed = target with { Id = G1 }; break;
            case "dependency": changed = target with { Dependencies = new Dictionary<string, string?> { ["Dep"] = "1.0.0.1" } }; break;
            case "dependency-gone": changed = target with { Dependencies = new Dictionary<string, string?> { ["Dep"] = null } }; break;
            case "new-dependency": changed = target with { Dependencies = new Dictionary<string, string?> { ["Dep"] = "1.0.0.0", ["Dep2"] = null } }; break;
            case "environment": one = snap with { Environment = "Other" }; break;
            case "version": one = snap with { Version = "1.0.0.1" }; break;
            case "managed": one = snap with { Managed = false }; break;
            case "layers": one = snap with { LayersRead = false }; break;
            case "component": one = snap with { Components = [new(1, G1, true)] }; break;
            case "variable": one = snap with { Variables = [new("v", "h2")] }; break;
            case "reference": one = snap with { References = [new(G1, "r", "c", false)] }; break;
            case "flow": one = snap with { Flows = [new(G1, "F", "off")] }; break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }

        changed = changed with { Solutions = [one] };
        Assert.NotEqual(before, DeployPlan.Build(zips, changed).Hash);
    }

    [Fact]
    public void A_snapshot_matches_its_zip_by_unique_name_ignoring_case_and_dependency_names_hash_in_lowercase()
    {
        var zip = HashZip("App", H1, null);
        var upper = new DeployTarget("Test", TargetId, [Snap("APP", null)], new Dictionary<string, string?> { ["Lib"] = null });
        var lower = upper with { Dependencies = new Dictionary<string, string?> { ["LIB"] = null } };

        Assert.Equal(DeployPlan.Build([zip], upper).Hash, DeployPlan.Build([zip], lower).Hash);
        Assert.Equal(PlanAction.Install, DeployPlan.Build([zip], upper).Solutions[0].Action);
    }

    // ---- the recorded target ----------------------------------------------------------------------------------

    [Fact]
    public void A_managed_upgrade_over_the_recorded_seed_target_reports_what_the_snapshot_holds()
    {
        var snapshot = SnapshotFile.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-test.json"));
        var seed = Sol("PowerOpsSeed", "1.0.0.1") with
        {
            RootComponents = [],
            Workflows = [B.Flow(FlowId.ToString(), "Notify on new work item")],
            ConnectionReferences = [B.Ref("pops_sharedoffice365_6a2f1")],
            EnvironmentVariables = [B.Env("pops_DefaultStatus", def: "x")],
        };

        var r = Plan(seed, snapshot, """{"ConnectionReferences":[{"LogicalName":"pops_sharedoffice365_6a2f1","ConnectionId":"c"}]}""");

        var s = Assert.Single(r.Solutions);
        Assert.Equal((PlanAction.Upgrade, new InstalledState("1.0.0.0", true, true)), (s.Action, s.Installed));
        Assert.Equal(["Notify on new work item"], Of(r, PlanKind.UnmanagedLayer).Select(i => i.Subject));
        Assert.Equal(["1 table(s)"], Of(r, PlanKind.Deletes).Select(i => i.Subject));
        Assert.Equal([new PlannedFlow("Notify on new work item", "on", true, "on", "on")], s.Flows);
        Assert.Empty(Of(r, PlanKind.Settings));
        Assert.False(r.Blocked);
    }
}