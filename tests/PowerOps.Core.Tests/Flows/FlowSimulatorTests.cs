using System.Text.Json.Nodes;

using PowerOps.Core.Flows;

namespace PowerOps.Core.Tests;

public class FlowSimulatorTests
{
    private const string Manual = """{"manual":{"type":"Request","kind":"Button"}}""";

    private static JsonNode J(string json) => JsonNode.Parse(json)!;

    private static string Def(string actions, string triggers = Manual, string parameters = "{}") =>
        "{\"triggers\":" + triggers + ",\"actions\":" + actions + ",\"parameters\":" + parameters + "}";

    private static FlowMock Ok(string? outputs = null) => new("Succeeded", outputs is null ? null : J(outputs), null);

    private static FlowMock Fail(string? error = null) => new("Failed", null, error is null ? null : J(error));

    private static Dictionary<string, IReadOnlyList<FlowMock>> Mocks(params (string Name, FlowMock[] List)[] items) =>
        items.ToDictionary(i => i.Name, i => (IReadOnlyList<FlowMock>)i.List);

    private static FlowRunInputs In(
        string? trigger = null,
        Dictionary<string, IReadOnlyList<FlowMock>>? mocks = null,
        Dictionary<string, JsonNode?>? parameters = null,
        string? utcNow = null,
        string? guid = null,
        string? workflow = null) =>
        new(trigger is null ? null : J(trigger), mocks ?? [], parameters ?? [], utcNow, guid, workflow is null ? null : J(workflow));

    private static FlowRun Run(string actions, FlowRunInputs? inputs = null, string triggers = Manual, string parameters = "{}") =>
        FlowSimulator.Run((JsonObject)J(Def(actions, triggers, parameters)), inputs ?? In());

    private static string St(FlowRun run, string action, int call = 0) => run.Calls[action][call].Status;

    private static void Same(string expected, JsonNode? actual) =>
        Assert.True(JsonNode.DeepEquals(J(expected), actual), actual?.ToJsonString() ?? "null");

    private static string Compose(string value, string runAfter = "{}") => """{"type":"Compose","inputs":""" + value + ""","runAfter":""" + runAfter + "}";

    private static string Var(string name, string type, string? value = null, string runAfter = "{}") =>
        "{\"type\":\"InitializeVariable\",\"inputs\":{\"variables\":[{\"name\":\"" + name + "\",\"type\":\"" + type + "\"" + (value is null ? "" : ",\"value\":" + value) + "}]},\"runAfter\":" + runAfter + "}";

    private static string After(string action, string status = "Succeeded") => "{\"" + action + "\":[\"" + status + "\"]}";

    // Compose and variables

    [Fact]
    public void Compose_returns_its_evaluated_inputs()
    {
        var run = Run("""{"C":""" + Compose("""{"a":"@concat('x','y')","b":[1,"@{1}"]}""") + "}");

        Assert.Equal(FlowRunOutcome.Finished, run.Outcome);
        Assert.Equal("Succeeded", run.Status);
        Assert.Equal("Succeeded", St(run, "C"));
        Same("""{"a":"xy","b":[1,"1"]}""", run.Calls["C"][0].Inputs);
        Same("""{"a":"xy","b":[1,"1"]}""", run.Calls["C"][0].Outputs);
    }

    [Fact]
    public void Variables_are_set_and_appended_by_type()
    {
        var run = Run("{" + string.Join(",",
            "\"Init\":" + Var("s", "String", "\"a\""),
            "\"Add\":{\"type\":\"AppendToStringVariable\",\"inputs\":{\"name\":\"s\",\"value\":\"b\"},\"runAfter\":" + After("Init") + "}",
            "\"Arr\":" + Var("arr", "array", "[1]", After("Add")),
            "\"AddArr\":{\"type\":\"AppendToArrayVariable\",\"inputs\":{\"name\":\"arr\",\"value\":{\"k\":\"@variables('s')\"}},\"runAfter\":" + After("Arr") + "}",
            "\"N\":" + Var("n", "integer", "3", After("AddArr")),
            "\"SetN\":{\"type\":\"SetVariable\",\"inputs\":{\"name\":\"n\",\"value\":4},\"runAfter\":" + After("N") + "}",
            "\"F\":" + Var("f", "Float", "1.5", After("SetN")),
            "\"B\":" + Var("b", "boolean", "true", After("F")),
            "\"O\":" + Var("o", "OBJECT", null, After("B"))) + "}");

        Assert.Equal("Succeeded", run.Status);
        Assert.Equal("ab", (string?)run.Variables["s"]);
        Same("""[1,{"k":"ab"}]""", run.Variables["arr"]);
        Assert.Equal(4, (int?)run.Variables["n"]);
        Assert.Equal(1.5, (double?)run.Variables["f"]);
        Assert.True((bool?)run.Variables["b"]);
        Assert.Null(run.Variables["o"]);
        Assert.True(run.Variables.ContainsKey("o"));
    }

    [Theory]
    [InlineData("integer", "\"x\"")]
    [InlineData("string", "1")]
    [InlineData("boolean", "\"true\"")]
    [InlineData("array", "{}")]
    [InlineData("object", "[]")]
    [InlineData("float", "true")]
    public void A_variable_value_of_another_type_is_unsupported(string type, string value)
    {
        var run = Run("{\"V\":" + Var("v", type, value) + "}");

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("'V'", run.Message);
    }

    [Fact]
    public void Appending_a_non_string_to_a_string_variable_is_unsupported()
    {
        var run = Run("{\"V\":" + Var("v", "string", "\"a\"") + ",\"A\":{\"type\":\"AppendToStringVariable\",\"inputs\":{\"name\":\"v\",\"value\":1},\"runAfter\":" + After("V") + "}}");

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("'A'", run.Message);
    }

    [Fact]
    public void Setting_a_variable_that_was_never_initialized_fails_the_action_and_names_are_case_sensitive()
    {
        var run = Run("{\"V\":" + Var("s", "string", "\"a\"") + ",\"Set\":{\"type\":\"SetVariable\",\"inputs\":{\"name\":\"S\",\"value\":\"b\"},\"runAfter\":" + After("V") + "}}");

        Assert.Equal("Failed", St(run, "Set"));
        Assert.Equal("InvalidTemplate", (string?)run.Calls["Set"][0].Error!["code"]);
        Assert.Equal("Failed", run.Status);
        Assert.Equal("a", (string?)run.Variables["s"]);
    }

    // If, Switch, Scope

