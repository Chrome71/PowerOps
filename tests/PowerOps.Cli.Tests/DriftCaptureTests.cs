using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The scrubbed Step 1 captures (tests/fixtures/pac/drift/capture) behind fake pac and web functions.
internal static class Capture
{
    public const string DevEnv = "ENV-VALUE-DEV";
    public const string TestEnv = "ENV-VALUE-TEST";

    private static readonly string Dir = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "drift", "capture");

    public static string Read(string name) => File.ReadAllText(Path.Combine(Dir, name));

    // The layer rows of one component, as the live table answers: only for its id together with its exact component name, else an empty value list.
    private static string Layers(string state, Uri url)
    {
        var query = Uri.UnescapeDataString(url.Query);
        var (id, name) = (LiveFakes.LayerId(url), query.Split("msdyn_solutioncomponentname\" operator=\"eq\" value=\"")[1].Split('"')[0]);
        foreach (var file in Directory.GetFiles(Dir, $"test-{state}-read7-*.json"))
        {
            var body = File.ReadAllText(file);
            using var doc = JsonDocument.Parse(body);
            var row = doc.RootElement.GetProperty("value")[0];
            if (row.GetProperty("msdyn_componentid").GetString() == id && row.GetProperty("msdyn_solutioncomponentname").GetString() == name)
            {
                return body;
            }
        }

        return """{"value":[]}""";
    }

    /// <param name="state">"clean" or "edited": which captured layer reads of Test the fake returns.</param>
    public static LiveFakes Fakes(string state) => new()
    {
        PacFn = args => new PacResult(true, false, 0, Read($"{(args[^1].Contains("test", StringComparison.OrdinalIgnoreCase) ? "test" : "dev")}-read1.txt")),
        WebFn = url =>
        {
            var env = url.Host.Contains("test") ? "test" : "dev";
            if (LiveFakes.EntitySet(url) == "msdyn_componentlayers")
            {
                return env == "test" ? LiveFakes.Ok(Layers(state, url)) : throw new Xunit.Sdk.XunitException("the unmanaged Dev must not be asked for layers");
            }

            if (LiveFakes.IsOtherSolutions(url))
            {
                return LiveFakes.Ok(env == "dev" ? Read("dev-read8.json") : LiveFakes.NoRows);
            }

            var read = LiveFakes.EntitySet(url) switch
            {
                "solutions" => 2,
                "solutioncomponents" => 3,
                "workflows" => 4,
                "environmentvariabledefinitions" => 5,
                "connectionreferences" => 6,
                var other => throw new Xunit.Sdk.XunitException($"unexpected entity set {other}"),
            };
            return LiveFakes.Ok(Read($"{env}-read{read}.json"));
        },
    };
}