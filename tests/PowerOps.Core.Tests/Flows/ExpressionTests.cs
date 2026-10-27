using System.Globalization;
using System.Text.Json.Nodes;

using PowerOps.Core.Flows;

namespace PowerOps.Core.Tests;

public class ExpressionTests
{
    /// <summary>A fixed context; the nodes are shared across calls on purpose, so a function that re-parented one would throw.</summary>
    private sealed class FakeContext : IExpressionContext
    {
        private static readonly JsonNode GetUser = JsonNode.Parse("""
            {"statusCode":200,"headers":{"Content-Type":"application/json"},
             "body":{"FullName":"Contoso Corporation","Id":283541717,"UserName":"ContosoInc"}}
            """)!;

        private static readonly JsonNode Trigger = JsonNode.Parse("""
            {"headers":{"x":"1"},"body":{"value":[1,2],"text":"hi","empty":null}}
            """)!;

        private static readonly JsonNode Loop = JsonNode.Parse("""{"id":7}""")!;
        private static readonly JsonNode Current = JsonNode.Parse("""{"body":"hello"}""")!;

        private static readonly JsonNode Flow = JsonNode.Parse("""
            {"name":"flow1","run":{"name":"08585"},"tags":{"environmentName":"Dev","flowDisplayName":"My flow"}}
            """)!;

        private static readonly JsonNode Scope = JsonNode.Parse("""[{"name":"HTTP","status":"Succeeded"}]""")!;

        public JsonNode? Outputs(string action) => action switch
        {
            "Get_user" => GetUser,
            "No_body" => JsonNode.Parse("""{"statusCode":204}"""),
            _ => throw new ExpressionException($"No outputs for '{action}'."),
        };

        public JsonNode? TriggerOutputs() => Trigger;

        public JsonNode? Variable(string name) => name switch
        {
            "numItems" => JsonValue.Create(20),
            "nothing" => null,
            _ => throw new ExpressionException($"No variable '{name}'."),
        };

        public JsonNode? Parameter(string name) => name == "fullName" ? JsonValue.Create("Sophia Owen") : throw new UnmockedException($"Set parameter '{name}'.");

        public JsonNode? Items(string loop) => loop == "myForEachLoopName" ? Loop : throw new ExpressionException($"No loop '{loop}'.");

        public JsonNode? Item() => Current;

        public JsonNode? Workflow() => Flow;

        public JsonNode? Result(string scope) => scope == "For_each" ? Scope : throw new ExpressionException($"No scope '{scope}'.");

        public string UtcNow() => "2018-04-15T13:00:00Z";

        public string Guid() => "c2ecc88d-88c8-4096-912c-d6f2e2b138ce";
    }

    private static readonly FakeContext Ctx = new();

    private static JsonNode? Eval(string expression) => Expression.Evaluate("@" + expression, Ctx);

