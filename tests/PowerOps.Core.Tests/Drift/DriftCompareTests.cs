using PowerOps.Core.Drift;

namespace PowerOps.Core.Tests.Drift;

public class DriftCompareTests
{
    private static readonly Guid G1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid G2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid G3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static Snapshot Snap(
        string name = "Dev", string version = "1.0.0.0", bool managed = false, bool layersRead = true, bool installed = true,
        SnapshotComponent[]? components = null, SnapshotVariable[]? variables = null, SnapshotReference[]? references = null, SnapshotFlow[]? flows = null, SnapshotAlsoIn[]? alsoIn = null,
        SnapshotAgent[]? agents = null) =>
        new(name, "demo", installed, installed ? version : null, installed ? managed : null, layersRead, components ?? [], variables ?? [], references ?? [], flows ?? [], alsoIn, agents);

    private static IReadOnlyList<DriftItem> Run(params Snapshot[] s) => DriftCompare.Compare(s);

    private static DriftItem One(params Snapshot[] s) => Assert.Single(Run(s));

    [Fact]
    public void Equal_snapshots_give_no_items()
    {
        var s = Snap(components: [new(1, G1, null)], variables: [new("v", "h")], references: [new(G3, "r", "c", true)], flows: [new(G2, "F", "on")]);

        Assert.Empty(Run(s, s with { Environment = "Test" }));
    }

    private const string TypeLine = "not compared: its ids change on import, and drift compares components by id";

    [Fact]
    public void One_environment_compares_nothing_between_environments_and_says_so()
    {
        Assert.Equal(["only one environment was read, so nothing was compared between environments; only its own checks ran"], DriftCompare.NotCompared([Snap(components: [new(1, G1, null)])]));
        Assert.Empty(DriftCompare.NotCompared([]));
    }

    [Fact]
    public void No_installed_pair_compares_only_whether_the_solution_is_installed()
    {
        const string Line = "no environment has the solution installed together with the first, so only whether it is installed was compared";

        Assert.Equal([Line], DriftCompare.NotCompared([Snap(components: [new(1, G1, null)]), Snap("Test", installed: false)]));
        Assert.Equal([Line], DriftCompare.NotCompared([Snap(installed: false), Snap("Test", components: [new(1, G1, null)]), Snap("Prod", components: [new(1, G1, null)])]));
    }

    [Fact]
    public void Dropped_types_in_a_compared_pair_are_listed_once_in_type_order()
    {
        var dev = Snap(components: [new(300, G1, null), new(29, G2, null), new(10087, G3, null), new(9, G4, null)]);
        var test = Snap("Test", components: [new(1, G1, null)]);

        Assert.Equal(
            [$"component type 1 (table) {TypeLine}", $"component type 9 (global choice) {TypeLine}", $"component type 300 (canvas app) {TypeLine}", $"component type 10087 {TypeLine}"],
            DriftCompare.NotCompared([dev, test, test with { Environment = "Prod" }]));
    }

    [Fact]
    public void Types_compared_under_their_own_kinds_are_not_listed()
    {
        var s = Snap(components: [new(29, G1, null), new(380, G2, null), new(381, G3, null)]);

        Assert.Empty(DriftCompare.NotCompared([s, s with { Environment = "Test" }]));
    }

    [Fact]
    public void A_managed_environment_whose_layers_were_not_read_is_listed_by_position()
    {
        var dev = Snap();
        var test = Snap("Test", managed: true, layersRead: false);

        Assert.Equal(["2 Test: layers not read, so unmanaged layers there are not checked"], DriftCompare.NotCompared([dev, test, Snap("Prod", managed: false, layersRead: false)]));
    }

    [Fact]
    public void Kinds_are_in_the_documented_order_and_only_managed_agent_published_layers_not_read_and_also_in_solution_are_information()
    {
        Assert.Equal(17, DriftKind.Order.Length);
        Assert.Equal("also-in-solution", DriftKind.Order[^1]);
        Assert.Equal(["component-extra", "agent-missing", "agent-published", "unmanaged-layer"], DriftKind.Order[4..8]);
        Assert.Equal(["managed", "agent-published", "layers-not-read", "also-in-solution"], DriftKind.Order.Where(k => !DriftKind.IsDrift(k)));
    }

