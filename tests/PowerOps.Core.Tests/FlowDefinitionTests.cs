using PowerOps.Core.Flows;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

public class FlowDefinitionTests
{
    private static string Flow(string connectionReferences, string definition) =>
        "{\"properties\":{\"connectionReferences\":" + connectionReferences + ",\"definition\":" + definition + "}}";

    private const string DataverseApi = "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps";

    private static string Action(string parameters, string api = DataverseApi) =>
        "{\"type\":\"OpenApiConnection\",\"inputs\":{\"host\":{\"apiId\":\"" + api + "\",\"operationId\":\"ListRecords\"},\"parameters\":" + parameters + "}}";

    private static IEnumerable<string> Dataverse(FlowDefinition flow) =>
        flow.DataverseActions.Select(a => $"{a.Name}|{a.EntitySet}|{a.Expand}|{string.Join(",", a.Binds)}");

    [Fact]
    public void Dataverse_actions_are_read_at_any_depth_in_walk_order_with_their_literal_entity_set_expand_and_binds()
    {
        var flow = FlowDefinition.Parse(Flow("{}", "{\"actions\":{"
            + "\"List\":" + Action("""{"entityName":"demo_expenses","$expand":"demo_CostCentre"}""")
            + ",\"Scope\":{\"type\":\"Scope\",\"actions\":{\"Add\":"
            + Action("""{"entityName":"demo_expenses","item/demo_CostCentre@odata.bind":"demo_costcentres(1)","item/demo_name":"n","item/demo_Owner@odata.bind":"systemusers(2)"}""") + "}}"
            + ",\"If\":{\"type\":\"If\",\"actions\":{},\"else\":{\"actions\":{\"Expr\":"
            + Action("""{"entityName":"@parameters('Table')","$expand":"@{variables('Expand')}"}""") + "}}}"
            + ",\"Switch\":{\"type\":\"Switch\",\"cases\":{\"One\":{\"actions\":{\"Get\":"
            + Action("""{"entityName":"demo_costcentres"}""", "/providers/Microsoft.PowerApps/apis/Shared_CommonDataServiceForApps") + "}}}}"
            + ",\"Mail\":" + Action("""{"entityName":"demo_expenses","$expand":"demo_CostCentre"}""", "/providers/Microsoft.PowerApps/apis/shared_office365")
            + ",\"Legacy\":" + Action("""{"entityName":"demo_expenses","$expand":"demo_CostCentre"}""", "/providers/Microsoft.PowerApps/apis/shared_commondataservice")
            + "}}"));

        Assert.Equal(
            [
                "List|demo_expenses|demo_CostCentre|",
                "Add|demo_expenses||demo_CostCentre,demo_Owner",
                "Expr|||",
                "Get|demo_costcentres||",
            ],
            Dataverse(flow));
    }

    [Fact]
    public void Odd_dataverse_action_shapes_give_null_values_or_no_binds_and_never_throw()
    {
        var flow = FlowDefinition.Parse(Flow("{}", "{\"actions\":{"
            + "\"Array\":" + Action("""["item/demo_CostCentre@odata.bind"]""")
            + ",\"Numbers\":" + Action("""{"entityName":5,"$expand":true,"item/demo_CostCentre@odata.bind":1}""")
            + ""","NumberApi":{"type":"OpenApiConnection","inputs":{"host":{"apiId":5},"parameters":{"entityName":"demo_expenses"}}}"""
            + ""","TextInputs":{"type":"OpenApiConnection","inputs":"text"}"""
            + ""","TextHost":{"type":"OpenApiConnection","inputs":{"host":"x","parameters":{"entityName":"demo_expenses"}}}"""
            + "}}"));

        Assert.Equal(["Array|||", "Numbers|||demo_CostCentre"], Dataverse(flow));
    }

    [Fact]
    public void Deeply_nested_definition_parses_up_to_256_levels()
    {
        var nested = string.Concat(Enumerable.Repeat("{\"a\":", 100)) + "1" + new string('}', 100);

        var flow = FlowDefinition.Parse(Flow("{}", nested));

        Assert.Empty(flow.StringLiterals);
        Assert.Throws<InvalidDataException>(() => FlowDefinition.Parse(
            Flow("{}", string.Concat(Enumerable.Repeat("{\"a\":", 300)) + "1" + new string('}', 300))));
    }

