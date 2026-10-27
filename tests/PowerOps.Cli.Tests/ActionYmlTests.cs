using PowerOps.Tests.Shared;

using YamlDotNet.Serialization;

namespace PowerOps.Cli.Tests;

// Reads action.yml: the real fail-on gate must run before anything is imported, and a failing flow test must fail the job.
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

    [Fact]
    public void Flow_tests_run_on_every_event_when_set_and_the_last_step_fails_the_job()
    {
        var steps = Steps();
        int Index(string name) => steps.FindIndex(s => Text(s, "name") == name);
        var test = Index("Flow tests");
        var result = Index("Apply test result");

        Assert.True(Index("Upload SARIF") < test && test < Index("Deploy plan") && test < Index("Deploy apply"), "Flow tests must run after Upload SARIF and before the deploy steps");
        Assert.Equal("test", Text(steps[test], "id"));
        Assert.Equal("inputs.tests != ''", Text(steps[test], "if"));
        Assert.Contains("action/test.sh", Text(steps[test], "run"));
        Assert.DoesNotContain("continue-on-error", steps[test].Keys.Cast<string>());
        var env = (Dictionary<object, object>)steps[test]["env"];
        Assert.Equal("${{ inputs.tests }}", env["PO_TESTS"]);
        Assert.Equal("${{ inputs.path }}", env["PO_PATH"]);
        Assert.Equal("${{ github.action_path }}/src/PowerOps.Cli/bin/Release/net10.0/powerops.dll", env["PO_DLL"]);

        Assert.Equal(steps.Count - 1, result);
        Assert.Equal("always() && steps.test.outputs.exit-code != '' && steps.test.outputs.exit-code != '0'", Text(steps[result], "if"));
        Assert.Contains("exit 1", Text(steps[result], "run"));
        Assert.DoesNotContain("continue-on-error", steps[result].Keys.Cast<string>());

        Assert.EndsWith("&& (steps.test.outputs.exit-code == '' || steps.test.outputs.exit-code == '0')", Text(steps[Index("Deploy apply")], "if"));
    }
}