using System.Text.Json.Nodes;

using PowerOps.Core.Flows;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

/// <summary>Compares the simulator with one real run of the flow "PowerOps capture" (Dev, 2026-11-05); the fixture is in tests/fixtures/flowtests/capture.</summary>
public class CaptureTests
{
    private static readonly string Folder = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "flowtests", "capture");
    // The cloud ran an IncrementVariable (Until_step, twice, then Final_loops read 2); the simulator has no such action yet, so these are left out of the comparison and reported.
    private static readonly string[] NotCompared = ["Until_never", "Until_step", "Final_loops"];

    private static readonly string[] AllStatuses = ["Succeeded", "Failed", "Skipped", "TimedOut"];

    private static JsonObject Read(string path) => (JsonObject)JsonNode.Parse(File.ReadAllText(path))!;

    private static JsonObject Definition() =>
        (JsonObject)Read(Directory.GetFiles(Path.Combine(Folder, "solution", "Workflows"), "*.json").Single())["properties"]!["definition"]!;

    private static JsonObject Run() => Read(Path.Combine(Folder, "run.json"));

    private static string[] Unsupported() => [.. Read(Path.Combine(Folder, "expected-unsupported.json")).Select(p => p.Key)];

    public static IEnumerable<object[]> UnsupportedNames() => Unsupported().Select(name => new object[] { name });

    private static IEnumerable<(string Name, string Type)> Walk(JsonObject actions)
    {
        foreach (var (name, node) in actions)
        {
            var action = (JsonObject)node!;
            yield return (name, (string)action["type"]!);
            var holders = new[] { action, action["else"], action["default"] }.Concat(action["cases"] is JsonObject cases ? cases.Select(c => c.Value) : []);
            foreach (var inner in holders.OfType<JsonObject>().Select(h => h["actions"]).OfType<JsonObject>().SelectMany(Walk))
            {
                yield return inner;
            }
        }
    }

    private static FlowRunInputs Inputs(JsonObject run, Dictionary<string, IReadOnlyList<FlowMock>> mocks) =>
        new(JsonNode.Parse("""{"body":{}}"""), mocks, new Dictionary<string, JsonNode?>(), (string)run["utcNow"]!, null, null);

    private static Dictionary<string, IReadOnlyList<FlowMock>> ListRowsMock(JsonObject run) =>
        new() { ["List_rows"] = [new FlowMock("Succeeded", run["actions"]!["List_rows"]!["outputs"]!.DeepClone(), null)] };

    [Fact]
    public void The_record_covers_every_action()
    {
        var recorded = ((JsonObject)Run()["actions"]!).Select(p => p.Key).Order(StringComparer.Ordinal);
        var defined = Walk((JsonObject)Definition()["actions"]!).Select(a => a.Name).Order(StringComparer.Ordinal);

        Assert.Equal(defined, recorded);
    }

    // An action that runs after a removed action runs after what the removed one ran after.
    private static JsonObject Rewire(JsonObject runAfter, Dictionary<string, JsonObject> removed)
    {
        var result = new JsonObject();
        foreach (var (name, statuses) in runAfter)
        {
            if (removed.TryGetValue(name, out var before))
            {
                foreach (var (inherited, inheritedStatuses) in Rewire(before, removed).Where(p => !result.ContainsKey(p.Key)))
                {
                    result[inherited] = inheritedStatuses?.DeepClone();
                }
            }
            else
            {
                result[name] = statuses?.DeepClone();
            }
        }

        return result;
    }

    // A Compose's outputs are its value and a ParseJson's are {"body": ...} in both the run history and the simulator, so they are compared as they are.
    private static string Show(JsonNode? node) => node?.ToJsonString() ?? "null";

    // result('Try') in the cloud lists name, code, status, inputs, outputs and error with a message the simulator words its own way: compare name, status and error code.
    private static JsonNode? Summary(JsonNode? result) =>
        result is JsonArray items
            ? new JsonArray(items.Select(i => (JsonNode?)new JsonObject { ["name"] = i?["name"]?.DeepClone(), ["status"] = i?["status"]?.DeepClone(), ["errorCode"] = i?["error"]?["code"]?.DeepClone() }).ToArray())
            : result?.DeepClone();

    [Fact]
    public void The_simulator_matches_the_cloud_run_on_every_case_it_runs()
    {
        var run = Run();
        var unsupported = Unsupported().Concat(NotCompared).ToArray();
        var definition = (JsonObject)Definition().DeepClone();
        var actions = (JsonObject)definition["actions"]!;
        var removed = unsupported.Where(actions.ContainsKey).ToDictionary(name => name, name => (JsonObject)actions[name]!["runAfter"]!.DeepClone());
        foreach (var name in removed.Keys)
        {
            actions.Remove(name);
        }

        foreach (var (_, action) in actions)
        {
            action!["runAfter"] = Rewire((JsonObject)action["runAfter"]!, removed);
        }

        var types = Walk((JsonObject)Definition()["actions"]!).ToDictionary(a => a.Name, a => a.Type);
        var result = FlowSimulator.Run(definition, Inputs(run, ListRowsMock(run)));

        Assert.True(result.Outcome == FlowRunOutcome.Finished, result.Message);
        var mismatches = new List<string>();
        if (result.Status != (string)run["runStatus"]!)
        {
            mismatches.Add($"run status: simulator {result.Status} vs cloud {run["runStatus"]}");
        }

        foreach (var (name, node) in (JsonObject)run["actions"]!)
        {
            if (unsupported.Contains(name))
            {
                continue;
            }

            var cloud = (JsonObject)node!;
            if (!result.Calls.TryGetValue(name, out var calls))
            {
                mismatches.Add($"{name}: the simulator never ran it, the cloud gave {cloud["status"]}");
                continue;
            }

            var call = calls[^1];
            if (call.Status != (string)cloud["status"]!)
            {
                mismatches.Add($"{name}: status simulator {call.Status} vs cloud {cloud["status"]}");
                continue;
            }

            if (call.Status == "Failed" && !JsonNode.DeepEquals(call.Error?["code"], cloud["error"]?["code"]))
            {
                mismatches.Add($"{name}: error code simulator {Show(call.Error?["code"])} vs cloud {Show(cloud["error"]?["code"])}");
            }

            if (call.Status != "Succeeded")
            {
                continue;
            }

            if (name == "Catch_result")
            {
                var expected = Summary(cloud["inputs"]);
                var actual = Summary(call.Outputs);
                if (!JsonNode.DeepEquals(expected, actual))
                {
                    mismatches.Add($"{name}: result('Try') simulator {Show(actual)} vs cloud {Show(expected)}");
                }
            }
            else if (name == "List_rows")
            {
                // the service adds "accept" to the parameters it ran with; the test sends none
                var expected = (JsonObject)cloud["inputs"]!["parameters"]!.DeepClone();
                expected.Remove("accept");
                if (!JsonNode.DeepEquals(expected, call.Inputs?["parameters"]))
                {
                    mismatches.Add($"{name}: inputs simulator {Show(call.Inputs?["parameters"])} vs cloud {Show(expected)}");
                }
            }
            else if (types[name] is "Compose" or "ParseJson")
            {
                var expected = cloud["outputs"]?.DeepClone();
                if (!JsonNode.DeepEquals(expected, call.Outputs))
                {
                    mismatches.Add($"{name}: outputs simulator {Show(call.Outputs)} vs cloud {Show(expected)}");
                }
            }
        }

        Assert.True(mismatches.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    [Theory]
    [MemberData(nameof(UnsupportedNames))]
    public void Each_expected_unsupported_case_is_unsupported(string name)
    {
        var run = Run();
        var source = Definition();
        var actions = (JsonObject)source["actions"]!;
        var values = (JsonObject)actions["Values"]!.DeepClone();
        values["runAfter"] = new JsonObject();
        var action = (JsonObject)actions[name]!.DeepClone();
        action["runAfter"] = new JsonObject { ["Values"] = new JsonArray(AllStatuses.Select(s => (JsonNode?)s).ToArray()) };
        var definition = new JsonObject
        {
            ["parameters"] = source["parameters"]!.DeepClone(),
            ["actions"] = new JsonObject { ["Values"] = values, [name] = action },
        };

        var result = FlowSimulator.Run(definition, Inputs(run, []));

        Assert.True(result.Outcome == FlowRunOutcome.Unsupported, $"{result.Outcome}: {result.Message}");
    }
}