using PowerOps.Core.Diff;
using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Diff;

public class SolutionDiffTests
{
    private static string Fx(params string[] parts) => Path.Combine([Golden.RepoRoot, "tests", "fixtures", .. parts]);

    private static string[] Lines(IEnumerable<Change> changes) =>
        changes.Select(c => $"{c.Kind} {c.Component} {c.Key}" + (c.Name is null ? "" : $" [{c.Name}]") + (c.Detail is null ? "" : $": {c.Detail}") + (c.Risks.Count == 0 ? "" : $" ({string.Join(",", c.Risks)})")).ToArray();

    private static string[] Diff(Solution a, Solution b) => Lines(SolutionDiff.Compare(a, b).Changes);

    private static string[] DiffPaths(string a, string b) => Diff(SolutionLoader.Load(a), SolutionLoader.Load(b));

    [Fact]
    public void Managed_and_unmanaged_sample_differ_only_in_the_managed_flag() =>
        Assert.Equal(
            ["Changed solution managed: unmanaged → managed"],
            DiffPaths(Fx("zips", "ColumnLevelSecuritySampleSolution.zip"), Fx("zips", "ColumnLevelSecuritySampleSolution_managed.zip")));

    [Fact]
    public void Seed_v1_to_v2_adds_a_connection_reference_and_changes_the_flow_definition() =>
        Assert.Equal(
            [
                "Added connectionReference pops_SpareDataverse (connection-reference)",
                "Changed workflow 4c8dd007-dfcb-f111-a05c-0022480697a8 [Stamp work item status]: definition changed",
            ],
            DiffPaths(Fx("powerops-seed", "v1"), Fx("powerops-seed", "v2")));

    [Fact]
    public void A_solution_diffed_with_itself_has_no_changes()
    {
        foreach (var path in new[] { Fx("powerops-seed", "v2"), Fx("alm-sample"), Fx("zips", "Contoso_sample_1_0_0_1_managed.zip") })
        {
            Assert.Empty(DiffPaths(path, path));
        }
    }

    [Fact]
    public void An_unpacked_folder_and_its_exported_zip_have_no_changes()
    {
        var zip = Path.Combine(Path.GetTempPath(), $"powerops-diff-{Guid.NewGuid():N}.zip");
        try
        {
            SolutionZip.Create(Fx("alm-sample"), zip);

            Assert.Empty(DiffPaths(Fx("alm-sample"), zip));
        }
        finally
        {
            File.Delete(zip);
        }
    }

    private static Solution S(Action<List<Workflow>>? flows = null)
    {
        var list = new List<Workflow>();
        flows?.Invoke(list);
        return B.Empty() with { Workflows = list };
    }

    [Fact]
    public void Solution_version_and_publisher_changes_are_reported()
    {
        var a = B.Empty();
        var b = a with { Version = "1.1.0.0", Publisher = new Publisher("other", "oth") };

        Assert.Equal(["Changed solution version: 1.0.0.0 → 1.1.0.0", "Changed solution publisher: demopub (demo) → other (oth)"], Diff(a, b));
    }

    [Fact]
    public void Root_components_and_missing_dependencies_are_compared_as_sets()
    {
        var a = B.Empty() with { RootComponents = [new("1", "demo_a", null), new("29", null, "id-1")], MissingDependencies = [B.Dep("1", "gone")] };
        var b = B.Empty() with { RootComponents = [new("29", null, "id-1"), new("1", "demo_b", null)], MissingDependencies = [] };

        Assert.Equal(
            [
                "Removed rootComponent 1:demo_a (removed-component)",
                "Added rootComponent 1:demo_b",
                "Removed missingDependency 1:gone required by 29:x",
            ],
            Diff(a, b));
    }

    [Fact]
    public void Removing_a_flow_carries_the_removed_component_risk()
    {
        var a = S(f => f.Add(B.Flow(name: "Old")));

        Assert.Equal(["Removed workflow 11111111-1111-1111-1111-111111111111 [Old] (removed-component)"], Diff(a, S()));
    }

    [Fact]
    public void Adding_a_flow_has_no_risk()
    {
        var b = S(f => f.Add(B.Flow(name: "New")));

        Assert.Equal(["Added workflow 11111111-1111-1111-1111-111111111111 [New]"], Diff(S(), b));
    }