    [Fact]
    public void Also_in_solution_names_a_flow_by_its_name_a_component_by_type_and_id_and_is_not_drift()
    {
        var s = Snap(flows: [new(G1, "My flow", "on")], alsoIn: [new(29, G1, "Other"), new(1, G2, "Other")]);

        var items = Run(s);

        Assert.Equal([(DriftKind.AlsoInSolution, "1 " + G2.ToString("D")), (DriftKind.AlsoInSolution, "My flow")], items.Select(i => (i.Kind, i.Subject)));
        Assert.All(items, i => Assert.Equal((1, "Dev", false, null, "Other"), (i.Position, i.Environment, i.Drift, i.Expected, i.Actual)));
        Assert.False(PowerOps.Core.Output.DriftReport.HasDrift(items));
    }

    [Fact]
    public void Also_in_solution_orders_by_the_other_solution_for_one_component_and_comes_after_the_other_kinds()
    {
        var s = Snap(references: [new(G3, "r", "c", false)], alsoIn: [new(29, G1, "Zeta"), new(29, G1, "Alpha"), new(29, G1, "alpha")]);

        var items = Run(s);

        Assert.Equal([DriftKind.ReferenceUnbound, DriftKind.AlsoInSolution, DriftKind.AlsoInSolution, DriftKind.AlsoInSolution], items.Select(i => i.Kind));
        Assert.Equal(["Alpha", "Zeta", "alpha"], items.Skip(1).Select(i => i.Actual));
    }

    [Fact]
    public void Also_in_solution_is_reported_in_each_environment_and_not_for_a_solution_that_is_not_installed()
    {
        var also = new[] { new SnapshotAlsoIn(1, G2, "Other") };

        var items = Run(Snap(alsoIn: also), Snap("Test", alsoIn: also), Snap("Prod", installed: false, alsoIn: also));

        Assert.Equal([(1, DriftKind.AlsoInSolution), (2, DriftKind.AlsoInSolution), (3, DriftKind.SolutionMissing)], items.Select(i => (i.Position, i.Kind)));
        Assert.Empty(Run(Snap(alsoIn: [])));
        Assert.Empty(Run(Snap()));
    }

    [Fact]
    public void Solution_missing_fires_on_either_side_and_stops_the_pair()
    {
        var full = Snap(version: "1.0", components: [new(1, G1, null)]);
        var gone = Snap("Test", installed: false);

        var item = One(full, gone);
        Assert.Equal((2, "Test", DriftKind.SolutionMissing, "demo", true), (item.Position, item.Environment, item.Kind, item.Subject, item.Drift));
        Assert.Equal(DriftKind.SolutionMissing, One(gone with { Environment = "Dev" }, full with { Environment = "Test" }).Kind);
        Assert.Empty(Run(gone, gone));
    }

    [Fact]
    public void Version_differs()
    {
        Assert.Empty(Run(Snap(), Snap("Test")));
        var item = One(Snap(version: "1.0"), Snap("Test", version: "2.0"));
        Assert.Equal((DriftKind.Version, "1.0", "2.0", true), (item.Kind, item.Expected, item.Actual, item.Drift));
    }

    [Fact]
    public void Managed_flag_differs_as_information()
    {
        var item = One(Snap(managed: true), Snap("Test", managed: false));
        Assert.Equal((DriftKind.Managed, "managed", "unmanaged", false), (item.Kind, item.Expected, item.Actual, item.Drift));
    }

