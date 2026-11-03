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
}