    private const string IfFlow = """
        {"N":{"type":"InitializeVariable","inputs":{"variables":[{"name":"n","type":"integer","value":VALUE}]},"runAfter":{}},
         "Check":{"type":"If","expression":{"and":[{"equals":["@variables('n')",1]},{"not":{"equals":[1,2]}}]},
           "actions":{"Yes":{"type":"Compose","inputs":"yes","runAfter":{}}},
           "else":{"actions":{"Other":{"type":"Scope","actions":{"Inner":{"type":"Compose","inputs":"inner","runAfter":{}}},"runAfter":{}}}},
           "runAfter":{"N":["Succeeded"]}}}
        """;

    [Fact]
    public void If_runs_the_true_branch_and_skips_the_else_branch_with_everything_in_it()
    {
        var run = Run(IfFlow.Replace("VALUE", "1"));

        Assert.Equal("Succeeded", St(run, "Check"));
        Assert.Equal("Succeeded", St(run, "Yes"));
        Assert.Equal("Skipped", St(run, "Other"));
        Assert.Equal("Skipped", St(run, "Inner"));
        Assert.Single(run.Calls["Inner"]);
        Assert.Null(run.Calls["Inner"][0].Inputs);
        Assert.Null(run.Calls["Inner"][0].Outputs);
    }

    [Fact]
    public void If_runs_the_else_branch_and_skips_the_true_branch()
    {
        var run = Run(IfFlow.Replace("VALUE", "2"));

        Assert.Equal("Skipped", St(run, "Yes"));
        Assert.Equal("Succeeded", St(run, "Other"));
        Assert.Equal("Succeeded", St(run, "Inner"));
    }

    [Fact]
    public void If_with_a_non_boolean_condition_fails_the_action()
    {
        var run = Run("""
            {"N":{"type":"InitializeVariable","inputs":{"variables":[{"name":"n","type":"integer","value":1}]},"runAfter":{}},
             "Check":{"type":"If","expression":"@variables('n')","actions":{},"runAfter":{"N":["Succeeded"]}}}
            """);

        Assert.Equal("Failed", St(run, "Check"));
        Assert.Equal("Failed", run.Status);
    }

    private const string SwitchFlow = """
        {"N":{"type":"InitializeVariable","inputs":{"variables":[{"name":"n","type":"integer","value":VALUE}]},"runAfter":{}},
         "Sw":{"type":"Switch","expression":"@variables('n')",
           "cases":{"One":{"case":1,"actions":{"A1":{"type":"Compose","inputs":1,"runAfter":{}}}},
                    "Two":{"case":2,"actions":{"A2":{"type":"Compose","inputs":2,"runAfter":{}}}}},
           "default":{"actions":{"D":{"type":"Compose","inputs":0,"runAfter":{}}}},
           "runAfter":{"N":["Succeeded"]}}}
        """;

    [Theory]
    [InlineData("1", "A1", "A2", "D")]
    [InlineData("2", "A2", "A1", "D")]
    [InlineData("9", "D", "A1", "A2")]
    public void Switch_runs_the_first_matching_case_or_the_default_and_skips_the_rest(string value, string ran, string skipped1, string skipped2)
    {
        var run = Run(SwitchFlow.Replace("VALUE", value));

        Assert.Equal("Succeeded", St(run, "Sw"));
        Assert.Equal("Succeeded", St(run, ran));
        Assert.Equal("Skipped", St(run, skipped1));
        Assert.Equal("Skipped", St(run, skipped2));
    }

    [Fact]
    public void Scope_takes_the_status_of_its_block_and_an_unhandled_failure_skips_the_rest()
    {
        var run = Run("""
            {"Try":{"type":"Scope","actions":{"Boom":{"type":"Compose","inputs":"@variables('nope')","runAfter":{}}},"runAfter":{}},
             "Next":{"type":"Compose","inputs":1,"runAfter":{"Try":["Succeeded"]}},
             "S":{"type":"Scope","actions":{"Inner":{"type":"Compose","inputs":1,"runAfter":{}},
                "Branch":{"type":"If","expression":"@true","actions":{"Deep":{"type":"Compose","inputs":1,"runAfter":{}}},"runAfter":{}}},
                "runAfter":{"Try":["Succeeded"]}}}
            """);

        Assert.Equal("Failed", St(run, "Try"));
        Assert.Equal("Failed", St(run, "Boom"));
        Assert.Equal("Skipped", St(run, "Next"));
        Assert.Equal("Skipped", St(run, "S"));
        foreach (var nested in new[] { "Inner", "Branch", "Deep" })
        {
            Assert.Single(run.Calls[nested]);
            Assert.Equal("Skipped", St(run, nested));
        }

        Assert.Equal("Failed", run.Status);
    }

    // Foreach

    [Fact]
    public void Foreach_over_an_empty_array_leaves_the_inner_actions_without_a_call()
    {
        var run = Run("{\"Arr\":" + Var("arr", "array", "[]") + """
            ,"Loop":{"type":"Foreach","foreach":"@variables('arr')","actions":{"Inner":{"type":"Compose","inputs":1,"runAfter":{}}},"runAfter":{"Arr":["Succeeded"]}}}
            """);

        Assert.Equal("Succeeded", St(run, "Loop"));
        Assert.False(run.Calls.ContainsKey("Inner"));
        Assert.Equal("Succeeded", run.Status);
    }

    [Fact]
    public void Foreach_runs_one_item_at_a_time_and_is_failed_when_an_iteration_fails()
    {
        var run = Run("""
            {"Loop":{"type":"Foreach","foreach":"@triggerBody()['list']","actions":{
               "Inner":{"type":"Compose","inputs":"@substring('abc', item(), 1)","runAfter":{}}},"runAfter":{}}}
            """, In("""{"body":{"list":[0,5,1]}}"""));

        Assert.Equal(["Succeeded", "Failed", "Succeeded"], run.Calls["Inner"].Select(c => c.Status));
        Assert.Equal("a", (string?)run.Calls["Inner"][0].Outputs);
        Assert.Equal("Failed", St(run, "Loop"));
        Assert.Equal("Failed", run.Status);
    }

    [Fact]
    public void Foreach_over_something_that_is_not_an_array_fails_the_action()
    {
        var run = Run("""{"Loop":{"type":"Foreach","foreach":"not an array","actions":{"Inner":{"type":"Compose","inputs":1,"runAfter":{}}},"runAfter":{}}}""");

        Assert.Equal("Failed", St(run, "Loop"));
        Assert.False(run.Calls.ContainsKey("Inner"));
    }

