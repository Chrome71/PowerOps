using PowerOps.Core.Flows;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

public class FlowDefinitionTests
{
    private static string Flow(string connectionReferences, string definition) =>
        "{\"properties\":{\"connectionReferences\":" + connectionReferences + ",\"definition\":" + definition + "}}";

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
    public void Missing_sections_give_empty_lists()
    {
        var flow = FlowDefinition.Parse("""{"properties":{}}""");

        Assert.Empty(flow.ConnectionReferences);
        Assert.Empty(flow.EnvironmentVariables);
        Assert.Empty(flow.ChildFlowIds);
        Assert.Empty(flow.StringLiterals);
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
    }

    [Fact]
    public void Coe_parent_flow_fixture_calls_both_children()
    {
        var flow = ParseFixture("coe-auditlogs", "DeprecatedAdminSyncAuditLogs-");

        Assert.Equal(["751183fc-db68-ea11-a811-000d3a31cf04", "5625768c-bd3d-ec11-8c63-00224829720b"], flow.ChildFlowIds);
    }

    [Theory]
    [InlineData("coe-auditlogs")]
    [InlineData("alm-sample")]
    [InlineData("alm-makers")]
    public void Every_fixture_workflow_parses(string folder)
    {
        var files = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "tests", "fixtures", folder, "Workflows"), "*.json");

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var flow = FlowDefinition.Parse(File.ReadAllText(file));
            Assert.NotEmpty(flow.StringLiterals);
        }
    }
}