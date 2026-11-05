using System.Xml.Linq;

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
            ["Changed solution managed: unmanaged → managed (same-version)"],
            DiffPaths(Fx("zips", "ColumnLevelSecuritySampleSolution.zip"), Fx("zips", "ColumnLevelSecuritySampleSolution_managed.zip")));

    [Fact]
    public void Seed_v1_to_v2_adds_a_connection_reference_and_changes_the_flow_definition() =>
        Assert.Equal(
            [
                "Added connectionReference pops_SpareDataverse (connection-reference,same-version)",
                "Changed workflow 4c8dd007-dfcb-f111-a05c-0022480697a8 [Stamp work item status]: definition changed (same-version)",
            ],
            DiffPaths(Fx("powerops-seed", "v1"), Fx("powerops-seed", "v2")));

    [Fact]
    public void A_change_at_an_equal_version_carries_same_version_after_its_other_risks()
    {
        var a = B.Empty();
        var b = B.Empty() with { ConnectionReferences = [B.Ref("demo_c")] };

        Assert.Equal(["Added connectionReference demo_c (connection-reference,same-version)"], Diff(a, b));
        Assert.Equal(["Removed connectionReference demo_c (removed-component,connection-reference,same-version)"], Diff(b, a));
    }

    [Fact]
    public void A_changed_version_or_two_unique_names_or_no_change_give_no_same_version()
    {
        var a = B.Empty() with { Version = "1.0.0.0" };
        var added = new[] { B.Ref("demo_c") };

        Assert.Equal(
            ["Changed solution version: 1.0.0.0 → 1.0.0.1", "Added connectionReference demo_c (connection-reference)"],
            Diff(a, a with { Version = "1.0.0.1", ConnectionReferences = added }));
        Assert.Equal(
            ["Changed solution uniqueName: " + a.UniqueName + " → Other", "Added connectionReference demo_c (connection-reference)"],
            Diff(a, a with { UniqueName = "Other", ConnectionReferences = added }));
        Assert.Empty(Diff(a, a));
    }

    [Fact]
    public void The_unique_name_match_for_same_version_ignores_case()
    {
        var a = B.Empty() with { UniqueName = "App" };

        Assert.Equal(
            ["Changed solution uniqueName: App → APP (same-version)", "Added connectionReference demo_c (connection-reference,same-version)"],
            Diff(a, a with { UniqueName = "APP", ConnectionReferences = [B.Ref("demo_c")] }));
    }

    [Fact]
    public void Versions_that_are_not_four_numeric_parts_give_no_same_version()
    {
        var a = B.Empty() with { Version = "1.0.0" };

        Assert.Equal(["Added connectionReference demo_c (connection-reference)"], Diff(a, a with { ConnectionReferences = [B.Ref("demo_c")] }));
    }

    [Fact]
    public void A_version_written_in_another_spelling_is_a_change_that_carries_same_version()
    {
        var a = B.Empty() with { Version = "1.0.0.0" };

        Assert.Equal(["Changed solution version: 1.0.0.0 → 1.0.0.00 (same-version)"], Diff(a, a with { Version = "1.0.0.00" }));
    }

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
                "Removed rootComponent 1:demo_a (removed-component,same-version)",
                "Added rootComponent 1:demo_b (same-version)",
                "Removed missingDependency 1:gone required by 29:x (same-version)",
            ],
            Diff(a, b));
    }

    [Fact]
    public void Removing_a_flow_carries_the_removed_component_risk()
    {
        var a = S(f => f.Add(B.Flow(name: "Old")));

        Assert.Equal(["Removed workflow 11111111-1111-1111-1111-111111111111 [Old] (removed-component,same-version)"], Diff(a, S()));
    }

    [Fact]
    public void Adding_a_flow_has_no_risk()
    {
        var b = S(f => f.Add(B.Flow(name: "New")));

        Assert.Equal(["Added workflow 11111111-1111-1111-1111-111111111111 [New] (same-version)"], Diff(S(), b));
    }

    [Fact]
    public void Turning_a_cloud_flow_on_or_off_carries_the_flow_state_risk()
    {
        var on = S(f => f.Add(B.Flow(state: 1)));
        var off = S(f => f.Add(B.Flow(state: 0)));

        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: state: on → off (flow-state,same-version)"], Diff(on, off));
        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: state: off → on (flow-state,same-version)"], Diff(off, on));
    }

    [Fact]
    public void A_state_change_of_something_that_is_not_a_cloud_flow_has_no_flow_state_risk()
    {
        var on = S(f => f.Add(B.Flow(state: 1, category: 2)));
        var off = S(f => f.Add(B.Flow(state: 0, category: 2)));

        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: state: on → off (same-version)"], Diff(on, off));
    }

    [Fact]
    public void Flows_are_matched_by_id_ignoring_case_and_renames_are_changes()
    {
        var a = S(f => f.Add(B.Flow(id: "AAAAAAAA-1111-1111-1111-111111111111", name: "One")));
        var b = S(f => f.Add(B.Flow(id: "aaaaaaaa-1111-1111-1111-111111111111", name: "Two")));

        Assert.Equal(["Changed workflow aaaaaaaa-1111-1111-1111-111111111111 [Two]: name: One → Two (same-version)"], Diff(a, b));
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
    public void Regenerated_operationMetadataIds_in_the_flow_definition_are_not_a_change()
    {
        const string Template = """{"properties":{"definition":{"actions":{"A":{"type":"Compose","inputs":"x","metadata":{"operationMetadataId":"{0}"}}},"triggers":{"T":{"type":"Request","metadata":{"operationMetadataId":"{1}"}}}}}}""";
        var a = S(f => f.Add(B.Flow() with { ClientData = Template.Replace("{0}", "aaaaaaaa-0000-0000-0000-000000000001").Replace("{1}", "aaaaaaaa-0000-0000-0000-000000000002") }));
        var b = S(f => f.Add(B.Flow() with { ClientData = Template.Replace("{0}", "bbbbbbbb-0000-0000-0000-000000000001").Replace("{1}", "bbbbbbbb-0000-0000-0000-000000000002") }));

        Assert.Empty(Diff(a, b));
        Assert.Single(Diff(a, S(f => f.Add(B.Flow() with { ClientData = Template.Replace("{0}", "a").Replace("{1}", "b").Replace("\"Compose\"", "\"Response\"") }))));
    }

    private static string[] FlowDiff(string template) =>
        Diff(S(f => f.Add(B.Flow() with { ClientData = template.Replace("{0}", "aaaaaaaa-0000-0000-0000-000000000001") })),
            S(f => f.Add(B.Flow() with { ClientData = template.Replace("{0}", "bbbbbbbb-0000-0000-0000-000000000002") })));

    [Theory]
    [InlineData("""{"properties":{"definition":{"actions":{"A":{"type":"Compose","inputs":{"operationMetadataId":"{0}"}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"A":{"type":"Compose","inputs":{"metadata":{"operationMetadataId":"{0}"}}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"triggers":{"T":{"type":"Request","inputs":{"operationMetadataId":"{0}"}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"A":{"type":"Compose","operationMetadataId":"{0}"}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"A":{"type":"Compose","metadata":{"other":{"operationMetadataId":"{0}"}}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"operationMetadataId":"{0}"}}}""")]
    [InlineData("""{"properties":{"operationMetadataId":"{0}"}}""")]
    [InlineData("""{"operationMetadataId":"{0}"}""")]
    [InlineData("""{"properties":{"connectionReferences":{"c":{"operationMetadataId":"{0}"}},"parameters":{"p":{"operationMetadataId":"{0}"}}}}""")]
    [InlineData("""{"definition":{"actions":{"A":{"metadata":{"operationMetadataId":"{0}"}}}}}""")]
    public void An_operationMetadataId_outside_the_metadata_of_an_action_or_trigger_is_compared(string template) =>
        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: definition changed (same-version)"], FlowDiff(template));

    [Theory]
    [InlineData("""{"properties":{"definition":{"triggers":{"T":{"metadata":{"operationMetadataId":"{0}"}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"S":{"type":"Scope","actions":{"A":{"metadata":{"operationMetadataId":"{0}"}}}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"C":{"type":"If","else":{"actions":{"E":{"metadata":{"operationMetadataId":"{0}"}}}}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"W":{"type":"Switch","cases":{"X":{"actions":{"K":{"metadata":{"operationMetadataId":"{0}"}}}}}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"W":{"type":"Switch","default":{"actions":{"D":{"metadata":{"operationMetadataId":"{0}"}}}}}}}}}""")]
    [InlineData("""{"properties":{"definition":{"actions":{"S":{"actions":{"C":{"else":{"actions":{"W":{"cases":{"X":{"actions":{"D":{"default":{"actions":{"Z":{"metadata":{"operationMetadataId":"{0}"}}}}}}}}}}}}}}}}}}""")]
    public void An_operationMetadataId_in_the_metadata_of_an_action_or_trigger_at_any_nesting_is_not_a_change(string template) =>
        Assert.Empty(FlowDiff(template));

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
        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: definition changed (same-version)"], Diff(a, S(f => f.Add(B.Flow() with { ClientData = "not  json" }))));
    }

    [Fact]
    public void A_flow_definition_with_a_lone_surrogate_key_is_compared_as_raw_text()
    {
        // The key parses, but reading the node (a lookup or the comparison) throws InvalidOperationException.
        var a = S(f => f.Add(B.Flow() with { ClientData = """{"\ud800":1}""" }));

        Assert.Empty(Diff(a, S(f => f.Add(B.Flow() with { ClientData = """{"\ud800":1}""" }))));
        Assert.Equal(["Changed workflow 11111111-1111-1111-1111-111111111111 [Flow]: definition changed (same-version)"], Diff(a, S(f => f.Add(B.Flow() with { ClientData = """{"\ud800":2}""" }))));
    }

    [Fact]
    public void Adding_or_removing_a_connection_reference_carries_its_risk()
    {
        var a = B.Empty();
        var b = B.Empty() with { ConnectionReferences = [B.Ref("demo_c")] };

        Assert.Equal(["Added connectionReference demo_c (connection-reference,same-version)"], Diff(a, b));
        Assert.Equal(["Removed connectionReference demo_c (removed-component,connection-reference,same-version)"], Diff(b, a));
    }

    [Fact]
    public void A_changed_connector_is_a_change_without_risk()
    {
        var a = B.Empty() with { ConnectionReferences = [B.Ref("demo_c", "/providers/x")] };
        var b = B.Empty() with { ConnectionReferences = [B.Ref("demo_c", "/providers/y")] };

        Assert.Equal(["Changed connectionReference demo_c: connectorId: /providers/x → /providers/y (same-version)"], Diff(a, b));
    }

    [Fact]
    public void Adding_or_removing_an_environment_variable_carries_its_risk()
    {
        var a = B.Empty();
        var b = B.Empty() with { EnvironmentVariables = [B.Env("demo_v")] };

        Assert.Equal(["Added environmentVariable demo_v (environment-variable,same-version)"], Diff(a, b));
        Assert.Equal(["Removed environmentVariable demo_v (removed-component,environment-variable,same-version)"], Diff(b, a));
    }

    [Fact]
    public void Environment_variable_type_required_default_and_current_value_are_compared()
    {
        var a = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", required: false, def: "a", cur: null)] };
        var b = B.Empty() with { EnvironmentVariables = [B.Env("demo_v", required: true, def: "b", cur: "c") with { Type = "100000002" }] };

        Assert.Equal(
            ["Changed environmentVariable demo_v: type: 100000000 → 100000002; required: false → true; defaultValue: a → b; currentValue added (same-version)"],
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
                "Removed canvasApp demo_a (removed-component,same-version)",
                "Changed canvasApp demo_b: definition changed; republished (AppVersion 2026-01-01T00:00:00Z → 2026-02-02T00:00:00Z) (same-version)",
                "Changed canvasApp demo_c: republished (AppVersion 1 → 2) (same-version)",
                "Added canvasApp demo_d (same-version)",
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

        Assert.Equal(["Changed environmentVariable demo_v: currentValue changed (same-version)"], Diff(a, b));
        Assert.Equal(["Changed environmentVariable demo_v: currentValue removed (same-version)"], Diff(a, none));
    }

    [Fact]
    public void A_canvas_app_inside_customizations_xml_equals_the_same_app_in_a_meta_file()
    {
        // Real exports keep the app in customizations.xml, where xmlns:xsi is declared on the root; unpacking writes it to CanvasApps/*.meta.xml.
        var dir = Directory.CreateTempSubdirectory("powerops-diff-").FullName;
        var zip = dir + ".zip";
        try
        {
            var src = Fx("powerops-seed", "v1");
            foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(dir, Path.GetRelativePath(src, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }

            Directory.Delete(Path.Combine(dir, "CanvasApps"), recursive: true);
            var meta = XDocument.Load(Directory.GetFiles(Path.Combine(src, "CanvasApps"), "*.meta.xml").Single()).Root!;
            var custom = XDocument.Load(Path.Combine(src, "Other", "Customizations.xml"));
            custom.Root!.Element("CanvasApps")!.Add(new XElement("CanvasApp", meta.Elements()));
            custom.Save(Path.Combine(dir, "Other", "Customizations.xml"));
            SolutionZip.Create(dir, zip);

            Assert.Empty(DiffPaths(src, zip));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
            File.Delete(zip);
        }
    }

    private static Solution WithAgents(params Agent[] agents) => B.Empty() with { Agents = agents };

    [Fact]
    public void An_added_agent_tool_carries_agent_tool_and_a_removed_one_also_removed_component()
    {
        var none = WithAgents(B.Agent());
        var one = WithAgents(B.Agent(tools: [B.Tool()]));

        Assert.Equal(["Added agentTool demo_agent.topic.Orders (agent-tool,same-version)"], Diff(none, one));
        Assert.Equal(["Removed agentTool demo_agent.topic.Orders (removed-component,agent-tool,same-version)"], Diff(one, none));
    }

    [Fact]
    public void Changed_instructions_carry_agent_instructions_and_never_show_the_text()
    {
        Assert.Equal(
            ["Changed agent demo_agent: instructions changed (agent-instructions,same-version)"],
            Diff(WithAgents(B.Agent(instructions: "Old text.")), WithAgents(B.Agent(instructions: "New text."))));
        Assert.Equal(
            ["Changed agent demo_agent: instructions changed (agent-instructions,same-version)"],
            Diff(WithAgents(B.Agent(instructions: null)), WithAgents(B.Agent(instructions: "New text."))));
        Assert.Equal(
            ["Changed agent demo_agent: instructions changed (agent-instructions,same-version)"],
            Diff(WithAgents(B.Agent(instructions: "Old text.")), WithAgents(B.Agent(instructions: null))));
    }

    [Theory]
    [InlineData("Answer questions.", "answer questions.")]
    [InlineData("Answer questions.", "Answer questions.\n")]
    public void The_instructions_compare_is_exact(string before, string after) =>
        Assert.Equal(
            ["Changed agent demo_agent: instructions changed (agent-instructions,same-version)"],
            Diff(WithAgents(B.Agent(instructions: before)), WithAgents(B.Agent(instructions: after))));

    [Fact]
    public void The_same_instructions_or_none_in_both_are_no_change()
    {
        Assert.Empty(Diff(WithAgents(B.Agent()), WithAgents(B.Agent())));
        Assert.Empty(Diff(WithAgents(B.Agent(instructions: null)), WithAgents(B.Agent(instructions: null))));
    }

    [Fact]
    public void A_tool_whose_mode_mcp_flag_or_action_changed_is_one_detail_with_no_risk()
    {
        Assert.Equal(
            ["Changed agentTool demo_agent.topic.Orders: mode: Invoker → Maker (same-version)"],
            Diff(WithAgents(B.Agent(tools: [B.Tool(mode: "Invoker")])), WithAgents(B.Agent(tools: [B.Tool(mode: "Maker")]))));
        Assert.Equal(
            ["Changed agentTool demo_agent.topic.Orders: action: A → B (same-version)"],
            Diff(WithAgents(B.Agent(tools: [B.Tool(action: "A")])), WithAgents(B.Agent(tools: [B.Tool(action: "B")]))));
        Assert.Equal(
            ["Changed agentTool demo_agent.topic.Orders: action: InvokeConnectorTaskAction → InvokeExternalAgentTaskAction; mcp: false → true (same-version)"],
            Diff(WithAgents(B.Agent(tools: [B.Tool(mcp: false)])), WithAgents(B.Agent(tools: [B.Tool(mcp: true)]))));
    }

    private const string Readable = "instructions PowerOps can read (the text of a single Custom GPT component, or of configuration.json's segments when the agent has no Custom GPT component)";

    private static IReadOnlyList<string> NotCompared(Solution a, Solution b) => SolutionDiff.Compare(a, b).NotCompared;

    private static Agent NewUi(string name = "demo_agent", params AgentComponent[] other) => B.Agent(name, instructions: null, recognizer: "CLICopilotRecognizer") with { OtherComponents = other };

    private static AgentComponent Other(string? kind, string name) => new(kind, $"botcomponents/{name}/data");

    [Fact]
    public void Instructions_not_read_in_either_version_of_a_kept_agent_are_listed()
    {
        Assert.Equal(
            [$"agent 'demo_agent': instructions not compared: neither version has {Readable}"],
            NotCompared(WithAgents(NewUi()), WithAgents(NewUi())));
        Assert.Equal(
            [$"agent 'demo_agent': instructions not compared: the old version has no {Readable}"],
            NotCompared(WithAgents(NewUi()), WithAgents(B.Agent())));
        Assert.Equal(
            [$"agent 'demo_agent': instructions not compared: the new version has no {Readable}"],
            NotCompared(WithAgents(B.Agent()), WithAgents(NewUi("DEMO_AGENT"))));
    }

    [Fact]
    public void Agents_are_listed_in_name_order_and_a_duplicate_name_uses_the_first()
    {
        var first = NewUi("demo_agent", Other("McpTool", "demo_agent.tool.Docs"));
        var duplicate = NewUi("DEMO_AGENT", Other("IQCapability", "demo_agent.iq.mail"));

        Assert.Equal(
            [
                $"agent 'a_agent': instructions not compared: neither version has {Readable}",
                $"agent 'demo_agent': instructions not compared: neither version has {Readable}",
                "agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'McpTool'",
            ],
            NotCompared(WithAgents(first, duplicate, NewUi("a_agent")), WithAgents(first, duplicate, NewUi("a_agent"))));
    }

    [Fact]
    public void A_long_agent_name_is_cut()
    {
        var name = new string('n', 150);

        Assert.StartsWith($"agent '{new string('n', 100)}…': instructions not compared", Assert.Single(NotCompared(WithAgents(NewUi(name)), WithAgents(NewUi(name)))));
    }

    [Fact]
    public void A_standard_agent_and_no_agent_list_nothing()
    {
        Assert.Empty(NotCompared(WithAgents(B.Agent(tools: [B.Tool()])), WithAgents(B.Agent(instructions: "Other.", tools: [B.Tool(mcp: true)]))));
        Assert.Empty(NotCompared(B.Empty(), B.Empty()));
    }

    private static AgentCapability Cap(string name, string access = "Write", string? schema = null) => new(schema ?? $"demo_agent.iq.{name}", name, access, $"botcomponents/{schema ?? $"demo_agent.iq.{name}"}/data");

    private static Agent WithCaps(string? instructions, params AgentCapability[] caps) => B.Agent(instructions: instructions) with { Capabilities = caps };

    [Fact]
    public void The_not_compared_instructions_line_has_the_exact_wording()
    {
        Assert.Equal(
            ["agent 'demo_agent': instructions not compared: the old version has no instructions PowerOps can read (the text of a single Custom GPT component, or of configuration.json's segments when the agent has no Custom GPT component)"],
            NotCompared(WithAgents(NewUi()), WithAgents(B.Agent())));
    }

    [Fact]
    public void A_work_iq_workload_removed_added_or_changed_is_a_part_of_the_agent_change_with_no_risk()
    {
        Assert.Equal(
            ["Changed agent demo_agent: IQCapability demo_agent.iq.mail: mail Write → (none) (same-version)"],
            Diff(WithAgents(WithCaps("Text.", Cap("mail"))), WithAgents(WithCaps("Text."))));
        Assert.Equal(
            ["Changed agent demo_agent: IQCapability demo_agent.iq.mail: (none) → mail Write (same-version)"],
            Diff(WithAgents(WithCaps("Text.")), WithAgents(WithCaps("Text.", Cap("mail")))));
        Assert.Equal(
            ["Changed agent demo_agent: IQCapability demo_agent.iq.mail: mail Write → mail Read (same-version)"],
            Diff(WithAgents(WithCaps("Text.", Cap("mail"))), WithAgents(WithCaps("Text.", Cap("mail", "Read")))));
    }

    [Fact]
    public void Parts_are_joined_with_a_semicolon_in_schema_name_order_and_changed_instructions_come_first_with_their_risk()
    {
        Assert.Equal(
            ["Changed agent demo_agent: instructions changed; IQCapability demo_agent.iq.files: files Write → (none); IQCapability demo_agent.iq.mail: mail Write → mail Read (agent-instructions,same-version)"],
            Diff(
                WithAgents(WithCaps("Old.", Cap("mail"), Cap("files"))),
                WithAgents(WithCaps("New.", Cap("mail", "Read")))));
    }

    [Fact]
    public void Equal_workloads_are_no_change_and_their_schema_names_match_ignoring_case()
    {
        Assert.Empty(Diff(WithAgents(WithCaps("Text.", Cap("mail"), Cap("files"))), WithAgents(WithCaps("Text.", Cap("files"), Cap("mail")))));
        Assert.Empty(Diff(WithAgents(WithCaps("Text.", Cap("mail"))), WithAgents(WithCaps("Text.", Cap("mail", schema: "DEMO_AGENT.IQ.MAIL")))));
    }

    [Fact]
    public void A_workload_re_added_under_a_new_schema_name_is_one_part_removed_and_one_added()
    {
        Assert.Equal(
            ["Changed agent demo_agent: IQCapability demo_agent.iq.mail_a: mail Write → (none); IQCapability demo_agent.iq.mail_b: (none) → mail Write (same-version)"],
            Diff(WithAgents(WithCaps("Text.", Cap("mail", schema: "demo_agent.iq.mail_a"))), WithAgents(WithCaps("Text.", Cap("mail", schema: "demo_agent.iq.mail_b")))));
    }

    [Fact]
    public void A_workload_side_and_a_schema_name_are_cut_to_one_hundred_characters()
    {
        var long_ = new AgentCapability(new string('n', 150), new string('c', 150), "Write", "f");

        Assert.Equal(
            [$"Changed agent demo_agent: IQCapability {new string('n', 100)}…: {new string('c', 100)}… → (none) (same-version)"],
            Diff(WithAgents(WithCaps("Text.", long_)), WithAgents(WithCaps("Text."))));
    }

    [Fact]
    public void An_unread_workload_on_one_side_shows_as_none_and_is_listed_in_the_components_line()
    {
        var unread = B.Agent(instructions: "Text.") with { OtherComponents = [Other("IQCapability", "demo_agent.iq.mail")] };

        Assert.Equal(
            ["Changed agent demo_agent: IQCapability demo_agent.iq.mail: mail Write → (none) (same-version)"],
            Diff(WithAgents(WithCaps("Text.", Cap("mail"))), WithAgents(unread)));
        Assert.Equal(
            ["agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'IQCapability'"],
            NotCompared(WithAgents(WithCaps("Text.", Cap("mail"))), WithAgents(unread)));
    }

    [Fact]
    public void A_tool_whose_connection_reference_or_operation_id_changed_is_one_detail_after_the_mode()
    {
        static AgentTool Mcp(string? mode = "Invoker", string? reference = "demo_agent.cr.a", string? operation = "search") =>
            new("demo_agent.tool.Docs", null, true, mode, "botcomponents/demo_agent.tool.Docs/data", reference, operation, "McpTool");

        Assert.Equal(
            ["Changed agentTool demo_agent.tool.Docs: operationId: search → fetch (same-version)"],
            Diff(WithAgents(B.Agent(tools: [Mcp()])), WithAgents(B.Agent(tools: [Mcp(operation: "fetch")]))));
        Assert.Equal(
            ["Changed agentTool demo_agent.tool.Docs: mode: Invoker → Maker; connectionReference: demo_agent.cr.a → demo_agent.cr.b; operationId: search → (none) (same-version)"],
            Diff(WithAgents(B.Agent(tools: [Mcp()])), WithAgents(B.Agent(tools: [Mcp("Maker", "demo_agent.cr.b", null)]))));
        Assert.Empty(Diff(WithAgents(B.Agent(tools: [Mcp()])), WithAgents(B.Agent(tools: [Mcp()]))));
    }

    [Fact]
    public void A_configuration_conflict_in_a_kept_agent_is_listed_for_the_old_the_new_or_both_versions_and_never_for_an_added_agent()
    {
        var conflict = B.Agent() with { InstructionsConflict = true };
        var plain = B.Agent();
        const string Tail = "they differ from the Custom GPT component's or can't be read, and only the component's are compared";

        Assert.Equal([$"agent 'demo_agent': configuration.json instructions not compared: in the old version {Tail}"], NotCompared(WithAgents(conflict), WithAgents(plain)));
        Assert.Equal([$"agent 'demo_agent': configuration.json instructions not compared: in the new version {Tail}"], NotCompared(WithAgents(plain), WithAgents(conflict)));
        Assert.Equal([$"agent 'demo_agent': configuration.json instructions not compared: in both versions {Tail}"], NotCompared(WithAgents(conflict), WithAgents(conflict)));
        Assert.Empty(NotCompared(B.Empty(), WithAgents(conflict)));
        Assert.Empty(NotCompared(WithAgents(conflict), B.Empty()));
        Assert.Empty(NotCompared(WithAgents(plain), WithAgents(plain)));
    }

    [Fact]
    public void The_conflict_line_comes_after_the_instructions_line_and_before_the_components_line()
    {
        var a = NewUi("demo_agent", Other("Widget", "demo_agent.x")) with { InstructionsConflict = true };

        Assert.Equal(
            [
                $"agent 'demo_agent': instructions not compared: neither version has {Readable}",
                "agent 'demo_agent': configuration.json instructions not compared: in both versions they differ from the Custom GPT component's or can't be read, and only the component's are compared",
                "agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'Widget'",
            ],
            NotCompared(WithAgents(a), WithAgents(a)));
    }

    [Fact]
    public void An_added_agent_gets_no_instructions_line_but_its_other_components_are_listed()
    {
        Assert.Equal(
            ["agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'McpTool'"],
            NotCompared(B.Empty(), WithAgents(NewUi("demo_agent", Other("McpTool", "demo_agent.tool.Docs")))));
        Assert.Equal(
            ["agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'McpTool'"],
            NotCompared(WithAgents(NewUi("demo_agent", Other("McpTool", "demo_agent.tool.Docs"))), B.Empty()));
    }

    [Fact]
    public void Other_components_are_counted_once_across_versions_by_kind_and_a_removed_one_still_counts()
    {
        AgentComponent[] v1 = [Other("McpTool", "a.tool.Docs"), Other("McpTool", "a.tool.Work"), Other("IQCapability", "a.iq.mail"), Other(null, "a.odd")];
        AgentComponent[] v2 = [Other("McpTool", "A.TOOL.DOCS"), Other("IQCapability", "a.iq.mail"), Other(null, "a.odd")];

        Assert.Equal(
            [
                $"agent 'demo_agent': instructions not compared: neither version has {Readable}",
                "agent 'demo_agent': 4 components not read as tools, so a change to them is not compared: 1 'IQCapability', 2 'McpTool', 1 with no kind",
            ],
            NotCompared(WithAgents(NewUi("demo_agent", v1)), WithAgents(NewUi("demo_agent", v2))));
    }

    [Fact]
    public void Unread_tool_files_are_listed_and_a_long_kind_is_cut()
    {
        var unread = B.Agent(unread: ["botcomponents/demo_agent.topic.Odd/data"]);
        var odd = B.Agent("demo_two") with { OtherComponents = [Other(new string('k', 150), "demo_two.x")] };

        Assert.Equal(
            [
                "agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'TaskDialog' with keys PowerOps can't map",
                $"agent 'demo_two': 1 component not read as tools, so a change to it is not compared: 1 '{new string('k', 100)}…'",
            ],
            NotCompared(WithAgents(unread, odd), WithAgents(unread, odd)));
    }

    [Fact]
    public void An_added_or_removed_agent_has_no_risk_of_its_own_beyond_removed_component()
    {
        Assert.Equal(["Added agent demo_agent (same-version)"], Diff(B.Empty(), WithAgents(B.Agent())));
        Assert.Equal(["Removed agent demo_agent (removed-component,same-version)"], Diff(WithAgents(B.Agent()), B.Empty()));
    }

    [Fact]
    public void Tools_of_two_agents_are_compared_as_one_set_and_keys_ignore_case()
    {
        var a = WithAgents(B.Agent("demo_a", tools: [B.Tool("demo_a.topic.One")]), B.Agent("demo_b"));
        var b = WithAgents(B.Agent("demo_a"), B.Agent("demo_b", tools: [B.Tool("DEMO_A.TOPIC.ONE")]));

        Assert.Empty(Diff(a, b));
    }

    [Fact]
    public void A_solution_with_no_agents_has_no_agent_lines() =>
        Assert.Empty(Diff(B.Empty(), B.Empty() with { Version = "1.0.0.0" }));

    [Fact]
    public void The_agent_risk_ids_have_descriptions()
    {
        Assert.Equal("An agent tool was added or removed.", Risk.Describe(Risk.AgentTool));
        Assert.Equal("An agent's instructions changed.", Risk.Describe(Risk.AgentInstructions));
    }

    [Fact]
    public void An_added_agent_with_two_tools_gives_the_agent_and_each_tool_and_removing_it_the_reverse()
    {
        var agent = WithAgents(B.Agent(tools: [B.Tool("demo_agent.topic.One"), B.Tool("demo_agent.topic.Two", mcp: true)]));

        Assert.Equal(
            ["Added agent demo_agent (same-version)", "Added agentTool demo_agent.topic.One (agent-tool,same-version)", "Added agentTool demo_agent.topic.Two (agent-tool,same-version)"],
            Diff(B.Empty(), agent));
        Assert.Equal(
            ["Removed agent demo_agent (removed-component,same-version)", "Removed agentTool demo_agent.topic.One (removed-component,agent-tool,same-version)", "Removed agentTool demo_agent.topic.Two (removed-component,agent-tool,same-version)"],
            Diff(agent, B.Empty()));
    }

    [Fact]
    public void With_a_schema_name_twice_in_one_solution_the_first_agent_and_the_first_tool_count()
    {
        var first = WithAgents(B.Agent(instructions: "A"), B.Agent(instructions: "B"));

        Assert.Empty(Diff(first, WithAgents(B.Agent(instructions: "A"))));
        Assert.Equal(["Changed agent demo_agent: instructions changed (agent-instructions,same-version)"], Diff(first, WithAgents(B.Agent(instructions: "B"))));

        var tools = WithAgents(B.Agent(tools: [B.Tool(mode: "Invoker"), B.Tool(mode: "Maker")]));

        Assert.Empty(Diff(tools, WithAgents(B.Agent(tools: [B.Tool(mode: "Invoker")]))));
        Assert.Equal(["Changed agentTool demo_agent.topic.Orders: mode: Maker → Invoker (same-version)"], Diff(WithAgents(B.Agent(tools: [B.Tool(mode: "Maker")])), tools));
    }

    [Fact]
    public void Agent_names_that_differ_only_in_case_are_the_same_agent() =>
        Assert.Empty(Diff(WithAgents(B.Agent("demo_agent")), WithAgents(B.Agent("DEMO_AGENT"))));

    [Fact]
    public void Empty_instructions_and_missing_instructions_are_a_change() =>
        Assert.Equal(
            ["Changed agent demo_agent: instructions changed (agent-instructions,same-version)"],
            Diff(WithAgents(B.Agent(instructions: "")), WithAgents(B.Agent(instructions: null))));

    [Fact]
    public void A_tool_moved_to_another_agent_under_the_same_schema_name_is_no_change() =>
        Assert.Empty(Diff(
            WithAgents(B.Agent("demo_a", tools: [B.Tool("demo_x.topic.One")]), B.Agent("demo_b")),
            WithAgents(B.Agent("demo_a"), B.Agent("demo_b", tools: [B.Tool("demo_x.topic.One")]))));
}