    [Fact]
    public void Embedded_connection_has_null_logical_name()
    {
        var flow = FlowDefinition.Parse(Flow(
            """{"shared_a":{"api":{"name":"shared_a"},"connection":{"name":"@parameters('$connections')['x']"}},"shared_b_1":{"api":{"name":"shared_b"},"connection":{"connectionReferenceLogicalName":"pfx_B"}}}""",
            """{"actions":{}}"""));

        Assert.Equal(
            [new FlowConnectionReference("shared_a", null, "shared_a"), new FlowConnectionReference("shared_b_1", "pfx_B", "shared_b")],
            flow.ConnectionReferences);
    }

    [Fact]
    public void Parameters_use_metadata_schemaName_or_the_label_suffix_and_skip_system_ones()
    {
        var flow = FlowDefinition.Parse(Flow(
            "{}",
            """
            {"parameters":{
              "$connections":{"defaultValue":{},"type":"Object"},
              "$authentication":{"defaultValue":{},"type":"SecureObject"},
              "Anything":{"type":"String","metadata":{"schemaName":"pfx_FromMetadata"}},
              "Mail (pfx_FromLabel)":{"type":"String"},
              "Both (pfx_Label)":{"type":"String","metadata":{"schemaName":"pfx_Meta"}},
              "Plain":{"type":"String"}
            }}
            """));

        Assert.Equal(["pfx_FromMetadata", "pfx_FromLabel", "pfx_Meta"], flow.EnvironmentVariables);
    }

    [Fact]
    public void Child_flow_ids_are_found_in_nested_actions_lower_cased_and_distinct()
    {
        var flow = FlowDefinition.Parse(Flow(
            "{}",
            """
            {"actions":{
              "Scope":{"type":"Scope","actions":{
                "If":{"type":"If","actions":{"Call":{"inputs":{"host":{"workflowReferenceName":"AAAAAAAA-0000-0000-0000-000000000001"}}}},
                      "else":{"actions":{"Call2":{"inputs":{"host":{"workflowReferenceName":"aaaaaaaa-0000-0000-0000-000000000001"}}}}}},
                "Call3":{"inputs":{"host":{"workflowReferenceName":"bbbbbbbb-0000-0000-0000-000000000002"}}}
              }}
            }}
            """));

        Assert.Equal(["aaaaaaaa-0000-0000-0000-000000000001", "bbbbbbbb-0000-0000-0000-000000000002"], flow.ChildFlowIds);
    }

    [Fact]
    public void String_literals_skip_metadata_schema_and_operationMetadataId()
    {
        var flow = FlowDefinition.Parse(Flow(
            "{}",
            """
            {"$schema":"https://schema.management.azure.com/x.json",
            "actions":{
              "Get":{"inputs":{"uri":"https://org1.crm.dynamics.com/api","items":["one",{"k":"two"}],"n":5},
                     "metadata":{"operationMetadataId":"id","note":"skip me"},
                     "operationMetadataId":"skip too"}
            }}
            """));

        Assert.Equal(
            [
                new StringLiteral(false, "https://org1.crm.dynamics.com/api"),
                new StringLiteral(false, "one"),
                new StringLiteral(false, "two"),
            ],
            flow.StringLiterals);
    }

    [Fact]
    public void String_literals_are_flagged_only_under_the_top_level_parameters_key()
    {
        var flow = FlowDefinition.Parse(Flow(
            "{}",
            """{"parameters":{"U":{"defaultValue":"in","list":["in2"]}},"parametersX":"out1","actions":{"A":{"inputs":{"parameters":{"p":"out2"}}}}}"""));

        Assert.Equal(
            [new StringLiteral(true, "in"), new StringLiteral(true, "in2"), new StringLiteral(false, "out1"), new StringLiteral(false, "out2")],
            flow.StringLiterals);
    }

