using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;

using PowerOps.Core.Flows;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class OutputSchemaTests
{
    // Golden file name prefix -> schema file. A new JSON golden must be added here (or the test fails).
    private static readonly (string Prefix, string Schema)[] Map =
    [
        ("analyze-", "analyze.schema.json"),
        ("deploy-apply", "deploy-apply.schema.json"),
        ("deploy-plan", "deploy-plan.schema.json"),
        ("diff-", "diff.schema.json"),
        ("drift-", "drift.schema.json"),
        ("plan-status", "plan-status.schema.json"),
        ("plan-facts", "plan-facts.schema.json"),
        ("rules", "rules.schema.json"),
        ("settings-apply", "settings-apply.schema.json"),
        ("settings-validate", "settings-validate.schema.json"),
        ("snapshot-", "snapshot.schema.json"),
        ("test-", "test.schema.json"),
    ];

    // Input file formats, not outputs: the schema and the files under tests/fixtures it must accept.
    private static readonly (string Schema, string Pattern)[] Inputs =
    [
        ("flow-test.schema.json", "*.flowtest.json"),
    ];

    private static readonly string[] Goldens = Directory
        .GetFiles(Path.Combine(Golden.RepoRoot, "tests", "golden"), "*.json")
        .Select(f => Path.GetFileName(f))
        .Where(f => !f.StartsWith("settings-generate", StringComparison.Ordinal)) // pac's settings file format, not a powerops output
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToArray();

    private static string SchemaFor(string golden) =>
        Map.FirstOrDefault(m => golden.StartsWith(m.Prefix, StringComparison.Ordinal)).Schema
        ?? throw new Xunit.Sdk.XunitException($"tests/golden/{golden} has no schema; add one in docs/schemas/ and map it in OutputSchemaTests.");

    // Cached: parsing registers the schema's $id globally, and a second parse of the same $id throws.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, JsonSchema> Loaded = new();

    private static JsonSchema Load(string schema) =>
        Loaded.GetOrAdd(schema, name => JsonSchema.FromText(File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "schemas", name))));

    private static string Errors(JsonSchema schema, JsonNode instance)
    {
        var result = schema.Evaluate(JsonSerializer.SerializeToElement(instance), new EvaluationOptions { OutputFormat = OutputFormat.List });
        return result.IsValid
            ? ""
            : string.Join("\n", (result.Details ?? []).Where(d => d.Errors is not null).SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}")));
    }

    private static JsonNode Golden_(string golden) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "golden", golden)))!;

    public static TheoryData<string> GoldenFiles() => new(Goldens);

    [Theory]
    [MemberData(nameof(GoldenFiles))]
    public void Golden_output_validates_against_its_schema(string golden)
    {
        var errors = Errors(Load(SchemaFor(golden)), Golden_(golden));

        Assert.True(errors.Length == 0, $"{golden} does not match {SchemaFor(golden)}:\n{errors}");
    }

    [Theory]
    [MemberData(nameof(GoldenFiles))]
    public void Golden_output_without_schemaVersion_is_invalid(string golden)
    {
        var node = Golden_(golden);
        node.AsObject().Remove("schemaVersion");

        Assert.NotEqual("", Errors(Load(SchemaFor(golden)), node));
    }

    private static string[] FixtureFiles(string pattern) => Directory
        .GetFiles(Path.Combine(Golden.RepoRoot, "tests", "fixtures"), pattern, SearchOption.AllDirectories)
        .Select(f => Path.GetRelativePath(Golden.RepoRoot, f).Replace('\\', '/'))
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToArray();

    [Fact]
    public void Every_schema_is_used_by_a_golden_file_or_matches_a_fixture_file()
    {
        var schemas = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "docs", "schemas"), "*.schema.json").Select(f => Path.GetFileName(f));
        var used = Goldens.Select(SchemaFor).Concat(Inputs.Where(i => FixtureFiles(i.Pattern).Length > 0).Select(i => i.Schema)).ToHashSet();

        var unused = schemas.Where(s => !used.Contains(s)).ToList();

        Assert.True(unused.Count == 0, "No golden file or fixture file uses: " + string.Join(", ", unused));
    }

    public static TheoryData<string, string> InputFiles() => new(Inputs.SelectMany(i => FixtureFiles(i.Pattern).Select(f => (i.Schema, f))));

    [Theory]
    [MemberData(nameof(InputFiles))]
    public void Fixture_input_file_validates_against_its_schema(string schema, string file)
    {
        var errors = Errors(Load(schema), JsonNode.Parse(File.ReadAllText(Path.Combine(Golden.RepoRoot, file)))!);

        Assert.True(errors.Length == 0, $"{file} does not match {schema}:\n{errors}");
    }

    private const string Run = """{"kind":"runStatus","equals":"Succeeded"}""";

    private static string Flow(string extra = "", string assertions = "[" + Run + "]") => "{\"flow\":\"F\"," + extra + "\"assertions\":" + assertions + "}";

    private static string Assertion(string assertion) => Flow(assertions: "[" + assertion + "]");

    private static string Mock(string mock) => Flow("\"mocks\":{\"A\":" + mock + "},");

    // One text per rejection rule a schema can express (the 4 MB limit and invalid JSON are the code's alone).
    public static TheoryData<string> InvalidFlowTests() =>
    [
        "[]",
        """{"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""",
        """{"flow":"F"}""",
        """{"flow":"F","assertions":[]}""",
        """{"flow":"F","assertions":[{"kind":"runStatus","equals":"Succeeded"}],"extra":1}""",
        """{"flow":1,"assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""",
        """{"flow":"","assertions":[{"kind":"runStatus","equals":"Succeeded"}]}""",
        Flow("\"trigger\":[],"),
        Flow("\"trigger\":{\"x\":1},"),
        Flow("\"trigger\":{\"headers\":[]},"),
        Flow("\"mocks\":[],"),
        Mock("\"x\""),
        Mock("""{"outputs":{}}"""),
        Mock("""{"status":"TimedOut"}"""),
        Mock("""[{"status":"Succeeded"},{"status":"Skipped"}]"""),
        Mock("""{"status":"Succeeded","error":{"code":"x"}}"""),
        Mock("""{"status":"Failed","error":{"other":"x"}}"""),
        Mock("""{"status":"Succeeded","outputs":[]}"""),
        Mock("""{"status":"Succeeded","extra":1}"""),
        Flow("\"environmentVariables\":[],"),
        Flow("\"utcNow\":1,"),
        Flow("\"guid\":true,"),
        Flow("\"workflow\":\"w\","),
        """{"flow":"F","assertions":{}}""",
        """{"flow":"F","assertions":[1]}""",
        Assertion("""{"equals":"Succeeded"}"""),
        Assertion("""{"kind":"other","equals":1}"""),
        Assertion("""{"kind":"runStatus"}"""),
        Assertion("""{"kind":"runStatus","equals":"TimedOut"}"""),
        Assertion("""{"kind":"runStatus","equals":"Succeeded","action":"A"}"""),
        Assertion("""{"kind":"runStatus","equals":"Succeeded","call":0}"""),
        Assertion("""{"kind":"runStatus","equals":"Succeeded","path":["a"]}"""),
        Assertion("""{"kind":"runStatus","equals":"Succeeded","name":"v"}"""),
        Assertion("""{"kind":"runStatus","equals":"Succeeded","other":1}"""),
        Assertion("""{"kind":"actionStatus","equals":"Succeeded"}"""),
        Assertion("""{"kind":"actionStatus","action":"A","equals":"TimedOut"}"""),
        Assertion("""{"kind":"actionStatus","action":"A","equals":"Cancelled"}"""),
        Assertion("""{"kind":"actionStatus","action":"A","equals":"Succeeded","path":["a"]}"""),
        Assertion("""{"kind":"actionStatus","action":"A","equals":"Succeeded","name":"v"}"""),
        Assertion("""{"kind":"actionStatus","action":1,"equals":"Succeeded"}"""),
        Assertion("""{"kind":"inputs","equals":1}"""),
        Assertion("""{"kind":"inputs","action":"A","equals":1,"name":"v"}"""),
        Assertion("""{"kind":"outputs","action":"A","call":-1,"equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","call":1.5,"equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","call":"1","equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","path":"a","equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","path":[true],"equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","path":[-1],"equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","path":[1.5],"equals":1}"""),
        Assertion("""{"kind":"outputs","action":"A","path":[null],"equals":1}"""),
        Assertion("""{"kind":"variable","equals":1}"""),
        Assertion("""{"kind":"variable","name":"v","action":"A","equals":1}"""),
        Assertion("""{"kind":"variable","name":"v","call":0,"equals":1}"""),
        Assertion("""{"kind":"response","action":"A","equals":1}"""),
        Assertion("""{"kind":"response","call":0,"equals":1}"""),
        Assertion("""{"kind":"response","name":"v","equals":1}"""),
    ];

    [Theory]
    [MemberData(nameof(InvalidFlowTests))]
    public void An_invalid_flow_test_is_rejected_by_both_the_schema_and_the_parser(string text)
    {
        Assert.NotEqual("", Errors(Load("flow-test.schema.json"), JsonNode.Parse(text)!));
        Assert.Throws<InvalidDataException>(() => FlowTestFile.Parse(text, "x.flowtest.json"));
    }

    [Fact]
    public void A_valid_flow_test_is_accepted_by_both_the_schema_and_the_parser()
    {
        var text = Flow(
            "\"$schema\":\"x\",\"trigger\":{\"headers\":{},\"body\":null},\"mocks\":{\"A\":[{\"status\":\"Failed\",\"error\":{\"code\":\"c\",\"message\":\"m\"}},{\"status\":\"Succeeded\",\"outputs\":{}}]},\"environmentVariables\":{\"a\":null},\"utcNow\":\"t\",\"guid\":\"g\",\"workflow\":{},",
            "[" + Run + ",{\"kind\":\"actionStatus\",\"action\":\"A\",\"call\":0,\"equals\":\"Skipped\"},{\"kind\":\"outputs\",\"action\":\"A\",\"path\":[\"a\",0],\"equals\":null},{\"kind\":\"variable\",\"name\":\"v\",\"equals\":[1]},{\"kind\":\"response\",\"equals\":{}}]");

        Assert.Equal("", Errors(Load("flow-test.schema.json"), JsonNode.Parse(text)!));
        Assert.Equal(5, FlowTestFile.Parse(text, "x.flowtest.json").Assertions.Count);
    }
}