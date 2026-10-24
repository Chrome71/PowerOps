using PowerOps.Tests.Shared;

using YamlDotNet.Serialization;

namespace PowerOps.Cli.Tests;

// Reads action.yml: the real fail-on gate must run before anything is imported.
public sealed class ActionYmlTests
{
    private static List<Dictionary<object, object>> Steps()
    {
        var action = new DeserializerBuilder().Build().Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(Golden.RepoRoot, "action.yml")));
        var runs = (Dictionary<object, object>)action["runs"];
        return [.. ((List<object>)runs["steps"]).Cast<Dictionary<object, object>>()];
    }

    private static string? Text(Dictionary<object, object> step, string key) => step.TryGetValue(key, out var v) ? (string)v : null;

    [Fact]
    public void The_apply_is_gated_on_fail_on_by_a_step_before_it_that_cannot_be_skipped_by_always()
    {
        var steps = Steps();
        var gate = steps.FindIndex(s => Text(s, "name") == "Gate the apply");
        var apply = steps.FindIndex(s => Text(s, "name") == "Deploy apply");

        Assert.True(gate >= 0 && gate < apply, "the gate step must come before Deploy apply");
        Assert.Equal("inputs.plan-file != ''", Text(steps[gate], "if"));
        Assert.DoesNotContain("always()", Text(steps[gate], "if"));
        Assert.DoesNotContain("always()", Text(steps[apply], "if"));
        Assert.DoesNotContain("continue-on-error", steps[gate].Keys.Cast<string>());

        var run = Text(steps[gate], "run")!;
        Assert.Contains("--fail-on \"$PO_FAIL_ON\"", run);
        Assert.Contains("--baseline", run);
        Assert.DoesNotContain("${{", run);
    }
}