using PowerOps.Core.Model;
using PowerOps.Core.Rules;

namespace PowerOps.Core.Tests.Rules;

public class WarningRuleTests
{
    private static IReadOnlyList<Finding> Run(IRule rule, Solution s) => rule.Evaluate(s).ToList();

    [Fact]
    public void PO005_fires_for_unused_reference()
    {
        var s = B.Empty() with { ConnectionReferences = [B.Ref("demo_a")], Workflows = [B.Flow(refs: ["demo_b"])] };

        Assert.Equal("demo_a", Assert.Single(Run(new PO005UnusedConnectionReference(), s)).Component);
    }

    [Fact]
    public void PO005_quiet_when_used_by_flow()
    {
        var s = B.Empty() with { ConnectionReferences = [B.Ref("demo_a")], Workflows = [B.Flow(refs: ["DEMO_A"])] };

        Assert.Empty(Run(new PO005UnusedConnectionReference(), s));
    }

    [Fact]
    public void PO005_quiet_when_named_in_canvas_app()
    {
        var s = B.Empty() with { ConnectionReferences = [B.Ref("demo_a")], CanvasAppTexts = ["uses Demo_A here"] };

        Assert.Empty(Run(new PO005UnusedConnectionReference(), s));
    }

    [Fact]
    public void PO005_quiet_when_canvas_app_lists_the_connector_api_id()
    {
        var s = B.Empty() with
        {
            ConnectionReferences = [B.Ref("demo_a", "/providers/Microsoft.PowerApps/apis/shared_office365")],
            CanvasAppTexts = ["""{"id":"/providers/microsoft.powerapps/apis/shared_office365"}"""],
        };

        Assert.Empty(Run(new PO005UnusedConnectionReference(), s));
    }

    [Fact]
    public void PO005_fires_when_canvas_app_uses_a_different_connector()
    {
        var s = B.Empty() with
        {
            ConnectionReferences = [B.Ref("demo_a", "/providers/Microsoft.PowerApps/apis/shared_office365")],
            CanvasAppTexts = ["""{"id":"/providers/microsoft.powerapps/apis/shared_sharepointonline"}"""],
        };

        Assert.Equal("demo_a", Assert.Single(Run(new PO005UnusedConnectionReference(), s)).Component);
    }

    [Fact]
    public void PO005_quiet_when_an_agent_maps_it()
    {
        var s = B.Empty() with { ConnectionReferences = [B.Ref("demo_a")], AgentConnectionReferences = ["DEMO_A"] };

        Assert.Empty(Run(new PO005UnusedConnectionReference(), s));
    }

    [Fact]
    public void PO005_fires_when_an_agent_maps_another_reference()
    {
        var s = B.Empty() with { ConnectionReferences = [B.Ref("demo_a")], AgentConnectionReferences = ["demo_b"] };

        Assert.Equal("demo_a", Assert.Single(Run(new PO005UnusedConnectionReference(), s)).Component);
    }

    [Fact]
    public void PO006_fires_once_per_distinct_url_per_flow()
    {
        var s = B.Empty() with
        {
            Workflows = [B.Flow(literals: ["https://org1.crm4.dynamics.com/api", "https://ORG1.crm4.dynamics.com/x", "https://contoso.sharepoint.com/sites/hr/list"])],
        };

        var f = Run(new PO006HardCodedEnvironmentUrl(), s);

        Assert.Equal(2, f.Count);
    }

    [Fact]
    public void PO006_reports_each_sharepoint_site_and_teams_site()
    {
        var s = B.Empty() with
        {
            Workflows = [B.Flow(literals: ["https://contoso.sharepoint.com/sites/hr/list", "https://contoso.sharepoint.com/sites/finance/x", "https://contoso.sharepoint.com/teams/x"])],
        };

        var f = Run(new PO006HardCodedEnvironmentUrl(), s);

        Assert.Equal(
            ["https://contoso.sharepoint.com/sites/hr", "https://contoso.sharepoint.com/sites/finance", "https://contoso.sharepoint.com/teams/x"],
            f.Select(x => x.Component));
    }

    [Fact]
    public void PO006_ignores_globaldisco()
    {
        var s = B.Empty() with { Workflows = [B.Flow(literals: ["https://globaldisco.crm.dynamics.com/api/discovery"])] };

        Assert.Empty(Run(new PO006HardCodedEnvironmentUrl(), s));
    }

