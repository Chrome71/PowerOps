using System.Diagnostics;

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

    private static readonly string Anon = B.Guid(0xa1);
    private static readonly string Auth = B.Guid(0xa2);
    private static readonly string OtherSite = B.Guid(0xf2);

    private static Solution Pages(params PowerPageComponent[] components) => B.Empty() with { PowerPageComponents = components };

    private static PowerPageComponent AnonRole(string id) => B.Role(id, "Anonymous Users", anonymous: true);

    private static PowerPageComponent AuthRole(string id) => B.Role(id, "Authenticated Users", authenticated: true);

    [Fact]
    public void PO019_fires_for_a_star_fields_setting_on_an_enabled_table()
    {
        var s = Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "*"));

        var f = Assert.Single(Run(new PO019WebApiWildcardFields(), s));

        Assert.Equal("Webapi/demo_item/fields", f.Component);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Site setting 'Webapi/demo_item/fields' is '*' with the Web API enabled for 'demo_item', which Power Pages no longer supports; list the columns before Production.", f.Message);
        Assert.StartsWith("powerpagecomponents/", f.File);
    }

    [Fact]
    public void PO019_ignores_case_and_spaces_in_the_name_and_values()
    {
        var s = Pages(B.Setting("WebAPI/Demo_Item/Enabled", " True "), B.Setting("webapi/demo_item/FIELDS", " * "));

        Assert.Single(Run(new PO019WebApiWildcardFields(), s));
    }

    [Fact]
    public void PO019_quiet_when_the_api_is_disabled_or_has_no_enabled_setting()
    {
        Assert.Empty(Run(new PO019WebApiWildcardFields(), Pages(B.Setting("Webapi/demo_item/enabled", "false"), B.Setting("Webapi/demo_item/fields", "*"))));
        Assert.Empty(Run(new PO019WebApiWildcardFields(), Pages(B.Setting("Webapi/demo_item/fields", "*"))));
    }

    [Fact]
    public void PO019_quiet_with_explicit_fields_and_for_a_star_inside_a_list()
    {
        Assert.Empty(Run(new PO019WebApiWildcardFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "demo_name,demo_size"))));
        Assert.Empty(Run(new PO019WebApiWildcardFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "demo_name,*"))));
    }

    [Fact]
    public void PO019_quiet_when_only_another_site_enables_the_table()
    {
        var s = Pages(B.Setting("Webapi/demo_item/enabled", "true", site: OtherSite), B.Setting("Webapi/demo_item/fields", "*"));

        Assert.Empty(Run(new PO019WebApiWildcardFields(), s));
    }

    [Fact]
    public void PO019_quiet_for_an_inactive_setting()
    {
        var s = Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "*", active: false));

        Assert.Empty(Run(new PO019WebApiWildcardFields(), s));
    }

    [Fact]
    public void PO020_fires_for_an_enabled_table_with_no_fields_setting()
    {
        var s = Pages(B.Setting("Webapi/demo_item/enabled", "True"));

        var f = Assert.Single(Run(new PO020WebApiNoFields(), s));

        Assert.Equal("Webapi/demo_item/enabled", f.Component);
        Assert.Equal("Site setting 'Webapi/demo_item/enabled' enables the Web API for 'demo_item' with no fields setting and UseFieldsFromView not True; set the columns before Production.", f.Message);
    }

    [Fact]
    public void PO020_counts_a_fields_setting_with_an_empty_value_as_none()
    {
        Assert.Single(Run(new PO020WebApiNoFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", " "))));
        Assert.Single(Run(new PO020WebApiNoFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", null))));
    }

    [Fact]
    public void PO020_quiet_with_explicit_fields_or_a_disabled_api_or_an_inactive_setting()
    {
        Assert.Empty(Run(new PO020WebApiNoFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "demo_name"))));
        Assert.Empty(Run(new PO020WebApiNoFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "*"))));
        Assert.Empty(Run(new PO020WebApiNoFields(), Pages(B.Setting("Webapi/demo_item/enabled", "false"))));
        Assert.Empty(Run(new PO020WebApiNoFields(), Pages(B.Setting("Webapi/demo_item/enabled", "true", active: false))));
    }

    [Fact]
    public void PO020_quiet_with_UseFieldsFromView_and_says_it_did_not_check()
    {
        var s = Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/UseFieldsFromView", "True"));

        Assert.Empty(Run(new PO020WebApiNoFields(), s));
        Assert.Equal(
            ["PO020 not checked for table 'demo_item': UseFieldsFromView is True, and the view that holds the columns is not read."],
            Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO020_gives_no_note_when_fields_are_set_or_UseFieldsFromView_is_off()
    {
        Assert.Empty(Analyzer.NotChecked(Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/fields", "demo_name"), B.Setting("Webapi/demo_item/UseFieldsFromView", "true"))));
        Assert.Empty(Analyzer.NotChecked(Pages(B.Setting("Webapi/demo_item/enabled", "true"), B.Setting("Webapi/demo_item/UseFieldsFromView", "false"))));
        Assert.Empty(Analyzer.NotChecked(Pages(B.Setting("Webapi/demo_item/enabled", "false"), B.Setting("Webapi/demo_item/UseFieldsFromView", "true"))));
    }

    [Fact]
    public void PO021_fires_for_a_permission_with_no_web_role()
    {
        var s = Pages(B.Permission(B.Guid(1), "Item access"));

        var f = Assert.Single(Run(new PO021PermissionWithoutRole(), s));

        Assert.Equal("Item access", f.Component);
        Assert.Equal("Table permission 'Item access' on 'demo_item' has no web role, so it gives no access; add a role or remove it before Production.", f.Message);
        Assert.Equal($"powerpagecomponents/{B.Guid(1)}/powerpagecomponent.xml", f.File);
    }

    [Fact]
    public void PO021_quiet_with_a_role_for_a_child_and_for_an_inactive_permission()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", roles: [Auth]),
            B.Permission(B.Guid(2), "Child", parent: B.Guid(1)),
            B.Permission(B.Guid(3), "Off", active: false));

        Assert.Empty(Run(new PO021PermissionWithoutRole(), s));
    }

    [Fact]
    public void PO022_fires_once_per_role_the_parent_lacks()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", roles: [Auth]),
            B.Permission(B.Guid(2), "Child", roles: [Auth, Anon, B.Guid(0xa3)], parent: B.Guid(1)),
            AuthRole(Auth),
            AnonRole(Anon));

        var f = Run(new PO022ChildPermissionRoles(), s).OrderBy(x => x.Message, StringComparer.Ordinal).ToList();

        Assert.Equal(
            [
                $"Table permission 'Child' has web role '{B.Guid(0xa3)}', which its parent 'Parent' doesn't have; fix the roles before Production.",
                "Table permission 'Child' has web role 'Anonymous Users', which its parent 'Parent' doesn't have; fix the roles before Production.",
            ],
            f.Select(x => x.Message));
        Assert.All(f, x => Assert.Equal("Child", x.Component));
    }

    [Fact]
    public void PO022_fires_once_for_an_inactive_parent()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", roles: [Auth], active: false),
            B.Permission(B.Guid(2), "Child", roles: [Auth, Anon], parent: B.Guid(1)));

        var f = Assert.Single(Run(new PO022ChildPermissionRoles(), s));

        Assert.Equal("Child", f.Component);
        Assert.Equal("Table permission 'Child' has the inactive parent 'Parent', so it is not in effect; fix it before Production.", f.Message);
    }

    [Fact]
    public void PO022_quiet_when_the_parent_holds_every_role_or_the_child_has_none()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", roles: [Auth, Anon]),
            B.Permission(B.Guid(2), "Child", roles: [Auth], parent: B.Guid(1)),
            B.Permission(B.Guid(3), "Bare", parent: B.Guid(1)));

        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
    }

    [Fact]
    public void PO022_reads_the_parents_roles_through_the_chain()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Top", roles: [Auth]),
            B.Permission(B.Guid(2), "Middle", parent: B.Guid(1)),
            B.Permission(B.Guid(3), "Leaf", roles: [Auth], parent: B.Guid(2)));

        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO022_gives_a_note_and_no_finding_for_a_parent_outside_the_solution()
    {
        var s = Pages(B.Permission(B.Guid(2), "Child", roles: [Auth], parent: B.Guid(9)));

        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
        Assert.Empty(Run(new PO021PermissionWithoutRole(), s));
        Assert.Equal(["PO022 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution."], Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO022_stops_at_a_cycle()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "A", parent: B.Guid(2)),
            B.Permission(B.Guid(2), "B", parent: B.Guid(1)));

        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
    }

    [Theory]
    [InlineData(true, false, false, "create")]
    [InlineData(false, true, false, "write")]
    [InlineData(false, false, true, "delete")]
    [InlineData(true, true, false, "create and write")]
    [InlineData(true, true, true, "create, write and delete")]
    public void PO023_fires_for_each_privilege_granted_to_an_anonymous_role(bool create, bool write, bool delete, string listed)
    {
        var s = Pages(B.Permission(B.Guid(1), "Item access", roles: [Anon], create: create, write: write, delete: delete), AnonRole(Anon));

        var f = Assert.Single(Run(new PO023AnonymousWrite(), s));

        Assert.Equal("Item access", f.Component);
        Assert.Equal($"Table permission 'Item access' on 'demo_item' lets the anonymous role 'Anonymous Users' {listed}, so anyone can without signing in; confirm it before Production.", f.Message);
    }

    [Fact]
    public void PO023_fires_through_a_child_that_inherits_an_anonymous_parents_roles()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", roles: [Anon], read: true),
            B.Permission(B.Guid(2), "Child", parent: B.Guid(1), create: true),
            AnonRole(Anon));

        var f = Assert.Single(Run(new PO023AnonymousWrite(), s));

        Assert.Equal("Child", f.Component);
    }

    [Fact]
    public void PO023_fires_once_per_anonymous_role()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Item access", roles: [Anon, B.Guid(0xa4)], create: true),
            AnonRole(Anon),
            B.Role(B.Guid(0xa4), "Guests", anonymous: true));

        Assert.Equal(2, Run(new PO023AnonymousWrite(), s).Count);
    }

    [Fact]
    public void PO023_quiet_for_read_append_and_append_to_an_authenticated_role_and_an_inactive_permission()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Reads", roles: [Anon], read: true, append: true, appendTo: true),
            B.Permission(B.Guid(2), "Signed in", roles: [Auth], create: true, write: true, delete: true),
            B.Permission(B.Guid(3), "Off", roles: [Anon], create: true, active: false),
            AnonRole(Anon),
            AuthRole(Auth));

        Assert.Empty(Run(new PO023AnonymousWrite(), s));
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO023_gives_a_note_for_a_role_outside_the_solution_only_when_it_grants_create_write_or_delete()
    {
        var granting = Pages(B.Permission(B.Guid(1), "Item access", roles: [B.Guid(0xA5)], write: true));
        var reading = Pages(B.Permission(B.Guid(1), "Item access", roles: [B.Guid(0xA5)], read: true));

        Assert.Empty(Run(new PO023AnonymousWrite(), granting));
        Assert.Equal([$"PO023 not checked for table permission 'Item access': web role '{B.Guid(0xa5)}' is not in the solution."], Analyzer.NotChecked(granting));
        Assert.Empty(Analyzer.NotChecked(reading));
    }

    [Fact]
    public void The_new_rules_are_registered_as_warnings()
    {
        foreach (var id in new[] { "PO019", "PO020", "PO021", "PO022", "PO023" })
        {
            var rule = Assert.IsAssignableFrom<IRule>(Analyzer.Find(id));
            Assert.Equal(Severity.Warning, rule.Severity);
            Assert.EndsWith(".", rule.Fix);
        }
    }

    [Fact]
    public void Notes_are_distinct_and_sorted()
    {
        var s = Pages(
            B.Permission(B.Guid(2), "B", roles: [B.Guid(0xa5)], create: true),
            B.Permission(B.Guid(1), "A", roles: [B.Guid(0xa5)], create: true),
            B.Permission(B.Guid(3), "A", roles: [B.Guid(0xa5)], create: true, active: true));

        var notes = Analyzer.NotChecked(s);

        Assert.Equal(notes.Order(StringComparer.Ordinal).Distinct(StringComparer.Ordinal), notes);
        Assert.Equal(2, notes.Count);
    }

    private static IReadOnlyList<Finding> Canvas(params string[] formulas) =>
        Run(new PO018CanvasForAllPatch(), B.Empty() with { CanvasFormulas = [.. formulas.Select(f => B.Formula(f))] });

    private const string PatchMessage = "Canvas app 'Demo' calls Patch inside ForAll in Button1.OnSelect, which is less efficient than passing the ForAll result to one Patch.";
    private const string NestedMessage = "Canvas app 'Demo' nests ForAll inside ForAll in Button1.OnSelect, which multiplies the iterations.";

    [Theory]
    [InlineData("=ForAll(colSampleFoodSales, Patch(SampleFoodSalesData, {demoName:\"test\"}))")]
    [InlineData("=ForAll(a, If(b, Patch(c, d), e))")]
    [InlineData("=forall(a, PATCH(c, d))")]
    [InlineData("=ForAll (a, Patch (c, d))")]
    [InlineData("=X.ForAll(a, Patch(c, d))")]
    [InlineData("=ForAll(a, Patch(c, d)); Patch(e, f)")]
    public void PO018_fires_for_patch_inside_forall(string formula)
    {
        var f = Assert.Single(Canvas(formula));

        Assert.Equal("PO018", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal(PatchMessage, f.Message);
        Assert.Equal("Button1.OnSelect", f.Component);
        Assert.Equal("CanvasApps/demo_DocumentUri.msapp", f.File);
    }

    [Theory]
    [InlineData("=ForAll(a, ForAll(b, c))")]
    [InlineData("=ForAll(a, Filter(b, ForAll(c, d)))")]
    [InlineData("=forall(ForAll(a, b), c)")]
    public void PO018_fires_for_forall_inside_forall(string formula) =>
        Assert.Equal(NestedMessage, Assert.Single(Canvas(formula)).Message);

    [Fact]
    public void PO018_gives_two_findings_when_a_formula_has_both_kinds_and_one_per_kind()
    {
        var found = Canvas("=ForAll(a, ForAll(b, Patch(c, d)), Patch(e, f))");

        Assert.Equal([PatchMessage, NestedMessage], found.Select(f => f.Message));
    }

    [Theory]
    [InlineData("=Patch(SampleFoodSalesData, ForAll(colSampleFoodSales, {demoName:\"fromCanvas2\"}))")]
    [InlineData("=ForAll(a, b); Patch(c, d)")]
    [InlineData("=ForAll(a, \"Patch(c, d)\")")]
    [InlineData("=ForAll(a, \"say \"\"hi\"\" Patch(c, d) \")")]
    [InlineData("=ForAll(a, 'Patch(c)'.Name)")]
    [InlineData("=ForAll(a, 'it''s Patch(c)'.Name)")]
    [InlineData("=ForAll(a, // Patch(c, d)\n b)")]
    [InlineData("=ForAll(a, /* Patch(c, d) */ b)")]
    [InlineData("=ForAll(a, /* Patch(c, d)")]
    [InlineData("=ForAll(a, MyPatch(c, d))")]
    [InlineData("=ForAll(a, Patches(c, d))")]
    [InlineData("=ForAll(a, Patch)")]
    [InlineData("=ForAll(a, 1Patch(c))")]
    [InlineData("=ForAll(a, b")]
    [InlineData("=a) ForAll(b, c) )) Patch(d, e)")]
    [InlineData("=MyForAll(a, Patch(c, d))")]
    [InlineData("=Patch(a, b)")]
    public void PO018_is_quiet(string formula) => Assert.Empty(Canvas(formula));

    [Fact]
    public void PO018_skips_an_interpolated_string_whole_which_is_a_known_miss() =>
        Assert.Empty(Canvas("=ForAll(colX, $\"{Patch(DS, {a: 1})}\")"));

    [Fact]
    public void PO018_counts_a_call_in_the_table_argument_of_forall() =>
        Assert.Equal(PatchMessage, Assert.Single(Canvas("=ForAll(Patch(DS, {a: 1}), {b: 2})")).Message);

    [Fact]
    public void PO018_names_the_app_control_and_property_of_each_formula()
    {
        var s = B.Empty() with { CanvasFormulas = [B.Formula("=ForAll(a, Patch(c, d))", "Gallery1", "Items", "Tracker")] };

        var f = Assert.Single(Run(new PO018CanvasForAllPatch(), s));

        Assert.Equal("Canvas app 'Tracker' calls Patch inside ForAll in Gallery1.Items, which is less efficient than passing the ForAll result to one Patch.", f.Message);
        Assert.Equal("Gallery1.Items", f.Component);
    }

    [Fact]
    public void PO018_runs_in_linear_time_on_a_million_nested_calls()
    {
        var deep = "=" + string.Concat(Enumerable.Repeat("ForAll(", 1_000_000)) + "Patch(a, b)";

        var sw = Stopwatch.StartNew();
        var found = Canvas(deep);

        Assert.Equal([PatchMessage, NestedMessage], found.Select(f => f.Message));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"took {sw.Elapsed}");
    }

    [Fact]
    public void PO018_is_a_registered_warning_with_a_fix_sentence()
    {
        var rule = Assert.IsAssignableFrom<IRule>(Analyzer.Find("PO018"));

        Assert.Equal(Severity.Warning, rule.Severity);
        Assert.Equal("Canvas app formula calls Patch or ForAll inside ForAll", rule.Title);
        Assert.Equal("Pass the ForAll result to one Patch call instead of calling Patch inside ForAll, and avoid nesting ForAll.", rule.Fix);
    }

    [Fact]
    public void PO018_says_which_apps_it_did_not_check_and_sorts_the_notes()
    {
        var s = B.Empty() with
        {
            CanvasAppsWithoutSource = ["CanvasApps/b_DocumentUri.msapp", "CanvasApps/a_DocumentUri.msapp"],
        };

        Assert.Equal(
            [
                "PO014 and PO018 not checked for canvas app 'CanvasApps/a_DocumentUri.msapp': no Src/*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).",
                "PO014 and PO018 not checked for canvas app 'CanvasApps/b_DocumentUri.msapp': no Src/*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).",
            ],
            Analyzer.NotChecked(s));
        Assert.Empty(Analyzer.NotChecked(B.Empty()));
    }

    [Fact]
    public void PO019_has_the_review_wording_for_its_fix()
    {
        Assert.Equal("Replace * with the comma-separated logical names of the columns the site needs, or set UseFieldsFromView to True with a view that holds them.", new PO019WebApiWildcardFields().Fix);
    }

    [Fact]
    public void A_parent_of_another_JSON_kind_counts_as_a_parent_that_is_not_found()
    {
        var s = Pages(B.Permission(B.Guid(2), "Child", roles: [Auth], parent: new { id = 1 }));

        Assert.Empty(Run(new PO021PermissionWithoutRole(), s));
        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
        Assert.Equal(["PO022 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution."], Analyzer.NotChecked(s));
    }

    [Fact]
    public void Ids_written_with_braces_and_upper_case_still_match()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", roles: ["{" + Anon.ToUpperInvariant() + "}"]),
            B.Permission(B.Guid(2), "Child", roles: [Anon.ToUpperInvariant()], parent: "{" + B.Guid(1).ToUpperInvariant() + "}", create: true),
            AnonRole(Anon));

        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
        Assert.Empty(Analyzer.NotChecked(s));
        Assert.Equal("Child", Assert.Single(Run(new PO023AnonymousWrite(), s)).Component);
    }

    [Theory]
    [InlineData("Webapi/")]
    [InlineData("Webapi/x")]
    [InlineData("Webapi/a/b/c")]
    [InlineData("Webapi//enabled")]
    [InlineData("Webapi/error/innererror")]
    [InlineData("webapi")]
    public void Odd_setting_names_give_no_finding_and_do_not_throw(string name)
    {
        var s = Pages(B.Setting(name, "true"), B.Setting(name, "*"));

        Assert.Empty(Analyzer.Run(s));
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void Two_components_with_one_id_do_not_throw()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "One", roles: [Anon], create: true),
            B.Permission(B.Guid(1), "Two", roles: [Anon], create: true, parent: B.Guid(1)),
            AnonRole(Anon),
            AnonRole(Anon));

        _ = Analyzer.Run(s);
        _ = Analyzer.NotChecked(s);
    }

    [Fact]
    public void A_permission_that_is_its_own_parent_does_not_loop()
    {
        var s = Pages(B.Permission(B.Guid(1), "Loop", roles: [Auth], parent: B.Guid(1), create: true), AuthRole(Auth));

        Assert.Empty(Analyzer.Run(s));
    }

    [Fact]
    public void An_inactive_permission_up_the_chain_takes_the_child_out_of_effect()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Grant", roles: [Anon], create: true),
            B.Permission(B.Guid(2), "Middle", parent: B.Guid(1), active: false),
            B.Permission(B.Guid(3), "Child", parent: B.Guid(2), create: true),
            B.Permission(B.Guid(4), "Leaf", parent: B.Guid(3), create: true, roles: [Anon]),
            AnonRole(Anon));

        var anonymous = Run(new PO023AnonymousWrite(), s);
        var child = Assert.Single(Run(new PO022ChildPermissionRoles(), s));

        Assert.Equal(["Grant"], anonymous.Select(f => f.Component));
        Assert.Equal("Child", child.Component);
        Assert.Contains("inactive parent 'Middle'", child.Message);
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void A_chain_of_50000_permissions_is_evaluated_by_every_rule_in_linear_time()
    {
        var chain = new List<PowerPageComponent> { B.Permission(B.Guid(1), "P1", roles: [Anon], create: true), AnonRole(Anon) };
        for (var i = 2; i <= 50_000; i++)
        {
            chain.Add(B.Permission(B.Guid(i), $"P{i}", parent: B.Guid(i - 1), create: true));
        }

        chain.Reverse(); // the leaf first, so the first walk is the whole chain
        var s = B.Empty() with { PowerPageComponents = chain };

        var sw = Stopwatch.StartNew();
        var findings = Analyzer.Run(s);
        _ = Analyzer.NotChecked(s);

        Assert.Equal(50_000, findings.Count(f => f.RuleId == "PO023"));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }

    [Fact]
    public void A_huge_parent_value_that_is_not_a_string_is_not_copied_per_read()
    {
        var huge = new Dictionary<string, string> { ["x"] = new string('a', 10_000_000) };
        var s = Pages(B.Permission(B.Guid(1), "Child", roles: [Auth], parent: huge, create: true), AuthRole(Auth));

        var before = GC.GetAllocatedBytesForCurrentThread();
        var findings = Analyzer.Run(s);
        var notes = Analyzer.NotChecked(s);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.DoesNotContain(findings, f => f.RuleId == "PO021");
        Assert.Contains(notes, n => n.StartsWith("PO022 not checked for table permission 'Child'", StringComparison.Ordinal));
        Assert.True(allocated < 20L * 1024 * 1024, $"allocated {allocated / (1024 * 1024)} MB");
    }

    [Fact]
    public void PO022_gives_a_note_when_the_parent_exists_but_a_permission_above_it_does_not()
    {
        var s = Pages(
            B.Permission(B.Guid(1), "Parent", parent: B.Guid(9)),
            B.Permission(B.Guid(2), "Child", roles: [Auth], parent: B.Guid(1)));

        Assert.Empty(Run(new PO022ChildPermissionRoles(), s));
        Assert.Equal(
            [
                "PO022 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution.",
                "PO022 not checked for table permission 'Parent': its parent, or a permission above it, is not found in the solution.",
            ],
            Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO023_gives_a_note_when_the_roles_cannot_be_resolved_only_for_create_write_or_delete()
    {
        var granting = Pages(B.Permission(B.Guid(2), "Child", parent: B.Guid(9), write: true));
        var reading = Pages(B.Permission(B.Guid(2), "Child", parent: B.Guid(9), read: true));
        var above = Pages(B.Permission(B.Guid(1), "Parent", parent: B.Guid(9)), B.Permission(B.Guid(2), "Child", parent: B.Guid(1), delete: true));

        Assert.Empty(Run(new PO023AnonymousWrite(), granting));
        Assert.Equal(["PO022 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution.", "PO023 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution."], Analyzer.NotChecked(granting));
        Assert.Equal(["PO022 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution."], Analyzer.NotChecked(reading));
        Assert.Contains("PO023 not checked for table permission 'Child': its parent, or a permission above it, is not found in the solution.", Analyzer.NotChecked(above));
    }

    [Fact]
    public void PO023_gives_no_finding_and_no_note_for_an_inactive_web_role()
    {
        var s = Pages(B.Permission(B.Guid(1), "Item access", roles: [Anon], create: true), B.Role(Anon, "Anonymous Users", anonymous: true, active: false));

        Assert.Empty(Run(new PO023AnonymousWrite(), s));
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void Of_two_settings_with_one_key_in_one_site_the_first_is_used()
    {
        var on = B.Setting("Webapi/demo_item/enabled", "true");
        var off = B.Setting("Webapi/demo_item/enabled", "false");

        Assert.Single(Run(new PO020WebApiNoFields(), Pages(on, off)));
        Assert.Empty(Run(new PO020WebApiNoFields(), Pages(off, on)));
    }

    private static (string Key, string Id)[] Conns(int keys, int connectors = int.MaxValue) =>
        [.. Enumerable.Range(0, keys).Select(i => ($"key{i}", $"/providers/microsoft.powerapps/apis/shared_{i % connectors}"))];

    private static Solution AppOf(string[] pages, params CanvasAppMeta[] canvas) =>
        B.Empty() with { AppModules = [B.App("demo_app", pages: pages)], CanvasApps = canvas };

    private static string[] Names(int n) => [.. Enumerable.Range(0, n).Select(i => $"demo_page{i}")];

    private static Solution WithPages(int n) => AppOf(Names(n), [.. Names(n).Select(p => B.Canvas(p))]);

    [Fact]
    public void PO025_fires_at_26_pages_and_is_quiet_at_25()
    {
        var f = Assert.Single(Run(new PO025AppPageLimits(), WithPages(26)));

        Assert.Equal("PO025", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("App 'demo_app' has 26 custom pages, more than 25.", f.Message);
        Assert.Equal("demo_app", f.Component);
        Assert.Equal("AppModules/demo_app/AppModule.xml", f.File);
        Assert.Empty(Run(new PO025AppPageLimits(), WithPages(25)));
    }

    [Fact]
    public void PO025_counts_an_appelement_page_and_the_same_page_in_the_site_map_once()
    {
        var names = Names(25);
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["Demo_Map"], pages: names)],
            SiteMaps = [B.SiteMap("demo_map", pages: [.. names.Select(n => n.ToUpperInvariant())])],
            CanvasApps = [.. names.Select(n => B.Canvas(n))],
        };

        Assert.Empty(Run(new PO025AppPageLimits(), s));
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO025_adds_the_pages_of_the_named_site_maps_only()
    {
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_map", "demo_nomap"], pages: Names(20))],
            SiteMaps = [B.SiteMap("demo_map", pages: [.. Names(26).Skip(20)]), B.SiteMap("demo_other", pages: ["demo_a", "demo_b", "demo_c"])],
            CanvasApps = [.. Names(26).Select(n => B.Canvas(n))],
        };

        Assert.Equal("App 'demo_app' has 26 custom pages, more than 25.", Assert.Single(Run(new PO025AppPageLimits(), s)).Message);
    }

    [Fact]
    public void PO025_fires_at_11_connectors_and_is_quiet_at_10()
    {
        var f = Assert.Single(Run(new PO025AppPageLimits(), AppOf(["demo_p"], B.Canvas("demo_p", Conns(11)))));
        Assert.Equal("The custom pages of app 'demo_app' use 11 connectors, more than 10.", f.Message);
        Assert.Equal("demo_app", f.Component);
        Assert.Equal("AppModules/demo_app/AppModule.xml", f.File);

        Assert.Empty(Run(new PO025AppPageLimits(), AppOf(["demo_p"], B.Canvas("demo_p", Conns(10)))));
    }

    [Fact]
    public void PO025_fires_at_21_connection_references_and_is_quiet_at_20()
    {
        var f = Assert.Single(Run(new PO025AppPageLimits(), AppOf(["demo_p"], B.Canvas("demo_p", Conns(21, connectors: 5)))));
        Assert.Equal("The custom pages of app 'demo_app' use 21 connection references, more than 20.", f.Message);

        Assert.Empty(Run(new PO025AppPageLimits(), AppOf(["demo_p"], B.Canvas("demo_p", Conns(20, connectors: 5)))));
    }

    [Fact]
    public void PO025_counts_one_key_in_two_pages_once_and_one_connector_in_two_keys_once()
    {
        // 11 keys over 2 pages share their keys; 11 distinct keys carry 10 distinct connectors.
        var shared = Conns(11, connectors: 10);
        var s = AppOf(["demo_p1", "demo_p2"], B.Canvas("demo_p1", shared), B.Canvas("DEMO_P2", [.. shared.Select(c => (c.Key.ToUpperInvariant(), c.Id.ToUpperInvariant()))]));

        Assert.Empty(Run(new PO025AppPageLimits(), s));
    }

    [Fact]
    public void PO025_does_not_count_an_empty_id_as_a_connector()
    {
        var s = AppOf(["demo_p"], B.Canvas("demo_p", [.. Conns(10), ("empty1", ""), ("empty2", "")]));

        Assert.Empty(Run(new PO025AppPageLimits(), s));
    }

    [Fact]
    public void PO025_gives_a_note_for_pages_without_readable_metadata_and_none_when_every_page_is_held()
    {
        var s = AppOf(["demo_a", "demo_b", "demo_c"], B.Canvas("demo_a"), B.CanvasUnreadable("demo_b"));

        Assert.Empty(Run(new PO025AppPageLimits(), s));
        Assert.Equal(
            ["PO025 not checked for app 'demo_app': 2 of its custom pages have no readable metadata in the solution, so their connectors and connection references are not counted."],
            Analyzer.NotChecked(s));
        Assert.Empty(Analyzer.NotChecked(AppOf(["demo_a"], B.Canvas("demo_a"))));
    }

    [Fact]
    public void PO025_still_counts_what_it_can_read_when_a_page_is_unreadable()
    {
        var s = AppOf(["demo_a", "demo_b"], B.Canvas("demo_a", Conns(11)));

        Assert.Single(Run(new PO025AppPageLimits(), s));
        Assert.Single(Analyzer.NotChecked(s), n => n.StartsWith("PO025 not checked for app 'demo_app': 1 of its custom pages", StringComparison.Ordinal));
    }

    [Fact]
    public void PO025_gives_no_finding_for_an_app_with_no_pages_and_notes_a_site_map_the_solution_lacks()
    {
        var s = B.Empty() with { AppModules = [B.App("demo_app", siteMaps: ["demo_nomap"])] };

        Assert.Empty(Run(new PO025AppPageLimits(), s));
        Assert.Equal(["PO025 not checked for app 'demo_app': its site map 'demo_nomap' is not in the solution, so the custom pages it opens are not counted."], Analyzer.NotChecked(s));
        Assert.Empty(Analyzer.NotChecked(B.Empty() with { AppModules = [B.App("demo_app")] }));
    }

    [Fact]
    public void PO025_counts_the_site_maps_in_the_solution_and_notes_the_one_it_lacks()
    {
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_b", "demo_gone", "demo_a"])],
            SiteMaps = [B.SiteMap("demo_a", pages: Names(26)), B.SiteMap("demo_b", pages: ["demo_x"])],
            CanvasApps = [.. Names(26).Select(n => B.Canvas(n)), B.Canvas("demo_x")],
        };

        Assert.Equal("App 'demo_app' has 26 custom pages, more than 25.", Assert.Single(Run(new PO025AppPageLimits(), s)).Message);
        Assert.Equal(
            [
                "PO025 not checked for app 'demo_app': it names 2 site maps in the solution, and only 'demo_a' is counted.",
                "PO025 not checked for app 'demo_app': its site map 'demo_gone' is not in the solution, so the custom pages it opens are not counted.",
            ],
            Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO025_uses_the_first_record_in_file_order_when_two_share_a_name()
    {
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_map"])],
            SiteMaps = [B.SiteMap("DEMO_MAP", pages: Names(30)) with { File = "AppModuleSiteMaps/b/AppModuleSiteMap.xml" }, B.SiteMap("demo_map", pages: ["demo_p"]) with { File = "AppModuleSiteMaps/a/AppModuleSiteMap.xml" }],
            CanvasApps = [B.Canvas("demo_p", Conns(11)), B.Canvas("DEMO_P")],
        };

        var f = Assert.Single(Run(new PO025AppPageLimits(), s));

        Assert.Contains("11 connectors", f.Message);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(null, true)]
    public void PO025_counts_an_appelement_page_unless_its_metadata_says_another_type(int? type, bool fires)
    {
        var names = Names(26);
        var s = AppOf(names, [.. names.Take(25).Select(n => B.Canvas(n)), B.Canvas(names[25]) with { Type = type }]);

        Assert.Equal(fires, Run(new PO025AppPageLimits(), s).Count == 1);
    }

    [Fact]
    public void PO025_counts_an_appelement_page_with_no_metadata_and_a_site_map_target_of_any_type()
    {
        var names = Names(26);
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_map"], pages: names.Take(25).ToArray())],
            SiteMaps = [B.SiteMap("demo_map", pages: [names[25]])],
            CanvasApps = [.. names.Take(25).Select(n => B.Canvas(n)), B.Canvas(names[25]) with { Type = 1 }],
        };

        Assert.Single(Run(new PO025AppPageLimits(), s));
        Assert.Single(Run(new PO025AppPageLimits(), AppOf(names, [.. names.Take(25).Select(n => B.Canvas(n))])));
    }

    [Fact]
    public void PO025_counts_the_connections_of_the_counted_pages_only()
    {
        var s = AppOf(["demo_page", "demo_library"], B.Canvas("demo_page", Conns(5)), B.Canvas("demo_library", Conns(30)) with { Type = 1 });

        Assert.Empty(Run(new PO025AppPageLimits(), s));
    }

    [Fact]
    public void PO025_adds_an_apps_own_pages_to_those_of_its_site_map_with_the_counts_of_one_set()
    {
        var mapPages = Enumerable.Range(0, 10).Select(i => $"demo_m{i}").ToArray();
        string[] extras = ["DEMO_M0", "demo_x1", "demo_x2", "demo_x3"];
        CanvasAppMeta[] canvas =
        [
            .. mapPages.Select((p, i) => B.Canvas(p, ($"k{i}", $"/c{i}"))),
            B.Canvas("demo_x1", ("kx1", "/C3")),
            B.Canvas("demo_x2", ("kx2", "/cnew")),
        ];
        var split = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_map"], pages: extras)],
            SiteMaps = [B.SiteMap("demo_map", pages: mapPages)],
            CanvasApps = canvas,
        };
        var together = B.Empty() with { AppModules = [B.App("demo_app", pages: [.. mapPages, .. extras])], CanvasApps = canvas };

        var f = Assert.Single(Run(new PO025AppPageLimits(), split));

        Assert.Equal("The custom pages of app 'demo_app' use 11 connectors, more than 10.", f.Message);
        Assert.Equal(Analyzer.NotChecked(together), Analyzer.NotChecked(split));
        Assert.Equal(
            ["PO025 not checked for app 'demo_app': 1 of its custom pages have no readable metadata in the solution, so their connectors and connection references are not counted."],
            Analyzer.NotChecked(split));
        Assert.Equal(f.Message, Assert.Single(Run(new PO025AppPageLimits(), together)).Message);
    }

    [Fact]
    public void PO025_runs_in_linear_time_on_5000_apps_naming_one_100000_page_site_map()
    {
        var pages = Enumerable.Range(0, 100_000).Select(i => $"demo_page{i}").ToArray();
        var s = B.Empty() with
        {
            AppModules = [.. Enumerable.Range(0, 5_000).Select(i => B.App($"demo_app{i}", siteMaps: ["demo_map"], pages: [$"demo_own{i}"]))],
            SiteMaps = [B.SiteMap("demo_map", pages: pages)],
            CanvasApps = [.. pages.Where((_, i) => i % 2 == 0).Select((p, i) => B.Canvas(p, ($"key{i}", $"/providers/microsoft.powerapps/apis/shared_{i}")))],
        };

        var sw = Stopwatch.StartNew();
        var findings = Run(new PO025AppPageLimits(), s);
        var notes = Analyzer.NotChecked(s);

        Assert.Equal(15_000, findings.Count);
        Assert.Equal(5_000, notes.Count);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }

    [Theory]
    [InlineData("demo_big", "demo_small")]
    [InlineData("demo_small", "demo_big")]
    public void PO025_counts_only_the_first_site_map_by_name_of_an_app_that_names_two_and_says_so(string first, string second)
    {
        var small = B.SiteMap("demo_a", pages: ["demo_p"]);
        var big = B.SiteMap("demo_b", pages: Names(30));
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: [first == "demo_big" ? "demo_b" : "demo_a", second == "demo_big" ? "demo_b" : "demo_a"])],
            SiteMaps = [big, small],
            CanvasApps = [B.Canvas("demo_p"), .. Names(30).Select(n => B.Canvas(n))],
        };

        Assert.Empty(Run(new PO025AppPageLimits(), s));
        Assert.Equal(["PO025 not checked for app 'demo_app': it names 2 site maps in the solution, and only 'demo_a' is counted."], Analyzer.NotChecked(s));

        var bigFirst = s with { SiteMaps = [B.SiteMap("demo_a", pages: Names(26)), B.SiteMap("demo_b", pages: ["demo_p"])] };
        Assert.Equal("App 'demo_app' has 26 custom pages, more than 25.", Assert.Single(Run(new PO025AppPageLimits(), bigFirst)).Message);
    }

    [Fact]
    public void PO025_puts_the_unread_note_before_the_site_map_note()
    {
        var s = B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_b", "demo_a"])],
            SiteMaps = [B.SiteMap("demo_a", pages: ["demo_p"]), B.SiteMap("demo_b")],
        };

        Assert.Equal(
            [
                "PO025 not checked for app 'demo_app': 1 of its custom pages have no readable metadata in the solution, so their connectors and connection references are not counted.",
                "PO025 not checked for app 'demo_app': it names 2 site maps in the solution, and only 'demo_a' is counted.",
            ],
            new PO025AppPageLimits().NotChecked(s));
    }

    [Fact]
    public void PO025_runs_in_linear_time_on_1000_apps_naming_a_100000_page_site_map_and_a_small_one()
    {
        var pages = Enumerable.Range(0, 100_000).Select(i => $"demo_page{i}").ToArray();
        var s = B.Empty() with
        {
            AppModules = [.. Enumerable.Range(0, 1_000).Select(i => B.App($"demo_app{i}", siteMaps: ["demo_big", "demo_small"]))],
            SiteMaps = [B.SiteMap("demo_big", pages: pages), B.SiteMap("demo_small", pages: ["demo_one"])],
            CanvasApps = [.. pages.Select((p, i) => B.Canvas(p, ($"key{i}", $"/providers/microsoft.powerapps/apis/shared_{i}")))],
        };

        var sw = Stopwatch.StartNew();
        var findings = Run(new PO025AppPageLimits(), s);
        var notes = Analyzer.NotChecked(s);

        Assert.Equal(3_000, findings.Count);
        Assert.Equal(1_000, notes.Count);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }

    [Fact]
    public void PO025_counts_a_key_shared_by_an_own_page_and_a_site_map_page_once()
    {
        var keys = Enumerable.Range(0, 20).Select(i => ($"key{i}", "/c0")).ToArray();
        Solution WithOwn(params CanvasAppMeta[] own) => B.Empty() with
        {
            AppModules = [B.App("demo_app", siteMaps: ["demo_map"], pages: ["demo_own"])],
            SiteMaps = [B.SiteMap("demo_map", pages: ["demo_s"])],
            CanvasApps = [B.Canvas("demo_s", keys), .. own],
        };

        Assert.Empty(Run(new PO025AppPageLimits(), WithOwn(B.Canvas("demo_own", ("key0", "/c0")))));
        var f = Assert.Single(Run(new PO025AppPageLimits(), WithOwn(B.Canvas("demo_own", ("key0", "/c0"), ("keynew", "/c0")))));
        Assert.Equal("The custom pages of app 'demo_app' use 21 connection references, more than 20.", f.Message);
    }
}