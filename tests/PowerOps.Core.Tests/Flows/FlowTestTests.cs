using System.Text.Json.Nodes;

using PowerOps.Core.Flows;
using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Tests.Readers;

namespace PowerOps.Core.Tests;

public class FlowTestTests
{
    private const string File = "t.flowtest.json";
    private const string Manual = """{"manual":{"type":"Request","kind":"Button"}}""";

    private static JsonNode J(string json) => JsonNode.Parse(json)!;

    // Parsing

    private static FlowTestFile Parse(string json) => FlowTestFile.Parse(json, File);

    private static string Test(string extra = "", string flow = "F", string assertions = """[{"kind":"runStatus","equals":"Succeeded"}]""") =>
        "{\"flow\":\"" + flow + "\"," + extra + "\"assertions\":" + assertions + "}";

    [Fact]
    public void A_minimal_file_parses()
    {
        var test = Parse(Test());

        Assert.Equal("F", test.Flow);
        Assert.Null(test.Trigger);
        Assert.Empty(test.Mocks);
        Assert.Empty(test.EnvironmentVariables);
        Assert.Null(test.UtcNow);
        Assert.Null(test.Guid);
        Assert.Null(test.Workflow);
        var a = Assert.Single(test.Assertions);
        Assert.Equal("runStatus", a.Kind);
        Assert.Equal("Succeeded", a.Expected!.GetValue<string>());
    }