    [Fact]
    public void PO006_ignores_definition_parameters_which_PO007_owns()
    {
        var json = """{"properties":{"definition":{"parameters":{"Url (demo_Url)":{"defaultValue":"https://contoso.crm.dynamics.com/"}},"actions":{}}}}""";
        var s = B.Empty() with { Workflows = [B.Flow() with { ClientData = json }] };

        Assert.Empty(Run(new PO006HardCodedEnvironmentUrl(), s));
    }

    [Fact]
    public void PO006_quiet_for_other_urls()
    {
        var s = B.Empty() with { Workflows = [B.Flow(literals: ["https://example.com/a", "https://contoso.sharepoint.com/"])] };

        Assert.Empty(Run(new PO006HardCodedEnvironmentUrl(), s));
    }

    private const string Site = "https://contoso.sharepoint.com/sites/ops";

    private static CanvasFormula Formula(string control, string property, string value, string file = "canvasapps/a/a_DocumentUri/Src/App.pa.yaml", string app = "Work Items") =>
        new(app, control, property, value, file);

    [Fact]
    public void PO014_fires_on_a_sharepoint_site_url_in_a_formula()
    {
        var s = B.Empty() with { CanvasFormulas = [Formula("Button1", "OnSelect", $"=Launch(\"{Site}\")")] };

        var f = Assert.Single(Run(new PO014CanvasHardCodedUrl(), s));
        Assert.Equal("PO014", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Canvas app 'Work Items' hard-codes the URL 'https://contoso.sharepoint.com/sites/ops' in Button1.OnSelect.", f.Message);
        Assert.Equal(Site, f.Component);
        Assert.Equal("canvasapps/a/a_DocumentUri/Src/App.pa.yaml", f.File);
    }

    [Fact]
    public void PO014_ignores_public_links_and_values_that_are_not_formulas()
    {
        var s = B.Empty() with
        {
            CanvasFormulas = [Formula("Button1", "OnSelect", "=Launch(\"https://learn.microsoft.com/power-apps\")"), Formula("Label1", "Text", Site)],
        };

        Assert.Empty(Run(new PO014CanvasHardCodedUrl(), s));
    }

    [Fact]
    public void PO014_reports_a_url_once_per_app_naming_the_first_formula()
    {
        var s = B.Empty() with
        {
            CanvasFormulas =
            [
                Formula("Button1", "OnSelect", $"=Launch(\"{Site}\")", "a/Src/A.pa.yaml"),
                Formula("Button2", "OnSelect", $"=Launch(\"{Site.ToUpperInvariant()}\")", "a/Src/A.pa.yaml"),
                Formula("Button3", "OnSelect", $"=Launch(\"{Site}\")", "a/Src/B.pa.yaml"),
            ],
        };

        var f = Assert.Single(Run(new PO014CanvasHardCodedUrl(), s));
        Assert.EndsWith("in Button1.OnSelect.", f.Message);
        Assert.Equal("a/Src/A.pa.yaml", f.File);
    }

    [Fact]
    public void PO014_reports_each_distinct_url_in_an_app()
    {
        var s = B.Empty() with
        {
            CanvasFormulas = [Formula("Button1", "OnSelect", $"=Launch(\"{Site}\")"), Formula("Button2", "OnSelect", "=Launch(\"https://contoso.sharepoint.com/sites/hr\")")],
        };

        Assert.Equal(2, Run(new PO014CanvasHardCodedUrl(), s).Count);
    }

    [Fact]
    public void PO014_tells_apps_with_the_same_display_name_apart()
    {
        var s = B.Empty() with
        {
            CanvasFormulas =
            [
                Formula("Button1", "OnSelect", $"=Launch(\"{Site}\")", "canvasapps/a/a_DocumentUri/Src/App.pa.yaml"),
                Formula("Button1", "OnSelect", $"=Launch(\"{Site}\")", "canvasapps/b/b_DocumentUri/Src/App.pa.yaml"),
            ],
        };

        Assert.Equal(2, Run(new PO014CanvasHardCodedUrl(), s).Count);
    }

    [Fact]
    public void PO014_quiet_without_formulas()
    {
        Assert.Empty(Run(new PO014CanvasHardCodedUrl(), B.Empty()));
    }

    [Fact]
    public void PO006_and_PO014_share_one_fix_sentence()
    {
        Assert.Equal(new PO006HardCodedEnvironmentUrl().Fix, new PO014CanvasHardCodedUrl().Fix);
    }

    [Theory]
    [InlineData("https://contoso.crm.dynamics.com/", "a URL")]
    [InlineData("11111111-2222-3333-4444-555555555555", "a GUID")]
    [InlineData("someone@example.com", "an email address")]
    public void PO007_message_uses_the_right_article(string value, string phrase)
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_a", def: value)] };

        Assert.EndsWith($"looks like {phrase}.", Assert.Single(Run(new PO007EnvironmentSpecificDefaultValue(), s)).Message);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("42")]
    [InlineData(null)]
    public void PO007_quiet_for_plain_default(string? value)
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_a", def: value)] };