    [Fact]
    public void Turning_a_cloud_flow_on_or_off_carries_the_flow_state_risk()
    {
        var on = S(f => f.Add(B.Flow(state: 1)));
        var off = S(f => f.Add(B.Flow(state: 0)));

        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: state: on → off (flow-state)"], Diff(on, off));
        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: state: off → on (flow-state)"], Diff(off, on));
    }

    [Fact]
    public void A_state_change_of_something_that_is_not_a_cloud_flow_has_no_flow_state_risk()
    {
        var on = S(f => f.Add(B.Flow(state: 1, category: 2)));
        var off = S(f => f.Add(B.Flow(state: 0, category: 2)));

        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: state: on → off"], Diff(on, off));
    }

    [Fact]
    public void Flows_are_matched_by_id_ignoring_case_and_renames_are_changes()
    {
        var a = S(f => f.Add(B.Flow(id: "AAAAAAAA-1111-1111-1111-111111111111", name: "One")));
        var b = S(f => f.Add(B.Flow(id: "aaaaaaaa-1111-1111-1111-111111111111", name: "Two")));

        Assert.Equal(["Changed workflow aaaaaaaa-1111-1111-1111-111111111111 [Two]: name: One → Two"], Diff(a, b));
    }

    [Fact]
    public void File_paths_are_not_identity()
    {
        var flow = B.Flow();
        var a = B.Empty() with { Workflows = [flow], ConnectionReferences = [B.Ref("demo_c")], EnvironmentVariables = [B.Env("demo_v")] };
        var b = B.Empty() with
        {
            Workflows = [flow with { File = "customizations.xml" }],
            ConnectionReferences = [B.Ref("demo_c") with { File = "elsewhere.xml" }],
            EnvironmentVariables = [B.Env("demo_v") with { File = "other/path.xml", ValuesFile = "other/values.json" }],
        };

        Assert.Empty(Diff(a, b));
    }

    [Fact]
    public void Reordered_keys_and_whitespace_in_the_flow_definition_are_not_a_change()
    {
        var a = S(f => f.Add(B.Flow() with { ClientData = """{"a":1,"b":{"x":[1,2],"y":"z"}}""" }));
        var b = S(f => f.Add(B.Flow() with { ClientData = "{\n  \"b\": { \"y\": \"z\", \"x\": [1, 2] },\n  \"a\": 1\n}" }));

        Assert.Empty(Diff(a, b));
    }

    [Fact]
    public void A_changed_value_or_array_order_in_the_flow_definition_is_a_change()
    {
        var a = S(f => f.Add(B.Flow() with { ClientData = """{"a":[1,2]}""" }));

        Assert.Single(Diff(a, S(f => f.Add(B.Flow() with { ClientData = """{"a":[1,3]}""" }))));
        Assert.Single(Diff(a, S(f => f.Add(B.Flow() with { ClientData = """{"a":[2,1]}""" }))));
        Assert.Single(Diff(a, S(f => f.Add(B.Flow() with { ClientData = null }))));
    }

    [Fact]
    public void Flow_definitions_that_are_not_json_are_compared_as_raw_text()
    {
        var a = S(f => f.Add(B.Flow() with { ClientData = "not json" }));

        Assert.Empty(Diff(a, S(f => f.Add(B.Flow() with { ClientData = "not json" }))));
        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: definition changed"], Diff(a, S(f => f.Add(B.Flow() with { ClientData = "not  json" }))));
    }

    [Fact]
    public void Adding_or_removing_a_connection_reference_carries_its_risk()
    {
        var a = B.Empty();
        var b = B.Empty() with { ConnectionReferences = [B.Ref("demo_c")] };

        Assert.Equal(["Added connectionReference demo_c (connection-reference)"], Diff(a, b));
        Assert.Equal(["Removed connectionReference demo_c (removed-component,connection-reference)"], Diff(b, a));
    }

    [Fact]
    public void A_changed_connector_is_a_change_without_risk()
    {
        var a = B.Empty() with { ConnectionReferences = [B.Ref("demo_c", "/providers/x")] };
        var b = B.Empty() with { ConnectionReferences = [B.Ref("demo_c", "/providers/y")] };

        Assert.Equal(["Changed connectionReference demo_c: connectorId: /providers/x → /providers/y"], Diff(a, b));
    }

