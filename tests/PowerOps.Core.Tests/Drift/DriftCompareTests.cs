using PowerOps.Core.Drift;

namespace PowerOps.Core.Tests.Drift;

public class DriftCompareTests
{
    private static readonly Guid G1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid G2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid G3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static Snapshot Snap(
        string name = "Dev", string version = "1.0.0.0", bool managed = false, bool layersRead = true, bool installed = true,
        SnapshotComponent[]? components = null, SnapshotVariable[]? variables = null, SnapshotReference[]? references = null, SnapshotFlow[]? flows = null) =>
        new(name, "demo", installed, installed ? version : null, installed ? managed : null, layersRead, components ?? [], variables ?? [], references ?? [], flows ?? []);

    private static IReadOnlyList<DriftItem> Run(params Snapshot[] s) => DriftCompare.Compare(s);

    private static DriftItem One(params Snapshot[] s) => Assert.Single(Run(s));

    [Fact]
    public void Equal_snapshots_give_no_items()
    {
        var s = Snap(components: [new(1, G1, null)], variables: [new("v", "h")], references: [new(G3, "r", "c", true)], flows: [new(G2, "F", "on")]);

        Assert.Empty(Run(s, s with { Environment = "Test" }));
    }

    [Fact]
    public void Kinds_are_in_the_documented_order_and_only_managed_and_layers_not_read_are_information()
    {
        Assert.Equal(14, DriftKind.Order.Length);
        Assert.Equal(["managed", "layers-not-read"], DriftKind.Order.Where(k => !DriftKind.IsDrift(k)));
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
}