    [Fact]
    public void Nested_foreach_reads_the_outer_item_with_items()
    {
        var run = Run("""
            {"Outer":{"type":"Foreach","foreach":"@triggerBody()['groups']","actions":{
               "Inner":{"type":"Foreach","foreach":"@items('Outer')","actions":{
                 "C":{"type":"Compose","inputs":"@concat(string(length(items('Outer'))),':',string(item()))","runAfter":{}}},"runAfter":{}}},"runAfter":{}}}
            """, In("""{"body":{"groups":[[1,2],[3]]}}"""));

        Assert.Equal(["2:1", "2:2", "1:3"], run.Calls["C"].Select(c => (string?)c.Outputs));
        Assert.Equal(2, run.Calls["Inner"].Count);
        Assert.Equal("Succeeded", run.Status);
    }

    [Fact]
    public void Foreach_over_a_mocked_array_uses_a_list_mock_one_entry_per_call_and_records_each_calls_inputs()
    {
        var mocks = Mocks(("Call", [Ok("""{"statusCode":200,"body":{"n":1}}"""), Fail("""{"code":"BadRequest","message":"no"}""")]));
        var run = Run("""
            {"Loop":{"type":"Foreach","foreach":"@triggerBody()['items']","actions":{
               "Call":{"type":"Http","inputs":{"method":"GET","uri":"https://contoso.example/@{items('Loop')['id']}"},"runAfter":{}}},"runAfter":{}}}
            """, In("""{"body":{"items":[{"id":"a"},{"id":"b"}]}}""", mocks));

        Assert.Equal(["Succeeded", "Failed"], run.Calls["Call"].Select(c => c.Status));
        Same("""{"method":"GET","uri":"https://contoso.example/a"}""", run.Calls["Call"][0].Inputs);
        Same("""{"method":"GET","uri":"https://contoso.example/b"}""", run.Calls["Call"][1].Inputs);
        Same("""{"statusCode":200,"body":{"n":1}}""", run.Calls["Call"][0].Outputs);
        Same("""{"code":"BadRequest","message":"no"}""", run.Calls["Call"][1].Error);
        Assert.Null(run.Calls["Call"][0].Error);
        Assert.Equal("Failed", St(run, "Loop"));
        Assert.Equal("Failed", run.Status);
    }

    // Order and runAfter

    [Fact]
    public void Parallel_branches_run_one_after_the_other_and_the_first_ready_action_in_definition_order_goes_first()
    {
        static string Append(string name, string after, string text) =>
            "\"" + name + "\":{\"type\":\"AppendToStringVariable\",\"inputs\":{\"name\":\"s\",\"value\":\"" + text + "\"},\"runAfter\":" + after + "}";

        var run = Run("{\"S\":" + Var("s", "string", "\"\"") + "," + string.Join(",",
            Append("B", After("A"), "b"),
            Append("A", After("S"), "a"),
            Append("C", After("S"), "c")) + "}");

        // B is first in definition order but not ready until A is done; C is ready earlier but is rescanned after B.
        Assert.Equal("abc", (string?)run.Variables["s"]);
    }

    [Theory]
    [InlineData("""{"Outside":{"type":"Compose","inputs":1,"runAfter":{}},"S":{"type":"Scope","actions":{"X":{"type":"Compose","inputs":1,"runAfter":{"Outside":["Succeeded"]}}},"runAfter":{}}}""", "Outside")]
    [InlineData("""{"A":{"type":"Compose","inputs":1,"runAfter":{}},"B":{"type":"Compose","inputs":1,"runAfter":{"A":["Done"]}}}""", "Done")]
    [InlineData("""{"A":{"type":"Compose","inputs":1,"runAfter":{"B":["Succeeded"]}},"B":{"type":"Compose","inputs":1,"runAfter":{"A":["Succeeded"]}}}""", "cycle")]
    [InlineData("""{"A":{"type":"Compose","inputs":1,"runAfter":{"Missing":["Succeeded"]}}}""", "Missing")]
    public void A_runAfter_that_cannot_be_resolved_is_unsupported(string actions, string mentions)
    {
        var run = Run(actions);

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains(mentions, run.Message);
    }

    [Fact]
    public void RunAfter_on_Failed_makes_a_try_catch_and_result_lists_the_scopes_records()
    {
        var run = Run("""
            {"Try":{"type":"Scope","actions":{"Boom":{"type":"Compose","inputs":"@variables('nope')","runAfter":{}}},"runAfter":{}},
             "Catch":{"type":"Scope","actions":{
               "Failures":{"type":"Query","inputs":{"from":"@result('Try')","where":"@equals(item()['status'],'Failed')"},"runAfter":{}},
               "Msg":{"type":"Compose","inputs":"@first(body('Failures'))['name']","runAfter":{"Failures":["Succeeded"]}}},
               "runAfter":{"Try":["Failed"]}}}
            """);

        Assert.Equal("Succeeded", run.Status);
        Assert.Equal("Failed", St(run, "Try"));
        Assert.Equal("Succeeded", St(run, "Catch"));
        Assert.Equal("Boom", (string?)run.Calls["Msg"][0].Outputs);
        var failures = run.Calls["Failures"][0].Outputs!["body"]!.AsArray();
        Assert.Single(failures);
        Assert.Equal("InvalidTemplate", (string?)failures[0]!["error"]!["code"]);
        Assert.Null(failures[0]!["inputs"]);
    }

    [Fact]
    public void RunAfter_on_Skipped_runs_after_a_skipped_action()
    {
        var run = Run("""
            {"A":{"type":"Compose","inputs":1,"runAfter":{}},
             "B":{"type":"Compose","inputs":1,"runAfter":{"A":["Failed"]}},
             "C":{"type":"Compose","inputs":1,"runAfter":{"B":["Skipped"]}},
             "D":{"type":"Compose","inputs":1,"runAfter":{"B":["Succeeded"]}}}
            """);

        Assert.Equal("Skipped", St(run, "B"));
        Assert.Equal("Succeeded", St(run, "C"));
        Assert.Equal("Skipped", St(run, "D"));
        Assert.Equal("Succeeded", run.Status);
    }

    [Fact]
    public void A_failure_is_handled_only_by_a_handler_that_ran()
    {
        var run = Run("""
            {"A":{"type":"Compose","inputs":"@variables('nope')","runAfter":{}},
             "B":{"type":"Compose","inputs":1,"runAfter":{}},
             "H":{"type":"Compose","inputs":1,"runAfter":{"A":["Failed"],"B":["Failed"]}}}
            """);

        Assert.Equal("Failed", St(run, "A"));
        Assert.Equal("Succeeded", St(run, "B"));
        Assert.Equal("Skipped", St(run, "H"));
        Assert.Equal("Failed", run.Status);
    }

    // Until

