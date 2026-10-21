using PowerOps.Core.Model;
using PowerOps.Core.Settings;
using PowerOps.Core.Tests.Rules;

namespace PowerOps.Core.Tests.Settings;

public class DeploymentSettingsTests
{
    private static Solution Sln(EnvironmentVariable[]? env = null, ConnectionReference[]? refs = null) =>
        B.Empty() with { EnvironmentVariables = env ?? [], ConnectionReferences = refs ?? [] };

    private static string[] Run(Solution s, string json) =>
        DeploymentSettings.Validate(s, json).Select(p => $"{p.Kind} {p.ComponentType} {p.Name}").ToArray();

    [Fact]
    public void Generate_sorts_by_name_and_leaves_values_empty()
    {
        var s = Sln([B.Env("b_Two"), B.Env("a_One")], [B.Ref("z_Ref", "/providers/z"), new ConnectionReference("a_Ref", null, "f")]);

        Assert.Equal(
            "{\n  \"EnvironmentVariables\": [\n    {\n      \"SchemaName\": \"a_One\",\n      \"Value\": \"\"\n    },\n    {\n      \"SchemaName\": \"b_Two\",\n      \"Value\": \"\"\n    }\n  ],\n" +
            "  \"ConnectionReferences\": [\n    {\n      \"LogicalName\": \"a_Ref\",\n      \"ConnectionId\": \"\",\n      \"ConnectorId\": \"\"\n    },\n    {\n      \"LogicalName\": \"z_Ref\",\n      \"ConnectionId\": \"\",\n      \"ConnectorId\": \"/providers/z\"\n    }\n  ]\n}\n",
            DeploymentSettings.Generate(s));
    }

    [Fact]
    public void Missing_entries_fire_and_matching_ones_are_quiet()
    {
        var s = Sln([B.Env("a_Env")], [B.Ref("a_Ref")]);

        Assert.Equal(["missing connectionReference a_Ref", "missing environmentVariable a_Env"], Run(s, "{}"));
        Assert.Empty(Run(s, """{"EnvironmentVariables":[{"SchemaName":"a_Env","Value":"v"}],"ConnectionReferences":[{"LogicalName":"a_Ref","ConnectionId":"c"}]}"""));
    }

    [Fact]
    public void Extra_entries_fire_and_names_match_ignoring_case()
    {
        var s = Sln([B.Env("a_Env", def: "d")], [B.Ref("a_Ref")]);
        var json = """{"EnvironmentVariables":[{"SchemaName":"A_ENV","Value":"v"},{"SchemaName":"b_Extra"}],"ConnectionReferences":[{"LogicalName":"a_ref","ConnectionId":"c"},{"LogicalName":"c_Extra","ConnectionId":"c"}]}""";

        Assert.Equal(["extra connectionReference c_Extra", "extra environmentVariable b_Extra"], Run(s, json));
    }

    [Theory]
    [InlineData("""{"LogicalName":"a_Ref"}""")]
    [InlineData("""{"LogicalName":"a_Ref","ConnectionId":null}""")]
    [InlineData("""{"LogicalName":"a_Ref","ConnectionId":""}""")]
    [InlineData("""{"LogicalName":"a_Ref","ConnectionId":"  "}""")]
    [InlineData("""{"LogicalName":"a_Ref","ConnectionId":123}""")]
    [InlineData("""{"LogicalName":"a_Ref","ConnectionId":{}}""")]
    public void Empty_connection_id_fires(string entry) =>
        Assert.Equal(["emptyConnectionId connectionReference a_Ref"], Run(Sln(refs: [B.Ref("a_Ref")]), $$"""{"ConnectionReferences":[{{entry}}]}"""));

    [Theory]
    [InlineData("""{"SchemaName":"a_Env"}""")]
    [InlineData("""{"SchemaName":"a_Env","Value":null}""")]
    [InlineData("""{"SchemaName":"a_Env","Value":""}""")]
    public void Empty_value_fires_when_the_solution_has_no_default_and_no_current_value(string entry) =>
        Assert.Equal(["emptyValue environmentVariable a_Env"], Run(Sln([B.Env("a_Env")]), $$"""{"EnvironmentVariables":[{{entry}}]}"""));

