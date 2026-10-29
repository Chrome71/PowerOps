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

    [Fact]
    public void The_analyze_steps_run_through_powerops_sh_so_only_error_and_note_lines_reach_the_log()
    {
        var steps = Steps();
        foreach (var name in new[] { "Job summary", "Annotations", "Write SARIF", "Gate the apply", "Apply fail-on" })
        {
            var step = steps.Single(s => Text(s, "name") == name);

            Assert.Contains("action/powerops.sh", Text(step, "run"));
            Assert.Equal("${{ github.action_path }}/src/PowerOps.Cli/bin/Release/net10.0/powerops.dll", ((Dictionary<object, object>)step["env"])["PO_DLL"]);
        }

        Assert.All(steps.Where(s => Text(s, "run") is not null), s => Assert.DoesNotContain("powerops.dll", Text(s, "run")));
    }

    [Fact]
    public void The_summary_is_capped_and_the_annotations_are_made_by_the_scripts()
    {
        var steps = Steps();

        Assert.Contains("| bash \"$GITHUB_ACTION_PATH/action/summary.sh\"", Text(steps.Single(s => Text(s, "name") == "Job summary"), "run"));
        Assert.DoesNotContain("GITHUB_STEP_SUMMARY", Text(steps.Single(s => Text(s, "name") == "Job summary"), "run"));
        var annotations = Text(steps.Single(s => Text(s, "name") == "Annotations"), "run")!;
        Assert.Contains("| bash \"$GITHUB_ACTION_PATH/action/annotate.sh\"", annotations);
        Assert.DoesNotContain("jq", annotations);
    }

    [Fact]
    public void The_apply_step_passes_the_head_repository_so_a_missing_one_is_refused()
    {
        var env = (Dictionary<object, object>)Steps().Single(s => Text(s, "name") == "Deploy apply")["env"];

        Assert.Equal("${{ github.event.pull_request.head.repo.full_name }}", env["PO_HEAD_REPO"]);
        Assert.DoesNotContain("PO_FORK", env.Keys.Cast<string>());
    }
}