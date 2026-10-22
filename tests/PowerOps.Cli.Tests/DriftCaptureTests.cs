using System.Text.Json;

using PowerOps.Core.Drift;
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

public class DriftCaptureTests
{
    private const string Solution = "PowerOpsSeed";
    private const string FlowId = "4c8dd007-dfcb-f111-a05c-0022480697a8";

    [Fact]
    public void The_dev_capture_builds_the_unmanaged_snapshot_without_layer_reads()
    {
        var f = Capture.Fakes("clean");

        var s = f.Reader.Read(Solution, Capture.DevEnv);

        Assert.Equal(("Contoso Dev", Solution, true, "1.0.0.0", false, true), (s.Environment, s.Solution, s.Installed, s.Version, s.Managed, s.LayersRead));
        Assert.Equal([1, 29, 300, 380, 10164, 10164], s.Components.Select(c => c.Type));
        Assert.All(s.Components, c => Assert.Null(c.UnmanagedLayer));
        Assert.Equal(Snapshot.Hash("Open, then\nreview"), s.Variables.Single().ValueHash);
        Assert.Equal(["pops_SeedDataverse", "pops_SpareDataverse"], s.References.Select(r => r.LogicalName));
        Assert.All(s.References, r => Assert.True(r.Bound));
        Assert.Equal(new SnapshotFlow(Guid.Parse(FlowId), "Stamp work item status", "on"), s.Flows.Single());
        Assert.DoesNotContain(f.WebCalls, c => c.Url.AbsolutePath.EndsWith("msdyn_componentlayers"));
    }

    [Fact]
    public void The_clean_test_capture_has_a_layer_read_for_each_component_and_no_unmanaged_layer()
    {
        var f = Capture.Fakes("clean");

        var s = f.Reader.Read(Solution, Capture.TestEnv);

        Assert.Equal(("Contoso Test", true, true), (s.Environment, s.Managed, s.LayersRead));
        Assert.Equal([1, 29, 300, 380, 10164, 10164], s.Components.Select(c => c.Type));
        Assert.All(s.Components, c => Assert.False(c.UnmanagedLayer));
        Assert.Equal(6, f.WebCalls.Count(c => c.Url.AbsolutePath.EndsWith("msdyn_componentlayers")));
    }

    [Fact]
    public void The_edited_test_capture_has_the_unmanaged_layer_on_the_flow_only()
    {
        var s = Capture.Fakes("edited").Reader.Read(Solution, Capture.TestEnv);

        Assert.Equal(FlowId, Assert.Single(s.Components, c => c.UnmanagedLayer == true).Id.ToString());
        Assert.Equal(5, s.Components.Count(c => c.UnmanagedLayer == false));
    }

    [Fact]
    public void Dev_against_a_clean_test_differs_only_by_the_managed_information_item()
    {
        var fakes = Capture.Fakes("clean");

        var items = DriftCompare.Compare([fakes.Reader.Read(Solution, Capture.DevEnv), fakes.Reader.Read(Solution, Capture.TestEnv)]);

        Assert.Equal([(DriftKind.Managed, false)], items.Select(i => (i.Kind, i.Drift)));
    }

    [Fact]
    public void Dev_against_the_edited_test_names_the_unmanaged_layer_by_the_flow_name()
    {
        var fakes = Capture.Fakes("edited");

        var items = DriftCompare.Compare([fakes.Reader.Read(Solution, Capture.DevEnv), fakes.Reader.Read(Solution, Capture.TestEnv)]);

        Assert.Equal([(DriftKind.Managed, false, "Solution"), (DriftKind.UnmanagedLayer, true, "Stamp work item status")], items.Select(i => (i.Kind, i.Drift, i.Subject == Solution ? "Solution" : i.Subject)));
    }

    [Fact]
    public void The_layer_fake_answers_like_the_live_table_with_no_rows_for_a_wrong_name()
    {
        var f = Capture.Fakes("clean");
        var url = new Uri("https://contoso-test.crm.dynamics.com/api/data/v9.2/msdyn_componentlayers?fetchXml=" + Uri.EscapeDataString(DriftQueries.Layers(Guid.Parse(FlowId), "workflow")));

        Assert.Equal("""{"value":[]}""", f.WebFn(url).Body);
    }
}