    [Fact]
    public void Component_missing_and_extra_are_named_by_type_and_id_on_the_later_environment()
    {
        var items = Run(Snap(components: [new(61, G1, null)]), Snap("Test", components: [new(26, G2, null)]));

        Assert.Equal([(DriftKind.ComponentMissing, "61 " + G1.ToString("D")), (DriftKind.ComponentExtra, "26 " + G2.ToString("D"))], items.Select(i => (i.Kind, i.Subject)));
        Assert.All(items, i => Assert.Equal(2, i.Position));
        Assert.Empty(Run(Snap(components: [new(61, G1, null)]), Snap("Test", components: [new(61, G1, null)])));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(62)]
    [InlineData(80)]
    [InlineData(300)]
    [InlineData(380)]
    [InlineData(381)]
    [InlineData(10087)]
    public void Types_whose_ids_change_on_import_are_left_out_of_the_component_kinds(int type)
    {
        Assert.Empty(Run(Snap(components: [new(type, G1, null)]), Snap("Test", components: [new(type, G2, null)])));
        Assert.Empty(Run(Snap(components: [new(type, G1, null)]), Snap("Test")));
        Assert.Empty(Run(Snap(), Snap("Test", components: [new(type, G1, null)])));
    }

    [Fact]
    public void A_model_driven_app_with_new_ids_after_import_is_no_drift_but_a_flow_with_a_new_id_still_is()
    {
        var items = Run(
            Snap(components: [new(80, G1, null), new(62, G1, null), new(10087, G1, null), new(29, G3, null)], flows: [new(G3, "Flow", "on")]),
            Snap("Test", components: [new(80, G2, null), new(62, G2, null), new(10087, G2, null), new(29, G2, null)], flows: [new(G2, "Flow", "on")]));

        Assert.Equal([(DriftKind.FlowMissing, "Flow"), (DriftKind.FlowMissing, "Flow")], items.Select(i => (i.Kind, i.Subject)));
    }

    [Fact]
    public void Other_types_and_a_type_29_row_that_is_no_flow_are_still_compared_beside_the_left_out_ones()
    {
        var items = Run(
            Snap(components: [new(1, G1, null), new(300, G1, null), new(29, G2, null), new(61, G3, null)]),
            Snap("Test", components: [new(1, G2, null), new(300, G2, null), new(29, G3, null), new(61, G3, null)]));

        Assert.Equal([(DriftKind.ComponentMissing, "29 " + G2.ToString("D")), (DriftKind.ComponentExtra, "29 " + G3.ToString("D"))], items.Select(i => (i.Kind, i.Subject)));
    }

    [Fact]
    public void A_connection_reference_in_one_environment_gives_one_reference_item_whatever_its_component_type()
    {
        var item = One(
            Snap(components: [new(10150, G3, null)], references: [new(G3, "r", "c", true)]),
            Snap("Test"));

        Assert.Equal((DriftKind.ReferenceMissing, "r"), (item.Kind, item.Subject));
        item = One(Snap(), Snap("Test", components: [new(10150, G3, null)], references: [new(G3, "r", "c", true)]));
        Assert.Equal((DriftKind.ReferenceMissing, "r"), (item.Kind, item.Subject));
    }

    [Fact]
    public void Variables_are_matched_ignoring_case_and_show_no_value_or_hash()
    {
        Assert.Empty(Run(Snap(variables: [new("Demo_V", "h")]), Snap("Test", variables: [new("demo_v", "h")])));

        var missing = Run(Snap(variables: [new("a", null)]), Snap("Test", variables: [new("b", null)]));
        Assert.Equal([(DriftKind.VariableMissing, "a"), (DriftKind.VariableMissing, "b")], missing.Select(i => (i.Kind, i.Subject)));

        foreach (var (x, y) in new[] { ("secret1", "secret2"), (null, "secret2"), ("secret1", null) })
        {
            var item = One(Snap(variables: [new("v", x)]), Snap("Test", variables: [new("V", y)]));
            Assert.Equal((DriftKind.VariableValue, "v", null, null), (item.Kind, item.Subject, item.Expected, item.Actual));
        }
    }

    [Fact]
    public void References_are_matched_ignoring_case_and_compared_by_connector()
    {
        Assert.Empty(Run(Snap(references: [new(G3, "Demo_R", "c", true)]), Snap("Test", references: [new(G3, "demo_r", "c", true)])));

        var missing = One(Snap(references: [new(G3, "r", "c", true)]), Snap("Test"));
        Assert.Equal((DriftKind.ReferenceMissing, "r"), (missing.Kind, missing.Subject));

        var item = One(Snap(references: [new(G3, "r", "c1", true)]), Snap("Test", references: [new(G3, "R", "c2", true)]));
        Assert.Equal((DriftKind.ReferenceConnector, "c1", "c2"), (item.Kind, item.Expected, item.Actual));
    }