    [Fact]
    public void Environment_variable_defaults_are_read_for_bound_string_defaults_only()
    {
        var flow = FlowDefinition.Parse(Flow("{}", "{\"parameters\":{"
            + "\"$connections\":{\"defaultValue\":\"x\"}"
            + ",\"$authentication\":{\"defaultValue\":\"x\"}"
            + ",\"Mail (demo_Mail)\":{\"type\":\"String\",\"defaultValue\":\" a@b.example \"}"
            + ",\"Other\":{\"type\":\"String\",\"defaultValue\":\"y\",\"metadata\":{\"schemaName\":\"demo_Meta\"}}"
            + ",\"Unbound\":{\"type\":\"String\",\"defaultValue\":\"https://example.com\"}"
            + ",\"Number (demo_Num)\":{\"defaultValue\":30}"
            + ",\"Object (demo_Obj)\":{\"defaultValue\":{\"a\":1}}"
            + ",\"NoDefault (demo_None)\":{\"type\":\"String\"}"
            + "},\"actions\":{}}"));

        Assert.Equal([new EnvironmentVariableDefault("demo_Mail", " a@b.example "), new EnvironmentVariableDefault("demo_Meta", "y")], flow.EnvironmentVariableDefaults);
        Assert.Equal(["demo_Mail", "demo_Meta", "demo_Num", "demo_Obj", "demo_None"], flow.EnvironmentVariables);
    }

    [Fact]
    public void Missing_sections_give_empty_lists()
    {
        var flow = FlowDefinition.Parse("""{"properties":{}}""");

        Assert.Empty(flow.ConnectionReferences);
        Assert.Empty(flow.EnvironmentVariables);
        Assert.Empty(flow.EnvironmentVariableDefaults);
        Assert.Empty(flow.ChildFlowIds);
        Assert.Empty(flow.StringLiterals);
        Assert.Equal(0, flow.ActionCount);
        Assert.Equal(0, flow.Depth);
        Assert.Empty(flow.NestedLoops);
    }

    [Theory]
    [InlineData("""{"triggers":{"t":{"type":"Request"}}}""", 0)]
    [InlineData("""{"actions":[{"a":1}]}""", 0)]
    [InlineData("""{"actions":null}""", 0)]
    [InlineData("""{"actions":{"a":1,"b":"x","c":null}}""", 3)]
    public void Odd_action_shapes_are_counted_without_throwing(string definition, int actions)
    {
        var flow = FlowDefinition.Parse(Flow("{}", definition));

        Assert.Equal(actions, flow.ActionCount);
        Assert.Equal(actions, flow.Actions.Count);
        Assert.Equal(0, flow.Depth);
        Assert.Empty(flow.NestedLoops);
    }

    private static IEnumerable<string> Walked(FlowDefinition flow) => flow.Actions.Select(a => $"{a.Name}|{a.Depth}|{a.Descendants}");

    [Fact]
    public void Actions_count_equals_ActionCount_on_every_cloud_flow_under_the_fixtures()
    {
        var root = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
        var flows = 0;
        foreach (var path in SolutionDiscovery.Find(root).Where(p => !p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var w in SolutionLoader.Load(path).Workflows.Where(w => w.Category == 5 && w.ClientData is not null))
            {
                var flow = FlowDefinition.Parse(w.ClientData!);
                Assert.True(flow.Actions.Count == flow.ActionCount, $"{path} {w.Name}: {flow.Actions.Count} actions, ActionCount {flow.ActionCount}");
                flows++;
            }
        }

        Assert.True(flows > 10, $"only {flows} flows");
    }