    [Fact]
    public void Adding_or_removing_an_environment_variable_carries_its_risk()
    {
        var a = B.Empty();
        var b = B.Empty() with { EnvironmentVariables = [B.Env("demo_v")] };

        Assert.Equal(["Added environmentVariable demo_v (environment-variable)"], Diff(a, b));
        Assert.Equal(["Removed environmentVariable demo_v (removed-component,environment-variable)"], Diff(b, a));
    }

    [Fact]
    public void Environment_variable_type_required_default_and_current_value_are_compared()
    {
        var a = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", required: false, def: "a", cur: null)] };
        var b = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", required: true, def: "b", cur: "c") with { Type = "100000002" }] };

        Assert.Equal(
            ["Changed environmentVariable demo_v: type: 100000000 → 100000002; required: false → true; defaultValue: a → b; currentValue added"],
            Diff(a, b));
    }

    private static string App(string name, string version, string display = "App") =>
        $"<CanvasApp><Name>{name}</Name><AppVersion>{version}</AppVersion><DisplayName>{display}</DisplayName></CanvasApp>";

    [Fact]
    public void Canvas_apps_are_matched_by_name_and_a_new_AppVersion_is_reported_as_a_republish()
    {
        var a = B.Empty() with { CanvasAppTexts = [App("demo_a", "2026-01-01T00:00:00Z"), App("demo_b", "2026-01-01T00:00:00Z"), App("demo_c", "1")] };
        var b = B.Empty() with { CanvasAppTexts = [App("demo_c", "2"), App("demo_b", "2026-02-02T00:00:00Z", "Renamed"), App("demo_d", "1")] };

        Assert.Equal(
            [
                "Removed canvasApp demo_a (removed-component)",
                "Changed canvasApp demo_b: definition changed; republished (AppVersion 2026-01-01T00:00:00Z → 2026-02-02T00:00:00Z)",
                "Changed canvasApp demo_c: republished (AppVersion 1 → 2)",
                "Added canvasApp demo_d",
            ],
            Diff(a, b));
    }

    [Fact]
    public void Reindented_canvas_app_metadata_is_not_a_change()
    {
        var a = B.Empty() with { CanvasAppTexts = ["<CanvasApp><Name>demo_a</Name><AppVersion>1</AppVersion><DisplayName>App</DisplayName></CanvasApp>"] };
        var b = B.Empty() with { CanvasAppTexts = ["<CanvasApp>\n    <Name>demo_a</Name>\n  <AppVersion>1</AppVersion>\n\t<DisplayName>App</DisplayName>\n</CanvasApp>"] };

        Assert.Empty(Diff(a, b));
    }

    [Fact]
    public void Flow_definitions_with_duplicate_json_keys_fall_back_to_raw_text_instead_of_throwing()
    {
        var a = S(f => f.Add(B.Flow() with { ClientData = """{"a":1,"a":2}""" }));

        Assert.Empty(Diff(a, S(f => f.Add(B.Flow() with { ClientData = """{"a":1,"a":2}""" }))));
        Assert.Single(Diff(a, S(f => f.Add(B.Flow() with { ClientData = """{"a":1,"a":3}""" }))));
    }

    [Fact]
    public void Solution_unique_name_is_compared()
    {
        Assert.Equal(["Changed solution uniqueName: Demo → Other"], Diff(B.Empty(), B.Empty() with { UniqueName = "Other" }));
    }

    [Fact]
    public void Long_values_are_cut_without_splitting_a_surrogate_pair()
    {
        var value = new string('a', 99) + "\U0001F600tail";
        var a = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", def: "x")] };
        var b = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", def: value)] };

        var detail = SolutionDiff.Compare(a, b).Changes.Single().Detail!;

        Assert.Equal("defaultValue: x → " + new string('a', 99) + "…", detail);
        Assert.Equal(new string('a', 100) + "…", SolutionDiff.Truncate(new string('a', 100) + "\U0001F600tail", 101));
    }

    [Fact]
    public void Environment_variable_current_values_are_never_in_the_output()
    {
        var a = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", cur: "https://secret-old.example")] };
        var b = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", cur: "https://secret-new.example")] };
        var none = B.Empty() with { EnvironmentVariables = [B.Env("demo_v")] };

        Assert.Equal(["Changed environmentVariable demo_v: currentValue changed"], Diff(a, b));
        Assert.Equal(["Changed environmentVariable demo_v: currentValue removed"], Diff(a, none));
    }
}