    [Fact]
    public void Flows_are_matched_by_id_and_compare_state_and_off_in_both_is_no_drift()
    {
        Assert.Empty(Run(Snap(flows: [new(G1, "A", "off")]), Snap("Test", flows: [new(G1, "Renamed", "off")])));

        var item = One(Snap(flows: [new(G1, "A", "on")]), Snap("Test", flows: [new(G1, "A", "off")]));
        Assert.Equal((DriftKind.FlowState, "A", "on", "off"), (item.Kind, item.Subject, item.Expected, item.Actual));

        var extra = One(Snap(), Snap("Test", flows: [new(G1, "OnlyHere", "on")]));
        Assert.Equal((DriftKind.FlowMissing, "OnlyHere"), (extra.Kind, extra.Subject));
    }

    [Fact]
    public void Unmanaged_layer_fires_only_in_a_managed_solution_and_names_a_flow_by_its_name()
    {
        var comps = new SnapshotComponent[] { new(29, G1, true), new(1, G2, true), new(1, G3, false) };
        var flows = new[] { new SnapshotFlow(G1, "My flow", "on") };

        var items = Run(Snap(managed: true, components: comps, flows: flows));

        Assert.Equal([(DriftKind.UnmanagedLayer, "1 " + G2.ToString("D")), (DriftKind.UnmanagedLayer, "My flow")], items.Select(i => (i.Kind, i.Subject)));
        Assert.Empty(Run(Snap(managed: false, components: comps, flows: flows)));
    }

    [Fact]
    public void Reference_unbound_fires_in_every_environment_including_the_first()
    {
        var items = Run(Snap(references: [new(G3, "r", "c", false)]), Snap("Test", references: [new(G3, "r", "c", false)]));

        Assert.Equal([(1, DriftKind.ReferenceUnbound), (2, DriftKind.ReferenceUnbound)], items.Select(i => (i.Position, i.Kind)));
        Assert.Empty(Run(Snap(references: [new(G3, "r", "c", true)])));
    }

    [Fact]
    public void Layers_not_read_is_information_and_only_for_a_managed_solution()
    {
        var item = One(Snap(managed: true, layersRead: false), Snap("Test", managed: true));
        Assert.Equal((1, DriftKind.LayersNotRead, "demo", false), (item.Position, item.Kind, item.Subject, item.Drift));
        Assert.Empty(Run(Snap(managed: false, layersRead: false), Snap("Test", managed: false, layersRead: false)));
    }

    [Fact]
    public void Single_environment_checks_still_run_where_the_pair_found_solution_missing()
    {
        var items = Run(Snap(installed: false), Snap("Test", references: [new(G3, "r", "c", false)]));

        Assert.Equal([DriftKind.SolutionMissing, DriftKind.ReferenceUnbound], items.Select(i => i.Kind));
    }

    [Fact]
    public void Three_environments_are_each_compared_with_the_first_only()
    {
        var items = Run(Snap(version: "1"), Snap("Test", version: "2"), Snap("Prod", version: "2"));

        Assert.Equal([(2, "Test"), (3, "Prod")], items.Select(i => (i.Position, i.Environment)));
        Assert.All(items, i => Assert.Equal("1", i.Expected));
    }

    [Fact]
    public void Environments_with_the_same_name_stay_apart_by_position()
    {
        var items = Run(Snap("Same", version: "1"), Snap("Same", version: "2"), Snap("Same", version: "3"));

        Assert.Equal([2, 3], items.Select(i => i.Position));
    }