    private static string UntilFlow(string expression, string limit) =>
        "{\"S\":" + Var("s", "string", "\"\"") + """
        ,"Loop":{"type":"Until","expression":EXPRESSION,"limit":LIMIT,"actions":{"Add":{"type":"AppendToStringVariable","inputs":{"name":"s","value":"x"},"runAfter":{}}},"runAfter":{"S":["Succeeded"]}}}
        """.Replace("EXPRESSION", expression).Replace("LIMIT", limit);

    [Fact]
    public void Until_runs_the_body_then_checks_the_condition_and_stops_when_it_holds()
    {
        var run = Run(UntilFlow("\"@equals(length(variables('s')), 3)\"", """{"count":10,"timeout":"PT1H"}"""));

        Assert.Equal(3, run.Calls["Add"].Count);
        Assert.Equal("xxx", (string?)run.Variables["s"]);
        Assert.Equal("Succeeded", St(run, "Loop"));
    }

    [Fact]
    public void Until_stops_at_its_count_limit_and_is_then_succeeded()
    {
        var run = Run(UntilFlow("\"@equals(1, 2)\"", """{"count":4}"""));

        Assert.Equal(4, run.Calls["Add"].Count);
        Assert.Equal("Succeeded", St(run, "Loop"));
        Assert.Equal("Succeeded", run.Status);
    }

    [Fact]
    public void Until_without_a_limit_stops_after_60_iterations()
    {
        var run = Run(UntilFlow("\"@equals(1, 2)\"", "{}"));

        Assert.Equal(60, run.Calls["Add"].Count);
    }

    private const string HttpUntil = """
        {"Loop":{"type":"Until","expression":"@equals(outputs('Http')?['statusCode'], 200)","limit":{"count":LIMIT},"actions":{
           "Http":{"type":"Http","inputs":{"method":"GET","uri":"https://contoso.example/x"},"runAfter":{}}},"runAfter":{}}}
        """;

    [Fact]
    public void An_Until_goes_on_after_a_failed_iteration_and_takes_its_last_iterations_status()
    {
        var run = Run(HttpUntil.Replace("LIMIT", "3"), In(mocks: Mocks(("Http", [Fail(), Ok("""{"statusCode":200}""")]))));

        Assert.Equal(FlowRunOutcome.Finished, run.Outcome);
        Assert.Equal(2, run.Calls["Http"].Count);
        Assert.Equal("Succeeded", St(run, "Loop"));
        Assert.Equal("Succeeded", run.Status);
    }

    [Fact]
    public void An_Until_whose_last_iteration_fails_is_failed()
    {
        var run = Run(HttpUntil.Replace("LIMIT", "2"), In(mocks: Mocks(("Http", [Fail(), Fail()]))));

        Assert.Equal(2, run.Calls["Http"].Count);
        Assert.Equal("Failed", St(run, "Loop"));
        Assert.Equal("Failed", run.Status);
    }

    [Theory]
    [InlineData("{\"count\":\"2\"}", "limit.count")]
    [InlineData("{\"count\":\"@variables('n')\"}", "limit.count")]
    [InlineData("{\"count\":2.5}", "limit.count")]
    [InlineData("{\"count\":null}", "limit.count")]
    [InlineData("\"3\"", "'limit'")]
    public void An_Until_limit_that_is_not_an_integer_count_is_unsupported(string limit, string field)
    {
        var run = Run(UntilFlow("\"@equals(1, 2)\"", limit));

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains(field, run.Message);
    }

