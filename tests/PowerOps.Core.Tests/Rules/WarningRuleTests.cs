using System.Diagnostics;

using PowerOps.Core.Model;
using PowerOps.Core.Rules;

namespace PowerOps.Core.Tests.Rules;

// Serial: its wall-clock bounds (M43 to M47) failed on a shared CI runner while other classes ran beside them.
[CollectionDefinition("time bounds", DisableParallelization = true)]
public sealed class TimeBoundsCollection;

[Collection("time bounds")]
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

    private const string FlowName = "Invoices";

    // One flow whose definition.parameters holds one parameter; `meta` becomes metadata.schemaName, `valueJson` is the raw JSON of its defaultValue.
    private static Workflow FlowWithParam(string key, string? meta, string valueJson)
    {
        var metadata = meta is null ? "" : ",\"metadata\":{\"schemaName\":\"" + meta + "\"}";
        var json = "{\"properties\":{\"definition\":{\"parameters\":{" + System.Text.Json.JsonSerializer.Serialize(key)
            + ":{\"type\":\"String\",\"defaultValue\":" + valueJson + metadata + "}},\"actions\":{}}}}";
        return B.Flow(name: FlowName) with { ClientData = json };
    }

    private static string Quote(string v) => System.Text.Json.JsonSerializer.Serialize(v);

    private static string SchemaOf(string key, string? meta) => meta ?? key[(key.IndexOf('(') + 1)..^1];

    private static List<Finding> FlowFindings(Solution s) =>
        Run(new PO007EnvironmentSpecificDefaultValue(), s).Where(f => f.Message.StartsWith("Flow '", StringComparison.Ordinal)).ToList();

    // The value, and the parts of it a finding must not hold: local part, host, domain, the groups of a GUID.
    private static IEnumerable<string> ValueParts(string value)
    {
        var v = value.Trim();
        yield return v;
        var tokens = v.Split(['@', '/', ':']).Where(t => t.Length > 0 && t is not ("http" or "https")).ToList();
        if (Guid.TryParse(v, out _))
        {
            tokens.AddRange(v.Trim('{', '}').Split('-'));
        }

        foreach (var t in tokens)
        {
            yield return t;
            var labels = t.Split('.');
            if (labels.Length > 2)
            {
                yield return string.Join('.', labels[^2..]);
            }
        }
    }

    [Theory]
    [InlineData("Mail", "demo_Mail", "ann.lee@contoso.com", "", "an email address")]
    [InlineData("Mail", "demo_Mail", "ann.lee@contoso.com", "bob.ray@fabrikam.example", "an email address")]
    [InlineData("Portal (demo_Portal)", null, "https://portal.fabrikam.example/sites/hr", null, "a URL")]
    [InlineData("Tenant (demo_Tenant)", null, "9f3c2a7e-5b41-4d8a-a6c0-7e12b3d4f5a6", null, "a GUID")]
    [InlineData("Mail (demo_Mail)", null, "  ann.lee@contoso.com\t ", null, "an email address")]
    public void PO007_fires_on_a_flow_default_and_holds_no_part_of_the_value(string key, string? meta, string value, string? definitionDefault, string kind)
    {
        var schema = SchemaOf(key, meta);
        var s = B.Empty() with
        {
            Workflows = [FlowWithParam(key, meta, Quote(value))],
            EnvironmentVariables = definitionDefault is null ? [] : [B.Env(schema, def: definitionDefault)],
        };

        var flow = s.Workflows[0];
        var f = Assert.Single(FlowFindings(s));

        Assert.Equal("PO007", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal($"Flow '{FlowName}' holds a default for environment variable '{schema}' that looks like {kind}.", f.Message);
        Assert.Equal(flow.File, f.File);
        Assert.Equal(schema, f.Component);
        foreach (var part in ValueParts(value))
        {
            Assert.False(f.Message.Contains(part, StringComparison.OrdinalIgnoreCase), "message holds a part of the value");
            Assert.False(f.File!.Contains(part, StringComparison.OrdinalIgnoreCase), "file holds a part of the value");
            Assert.False(f.Component!.Contains(part, StringComparison.OrdinalIgnoreCase), "component holds a part of the value");
        }
    }

    [Theory]
    [InlineData("Mail (demo_Mail)", "\"\"")]
    [InlineData("Mail (demo_Mail)", "\"   \"")]
    [InlineData("Days (demo_Days)", "\"30\"")]
    [InlineData("Days (demo_Days)", "30")]
    [InlineData("$connections", "\"https://portal.fabrikam.example/sites/hr\"")]
    [InlineData("Unbound", "\"https://portal.fabrikam.example/sites/hr\"")]
    public void PO007_quiet_for_a_flow_default_that_is_empty_plain_or_not_bound(string key, string valueJson)
    {
        var s = B.Empty() with { Workflows = [FlowWithParam(key, null, valueJson)] };

        Assert.Empty(FlowFindings(s));
    }

    [Theory]
    [InlineData("demo_Mail", "ann.lee@contoso.com", "ann.lee@contoso.com")]
    [InlineData("DEMO_MAIL", "ann.lee@contoso.com", "ann.lee@contoso.com")]
    [InlineData("demo_Mail", "ann.lee@contoso.com", "  ann.lee@contoso.com ")]
    [InlineData("demo_Mail", "  ann.lee@contoso.com ", "ann.lee@contoso.com")]
    public void PO007_leaves_a_flow_default_identical_to_the_definitions_to_the_definitions_finding(string definitionSchema, string definitionDefault, string flowDefault)
    {
        var s = B.Empty() with
        {
            Workflows = [FlowWithParam("Mail (demo_Mail)", null, Quote(flowDefault))],
            EnvironmentVariables = [B.Env(definitionSchema, def: definitionDefault)],
        };

        var f = Assert.Single(Run(new PO007EnvironmentSpecificDefaultValue(), s));

        Assert.Equal($"Environment variable '{definitionSchema}' has a default value that looks like an email address.", f.Message);
    }

    [Fact]
    public void PO007_flow_default_differing_only_in_case_from_the_definitions_still_fires()
    {
        var s = B.Empty() with
        {
            Workflows = [FlowWithParam("Mail (demo_Mail)", null, Quote("Ann.Lee@contoso.com"))],
            EnvironmentVariables = [B.Env("demo_Mail", def: "ann.lee@contoso.com")],
        };

        Assert.Single(FlowFindings(s));
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
                "PO014, PO018 and PO036 not checked for canvas app 'CanvasApps/a_DocumentUri.msapp': no Src/*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).",
                "PO014, PO018 and PO036 not checked for canvas app 'CanvasApps/b_DocumentUri.msapp': no Src/*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).",
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

    private static Solution Code(params CanvasAppMeta[] apps) => B.Empty() with { CanvasApps = apps };

    [Fact]
    public void PO026_fires_for_a_missing_table_and_a_missing_connector_with_the_app_file()
    {
        var s = Code(B.CodeApp("demo_app", tables: ["demo_missingtable"], connectors: ["demo_missingconnector"]));

        var findings = Run(new PO026CodeAppDependencyMissing(), s);

        Assert.Equal(2, findings.Count);
        Assert.All(findings, f =>
        {
            Assert.Equal("PO026", f.RuleId);
            Assert.Equal(Severity.Warning, f.Severity);
            Assert.Equal("CanvasApps/demo_app.meta.xml", f.File);
        });
        Assert.Equal("Code app 'demo_app' depends on table 'demo_missingtable', which is not in the solution or its dependencies.", findings[0].Message);
        Assert.Equal("demo_missingtable", findings[0].Component);
        Assert.Equal("Code app 'demo_app' depends on connector 'demo_missingconnector', which is not in the solution or its dependencies.", findings[1].Message);
        Assert.Equal("demo_missingconnector", findings[1].Component);
    }

    [Fact]
    public void PO026_is_quiet_for_a_table_and_a_connector_held_as_root_components_of_the_same_type_in_any_case()
    {
        var s = Code(B.CodeApp("demo_app", tables: ["demo_order"], connectors: ["demo_api"])) with
        {
            RootComponents = [new("1", "DEMO_Order", null), new("372", "Demo_API", null)],
        };

        Assert.Empty(Run(new PO026CodeAppDependencyMissing(), s));
    }

    [Fact]
    public void PO026_is_quiet_for_a_table_and_a_connector_declared_in_missing_dependencies()
    {
        var s = Code(B.CodeApp("demo_app", tables: ["demo_order"], connectors: ["demo_api"])) with
        {
            MissingDependencies = [B.Dep("1", "Demo_Order"), B.Dep("372", "DEMO_API")],
        };

        Assert.Empty(Run(new PO026CodeAppDependencyMissing(), s));
    }

    [Fact]
    public void PO026_fires_for_a_table_named_only_by_a_connector_root_component_and_a_connector_named_only_by_a_table_one()
    {
        var s = Code(B.CodeApp("demo_app", tables: ["demo_one"], connectors: ["demo_two"])) with
        {
            RootComponents = [new("372", "demo_one", null), new("1", "demo_two", null)],
        };

        Assert.Equal(2, Run(new PO026CodeAppDependencyMissing(), s).Count);
    }

    [Fact]
    public void PO026_does_not_check_a_table_with_no_underscore_but_checks_a_connector_with_none()
    {
        var s = Code(B.CodeApp("demo_app", tables: ["systemuser", "team"], connectors: ["shared"]));

        var f = Assert.Single(Run(new PO026CodeAppDependencyMissing(), s));

        Assert.Equal("Code app 'demo_app' depends on connector 'shared', which is not in the solution or its dependencies.", f.Message);
        Assert.Equal("shared", f.Component);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(null)]
    public void PO026_checks_only_code_apps(int? type)
    {
        var s = Code(B.CodeApp("demo_app", tables: ["demo_missingtable"], connectors: ["demo_missingconnector"], type: type));

        Assert.Empty(Run(new PO026CodeAppDependencyMissing(), s));
    }

    [Fact]
    public void PO026_gives_a_note_for_a_code_app_with_unreadable_text_and_none_for_other_apps_or_readable_ones()
    {
        var unreadable = B.CodeApp("demo_app") with { Tables = null, Connectors = null, OtherTypes = null };
        var s = Code(unreadable, B.CodeApp("demo_page", type: 2) with { Tables = null, Connectors = null, OtherTypes = null }, B.CodeApp("demo_ok"));

        Assert.Empty(Run(new PO026CodeAppDependencyMissing(), s));
        Assert.Equal(
            ["PO026 not checked for code app 'demo_app': its CdsDependencies is missing or not in a shape PowerOps reads."],
            Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO026_still_checks_a_code_app_with_entries_of_other_types_and_says_which_types_it_did_not_check()
    {
        var s = Code(B.CodeApp("demo_app", tables: ["demo_missingtable"], otherTypes: [10001, 10000]), B.CodeApp("demo_page", type: 2, otherTypes: [10000]));

        var f = Assert.Single(Run(new PO026CodeAppDependencyMissing(), s));

        Assert.Equal("demo_missingtable", f.Component);
        Assert.Equal(
            ["PO026 not checked for code app 'demo_app': its CdsDependencies has entries of other component types (10001, 10000), which are not checked."],
            Analyzer.NotChecked(s));
    }

    [Theory]
    [InlineData(5, "10006, 10005, 10004, 10003, 10002")]
    [InlineData(6, "10006, 10005, 10004, 10003, 10002, and 1 more")]
    [InlineData(7, "10006, 10005, 10004, 10003, 10002, and 2 more")]
    public void PO026_names_at_most_five_other_types_in_its_note(int count, string types)
    {
        var s = Code(B.CodeApp("demo_app", otherTypes: Enumerable.Range(0, count).Select(i => 10006 - i)));

        Assert.Equal(
            [$"PO026 not checked for code app 'demo_app': its CdsDependencies has entries of other component types ({types}), which are not checked."],
            Analyzer.NotChecked(s));
    }

    [Fact]
    public void PO026_gives_a_finding_per_code_app_in_file_order()
    {
        var s = Code(B.CodeApp("demo_b", tables: ["demo_missing"]), B.CodeApp("demo_a", tables: ["demo_missing"]));

        var findings = Run(new PO026CodeAppDependencyMissing(), s);

        Assert.Equal(["CanvasApps/demo_a.meta.xml", "CanvasApps/demo_b.meta.xml"], findings.Select(f => f.File));
        Assert.Equal(["demo_a", "demo_b"], findings.Select(f => f.Message.Split('\'')[1]));
    }

    [Fact]
    public void PO026_runs_in_linear_time_on_100000_missing_tables_against_100000_root_components_and_100000_declared_names()
    {
        var s = Code(B.CodeApp("demo_app", tables: Enumerable.Range(0, 100_000).Select(i => $"demo_missing{i}"))) with
        {
            RootComponents = [.. Enumerable.Range(0, 100_000).Select(i => new RootComponent("1", $"demo_root{i}", null))],
            MissingDependencies = [.. Enumerable.Range(0, 100_000).Select(i => B.Dep("1", $"demo_declared{i}"))],
        };

        var sw = Stopwatch.StartNew();
        var findings = Run(new PO026CodeAppDependencyMissing(), s);

        Assert.Equal(100_000, findings.Count);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }

    private static Solution WithAgents(params Agent[] agents) => B.Empty() with { Agents = agents };

    private static string Long(int n) => new('x', n);

    [Theory]
    [InlineData("")]
    [InlineData("  \n\t ")]
    public void PO028_fires_for_empty_or_whitespace_instructions(string instructions)
    {
        var f = Assert.Single(Run(new PO028AgentInstructionsLength(), WithAgents(B.Agent(instructions: instructions))));

        Assert.Equal("PO028", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Agent 'demo_agent' has no instructions.", f.Message);
        Assert.Equal("demo_agent", f.Component);
        Assert.Equal("botcomponents/demo_agent.gpt.default/data", f.File);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void PO028_is_quiet_for_empty_instructions_unless_generative_orchestration_is_on(bool? generative)
    {
        var rule = new PO028AgentInstructionsLength();
        var s = WithAgents(B.Agent(instructions: "", generative: generative));

        Assert.Empty(Run(rule, s));
        Assert.Empty(rule.NotChecked(s));
    }

    [Theory]
    [InlineData(8000, "\n", false)]
    [InlineData(8000, "\r\n", false)]
    [InlineData(8000, "\n\n", false)]
    [InlineData(8001, "\n", true)]
    public void PO028_does_not_count_trailing_line_breaks(int length, string tail, bool fires)
    {
        var found = Run(new PO028AgentInstructionsLength(), WithAgents(B.Agent(instructions: Long(length) + tail)));

        Assert.Equal(fires, found.Count == 1);
        Assert.All(found, f => Assert.Contains("8001 characters", f.Message));
    }

    [Fact]
    public void PO028_counts_utf16_code_units()
    {
        var f = Assert.Single(Run(new PO028AgentInstructionsLength(), WithAgents(B.Agent(instructions: string.Concat(Enumerable.Repeat("\U0001F600", 4001))))));

        Assert.Contains("8002 characters", f.Message);
    }

    [Fact]
    public void PO028_fires_for_8001_characters_and_is_quiet_for_1_and_8000()
    {
        var f = Assert.Single(Run(new PO028AgentInstructionsLength(), WithAgents(B.Agent(instructions: Long(8001)))));

        Assert.Equal("Agent 'demo_agent' has instructions of 8001 characters, more than the 8,000 that Copilot Studio allows.", f.Message);
        Assert.Equal("botcomponents/demo_agent.gpt.default/data", f.File);
        Assert.Empty(Run(new PO028AgentInstructionsLength(), WithAgents(B.Agent(instructions: "x"))));
        Assert.Empty(Run(new PO028AgentInstructionsLength(), WithAgents(B.Agent(instructions: Long(8000)))));
    }

    [Fact]
    public void PO028_says_not_checked_when_the_instructions_are_unknown_and_not_when_they_are_known()
    {
        var rule = new PO028AgentInstructionsLength();
        var s = WithAgents(B.Agent(instructions: null));

        Assert.Empty(Run(rule, s));
        Assert.Equal(["PO028 not checked for agent 'demo_agent': it has no single Custom GPT component whose instructions are text."], rule.NotChecked(s));
        Assert.Empty(rule.NotChecked(WithAgents(B.Agent(instructions: ""))));
    }

    [Fact]
    public void PO028_does_not_read_instructions_from_configuration_json()
    {
        var rule = new PO028AgentInstructionsLength();
        foreach (var text in new[] { Long(8001), "" })
        {
            var s = WithAgents(B.Agent(instructions: text) with { InstructionsFromConfiguration = true, InstructionsFile = "bots/demo_agent/configuration.json" });

            Assert.Empty(Run(rule, s));
            Assert.Equal(["PO028 not checked for agent 'demo_agent': it has no single Custom GPT component whose instructions are text."], rule.NotChecked(s));
        }
    }

    [Fact]
    public void PO030_and_PO031_do_not_read_an_mcptool_kind_tool_of_a_standard_harness_agent()
    {
        static AgentTool Mcp(int n) => B.Tool($"demo_agent.tool.T{n:00}", mcp: true, mode: "Maker") with { Action = null, Kind = "McpTool" };
        var s = WithAgents(B.Agent(tools: Enumerable.Range(0, 31).Select(Mcp)));
        var one = WithAgents(B.Agent(tools: [Mcp(0)]));

        Assert.Empty(Run(new PO030ToolMakerConnection(), s));
        Assert.Empty(Run(new PO031AgentToolCount(), s));
        Assert.Empty(new PO031AgentToolCount().NotChecked(one));
    }

    [Fact]
    public void PO029_fires_for_authentication_none_with_the_bot_file()
    {
        var f = Assert.Single(Run(new PO029AgentWithoutAuthentication(), WithAgents(B.Agent(auth: 1))));

        Assert.Equal("PO029", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Agent 'demo_agent' has authentication None, so anyone with its link can chat with it.", f.Message);
        Assert.Equal("demo_agent", f.Component);
        Assert.Equal("bots/demo_agent/bot.xml", f.File);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void PO029_is_quiet_for_the_other_modes(int mode)
    {
        var rule = new PO029AgentWithoutAuthentication();
        var s = WithAgents(B.Agent(auth: mode));

        Assert.Empty(Run(rule, s));
        Assert.Empty(rule.NotChecked(s));
    }

    [Fact]
    public void PO029_says_not_checked_when_the_mode_is_unknown()
    {
        var rule = new PO029AgentWithoutAuthentication();
        var s = WithAgents(B.Agent(auth: null));

        Assert.Empty(Run(rule, s));
        Assert.Equal(["PO029 not checked for agent 'demo_agent': its bot.xml has no authenticationmode."], rule.NotChecked(s));
        Assert.Empty(rule.NotChecked(WithAgents(B.Agent(auth: 1))));
    }

    [Theory]
    [InlineData("Maker")]
    [InlineData("maker")]
    public void PO030_fires_for_a_tool_in_maker_mode(string mode)
    {
        var f = Assert.Single(Run(new PO030ToolMakerConnection(), WithAgents(B.Agent(tools: [B.Tool("demo_agent.topic.Weather", mode: mode)]))));

        Assert.Equal("PO030", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Tool 'demo_agent.topic.Weather' of agent 'demo_agent' uses the maker's credentials, so every user acts with the maker's permissions.", f.Message);
        Assert.Equal("demo_agent.topic.Weather", f.Component);
        Assert.Equal("botcomponents/demo_agent.topic.Weather/data", f.File);
    }

    [Theory]
    [InlineData("Invoker")]
    [InlineData(null)]
    public void PO030_is_quiet_for_invoker_mode_and_no_mode(string? mode) =>
        Assert.Empty(Run(new PO030ToolMakerConnection(), WithAgents(B.Agent(tools: [B.Tool(mode: mode)]))));

    private static IEnumerable<AgentTool> Tools(int n, string action = "InvokeConnectorTaskAction") =>
        Enumerable.Range(0, n).Select(i => B.Tool($"demo_agent.topic.T{i:D3}", action: action));

    [Fact]
    public void PO031_fires_for_31_counted_tools_and_is_quiet_for_30()
    {
        var f = Assert.Single(Run(new PO031AgentToolCount(), WithAgents(B.Agent(tools: Tools(31)))));

        Assert.Equal("PO031", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Agent 'demo_agent' has 31 tools, more than 30; Learn advises 25 to 30 at most.", f.Message);
        Assert.Equal("demo_agent", f.Component);
        Assert.Equal("bots/demo_agent/bot.xml", f.File);
        Assert.Empty(Run(new PO031AgentToolCount(), WithAgents(B.Agent(tools: Tools(30)))));
    }

    [Fact]
    public void PO031_does_not_count_a_connected_agent()
    {
        var thirty = Tools(30).Append(B.Tool("demo_agent.topic.Other", action: "InvokeConnectedAgentTaskAction"));
        var thirtyOne = Tools(30).Append(B.Tool("demo_agent.topic.Other")).Append(B.Tool("demo_agent.topic.Child", action: "InvokeConnectedAgentTaskAction"));

        Assert.Empty(Run(new PO031AgentToolCount(), WithAgents(B.Agent(tools: thirty))));
        Assert.Contains("has 31 tools", Assert.Single(Run(new PO031AgentToolCount(), WithAgents(B.Agent(tools: thirtyOne)))).Message);
    }

    [Fact]
    public void PO031_says_not_checked_for_30_or_fewer_tools_with_an_mcp_server()
    {
        var rule = new PO031AgentToolCount();
        var s = WithAgents(B.Agent(tools: Tools(3).Append(B.Tool("demo_agent.topic.M1", mcp: true)).Append(B.Tool("demo_agent.topic.M2", mcp: true))));

        Assert.Empty(Run(rule, s));
        Assert.Equal(["PO031 not checked for agent 'demo_agent': it has at least one MCP server, and the exports read here don't list the tools an MCP server publishes."], rule.NotChecked(s));
        Assert.Empty(rule.NotChecked(WithAgents(B.Agent(tools: Tools(3)))));
    }

    [Fact]
    public void PO031_gives_the_finding_and_no_note_for_31_tools_with_an_mcp_tool()
    {
        var rule = new PO031AgentToolCount();
        var s = WithAgents(B.Agent(tools: Tools(30).Append(B.Tool("demo_agent.topic.M1", mcp: true))));

        Assert.Single(Run(rule, s));
        Assert.Empty(rule.NotChecked(s));
    }

    [Fact]
    public void The_agent_rules_are_quiet_on_an_agent_outside_the_standard_harness_that_would_fire()
    {
        var tools = Tools(31).Append(B.Tool("demo_agent.topic.M", mcp: true, mode: "Maker"));
        var cli = B.Agent(auth: 1, instructions: "", generative: false, recognizer: "CLIAgentRecognizer", tools: tools);
        var noConfig = B.Agent(auth: 1, instructions: "", tools: tools) with { Configuration = null };

        foreach (var s in new[] { WithAgents(cli), WithAgents(noConfig) })
        {
            Assert.Empty(Analyzer.Run(s));
            Assert.StartsWith("PO027 to PO031 not checked for agent 'demo_agent': ", Assert.Single(Analyzer.NotChecked(s)));
        }

        // A second unplaced agent that would give every note if it were placed.
        var other = B.Agent("demo_other", auth: null, instructions: null, generative: null, recognizer: "CLIAgentRecognizer", tools: [B.Tool("demo_other.topic.M", mcp: true, mode: "Invoker")]);
        var both = WithAgents(cli, other);

        Assert.Equal(2, Analyzer.NotChecked(both).Count);
        Assert.All(Analyzer.NotChecked(both), n => Assert.StartsWith("PO027 to PO031 not checked for agent '", n));
        Assert.Contains(Analyzer.NotChecked(both), n => n.Contains("'demo_agent'"));
        Assert.Contains(Analyzer.NotChecked(both), n => n.Contains("'demo_other'"));
        Assert.Empty(new PO028AgentInstructionsLength().NotChecked(both));
        Assert.Empty(new PO029AgentWithoutAuthentication().NotChecked(both));
        Assert.Empty(new PO031AgentToolCount().NotChecked(both));
    }

    [Fact]
    public void The_agent_rules_give_findings_in_file_order()
    {
        var s = WithAgents(B.Agent("demo_b", auth: 1, instructions: "", tools: [B.Tool("demo_b.topic.Z", mode: "Maker"), B.Tool("demo_b.topic.A", mode: "Maker")]), B.Agent("demo_a", auth: 1, instructions: "", tools: [B.Tool("demo_a.topic.X", mode: "Maker")]));

        Assert.Equal(["bots/demo_a/bot.xml", "bots/demo_b/bot.xml"], Run(new PO029AgentWithoutAuthentication(), s).Select(f => f.File));
        Assert.Equal(["botcomponents/demo_a.gpt.default/data", "botcomponents/demo_b.gpt.default/data"], Run(new PO028AgentInstructionsLength(), s).Select(f => f.File));
        Assert.Equal(["demo_a.topic.X", "demo_b.topic.A", "demo_b.topic.Z"], Run(new PO030ToolMakerConnection(), s).Select(f => f.Component));
    }

    [Fact]
    public void The_agent_rules_run_in_linear_time_on_10000_agents_with_100_tools_each()
    {
        var tools = Enumerable.Range(0, 100).Select(i => B.Tool($"demo_agent.topic.T{i:D3}", mcp: i == 0, mode: i == 0 ? "Maker" : "Invoker")).ToList();
        var s = WithAgents([.. Enumerable.Range(0, 10_000).Select(i => B.Agent($"demo_agent{i}", auth: 1, instructions: Long(8001), generative: false, tools: tools))]);

        var sw = Stopwatch.StartNew();
        var rules = Analyzer.Rules.Where(r => r.Id is "PO027" or "PO028" or "PO029" or "PO030" or "PO031").ToList();
        var counts = rules.Select(r => r.Evaluate(s).Count()).ToList();
        var notes = rules.OfType<INotChecked>().Sum(r => r.NotChecked(s).Count());

        Assert.Equal([10_000, 10_000, 10_000, 10_000, 10_000], counts);
        Assert.Equal(0, notes);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }

    private const string SystemAdmin = "{627090FF-40A3-4053-8790-584EDC5BE201}";
    private const string Customizer = "{119F245C-3CC8-4B62-B31C-D1A046CED15D}";
    private const string BasicUser = "236750cd-45ae-4939-ab12-b24b920ced93";
    private static readonly string CustomRole = B.Guid(0x60a1);
    private static readonly string OtherRole = B.Guid(0x60a2);

    private static Solution WithApp(string[] roles, params Role[] inSolution) => B.Empty() with { AppModules = [B.App(roles: roles)], Roles = inSolution };

    // One model-driven app with no role map, so PO033 applies and PO032 has nothing to check.
    private static Solution WithRoles(params Role[] roles) => B.Empty() with { Roles = roles, AppModules = [B.App()] };

    [Fact]
    public void PO032_fires_for_an_app_shared_with_only_the_two_admin_templates_in_any_case()
    {
        // As Learn writes the ids (upper case, braces) and as the reader gives them (lower case, none).
        var lower = new[] { SystemAdmin, Customizer }.Select(r => r.Trim('{', '}').ToLowerInvariant()).ToArray();
        foreach (var roles in new[] { new[] { SystemAdmin, Customizer }, lower, [SystemAdmin], [lower[1], Customizer] })
        {
            var s = WithApp(roles);

            var f = Assert.Single(Run(new PO032AppSharedWithAdminsOnly(), s));

            Assert.Equal("PO032", f.RuleId);
            Assert.Equal(Severity.Warning, f.Severity);
            Assert.Equal("App 'demo_app' is shared only with System Administrator or System Customizer, so users without an admin role may not be able to open it.", f.Message);
            Assert.Equal("demo_app", f.Component);
            Assert.Equal("AppModules/demo_app/AppModule.xml", f.File);
            Assert.Empty(new PO032AppSharedWithAdminsOnly().NotChecked(s));
        }
    }

    [Fact]
    public void PO032_is_quiet_when_a_role_in_the_solution_or_another_template_is_also_mapped_and_for_an_empty_role_map()
    {
        var rule = new PO032AppSharedWithAdminsOnly();
        var custom = B.SecurityRole("Front desk", CustomRole, "prvReadAppModule");
        var solutions = new[]
        {
            WithApp([SystemAdmin.ToLowerInvariant().Trim('{', '}'), CustomRole], custom),
            WithApp([SystemAdmin.ToLowerInvariant().Trim('{', '}'), BasicUser]),
            WithApp([]),
        };

        foreach (var s in solutions)
        {
            Assert.Empty(Run(rule, s));
            Assert.Empty(rule.NotChecked(s));
        }
    }

    [Fact]
    public void PO032_says_not_checked_for_ids_that_are_neither_a_template_nor_a_role_in_the_solution()
    {
        var rule = new PO032AppSharedWithAdminsOnly();
        var withAdmin = WithApp([SystemAdmin.ToLowerInvariant().Trim('{', '}'), CustomRole, OtherRole, CustomRole]);
        var only = WithApp([CustomRole]);

        Assert.Empty(Run(rule, withAdmin));
        Assert.Equal(["PO032 not checked for app 'demo_app': 2 of its role map ids are neither a documented role template nor a role in the solution."], rule.NotChecked(withAdmin));
        Assert.Empty(Run(rule, only));
        Assert.Equal(["PO032 not checked for app 'demo_app': 1 of its role map ids are neither a documented role template nor a role in the solution."], rule.NotChecked(only));
    }

    [Fact]
    public void PO033_fires_for_a_role_with_a_table_privilege_and_none_of_the_three_app_opening_ones()
    {
        var s = WithRoles(B.SecurityRole("Front desk", CustomRole, "prvReadpops_loan", "prvExportToExcel"));

        var f = Assert.Single(Run(new PO033RoleWithoutAppBaseline(), s));

        Assert.Equal("PO033", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Role 'Front desk' has table privileges but none of prvReadAppModule, prvReadUser and prvReadUserQuery, so a user who holds only this role may not be able to open a model-driven app.", f.Message);
        Assert.Equal("Front desk", f.Component);
        Assert.Equal("Roles/Front desk.xml", f.File);
        Assert.Empty(new PO033RoleWithoutAppBaseline().NotChecked(s));
    }

    [Theory]
    [InlineData("prvReadAppModule")]
    [InlineData("prvReadUser")]
    [InlineData("prvreaduserquery")]
    public void PO033_is_quiet_when_the_role_holds_any_one_of_the_three(string baseline)
    {
        Assert.Empty(Run(new PO033RoleWithoutAppBaseline(), WithRoles(B.SecurityRole("Front desk", CustomRole, "prvReadpops_loan", baseline))));
    }

    [Fact]
    public void PO033_is_quiet_for_a_role_with_no_table_privileges_and_for_a_template_role()
    {
        var rule = new PO033RoleWithoutAppBaseline();
        var s = WithRoles(
            B.SecurityRole("Empty", CustomRole),
            B.SecurityRole("Misc", OtherRole, "prvExportToExcel"),
            B.SecurityRole("Basic User", BasicUser, "prvReadpops_loan"));

        Assert.Empty(Run(rule, s));
        Assert.Empty(rule.NotChecked(s));
    }

    [Fact]
    public void PO033_is_quiet_in_a_solution_without_a_model_driven_app_and_its_note_names_only_PO034()
    {
        var rule = new PO033RoleWithoutAppBaseline();
        var s = WithRoles(B.SecurityRole("Front desk", CustomRole, "prvReadpops_loan"), B.SecurityRoleUnread("None", OtherRole)) with { AppModules = [] };

        Assert.Empty(Run(rule, s));
        Assert.Equal(["PO034 not checked for role 'None': its file has no RolePrivileges element, so its privileges are not known."], rule.NotChecked(s));
    }

    [Fact]
    public void PO033_and_PO034_share_one_note_for_a_role_whose_privileges_are_not_known()
    {
        var s = WithRoles(B.SecurityRoleUnread("None", CustomRole), B.SecurityRoleUnread("Basic User", BasicUser));

        Assert.Empty(Run(new PO033RoleWithoutAppBaseline(), s));
        Assert.Empty(Run(new PO034RoleWritesUnusedTable(), s));
        Assert.Equal(["PO033 and PO034 not checked for role 'None': its file has no RolePrivileges element, so its privileges are not known."], new PO033RoleWithoutAppBaseline().NotChecked(s));
        Assert.Empty(new PO034RoleWritesUnusedTable().NotChecked(s));
    }

    [Fact]
    public void PO034_fires_once_for_a_table_naming_create_and_write()
    {
        var s = WithRoles(B.SecurityRole("Front desk", CustomRole, "prvCreateSharePointData", "prvWriteSharePointData", "prvReadSharePointData"));

        var f = Assert.Single(Run(new PO034RoleWritesUnusedTable(), s));

        Assert.Equal("PO034", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Role 'Front desk' has Create and Write on 'SharePointData', which no component in the solution uses, so it may grant more than the solution needs.", f.Message);
        Assert.Equal("Front desk", f.Component);
        Assert.Equal("Roles/Front desk.xml", f.File);
        Assert.Empty(new PO034RoleWritesUnusedTable().NotChecked(s));
    }

    [Fact]
    public void PO034_fires_once_per_table_with_the_verbs_that_table_has_in_table_order()
    {
        var s = WithRoles(B.SecurityRole("Front desk", CustomRole, "prvWriteZeta", "prvCreateAlpha", "prvWriteALPHA", "prvCreateBeta"));

        Assert.Equal(
            [
                "Role 'Front desk' has Create and Write on 'Alpha', which no component in the solution uses, so it may grant more than the solution needs.",
                "Role 'Front desk' has Create on 'Beta', which no component in the solution uses, so it may grant more than the solution needs.",
                "Role 'Front desk' has Write on 'Zeta', which no component in the solution uses, so it may grant more than the solution needs.",
            ],
            Run(new PO034RoleWritesUnusedTable(), s).Select(f => f.Message));
    }

    [Fact]
    public void PO034_is_quiet_for_a_table_the_solution_uses()
    {
        var role = B.SecurityRole("Front desk", CustomRole, "prvCreatepops_Loan", "prvWritepops_Loan");
        var flowText = B.Flow() with { ClientData = """{"entityName":"pops_loans","x":"POPS_LOAN"}""" };
        var used = new[]
        {
            WithRoles(role) with { RootComponents = [new RootComponent("1", "POPS_loan", null)] },
            WithRoles(role) with { MissingDependencies = [B.Dep("1", "pops_loan")] },
            WithRoles(role) with { CanvasApps = [B.CodeApp("demo_code", tables: ["Pops_Loan"])] },
            WithRoles(role) with { Workflows = [flowText] },
        };

        foreach (var s in used)
        {
            Assert.Empty(Run(new PO034RoleWritesUnusedTable(), s));
            Assert.Empty(new PO034RoleWritesUnusedTable().NotChecked(s));
        }
    }

    [Fact]
    public void PO034_is_quiet_for_user_entity_ui_settings_for_read_and_delete_and_for_a_template_role()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var s = WithRoles(
            B.SecurityRole("Front desk", CustomRole, "prvWriteUserEntityUISettings", "prvReadpops_x", "prvDeletepops_x", "prvAssignpops_x"),
            B.SecurityRole("Basic User", BasicUser, "prvWritepops_x"));

        Assert.Empty(Run(rule, s));
        Assert.Empty(rule.NotChecked(s));
    }

    [Fact]
    public void PO034_gives_a_note_instead_of_findings_when_the_solution_holds_a_component_it_cannot_read()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var role = B.SecurityRole("Front desk", CustomRole, "prvCreatepops_a", "prvWritepops_b");
        var cases = new (Solution Solution, string Reason)[]
        {
            (WithRoles(role) with { RootComponents = [new RootComponent("91", null, null)] }, "a plugin assembly"),
            (WithRoles(role) with { CanvasApps = [B.CodeApp("demo_code") with { Tables = null }] }, "a canvas app with no readable CdsDependencies"),
            (WithRoles(role) with { Workflows = [B.Flow() with { ClientData = null }] }, "a workflow with no readable definition"),
            (WithRoles(role) with { Agents = [B.Agent(tools: [B.Tool()])] }, "agent 'demo_agent' tool 'demo_agent.topic.Orders' (it names no table)"),
            (WithRoles(role) with { RootComponents = [new RootComponent("91", null, null)], Agents = [B.Agent(tools: [B.Tool()])] }, "a plugin assembly"),
        };

        foreach (var (s, reason) in cases)
        {
            Assert.Empty(Run(rule, s));
            Assert.Equal([$"PO034 not checked for role 'Front desk': 2 of the tables it can create or write are used by no component PowerOps reads, and the solution holds {reason}, whose table use isn't read."], rule.NotChecked(s));
        }
    }

    private static string Note(string reason, int left = 2) =>
        $"PO034 not checked for role 'Front desk': {left} of the tables it can create or write are used by no component PowerOps reads, and the solution holds {reason}, whose table use isn't read.";

    private static Solution Roles2() =>
        WithRoles(B.SecurityRole("Front desk", CustomRole, "prvCreatepops_a", "prvWritepops_b"));

    private static AgentTool Dataverse(string? set = "pops_bs", string? name = "pops_b") =>
        B.Tool() with { Table = new ToolTable(set, name) };

    [Fact]
    public void PO034_names_the_agent_component_and_reason_that_still_blocks_it()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var longKind = new string('k', 150);
        var cut = new string('k', 100) + "…";
        var cases = new (Agent Agent, string Reason)[]
        {
            (B.Agent(unread: ["botcomponents/demo_agent.topic.Odd/data"]), "agent 'demo_agent' file 'botcomponents/demo_agent.topic.Odd/data' (YAML keys PowerOps can't map)"),
            (B.Agent() with { OtherComponents = [new AgentComponent("Widget", "botcomponents/demo_agent.x/data")] }, "agent 'demo_agent' file 'botcomponents/demo_agent.x/data' (a component of kind 'Widget')"),
            (B.Agent() with { OtherComponents = [new AgentComponent(null, "botcomponents/demo_agent.x/data")] }, "agent 'demo_agent' file 'botcomponents/demo_agent.x/data' (a component with no kind)"),
            (B.Agent() with { OtherComponents = [new AgentComponent(longKind, "botcomponents/demo_agent.x/data")] }, $"agent 'demo_agent' file 'botcomponents/demo_agent.x/data' (a component of kind '{cut}')"),
            (B.Agent() with { ActionDialogs = [new AgentComponent("InvokeFlowAction", "botcomponents/demo_agent.topic.T/data")] }, "agent 'demo_agent' file 'botcomponents/demo_agent.topic.T/data' (a topic or child agent with a 'InvokeFlowAction' node)"),
            (B.Agent(tools: [B.Tool(mcp: true)]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (an MCP tool)"),
            (B.Agent(tools: [B.Tool() with { Kind = "McpTool", Action = null }]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (an MCP tool)"),
            (B.Agent(tools: [B.Tool()]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (it names no table)"),
            (B.Agent(tools: [B.Tool() with { Table = new ToolTable(null, null) }]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (its entityName is not a literal entity set name)"),
            (B.Agent(tools: [Dataverse("pops_cs", null)]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (its entity set 'pops_cs' names no table in the solution)"),
            (B.Agent(tools: [Dataverse(longKind, null)]), $"agent 'demo_agent' tool 'demo_agent.topic.Orders' (its entity set '{cut}' names no table in the solution)"),
            (B.Agent(tools: [B.Tool(action: "InvokeFlowTaskAction")]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (its flow is not in the solution)"),
            (B.Agent(tools: [B.Tool(action: "InvokeFlowTaskAction") with { FlowId = B.Guid(0xaa) }]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (its flow is not in the solution)"),
            (B.Agent(tools: [B.Tool(action: "InvokeConnectedAgentTaskAction")]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (its action is 'InvokeConnectedAgentTaskAction')"),
            (B.Agent(tools: [B.Tool(action: longKind)]), $"agent 'demo_agent' tool 'demo_agent.topic.Orders' (its action is '{cut}')"),
            (B.Agent(tools: [B.Tool() with { Action = null }]), "agent 'demo_agent' tool 'demo_agent.topic.Orders' (it has no action kind)"),
        };

        foreach (var (agent, reason) in cases)
        {
            var s = Roles2() with { Agents = [agent] };
            Assert.Empty(Run(rule, s));
            Assert.Equal([Note(reason)], rule.NotChecked(s));
        }
    }

    [Fact]
    public void PO034_names_the_first_agent_with_a_blocker_and_within_it_the_file_before_the_tool()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var clean = B.Agent("demo_a", tools: [Dataverse()]);
        var blocked = B.Agent("demo_b", tools: [B.Tool("demo_b.topic.T")], unread: ["botcomponents/demo_b.topic.Odd/data"]);
        var s = Roles2() with { Agents = [blocked, clean] };

        Assert.Equal([Note("agent 'demo_b' file 'botcomponents/demo_b.topic.Odd/data' (YAML keys PowerOps can't map)", 1)], rule.NotChecked(s));
        Assert.Equal([Note("agent 'demo_b' file 'botcomponents/demo_b.topic.Odd/data' (YAML keys PowerOps can't map)", 1)], rule.NotChecked(s with { Agents = [clean, blocked] }));
    }

    [Fact]
    public void PO034_gives_findings_for_an_agent_whose_tools_are_all_known_and_ignores_work_iq_capabilities()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var agent = B.Agent(tools: [Dataverse()]) with { Capabilities = [new AgentCapability("demo_agent.iq.mail", "mail", "Write", "botcomponents/demo_agent.iq.mail/data")] };
        var s = Roles2() with { Agents = [agent] };

        Assert.Empty(rule.NotChecked(s));
        Assert.Contains("'pops_a'", Assert.Single(Run(rule, s)).Message);
    }

    [Fact]
    public void PO034_counts_a_business_rules_table_and_a_tools_table_as_used()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var role = B.SecurityRole("Front desk", CustomRole, "prvCreatepops_x", "prvWritepops_y", "prvWritepops_z");
        var rowRule = B.Flow(category: 2) with { ClientData = null, PrimaryEntity = "pops_Y" };
        var s = WithRoles(role) with { Workflows = [rowRule], Agents = [B.Agent(tools: [Dataverse("pops_zs", "pops_z")])] };

        var f = Assert.Single(Run(rule, s));
        Assert.Equal("Role 'Front desk' has Create on 'pops_x', which no component in the solution uses, so it may grant more than the solution needs.", f.Message);
        Assert.Empty(rule.NotChecked(s));
        Assert.Empty(Run(rule, s with { Agents = [B.Agent(tools: [Dataverse("pops_xs", "pops_x"), Dataverse("pops_zs", "pops_z")])] }));
    }

    [Fact]
    public void PO034_still_blocks_on_a_workflow_with_no_definition_unless_it_is_a_business_rule_with_a_table()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var reason = "a workflow with no readable definition";

        foreach (var w in new[] { B.Flow(category: 0) with { ClientData = null, PrimaryEntity = "account" }, B.Flow(category: 2) with { ClientData = null } })
        {
            Assert.Equal([Note(reason)], rule.NotChecked(Roles2() with { Workflows = [w] }));
        }

        Assert.Empty(rule.NotChecked(Roles2() with { Workflows = [B.Flow(category: 2) with { ClientData = null, PrimaryEntity = "account" }] }));
    }

    [Fact]
    public void PO034_reads_a_flow_tool_through_the_flow_check()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var role = B.SecurityRole("Front desk", CustomRole, "prvWritepops_a");
        AgentTool FlowTool(string id) => B.Tool(action: "InvokeFlowTaskAction") with { FlowId = id };
        Solution With(Workflow flow) => WithRoles(role) with { Workflows = [flow], Agents = [B.Agent(tools: [FlowTool(B.Guid(0xaa))])] };
        var naming = B.Flow(id: B.Guid(0xaa)) with { ClientData = """{"entityName":"pops_a"}""" };
        var other = B.Flow(id: B.Guid(0xaa)) with { ClientData = """{"entityName":"pops_other"}""" };
        var classic = B.Flow(id: B.Guid(0xaa), category: 0) with { ClientData = null };

        Assert.Empty(Run(rule, With(naming)));
        Assert.Empty(rule.NotChecked(With(naming)));
        Assert.Single(Run(rule, With(other)));
        Assert.Empty(rule.NotChecked(With(other)));
        Assert.Empty(Run(rule, With(classic)));
        Assert.Equal(["PO034 not checked for role 'Front desk': 1 of the tables it can create or write are used by no component PowerOps reads, and the solution holds a workflow with no readable definition, whose table use isn't read."], rule.NotChecked(With(classic)));
    }

    [Fact]
    public void PO034_gives_no_note_when_no_table_is_left()
    {
        var rule = new PO034RoleWritesUnusedTable();
        var s = WithRoles(B.SecurityRole("Front desk", CustomRole, "prvWritepops_a")) with { RootComponents = [new RootComponent("91", null, null), new RootComponent("1", "pops_a", null)] };

        Assert.Empty(Run(rule, s));
        Assert.Empty(rule.NotChecked(s));
    }

    [Fact]
    public void The_role_rules_give_findings_in_file_order()
    {
        var s = WithRoles(B.SecurityRole("Zed", CustomRole, "prvWritepops_a"), B.SecurityRole("Abe", OtherRole, "prvWritepops_a"));

        Assert.Equal(["Roles/Abe.xml", "Roles/Zed.xml"], Run(new PO033RoleWithoutAppBaseline(), s).Select(f => f.File));
        Assert.Equal(["Roles/Abe.xml", "Roles/Zed.xml"], Run(new PO034RoleWritesUnusedTable(), s).Select(f => f.File));
    }

    private static readonly NavigationProperty[] Exported =
    [
        new("demo_CostCentre", "demo_Expense", "demo_expenses"),
        new("demo_Approver", "demo_Expense", "demo_expenses"),
        new("demo_Expense_CostCentre_demo_CostCentre", "demo_CostCentre", "demo_costcentres"),
    ];

    private static object DataverseAction(Dictionary<string, object> parameters, string api = "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps") =>
        new { type = "OpenApiConnection", inputs = new { host = new { apiId = api, operationId = "ListRecords" }, parameters } };

    private static Dictionary<string, object> Expand(string expand, string set = "demo_expenses") => new() { ["entityName"] = set, ["$expand"] = expand };

    private static Dictionary<string, object> Bind(string name, string set = "demo_expenses") => new() { ["entityName"] = set, [$"item/{name}@odata.bind"] = "demo_costcentres(1)" };

    private static Solution Navigation(Dictionary<string, object> actions, IReadOnlyList<NavigationProperty>? exported = null, string flow = "Expenses") =>
        B.Empty() with { Workflows = [B.Flow(name: flow, actions: actions)], NavigationProperties = exported ?? Exported };

    private static Solution Navigation(string action, Dictionary<string, object> parameters, IReadOnlyList<NavigationProperty>? exported = null) =>
        Navigation(new Dictionary<string, object> { [action] = DataverseAction(parameters) }, exported);

    [Fact]
    public void PO035_fires_for_an_expand_name_in_the_wrong_case_and_names_both_spellings()
    {
        var f = Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand("demo_costcentre"))));

        Assert.Equal("PO035", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Flow 'Expenses' action 'List' names 'demo_costcentre' on 'demo_expenses', but the navigation property of table 'demo_Expense' is 'demo_CostCentre'; the Web API matches navigation property names by case.", f.Message);
        Assert.Equal("Workflows/Expenses-11111111-1111-1111-1111-111111111111.json", f.File);
        Assert.Equal("List", f.Component);
    }

    [Fact]
    public void PO035_fires_for_a_bind_key_in_the_wrong_case()
    {
        var f = Assert.Single(Run(new PO035NavigationName(), Navigation("Add", Bind("demo_costcentre"))));

        Assert.Equal("Flow 'Expenses' action 'Add' names 'demo_costcentre' on 'demo_expenses', but the navigation property of table 'demo_Expense' is 'demo_CostCentre'; the Web API matches navigation property names by case.", f.Message);
    }

    [Fact]
    public void PO035_fires_for_the_referenced_tables_collection_valued_name_in_the_wrong_case()
    {
        var f = Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand("demo_expense_costcentre_demo_costcentre", "demo_costcentres"))));

        Assert.Equal("Flow 'Expenses' action 'List' names 'demo_expense_costcentre_demo_costcentre' on 'demo_costcentres', but the navigation property of table 'demo_CostCentre' is 'demo_Expense_CostCentre_demo_CostCentre'; the Web API matches navigation property names by case.", f.Message);
    }

    [Fact]
    public void PO035_fires_once_for_the_one_wrong_name_of_a_comma_list_with_spaces()
    {
        var f = Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand(" demo_Approver , demo_costcentre "))));

        Assert.Contains("names 'demo_costcentre'", f.Message);
    }

    [Theory]
    [InlineData("demo_CostCentre")]
    [InlineData("demo_unknown")]
    [InlineData("demo_costcentre($select=demo_name)")]
    [InlineData("@{variables('Expand')}")]
    [InlineData("demo_expense_costcentre_demo_costcentre")] // another table's property: the action's own table has others
    [InlineData("demo_Approver($select=demo_name),demo_costcentre")] // one item that isn't a plain name: the whole value isn't read
    [InlineData("demo_Approver($select=demo_name,demo_costcentre,demo_x)")]
    public void PO035_quiet_for_an_expand_it_does_not_flag(string expand)
    {
        Assert.Empty(Run(new PO035NavigationName(), Navigation("List", Expand(expand))));
    }

    [Fact]
    public void PO035_matches_the_actions_entity_set_ignoring_case()
    {
        Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand("demo_costcentre", "Demo_Expenses"))));
    }

    [Fact]
    public void PO035_quiet_for_the_exact_case_bind_an_expression_table_no_exported_properties_and_another_connector()
    {
        var rule = new PO035NavigationName();

        Assert.Empty(Run(rule, Navigation("Add", Bind("demo_CostCentre"))));
        Assert.Empty(Run(rule, Navigation("List", Expand("demo_costcentre", "@parameters('Table')"))));
        Assert.Empty(Run(rule, Navigation("List", Expand("demo_costcentre"), [])));
        Assert.Empty(Run(rule, Navigation(new Dictionary<string, object> { ["Mail"] = DataverseAction(Expand("demo_costcentre"), "/providers/Microsoft.PowerApps/apis/shared_office365") })));
    }

    [Fact]
    public void PO035_gives_one_finding_for_a_name_in_expand_and_a_bind_key_and_for_a_relationship_read_twice()
    {
        var parameters = Expand("demo_costcentre");
        parameters["item/demo_costcentre@odata.bind"] = "demo_costcentres(1)";

        Assert.Single(Run(new PO035NavigationName(), Navigation("Add", parameters)));
        Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand("demo_costcentre"), [.. Exported, .. Exported])));
    }

    [Fact]
    public void PO035_names_the_first_case_insensitive_match_in_read_order()
    {
        NavigationProperty upper = new("DEMO_COSTCENTRE", "demo_Expense", "demo_expenses");

        Assert.EndsWith("is 'demo_CostCentre'; the Web API matches navigation property names by case.", Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand("demo_costcentre"), [Exported[0], upper]))).Message);
        Assert.EndsWith("is 'DEMO_COSTCENTRE'; the Web API matches navigation property names by case.", Assert.Single(Run(new PO035NavigationName(), Navigation("List", Expand("demo_costcentre"), [upper, Exported[0]]))).Message);
    }

    [Fact]
    public void PO035_gives_findings_in_flow_order_then_action_walk_order()
    {
        var zed = B.Flow(name: "Zed", actions: new Dictionary<string, object>
        {
            ["Add"] = DataverseAction(Bind("demo_costcentre")),
            ["Scope"] = new { type = "Scope", actions = new Dictionary<string, object> { ["List"] = DataverseAction(Expand("demo_approver")) } },
        });
        var abe = B.Flow(name: "Abe", actions: new Dictionary<string, object> { ["Get"] = DataverseAction(Expand("demo_costcentre")) });
        var s = B.Empty() with { Workflows = [zed, abe], NavigationProperties = Exported };

        Assert.Equal(["Zed/Add", "Zed/List", "Abe/Get"], Run(new PO035NavigationName(), s).Select(f => f.File[10..f.File.IndexOf('-')] + "/" + f.Component));
    }

    /// <summary>A formula whose text after the `=` is <paramref name="length"/> characters, with one space in it.</summary>
    private static string Repeated(int length) => "=Set(varNote, \"" + new string('x', length - 16) + "\")";

    private static IReadOnlyList<Finding> Repeats(params CanvasFormula[] formulas) =>
        Run(new PO036CanvasRepeatedFormula(), B.Empty() with { CanvasFormulas = formulas });

    [Fact]
    public void PO036_fires_for_a_formula_of_80_characters_in_three_places_that_differ_only_in_whitespace()
    {
        var text = Repeated(80);
        Assert.Equal(80, text.Length - 1);

        var f = Assert.Single(Repeats(
            B.Formula(text, "icoOpen"),
            B.Formula("=  " + text[1..].Replace(" ", "\r\n    ") + " \n", "galOrders"),
            B.Formula(text.Replace(" ", " \t "), "btnOpen")));

        Assert.Equal("PO036", f.RuleId);
        Assert.Equal(Severity.Warning, f.Severity);
        Assert.Equal("Canvas app 'Demo' has the same formula in 3 places (btnOpen.OnSelect, galOrders.OnSelect, icoOpen.OnSelect); a change must be made in each.", f.Message);
        Assert.Equal("btnOpen.OnSelect", f.Component);
        Assert.Equal("CanvasApps/demo_DocumentUri.msapp", f.File);
        Assert.DoesNotContain("varNote", f.Message);
        Assert.DoesNotContain("xxx", f.Message);
    }

    [Fact]
    public void PO036_names_the_first_three_places_in_ordinal_order_and_counts_the_rest()
    {
        var text = Repeated(120);

        var f = Assert.Single(Repeats(B.Formula(text, "btnB"), B.Formula(text, "Zed", "Items"), B.Formula(text, "abe"), B.Formula(text, "btnA"), B.Formula(text, "btnA", "OnChange")));

        Assert.Equal("Canvas app 'Demo' has the same formula in 5 places (Zed.Items, abe.OnSelect, btnA.OnChange, and 2 more); a change must be made in each.", f.Message);
        Assert.Equal("Zed.Items", f.Component);
    }

    [Fact]
    public void PO036_takes_the_file_of_the_first_place_when_an_apps_source_is_in_several_files()
    {
        const string Src = "canvasapps/demo_tracker/demo_tracker_DocumentUri/Src/";
        var text = Repeated(80);

        var f = Assert.Single(Repeats(
            B.Formula(text, "c") with { File = Src + "Screen1.pa.yaml" },
            B.Formula(text, "a") with { File = Src + "Screen3.pa.yaml" },
            B.Formula(text, "b") with { File = Src + "Screen2.pa.yaml" }));

        Assert.Equal("a.OnSelect", f.Component);
        Assert.Equal(Src + "Screen3.pa.yaml", f.File);
    }

    [Fact]
    public void PO036_gives_one_finding_per_repeated_formula_in_read_order()
    {
        var one = Repeated(80);
        var two = Repeated(81);

        var found = Repeats(B.Formula(two, "z1"), B.Formula(one, "a1"), B.Formula(two, "z2"), B.Formula(one, "a2"), B.Formula(two, "z3"), B.Formula(one, "a3"));

        Assert.Equal(["z1.OnSelect", "a1.OnSelect"], found.Select(f => f.Component));
    }

    [Fact]
    public void PO036_quiet_for_two_places()
    {
        var text = Repeated(80);

        Assert.Empty(Repeats(B.Formula(text, "a"), B.Formula(text, "b")));
    }

    [Fact]
    public void PO036_quiet_for_a_formula_of_79_characters_in_five_places_because_the_leading_equals_sign_is_not_counted()
    {
        var text = Repeated(79);
        Assert.Equal(80, text.Length);

        Assert.Empty(Repeats([.. "abcde".Select(c => B.Formula(text, c.ToString()))]));
    }

    [Theory]
    [InlineData("Other")]
    [InlineData("Demo")] // display names need not be unique: the app is its folder
    public void PO036_quiet_for_three_places_spread_over_two_apps(string otherName)
    {
        var text = Repeated(80);

        Assert.Empty(Repeats(B.Formula(text, "a"), B.Formula(text, "b"), B.Formula(text, "c", app: otherName) with { File = "CanvasApps/other_DocumentUri.msapp" }));
    }

    [Fact]
    public void PO036_quiet_when_the_third_entry_is_a_place_listed_twice()
    {
        var text = Repeated(80);

        Assert.Empty(Repeats(B.Formula(text, "a"), B.Formula(text, "b"), B.Formula(text, "a")));
    }

    [Fact]
    public void PO036_quiet_when_the_third_formula_differs_in_case_or_in_one_character()
    {
        var text = Repeated(80);

        Assert.Empty(Repeats(B.Formula(text, "a"), B.Formula(text, "b"), B.Formula(text.Replace("varNote", "VarNote"), "c"), B.Formula(text.Replace("varNote", "varNota"), "d")));
    }

    [Fact]
    public void PO036_is_a_registered_warning_with_a_fix_sentence()
    {
        var rule = Assert.IsAssignableFrom<IRule>(Analyzer.Find("PO036"));

        Assert.Equal(Severity.Warning, rule.Severity);
        Assert.Equal("Canvas app repeats the same formula in three or more places", rule.Title);
        Assert.Equal("Define the formula once as a named formula or user-defined function in App.Formulas, or in a component, and use that name in each place.", rule.Fix);
    }

    // M99: PO037 to PO040 read FlowDefinition.Actions and the trigger's recurrence.

    private static Solution FlowOf(string actions, string triggers = "{}") =>
        B.Empty() with { Workflows = [B.Flow() with { ClientData = "{\"properties\":{\"definition\":{\"triggers\":" + triggers + ",\"actions\":" + actions + "}}}" }] };

    private const string MailHost = """{"host":{"apiId":"/providers/Microsoft.PowerApps/apis/shared_office365","operationId":"SendEmailV2"}}""";

    private static string Mail(string runAfter = "{}") => """{"type":"OpenApiConnection","runAfter":""" + runAfter + ""","inputs":""" + MailHost + "}";

    private static string Outward(string type) => "{\"type\":\"" + type + "\",\"inputs\":" + MailHost + "}";

    private const string Try = "\"Try\":" + """{"type":"Scope","actions":{"Get":{"type":"Http","inputs":{"method":"GET"}}}}""";

    private static string Catch(string inner = "\"Set\":" + """{"type":"SetVariable"}""", string statuses = """["Failed","TimedOut"]""") =>
        "\"Catch\":" + """{"type":"Scope","runAfter":{"Try":""" + statuses + """},"actions":{""" + inner + "}}";

    private static IReadOnlyList<Finding> Po037(string actions) => Run(new PO037CatchTellsNoOne(), FlowOf(actions));

    [Fact]
    public void PO037_fires_on_a_catch_that_only_sets_a_variable()
    {
        var s = FlowOf("{" + Try + "," + Catch() + "}");

        var f = Assert.Single(Run(new PO037CatchTellsNoOne(), s));

        Assert.Equal("Flow 'Flow' has a failure handler 'Catch' that tells no one, so the run ends as succeeded.", f.Message);
        Assert.Equal(("PO037", Severity.Warning, "Flow", s.Workflows[0].File), (f.RuleId, f.Severity, f.Component, f.File));
    }

    [Fact]
    public void PO037_fires_on_a_handler_on_timed_out_only() =>
        Assert.Single(Po037("{" + Try + "," + Catch(statuses: """["timedOut"]""") + "}"));

    [Fact]
    public void PO037_fires_when_a_terminate_ends_the_run_as_succeeded() =>
        Assert.Single(Po037("{" + Try + "," + Catch("\"Stop\":" + """{"type":"Terminate","inputs":{"runStatus":"Succeeded"}}""") + "}"));

    [Fact]
    public void PO037_fires_when_the_only_mail_is_on_the_success_path() =>
        Assert.Single(Po037("{" + Try + "," + Catch() + ",\"Mail\":" + Mail("""{"Try":["Succeeded"]}""") + "}"));

    [Fact]
    public void PO037_reads_repeated_names_by_position_not_by_name()
    {
        // A mail named Notify inside the catch, a Compose named Notify elsewhere.
        Assert.Empty(Po037("{" + Try + "," + Catch("\"Notify\":" + Mail()) + ""","Later":{"type":"Scope","actions":{"Notify":{"type":"Compose"}}}}"""));

        // The same names swapped: the mail is on the success path, the Compose in the catch.
        var tryWithMail = "\"Try\":" + """{"type":"Scope","actions":{"Notify":""" + Mail() + "}}";
        Assert.Single(Po037("{" + tryWithMail + "," + Catch("\"Notify\":" + """{"type":"Compose"}""") + "}"));
    }

    [Theory]
    [InlineData("""{"Try":["Succeeded"]}""")]
    [InlineData("""{"Catch":["Succeeded"]}""")]
    public void PO037_quiet_when_a_terminate_fails_the_run_anywhere(string runAfter) =>
        Assert.Empty(Po037("{" + Try + "," + Catch() + ""","Stop":{"type":"Terminate","runAfter":""" + runAfter + ""","inputs":{"runStatus":"failed"}}}"""));

    [Fact]
    public void PO037_quiet_when_a_terminate_cancels_the_run() =>
        Assert.Empty(Po037("{" + Try + "," + Catch("\"Stop\":" + """{"type":"Terminate","inputs":{"runStatus":"Cancelled"}}""") + "}"));

    [Fact]
    public void PO037_quiet_when_a_response_is_anywhere() =>
        Assert.Empty(Po037("{" + Try + "," + Catch() + ""","Reply":{"type":"Response","runAfter":{"Try":["Succeeded"]},"inputs":{"statusCode":200}}}"""));

    public static TheoryData<string, string> NestedOutward()
    {
        var data = new TheoryData<string, string>();
        foreach (var type in new[] { "OpenApiConnection", "Http", "Workflow", "ApiConnection" })
        {
            data.Add(type, "\"Inner\":" + """{"type":"Scope","actions":{"Send":{0}}}""");
            data.Add(type, "\"Inner\":" + """{"type":"Scope","actions":{"If":{"type":"If","actions":{},"else":{"actions":{"Send":{0}}}}}}""");
            data.Add(type, "\"Pick\":" + """{"type":"Switch","cases":{"One":{"actions":{"Send":{0}}}}}""");
            data.Add(type, "\"Pick\":" + """{"type":"Switch","cases":{},"default":{"actions":{"Send":{0}}}}""");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(NestedOutward))]
    public void PO037_quiet_when_the_catch_sends_from_deep_inside(string type, string inner) =>
        Assert.Empty(Po037("{" + Try + "," + Catch(inner.Replace("{0}", Outward(type), StringComparison.Ordinal)) + "}"));

    [Fact]
    public void PO037_quiet_when_a_mail_runs_after_the_catch_on_succeeded() =>
        Assert.Empty(Po037("{" + Try + "," + Catch() + ",\"Mail\":" + Mail("""{"Catch":["Succeeded"]}""") + "}"));

    [Fact]
    public void PO037_quiet_without_a_handler() =>
        Assert.Empty(Po037("{" + Try + ",\"Mail\":" + Mail("""{"Try":["Succeeded","Skipped"]}""") + "}"));

    [Fact]
    public void PO037_follows_a_chain_of_50000_actions_listed_in_reverse_in_linear_time()
    {
        var actions = new List<string> { "\"A50000\":" + Mail("""{"A49999":["Succeeded"]}""") };
        for (var i = 49_999; i >= 1; i--)
        {
            var previous = i == 1 ? "Handler" : $"A{i - 1}";
            actions.Add($"\"A{i}\":" + "{\"type\":\"Compose\",\"runAfter\":{\"" + previous + "\":[\"Succeeded\"]}}");
        }

        actions.Add("\"Handler\":" + """{"type":"SetVariable","runAfter":{"Try":["Failed"]}}""");
        actions.Add(Try);
        var s = FlowOf("{" + string.Join(",", actions) + "}");

        var sw = Stopwatch.StartNew();
        var findings = Run(new PO037CatchTellsNoOne(), s);

        Assert.Empty(findings);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"took {sw.Elapsed}");
    }

    private static string StaticResult(string options) => """{"type":"Http","inputs":{"method":"GET"},"runtimeConfiguration":{"staticResult":{"name":"Get0","staticResultOptions":""" + $"\"{options}\"}}}}}}";

    [Fact]
    public void PO038_fires_on_an_enabled_static_result_also_inside_a_switch_case()
    {
        var s = FlowOf("{\"Get\":" + StaticResult("Enabled") + ""","Pick":{"type":"Switch","cases":{"One":{"actions":{"Deep":""" + StaticResult("Enabled") + "}}}}}");

        var found = Run(new PO038StaticResultOn(), s);

        Assert.Equal(["Get", "Deep"], found.Select(f => f.Component));
        Assert.Equal("Flow 'Flow' action 'Get' returns its static result instead of running.", found[0].Message);
        Assert.All(found, f => Assert.Equal(("PO038", Severity.Warning, s.Workflows[0].File), (f.RuleId, f.Severity, f.File)));
    }

    [Fact]
    public void PO038_quiet_on_a_disabled_or_absent_static_result() =>
        Assert.Empty(Run(new PO038StaticResultOn(), FlowOf("{\"Off\":" + StaticResult("Disabled") + ""","None":{"type":"Http"}}""")));

    private static IReadOnlyList<Finding> Po039(string trigger) => Run(new PO039RareTrigger(), FlowOf("{}", "{\"Every\":" + trigger + "}"));

    private static string Recurrence(string frequency, string interval) => """{"type":"Recurrence","recurrence":{"frequency":""" + $"\"{frequency}\",\"interval\":{interval}}}}}";

    [Theory]
    [InlineData("Day", "90", "90")]
    [InlineData("Week", "13", "13")]
    [InlineData("Month", "3", "3")]
    [InlineData("Hour", "2160", "2160")]
    [InlineData("Minute", "129600", "129600")]
    [InlineData("Second", "7776000", "7776000")]
    [InlineData("day", "\"90\"", "90")]
    public void PO039_fires_on_a_schedule_of_90_days_or_rarer(string frequency, string interval, string shown)
    {
        var f = Assert.Single(Po039(Recurrence(frequency, interval)));

        Assert.Equal($"Flow 'Flow' runs on a Recurrence with frequency {frequency} and interval {shown}; a flow not triggered within 90 days may be turned off.", f.Message);
        Assert.Equal(("PO039", Severity.Warning, "Every"), (f.RuleId, f.Severity, f.Component));
    }

    [Theory]
    [InlineData("Day", "89")]
    [InlineData("Week", "12")]
    [InlineData("Month", "2")]
    [InlineData("Day", "\"@parameters('Days')\"")]
    [InlineData("Day", "0")]
    [InlineData("Day", "-100")]
    [InlineData("Year", "1")]
    public void PO039_quiet_below_the_threshold_or_without_a_literal_interval(string frequency, string interval) =>
        Assert.Empty(Po039(Recurrence(frequency, interval)));

    [Fact]
    public void PO039_quiet_on_a_missing_interval_and_on_a_recurrence_under_another_trigger_type()
    {
        Assert.Empty(Po039("""{"type":"Recurrence","recurrence":{"frequency":"Day"}}"""));
        Assert.Empty(Po039("""{"type":"OpenApiConnection","recurrence":{"frequency":"Day","interval":120}}"""));
    }

    private static string Approval(string? timeout, string operation = "StartAndWaitForAnApproval", string api = "/providers/Microsoft.PowerApps/apis/shared_approvals", string inputs = "") =>
        """{"type":"OpenApiConnectionWebhook","inputs":{"host":{"apiId":""" + $"\"{api}\",\"operationId\":\"{operation}\"}}{inputs}}}"
        + (timeout is null ? "" : $",\"limit\":{{\"timeout\":\"{timeout}\"}}") + "}";

    private static (IReadOnlyList<Finding> Findings, IReadOnlyList<string> Notes) Po040(string action)
    {
        var s = FlowOf("{\"Approve\":" + action + "}");
        var rule = new PO040LongApproval();
        return (Run(rule, s), rule.NotChecked(s).ToList());
    }

    [Theory]
    [InlineData("P30D", 30)]
    [InlineData("PT720H", 30)]
    [InlineData("P60D", 60)]
    [InlineData("P1M", 30)]
    [InlineData("P1Y", 365)]
    public void PO040_fires_on_a_timeout_of_30_days_or_more(string timeout, int days)
    {
        var (findings, notes) = Po040(Approval(timeout));

        var f = Assert.Single(findings);
        Assert.Equal($"Flow 'Flow' approval 'Approve' has a timeout of {days} days; a run lasts at most 30 days.", f.Message);
        Assert.Equal(("PO040", Severity.Warning, "Approve"), (f.RuleId, f.Severity, f.Component));
        Assert.Empty(notes);
    }

    [Theory]
    [InlineData("WaitForAnApproval", "/providers/Microsoft.PowerApps/apis/shared_approvals")]
    [InlineData("startandwaitforanapproval", "/providers/Microsoft.PowerApps/apis/Shared_Approvals")]
    [InlineData("StartAndWaitForATextSuggestionApproval", "/providers/Microsoft.PowerApps/apis/shared_approvals")]
    [InlineData("approvalSubscribe", "/providers/Microsoft.PowerApps/apis/shared_approvals")]
    [InlineData("approvalSubscribeV2", "/providers/Microsoft.PowerApps/apis/shared_approvals")]
    public void PO040_fires_on_every_wait_operation_ignoring_case(string operation, string api) =>
        Assert.Single(Po040(Approval("P30D", operation, api)).Findings);

    [Theory]
    [InlineData("P29D")]
    [InlineData("-P60D")]
    [InlineData(null)]
    public void PO040_quiet_on_a_shorter_negative_or_missing_timeout(string? timeout)
    {
        var (findings, notes) = Po040(Approval(timeout));

        Assert.Empty(findings);
        Assert.Empty(notes);
    }

    [Fact]
    public void PO040_quiet_on_an_until_and_on_an_approval_that_does_not_wait()
    {
        Assert.Equal((0, 0), Count(Po040("""{"type":"Until","limit":{"timeout":"P60D"}}""")));
        Assert.Equal((0, 0), Count(Po040("""{"type":"Until","limit":{"timeout":"PT1H"}}""")));
        Assert.Equal((0, 0), Count(Po040(Approval("P60D", "CreateAnApproval"))));

        static (int, int) Count((IReadOnlyList<Finding> F, IReadOnlyList<string> N) r) => (r.F.Count, r.N.Count);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("@parameters('x')")]
    [InlineData("P5W")]
    [InlineData("p30d")]
    [InlineData("P99999999D")]
    public void PO040_notes_a_timeout_it_cannot_read_as_a_duration(string timeout)
    {
        var (findings, notes) = Po040(Approval(timeout));

        Assert.Empty(findings);
        Assert.Equal(["PO040 not checked for flow 'Flow': approval 'Approve' has a timeout this rule can't read as a duration."], notes);
    }

    [Fact]
    public void PO037_to_PO040_name_flows_actions_and_triggers_never_values()
    {
        const string Address = "pat@contoso.com";
        const string Url = "https://api.contoso.com/orders";
        const string Id = "12345678-aaaa-bbbb-cccc-1234567890ab";
        var values = $",\"parameters\":{{\"assignedTo\":\"{Address}\",\"uri\":\"{Url}\",\"item\":\"{Id}\"}}";
        var actions = "{"
            + "\"Try\":" + """{"type":"Scope","actions":{"Get":{"type":"Http","inputs":{"uri":""" + $"\"{Url}\",\"headers\":{{\"x-id\":\"{Id}\",\"from\":\"{Address}\"}}}},"
            + "\"runtimeConfiguration\":" + """{"staticResult":{"name":"Get0","staticResultOptions":"Enabled"}}}}}"""
            + ""","Catch":{"type":"Scope","runAfter":{"Try":["Failed"]},"actions":{"Set":{"type":"SetVariable","inputs":{"name":"v","value":""" + $"\"{Address} {Url} {Id}\"}}}}}}}}"
            + ",\"Approve\":" + Approval("P60D", inputs: values)
            + ",\"Approve_later\":" + Approval($"@{{concat('{Url}','{Id}','{Address}')}}", inputs: values)
            + "}";
        var s = FlowOf(actions, "{\"Every\":" + Recurrence("Month", "3") + "}");
        var rules = new IRule[] { new PO037CatchTellsNoOne(), new PO038StaticResultOn(), new PO039RareTrigger(), new PO040LongApproval() };

        var texts = rules.SelectMany(r => r.Evaluate(s)).Select(f => f.Message + " " + f.Component)
            .Concat(rules.OfType<INotChecked>().SelectMany(r => r.NotChecked(s))).ToList();

        Assert.Equal(5, texts.Count);
        Assert.All(texts, t =>
        {
            Assert.DoesNotContain("contoso", t, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("http", t, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@", t, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"[0-9a-f]{8}-[0-9a-f]{4}-", t);
        });
    }

    // M100: PO041 reads an environment variable's type and name; PO042 reads FlowAction's secure-data, authentication and unbound-action fields.

    private static EnvironmentVariable Text(string name) => B.Env(name);

    private static IReadOnlyList<Finding> Po041(params EnvironmentVariable[] variables) =>
        Run(new PO041TextSecretVariable(), B.Empty() with { EnvironmentVariables = variables });

    [Theory]
    [InlineData("demo_ApiToken")]
    [InlineData("demo_DbPassword")]
    [InlineData("demo_AdminPwd")]
    [InlineData("demo_PartnerApiKey")]
    [InlineData("demo_Partner_Api_Key")]
    [InlineData("demo_SqlConnectionString")]
    [InlineData("demo_ClientSecret")]
    [InlineData("DEMO_APITOKEN")]
    [InlineData("demo_TokenEndpoint")]
    [InlineData("demo_SecretVersion")]
    public void PO041_fires_on_a_text_variable_whose_name_has_a_secret_word(string name)
    {
        var v = Text(name);

        var f = Assert.Single(Po041(v));

        Assert.Equal($"Environment variable '{name}' is of type Text, but its name suggests it holds a secret.", f.Message);
        Assert.Equal(("PO041", Severity.Warning, v.File, name), (f.RuleId, f.Severity, f.File, f.Component));
    }

    [Theory]
    [InlineData("demo_SecretName")]
    [InlineData("demo_SortKey")]
    [InlineData("demo_TokenId")]
    [InlineData("demo_TokenUrl")]
    [InlineData("demo_TokenUri")]
    [InlineData("demo_SecretRef")]
    [InlineData("demo_SecretReference")]
    [InlineData("demo_SecretPath")]
    [InlineData("demo_TokenDate")]
    [InlineData("demo_TokenExpiry")]
    [InlineData("demo_TokenExpiration")]
    [InlineData("demo_PasswordName")]
    [InlineData("demo_PasswordValid")]
    public void PO041_quiet_on_a_text_variable_whose_name_ends_in_a_name_like_suffix_or_has_no_secret_word(string name) =>
        Assert.Empty(Po041(Text(name)));

    [Theory]
    [InlineData("100000005")]
    [InlineData("100000001")]
    [InlineData(null)]
    public void PO041_quiet_on_a_variable_that_is_not_of_type_text(string? type) =>
        Assert.Empty(Po041(Text("demo_ApiToken") with { Type = type }));

    [Fact]
    public void PO041_is_a_registered_warning_with_a_fix_sentence()
    {
        var rule = new PO041TextSecretVariable();

        Assert.Equal(("PO041", Severity.Warning, "Text environment variable looks like it holds a secret"), (rule.Id, rule.Severity, rule.Title));
        Assert.Equal("Make it an environment variable of type Secret backed by Azure Key Vault, and remove its value from the solution and the settings files.", rule.Fix);
    }

    private const string DataverseApi = "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps";

    private const string KeyVaultApi = "/providers/Microsoft.PowerApps/apis/shared_keyvault";

    private static string Secure(string? properties) =>
        properties is null ? "" : ",\"runtimeConfiguration\":{\"secureData\":{\"properties\":" + properties + "}}";

    private static string Connector(string api, string operation, string? actionName = null, string? properties = null) =>
        "{\"type\":\"OpenApiConnection\",\"inputs\":{\"host\":{\"apiId\":\"" + api + "\",\"operationId\":\"" + operation + "\"}"
        + (actionName is null ? "" : ",\"parameters\":{\"actionName\":" + actionName + "}") + "}" + Secure(properties) + "}";

    private static string SecretRead(string operation = "PerformUnboundAction", string api = DataverseApi, string actionName = "\"RetrieveEnvironmentVariableSecretValue\"", string? properties = null) =>
        Connector(api, operation, actionName, properties);

    private static string Send(string? authentication, string? properties = null, string type = "Http") =>
        "{\"type\":\"" + type + "\",\"inputs\":{\"method\":\"POST\"" + (authentication is null ? "" : ",\"authentication\":" + authentication) + "}" + Secure(properties) + "}";

    private const string Basic = """{"type":"Basic","username":"demo","password":"demo-only"}""";

    private static IReadOnlyList<Finding> Po042(string actions) => Run(new PO042SecretWithoutSecureData(), FlowOf(actions));

    public static TheoryData<string> UnsecuredSecretReads() => new()
    {
        { "{\"Get\":" + SecretRead() + "}" },
        { "{\"Get\":" + SecretRead(properties: """["inputs"]""") + "}" },
        { "{\"Get\":" + SecretRead("PerformUnboundActionWithOrganization") + "}" },
        { "{\"Get\":" + SecretRead("performunboundaction", "/providers/Microsoft.PowerApps/apis/SHARED_COMMONDATASERVICEFORAPPS", "\"retrieveenvironmentvariablesecretvalue\"") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "GetSecret") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "GetSecretVersion") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "getsecret") + "}" },
        { "{\"Box\":{\"type\":\"Scope\",\"actions\":{\"If\":{\"type\":\"If\",\"actions\":{},\"else\":{\"actions\":{\"Get\":" + SecretRead() + "}}}}}}" },
    };

    [Theory]
    [MemberData(nameof(UnsecuredSecretReads))]
    public void PO042_fires_on_a_secret_read_without_secure_outputs(string actions)
    {
        var s = FlowOf(actions);

        var f = Assert.Single(Run(new PO042SecretWithoutSecureData(), s));

        Assert.Equal("Flow 'Flow' action 'Get' reads a secret without secure outputs, so the secret shows in the run history.", f.Message);
        Assert.Equal(("PO042", Severity.Warning, s.Workflows[0].File, "Get"), (f.RuleId, f.Severity, f.File, f.Component));
    }

    public static TheoryData<string> UnsecuredSends() => new()
    {
        { "{\"Send\":" + Send(Basic) + "}" },
        { "{\"Send\":" + Send(Basic, """["outputs"]""") + "}" },
        { "{\"Send\":" + Send("\"@parameters('p')\"") + "}" },
        { "{\"Send\":" + Send(Basic, type: "http") + "}" },
        { "{\"Send\":{\"type\":\"HttpWebhook\",\"inputs\":{\"subscribe\":{\"method\":\"POST\",\"authentication\":" + Basic + "}}}}" },
        { "{\"Send\":{\"type\":\"HttpWebhook\",\"inputs\":{\"subscribe\":{\"method\":\"POST\"},\"unsubscribe\":{\"method\":\"POST\",\"authentication\":" + Basic + "}}}}" },
        // The accepted false-positive class: the credentials come from a secret read whose outputs are secured, and the sending action's inputs are not.
        { "{\"Get\":" + SecretRead(properties: """["outputs"]""") + ",\"Send\":" + Send("{\"type\":\"Raw\",\"value\":\"@{outputs('Get')?['body/EnvironmentVariableSecretValue']}\"}") + "}" },
    };

    [Theory]
    [MemberData(nameof(UnsecuredSends))]
    public void PO042_fires_on_a_sending_action_with_an_authentication_block_and_no_secure_inputs(string actions)
    {
        var s = FlowOf(actions);

        var f = Assert.Single(Run(new PO042SecretWithoutSecureData(), s));

        Assert.Equal("Flow 'Flow' action 'Send' has an authentication block and its secure inputs are off.", f.Message);
        Assert.Equal(("PO042", Severity.Warning, s.Workflows[0].File, "Send"), (f.RuleId, f.Severity, f.File, f.Component));
    }

    public static TheoryData<string> QuietActions() => new()
    {
        { "{\"Get\":" + SecretRead(properties: """["outputs"]""") + "}" },
        { "{\"Get\":" + SecretRead(properties: """["Outputs"]""") + "}" },
        { "{\"Get\":" + SecretRead(properties: """["inputs","outputs"]""") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "GetSecret", properties: """["outputs"]""") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "GetSecretVersion", properties: """["outputs"]""") + "}" },
        { "{\"Send\":" + Send(Basic, """["inputs"]""") + "}" },
        { "{\"Send\":{\"type\":\"HttpWebhook\",\"inputs\":{\"subscribe\":{\"authentication\":" + Basic + "}}" + Secure("""["inputs"]""") + "}}" },
        { "{\"Send\":" + Send(null) + "}" },
        { "{\"Send\":" + Send(Basic, type: "Compose") + "}" },
        { "{\"Send\":" + Connector(DataverseApi, "ListRecords") .Replace("\"inputs\":{", "\"inputs\":{\"authentication\":\"@parameters('$authentication')\",", StringComparison.Ordinal) + "}" },
        // The Raw object form that 49 alm-makers connector actions hold: the type gate keeps it quiet.
        { "{\"Send\":" + Connector(DataverseApi, "ListRecords").Replace("\"inputs\":{", "\"inputs\":{\"authentication\":{\"type\":\"Raw\",\"value\":\"@json(parameters('$authentication'))\"},", StringComparison.Ordinal) + "}" },
        { "{\"Get\":" + SecretRead(actionName: "\"SomethingElse\"") + "}" },
        { "{\"Get\":" + SecretRead(actionName: "\"@parameters('n')\"") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "ListSecrets") + "}" },
        { "{\"Get\":" + Connector(KeyVaultApi, "GetSecretMetadata") + "}" },
        { "{\"Get\":" + Connector("/providers/Microsoft.PowerApps/apis/shared_other", "GetSecret") + "}" },
    };

    [Theory]
    [MemberData(nameof(QuietActions))]
    public void PO042_quiet_on_secured_actions_and_on_everything_it_does_not_read(string actions) =>
        Assert.Empty(Po042(actions));

    [Fact]
    public void PO042_gives_one_finding_per_action_and_none_for_a_trigger()
    {
        var s = B.Empty() with
        {
            Workflows = [B.Flow() with { ClientData = "{\"properties\":{\"definition\":{\"triggers\":{\"On\":" + Send(Basic) + "},\"actions\":{\"Get\":" + SecretRead() + ",\"Send\":" + Send(Basic) + "}}}}" }],
        };

        Assert.Equal(["Get", "Send"], Run(new PO042SecretWithoutSecureData(), s).Select(f => f.Component));
    }

    [Fact]
    public void PO042_is_a_registered_warning_with_a_fix_sentence()
    {
        var rule = new PO042SecretWithoutSecureData();

        Assert.Equal(("PO042", Severity.Warning, "Flow reads or sends a secret without secure inputs or outputs"), (rule.Id, rule.Severity, rule.Title));
        Assert.Equal("Turn on secure outputs on every action that reads a secret, and secure inputs on every action that sends one.", rule.Fix);
    }

    [Fact]
    public void PO041_and_PO042_name_variables_flows_and_actions_never_values()
    {
        const string Url = "https://api.contoso.com/orders";
        const string Id = "12345678-aaaa-bbbb-cccc-1234567890ab";
        const string Address = "pat@contoso.com";
        var variable = B.Env("demo_ApiToken", def: "tok-" + Id, cur: "cur-" + Address);
        var send = "{\"type\":\"Http\",\"inputs\":{\"method\":\"POST\",\"uri\":\"" + Url + "\",\"headers\":{\"x-id\":\"" + Id + "\",\"from\":\"" + Address + "\"},"
            + "\"authentication\":{\"type\":\"Basic\",\"username\":\"" + Address + "\",\"password\":\"pw-" + Id + "\"}}}";
        var s = FlowOf("{\"Get\":" + SecretRead() + ",\"Call_partner\":" + send + "}") with { EnvironmentVariables = [variable] };
        var rules = new IRule[] { new PO041TextSecretVariable(), new PO042SecretWithoutSecureData() };

        var texts = rules.SelectMany(r => r.Evaluate(s)).Select(f => f.Message + " " + f.Component).ToList();

        Assert.Equal(3, texts.Count);
        Assert.All(texts, t =>
        {
            Assert.DoesNotContain("contoso", t, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("http", t, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@", t, StringComparison.Ordinal);
            Assert.DoesNotContain("pw-", t, StringComparison.Ordinal);
            Assert.DoesNotContain("tok-", t, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"[0-9a-f]{8}-[0-9a-f]{4}-", t);
        });
    }
}