    [Fact]
    public void Items_are_sorted_by_position_then_kind_order_then_subject()
    {
        var a = Snap(components: [new(61, G1, null)], flows: [new(G1, "F", "on")], variables: [new("b", null), new("a", null)]);
        var b = Snap("Test", version: "2", references: [new(G3, "r", "c", false)]);

        var items = Run(a, b);

        Assert.Equal(
            [(2, DriftKind.Version), (2, DriftKind.ComponentMissing), (2, DriftKind.VariableMissing), (2, DriftKind.VariableMissing), (2, DriftKind.ReferenceMissing), (2, DriftKind.ReferenceUnbound), (2, DriftKind.FlowMissing)],
            items.Select(i => (i.Position, i.Kind)));
        Assert.Equal(["a", "b"], items.Where(i => i.Kind == DriftKind.VariableMissing).Select(i => i.Subject));
    }

    // ---- by name (M76) ------------------------------------------------------------------------------------

    private static readonly Guid G4 = Guid.Parse("00000000-0000-0000-0000-000000000004");

    private const string Fallback = "compared with the first by type number and id, because it or the first has no type names (a snapshot from an older powerops), so agents were not compared";
    private const string NotReturned = "agent(s) or agent component(s) were not returned by the read (the account may not see them), so they were compared by id";

    private static string CountLine(string name, string counts) => $"component type {name} not compared: its ids can change on import, so it is counted, not compared by id ({counts})";

    private static string SettingLine(string name, string counts) => $"component type {name} not compared: Microsoft may own a setting's definition in the target, so a solution can list it in one environment and not another; counted ({counts})";

