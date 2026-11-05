using System.Text.RegularExpressions;

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

    private static bool HasDll(Dictionary<object, object> step) => step.TryGetValue("env", out var env) && ((Dictionary<object, object>)env).ContainsKey("PO_DLL");

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
    public void The_apply_check_runs_after_the_plan_and_before_the_gate_and_the_apply_and_cannot_be_skipped()
    {
        var steps = Steps();
        int Index(string name) => steps.FindIndex(s => Text(s, "name") == name);
        var check = Index("Check the apply");

        Assert.True(Index("Deploy plan") < check && check < Index("Gate the apply") && Index("Gate the apply") < Index("Deploy apply"), "the check must come after Deploy plan and before the gate");
        Assert.Equal("check", Text(steps[check], "id"));
        Assert.Equal("inputs.plan-file != ''", Text(steps[check], "if"));
        Assert.DoesNotContain("always()", Text(steps[check], "if"));
        Assert.DoesNotContain("continue-on-error", steps[check].Keys.Cast<string>());
        Assert.Equal("bash", Text(steps[check], "shell"));
        Assert.Equal("bash \"$GITHUB_ACTION_PATH/action/check-apply.sh\"", Text(steps[check], "run"));
        var env = (Dictionary<object, object>)steps[check]["env"];
        Assert.Equal(["PO_PATH", "PO_PLAN_FILE", "PO_APPLY"], env.Keys.Cast<string>());
        Assert.Equal("${{ inputs.path }}", env["PO_PATH"]);
        Assert.Equal("${{ inputs.plan-file }}", env["PO_PLAN_FILE"]);
        Assert.Equal("${{ inputs.apply }}", env["PO_APPLY"]);
        Assert.Equal("${{ steps.check.outputs.environment }}", ((Dictionary<object, object>)steps[Index("Deploy apply")]["env"])["PO_ENVIRONMENT"]);
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
        Assert.Equal("${{ steps.powerops.outputs.dll }}", env["PO_DLL"]);

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
            Assert.Equal("${{ steps.powerops.outputs.dll }}", ((Dictionary<object, object>)step["env"])["PO_DLL"]);
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

    [Fact]
    public void The_install_step_comes_right_after_setup_dotnet_and_before_every_step_that_runs_powerops()
    {
        var steps = Steps();
        var install = steps.FindIndex(s => Text(s, "name") == "Install PowerOps");
        var step = steps[install];

        Assert.True(Text(steps[install - 1], "uses")?.StartsWith("actions/setup-dotnet@", StringComparison.Ordinal) == true, "setup-dotnet comes first: the package needs the .NET 10 runtime");
        Assert.All(steps.Select((s, i) => (s, i)).Where(t => HasDll(t.s)), t => Assert.True(t.i > install));
        Assert.Equal("powerops", Text(step, "id"));
        Assert.Equal("bash", Text(step, "shell"));
        Assert.Equal("${{ github.action_path }}", Text(step, "working-directory"));
        Assert.Equal("bash \"$GITHUB_ACTION_PATH/action/install.sh\"", Text(step, "run"));
        Assert.Equal(["PO_RELEASE", "PO_RELEASE_SHA256"], ((Dictionary<object, object>)step["env"]).Keys.Cast<string>());
        Assert.All(steps.Where(HasDll), s => Assert.Equal("${{ steps.powerops.outputs.dll }}", ((Dictionary<object, object>)s["env"])["PO_DLL"]));
    }

    // The release pin is one version and the SHA-256 of its package, in action.yml, each a single-quoted string: both empty until a release is pinned.
    [Fact]
    public void The_release_pin_is_both_empty_or_a_version_and_a_sha256()
    {
        var lines = File.ReadAllLines(Path.Combine(Golden.RepoRoot, "action.yml"));
        var release = Assert.Single(lines, l => l.TrimStart().StartsWith("PO_RELEASE:", StringComparison.Ordinal));
        var sha = Assert.Single(lines, l => l.TrimStart().StartsWith("PO_RELEASE_SHA256:", StringComparison.Ordinal));
        var releaseMatch = Regex.Match(release, @"^\s*PO_RELEASE: '([^']*)'$");
        var shaMatch = Regex.Match(sha, @"^\s*PO_RELEASE_SHA256: '([^']*)'$");

        Assert.True(releaseMatch.Success && shaMatch.Success, "each pin value is a single-quoted string");
        if (releaseMatch.Groups[1].Value + shaMatch.Groups[1].Value != "")
        {
            Assert.Matches(@"^[0-9]+\.[0-9]+\.[0-9]+$", releaseMatch.Groups[1].Value);
            Assert.Matches("^[0-9a-f]{64}$", shaMatch.Groups[1].Value);
        }
    }

    [Fact]
    public void The_build_restores_in_locked_mode()
    {
        var script = File.ReadAllLines(Path.Combine(Golden.RepoRoot, "action", "install.sh"));

        Assert.Single(script, l => l.TrimStart().StartsWith("dotnet build src/PowerOps.Cli -c Release -p:RestoreLockedMode=true", StringComparison.Ordinal));
    }
}