    [Fact]
    public void An_Until_with_an_empty_body_and_a_huge_count_stops_at_the_call_cap()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var run = Run("""{"Loop":{"type":"Until","expression":"@equals(1, 2)","limit":{"count":3000000000},"actions":{},"runAfter":{}}}""");

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("100000", run.Message);
        // Loose on purpose: shared CI runners took 1.4-1.9 s; this only catches a cap that stops far too late.
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), watch.Elapsed.ToString());
    }

    [Fact]
    public void A_Foreach_with_an_empty_body_counts_every_item_toward_the_call_cap()
    {
        var items = "[" + string.Join(",", Enumerable.Repeat("0", 100_001)) + "]";

        var run = Run("""{"Loop":{"type":"Foreach","foreach":"@triggerBody()","actions":{},"runAfter":{}}}""", In("{\"body\":" + items + "}"));

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("100000", run.Message);
    }

    [Fact]
    public void A_value_nested_deeper_than_64_levels_is_unsupported()
    {
        var run = Run("{\"V\":" + Var("v", "array", "[]") + """
            ,"Loop":{"type":"Until","expression":"@equals(1, 2)","limit":{"count":100},"actions":{
               "Nest":{"type":"SetVariable","inputs":{"name":"v","value":"@createArray(variables('v'))"},"runAfter":{}}},"runAfter":{"V":["Succeeded"]}}}
            """);

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("a value nested deeper than 64 levels", run.Message);
    }

    [Fact]
    public void A_value_nested_exactly_64_levels_deep_is_fine()
    {
        var run = Run("{\"V\":" + Var("v", "array", "[]") + """
            ,"Loop":{"type":"Until","expression":"@equals(1, 2)","limit":{"count":60},"actions":{
               "Nest":{"type":"SetVariable","inputs":{"name":"v","value":"@createArray(variables('v'))"},"runAfter":{}}},"runAfter":{"V":["Succeeded"]}}}
            """);

        Assert.Equal(FlowRunOutcome.Finished, run.Outcome);
        Assert.Equal("Succeeded", run.Status);
    }

    // Memory

    [Fact]
    public void A_run_that_records_more_than_256_MB_of_values_is_unsupported_without_building_them()
    {
        var big = new string('a', 100_000);
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var run = Run(
            """{"Loop":{"type":"Until","expression":"@equals(1, 2)","limit":{"count":2000},"actions":{"C":{"type":"Compose","inputs":"@triggerBody()","runAfter":{}}},"runAfter":{}}}""",
            In("{\"body\":[\"" + big + "\",\"" + big + "\",\"" + big + "\"]}"));

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("256 MB", run.Message);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), watch.Elapsed.ToString());
    }

    // C-2. The probe shapes of the plan: a Foreach over N trigger items that reads triggerBody() each time (35 s at 40,000 items, over 100 s at 200,000),
    // and 20,000 appends of 1,000 characters (25 s and 1.1 GB).
    [Fact]
    public void A_Foreach_over_200000_trigger_items_that_reads_the_trigger_each_time_stops_at_the_call_limit_within_10_seconds()
    {
        var items = string.Join(',', Enumerable.Repeat("1", 200_000));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var run = Run(
            """{"Loop":{"type":"Foreach","foreach":"@triggerBody()?['items']","actions":{"C":{"type":"Compose","inputs":"@length(triggerBody()?['items'])","runAfter":{}}},"runAfter":{}}}""",
            In("{\"body\":{\"items\":[" + items + "]}}"));

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), watch.Elapsed.ToString());
        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("more than 100000 action calls", run.Message);
    }

    // The copies made inside expressions count against the same budget: the probes ran 57 s (string) and over 90 s (json of string) before.
    // 30 s, not 10: the macOS arm64 runner took up to 14 s under the full suite's load (5 s locally).
    [Theory]
    [InlineData("@length(string(triggerBody()))")]
    [InlineData("@length(json(string(triggerBody())))")]
    [InlineData("@length(replace(string(triggerBody()), '1', '22'))")]
    [InlineData("@length(split(string(triggerBody()), ','))")]
    public void A_Foreach_over_200000_trigger_items_that_copies_the_trigger_in_an_expression_hits_the_copy_budget_within_30_seconds(string expression)
    {
        var items = string.Join(',', Enumerable.Repeat("1", 200_000));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var run = Run(
            "{\"Loop\":{\"type\":\"Foreach\",\"foreach\":\"@triggerBody()?['items']\",\"actions\":{\"C\":{\"type\":\"Compose\",\"inputs\":\"" + expression + "\",\"runAfter\":{}}},\"runAfter\":{}}}",
            In("{\"body\":{\"items\":[" + items + "]}}"));

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), watch.Elapsed.ToString());
        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("pass 256 MB", run.Message);
    }

    [Fact]
    public void Twenty_thousand_appends_of_1000_characters_hit_the_copy_budget_within_10_seconds()
    {
        var items = string.Join(',', Enumerable.Repeat("1", 20_000));
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var run = Run(
            "{\"Init\":" + Var("s", "string", "\"\"") + ",\"Loop\":{\"type\":\"Foreach\",\"foreach\":\"@triggerBody()?['items']\",\"runAfter\":" + After("Init")
            + ",\"actions\":{\"Add\":{\"type\":\"AppendToStringVariable\",\"inputs\":{\"name\":\"s\",\"value\":\"" + new string('x', 1000) + "\"},\"runAfter\":{}}}}}",
            In("{\"body\":{\"items\":[" + items + "]}}"));

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), watch.Elapsed.ToString());
        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.StartsWith("Action 'Add': the run's recorded and copied values pass 256 MB", run.Message);
    }

    [Fact]
    public void Appends_that_copy_less_than_the_budget_still_finish()
    {
        var run = Run(
            "{\"Init\":" + Var("s", "string", "\"\"") + ",\"Loop\":{\"type\":\"Foreach\",\"foreach\":\"@triggerBody()?['items']\",\"runAfter\":" + After("Init")
            + ",\"actions\":{\"Add\":{\"type\":\"AppendToStringVariable\",\"inputs\":{\"name\":\"s\",\"value\":\"" + new string('x', 100) + "\"},\"runAfter\":{}}}}}",
            In("{\"body\":{\"items\":[" + string.Join(',', Enumerable.Repeat("1", 500)) + "]}}"));

        Assert.Equal(FlowRunOutcome.Finished, run.Outcome);
        Assert.Equal(50_000, ((string?)run.Variables["s"])!.Length);
    }

    [Fact]
    public void Reading_the_trigger_the_workflow_and_a_parameter_without_a_clone_leaves_the_inputs_alone_and_unshared()
    {
        var trigger = J("""{"body":{"k":[1,2]},"headers":{}}""");
        var workflow = J("""{"name":"w"}""");
        var given = J("""{"g":[1]}""");
        var inputs = new FlowRunInputs(trigger, new Dictionary<string, IReadOnlyList<FlowMock>>(), new Dictionary<string, JsonNode?> { ["p"] = given, ["q"] = null }, null, null, workflow);

        var run = Run(
            "{\"A\":" + Compose("\"@triggerOutputs()\"") + ",\"B\":" + Compose("\"@triggerBody()\"", After("A")) + ",\"C\":" + Compose("\"@workflow()\"", After("B"))
            + ",\"D\":" + Compose("\"@parameters('p')\"", After("C")) + ",\"E\":" + Compose("\"@parameters('d')\"", After("D"))
            + ",\"F\":" + Compose("\"@createArray(triggerBody(), if(true, workflow(), 1))\"", After("E")) + "}",
            inputs,
            parameters: """{"p":{"type":"Object"},"q":{"type":"Object"},"d":{"type":"Object","defaultValue":{"x":1}}}""");

        Assert.Equal(FlowRunOutcome.Finished, run.Outcome);
        foreach (var name in new[] { "A", "B", "C", "D", "E", "F" })
        {
            var call = run.Calls[name][0];
            Assert.Equal("Succeeded", call.Status);
            Assert.NotSame(trigger, call.Inputs);
            Assert.NotSame(call.Inputs, call.Outputs);
            Assert.Null(call.Inputs?.Parent);
            (call.Inputs as JsonObject)?.Add("added", 1);
            (call.Outputs as JsonObject)?.Add("added", 1);
        }

        Same("""{"body":{"k":[1,2]},"headers":{}}""", trigger);
        Same("""{"name":"w"}""", workflow);
        Same("""{"g":[1]}""", given);
        Assert.Null(trigger.Parent);
        Assert.Null(workflow.Parent);
        Assert.Null(given.Parent);
    }

    [Fact]
    public void A_replace_that_would_give_a_string_over_the_concat_limit_is_unsupported_without_building_it()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var run = Run(
            """{"C":{"type":"Compose","inputs":"@replace(triggerBody(), 'a', triggerBody())","runAfter":{}}}""",
            In("{\"body\":\"" + new string('a', 30_000) + "\"}"));

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("104857600", run.Message);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), watch.Elapsed.ToString());
    }

    // Malformed definitions

    [Theory]
    [InlineData("[]", Manual, "'actions'")]
    [InlineData("""{"S":{"type":"Scope","actions":[],"runAfter":{}}}""", Manual, "'actions'")]
    [InlineData("""{"I":{"type":"If","expression":true,"actions":{},"else":{"actions":[]},"runAfter":{}}}""", Manual, "'actions'")]
    [InlineData("""{"A":{"type":"Compose","inputs":1,"runAfter":{}},"B":{"type":"Compose","inputs":1,"runAfter":{"A":"Succeeded"}}}""", Manual, "'runAfter'")]
    [InlineData("""{"A":{"type":"Compose","inputs":1,"runAfter":[]}}""", Manual, "'runAfter'")]
    [InlineData("""{"A":{"type":"Compose","inputs":1,"runAfter":{}}}""", """{"manual":{"type":"Request","conditions":"@equals(1, 2)"}}""", "'conditions'")]
    public void A_field_of_the_wrong_json_type_is_unsupported_and_named(string actions, string triggers, string field)
    {
        var run = Run(actions, triggers: triggers);

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains(field, run.Message);
    }

    // Terminate

    [Fact]
    public void Terminate_with_Succeeded_ends_the_run_and_skips_every_unstarted_action_at_any_depth()
    {
        var run = Run("""
            {"A":{"type":"Compose","inputs":1,"runAfter":{}},
             "T":{"type":"Terminate","inputs":{"runStatus":"Succeeded"},"runAfter":{"A":["Succeeded"]}},
             "B":{"type":"Compose","inputs":1,"runAfter":{"T":["Succeeded"]}},
             "S":{"type":"Scope","actions":{"Inner":{"type":"Compose","inputs":1,"runAfter":{}}},"runAfter":{"T":["Succeeded"]}}}
            """);

        Assert.Equal("Succeeded", run.Status);
        Assert.Equal("Succeeded", St(run, "T"));
        Assert.Equal("Skipped", St(run, "B"));
        Assert.Equal("Skipped", St(run, "S"));
        Assert.Equal("Skipped", St(run, "Inner"));
        Assert.Single(run.Calls["Inner"]);
    }

    [Fact]
    public void Terminate_with_Failed_and_a_runError_fails_the_run_even_from_inside_a_scope()
    {
        var run = Run("""
            {"S":{"type":"Scope","actions":{
               "T":{"type":"Terminate","inputs":{"runStatus":"Failed","runError":{"code":"Nope","message":"stop"}},"runAfter":{}},
               "Later":{"type":"Compose","inputs":1,"runAfter":{"T":["Succeeded"]}}},"runAfter":{}},
             "After":{"type":"Compose","inputs":1,"runAfter":{"S":["Succeeded"]}}}
            """);

        Assert.Equal("Failed", run.Status);
        Assert.Equal("Succeeded", St(run, "T"));
        Same("""{"runStatus":"Failed","runError":{"code":"Nope","message":"stop"}}""", run.Calls["T"][0].Inputs);
        Assert.Equal("Skipped", St(run, "Later"));
        Assert.Equal("Skipped", St(run, "After"));
    }

    [Fact]
    public void Terminate_with_Cancelled_ends_the_run_as_cancelled()
    {
        var run = Run("""{"T":{"type":"Terminate","inputs":{"runStatus":"Cancelled"},"runAfter":{}}}""");

        Assert.Equal("Cancelled", run.Status);
    }

    // Response, ParseJson, Query, Wait

    [Fact]
    public void Response_outputs_status_headers_and_body_and_the_first_one_that_ran_is_the_run_response()
    {
        var run = Run("""
            {"R1":{"type":"Response","kind":"Http","inputs":{"statusCode":200,"headers":{"h":"v"},"body":{"ok":"@{1}"},"schema":{"x":"@bad("}},"runAfter":{}},
             "R2":{"type":"Response","kind":"Http","inputs":{"statusCode":500},"runAfter":{"R1":["Succeeded"]}}}
            """);

        Same("""{"statusCode":200,"headers":{"h":"v"},"body":{"ok":"1"}}""", run.Response);
        Same("""{"statusCode":200,"headers":{"h":"v"},"body":{"ok":"1"}}""", run.Calls["R1"][0].Outputs);
        Assert.Equal("Succeeded", St(run, "R2"));
    }

    [Theory]
    [InlineData("\"{\\\"a\\\":[1]}\"", """{"body":{"a":[1]}}""", "Succeeded")]
    [InlineData("""{"a":2}""", """{"body":{"a":2}}""", "Succeeded")]
    [InlineData("\"not json\"", null, "Failed")]
    public void ParseJson_outputs_the_parsed_content_as_body(string content, string? body, string status)
    {
        var run = Run("""{"P":{"type":"ParseJson","inputs":{"content":""" + content + ""","schema":{"type":"object"}},"runAfter":{}}}""");

        Assert.Equal(status, St(run, "P"));
        if (body is null)
        {
            Assert.Null(run.Calls["P"][0].Outputs);
            Assert.NotNull(run.Calls["P"][0].Error);
        }
        else
        {
            Same(body, run.Calls["P"][0].Outputs);
        }
    }

    [Fact]
    public void Query_keeps_the_items_where_the_condition_holds()
    {
        var run = Run("""
            {"Q":{"type":"Query","inputs":{"from":"@triggerBody()['list']","where":"@greater(item(), 2)"},"runAfter":{}},
             "Bad":{"type":"Query","inputs":{"from":"text","where":"@true"},"runAfter":{}}}
            """, In("""{"body":{"list":[1,2,3,4]}}"""));

        Same("""{"body":[3,4]}""", run.Calls["Q"][0].Outputs);
        Assert.Equal("Failed", St(run, "Bad"));
    }

    [Fact]
    public void Wait_succeeds_at_once_without_outputs()
    {
        var run = Run("""{"W":{"type":"Wait","inputs":{"interval":{"count":1,"unit":"Second"}},"runAfter":{}}}""");

        Assert.Equal("Succeeded", St(run, "W"));
        Assert.Null(run.Calls["W"][0].Outputs);
    }

    // Mocks, unmocked, unsupported, invalid

    [Theory]
    [InlineData("OpenApiConnection")]
    [InlineData("OpenApiConnectionWebhook")]
    [InlineData("Http")]
    [InlineData("Workflow")]
    public void A_connector_action_without_a_mock_stops_the_run_as_unmocked_naming_it(string type)
    {
        var run = Run("""{"Get_row":{"type":"%TYPE%","inputs":{"x":1},"runAfter":{}}}""".Replace("%TYPE%", type));

        Assert.Equal(FlowRunOutcome.Unmocked, run.Outcome);
        Assert.Contains("'Get_row'", run.Message);
        Assert.Null(run.Status);
    }

    [Fact]
    public void A_list_mock_that_runs_out_is_unmocked_on_the_first_call_it_lacks()
    {
        var run = Run("""
            {"Loop":{"type":"Foreach","foreach":"@triggerBody()['list']","actions":{
               "Call":{"type":"Http","inputs":{"uri":"u"},"runAfter":{}}},"runAfter":{}}}
            """, In("""{"body":{"list":[1,2]}}""", Mocks(("Call", [Ok()]))));

        Assert.Equal(FlowRunOutcome.Unmocked, run.Outcome);
        Assert.Contains("'Call'", run.Message);
        Assert.Single(run.Calls["Call"]);
    }

    [Fact]
    public void An_action_with_its_static_result_on_needs_a_mock_and_the_mock_replaces_its_work()
    {
        const string actions = """{"C":{"type":"Compose","inputs":"@concat('a','b')","runAfter":{},"runtimeConfiguration":{"staticResult":{"staticResultOptions":"Enabled","name":"C0"}}}}""";

        var without = Run(actions);
        Assert.Equal(FlowRunOutcome.Unmocked, without.Outcome);
        Assert.Contains("static result", without.Message);

        var with = Run(actions, In(mocks: Mocks(("C", [Ok("""{"body":"mocked"}""")]))));
        Assert.Equal("Succeeded", St(with, "C"));
        Same("""{"body":"mocked"}""", with.Calls["C"][0].Outputs);
        Assert.Equal("ab", (string?)with.Calls["C"][0].Inputs);
    }

    [Fact]
    public void A_static_result_that_is_not_enabled_does_not_need_a_mock()
    {
        var run = Run("""{"C":{"type":"Compose","inputs":1,"runAfter":{},"runtimeConfiguration":{"staticResult":{"staticResultOptions":"Disabled","name":"C0"}}}}""");

        Assert.Equal("Succeeded", run.Status);
    }

    [Fact]
    public void An_unknown_action_type_and_operationOptions_are_unsupported_only_when_reached()
    {
        var select = Run("""{"Pick":{"type":"Select","inputs":{"from":[1],"select":"@item()"},"runAfter":{}}}""");
        Assert.Equal(FlowRunOutcome.Unsupported, select.Outcome);
        Assert.Contains("'Pick'", select.Message);
        Assert.Contains("Select", select.Message);

        var options = Run("""{"Call":{"type":"Http","inputs":{},"operationOptions":"DisableAsyncPattern","runAfter":{}}}""", In(mocks: Mocks(("Call", [Ok()]))));
        Assert.Equal(FlowRunOutcome.Unsupported, options.Outcome);
        Assert.Contains("operationOptions", options.Message);

        var untaken = Run("""{"If":{"type":"If","expression":"@false","actions":{"Pick":{"type":"Select","inputs":{},"runAfter":{}}},"runAfter":{}}}""");
        Assert.Equal(FlowRunOutcome.Finished, untaken.Outcome);
        Assert.Equal("Skipped", St(untaken, "Pick"));
    }

    [Theory]
    [InlineData("C", "Succeeded", "C")]
    [InlineData("Scope", "Succeeded", "Scope")]
    [InlineData("Nope", "Succeeded", "Nope")]
    [InlineData("Call", "Skipped", "Call")]
    public void A_mock_that_cannot_apply_is_invalid_and_names_the_mock(string name, string status, string mentions)
    {
        var run = Run("""
            {"C":{"type":"Compose","inputs":1,"runAfter":{}},
             "Scope":{"type":"Scope","actions":{},"runAfter":{}},
             "Call":{"type":"Http","inputs":{},"runAfter":{}}}
            """, In(mocks: new Dictionary<string, IReadOnlyList<FlowMock>> { [name] = [new FlowMock(status, null, null)] }));

        Assert.Equal(FlowRunOutcome.Invalid, run.Outcome);
        Assert.Contains("'" + mentions + "'", run.Message);
        Assert.Empty(run.Calls);
    }

    // Triggers

    [Fact]
    public void More_than_one_trigger_or_a_splitOn_is_unsupported()
    {
        var two = Run("{}", triggers: """{"a":{"type":"Request"},"b":{"type":"Request"}}""");
        Assert.Equal(FlowRunOutcome.Unsupported, two.Outcome);

        var split = Run("{}", triggers: """{"a":{"type":"Request","splitOn":"@triggerBody()?['value']"}}""");
        Assert.Equal(FlowRunOutcome.Unsupported, split.Outcome);
        Assert.Contains("splitOn", split.Message);
    }

    [Theory]
    [InlineData("false", "Skipped", false)]
    [InlineData("true", "Succeeded", true)]
    public void A_trigger_condition_that_is_false_skips_the_run_without_running_anything(string go, string status, bool ran)
    {
        var run = Run(
            """{"C":{"type":"Compose","inputs":1,"runAfter":{}}}""",
            In("""{"body":{"go":""" + go + "}}"),
            """{"t":{"type":"Request","conditions":[{"expression":"@equals(triggerBody()?['go'], true)"}]}}""");

        Assert.Equal(FlowRunOutcome.Finished, run.Outcome);
        Assert.Equal(status, run.Status);
        Assert.Equal(ran, run.Calls.ContainsKey("C"));
    }

    [Theory]
    [InlineData("@triggerOutputs()", "trigger")]
    [InlineData("@utcNow()", "utcNow")]
    [InlineData("@guid()", "guid")]
    [InlineData("@workflow()", "workflow")]
    public void A_value_only_the_test_can_give_stops_the_run_as_unmocked_naming_the_property(string expression, string property)
    {
        var run = Run("""{"C":{"type":"Compose","inputs":"%E%","runAfter":{}}}""".Replace("%E%", expression));

        Assert.Equal(FlowRunOutcome.Unmocked, run.Outcome);
        Assert.Contains("'" + property + "'", run.Message);
        Assert.Contains("'C'", run.Message);
    }

    [Fact]
    public void The_values_the_test_gives_are_returned_by_their_functions()
    {
        var run = Run(
            """{"C":{"type":"Compose","inputs":["@utcNow()","@guid()","@workflow()['name']","@triggerBody()['x']"],"runAfter":{}}}""",
            In("""{"body":{"x":1}}""", utcNow: "2018-04-15T13:00:00Z", guid: "00000000-0000-0000-0000-000000000001", workflow: """{"name":"flow1"}"""));

        Same("""["2018-04-15T13:00:00.0000000Z","00000000-0000-0000-0000-000000000001","flow1",1]""", run.Calls["C"][0].Outputs);
    }

    // Parameters

    [Fact]
    public void A_parameter_comes_from_the_inputs_then_its_default_value_and_otherwise_stops_the_run()
    {
        const string parameters = """{"p1":{"type":"String","defaultValue":"fallback"},"p2":{"type":"String"}}""";
        var read1 = """{"C":{"type":"Compose","inputs":"@parameters('p1')","runAfter":{}}}""";

        var given = Run(read1, In(parameters: new() { ["p1"] = JsonValue.Create("given") }), parameters: parameters);
        Assert.Equal("given", (string?)given.Calls["C"][0].Outputs);

        var fallback = Run(read1, parameters: parameters);
        Assert.Equal("fallback", (string?)fallback.Calls["C"][0].Outputs);

        var missing = Run("""{"C":{"type":"Compose","inputs":"@parameters('p2')","runAfter":{}}}""", parameters: parameters);
        Assert.Equal(FlowRunOutcome.Unmocked, missing.Outcome);
        Assert.Contains("p2", missing.Message);

        var unknown = Run("""{"C":{"type":"Compose","inputs":"@parameters('zzz')","runAfter":{}}}""", parameters: parameters);
        Assert.Equal("Failed", St(unknown, "C"));
    }

    // outputs()

    [Fact]
    public void Outputs_of_an_action_that_was_skipped_or_has_not_run_fails_the_reader()
    {
        var run = Run("""
            {"Z":{"type":"Compose","inputs":1,"runAfter":{}},
             "A":{"type":"Compose","inputs":1,"runAfter":{"Z":["Failed"]}},
             "R":{"type":"Compose","inputs":"@outputs('A')","runAfter":{"A":["Skipped"]}},
             "E":{"type":"Compose","inputs":"@outputs('Later')","runAfter":{"Z":["Succeeded"]}},
             "Later":{"type":"Compose","inputs":1,"runAfter":{"E":["Failed"]}}}
            """);

        Assert.Equal("Failed", St(run, "R"));
        Assert.Equal("InvalidTemplate", (string?)run.Calls["R"][0].Error!["code"]);
        Assert.Equal("Failed", St(run, "E"));
    }

    [Fact]
    public void Outputs_of_an_action_inside_a_loop_can_be_read_inside_it_but_not_from_outside()
    {
        const string loop = """
            "Loop":{"type":"Foreach","foreach":"@triggerBody()['list']","actions":{
               "In":{"type":"Compose","inputs":"@item()","runAfter":{}},
               "Peek":{"type":"Compose","inputs":"@outputs('In')","runAfter":{"In":["Succeeded"]}}},"runAfter":{}}
            """;

        var inside = Run("{" + loop + "}", In("""{"body":{"list":[1,2]}}"""));
        Assert.Equal([1, 2], inside.Calls["Peek"].Select(c => (int?)c.Outputs));

        var outside = Run("{" + loop + ",\"Out\":" + Compose("\"@outputs('In')\"", After("Loop")) + "}", In("""{"body":{"list":[1,2]}}"""));
        Assert.Equal(FlowRunOutcome.Unsupported, outside.Outcome);
        Assert.Contains("'In'", outside.Message);
        Assert.Contains("'Out'", outside.Message);
    }

    [Fact]
    public void Result_of_a_loop_is_unsupported_and_of_a_non_scope_fails_the_reader()
    {
        var loop = Run("""
            {"Loop":{"type":"Foreach","foreach":[1],"actions":{},"runAfter":{}},
             "C":{"type":"Compose","inputs":"@result('Loop')","runAfter":{"Loop":["Succeeded"]}}}
            """);
        Assert.Equal(FlowRunOutcome.Unsupported, loop.Outcome);

        var notScope = Run("""
            {"A":{"type":"Compose","inputs":1,"runAfter":{}},
             "C":{"type":"Compose","inputs":"@result('A')","runAfter":{"A":["Succeeded"]}}}
            """);
        Assert.Equal("Failed", St(notScope, "C"));
    }

    // Limits and purity

    [Fact]
    public void A_run_of_more_than_100000_action_calls_is_unsupported()
    {
        var items = string.Join(",", Enumerable.Range(0, 200));
        var run = Run("""
            {"Loop":{"type":"Foreach","foreach":[%ITEMS%],"actions":{
               "Until":{"type":"Until","expression":"@equals(1, 2)","limit":{"count":1000},"actions":{
                 "C":{"type":"Compose","inputs":1,"runAfter":{}}},"runAfter":{}}},"runAfter":{}}}
            """.Replace("%ITEMS%", items));

        Assert.Equal(FlowRunOutcome.Unsupported, run.Outcome);
        Assert.Contains("100000", run.Message);
    }

    [Theory]
    [InlineData("""{"C":{"type":"Compose","inputs":"@json('\"\\ud800\"')","runAfter":{}}}""", "C", "json() cannot read its argument")]
    [InlineData("""{"C":{"type":"ParseJson","inputs":{"content":"{\"a\":\"\\ud800\"}"},"runAfter":{}}}""", "C", "The content is not valid JSON")]
    public void A_lone_surrogate_in_json_text_fails_the_action_without_throwing(string actions, string action, string message)
    {
        var run = Run(actions);

        Assert.Equal("Failed", run.Status);
        Assert.Contains(message, run.Calls[action][0].Error!["message"]!.GetValue<string>());
    }

    [Fact]
    public void A_run_changes_neither_the_definition_nor_the_inputs()
    {
        var definition = (JsonObject)J(Def("""
            {"V":{"type":"InitializeVariable","inputs":{"variables":[{"name":"arr","type":"array","value":"@triggerBody()['list']"}]},"runAfter":{}},
             "Add":{"type":"AppendToArrayVariable","inputs":{"name":"arr","value":"@triggerBody()"},"runAfter":{"V":["Succeeded"]}},
             "Loop":{"type":"Foreach","foreach":"@variables('arr')","actions":{
               "Call":{"type":"Http","inputs":{"uri":"@concat('u', string(item()))"},"runAfter":{}}},"runAfter":{"Add":["Succeeded"]}},
             "R":{"type":"Response","kind":"Http","inputs":{"statusCode":200,"body":"@triggerBody()"},"runAfter":{"Loop":["Succeeded"]}}}
            """));
        var mocks = Mocks(("Call", [Ok("""{"body":{"a":1}}"""), Ok("""{"body":{"a":2}}"""), Ok("""{"body":{"a":3}}""")]));
        var inputs = In("""{"body":{"list":[1,2]}}""", mocks, new() { ["p"] = J("""{"k":[1]}""") }, "2018-04-15T13:00:00Z", "g", """{"name":"w"}""");
        string Snapshot() => string.Join("|", definition.ToJsonString(), inputs.TriggerOutputs!.ToJsonString(), string.Join(",", mocks["Call"].Select(m => m.Outputs!.ToJsonString())), inputs.Parameters["p"]!.ToJsonString(), inputs.Workflow!.ToJsonString());
        var before = Snapshot();

        var run = FlowSimulator.Run(definition, inputs);
        Assert.Equal("Succeeded", run.Status);
        run.Response!["body"]!["list"] = "changed";
        run.Variables["arr"]!.AsArray().Clear();

        var after = Snapshot();
        Assert.Equal(before, after);
    }
}