    [Fact]
    public void Actions_are_read_in_scopes_conditions_and_switches_in_walk_order()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"actions":{
              "Scope":{"type":"Scope","actions":{
                "If":{"type":"If","actions":{"Yes":{"type":"Compose"}},"else":{"actions":{"No":{"type":"Compose"}}}},
                "Inner":{"type":"Scope","actions":{"Deep":{"type":"Compose"}}}
              }},
              "Switch":{"type":"Switch","default":{"actions":{"Other":{"type":"Compose"}}},"cases":{"B":{"actions":{"CaseB":{"type":"Compose"}}},"A":{"actions":{"CaseA":{"type":"Compose"}}}}},
              "Last":{"type":"Compose"}
            }}
            """));

        Assert.Equal(["Scope", "If", "Yes", "No", "Inner", "Deep", "Switch", "Other", "CaseB", "CaseA", "Last"], flow.Actions.Select(a => a.Name));
        Assert.Equal(flow.ActionCount, flow.Actions.Count);
    }

    [Fact]
    public void Depth_and_descendants_slice_each_container_in_walk_order()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"actions":{
              "Scope":{"type":"Scope","actions":{"Condition":{"type":"If","actions":{},"else":{"actions":{"Act":{"type":"Compose"}}}}}},
              "Switch":{"type":"Switch","cases":{"One":{"actions":{"C1":{"type":"Compose"}}}},"default":{"actions":{"D1":{"type":"Compose"},"D2":{"type":"Compose"}}}},
              "After":{"type":"Compose"}
            }}
            """));

        Assert.Equal(["Scope|0|2", "Condition|1|1", "Act|2|0", "Switch|0|3", "D1|1|0", "D2|1|0", "C1|1|0", "After|0|0"], Walked(flow));
    }

    [Fact]
    public void RunAfter_keeps_predecessors_in_key_order_and_statuses_as_written()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"actions":{
              "Many":{"runAfter":{"Zed":["Failed","timedOut"],"Alpha":["Succeeded",5,"Skipped"]}},
              "Empty":{"runAfter":{}},
              "NotArray":{"runAfter":{"Zed":"Failed"}},
              "NotObject":{"runAfter":["Zed"]},
              "None":{}
            }}
            """));

        var runAfter = flow.Actions.ToDictionary(a => a.Name, a => a.RunAfter.Select(r => $"{r.Action}:{string.Join(",", r.Statuses)}").ToList());
        Assert.Equal(["Zed:Failed,timedOut", "Alpha:Succeeded,Skipped"], runAfter["Many"]);
        Assert.Empty(runAfter["Empty"]);
        Assert.Equal(["Zed:"], runAfter["NotArray"]);
        Assert.Empty(runAfter["NotObject"]);
        Assert.Empty(runAfter["None"]);
    }

    [Fact]
    public void Action_fields_are_read_when_of_the_expected_kind()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"actions":{
              "Mail":{"type":"OpenApiConnection","inputs":{"host":{"apiId":"/providers/Microsoft.PowerApps/apis/shared_office365","operationId":"SendEmailV2"},"runStatus":"Failed"},"limit":{"timeout":"P2D"}},
              "Stop":{"type":"terminate","inputs":{"runStatus":"Failed"}},
              "On":{"type":"Http","runtimeConfiguration":{"staticResult":{"name":"On0","staticResultOptions":"Enabled"}}},
              "Off":{"type":"Http","runtimeConfiguration":{"staticResult":{"name":"Off0","staticResultOptions":"Disabled"}}},
              "Odd":{"type":5,"inputs":{"host":{"apiId":1}},"runtimeConfiguration":{"staticResult":"Enabled"},"limit":{"timeout":30}},
              "Bare":1
            }}
            """));

        var a = flow.Actions.ToDictionary(x => x.Name);
        Assert.Equal(("OpenApiConnection", "/providers/Microsoft.PowerApps/apis/shared_office365", "SendEmailV2", null, false, "P2D"), (a["Mail"].Type, a["Mail"].ApiId, a["Mail"].OperationId, a["Mail"].RunStatus, a["Mail"].StaticResult, a["Mail"].Timeout));
        Assert.Equal(("terminate", "Failed"), (a["Stop"].Type, a["Stop"].RunStatus));
        Assert.True(a["On"].StaticResult);
        Assert.False(a["Off"].StaticResult);
        Assert.Equal(new FlowAction("Odd", null, null, null, [], null, false, null, 0, 0), a["Odd"] with { RunAfter = [] });
        Assert.Equal(new FlowAction("Bare", null, null, null, [], null, false, null, 0, 0), a["Bare"] with { RunAfter = [] });
        Assert.Empty(a["Bare"].RunAfter);
    }

    private static FlowAction One(string action) =>
        Assert.Single(FlowDefinition.Parse(Flow("{}", "{\"actions\":{\"A\":" + action + "}}")).Actions);

    [Theory]
    [InlineData("""["inputs"]""", true, false)]
    [InlineData("""["outputs"]""", false, true)]
    [InlineData("""["inputs","outputs"]""", true, true)]
    [InlineData("""["Outputs"]""", false, true)]
    [InlineData("""["INPUTS"]""", true, false)]
    [InlineData("""[5,"outputs",null]""", false, true)]
    [InlineData("""[]""", false, false)]
    [InlineData("""[5]""", false, false)]
    [InlineData("""{"inputs":true}""", false, false)]
    [InlineData("\"inputs\"", false, false)]
    public void Secure_data_is_read_from_the_properties_array_ignoring_case(string properties, bool inputs, bool outputs)
    {
        var a = One("{\"type\":\"Http\",\"runtimeConfiguration\":{\"secureData\":{\"properties\":" + properties + "}}}");

        Assert.Equal((inputs, outputs), (a.SecureInputs, a.SecureOutputs));
    }

    [Theory]
    [InlineData("""{"type":"Http"}""")]
    [InlineData("""{"type":"Http","runtimeConfiguration":{"secureData":"inputs"}}""")]
    [InlineData("""{"type":"Http","runtimeConfiguration":{"secureData":["inputs"]}}""")]
    [InlineData("""{"type":"Http","runtimeConfiguration":"secureData"}""")]
    public void Secure_data_is_false_without_an_object_holding_a_properties_array(string action)
    {
        var a = One(action);

        Assert.False(a.SecureInputs || a.SecureOutputs);
    }

    [Theory]
    [InlineData("""{"type":"Http","inputs":{"authentication":{"type":"Basic"}}}""", true)]
    [InlineData("""{"type":"Http","inputs":{"authentication":"@parameters('$authentication')"}}""", true)]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"authentication":{"type":"Raw","value":"@parameters('$authentication')"}}}""", true)]
    [InlineData("""{"type":"Http","inputs":{"authentication":null}}""", false)]
    [InlineData("""{"type":"Http","inputs":{"authentication":5}}""", false)]
    [InlineData("""{"type":"Http","inputs":{"authentication":true}}""", false)]
    [InlineData("""{"type":"Http","inputs":{}}""", false)]
    [InlineData("""{"type":"Http"}""", false)]
    [InlineData("""{"type":"HttpWebhook","inputs":{"subscribe":{"method":"POST","authentication":{"type":"Basic"}}}}""", true)]
    [InlineData("""{"type":"HttpWebhook","inputs":{"unsubscribe":{"method":"POST","authentication":{"type":"Basic"}}}}""", true)]
    [InlineData("""{"type":"HttpWebhook","inputs":{"subscribe":{"method":"POST"},"unsubscribe":{"method":"POST"}}}""", false)]
    public void Authentication_is_true_for_an_object_or_a_string_also_under_subscribe_and_unsubscribe(string action, bool expected) =>
        Assert.Equal(expected, One(action).Authentication);

    [Theory]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"parameters":{"actionName":"RetrieveEnvironmentVariableSecretValue"}}}""", "RetrieveEnvironmentVariableSecretValue")]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"parameters":{"actionName":"@parameters('n')"}}}""", null)]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"parameters":{"actionName":"A@b"}}}""", null)]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"parameters":{"actionName":5}}}""", null)]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"parameters":{}}}""", null)]
    [InlineData("""{"type":"OpenApiConnection","inputs":{"parameters":"actionName"}}""", null)]
    public void Unbound_action_is_the_literal_action_name_only(string action, string? expected) =>
        Assert.Equal(expected, One(action).UnboundAction);

    [Fact]
    public void Secure_data_authentication_and_unbound_action_are_read_inside_a_conditions_else()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"actions":{"If":{"type":"If","actions":{},"else":{"actions":{
              "Deep":{"type":"Http","inputs":{"authentication":{"type":"Basic"},"parameters":{"actionName":"Name"}},"runtimeConfiguration":{"secureData":{"properties":["inputs","outputs"]}}}
            }}}}}
            """));

        var deep = flow.Actions.Single(x => x.Name == "Deep");
        Assert.Equal((true, true, true, "Name"), (deep.SecureInputs, deep.SecureOutputs, deep.Authentication, deep.UnboundAction));
    }

    [Fact]
    public void Trigger_type_frequency_and_interval_are_read()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"triggers":{
              "Number":{"type":"Recurrence","recurrence":{"frequency":"Day","interval":90}},
              "Text":{"type":"recurrence","recurrence":{"frequency":"day","interval":"90"}},
              "Expression":{"type":"Recurrence","recurrence":{"frequency":"Week","interval":"@parameters('n')"}},
              "Huge":{"type":"Recurrence","recurrence":{"frequency":"Second","interval":99999999999}},
              "Missing":{"type":"Recurrence"},
              "Webhook":{"type":"OpenApiConnection","recurrence":{"frequency":"Day","interval":120}}
            },"actions":{}}
            """));

        Assert.Equal(
            [
                "Number|Recurrence|Day|90",
                "Text|recurrence|day|90",
                "Expression|Recurrence|Week|",
                "Huge|Recurrence|Second|",
                "Missing|Recurrence||",
                "Webhook|OpenApiConnection||120",
            ],
            flow.Triggers.Select(t => $"{t.Name}|{t.Type}|{t.Frequency}|{t.Interval}"));
    }

    [Fact]
    public void Odd_holders_are_skipped_and_loop_types_ignore_case()
    {
        var flow = FlowDefinition.Parse(Flow("{}", """
            {"actions":{
              "b":{"type":"foreach","actions":[],"else":"x","cases":[1],"default":null},
              "c":{"type":"until","actions":{"d":{"type":"FOREACH","actions":null}},"cases":{"k":"x"}}
            }}
            """));

        Assert.Equal(3, flow.ActionCount);
        Assert.Equal(1, flow.Depth);
        Assert.Equal([new NestedLoop("c", "d")], flow.NestedLoops);
    }

    [Fact]
    public void Leading_BOM_is_ignored()
    {
        var flow = FlowDefinition.Parse("﻿" + Flow("{}", """{"actions":{"A":{"x":"y"}}}"""));

        Assert.Single(flow.StringLiterals);
    }

    [Fact]
    public void A_duplicate_key_is_refused_so_the_first_copy_cannot_hide_a_hit()
    {
        var e = Assert.Throws<InvalidDataException>(() => FlowDefinition.Parse(
            """{"properties":{"definition":{"actions":{"A":{"inputs":"https://org1.crm.dynamics.com/api"}}},"definition":{"actions":{}}}}"""));

        Assert.StartsWith("Flow definition is not valid JSON", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_lone_surrogate_in_a_literal_is_invalid_json_not_an_InvalidOperationException()
    {
        Assert.Throws<InvalidDataException>(() => FlowDefinition.Parse(Flow("{}", """{"actions":{"A":{"inputs":"\ud800"}}}""")));
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("[]")]
    [InlineData("")]
    public void Invalid_json_throws_InvalidDataException(string json)
    {
        Assert.Throws<InvalidDataException>(() => FlowDefinition.Parse(json));
    }

    private static FlowDefinition ParseFixture(string folder, string startsWith)
    {
        var dir = Path.Combine(Golden.RepoRoot, "tests", "fixtures", folder, "Workflows");
        var file = Directory.GetFiles(dir, startsWith + "*.json").Single();
        return FlowDefinition.Parse(File.ReadAllText(file));
    }

    [Fact]
    public void Coe_child_flow_fixture()
    {
        var flow = ParseFixture("coe-auditlogs", "DeprecatedChildAdminSyncLogs-");

        Assert.Equal(
            [
                new FlowConnectionReference("shared_admin-5foffice-20365-20management-20api-5f7fc9343cf45734fe", "admin_sharedadmin5foffice2036520management20api5ff18d20b7ec3dbffe_a3e3b", "shared_admin-5foffice-20365-20management-20api-5ff18d20b7ec3dbffe"),
                new FlowConnectionReference("shared_commondataserviceforapps", "admin_sharedcommondataserviceforapps_98924", "shared_commondataserviceforapps"),
                new FlowConnectionReference("shared_commondataserviceforapps_1", "new_CoEAuditLogsDataverse", "shared_commondataserviceforapps"),
            ],
            flow.ConnectionReferences);
        Assert.Equal(["5625768c-bd3d-ec11-8c63-00224829720b"], flow.ChildFlowIds);
        Assert.Equal(
            ["admin_AdminMail", "admin_TimeIntervalUnit", "admin_TimeIntervalInterval", "admin_AdmineMailPreferredLanguage", "admin_PowerAutomateEnvironmentVariable"],
            flow.EnvironmentVariables);
        Assert.Equal(36, flow.ActionCount);
        Assert.Equal(5, flow.Depth);
        Assert.Equal([new NestedLoop("Apply_to_each_cluster", "Apply_to_each_Audit_Log")], flow.NestedLoops);
    }

    [Fact]
    public void Coe_parent_flow_fixture_calls_both_children()
    {
        var flow = ParseFixture("coe-auditlogs", "DeprecatedAdminSyncAuditLogs-");

        Assert.Equal(["751183fc-db68-ea11-a811-000d3a31cf04", "5625768c-bd3d-ec11-8c63-00224829720b"], flow.ChildFlowIds);
    }
}