    [Fact]
    public void Every_property_is_read()
    {
        var test = Parse("""
            {
              "$schema": "../x.schema.json",
              "flow": "F",
              "trigger": { "headers": {"h": "1"}, "body": {"id": 7} },
              "mocks": {
                "One": { "status": "Succeeded", "outputs": { "statusCode": 200, "body": [1] } },
                "Many": [ { "status": "Succeeded" }, { "status": "Failed", "error": { "code": "BadGateway", "message": "no" } } ]
              },
              "environmentVariables": { "pops_A": "x", "pops_B": 2, "pops_C": null },
              "utcNow": "2026-01-02T03:04:05.0000000Z",
              "guid": "00000000-0000-0000-0000-000000000001",
              "workflow": { "name": "w" },
              "assertions": [
                { "kind": "runStatus", "equals": "Skipped" },
                { "kind": "actionStatus", "action": "A", "call": 1, "equals": "Failed" },
                { "kind": "inputs", "action": "A", "path": ["parameters", "item/x", 0], "equals": null },
                { "kind": "outputs", "action": "A", "equals": {} },
                { "kind": "variable", "name": "v", "path": ["a"], "equals": 1.5 },
                { "kind": "response", "path": ["statusCode"], "equals": 200 }
              ]
            }
            """);

        Assert.True(JsonNode.DeepEquals(J("""{"headers":{"h":"1"},"body":{"id":7}}"""), test.Trigger));
        Assert.Single(test.Mocks["One"]);
        Assert.Equal(["Succeeded", "Failed"], test.Mocks["Many"].Select(m => m.Status));
        Assert.Equal("BadGateway", test.Mocks["Many"][1].Error!["code"]!.GetValue<string>());
        Assert.Equal("x", test.EnvironmentVariables["pops_A"]!.GetValue<string>());
        Assert.Null(test.EnvironmentVariables["pops_C"]);
        Assert.Equal("2026-01-02T03:04:05.0000000Z", test.UtcNow);
        Assert.Equal("00000000-0000-0000-0000-000000000001", test.Guid);
        Assert.Equal("w", test.Workflow!["name"]!.GetValue<string>());
        Assert.Equal(6, test.Assertions.Count);
        Assert.Equal(1, test.Assertions[1].Call);
        Assert.Equal<object>(["parameters", "item/x", 0], test.Assertions[2].Path!);
        Assert.Null(test.Assertions[2].Expected);
        Assert.Equal("v", test.Assertions[4].Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"flow":"F"}""")]
    [InlineData("""{"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","assertions":[]}""")]
    [InlineData("""{"flow":1,"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"","assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","extra":1,"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","trigger":[],"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","trigger":{"x":1},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","trigger":{"headers":[]},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":[],"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":"x"},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":{"outputs":{}}},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":{"status":"TimedOut"}},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":[{"status":"Succeeded"},{"status":"Skipped"}]},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":{"status":"Succeeded","error":{"code":"x"}}},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":{"status":"Failed","error":{"other":"x"}}},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":{"status":"Succeeded","outputs":[]}},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","mocks":{"A":{"status":"Succeeded","extra":1}},"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","environmentVariables":[],"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","utcNow":1,"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","guid":true,"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","workflow":"w","assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","assertions":{}}""")]
    [InlineData("""{"flow":"F","assertions":[1]}""")]
    [InlineData("""{"flow":"F","assertions":[{"equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"other","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus","equals":"TimedOut"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus","equals":"Succeeded","action":"A"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus","equals":"Succeeded","call":0}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus","equals":"Succeeded","path":["a"]}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus","equals":"Succeeded","name":"v"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"runStatus","equals":"Succeeded","other":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"actionStatus","equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"actionStatus","action":"A","equals":"TimedOut"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"actionStatus","action":"A","equals":"Cancelled"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"actionStatus","action":"A","equals":"Succeeded","path":["a"]}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"actionStatus","action":"A","equals":"Succeeded","name":"v"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"actionStatus","action":1,"equals":"Succeeded"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"inputs","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"inputs","action":"A","equals":1,"name":"v"}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","call":-1,"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","call":1.5,"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","call":"1","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","path":"a","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","path":[true],"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","path":[-1],"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"outputs","action":"A","path":[1.5],"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"variable","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"variable","name":"v","action":"A","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"variable","name":"v","call":0,"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"response","action":"A","equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"response","call":0,"equals":1}]}""")]
    [InlineData("""{"flow":"F","assertions":[{"kind":"response","name":"v","equals":1}]}""")]
    public void A_file_that_breaks_a_rule_is_rejected_with_its_path_first(string json)
    {
        var e = Assert.Throws<InvalidDataException>(() => Parse(json));

        Assert.StartsWith(File + ":", e.Message);
    }

    [Fact]
    public void A_duplicate_property_is_rejected()
    {
        var e = Assert.Throws<InvalidDataException>(() => Parse("""{"flow":"F","flow":"G","assertions":[{"kind":"runStatus","equals":"Succeeded"}]}"""));

        Assert.StartsWith(File + ":", e.Message);
    }

    [Fact]
    public void A_duplicate_property_deep_inside_a_mock_is_rejected_when_the_file_is_read()
    {
        var e = Assert.Throws<InvalidDataException>(() => Parse(Test("\"mocks\":{\"A\":{\"status\":\"Succeeded\",\"outputs\":{\"body\":{\"a\":1,\"a\":2}}}},")));

        Assert.StartsWith(File + ":", e.Message);
    }

    [Fact]
    public void A_file_over_four_megabytes_is_rejected()
    {
        var json = Test(assertions: """[{"kind":"runStatus","equals":"Succeeded"}]""").Replace("\"flow\":\"F\"", "\"flow\":\"" + new string('x', 4 * 1024 * 1024) + "\"");

        var e = Assert.Throws<InvalidDataException>(() => Parse(json));

        Assert.StartsWith(File + ":", e.Message);
        Assert.Contains("4 MB", e.Message);
    }

    [Fact]
    public void Load_reads_a_file_and_rejects_one_over_four_megabytes()
    {
        using var folder = new TempFolder();
        folder.Write("ok.flowtest.json", Test());
        folder.Write("big.flowtest.json", new string(' ', 4 * 1024 * 1024 + 1));

        Assert.Equal("F", FlowTestFile.Load(System.IO.Path.Combine(folder.Path, "ok.flowtest.json")).Flow);
        var e = Assert.Throws<InvalidDataException>(() => FlowTestFile.Load(System.IO.Path.Combine(folder.Path, "big.flowtest.json")));
        Assert.StartsWith(System.IO.Path.Combine(folder.Path, "big.flowtest.json") + ":", e.Message);
    }

    [Fact]
    public void A_byte_order_mark_is_accepted()
    {
        Assert.Equal("F", Parse("﻿" + Test()).Flow);
    }

    // Running

    private static Workflow Flow(string name, string actions, string id = "00000000-0000-0000-0000-000000000001", string triggers = Manual, string parameters = "{}", int category = 5) =>
        new(id, name, category, 1, "Workflows/" + name + ".json",
            "{\"properties\":{\"definition\":{\"triggers\":" + triggers + ",\"actions\":" + actions + ",\"parameters\":" + parameters + "}}}");

    private static Solution Sol(string unique, string version, Workflow[] flows, params EnvironmentVariable[] variables) =>
        new(unique, version, false, new Publisher("p", "pp"), [], [], [], variables, flows, []);

    private static FlowTestResult Run(string testJson, params Solution[] solutions) =>
        FlowTestRunner.Run("sub/t.flowtest.json", Parse(testJson), solutions);

    private static FlowTestResult RunOne(string testJson, Workflow flow) => Run(testJson, Sol("S", "1.0.0.0", [flow]));

    private const string Compose = """{"C":{"type":"Compose","inputs":"x","runAfter":{}}}""";

    private static string Pass(string flow = "F") => Test(flow: flow);

    // Lookup

    [Fact]
    public void A_flow_is_found_by_its_display_name_and_the_report_names_it_and_its_solution()
    {
        var result = Run(Pass(), Sol("Other", "2.0.0.0", [Flow("G", Compose)]), Sol("Mine", "1.0.0.0", [Flow("F", Compose)]));

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
        Assert.Equal("sub/t.flowtest.json", result.File);
        Assert.Equal("F", result.Flow);
        Assert.Equal("Mine", result.Solution);
        Assert.Equal("Succeeded", result.RunStatus);
        Assert.Null(result.Message);
    }

    [Fact]
    public void The_display_name_is_matched_exactly()
    {
        var result = RunOne(Pass("f"), Flow("F", Compose));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("'f'", result.Message);
    }

    [Theory]
    [InlineData("{00000000-0000-0000-0000-00000000000A}")]
    [InlineData("00000000-0000-0000-0000-00000000000a")]
    [InlineData("00000000-0000-0000-0000-00000000000A")]
    public void A_flow_is_found_by_its_id_with_braces_and_case_ignored(string reference)
    {
        var result = RunOne(Pass(reference), Flow("F", Compose, id: "00000000-0000-0000-0000-00000000000a"));

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
        Assert.Equal("F", result.Flow);
    }

    [Fact]
    public void No_match_fails_and_lists_the_solutions()
    {
        var result = Run(Pass("Nope"), Sol("A", "1.0.0.0", [Flow("F", Compose)]), Sol("B", "2.0.0.0", []));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Equal("Nope", result.Flow);
        Assert.Null(result.Solution);
        Assert.Empty(result.Assertions);
        Assert.Contains("No cloud flow", result.Message);
        Assert.Contains("A 1.0.0.0, B 2.0.0.0", result.Message);
    }

    [Fact]
    public void Only_cloud_flows_are_found()
    {
        var result = RunOne(Pass(), Flow("F", Compose, category: 0));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void Two_flows_with_one_name_in_a_solution_fail_as_ambiguous()
    {
        var result = Run(Pass(), Sol("S", "1.0.0.0", [Flow("F", Compose), Flow("F", Compose, id: "00000000-0000-0000-0000-000000000002")]));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("2 flows", result.Message);
        Assert.Contains("S 1.0.0.0", result.Message);
    }

    [Fact]
    public void Flows_in_several_solutions_fail_as_ambiguous_and_name_each_solution_and_version()
    {
        var result = Run(Pass(), Sol("V", "1.0.0.0", [Flow("F", Compose)]), Sol("V", "2.0.0.0", [Flow("F", Compose)]));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("2 flows", result.Message);
        Assert.Contains("V 1.0.0.0, V 2.0.0.0", result.Message);
    }

    [Fact]
    public void Solutions_in_an_ambiguous_message_come_with_their_location_below_the_path()
    {
        var result = FlowTestRunner.Run(
            File,
            Parse(Pass()),
            [Sol("V", "1.0.0.0", [Flow("F", Compose)]), Sol("V", "1.0.0.0", [Flow("F", Compose)])],
            ["v1", "sub/v2"]);

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("in v1 (V 1.0.0.0), sub/v2 (V 1.0.0.0).", result.Message);
    }

    [Fact]
    public void A_flow_without_clientdata_or_a_definition_fails()
    {
        var none = Sol("S", "1.0.0.0", [Flow("F", Compose) with { ClientData = null }]);
        var empty = Sol("S", "1.0.0.0", [Flow("F", Compose) with { ClientData = "{\"properties\":{}}" }]);
        var broken = Sol("S", "1.0.0.0", [Flow("F", Compose) with { ClientData = "{" }]);

        foreach (var solution in new[] { none, empty, broken })
        {
            var result = Run(Pass(), solution);

            Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
            Assert.Contains("definition", result.Message);
        }
    }

    [Fact]
    public void A_definition_with_a_duplicate_property_fails_the_test_instead_of_throwing()
    {
        var duplicate = Flow("F", Compose) with { ClientData = "{\"properties\":{\"definition\":{\"triggers\":{},\"actions\":{\"C\":{\"type\":\"Compose\",\"inputs\":1,\"inputs\":2,\"runAfter\":{}}}}}}" };

        var result = RunOne(Pass(), duplicate);

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("definition", result.Message);
    }

    // Outcomes

    [Fact]
    public void A_connector_action_without_a_mock_is_unmocked_and_assertions_are_skipped()
    {
        var actions = """{"Call":{"type":"OpenApiConnection","inputs":{},"runAfter":{}}}""";

        var result = RunOne(Pass(), Flow("F", actions));

        Assert.Equal(FlowTestOutcome.Unmocked, result.Outcome);
        Assert.StartsWith("Action 'Call'", result.Message);
        Assert.Empty(result.Assertions);
        Assert.Null(result.RunStatus);
        Assert.Equal("F", result.Flow);
        Assert.Equal("S", result.Solution);
    }

    [Fact]
    public void An_unknown_function_is_unsupported()
    {
        var actions = """{"C":{"type":"Compose","inputs":"@addHours(utcNow(), 1)","runAfter":{}}}""";

        var result = RunOne(Test("\"utcNow\":\"2026-01-01T00:00:00.0000000Z\","), Flow("F", actions));

        Assert.Equal(FlowTestOutcome.Unsupported, result.Outcome);
        Assert.Contains("addHours", result.Message);
    }

    [Fact]
    public void A_mock_on_a_compose_fails_the_test_with_the_simulators_message()
    {
        var result = RunOne(Test("\"mocks\":{\"C\":{\"status\":\"Succeeded\"}},"), Flow("F", Compose));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("Mock 'C'", result.Message);
        Assert.Empty(result.Assertions);
    }

    [Fact]
    public void A_skipped_run_is_a_finished_run()
    {
        var triggers = """{"manual":{"type":"Request","conditions":[{"expression":"@equals(1, 2)"}]}}""";

        var result = RunOne(Test(assertions: """[{"kind":"runStatus","equals":"Skipped"}]"""), Flow("F", Compose, triggers: triggers));

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
        Assert.Equal("Skipped", result.RunStatus);
    }

    // Parameters

    private const string StatusParameter = "Default Status (pops_DefaultStatus)";

    private static Workflow ParameterFlow(string type = "String", string? defaultValue = "\"fromDefaultValue\"", bool schemaName = true) =>
        Flow(
            "F",
            "{\"C\":{\"type\":\"Compose\",\"inputs\":\"@parameters('" + StatusParameter + "')\",\"runAfter\":{}}}",
            parameters: "{\"" + StatusParameter + "\":{\"type\":\"" + type + "\"" + (defaultValue is null ? "" : ",\"defaultValue\":" + defaultValue) + (schemaName ? ",\"metadata\":{\"schemaName\":\"pops_DefaultStatus\"}" : "") + "}}");

    private static EnvironmentVariable Variable(string? current, string? @default) =>
        new("pops_DefaultStatus", "100000000", false, @default, current, "environmentvariabledefinitions/pops_DefaultStatus/environmentvariabledefinition.xml");

    private static FlowTestResult ParameterRun(Workflow flow, string testExtra, params EnvironmentVariable[] variables) =>
        Run(
            Test(testExtra, assertions: """[{"kind":"outputs","action":"C","equals":"(never the value)"}]"""),
            Sol("S", "1.0.0.0", [flow], variables));

    private static string Got(FlowTestResult result) => result.Assertions.Single().Actual?.ToJsonString() ?? "none";

    [Fact]
    public void A_parameter_takes_the_tests_value_first()
    {
        var result = ParameterRun(ParameterFlow(), "\"environmentVariables\":{\"pops_DefaultStatus\":\"fromTest\"},", Variable("fromCurrent", "fromSolutionDefault"));

        Assert.Equal("\"fromTest\"", Got(result));
    }

    [Fact]
    public void Then_the_solutions_current_value()
    {
        Assert.Equal("\"fromCurrent\"", Got(ParameterRun(ParameterFlow(), "", Variable("fromCurrent", "fromSolutionDefault"))));
    }

    [Fact]
    public void Then_the_solutions_default_value()
    {
        Assert.Equal("\"fromSolutionDefault\"", Got(ParameterRun(ParameterFlow(), "", Variable(null, "fromSolutionDefault"))));
    }

    [Fact]
    public void Then_the_parameters_own_default_value()
    {
        Assert.Equal("\"fromDefaultValue\"", Got(ParameterRun(ParameterFlow(), "", Variable(null, null))));
        Assert.Equal("\"fromDefaultValue\"", Got(ParameterRun(ParameterFlow(), "")));
    }

    [Fact]
    public void The_schema_name_can_come_from_the_label_suffix()
    {
        var result = ParameterRun(ParameterFlow(schemaName: false), "\"environmentVariables\":{\"pops_DefaultStatus\":\"fromTest\"},");

        Assert.Equal("\"fromTest\"", Got(result));
    }

    [Fact]
    public void A_parameter_with_no_value_anywhere_is_unmocked()
    {
        var result = ParameterRun(ParameterFlow(defaultValue: null), "");

        Assert.Equal(FlowTestOutcome.Unmocked, result.Outcome);
        Assert.Contains("parameter", result.Message);
    }

    [Theory]
    [InlineData("String", "abc", "\"abc\"")]
    [InlineData("SecureString", "abc", "\"abc\"")]
    [InlineData("Int", "42", "42")]
    [InlineData("Float", "1.5", "1.5")]
    [InlineData("Bool", "true", "true")]
    [InlineData("Bool", "False", "false")]
    [InlineData("Object", """{"a":[1]}""", """{"a":[1]}""")]
    [InlineData("Array", """[1,"b"]""", """[1,"b"]""")]
    public void The_solutions_text_is_converted_by_the_parameters_type(string type, string text, string json)
    {
        var result = Run(
            Test(assertions: "[{\"kind\":\"outputs\",\"action\":\"C\",\"equals\":" + json + "}]"),
            Sol("S", "1.0.0.0", [ParameterFlow(type)], Variable(text, null)));

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
    }

    [Theory]
    [InlineData("Int", "4.2")]
    [InlineData("Int", "x")]
    [InlineData("Float", "x")]
    [InlineData("Bool", "yes")]
    [InlineData("Object", "[1]")]
    [InlineData("Object", "{")]
    [InlineData("Array", "{}")]
    [InlineData("SecureObject", "{}")]
    public void A_value_that_does_not_convert_is_unsupported_and_names_the_variable(string type, string text)
    {
        var result = Run(Pass(), Sol("S", "1.0.0.0", [ParameterFlow(type)], Variable(text, null)));

        Assert.Equal(FlowTestOutcome.Unsupported, result.Outcome);
        Assert.Contains("pops_DefaultStatus", result.Message);
    }

    [Fact]
    public void The_message_of_a_bad_value_does_not_repeat_it_because_it_can_be_a_secret()
    {
        var result = Run(Pass(), Sol("S", "1.0.0.0", [ParameterFlow("Int")], Variable("s3cr3t-value", null)));

        Assert.Equal(FlowTestOutcome.Unsupported, result.Outcome);
        Assert.DoesNotContain("s3cr3t", result.Message);
    }

    [Fact]
    public void The_tests_value_is_not_converted()
    {
        var result = Run(
            Test("\"environmentVariables\":{\"pops_DefaultStatus\":\"x\"},", assertions: """[{"kind":"outputs","action":"C","equals":"x"}]"""),
            Sol("S", "1.0.0.0", [ParameterFlow("Int")], Variable("notANumber", null)));

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
    }

    [Fact]
    public void An_environment_variable_key_that_names_no_parameter_of_the_flow_fails_the_test()
    {
        var result = ParameterRun(ParameterFlow(), "\"environmentVariables\":{\"pops_DefaultStatuss\":\"x\"},", Variable("fromCurrent", null));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Equal("environment variable 'pops_DefaultStatuss' is not a parameter of this flow", result.Message);
        Assert.Empty(result.Assertions);
        Assert.Equal("F", result.Flow);
        Assert.Equal("S", result.Solution);
    }

    [Fact]
    public void An_environment_variable_key_fails_a_flow_that_has_no_parameters()
    {
        var result = RunOne(Test("\"environmentVariables\":{\"pops_A\":1},"), Flow("F", Compose));

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Contains("'pops_A'", result.Message);
    }

    // Assertions

    private const string AssertionActions = """
        {
          "Init": {"type":"InitializeVariable","inputs":{"variables":[{"name":"count","type":"integer","value":3}]},"runAfter":{}},
          "C": {"type":"Compose","inputs":{"a":{"b/c":[10,20]},"n":1,"f":2.50},"runAfter":{"Init":["Succeeded"]}},
          "Each": {"type":"Foreach","foreach":[1,2],"actions":{"Conn":{"type":"OpenApiConnection","inputs":{"parameters":{"id":"@item()"}},"runAfter":{}}},"runAfter":{"C":["Succeeded"]}},
          "Resp": {"type":"Response","inputs":{"statusCode":200,"headers":{"x":"y"},"body":{"ok":true}},"runAfter":{"Each":["Succeeded"]}},
          "Never": {"type":"Foreach","foreach":[],"actions":{"Inner":{"type":"Compose","inputs":1,"runAfter":{}}},"runAfter":{"Resp":["Succeeded"]}},
          "Skipme": {"type":"Compose","inputs":1,"runAfter":{"Resp":["Failed"]}}
        }
        """;

    private const string ConnMocks = "\"mocks\":{\"Conn\":[{\"status\":\"Succeeded\"},{\"status\":\"Succeeded\",\"outputs\":{\"body\":\"second\"}}]},";

    private static FlowTestResult RunAssertions(params string[] assertions) =>
        RunOne(Test(ConnMocks, assertions: "[" + string.Join(",", assertions) + "]"), Flow("F", AssertionActions));

    private static FlowAssertionResult One(string assertion) => RunAssertions(assertion).Assertions.Single();

    [Theory]
    [InlineData("""{"kind":"runStatus","equals":"Succeeded"}""", "run status")]
    [InlineData("""{"kind":"actionStatus","action":"C","equals":"Succeeded"}""", "status of C")]
    [InlineData("""{"kind":"actionStatus","action":"Skipme","equals":"Skipped"}""", "status of Skipme")]
    [InlineData("""{"kind":"actionStatus","action":"Conn","equals":"Succeeded"}""", "status of Conn")]
    [InlineData("""{"kind":"actionStatus","action":"Conn","call":1,"equals":"Succeeded"}""", "status of Conn call 1")]
    [InlineData("""{"kind":"inputs","action":"C","path":["a","b/c",1],"equals":20}""", "inputs of C at [\"a\",\"b/c\",1]")]
    [InlineData("""{"kind":"inputs","action":"C","path":[],"equals":{"a":{"b/c":[10,20]},"n":1,"f":2.5}}""", "inputs of C")]
    [InlineData("""{"kind":"outputs","action":"C","path":["n"],"equals":1.0}""", "outputs of C at [\"n\"]")]
    [InlineData("""{"kind":"outputs","action":"C","path":["f"],"equals":2.5}""", "outputs of C at [\"f\"]")]
    [InlineData("""{"kind":"outputs","action":"C","path":["f"],"equals":2.50}""", "outputs of C at [\"f\"]")]
    [InlineData("""{"kind":"inputs","action":"Conn","call":0,"path":["parameters","id"],"equals":1}""", "inputs of Conn call 0 at [\"parameters\",\"id\"]")]
    [InlineData("""{"kind":"inputs","action":"Conn","call":1,"path":["parameters","id"],"equals":2}""", "inputs of Conn call 1 at [\"parameters\",\"id\"]")]
    [InlineData("""{"kind":"outputs","action":"Conn","call":1,"path":["body"],"equals":"second"}""", "outputs of Conn call 1 at [\"body\"]")]
    [InlineData("""{"kind":"variable","name":"count","equals":3}""", "variable count")]
    [InlineData("""{"kind":"response","path":["statusCode"],"equals":200}""", "response at [\"statusCode\"]")]
    [InlineData("""{"kind":"response","path":["body","ok"],"equals":true}""", "response at [\"body\",\"ok\"]")]
    [InlineData("""{"kind":"response","path":["headers","x"],"equals":"y"}""", "response at [\"headers\",\"x\"]")]
    [InlineData("""{"kind":"response","equals":{"statusCode":200,"headers":{"x":"y"},"body":{"ok":true}}}""", "response")]
    public void An_assertion_that_holds_passes_and_is_described(string assertion, string description)
    {
        var result = One(assertion);

        Assert.True(result.Passed, result.Message ?? Got(RunAssertions(assertion)));
        Assert.Equal(description, result.Description);
        Assert.Equal(0, result.Index);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Numbers_are_compared_by_value()
    {
        Assert.True(One("""{"kind":"outputs","action":"C","path":["n"],"equals":1.00}""").Passed);
        Assert.True(One("""{"kind":"variable","name":"count","equals":3.0}""").Passed);
        Assert.False(One("""{"kind":"outputs","action":"C","path":["n"],"equals":"1"}""").Passed);
        Assert.False(One("""{"kind":"outputs","action":"C","path":["n"],"equals":1.1}""").Passed);
    }

    [Fact]
    public void A_failed_assertion_has_expected_and_actual()
    {
        var result = One("""{"kind":"runStatus","equals":"Failed"}""");

        Assert.False(result.Passed);
        Assert.Equal("Failed", result.Expected!.GetValue<string>());
        Assert.True(result.HasActual);
        Assert.Equal("Succeeded", result.Actual!.GetValue<string>());
    }

    [Fact]
    public void An_actual_null_is_an_actual()
    {
        var result = One("""{"kind":"outputs","action":"Conn","call":0,"equals":"x"}""");

        Assert.False(result.Passed);
        Assert.True(result.HasActual);
        Assert.Null(result.Actual);
    }

    [Fact]
    public void Without_call_every_call_must_match()
    {
        var holds = One("""{"kind":"inputs","action":"Conn","path":["parameters","id"],"equals":1}""");

        Assert.False(holds.Passed);
        Assert.Equal("call 1 of 2", holds.Message);
        Assert.Equal("2", holds.Actual!.ToJsonString());
    }

    [Fact]
    public void A_call_that_does_not_exist_fails_with_its_number_and_the_count()
    {
        var result = One("""{"kind":"actionStatus","action":"Conn","call":2,"equals":"Succeeded"}""");

        Assert.False(result.Passed);
        Assert.False(result.HasActual);
        Assert.Contains("call 2 of 2", result.Message);
    }

    [Fact]
    public void An_action_that_did_not_run_fails()
    {
        var result = One("""{"kind":"actionStatus","action":"Inner","equals":"Succeeded"}""");

        Assert.False(result.Passed);
        Assert.False(result.HasActual);
        Assert.Equal("did not run", result.Message);
    }

    [Fact]
    public void An_action_the_flow_does_not_have_says_so()
    {
        var result = One("""{"kind":"outputs","action":"Nope","equals":1}""");

        Assert.False(result.Passed);
        Assert.Contains("not in the flow", result.Message);
    }

    [Fact]
    public void A_missing_path_fails_with_path_not_found()
    {
        foreach (var path in new[] { """["a","zzz"]""", """["a","b/c",5]""", """["n","x"]""", """[0]""", """["a",0]""" })
        {
            var result = One("{\"kind\":\"inputs\",\"action\":\"C\",\"path\":" + path + ",\"equals\":1}");

            Assert.False(result.Passed, path);
            Assert.False(result.HasActual, path);
            Assert.Equal("path not found", result.Message);
        }
    }

    [Fact]
    public void A_path_into_a_value_that_is_null_is_not_found()
    {
        var result = One("""{"kind":"outputs","action":"Conn","call":0,"path":["body"],"equals":1}""");

        Assert.Equal("path not found", result.Message);
    }

    [Fact]
    public void An_unknown_variable_fails()
    {
        var result = One("""{"kind":"variable","name":"nope","equals":1}""");

        Assert.False(result.Passed);
        Assert.Contains("no variable named 'nope'", result.Message);
    }

    [Fact]
    public void A_response_assertion_fails_when_no_response_ran()
    {
        var result = RunOne(Test(assertions: """[{"kind":"response","equals":1}]"""), Flow("F", Compose));

        var assertion = result.Assertions.Single();
        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Equal("no Response action ran", assertion.Message);
    }

    [Fact]
    public void Every_assertion_is_checked_in_order_and_one_failure_fails_the_test()
    {
        var result = RunAssertions(
            """{"kind":"runStatus","equals":"Failed"}""",
            """{"kind":"runStatus","equals":"Succeeded"}""",
            """{"kind":"variable","name":"count","equals":4}""");

        Assert.Equal(FlowTestOutcome.Failed, result.Outcome);
        Assert.Equal("Succeeded", result.RunStatus);
        Assert.Equal([0, 1, 2], result.Assertions.Select(a => a.Index));
        Assert.Equal([false, true, false], result.Assertions.Select(a => a.Passed));
        Assert.Null(result.Message);
    }

    [Fact]
    public void A_test_with_all_assertions_passing_passes()
    {
        var result = RunAssertions("""{"kind":"runStatus","equals":"Succeeded"}""", """{"kind":"variable","name":"count","equals":3}""");

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
        Assert.All(result.Assertions, a => Assert.True(a.Passed));
    }

    [Fact]
    public void Trigger_utcNow_guid_and_workflow_reach_the_run()
    {
        var actions = """{"C":{"type":"Compose","inputs":["@triggerBody()['id']","@triggerOutputs()['headers']['h']","@utcNow()","@guid()","@workflow()['name']"],"runAfter":{}}}""";
        var test = Test(
            """
            "trigger":{"headers":{"h":"hv"},"body":{"id":7}},"utcNow":"2026-01-02T03:04:05.0000000Z","guid":"00000000-0000-0000-0000-000000000009","workflow":{"name":"wf"},
            """,
            assertions: """[{"kind":"outputs","action":"C","equals":[7,"hv","2026-01-02T03:04:05.0000000Z","00000000-0000-0000-0000-000000000009","wf"]}]""");

        var result = RunOne(test, Flow("F", actions));

        Assert.Equal(FlowTestOutcome.Passed, result.Outcome);
    }

    [Theory]
    [InlineData("""{"kind":"inputs","action":"Skipme","equals":null}""")]
    [InlineData("""{"kind":"outputs","action":"Skipme","equals":null}""")]
    [InlineData("""{"kind":"outputs","action":"Skipme","call":0,"equals":null}""")]
    public void A_skipped_call_has_no_inputs_or_outputs_so_the_assertion_fails_with_did_not_run(string assertion)
    {
        var result = One(assertion);

        Assert.False(result.Passed);
        Assert.False(result.HasActual);
        Assert.Equal("did not run", result.Message);
    }

    [Fact]
    public void Without_call_a_skipped_call_among_several_fails_and_names_it()
    {
        var actions = """
            {"Each":{"type":"Foreach","foreach":[1,2],"actions":{
               "Inner":{"type":"Compose","inputs":"@item()","runAfter":{}},
               "Later":{"type":"Compose","inputs":"@item()","runAfter":{"Inner":["Failed"]}}},"runAfter":{}}}
            """;

        var result = RunOne(Test(assertions: """[{"kind":"inputs","action":"Later","equals":null}]"""), Flow("F", actions));

        Assert.Equal("call 0 of 2: did not run", result.Assertions.Single().Message);
    }

    [Fact]
    public void A_skipped_status_can_still_be_asserted()
    {
        Assert.True(One("""{"kind":"actionStatus","action":"Skipme","call":0,"equals":"Skipped"}""").Passed);
    }

    // Report

    [Fact]
    public void A_name_with_a_newline_cannot_forge_a_report_line()
    {
        const string name = "x\\n::error::forged"; // the JSON escape of a newline
        var result = RunOne(
            Test(assertions: $$"""[{"kind":"variable","name":"{{name}}","equals":1},{"kind":"outputs","action":"{{name}}","equals":1}]"""),
            Flow("F", Compose));
        var text = new StringWriter();
        var markdown = new StringWriter();

        TestReport.WriteText([result], text);
        TestReport.WriteMarkdown([result], markdown);

        Assert.All(result.Assertions, a => Assert.Contains('\n', a.Message!));
        Assert.Equal(5, text.ToString().Split('\n').Length); // the test, its two assertions, the summary and the end
        Assert.DoesNotContain(text.ToString().Split('\n'), line => line.StartsWith("::error::", StringComparison.Ordinal));
        Assert.DoesNotContain(markdown.ToString().Split('\n'), line => line.StartsWith("::error::", StringComparison.Ordinal));
    }

    // Discovery

    [Fact]
    public void Test_files_are_found_recursively_sorted_by_forward_slash_path_and_skip_build_folders()
    {
        using var folder = new TempFolder();
        foreach (var name in new[] { "b.flowtest.json", "a/z.flowtest.json", "a/B.flowtest.json", "a-b/x.flowtest.json", "c/d/e.flowtest.json", "plain.json", "x.flowtest.json.bak", "bin/a.flowtest.json", "obj/a.flowtest.json", ".git/a.flowtest.json", "node_modules/a.flowtest.json", "c/bin/a.flowtest.json" })
        {
            folder.Write(name, "{}");
        }

        var found = FlowTestFiles.Find(folder.Path).Select(f => System.IO.Path.GetRelativePath(folder.Path, f).Replace('\\', '/'));

        Assert.Equal(["a-b/x.flowtest.json", "a/B.flowtest.json", "a/z.flowtest.json", "b.flowtest.json", "c/d/e.flowtest.json"], found);
    }

    [Fact]
    public void Linked_folders_are_not_followed()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("o.flowtest.json", "{}");
        folder.Write("a.flowtest.json", "{}");
        try
        {
            Directory.CreateSymbolicLink(System.IO.Path.Combine(folder.Path, "link"), outside.Path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var found = FlowTestFiles.Find(folder.Path).Select(f => System.IO.Path.GetRelativePath(folder.Path, f).Replace('\\', '/'));

        Assert.Equal(["a.flowtest.json"], found);
    }
}