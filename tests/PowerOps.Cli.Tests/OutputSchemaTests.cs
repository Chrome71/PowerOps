using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class OutputSchemaTests
{
    // Golden file name prefix -> schema file. A new JSON golden must be added here (or the test fails).
    private static readonly (string Prefix, string Schema)[] Map =
    [
        ("analyze-", "analyze.schema.json"),
        ("deploy-plan", "deploy-plan.schema.json"),
        ("diff-", "diff.schema.json"),
        ("drift-", "drift.schema.json"),
        ("plan-status", "plan-status.schema.json"),
        ("plan-facts", "plan-facts.schema.json"),
        ("rules", "rules.schema.json"),
        ("settings-apply", "settings-apply.schema.json"),
        ("settings-validate", "settings-validate.schema.json"),
        ("snapshot-", "snapshot.schema.json"),
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

    [Fact]
    public void Every_schema_is_used_by_a_golden_file()
    {
        var schemas = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "docs", "schemas"), "*.schema.json").Select(f => Path.GetFileName(f));
        var used = Goldens.Select(SchemaFor).ToHashSet();

        var unused = schemas.Where(s => !used.Contains(s)).ToList();

        Assert.True(unused.Count == 0, "No golden file uses: " + string.Join(", ", unused));
    }
}