        Assert.Empty(Run(new PO007EnvironmentSpecificDefaultValue(), s));
    }

    [Fact]
    public void PO008_fires_with_values_file()
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_a", cur: "x")] };

        Assert.EndsWith("environmentvariablevalues.json", Assert.Single(Run(new PO008ShippedCurrentValue(), s)).File);
    }

    [Fact]
    public void PO008_quiet_without_current_value()
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_a", def: "x")] };

        Assert.Empty(Run(new PO008ShippedCurrentValue(), s));
    }

    [Fact]
    public void PO009_fires_for_foreign_prefix_on_each_checked_kind()
    {
        var s = B.Empty() with
        {
            EnvironmentVariables = [B.Env("other_a")],
            ConnectionReferences = [B.Ref("other_b")],
            RootComponents = [new RootComponent("1", "other_c", null), new RootComponent("61", "other_d", null)],
        };

        var names = Run(new PO009ForeignPrefix(), s).Select(f => f.Component).Order().ToList();

        Assert.Equal(["other_a", "other_b", "other_c"], names);
    }

    [Fact]
    public void PO009_quiet_for_publisher_prefix_or_no_underscore()
    {
        var s = B.Empty() with
        {
            EnvironmentVariables = [B.Env("DEMO_a")],
            RootComponents = [new RootComponent("1", "account", null)],
        };

        Assert.Empty(Run(new PO009ForeignPrefix(), s));
    }

    [Theory]
    [InlineData("new", "pub")]
    [InlineData("cr5a3", "pub")]
    [InlineData("demo", "DefaultPublisherorg1")]
    public void PO010_fires_for_default_publishers(string prefix, string unique)
    {
        Assert.Single(Run(new PO010DefaultPublisher(), B.Empty(prefix, unique)));
    }

    [Fact]
    public void PO010_quiet_for_own_publisher()
    {
        Assert.Empty(Run(new PO010DefaultPublisher(), B.Empty("crimson", "crimsonpub")));
    }

    [Fact]
    public void PO011_fires_for_off_cloud_flow()
    {
        var s = B.Empty() with { Workflows = [B.Flow(name: "Off", state: 0)] };

        Assert.Equal("Off", Assert.Single(Run(new PO011FlowOff(), s)).Component);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(0, 0)]
    public void PO011_quiet_for_on_flow_or_other_category(int state, int category)
    {
        var s = B.Empty() with { Workflows = [B.Flow(state: state, category: category)] };

        Assert.Empty(Run(new PO011FlowOff(), s));
    }

    [Theory]
    [InlineData(3, null)]
    [InlineData("4", "")]
    [InlineData(7, "  ")]
    public void PO012_fires_for_update_trigger_without_filter_columns(object message, string? filter)
    {
        var s = B.Empty() with { Workflows = [B.Flow(name: "Upd", triggers: B.RowTrigger(message, filter))] };

        var f = Assert.Single(Run(new PO012RowUpdatedTriggerWithoutFilterColumns(), s));

        Assert.Equal("When_a_row", f.Component);
        Assert.Equal("Flow 'Upd' runs on every update of 'demo_item'; set filter columns.", f.Message);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(2, null)]
    [InlineData(5, null)]
    [InlineData(3, "demo_status")]
    public void PO012_quiet_for_create_delete_or_filtered_triggers(object message, string? filter)
    {
        var s = B.Empty() with { Workflows = [B.Flow(triggers: B.RowTrigger(message, filter))] };

        Assert.Empty(Run(new PO012RowUpdatedTriggerWithoutFilterColumns(), s));
    }

    private static Dictionary<string, object> Composes(int n, string prefix = "c") =>
        Enumerable.Range(0, n).ToDictionary(i => prefix + i, _ => (object)new { type = "Compose", inputs = "x" });

    private static Dictionary<string, object> InScope(string name, IDictionary<string, object> inner) =>
        new() { [name] = new { type = "Scope", actions = inner } };

    private static Dictionary<string, object> Loop(string name, string type, IDictionary<string, object> inner) =>
        new() { [name] = new { type, actions = inner } };

    private static IReadOnlyList<Finding> Size(IDictionary<string, object> actions, object? triggers = null) =>
        Run(new PO016FlowSizeOrDepth(), B.Empty() with { Workflows = [B.Flow(name: "Big", actions: actions, triggers: triggers)] });

    [Theory]
    [InlineData(51, 0)]
    [InlineData(1, 5)]
    public void PO016_fires_for_too_many_actions_or_too_deep(int compose, int scopes)
    {
        var actions = Composes(compose);
        for (var i = 0; i < scopes; i++)
        {
            actions = InScope("s" + i, actions);
        }

        var f = Assert.Single(Size(actions));

        Assert.Equal("Big", f.Component);
        Assert.Equal($"Flow 'Big' has {compose + scopes} actions and a nesting depth of {scopes}.", f.Message);
    }

    [Fact]
    public void PO016_counts_every_branch_and_a_switch_case_as_one_level()
    {
        var actions = new Dictionary<string, object>
        {
            ["cond"] = new { type = "If", expression = "x", actions = Composes(13, "t"), @else = new { actions = Composes(13, "e") } },
            ["sw"] = new
            {
                type = "Switch",
                expression = "x",
                cases = new Dictionary<string, object>
                {
                    ["c1"] = new { @case = "a", actions = Composes(13, "a") },
                    ["c2"] = new { @case = "b", actions = Composes(11, "b") },
                },
                @default = new { actions = Composes(1, "d") },
            },
        };

        var f = Assert.Single(Size(actions));

        Assert.Equal("Flow 'Big' has 53 actions and a nesting depth of 1.", f.Message);
    }

    [Fact]
    public void PO016_quiet_at_50_actions_and_depth_4()
    {
        var actions = Composes(46);
        for (var i = 0; i < 4; i++)
        {
            actions = InScope("s" + i, actions);
        }

        Assert.Empty(Size(actions));
    }

    [Fact]
    public void PO016_does_not_count_the_trigger()
    {
        Assert.Empty(Size(Composes(50), B.RowTrigger(1)));
    }

    [Theory]
    [InlineData("Foreach", "Foreach")]
    [InlineData("Foreach", "Until")]
    [InlineData("Until", "Foreach")]
    [InlineData("Until", "Until")]
    public void PO017_fires_for_a_loop_inside_a_loop(string outer, string inner)
    {
        var actions = Loop("outer", outer, Loop("inner", inner, Composes(1)));
        var s = B.Empty() with { Workflows = [B.Flow(name: "Nest", actions: actions)] };

        var f = Assert.Single(Run(new PO017NestedLoop(), s));

        Assert.Equal("inner", f.Component);
        Assert.Equal("Flow 'Nest' runs loop 'inner' inside loop 'outer'.", f.Message);
    }

    [Fact]
    public void PO017_fires_through_a_condition_between_the_loops()
    {
        var actions = Loop("outer", "Foreach", new Dictionary<string, object>
        {
            ["cond"] = new { type = "If", expression = "x", actions = Composes(1), @else = new { actions = Loop("inner", "Foreach", Composes(1)) } },
        });
        var s = B.Empty() with { Workflows = [B.Flow(name: "Nest", actions: actions)] };

        var f = Assert.Single(Run(new PO017NestedLoop(), s));

        Assert.Equal("inner", f.Component);
        Assert.Equal("Flow 'Nest' runs loop 'inner' inside loop 'outer'.", f.Message);
    }

    [Fact]
    public void PO017_reports_each_inner_loop_against_its_nearest_enclosing_loop()
    {
        var actions = Loop("a", "Foreach", Loop("b", "Foreach", Loop("c", "Until", Composes(1))));
        var s = B.Empty() with { Workflows = [B.Flow(name: "Deep", actions: actions)] };

        var found = Run(new PO017NestedLoop(), s);

        Assert.Equal(["Flow 'Deep' runs loop 'b' inside loop 'a'.", "Flow 'Deep' runs loop 'c' inside loop 'b'."], found.Select(f => f.Message));
    }

    [Fact]
    public void PO017_quiet_for_sibling_loops_and_a_loop_holding_only_a_scope()
    {
        var siblings = Loop("a", "Foreach", Composes(1));
        siblings["b"] = new { type = "Until", expression = "x", limit = new { count = 60 }, actions = Composes(1) };
        var withScope = Loop("outer", "Foreach", InScope("s", Composes(2)));

        Assert.Empty(Run(new PO017NestedLoop(), B.Empty() with { Workflows = [B.Flow(actions: siblings), B.Flow(id: "22222222-2222-2222-2222-222222222222", name: "Other", actions: withScope)] }));
    }
}