    private static void AssertJson(string expectedJson, JsonNode? actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expectedJson), actual), $"expected {expectedJson}, got {actual?.ToJsonString() ?? "null"}");

    private static void AssertEval(string expression, string expectedJson) => AssertJson(expectedJson, Eval(expression));

    // ---- text: @, @@, @{...}

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    [InlineData("@@", "@")]
    [InlineData("@@x", "@x")]
    [InlineData("a@@b", "a@b")] // not on Learn; plan "Not on Learn"
    [InlineData("a@b", "a@b")]
    [InlineData("end@", "end@")]
    [InlineData(" @", " @")]
    [InlineData("My age is @@{parameters('myAge')}", "My age is @{parameters('myAge')}")]
    [InlineData("My age is @{variables('numItems')}", "My age is 20")]
    [InlineData("@{concat('a','b')}-@{concat('c','d')}", "ab-cd")]
    [InlineData("@{concat('}', 'x')}", "}x")]
    [InlineData("@{concat('it''s', '}')}!", "it's}!")]
    [InlineData("@{variables('numItems')}", "20")]
    [InlineData("@{triggerOutputs()['body']['value']}", "[1,2]")]
    [InlineData("@{true}", "True")] // not on Learn; plan "Not on Learn"
    [InlineData("@{false}", "False")]
    [InlineData("@{null}", "")]
    [InlineData("@{1.5}", "1.5")]
    [InlineData("@{ concat('a', 'b') }", "ab")]
    public void Text_is_interpolated(string text, string expected)
    {
        var result = Expression.Evaluate(text, Ctx);

        Assert.Equal(expected, Assert.IsAssignableFrom<JsonValue>(result).GetValue<string>());
    }

    [Fact]
    public void Text_with_unterminated_interpolation_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Expression.Evaluate("a @{concat('a', 'b'", Ctx));

    [Fact]
    public void Interpolation_with_unreadable_expression_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Expression.Evaluate("a @{not a function}", Ctx));

    [Fact]
    public void Lone_at_sign_is_an_empty_expression_and_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Expression.Evaluate("@", Ctx));

    [Fact]
    public void Expression_keeps_the_type_of_its_value()
    {
        AssertJson("20", Expression.Evaluate("@variables('numItems')", Ctx));
        AssertJson("""{"id":7}""", Expression.Evaluate("@items('myForEachLoopName')", Ctx));
        AssertJson("true", Expression.Evaluate("@equals(1, 1)", Ctx));
    }

    [Fact]
    public void Result_can_be_added_to_another_node()
    {
        var result = Expression.Evaluate("@items('myForEachLoopName')", Ctx);

        var holder = new JsonObject { ["x"] = result };

        Assert.Equal("""{"x":{"id":7}}""", holder.ToJsonString());
    }

    // ---- literals, whitespace, nesting

    [Theory]
    [InlineData("'it''s'", "\"it's\"")] // not on Learn; plan "Not on Learn"
    [InlineData("'a b'", "\"a b\"")]
    [InlineData("''", "\"\"")]
    [InlineData("42", "42")]
    [InlineData("-7", "-7")]
    [InlineData("1.5", "1.5")]
    [InlineData("-0.25", "-0.25")]
    [InlineData(".5", "0.5")] // Learn's sub() example writes .3
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("null", "null")]
    [InlineData("  concat( 'a' ,\t'b'  )  ", "\"ab\"")]
    [InlineData("concat(toLower('A'), trim(' b '))", "\"ab\"")]
    [InlineData("concat(concat('a', concat('b', 'c')), 'd')", "\"abcd\"")]
    public void Literals_and_whitespace(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Nesting_up_to_100_calls_works_and_deeper_is_unsupported()
    {
        static string Nest(int n) => string.Concat(Enumerable.Repeat("not(", n)) + "true" + new string(')', n);

        AssertEval(Nest(100), "true"); // 100 nots of true
        var ex = Assert.Throws<UnsupportedException>(() => Eval(Nest(101)));
        Assert.Contains("100", ex.Message);
    }

    [Fact]
    public void Long_accessor_chain_is_bounded_like_nesting()
    {
        var chain = "triggerOutputs()" + string.Concat(Enumerable.Repeat("?['a']", 101));

        Assert.Throws<UnsupportedException>(() => Eval(chain));
    }

    [Theory]
    [InlineData("concat('a'")]
    [InlineData("'unterminated")]
    [InlineData("1 2")]
    [InlineData("foo")]
    [InlineData("concat('a',)")]
    [InlineData("concat('a') junk")]
    [InlineData("triggerOutputs()[")]
    [InlineData("triggerOutputs()['a'")]
    [InlineData("triggerOutputs()?")]
    [InlineData("triggerOutputs().")]
    [InlineData(")")]
    [InlineData("1.")]
    [InlineData("-")]
    [InlineData("#")]
    public void Unreadable_text_is_unsupported_and_quotes_the_expression(string expression)
    {
        var ex = Assert.Throws<UnsupportedException>(() => Eval(expression));

        Assert.Contains(expression, ex.Message);
    }

    [Fact]
    public void Unknown_function_is_unsupported_and_named()
    {
        var ex = Assert.Throws<UnsupportedException>(() => Eval("xml('<a/>')"));
        Assert.Contains("xml", ex.Message);

        ex = Assert.Throws<UnsupportedException>(() => Eval("json(xml('<a/>'))"));
        Assert.Contains("xml", ex.Message);
    }

    [Fact]
    public void Function_names_are_case_sensitive() =>
        Assert.Throws<UnsupportedException>(() => Eval("Concat('a', 'b')"));

    // ---- accessors

    [Theory]
    [InlineData("triggerOutputs().body.text", "\"hi\"")]
    [InlineData("triggerOutputs()['body']['text']", "\"hi\"")]
    [InlineData("triggerOutputs()?['body']?['text']", "\"hi\"")]
    [InlineData("triggerOutputs()?.body?.text", "\"hi\"")] // Learn's WDL page shows trigger().outputs?.body
    [InlineData("triggerOutputs().body.value[0]", "1")]
    [InlineData("triggerOutputs()['body']['value'][json('1')]", "2")]
    [InlineData("triggerOutputs()['body'][concat('te', 'xt')]", "\"hi\"")]
    [InlineData("triggerOutputs()['body']['value']?[1]", "2")]
    [InlineData("triggerOutputs()  [ 'body' ] . text", "\"hi\"")]
    [InlineData("triggerOutputs()['body']['empty']", "null")]
    [InlineData("triggerOutputs()['body']['empty']?['x']", "null")]
    [InlineData("triggerOutputs()['body']?['nope']", "null")]
    [InlineData("triggerOutputs()?['body']['value']?[5]", "null")]
    [InlineData("triggerOutputs()?['BODY']", "null")] // not on Learn; plan "Not on Learn"
    [InlineData("variables('nothing')?['x']", "null")]
    public void Property_and_index_access(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("triggerOutputs()['body']['empty']['x']")]
    [InlineData("variables('nothing')['x']")]
    [InlineData("triggerOutputs()['nope']")]
    [InlineData("triggerOutputs().nope")]
    [InlineData("triggerOutputs()['body']['value'][5]")]
    [InlineData("triggerOutputs()['body']['value'][-1]")]
    [InlineData("triggerOutputs()['BODY']")] // not on Learn; plan "Not on Learn"
    [InlineData("triggerOutputs()['body']['text'][0]")]
    [InlineData("triggerOutputs()['body']['value']['x']")]
    [InlineData("triggerOutputs()['body'][0]")]
    public void Access_error_without_question_mark(string expression) =>
        Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("triggerOutputs()?['body/value']", "[1,2]")] // not on Learn; plan "Not on Learn"
    [InlineData("triggerOutputs()?['body/text']", "\"hi\"")]
    [InlineData("triggerOutputs()?['body/nope/x']", "null")]
    [InlineData("triggerOutputs()?['body/empty/x']", "null")]
    [InlineData("triggerOutputs()['body/value']", "[1,2]")]
    [InlineData("json('{\"a/b\":1,\"a\":{\"b\":2}}')['a/b']", "1")] // a property of that exact name wins
    [InlineData("json('{\"a\":{\"b\":2}}')['a/b']", "2")]
    public void Slash_path_reads_one_segment_at_a_time(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("triggerOutputs()['body/nope']")]
    [InlineData("triggerOutputs()['body/empty/x']")]
    [InlineData("triggerOutputs()['nope/x']")]
    public void Slash_path_errors_without_question_mark(string expression) =>
        Assert.Throws<ExpressionException>(() => Eval(expression));

    // ---- workflow functions (against the fake context)

    // outputs: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#outputs
    [Theory]
    [InlineData("outputs('Get_user')['statusCode']", "200")]
    [InlineData("outputs('Get_user')['body']['UserName']", "\"ContosoInc\"")]
    public void Outputs(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Outputs_returns_the_whole_object()
    {
        var result = Eval("outputs('Get_user')");

        Assert.Equal(200, result!["statusCode"]!.GetValue<int>());
        Assert.Equal("Contoso Corporation", result["body"]!["FullName"]!.GetValue<string>());
    }

    // body: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#body
    [Theory]
    [InlineData("body('Get_user')['FullName']", "\"Contoso Corporation\"")]
    [InlineData("body('Get_user').Id", "283541717")]
    public void Body(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Body_is_outputs_body_and_errors_when_there_is_none()
    {
        AssertEval("body('Get_user')", """{"FullName":"Contoso Corporation","Id":283541717,"UserName":"ContosoInc"}""");
        Assert.Throws<ExpressionException>(() => Eval("body('No_body')"));
        Assert.Throws<ExpressionException>(() => Eval("body('Unknown')"));
    }

    // triggerOutputs: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#triggerOutputs (no example)
    [Fact]
    public void TriggerOutputs_returns_the_trigger_output() =>
        AssertEval("triggerOutputs()", """{"headers":{"x":"1"},"body":{"value":[1,2],"text":"hi","empty":null}}""");

    // triggerBody: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#triggerBody (no example)
    [Fact]
    public void TriggerBody_is_the_body_of_the_trigger_output() =>
        AssertEval("triggerBody()", """{"value":[1,2],"text":"hi","empty":null}""");

    // variables: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#variables
    [Fact]
    public void Variables() => AssertEval("variables('numItems')", "20");

    // parameters: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#parameters
    [Fact]
    public void Parameters() => AssertEval("parameters('fullName')", "\"Sophia Owen\"");

    [Fact]
    public void Parameters_of_an_unset_parameter_is_what_the_context_throws() =>
        Assert.Throws<UnmockedException>(() => Eval("parameters('other')"));

    // items: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#items
    [Fact]
    public void Items() => AssertEval("items('myForEachLoopName')", """{"id":7}""");

    // item: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#item
    [Fact]
    public void Item() => AssertEval("item().body", "\"hello\"");

    // workflow: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#workflow
    [Theory]
    [InlineData("workflow().run.name", "\"08585\"")]
    [InlineData("workflow()['tags']['environmentName']", "\"Dev\"")]
    [InlineData("workflow()['name']", "\"flow1\"")]
    public void Workflow(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    // result: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#result
    [Fact]
    public void Result()
    {
        AssertEval("result('For_each')", """[{"name":"HTTP","status":"Succeeded"}]""");
        AssertEval("result('For_each')[0].status", "\"Succeeded\"");
    }

    // ---- string functions

    // concat: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#concat
    [Theory]
    [InlineData("concat('Hello', 'World')", "\"HelloWorld\"")]
    [InlineData("concat('a', 'b', 'c')", "\"abc\"")]
    [InlineData("concat('n=', 5, true, null, 1.5)", "\"n=5True1.5\"")] // text form; not on Learn, plan "Not on Learn"
    [InlineData("concat('o=', triggerOutputs()['body']['value'])", "\"o=[1,2]\"")]
    public void Concat(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("concat('a')")] // Learn: at least two strings
    [InlineData("concat()")]
    public void Concat_needs_two_arguments(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // substring: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#substring
    [Theory]
    [InlineData("substring('hello world', 6, 5)", "\"world\"")]
    [InlineData("substring('hello world', 6)", "\"world\"")]
    [InlineData("substring('hello', 0, 5)", "\"hello\"")] // start + length may equal the length
    [InlineData("substring('hello', 5)", "\"\"")]
    [InlineData("substring('hello', 2, 0)", "\"\"")]
    public void Substring(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("substring('hello', 3, 5)")] // Learn: past the end is an error
    [InlineData("substring('hello', 6)")]
    [InlineData("substring('hello', 6, 0)")]
    [InlineData("substring('hello', -1, 2)")]
    [InlineData("substring('hello', 1, -1)")]
    [InlineData("substring('hello', '1', 2)")]
    [InlineData("substring(5, 0, 1)")]
    [InlineData("substring('hello')")]
    [InlineData("substring('abc', 9223372036854775807, 1)")] // start + length wraps around
    [InlineData("substring('abc', 1, 9223372036854775807)")]
    [InlineData("substring('abc', 9223372036854775807)")]
    [InlineData("substring('abc', -9223372036854775808)")]
    public void Substring_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // replace: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#replace
    [Theory]
    [InlineData("replace('the old string', 'old', 'new')", "\"the new string\"")]
    [InlineData("replace('the Old string', 'old', 'new')", "\"the Old string\"")] // Learn: case-sensitive
    [InlineData("replace('abc', 'x', 'y')", "\"abc\"")] // Learn: substring not found returns the original
    [InlineData("replace('aaa', 'a', 'bb')", "\"bbbbbb\"")]
    public void Replace(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("replace('abc', '', 'y')")]
    [InlineData("replace('abc', 'a')")]
    [InlineData("replace('abc', 1, 'y')")]
    public void Replace_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // split: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#split
    [Theory]
    [InlineData("split('a_b_c', '_')", """["a","b","c"]""")]
    [InlineData("split('a_b_c', ' ')", """["a_b_c"]""")]
    [InlineData("split('a,,b', ',')", """["a","","b"]""")]
    [InlineData("split('', ',')", """[""]""")]
    public void Split(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Split_of_a_non_string_is_an_error() => Assert.Throws<ExpressionException>(() => Eval("split(5, ',')"));

    // toLower: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#toLower
    [Theory]
    [InlineData("toLower('Hello World')", "\"hello world\"")]
    [InlineData("toLower('123 !')", "\"123 !\"")]
    public void ToLower(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void ToLower_of_a_non_string_is_an_error() => Assert.Throws<ExpressionException>(() => Eval("toLower(5)"));

    // trim: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#trim
    [Theory]
    [InlineData("trim(' Hello World  ')", "\"Hello World\"")]
    [InlineData("trim('  ')", "\"\"")]
    public void Trim(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    // guid: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#guid
    [Fact]
    public void Guid_is_the_fixed_value_of_the_context() => AssertEval("guid()", "\"c2ecc88d-88c8-4096-912c-d6f2e2b138ce\"");

    [Theory]
    [InlineData("guid('P')")] // Learn's example; the format argument is not supported
    [InlineData("guid('N')")]
    public void Guid_with_a_format_is_unsupported(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // ---- collection functions

    // contains: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#contains
    [Theory]
    [InlineData("contains('hello world', 'world')", "true")]
    [InlineData("contains('hello world', 'universe')", "false")]
    [InlineData("contains('hello world', 'World')", "false")] // Learn: case-sensitive
    [InlineData("contains(createArray('a', 'b'), 'b')", "true")]
    [InlineData("contains(createArray('a', 'b'), 'B')", "false")]
    [InlineData("contains(createArray(1, 2), 2)", "true")]
    [InlineData("contains(createArray(1, 2), 3)", "false")]
    [InlineData("contains(triggerOutputs()['body'], 'text')", "true")]
    [InlineData("contains(triggerOutputs()['body'], 'Text')", "false")]
    [InlineData("contains(triggerOutputs()['body'], 'empty')", "true")] // a key whose value is null is still a key
    public void Contains(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("contains(5, 'a')")]
    [InlineData("contains('abc', 1)")]
    [InlineData("contains(triggerOutputs()['body'], 1)")]
    [InlineData("contains('abc')")]
    public void Contains_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // empty: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#empty
    [Theory]
    [InlineData("empty('')", "true")]
    [InlineData("empty('abc')", "false")]
    [InlineData("empty(createArray('a', 'b'))", "false")]
    [InlineData("empty(json('[]'))", "true")]
    [InlineData("empty(json('{}'))", "true")]
    [InlineData("empty(triggerOutputs()['body'])", "false")]
    [InlineData("empty(null)", "true")] // not on Learn; plan "Not on Learn" (null is the empty string in comparisons)
    public void Empty(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Empty_of_a_number_is_an_error() => Assert.Throws<ExpressionException>(() => Eval("empty(5)"));

    // first: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#first
    [Theory]
    [InlineData("first('hello')", "\"h\"")]
    [InlineData("first(createArray(0, 1, 2))", "0")]
    [InlineData("first('')", "null")] // not on Learn; plan "Not on Learn"
    [InlineData("first(json('[]'))", "null")] // not on Learn; plan "Not on Learn"
    public void First(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void First_of_a_number_is_an_error() => Assert.Throws<ExpressionException>(() => Eval("first(5)"));

    // last: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#last
    [Theory]
    [InlineData("last('abcd')", "\"d\"")]
    [InlineData("last(createArray(0, 1, 2, 3))", "3")]
    [InlineData("last('')", "null")] // not on Learn; plan "Not on Learn"
    [InlineData("last(json('[]'))", "null")] // not on Learn; plan "Not on Learn"
    public void Last(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Last_of_a_number_is_an_error() => Assert.Throws<ExpressionException>(() => Eval("last(5)"));

    // length: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#length
    [Theory]
    [InlineData("length('abcd')", "4")]
    [InlineData("length(createArray(0, 1, 2, 3))", "4")]
    [InlineData("length('')", "0")]
    public void Length(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("length(5)")]
    [InlineData("length(null)")]
    [InlineData("length(triggerOutputs()['body'])")] // Learn: a string or an array
    public void Length_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // ---- logical functions

    // equals: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#equals
    [Theory]
    [InlineData("equals(true, 1)", "true")]
    [InlineData("equals('abc', 'abcd')", "false")]
    [InlineData("equals('abc', 'abc')", "true")]
    [InlineData("equals('abc', 'ABC')", "false")] // not on Learn; plan "Not on Learn" (ordinal)
    [InlineData("equals(1, 1.0)", "true")] // numbers by value
    [InlineData("equals(1, 2)", "false")]
    [InlineData("equals(false, 0)", "true")] // not on Learn; plan "Not on Learn"
    [InlineData("equals(true, 0)", "false")]
    [InlineData("equals(true, true)", "true")]
    [InlineData("equals(true, false)", "false")]
    [InlineData("equals(null, '')", "true")] // Learn's logical-functions intro: null is converted to ""
    [InlineData("equals(null, null)", "true")]
    [InlineData("equals(null, 'a')", "false")]
    [InlineData("equals('1', 1)", "false")]
    [InlineData("equals(createArray(1, 2), json('[1,2]'))", "true")]
    [InlineData("equals(createArray(1, 2), json('[2,1]'))", "false")]
    [InlineData("equals(triggerOutputs()['body']['text'], 'hi')", "true")]
    public void Equals_(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Equals_needs_two_arguments() => Assert.Throws<ExpressionException>(() => Eval("equals(1)"));

    // greater: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#greater
    [Theory]
    [InlineData("greater(10, 5)", "true")]
    [InlineData("greater('apple', 'banana')", "false")]
    [InlineData("greater('b', 'a')", "true")]
    [InlineData("greater('B', 'a')", "false")] // not on Learn; plan "Not on Learn" (ordinal: 'B' < 'a')
    [InlineData("greater(1.5, 1)", "true")]
    [InlineData("greater(5, 5)", "false")]
    public void Greater(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    // greaterOrEquals: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#greaterOrEquals
    [Theory]
    [InlineData("greaterOrEquals(5, 5)", "true")]
    [InlineData("greaterOrEquals('apple', 'banana')", "false")]
    [InlineData("greaterOrEquals(6, 5)", "true")]
    [InlineData("greaterOrEquals(4, 5)", "false")]
    public void GreaterOrEquals(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    // lessOrEquals: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#lessOrEquals
    [Theory]
    [InlineData("lessOrEquals(10, 10)", "true")]
    [InlineData("lessOrEquals('apply', 'apple')", "false")]
    [InlineData("lessOrEquals(4, 5)", "true")]
    [InlineData("lessOrEquals(6, 5)", "false")]
    public void LessOrEquals(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("greater(1, 'a')")]
    [InlineData("greater(true, false)")]
    [InlineData("lessOrEquals(1)")]
    [InlineData("greaterOrEquals(createArray(1), createArray(2))")]
    public void Comparisons_of_mixed_or_unordered_types_are_errors(string expression) =>
        Assert.Throws<ExpressionException>(() => Eval(expression));

    // if: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#if
    [Theory]
    [InlineData("if(equals(1, 1), 'yes', 'no')", "\"yes\"")]
    [InlineData("if(equals(1, 2), 'yes', 'no')", "\"no\"")]
    [InlineData("if(true, createArray(1), null)", "[1]")]
    [InlineData("if(false, 1, null)", "null")]
    public void If(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("if('true', 1, 2)")]
    [InlineData("if(1, 1, 2)")]
    [InlineData("if(true, 1)")]
    public void If_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Fact]
    public void Arguments_are_all_evaluated_eagerly_including_the_untaken_branch()
    {
        // not on Learn; plan "Not on Learn": the untaken branch is still evaluated, so it can fail the expression
        Assert.Throws<ExpressionException>(() => Eval("if(true, 'ok', triggerOutputs()['nope'])"));
        Assert.Throws<ExpressionException>(() => Eval("or(true, triggerOutputs()['nope'])"));
        Assert.Throws<ExpressionException>(() => Eval("and(false, triggerOutputs()['nope'])"));
        Assert.Throws<UnmockedException>(() => Eval("if(true, 'ok', parameters('other'))"));
    }

    [Fact]
    public void Arguments_are_evaluated_left_to_right()
    {
        // the first failing argument is the one reported
        var ex = Assert.Throws<UnmockedException>(() => Eval("concat(parameters('first'), outputs('Unknown'))"));

        Assert.Contains("first", ex.Message);
    }

    // and: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#and
    [Theory]
    [InlineData("and(true, true)", "true")]
    [InlineData("and(false, true)", "false")]
    [InlineData("and(false, false)", "false")]
    [InlineData("and(equals(1, 1), equals(2, 2))", "true")]
    [InlineData("and(equals(1, 1), equals(1, 2))", "false")]
    [InlineData("and(equals(1, 2), equals(1, 3))", "false")]
    [InlineData("and(true, true, true)", "true")]
    [InlineData("and(true, true, false)", "false")]
    public void And(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    // or: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#or
    [Theory]
    [InlineData("or(true, false)", "true")]
    [InlineData("or(false, false)", "false")]
    [InlineData("or(equals(1, 1), equals(1, 2))", "true")]
    [InlineData("or(equals(1, 2), equals(1, 3))", "false")]
    [InlineData("or(false, false, true)", "true")]
    public void Or(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("and(true, 'x')")]
    [InlineData("or(false, 1)")]
    [InlineData("and()")]
    [InlineData("or()")]
    public void And_and_or_need_booleans(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // not: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#not
    [Theory]
    [InlineData("not(false)", "true")]
    [InlineData("not(true)", "false")]
    [InlineData("not(equals(1, 2))", "true")]
    [InlineData("not(equals(1, 1))", "false")]
    public void Not(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("not('x')")]
    [InlineData("not(true, false)")]
    [InlineData("not()")]
    public void Not_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // ---- conversion functions

    // json: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#json
    [Theory]
    [InlineData("json('[1, 2, 3]')", "[1,2,3]")]
    [InlineData("json('{\"fullName\": \"Sophia Owen\"}')", """{"fullName":"Sophia Owen"}""")]
    [InlineData("json('{\"fullName\": \"Sophia Owen\"}')['fullName']", "\"Sophia Owen\"")]
    [InlineData("json('5')", "5")]
    [InlineData("json(null)", "{}")] // Learn: a null string gives an empty object
    public void Json(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("json('not json')")]
    [InlineData("json('')")]
    [InlineData("json(5)")]
    [InlineData("json('{')")]
    public void Json_of_invalid_text_is_an_error(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Fact]
    public void Json_of_xml_is_unsupported_because_xml_is() // Learn's examples 3 and 4 use xml()
    {
        var ex = Assert.Throws<UnsupportedException>(() => Eval("json(xml('<root><person id=\"1\"/></root>'))"));

        Assert.Contains("xml", ex.Message);
    }

    // string: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#string
    [Theory]
    [InlineData("string(10)", "\"10\"")]
    [InlineData("string(1.5)", "\"1.5\"")]
    [InlineData("string(true)", "\"True\"")] // not on Learn; plan "Not on Learn"
    [InlineData("string(false)", "\"False\"")]
    [InlineData("string(null)", "\"\"")] // Learn: null becomes an empty string
    [InlineData("string('abc')", "\"abc\"")]
    [InlineData("string(createArray(1, 'a'))", "\"[1,\\\"a\\\"]\"")]
    public void String(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void String_of_an_object_is_compact_json()
    {
        // Learn's example passes an object literal, which the expression language has no syntax for; json() builds the same object
        var result = Eval("string(json('{\"name\":\"Sophie Owen\"}'))");

        Assert.Equal("""{"name":"Sophie Owen"}""", result!.GetValue<string>());
    }

    [Fact]
    public void String_needs_one_argument() => Assert.Throws<ExpressionException>(() => Eval("string(1, 2)"));

    // decodeBase64 (deprecated; from base64ToString): https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#base64ToString
    [Theory]
    [InlineData("decodeBase64('aGVsbG8=')", "\"hello\"")]
    [InlineData("decodeBase64('')", "\"\"")]
    [InlineData("decodeBase64('w6k=')", "\"é\"")]
    public void DecodeBase64(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("decodeBase64('%%%')")]
    [InlineData("decodeBase64(5)")]
    [InlineData("decodeBase64('/w==')")] // 0xFF is not valid UTF-8
    public void DecodeBase64_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // createArray: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#createArray
    [Theory]
    [InlineData("createArray('h', 'e', 'l', 'l', 'o')", """["h","e","l","l","o"]""")]
    [InlineData("createArray(1, 2)", "[1,2]")]
    [InlineData("createArray('1')", """["1"]""")] // Learn says at least two, but an exported fixture flow calls it with one
    [InlineData("createArray(outputs('Get_user')['statusCode'], null)", "[200,null]")]
    public void CreateArray(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void CreateArray_copies_its_arguments_and_needs_one()
    {
        AssertEval("createArray(items('myForEachLoopName'), items('myForEachLoopName'))", """[{"id":7},{"id":7}]""");
        Assert.Throws<ExpressionException>(() => Eval("createArray()"));
    }

    // ---- math, date and time

    // min: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#min
    [Theory]
    [InlineData("min(1, 2, 3)", "1")]
    [InlineData("min(createArray(1, 2, 3))", "1")]
    [InlineData("min(3, 1.5)", "1.5")]
    [InlineData("min(createArray(2, -4.5, 9))", "-4.5")]
    [InlineData("min(2, 1)", "1")]
    public void Min(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("min(1)")]
    [InlineData("min()")]
    [InlineData("min(json('[]'))")]
    [InlineData("min('a', 'b')")]
    [InlineData("min(createArray(1, 'a'))")]
    [InlineData("min(createArray(1), 2)")]
    public void Min_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    // utcNow: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#utcNow
    [Fact]
    public void UtcNow_is_the_fixed_timestamp_in_the_o_format() => AssertEval("utcNow()", "\"2018-04-15T13:00:00.0000000Z\"");

    [Theory]
    [InlineData("utcNow('u')")] // Learn's example; the format argument is not supported
    [InlineData("utcNow('yyyy')")]
    public void UtcNow_with_a_format_is_unsupported(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // addDays: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#addDays
    [Theory]
    [InlineData("addDays('2018-03-15T00:00:00Z', 10)", "\"2018-03-25T00:00:00.0000000Z\"")]
    [InlineData("addDays('2018-03-15T00:00:00Z', -5)", "\"2018-03-10T00:00:00.0000000Z\"")]
    [InlineData("addDays(utcNow(), 1)", "\"2018-04-16T13:00:00.0000000Z\"")]
    [InlineData("addDays('2018-03-15T10:30:00+02:00', 0)", "\"2018-03-15T08:30:00.0000000Z\"")]
    [InlineData("addDays('2018-03-15T00:00:00.1234567Z', 1)", "\"2018-03-16T00:00:00.1234567Z\"")]
    [InlineData("addDays('2018-03-15', 1)", "\"2018-03-16T00:00:00.0000000Z\"")] // read as UTC
    public void AddDays(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("addDays('2018-03-15T00:00:00Z', 'x')")]
    [InlineData("addDays('not a date', 1)")]
    [InlineData("addDays('2018-03-15T00:00:00Z', 1.5)")]
    [InlineData("addDays('2018-03-15T00:00:00Z', '1')")]
    [InlineData("addDays(5, 1)")]
    [InlineData("addDays('2018-03-15T00:00:00Z')")]
    [InlineData("addDays('9999-12-31T00:00:00Z', 1)")]
    public void AddDays_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Fact]
    public void AddDays_with_a_format_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Eval("addDays('2018-03-15T00:00:00Z', 1, 'yyyy')"));

    // ---- the list of functions

    [Fact]
    public void Names_are_exactly_the_37_functions_sorted()
    {
        string[] expected =
        [
            "addDays", "and", "body", "concat", "contains", "createArray", "decodeBase64", "empty", "equals", "first",
            "greater", "greaterOrEquals", "guid", "if", "item", "items", "json", "last", "length", "lessOrEquals",
            "min", "not", "or", "outputs", "parameters", "replace", "result", "split", "string", "substring",
            "toLower", "triggerBody", "triggerOutputs", "trim", "utcNow", "variables", "workflow",
        ];

        Assert.Equal(37, expected.Length);
        Assert.Equal(expected, ExpressionFunctions.Names);
        Assert.Equal(ExpressionFunctions.Names.Order(StringComparer.Ordinal), ExpressionFunctions.Names);
    }

    [Fact]
    public void Every_listed_name_is_known_to_the_evaluator()
    {
        foreach (var name in ExpressionFunctions.Names)
        {
            // an argument-count error is fine here; an unknown-function error is not
            try
            {
                Eval(name + "()");
            }
            catch (ExpressionException)
            {
            }
            catch (UnmockedException)
            {
            }
        }
    }

    // ---- EvaluateValue

    [Fact]
    public void EvaluateValue_evaluates_every_string_in_nested_values_and_leaves_keys_alone()
    {
        var source = JsonNode.Parse("""
            {"@keep":"@concat('a','b')","n":1,"b":true,"z":null,
             "list":["@variables('numItems')","plain",["@{variables('numItems')}!"],{"k":"@@x"}],
             "deep":{"v":"@items('myForEachLoopName')"}}
            """)!;
        var before = source.ToJsonString();

        var result = Expression.EvaluateValue(source, Ctx);

        AssertJson("""
            {"@keep":"ab","n":1,"b":true,"z":null,
             "list":[20,"plain",["20!"],{"k":"@x"}],
             "deep":{"v":{"id":7}}}
            """, result);
        Assert.Equal(before, source.ToJsonString());
        Assert.Null(result!.Parent);
        Assert.NotSame(source, result);
    }

    [Fact]
    public void EvaluateValue_of_scalars()
    {
        Assert.Null(Expression.EvaluateValue(null, Ctx));
        AssertJson("5", Expression.EvaluateValue(JsonValue.Create(5), Ctx));
        AssertJson("\"x\"", Expression.EvaluateValue(JsonValue.Create("x"), Ctx));
        AssertJson("true", Expression.EvaluateValue(JsonValue.Create("@equals(1, 1)"), Ctx));
    }

    [Fact]
    public void EvaluateValue_does_not_reparent_values_taken_from_the_source_or_the_context()
    {
        var source = JsonNode.Parse("""{"a":"@outputs('Get_user')","b":"@outputs('Get_user')"}""")!;

        var result = Expression.EvaluateValue(source, Ctx);

        Assert.Equal(200, result!["a"]!["statusCode"]!.GetValue<int>());
        Assert.Equal(200, result["b"]!["statusCode"]!.GetValue<int>());
    }

    // ---- EvaluateCondition

    [Fact]
    public void EvaluateCondition_of_a_string_is_an_expression() =>
        AssertJson("true", Expression.EvaluateCondition(JsonValue.Create("@equals(1, 1)"), Ctx));

    [Fact]
    public void EvaluateCondition_of_other_values_is_the_value() =>
        AssertJson("true", Expression.EvaluateCondition(JsonValue.Create(true), Ctx));

    [Theory]
    [InlineData("""{"equals":["@variables('numItems')",20]}""", "true")]
    [InlineData("""{"equals":["@variables('numItems')",21]}""", "false")]
    [InlineData("""{"greater":["@variables('numItems')",5]}""", "true")]
    [InlineData("""{"greater":[5,"@variables('numItems')"]}""", "false")]
    [InlineData("""{"lessOrEquals":["@variables('numItems')",20]}""", "true")]
    [InlineData("""{"contains":["@triggerBody()?['text']","h"]}""", "true")]
    [InlineData("""{"contains":["hello world","world"]}""", "true")]
    [InlineData("""{"not":{"equals":[1,2]}}""", "true")]
    [InlineData("""{"not":[{"equals":[1,1]}]}""", "false")]
    [InlineData("""{"and":[{"equals":[1,1]},{"greater":[2,1]}]}""", "true")]
    [InlineData("""{"and":[{"equals":[1,1]},{"greater":[1,2]}]}""", "false")]
    [InlineData("""{"or":[{"equals":[1,2]},{"lessOrEquals":[1,1]}]}""", "true")]
    [InlineData("""{"or":[{"equals":[1,2]},{"lessOrEquals":[2,1]}]}""", "false")]
    [InlineData("""{"and":[{"or":[{"equals":[1,2]},{"not":{"equals":[1,2]}}]},{"contains":["abc","b"]}]}""", "true")]
    [InlineData("""{"and":["@equals(1, 1)",true]}""", "true")]
    [InlineData("""{"equals":["@{variables('numItems')}","20"]}""", "true")]
    public void EvaluateCondition_object_form(string condition, string expectedJson) =>
        AssertJson(expectedJson, Expression.EvaluateCondition(JsonNode.Parse(condition), Ctx));

    [Fact]
    public void EvaluateCondition_array_literal_argument_is_a_literal()
    {
        var condition = JsonNode.Parse("""{"contains":[[1,2,3],2]}""");

        AssertJson("true", Expression.EvaluateCondition(condition, Ctx));
    }

    [Fact]
    public void EvaluateCondition_evaluates_the_items_of_an_array_argument()
    {
        var condition = JsonNode.Parse("""{"contains":[["@variables('numItems')","b"],20]}""");

        AssertJson("true", Expression.EvaluateCondition(condition, Ctx));
    }

    [Fact]
    public void EvaluateCondition_evaluates_an_object_argument_with_several_properties_property_by_property()
    {
        var condition = JsonNode.Parse("""{"equals":[{"a":"@variables('numItems')","b":1},{"a":20,"b":1}]}""");

        AssertJson("true", Expression.EvaluateCondition(condition, Ctx));
    }

    [Fact]
    public void A_replace_that_would_give_a_string_over_the_concat_limit_is_unsupported()
    {
        var a = new string('a', 30_000);

        var ex = Assert.Throws<UnsupportedException>(() => Expression.Evaluate($"@replace('{a}', 'a', '{a}')", Ctx));

        Assert.Contains("104857600", ex.Message);
    }

    [Fact]
    public void EvaluateCondition_with_an_unknown_function_is_unsupported()
    {
        var ex = Assert.Throws<UnsupportedException>(() => Expression.EvaluateCondition(JsonNode.Parse("""{"startsWith":["abc","a"]}"""), Ctx));

        Assert.Contains("startsWith", ex.Message);
        Assert.Throws<UnsupportedException>(() => Expression.EvaluateCondition(JsonNode.Parse("""{"and":[{"nope":[1]}]}"""), Ctx));
    }

    [Fact]
    public void EvaluateCondition_object_needs_exactly_one_function() =>
        Assert.Throws<ExpressionException>(() => Expression.EvaluateCondition(JsonNode.Parse("""{"equals":[1,1],"not":true}"""), Ctx));

    [Fact]
    public void EvaluateCondition_errors_of_arguments_propagate() =>
        Assert.Throws<ExpressionException>(() => Expression.EvaluateCondition(JsonNode.Parse("""{"equals":["@triggerBody()['nope']",1]}"""), Ctx));

    // ---- ToText

    [Theory]
    [InlineData("\"abc\"", "abc")]
    [InlineData("12", "12")]
    [InlineData("-1.25", "-1.25")]
    [InlineData("true", "True")]
    [InlineData("false", "False")]
    [InlineData("null", "")]
    [InlineData("""{"a":[1,"b"]}""", """{"a":[1,"b"]}""")]
    [InlineData("""[1, 2]""", "[1,2]")]
    public void ToText_forms(string json, string expected) => Assert.Equal(expected, Expression.ToText(JsonNode.Parse(json)));

    [Fact]
    public void Text_conversions_use_the_invariant_culture()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1.5", Expression.ToText(JsonValue.Create(1.5)));
            AssertEval("concat('v=', 1.5)", "\"v=1.5\"");
            AssertEval("addDays('2018-03-15T00:00:00Z', 1)", "\"2018-03-16T00:00:00.0000000Z\"");
            AssertEval("greater(1.5, 1)", "true");
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // Hostile input: only the three Flows exceptions may come out of an expression, never a .NET one.
    [Theory]
    [InlineData("substring('abc', 9223372036854775807, 1)")]
    [InlineData("substring('abc', -9223372036854775808, 9223372036854775807)")]
    [InlineData("addDays('2026-01-01T00:00:00Z', 9223372036854775807)")]
    [InlineData("addDays('2026-01-01T00:00:00Z', -9223372036854775808)")]
    [InlineData("addDays('9999-12-31T23:59:59Z', 1)")]
    [InlineData("addDays('0001-01-01T00:00:00Z', -1)")]
    [InlineData("addDays('0001-01-01T00:00:00+14:00', 1)")]
    [InlineData("createArray(1)[9223372036854775807]")]
    [InlineData("createArray(1)[-9223372036854775808]")]
    [InlineData("createArray(1)?[9223372036854775807]")]
    [InlineData("createArray(1)[2147483648]")]
    [InlineData("createArray(1)[1.5]")]
    [InlineData("split('abc', '')")]
    [InlineData("replace('abc', '', 'x')")]
    [InlineData("min(9223372036854775807, -9223372036854775808)")]
    [InlineData("min(createArray())")]
    [InlineData("min(createArray(1, 'a'))")]
    [InlineData("decodeBase64('!!!')")]
    [InlineData("decodeBase64('/w==')")] // 0xFF is not UTF-8
    [InlineData("decodeBase64('YQ')")] // padding missing
    [InlineData("json('1e999')")]
    [InlineData("json('{\"a\":')")]
    [InlineData("json('{\"a\":1,\"a\":2}')['a']")] // a JsonObject finds the duplicate only when it is read
    [InlineData("json('[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[1]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]')")]
    [InlineData("string(99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999)")]
    [InlineData("greater(99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999, 1)")]
    public void Hostile_input_never_escapes_as_a_dotnet_exception(string expression)
    {
        try
        {
            Eval(expression)?.ToJsonString();
        }
        catch (Exception e) when (e is ExpressionException or UnsupportedException or UnmockedException)
        {
        }
    }
}