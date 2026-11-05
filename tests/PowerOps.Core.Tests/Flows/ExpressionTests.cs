using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Core.Flows;
using PowerOps.Tests.Shared;

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

        public void ChargeCopy(long characters)
        {
        }

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
    [InlineData("a@@b", "a@@b")] // the capture run M03: @@ escapes only at the start of a string
    [InlineData("mail a@@b", "mail a@@b")]
    [InlineData("a@b", "a@b")]
    [InlineData("end@", "end@")]
    [InlineData(" @", " @")]
    [InlineData("@@{parameters('myAge')}", "@{parameters('myAge')}")]
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
    public void A_double_at_before_a_brace_inside_a_text_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Expression.Evaluate("My age is @@{parameters('myAge')}", Ctx)); // the capture does not cover it

    [Fact]
    public void Text_with_unterminated_interpolation_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Expression.Evaluate("a @{concat('a', 'b'", Ctx));

    [Fact]
    public void Interpolation_with_unreadable_expression_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Expression.Evaluate("a @{not a function}", Ctx));

    // C-10a: a parse error echoes at most 200 characters of the expression, however long it is.
    [Theory]
    [InlineData("@(", '1', "")]
    [InlineData("@concat('a',", '1', "")]
    [InlineData("@", 'x', "")] // a 5 MB name that is not true, false or null
    [InlineData("@", 'x', "(")] // an unknown function with a 5 MB name
    [InlineData("a @{", '1', "")] // never closed
    public void A_parse_error_of_a_5_MB_expression_echoes_200_characters(string start, char fill, string end)
    {
        var e = Assert.Throws<UnsupportedException>(() => Expression.Evaluate(start + new string(fill, 5_000_000) + end, Ctx));

        Assert.InRange(e.Message.Length, 1, 600);
    }

    [Fact]
    public void A_short_expression_is_echoed_whole_and_a_cut_never_splits_a_surrogate_pair()
    {
        var e = Assert.Throws<UnsupportedException>(() => Expression.Evaluate("@(", Ctx));
        Assert.Equal("Cannot read expression '(': unexpected '('.", e.Message);

        var text = "@(" + new string('1', 198) + "😀" + new string('1', 50); // the parser sees "(" and 198 ones, so the emoji's high surrogate is its 200th character
        e = Assert.Throws<UnsupportedException>(() => Expression.Evaluate(text, Ctx));
        Assert.DoesNotContain(e.Message, c => char.IsSurrogate(c));
        Assert.Contains("…'", e.Message);
    }

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
    [InlineData("triggerOutputs()?['BOD']", "null")] // a name that differs by more than case
    [InlineData("variables('nothing')?['x']", "null")]
    public void Property_and_index_access(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("triggerOutputs()['body']['empty']['x']")]
    [InlineData("variables('nothing')['x']")]
    [InlineData("triggerOutputs()['nope']")]
    [InlineData("triggerOutputs().nope")]
    [InlineData("triggerOutputs()['body']['value'][5]")]
    [InlineData("triggerOutputs()['body']['value'][-1]")]
    [InlineData("triggerOutputs()['BOD']")] // a name that differs by more than case
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
    [InlineData("contains(createArray(true), 1)", "false")]
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
    [InlineData("first('')", "\"\"")] // the capture run M16: last('') gave ""
    [InlineData("first(json('[]'))", "null")] // not on Learn; plan "Not on Learn"
    public void First(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void First_of_a_number_is_an_error() => Assert.Throws<ExpressionException>(() => Eval("first(5)"));

    // last: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#last
    [Theory]
    [InlineData("last('abcd')", "\"d\"")]
    [InlineData("last(createArray(0, 1, 2, 3))", "3")]
    [InlineData("last('')", "\"\"")] // the capture run M16
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

    // ---- property names: exact first, else ignoring case (the capture run M09)

    [Theory]
    [InlineData("outputs('Get_user')['body']['FULLNAME']", "\"Contoso Corporation\"")]
    [InlineData("outputs('Get_user')?['BODY']?['username']", "\"ContosoInc\"")]
    [InlineData("outputs('Get_user')['body/fullname']", "\"Contoso Corporation\"")]
    [InlineData("json('{\"a\":1,\"A\":2}')['A']", "2")] // an exact match wins
    [InlineData("json('{\"a\":1,\"A\":2}')['a']", "1")]
    [InlineData("json('{\"AB\":2,\"ab\":1}')['Ab']", "2")] // none exact: the first in document order
    public void A_property_is_read_by_its_exact_name_else_ignoring_case(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void A_property_that_differs_by_more_than_case_does_not_exist() =>
        Assert.Throws<ExpressionException>(() => Eval("outputs('Get_user')['bod']"));

    // ---- logical functions

    // equals: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#equals
    [Theory]
    [InlineData("equals(true, 1)", "false")] // the capture run M06: a boolean equals only a boolean
    [InlineData("equals('abc', 'abcd')", "false")]
    [InlineData("equals('abc', 'abc')", "true")]
    [InlineData("equals('abc', 'ABC')", "false")] // not on Learn; plan "Not on Learn" (ordinal)
    [InlineData("equals(1, 1.0)", "true")] // numbers by value
    [InlineData("equals(1, 2)", "false")]
    [InlineData("equals(false, 0)", "false")]
    [InlineData("equals(true, 'true')", "false")]
    [InlineData("equals(true, 0)", "false")]
    [InlineData("equals(true, true)", "true")]
    [InlineData("equals(true, false)", "false")]
    [InlineData("equals(null, '')", "false")] // the capture run M07: null equals only null, although Learn's logical-functions intro says null is converted to ""
    [InlineData("equals(null, 0)", "false")]
    [InlineData("equals(triggerOutputs()['body']['empty'], '')", "false")]
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
    [InlineData("if(equals(1, 1), 'yes', json('not json'))", "\"yes\"")] // the capture run M04: only the branch it returns is evaluated
    [InlineData("if(false, triggerOutputs()['nope'], 'no')", "\"no\"")]
    public void If(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("if('true', 1, 2)")]
    [InlineData("if(1, 1, 2)")]
    [InlineData("if(true, 1)")]
    [InlineData("if(true, triggerOutputs()['nope'], 2)")] // the branch it returns is evaluated
    [InlineData("if(triggerOutputs()['nope'], 1, 2)")]
    public void If_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Fact]
    public void And_and_or_evaluate_every_argument_but_if_only_the_branch_it_returns()
    {
        // not on Learn; plan "Not on Learn": and and or still evaluate every argument (the capture did not cover them), so one can fail the expression
        Assert.Throws<ExpressionException>(() => Eval("or(true, triggerOutputs()['nope'])"));
        Assert.Throws<ExpressionException>(() => Eval("and(false, triggerOutputs()['nope'])"));
        AssertEval("if(true, 'ok', parameters('other'))", "\"ok\"");
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
    [InlineData("addDays('2018-03-15T10:30:00+02:00', 0)", "\"2018-03-15T08:30:00.0000000+00:00\"")] // the capture run A11: converted to UTC, labelled +00:00
    [InlineData("addDays('2018-03-15T00:00:00.1234567Z', 1)", "\"2018-03-16T00:00:00.1234567Z\"")]
    [InlineData("addDays('2018-03-15', 1)", "\"2018-03-16T00:00:00.0000000\"")] // the capture run A06: no zone in, no zone out
    [InlineData("addDays('2018-03-15T10:30:00+00:00', 0)", "\"2018-03-15T10:30:00.0000000+00:00\"")]
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

    // addDays with a format: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#addDays
    [Theory]
    [InlineData("addDays('2018-03-15T00:00:00Z', 10, 'yyyy-MM-dd')", "\"2018-03-25\"")]
    [InlineData("addDays('2026-12-31T23:00:00Z', 1, 'yyyy-MM-dd')", "\"2027-01-01\"")]
    [InlineData("addDays('2028-02-28T00:00:00Z', 1, 'yyyy-MM-dd')", "\"2028-02-29\"")]
    [InlineData("addDays('2018-03-15T00:00:00Z', 0, 'D')", "\"Thursday, March 15, 2018\"")]
    [InlineData("addDays('2018-03-15T13:00:00Z', 0, 't')", "\"1:00 PM\"")]
    [InlineData("addDays('2018-03-15T00:00:00Z', 1, 'yyyy')", "\"2018\"")]
    public void AddDays_with_a_format(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("addDays('2018-03-15T00:00:00Z', 1, 'X')")]
    [InlineData("addDays('2018-03-15T00:00:00Z', 1, 5)")]
    [InlineData("addDays('2018-03-15T00:00:00Z', 1, 'yyyy', 'en-us')")]
    public void AddDays_with_a_format_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("addDays('2018-03-15T00:00:00Z', 1, '')")]
    [InlineData("addDays('2018-03-15', 1, 'yyyy-MM-dd')")] // a format needs a timestamp ending in Z
    [InlineData("addDays('2018-03-15T10:30:00+02:00', 0, 'HH:mm')")]
    [InlineData("addDays('2018-03-15T10:30:00+00:00', 0, 'HH:mm')")]
    public void AddDays_with_a_format_is_unsupported_for(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // formatDateTime: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#formatDateTime
    // and https://learn.microsoft.com/power-automate/date-time-values and https://learn.microsoft.com/power-automate/expression-cookbook#date-and-time
    [Theory]
    [InlineData("formatDateTime('03/15/2018')", "\"2018-03-15T00:00:00.0000000\"")]
    [InlineData("formatDateTime('03/15/2018 12:00:00', 'yyyy-MM-ddTHH:mm:ss')", "\"2018-03-15T12:00:00\"")]
    [InlineData("formatDateTime('01/31/2016', 'dddd MMMM d')", "\"Sunday January 31\"")]
    [InlineData("formatDateTime('01/31/2016', 'dddd MMMM d', 'fr-fr')", "\"dimanche janvier 31\"")]
    [InlineData("formatDateTime('01/31/2016', 'dddd MMMM d', 'fr-FR')", "\"dimanche janvier 31\"")]
    [InlineData("formatDateTime('01/31/2016', 'dddd MMMM d', 'es-es')", "\"domingo enero 31\"")]
    [InlineData("formatDateTime('2009-06-15T13:45:30', 'g')", "\"6/15/2009 1:45 PM\"")] // a plain space
    [InlineData("formatDateTime('2009-06-15T13:45:30', 'D')", "\"Monday, June 15, 2009\"")]
    [InlineData("formatDateTime('2009-06-15T13:45:30', 'M/dd/yyyy h:mm tt')", "\"6/15/2009 1:45 PM\"")]
    [InlineData("formatDateTime('2009-06-15T13:45:30', 'M/dd/yyyy HH:mm:ss tt')", "\"6/15/2009 13:45:30 PM\"")]
    // Learn's page shows 1:45:30, but hh is two digits in .NET's custom format reference; the capture checks it
    [InlineData("formatDateTime('2009-06-15T13:45:30', 'yyyy/MM/dd hh:mm:ss tt')", "\"2009/06/15 01:45:30 PM\"")]
    [InlineData("formatDateTime('2026-03-18T14:30:00Z', 'MMMM dd, yyyy')", "\"March 18, 2026\"")]
    [InlineData("formatDateTime('2018-03-15T00:00:00Z')", "\"2018-03-15T00:00:00.0000000Z\"")]
    [InlineData("formatDateTime('03/15/2018', 'o')", "\"2018-03-15T00:00:00.0000000\"")]
    [InlineData("formatDateTime('2018-03-15T10:30Z', 'HH:mm')", "\"10:30\"")]
    [InlineData("formatDateTime('01/31/2016', 'dd MMMM yyyy', 'fr-fr')", "\"31 janvier 2016\"")]
    [InlineData("formatDateTime('01/31/2016', 'dddd MMMM d', 'FR-fr')", "\"dimanche janvier 31\"")]
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', 'zzz')", "\"+00:00\"")]
    [InlineData("formatDateTime('2018-03-15T13:00:00Z', 'U')", "\"Thursday, March 15, 2018 1:00:00 PM\"")]
    [InlineData("formatDateTime(utcNow(), 'yyyy-MM-dd')", "\"2018-04-15\"")]
    public void FormatDateTime(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("g")]
    [InlineData("t")]
    [InlineData("T")]
    [InlineData("U")]
    public void FormatDateTime_writes_a_plain_space_before_AM_or_PM(string format) =>
        Assert.DoesNotContain('\u202F', Eval($"formatDateTime('2009-06-15T13:45:30Z', '{format}')")!.GetValue<string>());

    [Theory]
    [InlineData("formatDateTime(5)")]
    [InlineData("formatDateTime('not a date')")]
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', 5)")]
    [InlineData("formatDateTime()")]
    [InlineData("formatDateTime('a', 'b', 'c', 'd')")]
    public void FormatDateTime_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', '')")]
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', 'o', '')")]
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', 'X')")] // Learn doesn't say an invalid format is an error here
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', 'o', 'de-DE')")] // a known locale Learn doesn't show
    [InlineData("formatDateTime('2018-03-15T00:00:00Z', 'o', 'xx-YY')")] // Learn says an error; the capture run F18 gave English text
    // fr-fr and es-es only with day, month and year formats, as Learn shows them
    [InlineData("formatDateTime('01/31/2016', 'g', 'fr-fr')")]
    [InlineData("formatDateTime('01/31/2016', 'D', 'fr-fr')")]
    [InlineData("formatDateTime('01/31/2016', 'tt', 'fr-fr')")]
    [InlineData("formatDateTime('01/31/2016', 'HH:mm', 'es-es')")]
    [InlineData("formatDateTime('01/31/2016', 'ddd d MMM', 'es-es')")]
    [InlineData("formatDateTime('01/31/2016', 'dd/MM/yyyy', 'fr-fr')")]
    // other text that TryParse reads borrows parts from today's date
    [InlineData("formatDateTime('10:30')")]
    [InlineData("formatDateTime('March 15')")]
    [InlineData("formatDateTime('3/15/2018')")]
    [InlineData("formatDateTime('2018-03-15 10:30:00')")]
    [InlineData("addDays('10:30', 1)")]
    [InlineData("addDays('March 15', 1, 'yyyy-MM-dd')")]
    [InlineData("formatDateTime('2018-03-15T10:30:00+02:00', 'HH:mm')")] // Learn doesn't say whether an offset is kept
    [InlineData("formatDateTime('2018-03-15T00:00:00+00:00')")]
    // .NET would take this computer's time zone for these on a timestamp with no zone
    [InlineData("formatDateTime('03/15/2018', 'U')")]
    [InlineData("formatDateTime('03/15/2018', 'u')")]
    [InlineData("formatDateTime('03/15/2018', 'r')")]
    [InlineData("formatDateTime('03/15/2018', 'R')")]
    [InlineData("formatDateTime('03/15/2018', 'K')")]
    [InlineData("formatDateTime('03/15/2018', 'yyyy K')")]
    [InlineData("formatDateTime('03/15/2018', 'zzz')")]
    [InlineData("formatDateTime('03/15/2018', 'HH:mm z')")]
    public void FormatDateTime_is_unsupported_for(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // int: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#int
    // and https://learn.microsoft.com/power-automate/expression-cookbook
    [Theory]
    [InlineData("int('10')", "10")]
    [InlineData("int('42')", "42")]
    [InlineData("int('-7')", "-7")]
    [InlineData("int('0')", "0")]
    [InlineData("int('-0')", "0")]
    [InlineData("int('2147483647')", "2147483647")]
    [InlineData("int(14)", "14")] // not on Learn, plan "Not on Learn": an integer number gives itself
    public void Int(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("int('3.14')")] // the cookbook: a decimal string is an error, not truncated
    [InlineData("int('-3.14')")]
    [InlineData("int('abc')")]
    [InlineData("int('')")]
    [InlineData("int('-')")]
    [InlineData("int('NaN')")]
    [InlineData("int()")]
    [InlineData("int('1', '2')")]
    public void Int_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("int(' 1')")] // Learn doesn't say how int reads a sign, spaces, leading zeros, an exponent or a number past 32 bits
    [InlineData("int('+5')")]
    [InlineData("int('007')")]
    [InlineData("int('1e3')")]
    [InlineData("int('2147483648')")]
    [InlineData("int('99999999999999999999')")]
    [InlineData("int('12abc')")]
    [InlineData("int('1,000')")]
    [InlineData("int('\uFF13')")] // a full-width 3
    [InlineData("int(2147483648)")]
    [InlineData("int(1.5)")]
    [InlineData("int(true)")]
    [InlineData("int(null)")]
    [InlineData("int(createArray(1))")]
    [InlineData("int(json('{}'))")]
    public void Int_of_what_Learn_does_not_settle_is_unsupported(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // addHours, addMinutes, addSeconds: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#addHours (and #addMinutes, #addSeconds)
    [Theory]
    [InlineData("addHours('2018-03-15T00:00:00Z', 10)", "\"2018-03-15T10:00:00.0000000Z\"")]
    [InlineData("addHours('2018-03-15T15:00:00Z', -5)", "\"2018-03-15T10:00:00.0000000Z\"")]
    [InlineData("addMinutes('2018-03-15T00:10:00Z', 10)", "\"2018-03-15T00:20:00.0000000Z\"")]
    [InlineData("addMinutes('2018-03-15T00:20:00Z', -5)", "\"2018-03-15T00:15:00.0000000Z\"")]
    [InlineData("addSeconds('2018-03-15T00:00:00Z', 10)", "\"2018-03-15T00:00:10.0000000Z\"")]
    [InlineData("addSeconds('2018-03-15T00:00:30Z', -5)", "\"2018-03-15T00:00:25.0000000Z\"")]
    [InlineData("addHours(utcNow(), 1)", "\"2018-04-15T14:00:00.0000000Z\"")]
    [InlineData("addHours('2018-03-15T10:00:00', 1)", "\"2018-03-15T11:00:00.0000000\"")] // not on Learn: no zone in, no zone out, as addDays
    [InlineData("addMinutes('2018-03-15', 90)", "\"2018-03-15T01:30:00.0000000\"")]
    [InlineData("addHours('2018-03-15T10:30:00+02:00', 1)", "\"2018-03-15T09:30:00.0000000+00:00\"")] // as the capture run A11 for addDays
    [InlineData("addSeconds('2018-03-15T10:30:00+02:00', 0)", "\"2018-03-15T08:30:00.0000000+00:00\"")]
    public void AddHours_AddMinutes_AddSeconds(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("addHours('2018-03-15T00:00:00Z', 10, 'yyyy-MM-dd HH:mm:ss')", "\"2018-03-15 10:00:00\"")]
    [InlineData("addHours('2018-03-15T20:00:00Z', 5, 'yyyy-MM-dd HH:mm:ss')", "\"2018-03-16 01:00:00\"")]
    [InlineData("addMinutes('2018-03-15T00:10:00Z', 10, 'yyyy-MM-dd HH:mm:ss')", "\"2018-03-15 00:20:00\"")]
    [InlineData("addSeconds('2018-03-15T00:00:30Z', -5, 'yyyy-MM-dd HH:mm:ss')", "\"2018-03-15 00:00:25\"")]
    [InlineData("addHours('2018-03-15T00:00:00Z', 0, 'D')", "\"Thursday, March 15, 2018\"")]
    public void AddHours_AddMinutes_AddSeconds_with_a_format(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("addHours('2018-03-15T00:00:00Z', 1.5)")]
    [InlineData("addHours('2018-03-15T00:00:00Z', '1')")]
    [InlineData("addMinutes('2018-03-15T00:00:00Z', 1.5)")]
    [InlineData("addSeconds('2018-03-15T00:00:00Z', 'x')")]
    [InlineData("addHours('2018-03-15T00:00:00Z', 1, 'X')")]
    [InlineData("addMinutes('2018-03-15T00:00:00Z', 1, 'X')")]
    [InlineData("addSeconds('2018-03-15T00:00:00Z', 1, 'X')")]
    [InlineData("addHours('2018-03-15T00:00:00Z', 1, 5)")]
    [InlineData("addHours('not a date', 1)")]
    [InlineData("addHours(5, 1)")]
    [InlineData("addHours('2018-03-15T00:00:00Z')")]
    [InlineData("addMinutes()")]
    [InlineData("addSeconds('2018-03-15T00:00:00Z', 1, 'yyyy', 'en-us')")]
    [InlineData("addHours('2018-03-15T00:00:00Z', 9223372036854775807)")]
    [InlineData("addMinutes('2018-03-15T00:00:00Z', -9223372036854775808)")]
    [InlineData("addSeconds('2018-03-15T00:00:00Z', 9223372036854775807)")]
    [InlineData("addHours('9999-12-31T23:00:00Z', 2)")]
    [InlineData("addMinutes('9999-12-31T23:59:00Z', 1)")]
    [InlineData("addSeconds('0001-01-01T00:00:00Z', -1)")]
    public void AddHours_AddMinutes_AddSeconds_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("addHours('2018-03-15T00:00:00Z', 1, '')")]
    [InlineData("addHours('2018-03-15', 1, 'yyyy-MM-dd')")] // a format needs a timestamp ending in Z
    [InlineData("addMinutes('2018-03-15T10:30:00+02:00', 0, 'HH:mm')")]
    [InlineData("addSeconds('2018-03-15T10:30:00+00:00', 0, 'HH:mm')")]
    public void AddHours_AddMinutes_AddSeconds_with_a_format_is_unsupported_for(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // startOfDay: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#startOfDay
    [Theory]
    [InlineData("startOfDay('2018-03-15T13:30:30Z')", "\"2018-03-15T00:00:00.0000000Z\"")]
    [InlineData("startOfDay('2018-03-15T00:00:00Z')", "\"2018-03-15T00:00:00.0000000Z\"")]
    [InlineData("startOfDay('2018-03-15T23:59:59.9999999Z')", "\"2018-03-15T00:00:00.0000000Z\"")]
    [InlineData("startOfDay('2018-03-15T13:30:30')", "\"2018-03-15T00:00:00.0000000\"")] // not on Learn: no zone stays none
    [InlineData("startOfDay('03/15/2018 12:00:00')", "\"2018-03-15T00:00:00.0000000\"")]
    [InlineData("startOfDay('2018-03-15T13:30:30Z', 'yyyy-MM-dd')", "\"2018-03-15\"")]
    [InlineData("startOfDay('2018-03-15T13:30:30Z', 'D')", "\"Thursday, March 15, 2018\"")]
    [InlineData("startOfDay('2018-03-15T13:30:30Z', 'HH:mm')", "\"00:00\"")]
    public void StartOfDay(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("startOfDay(5)")]
    [InlineData("startOfDay('not a date')")]
    [InlineData("startOfDay('2018-03-15T13:30:30Z', 'X')")]
    [InlineData("startOfDay('2018-03-15T13:30:30Z', 5)")]
    [InlineData("startOfDay()")]
    [InlineData("startOfDay('2018-03-15T13:30:30Z', 'yyyy', 'en-us')")]
    public void StartOfDay_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("startOfDay('2018-03-15T01:00:00+02:00')")] // the day depends on the zone it is taken in
    [InlineData("startOfDay('2018-03-15T01:00:00+00:00')")]
    [InlineData("startOfDay('2018-03-15T13:30:30', 'yyyy-MM-dd')")] // a format needs a timestamp ending in Z
    [InlineData("startOfDay('2018-03-15T13:30:30Z', '')")]
    public void StartOfDay_is_unsupported_for(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // dayOfWeek: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#dayOfWeek
    [Theory]
    [InlineData("dayOfWeek('2018-03-15T13:27:36Z')", "4")]
    [InlineData("dayOfWeek('2018-03-18T00:00:00Z')", "0")] // a Sunday
    [InlineData("dayOfWeek('2018-03-17T23:59:59Z')", "6")] // a Saturday
    [InlineData("dayOfWeek('2018-03-15T23:59:59')", "4")]
    [InlineData("dayOfWeek('03/15/2018')", "4")]
    public void DayOfWeek(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Theory]
    [InlineData("dayOfWeek(5)")]
    [InlineData("dayOfWeek('not a date')")]
    [InlineData("dayOfWeek()")]
    [InlineData("dayOfWeek('2018-03-15T13:27:36Z', 'yyyy')")]
    public void DayOfWeek_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("dayOfWeek('2018-03-15T01:00:00+02:00')")]
    [InlineData("dayOfWeek('2018-03-15T01:00:00+00:00')")]
    public void DayOfWeek_of_an_offset_is_unsupported(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // time-zone names, taken only as Windows writes them: https://learn.microsoft.com/windows-hardware/manufacture/desktop/default-time-zones
    [Theory]
    [InlineData("UTC", "08:00")]
    [InlineData("Pacific Standard Time", "00:00")]
    [InlineData("W. Europe Standard Time", "09:00")]
    [InlineData("Central Standard Time (Mexico)", "02:00")]
    [InlineData("UTC-11", "21:00")]
    public void A_windows_time_zone_name_is_taken(string zone, string expected) =>
        AssertEval($"convertFromUtc('2024-01-01T08:00:00Z', '{zone}', 'HH:mm')", $"\"{expected}\"");

    [Theory]
    [InlineData("America/Los_Angeles")]
    [InlineData("Etc/UTC")]
    [InlineData("pacific standard time")]
    [InlineData("utc")]
    [InlineData("Coordinated Universal Time")]
    [InlineData("Hawaiian Standard Time")] // .NET knows it, the simulator's table does not
    [InlineData("Bogus")]
    [InlineData("")]
    public void Any_other_time_zone_name_is_unsupported(string zone)
    {
        Assert.Throws<UnsupportedException>(() => Eval($"convertFromUtc('2024-01-01T08:00:00Z', '{zone}', 'HH:mm')"));
        Assert.Throws<UnsupportedException>(() => Eval($"convertTimeZone('2024-01-01T08:00:00Z', 'UTC', '{zone}', 'HH:mm')"));
        Assert.Throws<UnsupportedException>(() => Eval($"convertTimeZone('2024-01-01T08:00:00', '{zone}', 'UTC', 'HH:mm')"));
    }

    [Fact]
    public void A_time_zone_name_that_is_not_a_string_is_an_error()
    {
        Assert.Throws<ExpressionException>(() => Eval("convertFromUtc('2018-01-01T08:00:00Z', 5)"));
        Assert.Throws<ExpressionException>(() => Eval("convertTimeZone('2018-01-01T08:00:00Z', 5, 'UTC')"));
    }

    // convertFromUtc and convertTimeZone: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#convertFromUtc (and #convertTimeZone)
    [Theory]
    [InlineData("convertFromUtc('2018-01-01T08:00:00.0000000Z', 'Pacific Standard Time')", "\"2018-01-01T00:00:00.0000000\"")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00.0000000Z', 'Pacific Standard Time', 'D')", "\"Monday, January 1, 2018\"")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00.0000000Z', 'UTC', 'Pacific Standard Time')", "\"2018-01-01T00:00:00.0000000\"")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00.0000000Z', 'UTC', 'Pacific Standard Time', 'D')", "\"Monday, January 1, 2018\"")] // Learn's example has T80; Q12
    // not on Learn, plan "Not on Learn": a timestamp with no zone is a time on the source zone's clock
    [InlineData("convertFromUtc('2018-01-01T08:00:00', 'Pacific Standard Time')", "\"2018-01-01T00:00:00.0000000\"")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00', 'UTC', 'yyyy-MM-ddTHH:mm:ss')", "\"2018-01-01T08:00:00\"")]
    [InlineData("convertTimeZone('2018-07-01T09:00:00', 'Pacific Standard Time', 'Eastern Standard Time')", "\"2018-07-01T12:00:00.0000000\"")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00', 'W. Europe Standard Time')", "\"2018-01-01T09:00:00.0000000\"")] // convertFromUtc takes punctuation in a name
    [InlineData("convertTimeZone('2018-07-01T09:00:00', 'Pacific Standard Time', 'UTC', 'yyyy-MM-ddTHH:mm:ss')", "\"2018-07-01T16:00:00\"")]
    [InlineData("convertTimeZone('07/01/2018', 'UTC', 'Pacific Standard Time', 'yyyy-MM-dd HH:mm')", "\"2018-06-30 17:00\"")]
    [InlineData("convertFromUtc(utcNow(), 'Pacific Standard Time', 'yyyy-MM-dd HH:mm')", "\"2018-04-15 06:00\"")]
    [InlineData("convertFromUtc('2008-01-01T00:00:00Z', 'Pacific Standard Time', 'yyyy-MM-dd HH:mm')", "\"2007-12-31 16:00\"")] // the first instant of the window
    [InlineData("convertFromUtc('2030-12-31T23:59:59Z', 'Tokyo Standard Time', 'yyyy-MM-dd HH:mm:ss')", "\"2031-01-01 08:59:59\"")] // the last
    [InlineData("convertFromUtc('2024-01-01T00:00:00Z', 'Egypt Standard Time', 'HH:mm')", "\"02:00\"")]
    public void ConvertFromUtc_and_ConvertTimeZone(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void Learns_second_convertTimeZone_example_has_an_hour_80_and_is_an_error() =>
        Assert.Throws<ExpressionException>(() => Eval("convertTimeZone('2018-01-01T80:00:00.0000000Z', 'UTC', 'Pacific Standard Time', 'D')"));

    [Theory]
    [InlineData("convertFromUtc(5, 'UTC')")]
    [InlineData("convertFromUtc('not a date', 'Pacific Standard Time')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time', 'X')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time', 5)")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time', 'D', 'en-us')")]
    [InlineData("convertTimeZone(5, 'UTC', 'Pacific Standard Time')")]
    [InlineData("convertTimeZone('not a date', 'UTC', 'Pacific Standard Time')")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00Z', 'UTC', 'Pacific Standard Time', 'X')")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00Z', 'UTC')")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00Z', 'UTC', 'Pacific Standard Time', 'D', 'en-us')")]
    public void Conversion_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("convertFromUtc('2018-01-01T08:00:00+02:00', 'Pacific Standard Time')")] // an offset
    [InlineData("convertTimeZone('2018-01-01T08:00:00+00:00', 'UTC', 'Pacific Standard Time')")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00Z', 'Pacific Standard Time', 'Eastern Standard Time')")] // Z with another source: the two disagree
    [InlineData("convertTimeZone('2018-01-01T08:00:00Z', 'UTC', 'W. Europe Standard Time')")] // convertTimeZone: Learn says to remove punctuation from a name
    [InlineData("convertTimeZone('2018-01-01T08:00:00', 'W. Europe Standard Time', 'UTC', 'HH:mm')")]
    [InlineData("convertTimeZone('2024-01-01T08:00:00Z', 'UTC-11', 'UTC', 'HH:mm')")]
    [InlineData("convertTimeZone('2024-01-01T08:00:00Z', 'UTC', 'Central Standard Time (Mexico)', 'HH:mm')")]
    [InlineData("convertTimeZone('2026-03-08T02:30:00', 'Pacific Standard Time', 'UTC', 'HH:mm')")] // a skipped clock time
    [InlineData("convertTimeZone('2026-11-01T01:30:00', 'Pacific Standard Time', 'UTC', 'HH:mm')")] // a repeated clock time
    [InlineData("convertFromUtc('2007-12-31T23:59:59Z', 'Pacific Standard Time')")] // before the zone's first year
    [InlineData("convertFromUtc('2023-07-15T12:00:00Z', 'Egypt Standard Time')")]
    [InlineData("convertFromUtc('2031-01-01T00:00:00Z', 'Pacific Standard Time')")] // after 2030
    [InlineData("convertFromUtc('9999-12-31T20:00:00Z', 'Tokyo Standard Time')")]
    [InlineData("convertTimeZone('2031-01-01T10:00:00', 'Tokyo Standard Time', 'Pacific Standard Time')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'UTC')")] // UTC as destination with the o format
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'UTC', 'o')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'UTC', 'O')")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00', 'Pacific Standard Time', 'UTC')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00', 'Pacific Standard Time', 'zzz')")] // the result has no zone
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time', 'u')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time', 'K')")]
    [InlineData("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time', '')")]
    [InlineData("convertTimeZone('2018-01-01T08:00:00Z', 'UTC', 'Pacific Standard Time', '')")]
    public void Conversion_is_unsupported_for(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    // Every zone the simulator takes, in the middle of January and of July of every year from its first year (the one after its last rule change) to 2030.
    // The offsets are what each zone's rules give; Windows and Linux CI both check them against their own data.
    [Theory]
    [InlineData("UTC", "+00:00", "+00:00", 2008)]
    [InlineData("Pacific Standard Time", "-08:00", "-07:00", 2008)]
    [InlineData("Mountain Standard Time", "-07:00", "-06:00", 2008)]
    [InlineData("US Mountain Standard Time", "-07:00", "-07:00", 2008)]
    [InlineData("Central Standard Time", "-06:00", "-05:00", 2008)]
    [InlineData("Eastern Standard Time", "-05:00", "-04:00", 2008)]
    [InlineData("Central Standard Time (Mexico)", "-06:00", "-06:00", 2023)]
    [InlineData("E. South America Standard Time", "-03:00", "-03:00", 2020)]
    [InlineData("GMT Standard Time", "+00:00", "+01:00", 2008)]
    [InlineData("W. Europe Standard Time", "+01:00", "+02:00", 2008)]
    [InlineData("Romance Standard Time", "+01:00", "+02:00", 2008)]
    [InlineData("Central Europe Standard Time", "+01:00", "+02:00", 2008)]
    [InlineData("South Africa Standard Time", "+02:00", "+02:00", 2008)]
    [InlineData("Egypt Standard Time", "+02:00", "+03:00", 2024)]
    [InlineData("Russian Standard Time", "+03:00", "+03:00", 2015)]
    [InlineData("Arabian Standard Time", "+04:00", "+04:00", 2008)]
    [InlineData("India Standard Time", "+05:30", "+05:30", 2008)]
    [InlineData("Nepal Standard Time", "+05:45", "+05:45", 2008)]
    [InlineData("China Standard Time", "+08:00", "+08:00", 2008)]
    [InlineData("Singapore Standard Time", "+08:00", "+08:00", 2008)]
    [InlineData("Tokyo Standard Time", "+09:00", "+09:00", 2008)]
    [InlineData("AUS Eastern Standard Time", "+11:00", "+10:00", 2009)]
    [InlineData("New Zealand Standard Time", "+13:00", "+12:00", 2008)]
    [InlineData("UTC-11", "-11:00", "-11:00", 2008)]
    public void Every_supported_zone_gives_its_offset_from_its_first_year_to_2030(string zone, string january, string july, int firstYear)
    {
        for (var year = firstYear; year <= 2030; year++)
        {
            foreach (var (month, offset) in new[] { (1, january), (7, july) })
            {
                var utc = new DateTime(year, month, 15, 12, 0, 0, DateTimeKind.Unspecified);
                var expected = utc + TimeSpan.Parse(offset.TrimStart('+'), CultureInfo.InvariantCulture);
                AssertEval($"convertFromUtc('{year}-{month:00}-15T12:00:00Z', '{zone}', 'yyyy-MM-ddTHH:mm:ss')", $"\"{expected:yyyy-MM-ddTHH:mm:ss}\"");
            }
        }
    }

    // The minute before and the minute of each 2026 change: the UTC instant and the clock time it shows.
    [Theory]
    [InlineData("Pacific Standard Time", "2026-03-08T09:59:00Z", "2026-03-08T01:59:00")]
    [InlineData("Pacific Standard Time", "2026-03-08T10:00:00Z", "2026-03-08T03:00:00")]
    [InlineData("Pacific Standard Time", "2026-11-01T08:59:00Z", "2026-11-01T01:59:00")]
    [InlineData("Pacific Standard Time", "2026-11-01T09:00:00Z", "2026-11-01T01:00:00")]
    [InlineData("W. Europe Standard Time", "2026-03-29T00:59:00Z", "2026-03-29T01:59:00")]
    [InlineData("W. Europe Standard Time", "2026-03-29T01:00:00Z", "2026-03-29T03:00:00")]
    [InlineData("W. Europe Standard Time", "2026-10-25T00:59:00Z", "2026-10-25T02:59:00")]
    [InlineData("W. Europe Standard Time", "2026-10-25T01:00:00Z", "2026-10-25T02:00:00")]
    [InlineData("AUS Eastern Standard Time", "2026-04-04T15:59:00Z", "2026-04-05T02:59:00")]
    [InlineData("AUS Eastern Standard Time", "2026-04-04T16:00:00Z", "2026-04-05T02:00:00")]
    [InlineData("AUS Eastern Standard Time", "2026-10-03T15:59:00Z", "2026-10-04T01:59:00")]
    [InlineData("AUS Eastern Standard Time", "2026-10-03T16:00:00Z", "2026-10-04T03:00:00")]
    [InlineData("Egypt Standard Time", "2026-04-23T21:59:00Z", "2026-04-23T23:59:00")]
    [InlineData("Egypt Standard Time", "2026-04-23T22:00:00Z", "2026-04-24T01:00:00")]
    [InlineData("Egypt Standard Time", "2026-10-29T20:59:00Z", "2026-10-29T23:59:00")]
    [InlineData("Egypt Standard Time", "2026-10-29T21:00:00Z", "2026-10-29T23:00:00")]
    public void Daylight_saving_boundaries_of_2026_convert_from_utc(string zone, string utc, string local) =>
        AssertEval($"convertFromUtc('{utc}', '{zone}', 'yyyy-MM-ddTHH:mm:ss')", $"\"{local}\"");

    // Back to UTC with convertTimeZone, for the spring changes (the clock times before and after the skipped hour); its names take no punctuation, so not W. Europe.
    [Theory]
    [InlineData("Pacific Standard Time", "2026-03-08T01:59:00", "2026-03-08T09:59:00")]
    [InlineData("Pacific Standard Time", "2026-03-08T03:00:00", "2026-03-08T10:00:00")]
    [InlineData("AUS Eastern Standard Time", "2026-10-04T01:59:00", "2026-10-03T15:59:00")]
    [InlineData("AUS Eastern Standard Time", "2026-10-04T03:00:00", "2026-10-03T16:00:00")]
    public void Daylight_saving_boundaries_of_2026_convert_back_to_utc(string zone, string local, string utc) =>
        AssertEval($"convertTimeZone('{local}', '{zone}', 'UTC', 'yyyy-MM-ddTHH:mm:ss')", $"\"{utc}\"");

    // The autumn changes repeat an hour of clock time, and the skipped hour of each spring change has no time at all.
    [Theory]
    [InlineData("Pacific Standard Time", "2026-11-01T01:59:00")]
    [InlineData("Pacific Standard Time", "2026-11-01T01:00:00")]
    [InlineData("AUS Eastern Standard Time", "2026-04-05T02:59:00")]
    [InlineData("AUS Eastern Standard Time", "2026-04-05T02:00:00")]
    [InlineData("Pacific Standard Time", "2026-03-08T02:00:00")]
    [InlineData("AUS Eastern Standard Time", "2026-10-04T02:30:00")]
    public void A_repeated_or_skipped_clock_time_is_unsupported_from_every_zone(string zone, string local) =>
        Assert.Throws<UnsupportedException>(() => Eval($"convertTimeZone('{local}', '{zone}', 'UTC', 'yyyy-MM-ddTHH:mm:ss')"));

    // float: https://learn.microsoft.com/azure/logic-apps/expression-functions-reference#float
    [Theory]
    [InlineData("float('10,000.333')", "10000.333")]
    [InlineData("float('10.000,333', 'de-DE')", "10000.333")]
    [InlineData("float('12,3,4,5678')", "12345678")] // Learn's "unexpectedly valid inputs"
    [InlineData("float('1234567,8+')", "12345678")]
    [InlineData("float('12345,6,78,-')", "-12345678")]
    [InlineData("float('-12,345,678,')", "-12345678")]
    [InlineData("float('12345678.-')", "-12345678")]
    [InlineData("float('.12345678')", "0.12345678")]
    [InlineData("float('-1.5')", "-1.5")]
    [InlineData("float('14')", "14.0")]
    [InlineData("float(14)", "14.0")] // not on Learn, plan "Not on Learn": a number gives itself
    [InlineData("float(1.5)", "1.5")]
    [InlineData("float('10.000,333', 'DE-de')", "10000.333")]
    [InlineData("float('1,000.5', 'en-US')", "1000.5")]
    [InlineData("float('1,000.5', 'EN-us')", "1000.5")]
    [InlineData("equals(float('14'), 14)", "true")]
    [InlineData("string(float('1.5'))", "\"1.5\"")]
    public void Float(string expression, string expectedJson) => AssertEval(expression, expectedJson);

    [Fact]
    public void A_float_with_no_fraction_is_not_an_integer()
    {
        Assert.Throws<ExpressionException>(() => Eval("addDays('2018-03-15T00:00:00Z', float('1'))")); // the capture run A10 refused a Float
        Assert.Throws<UnsupportedException>(() => Eval("int(float('14'))"));
    }

    [Theory]
    [InlineData("float('abc')")]
    [InlineData("float('')")]
    [InlineData("float('1,000.5.5')")]
    [InlineData("float()")]
    [InlineData("float('1', 'en-US', 'x')")]
    [InlineData("float('1', 5)")]
    public void Float_errors(string expression) => Assert.Throws<ExpressionException>(() => Eval(expression));

    [Theory]
    [InlineData("float(' 1')")] // Learn doesn't say whether the service reads spaces, an exponent or parentheses
    [InlineData("float('1e3')")]
    [InlineData("float('(5)')")]
    [InlineData("float('NaN')")]
    [InlineData("float('Infinity')")]
    [InlineData("float('-Infinity')")]
    [InlineData("float('1e999')")]
    [InlineData("float('1', 'fr-FR')")]
    [InlineData("float('1', 'xx-YY')")]
    [InlineData("float('1', '')")]
    [InlineData("float(true)")]
    [InlineData("float(null)")]
    [InlineData("float(createArray(1))")]
    [InlineData("float(json('{}'))")]
    public void Float_of_what_Learn_does_not_settle_is_unsupported(string expression) => Assert.Throws<UnsupportedException>(() => Eval(expression));

    [Fact]
    public void Float_of_a_400_digit_string_is_unsupported() =>
        Assert.Throws<UnsupportedException>(() => Eval($"float('{new string('9', 400)}')"));

    // ---- the list of functions

    [Fact]
    public void Names_are_exactly_the_47_functions_sorted()
    {
        string[] expected =
        [
            "addDays", "addHours", "addMinutes", "addSeconds", "and", "body", "concat", "contains", "convertFromUtc",
            "convertTimeZone", "createArray", "dayOfWeek", "decodeBase64", "empty", "equals", "first", "float",
            "formatDateTime", "greater", "greaterOrEquals", "guid", "if", "int", "item", "items", "json", "last",
            "length", "lessOrEquals", "min", "not", "or", "outputs", "parameters", "replace", "result", "split",
            "startOfDay", "string", "substring", "toLower", "triggerBody", "triggerOutputs", "trim", "utcNow",
            "variables", "workflow",
        ];

        Assert.Equal(47, expected.Length);
        Assert.Equal(expected, ExpressionFunctions.Names);
        Assert.Equal(ExpressionFunctions.Names.Order(StringComparer.Ordinal), ExpressionFunctions.Names);
    }

    [Fact]
    public void Flow_tests_doc_lists_exactly_the_supported_functions()
    {
        var lines = File.ReadAllLines(Path.Combine(Golden.RepoRoot, "docs", "FLOW-TESTS.md"))
            .SkipWhile(line => line != "## Supported functions")
            .Skip(1)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal));
        var listed = Regex.Matches(string.Join('\n', lines), "`([^`\n]*)`")
            .Select(span => Regex.Match(span.Groups[1].Value, @"^([A-Za-z0-9]+)\("))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .Order(StringComparer.Ordinal);

        Assert.Equal(ExpressionFunctions.Names, listed);
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
    [InlineData("substring('abc', -9223372036854775808, 9223372036854775807)")]
    [InlineData("addDays('2026-01-01T00:00:00Z', 9223372036854775807)")]
    [InlineData("addDays('2026-01-01T00:00:00Z', -9223372036854775808)")]
    [InlineData("addDays('9999-12-31T23:59:59Z', 1)")]
    [InlineData("addDays('0001-01-01T00:00:00Z', -1)")]
    [InlineData("addDays('0001-01-01T00:00:00+14:00', 1)")]
    [InlineData("addHours('2026-01-01T00:00:00Z', 9223372036854775807)")]
    [InlineData("addMinutes('2026-01-01T00:00:00Z', -9223372036854775808)")]
    [InlineData("addSeconds('9999-12-31T23:59:59Z', 1)")]
    [InlineData("addSeconds('0001-01-01T00:00:00+14:00', 1)")]
    [InlineData("startOfDay('0001-01-01T00:00:00+14:00')")]
    [InlineData("dayOfWeek('0001-01-01T00:00:00+14:00')")]
    [InlineData("startOfDay('9999-12-31T23:59:59.9999999Z', 'o')")]
    [InlineData("convertFromUtc('9999-12-31T23:59:59Z', 'Tokyo Standard Time')")]
    [InlineData("convertFromUtc('0001-01-01T00:00:00Z', 'Tokyo Standard Time')")]
    [InlineData("convertTimeZone('0001-01-01T00:00:00', 'Tokyo Standard Time', 'UTC', 'yyyy')")]
    [InlineData("convertTimeZone('9999-12-31T23:59:59', 'UTC', 'Tokyo Standard Time', 'yyyy')")]
    [InlineData("convertTimeZone('9999-12-31T23:59:59', 'Tokyo Standard Time', 'UTC', 'yyyy')")]
    [InlineData("convertTimeZone('0001-01-01T00:00:00', 'UTC', 'UTC-11', 'yyyy')")]
    [InlineData("float('1e999')")]
    [InlineData("float('-1e999')")]
    [InlineData("float('0.000000000000000000000000000000000001', 'de-DE')")]
    [InlineData("createArray(1)[9223372036854775807]")]
    [InlineData("createArray(1)[-9223372036854775808]")]
    [InlineData("createArray(1)?[9223372036854775807]")]
    [InlineData("createArray(1)[2147483648]")]
    [InlineData("createArray(1)[1.5]")]
    [InlineData("split('abc', '')")]
    [InlineData("replace('abc', '', 'x')")]
    [InlineData("min(9223372036854775807, -9223372036854775808)")]
    [InlineData("min(createArray())")]
    [InlineData("decodeBase64('!!!')")]
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