    [Fact]
    public void The_same_agent_under_other_type_numbers_and_ids_gives_no_item()
    {
        var dev = Snap(components: [new(10226, G1, null, "bot", "demo_Helper"), new(10227, G2, null, "botcomponent", "demo_Helper.topic.Greeting")], agents: [new("demo_Helper", true)]);
        var test = Snap("Test", components: [new(10229, G3, null, "BOT", "DEMO_HELPER"), new(10230, G4, null, "BotComponent", "demo_helper.topic.greeting")], agents: [new("DEMO_HELPER", true)]);

        Assert.Empty(Run(dev, test));
        Assert.Empty(DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void An_agent_on_one_side_only_is_agent_missing_and_no_component_item()
    {
        var dev = Snap(components: [new(10226, G1, null, "bot", "demo_Helper")], agents: [new("demo_Helper", false)]);
        var test = Snap("Test", agents: []);

        var item = One(dev, test);
        Assert.Equal((2, DriftKind.AgentMissing, "demo_Helper", true), (item.Position, item.Kind, item.Subject, item.Drift));
        item = One(test with { Environment = "Dev" }, dev with { Environment = "Test" });
        Assert.Equal((DriftKind.AgentMissing, "demo_Helper"), (item.Kind, item.Subject));
    }

    [Fact]
    public void An_agent_component_on_one_side_only_is_missing_or_extra_named_by_schema_name()
    {
        var with = Snap(components: [new(10227, G1, null, "botcomponent", "demo_Helper.topic.Greeting")], agents: []);
        var without = Snap("Test", agents: []);

        var item = One(with, without);
        Assert.Equal((DriftKind.ComponentMissing, "botcomponent demo_Helper.topic.Greeting", true), (item.Kind, item.Subject, item.Drift));
        item = One(without with { Environment = "Dev" }, with with { Environment = "Test" });
        Assert.Equal((DriftKind.ComponentExtra, "botcomponent demo_Helper.topic.Greeting"), (item.Kind, item.Subject));
    }

    [Fact]
    public void Equal_counts_with_one_changed_schema_name_still_give_items()
    {
        var dev = Snap(components: [new(10227, G1, null, "botcomponent", "demo_H.topic.A"), new(10227, G2, null, "botcomponent", "demo_H.topic.B")], agents: []);
        var test = Snap("Test", components: [new(10230, G3, null, "botcomponent", "demo_H.topic.A"), new(10230, G4, null, "botcomponent", "demo_H.topic.C")], agents: []);

        Assert.Equal(
            [(DriftKind.ComponentMissing, "botcomponent demo_H.topic.B"), (DriftKind.ComponentExtra, "botcomponent demo_H.topic.C")],
            Run(dev, test).Select(i => (i.Kind, i.Subject)));
    }

    [Fact]
    public void Two_components_with_one_number_but_other_definition_names_never_match()
    {
        var dev = Snap(components: [new(10090, G1, null, "customapi")], agents: []);
        var test = Snap("Test", components: [new(10090, G1, null, "AppSetting")], agents: []);

        var item = One(dev, test);
        Assert.Equal((DriftKind.ComponentMissing, "customapi " + G1.ToString("D")), (item.Kind, item.Subject));
        Assert.Equal([CountLine("AppSetting", "1 Dev: 0, 2 Test: 1")], DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void Agent_link_tables_and_app_settings_are_counted_not_compared()
    {
        var dev = Snap(components: [new(10231, G1, null, "botcomponent_workflow"), new(10232, G2, null, "bot_botcomponent"), new(10090, G3, null, "appsetting")], agents: []);
        var test = Snap("Test", components: [new(10234, G3, null, "BotComponent_Workflow"), new(10235, G4, null, "bot_botcomponent")], agents: []);

        Assert.Empty(Run(dev, test));
        Assert.Equal(
            [CountLine("appsetting", "1 Dev: 1, 2 Test: 0"), CountLine("botcomponent_workflow", "1 Dev: 1, 2 Test: 1"), CountLine("bot_botcomponent", "1 Dev: 1, 2 Test: 1")],
            DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void A_setting_definition_on_one_side_only_is_counted_not_compared()
    {
        var with = Snap(components: [new(10090, G1, null, "settingdefinition")], agents: []);
        var without = Snap("Test", agents: []);

        Assert.Empty(Run(with, without));
        Assert.Equal([SettingLine("settingdefinition", "1 Dev: 1, 2 Test: 0")], DriftCompare.NotCompared([with, without]));
        var (dev, test) = (without with { Environment = "Dev" }, with with { Environment = "Test" });
        Assert.Empty(Run(dev, test));
        Assert.Equal([SettingLine("settingdefinition", "1 Dev: 0, 2 Test: 1")], DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void A_global_choice_on_one_side_only_is_no_item_in_a_pair_compared_by_name()
    {
        var with = Snap(components: [new(9, G1, null)], agents: []);
        var without = Snap("Test", agents: []);
        var line = $"component type 9 (global choice) {TypeLine}";

        Assert.Empty(Run(with, without));
        Assert.Equal([line], DriftCompare.NotCompared([with, without]));
        var (dev, test) = (without with { Environment = "Dev" }, with with { Environment = "Test" });
        Assert.Empty(Run(dev, test));
        Assert.Equal([line], DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void After_a_first_import_a_new_choice_id_and_a_setting_definition_give_no_item_but_a_value_and_a_missing_flow_still_do()
    {
        var dev = Snap(
            components: [new(9, G1, null), new(10090, G2, null, "SettingDefinition"), new(10090, G3, null, "appsetting")],
            variables: [new("v", "h1")], flows: [new(G1, "demo_Flow", "on")], agents: []);
        var test = Snap("Test", components: [new(9, G2, null), new(10090, G4, null, "appsetting")], variables: [new("v", "h2")], agents: []);

        var items = Run(dev, test);
        Assert.Equal([(DriftKind.VariableValue, "v"), (DriftKind.FlowMissing, "demo_Flow")], items.Select(i => (i.Kind, i.Subject)));
        Assert.All(items, i => Assert.Equal((2, true), (i.Position, i.Drift)));
        Assert.True(PowerOps.Core.Output.DriftReport.HasDrift(items));
        Assert.Equal(
            [$"component type 9 (global choice) {TypeLine}", CountLine("appsetting", "1 Dev: 1, 2 Test: 1"), SettingLine("SettingDefinition", "1 Dev: 1, 2 Test: 0")],
            DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void A_dynamic_type_with_no_name_is_compared_by_number_and_id()
    {
        var items = Run(Snap(components: [new(10164, G1, null)], agents: []), Snap("Test", components: [new(10164, G2, null)], agents: []));

        Assert.Equal([(DriftKind.ComponentMissing, "10164 " + G1.ToString("D")), (DriftKind.ComponentExtra, "10164 " + G2.ToString("D"))], items.Select(i => (i.Kind, i.Subject)));
        Assert.Empty(DriftCompare.NotCompared([Snap(components: [new(10164, G1, null)], agents: []), Snap("Test", components: [new(10164, G2, null)], agents: [])]));
    }

    [Fact]
    public void Agent_published_on_one_side_only_is_information()
    {
        var items = Run(Snap(agents: [new("demo_Helper", true)]), Snap("Test", agents: [new("demo_helper", false)]));

        var item = Assert.Single(items);
        Assert.Equal((DriftKind.AgentPublished, "demo_Helper", "published", "not published", false), (item.Kind, item.Subject, item.Expected, item.Actual, item.Drift));
        Assert.False(PowerOps.Core.Output.DriftReport.HasDrift(items));
    }

    [Fact]
    public void An_agent_component_the_read_did_not_return_is_compared_by_id_and_counted()
    {
        var dev = Snap(components: [new(10227, G1, null, "botcomponent"), new(10226, G3, null, "bot")], agents: []);
        var test = Snap("Test", components: [new(10230, G1, null, "botcomponent"), new(10229, G4, null, "bot")], agents: []);

        Assert.Equal([(DriftKind.ComponentMissing, "bot " + G3.ToString("D")), (DriftKind.ComponentExtra, "bot " + G4.ToString("D"))], Run(dev, test).Select(i => (i.Kind, i.Subject)));
        Assert.Equal([$"1 Dev: 2 {NotReturned}", $"2 Test: 2 {NotReturned}"], DriftCompare.NotCompared([dev, test]));
    }

    [Fact]
    public void A_pair_where_one_side_has_no_names_is_compared_by_number_in_both_orders()
    {
        var named = Snap(components: [new(10227, G1, null, "botcomponent", "demo_H.topic.A")], agents: [new("demo_H", true)]);
        var old = Snap("Test", components: [new(10230, G2, null)]);

        Assert.Equal([(DriftKind.ComponentMissing, "10227 " + G1.ToString("D")), (DriftKind.ComponentExtra, "10230 " + G2.ToString("D"))], Run(named, old).Select(i => (i.Kind, i.Subject)));
        Assert.Equal([$"2 Test: {Fallback}"], DriftCompare.NotCompared([named, old]));

        var (first, second) = (old with { Environment = "Dev" }, named with { Environment = "Test" });
        Assert.Equal([(DriftKind.ComponentMissing, "10230 " + G2.ToString("D")), (DriftKind.ComponentExtra, "10227 " + G1.ToString("D"))], Run(first, second).Select(i => (i.Kind, i.Subject)));
        Assert.Equal([$"2 Test: {Fallback}"], DriftCompare.NotCompared([first, second]));
    }

    [Fact]
    public void Two_snapshots_without_names_give_the_fallback_line_only_for_a_kept_dynamic_type()
    {
        Assert.Equal([$"component type 10087 {TypeLine}"], DriftCompare.NotCompared([Snap(components: [new(10087, G1, null)]), Snap("Test", components: [new(10087, G2, null)])]));
        Assert.Empty(DriftCompare.NotCompared([Snap(components: [new(10150, G3, null)], references: [new(G3, "r", "c", true)]), Snap("Test")]));
        Assert.Equal([$"2 Test: {Fallback}"], DriftCompare.NotCompared([Snap(components: [new(10164, G1, null)]), Snap("Test", components: [new(10164, G1, null)])]));
    }

    [Fact]
    public void Three_environments_give_each_line_for_its_own_pair_only()
    {
        var dev = Snap(components: [new(10227, G1, null, "botcomponent"), new(10090, G2, null, "appsetting")], agents: []);
        var test = Snap("Test", components: [new(10227, G1, null)]);
        var prod = Snap("Prod", components: [new(10230, G1, null, "botcomponent"), new(10091, G4, null, "appsetting"), new(10087, G3, null)], agents: []);

        Assert.Equal(
            [CountLine("appsetting", "1 Dev: 1, 3 Prod: 1"), $"2 Test: {Fallback}", $"1 Dev: 1 {NotReturned}", $"3 Prod: 1 {NotReturned}"],
            DriftCompare.NotCompared([dev, test, prod]));
    }
}