    [Theory]
    [InlineData("d", null)]
    [InlineData(null, "c")]
    public void Empty_value_is_quiet_when_the_solution_has_a_default_or_current_value(string? def, string? cur) =>
        Assert.Empty(Run(Sln([B.Env("a_Env", def: def, cur: cur)]), """{"EnvironmentVariables":[{"SchemaName":"a_Env"}]}"""));

    // pac 2.13.1 imports a file without the variable (it keeps its default or current value) but rejects "Value": "".
    [Theory]
    [InlineData("d", null)]
    [InlineData(null, "c")]
    public void A_variable_left_out_is_quiet_when_the_solution_has_a_default_or_current_value(string? def, string? cur) =>
        Assert.Empty(Run(Sln([B.Env("a_Env", def: def, cur: cur)]), "{}"));

    [Fact]
    public void An_empty_string_value_fires_even_with_a_default_because_pac_rejects_it()
    {
        var p = DeploymentSettings.Validate(Sln([B.Env("a", def: "d"), B.Env("b", required: true)]), """{"EnvironmentVariables":[{"SchemaName":"a","Value":""},{"SchemaName":"b","Value":""}]}""");

        Assert.Equal(["emptyValue", "emptyValue"], p.Select(x => x.Kind));
        Assert.Equal(
            [
                "has an empty Value, which pac solution import rejects; set a value, or leave the entry out to keep its default or current value",
                "has an empty Value, which pac solution import rejects; set a value (required)",
            ],
            p.Select(x => x.Message));
    }

    [Fact]
    public void Non_string_value_counts_as_set() =>
        Assert.Empty(Run(Sln([B.Env("a_Env")]), """{"EnvironmentVariables":[{"SchemaName":"a_Env","Value":5}]}"""));

    [Fact]
    public void Empty_value_message_says_required()
    {
        var p = DeploymentSettings.Validate(Sln([B.Env("a", required: true), B.Env("b")]), """{"EnvironmentVariables":[{"SchemaName":"a"},{"SchemaName":"b"}]}""");

        Assert.Equal(["has no value and no default (required)", "has no value and no default"], p.Select(x => x.Message));
    }

    [Fact]
    public void Unknown_fields_and_arrays_are_tolerated()
    {
        var s = Sln([B.Env("a_Env")], [B.Ref("a_Ref")]);
        var json = """
            {"CopilotAgents":[],"Other":1,
             "EnvironmentVariables":[{"SchemaName":"a_Env","Value":"v","DefaultValue":"x","IsRequired":true,"Extra":{"a":1}}],
             "ConnectionReferences":[{"LogicalName":"a_Ref","ConnectionId":"c","ConnectorId":"/p"}]}
            """;

        Assert.Empty(Run(s, json));
    }

    [Fact]
    public void Duplicate_entries_fire_once_per_name_whatever_the_case()
    {
        var s = Sln([B.Env("a_Env")], [B.Ref("a_Ref")]);
        var json = """
            {"EnvironmentVariables":[{"SchemaName":"a_Env","Value":"v"},{"SchemaName":"A_ENV"},{"SchemaName":"a_env","Value":"w"}],
             "ConnectionReferences":[{"LogicalName":"a_Ref","ConnectionId":"c"},{"LogicalName":"a_Ref","ConnectionId":"c"}]}
            """;

        Assert.Equal(["duplicate connectionReference a_Ref", "duplicate environmentVariable a_Env"], Run(s, json));
    }

    [Fact]
    public void Problems_are_sorted_by_type_name_and_kind()
    {
        var s = Sln([B.Env("b_Env"), B.Env("a_Env")], [B.Ref("b_Ref")]);

        Assert.Equal(
            ["missing connectionReference b_Ref", "missing environmentVariable a_Env", "extra environmentVariable a_Extra", "missing environmentVariable b_Env"],
            Run(s, """{"EnvironmentVariables":[{"SchemaName":"a_Extra"}]}"""));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"EnvironmentVariables":{}}""")]
    [InlineData("""{"ConnectionReferences":"x"}""")]
    [InlineData("""{"EnvironmentVariables":["x"]}""")]
    [InlineData("""{"EnvironmentVariables":[{"SchemaName":1}]}""")]
    [InlineData("""{"ConnectionReferences":[{"ConnectionId":"c"}]}""")]
    public void Invalid_shapes_are_rejected(string json) =>
        Assert.Throws<InvalidDataException>(() => DeploymentSettings.Validate(Sln(), json));
}