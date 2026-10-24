using System.Diagnostics;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

using YamlDotNet.Serialization;

namespace PowerOps.Cli.Tests;

// Reads the example workflows in docs/examples (promote-dev-to-test.yml, ephemeral-pr.yml, ephemeral-sweep.yml) and checks what the
// README and the plans say they must hold.
public sealed partial class ExampleWorkflowTests
{
    private static string ExamplePath(string file) => Path.Combine(Golden.RepoRoot, "docs", "examples", file);

    private static Dictionary<string, object> Load(string file) =>
        new DeserializerBuilder().Build().Deserialize<Dictionary<string, object>>(File.ReadAllText(ExamplePath(file)));

    private static readonly Dictionary<string, object> Workflow = Load("promote-dev-to-test.yml");
    private static readonly Dictionary<string, object> Pr = Load("ephemeral-pr.yml");
    private static readonly Dictionary<string, object> SweepWorkflow = Load("ephemeral-sweep.yml");

    [GeneratedRegex(@"^\s*(-\s+)?uses:\s+[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[0-9a-f]{40} # \S")]
    private static partial Regex PinnedUse();

    [GeneratedRegex(@"^\s*(-\s+)?uses:")]
    private static partial Regex UsesLine();

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex Sha();

    private sealed record Step(string Job, Dictionary<object, object> Raw)
    {
        public string? Uses => Raw.TryGetValue("uses", out var v) ? (string)v : null;

        public string Run => Raw.TryGetValue("run", out var v) ? (string)v : "";

        public Dictionary<object, object> With => Raw.TryGetValue("with", out var v) ? (Dictionary<object, object>)v : [];
    }

    private static Dictionary<object, object> JobsOf(Dictionary<string, object> workflow) => (Dictionary<object, object>)workflow["jobs"];

    private static List<Step> StepsOf(Dictionary<string, object> workflow, string? job = null) =>
        JobsOf(workflow).Keys.Cast<string>().Where(j => job is null || j == job)
            .SelectMany(j => ((List<object>)((Dictionary<object, object>)JobsOf(workflow)[j])["steps"]).Select(s => new Step(j, (Dictionary<object, object>)s))).ToList();

    private static Dictionary<object, object> Jobs => JobsOf(Workflow);

    private static Dictionary<object, object> Job(string name) => (Dictionary<object, object>)Jobs[name];

    private static List<Step> Steps(string? job = null) => StepsOf(Workflow, job);

    [Fact]
    public void Plan_and_apply_build_their_settings_file_with_the_same_script()
    {
        var scripts = new[] { "plan", "apply" }
            .Select(job => Steps(job).Where(s => s.Run.Contains("build-settings.sh")).Select(s => Regex.Match(s.Run, @"\S*build-settings\.sh").Value).Single())
            .ToList();

        Assert.Equal(scripts[0], scripts[1]);
        Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, "docs", "examples", "powerops", "build-settings.sh")));
    }

    [Fact]
    public void Every_job_that_uses_the_powerops_action_uses_the_same_ref()
    {
        var refs = Steps().Where(s => s.Uses?.StartsWith("Chrome71/PowerOps@", StringComparison.Ordinal) == true).Select(s => (s.Job, s.Uses)).ToList();

        Assert.Single(refs.Select(r => r.Uses).Distinct());
        Assert.Contains(refs, r => r.Job == "plan");
        Assert.Contains(refs, r => r.Job == "apply");
    }

    [Fact]
    public void Every_job_installs_the_one_pac_version_set_in_the_workflow_env()
    {
        Assert.True(((Dictionary<object, object>)Workflow["env"]).ContainsKey("PAC_VERSION"));
        var installs = Steps().Where(s => s.Run.Contains("Microsoft.PowerApps.CLI.Tool")).ToList();

        Assert.Equal(["apply", "plan"], installs.Select(s => s.Job).Order());
        Assert.All(installs, s => Assert.Contains("--version \"$PAC_VERSION\"", s.Run));
        Assert.All(Steps(), s => Assert.False(s.Raw.TryGetValue("env", out var env) && ((Dictionary<object, object>)env).ContainsKey("PAC_VERSION")));
        Assert.All(Jobs.Values, job => Assert.False(((Dictionary<object, object>)job).TryGetValue("env", out var env) && ((Dictionary<object, object>)env).ContainsKey("PAC_VERSION")));
    }

    [Fact]
    public void Every_action_is_pinned_by_a_full_commit_sha()
    {
        var uses = Steps().Where(s => s.Uses is not null).Select(s => s.Uses!).ToList();

        Assert.NotEmpty(uses);
        Assert.All(uses, u => Assert.Matches(Sha(), u[(u.IndexOf('@') + 1)..]));
    }

    [Theory]
    [InlineData("promote-dev-to-test.yml")]
    [InlineData("ephemeral-pr.yml")]
    [InlineData("ephemeral-sweep.yml")]
    public void No_run_text_in_any_example_workflow_holds_an_expression(string file)
    {
        var steps = StepsOf(Load(file));

        Assert.NotEmpty(steps);
        Assert.All(steps, s => Assert.DoesNotContain("${{", s.Run));
    }

    [Theory]
    [InlineData("promote-dev-to-test.yml")]
    [InlineData("ephemeral-pr.yml")]
    [InlineData("ephemeral-sweep.yml")]
    public void Every_action_in_any_example_workflow_the_powerops_action_included_is_pinned_by_a_full_sha_with_a_comment(string file)
    {
        var lines = File.ReadAllLines(ExamplePath(file)).Where(l => UsesLine().IsMatch(l)).ToList();

        Assert.NotEmpty(lines);
        Assert.All(lines, l => Assert.Matches(PinnedUse(), l));
    }

    [Theory]
    [InlineData("promote-dev-to-test.yml")]
    [InlineData("ephemeral-pr.yml")]
    [InlineData("ephemeral-sweep.yml")]
    public void Every_script_an_example_workflow_calls_exists(string file)
    {
        var scripts = Regex.Matches(string.Join("\n", StepsOf(Load(file)).Select(s => s.Run)), @"docs/examples/powerops/[A-Za-z0-9._-]+\.sh").Select(m => m.Value).Distinct().ToList();

        Assert.NotEmpty(scripts);
        Assert.All(scripts, script => Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, script.Replace('/', Path.DirectorySeparatorChar))), script));
    }

    // ---- ephemeral-pr.yml ----

    private static Dictionary<object, object> PrJob => (Dictionary<object, object>)JobsOf(Pr)["ephemeral"];

    private static List<Step> PrSteps => StepsOf(Pr, "ephemeral");

    private static string Text(Dictionary<object, object> map, string key) => (string)map[key];

    private static Dictionary<object, object> Map(object value) => (Dictionary<object, object>)value;

    [Theory]
    [InlineData("ephemeral-pr.yml")]
    [InlineData("ephemeral-sweep.yml")]
    public void The_recipe_says_in_its_first_line_that_it_was_not_run(string file)
    {
        Assert.Equal("# not run end to end; see docs/examples/README.md", File.ReadLines(ExamplePath(file)).First());
    }

    [Fact]
    public void The_delete_step_is_the_last_step_of_the_create_steps_job_and_always_runs()
    {
        var create = Assert.Single(StepsOf(Pr), s => s.Run.Contains("pac admin create", StringComparison.Ordinal));
        var delete = Assert.Single(StepsOf(Pr), s => s.Run.Contains("ephemeral-env.sh delete", StringComparison.Ordinal));
        var steps = PrSteps;

        Assert.Single(JobsOf(Pr));
        Assert.Equal(create.Job, delete.Job);
        Assert.Same(delete.Raw, steps[^1].Raw);
        Assert.Contains("always()", Text(delete.Raw, "if"));
        Assert.False(delete.Raw.ContainsKey("continue-on-error"));
        Assert.False(create.Raw.ContainsKey("continue-on-error"));
        Assert.Equal("75", Text(create.Raw, "timeout-minutes"));
        Assert.Equal("10", Text(delete.Raw, "timeout-minutes"));
        // The first sign-in may be hours old: the step signs in again, with no environment.
        Assert.Contains("pac auth create --githubFederated", delete.Run);
        Assert.DoesNotContain("--environment", delete.Run);
        Assert.True(delete.Run.IndexOf("pac auth create", StringComparison.Ordinal) < delete.Run.IndexOf("ephemeral-env.sh delete", StringComparison.Ordinal));
    }

    [Fact]
    public void The_job_guard_covers_forks_and_dependabot_and_sits_on_the_job()
    {
        var guard = Text(PrJob, "if");

        Assert.Contains("github.event.pull_request.head.repo.full_name == github.repository", guard);
        Assert.Contains("github.actor != 'dependabot[bot]'", guard);
        // A person re-running a Dependabot PR becomes the actor, so the PR's author counts too.
        Assert.Contains("github.event.pull_request.user.login != 'dependabot[bot]'", guard);
        Assert.All(PrSteps.Where(s => s.Raw.ContainsKey("if")), s => Assert.Equal("Delete the environment", (string)s.Raw["name"]));
    }

    [Fact]
    public void The_sweep_step_of_the_pr_workflow_cannot_fail_the_run_and_no_other_step_may_continue_on_error()
    {
        var sweep = Assert.Single(PrSteps, s => s.Run.Contains("ephemeral-sweep.sh", StringComparison.Ordinal));

        Assert.Matches(@"ephemeral-sweep\.sh \|\| echo ""::warning::", sweep.Run);
        Assert.All(PrSteps, s => Assert.False(s.Raw.ContainsKey("continue-on-error"), (string)s.Raw.GetValueOrDefault("name", s.Uses ?? "")!));
    }

    [Fact]
    public void The_job_waits_for_an_approval_has_least_privilege_and_bounded_time_and_one_run_per_pr()
    {
        var permissions = Map(PrJob["permissions"]);
        var concurrency = Map(PrJob["concurrency"]);
        var on = Map(Map(Pr["on"])["pull_request"]);

        Assert.Equal("ephemeral", Text(PrJob, "environment"));
        Assert.Equal("150", Text(PrJob, "timeout-minutes"));
        Assert.Equal(["actions", "contents", "id-token"], permissions.Keys.Cast<string>().Order());
        Assert.Equal("write", Text(permissions, "id-token"));
        Assert.Equal("read", Text(permissions, "contents"));
        Assert.Equal("read", Text(permissions, "actions"));
        Assert.Contains("github.event.pull_request.number", Text(concurrency, "group"));
        Assert.Equal("true", Text(concurrency, "cancel-in-progress"));
        Assert.Equal(["opened", "reopened", "synchronize"], ((List<object>)on["types"]).Cast<string>().Order());
        Assert.Equal(["deploy/**"], ((List<object>)on["paths"]).Cast<string>());
        Assert.Equal(["pull_request"], Map(Pr["on"]).Keys.Cast<string>());
    }

    [Fact]
    public void The_steps_run_in_the_order_the_recipe_needs()
    {
        var steps = PrSteps;
        int At(Func<Step, bool> match) => steps.FindIndex(s => match(s));
        var order = new[]
        {
            At(s => s.Run.Contains("Microsoft.PowerApps.CLI.Tool", StringComparison.Ordinal)),
            At(s => s.Run.Contains("pac auth create --githubFederated", StringComparison.Ordinal) && !s.Run.Contains("--environment", StringComparison.Ordinal) && s.Raw.GetValueOrDefault("if") is null),
            At(s => s.Run.Contains("ephemeral-sweep.sh", StringComparison.Ordinal)),
            At(s => s.Run.Contains("pac admin create", StringComparison.Ordinal)),
            At(s => s.Run.Contains("ephemeral-env.sh find", StringComparison.Ordinal)),
            At(s => s.Run.Contains("set-governance-config", StringComparison.Ordinal)),
            At(s => s.Run.Contains("assign-user", StringComparison.Ordinal)),
            At(s => s.Uses?.StartsWith("azure/login@", StringComparison.Ordinal) == true),
            At(s => s.Run.Contains("--environment \"$ENVIRONMENT_URL\"", StringComparison.Ordinal)),
            At(s => s.Uses?.StartsWith("Chrome71/PowerOps@", StringComparison.Ordinal) == true && !s.With.ContainsKey("plan-file")),
            At(s => s.Uses?.StartsWith("Chrome71/PowerOps@", StringComparison.Ordinal) == true && s.With.ContainsKey("plan-file")),
            At(s => s.Run.Contains("ephemeral-env.sh delete", StringComparison.Ordinal)),
        };

        Assert.All(order, i => Assert.True(i >= 0));
        Assert.Equal(order.Order(), order);
        Assert.Equal(order.Length, order.Distinct().Count());
        Assert.Contains("--protection-level Standard", steps[order[5]].Run);
        Assert.Contains("--role \"System Administrator\" --application-user", steps[order[6]].Run);
        Assert.Equal("true", Text(Map(steps[order[2]].Raw["env"]), "POWEROPS_SWEEP_DELETE"));
        Assert.Contains("github.event.pull_request.number", Text(Map(steps[order[2]].Raw["env"]), "POWEROPS_SWEEP_PR"));
    }

    [Fact]
    public void The_two_powerops_calls_use_one_ref_and_the_second_applies_the_plan_of_the_first()
    {
        var calls = PrSteps.Where(s => s.Uses?.StartsWith("Chrome71/PowerOps@", StringComparison.Ordinal) == true).ToList();
        var (plan, apply) = (calls[0], calls[1]);

        Assert.Equal(2, calls.Count);
        Assert.Equal(plan.Uses, apply.Uses);
        Assert.All(calls, c => Assert.Equal("false", (string)c.With["upload-sarif"]));
        Assert.All(calls, c => Assert.Equal("deploy", (string)c.With["path"]));
        Assert.All(calls, c => Assert.Equal("${{ steps.find.outputs.url }}", (string)c.With["target"]));
        Assert.Equal("deploy", (string)plan.With["deploy-folder"]);
        Assert.False(plan.With.ContainsKey("apply"));
        Assert.False(plan.With.ContainsKey("comment"));
        Assert.Equal("deploy/powerops-plan.json", (string)apply.With["plan-file"]);
        Assert.Equal("true", (string)apply.With["apply"]);
        Assert.Equal("none", (string)apply.With["fail-on"]);
        Assert.Single(PrSteps, s => s.With.TryGetValue("apply", out var v) && (string)v == "true");
    }

    [Fact]
    public void The_create_step_and_the_script_build_the_environment_name_the_same_way()
    {
        const string Formula = "\"$EPHEMERAL_PREFIX-$REPO_ID-pr$PR_NUMBER-$RUN_ID-$RUN_ATTEMPT\"";
        var create = PrSteps.Single(s => s.Run.Contains("pac admin create", StringComparison.Ordinal));
        var script = File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "examples", "powerops", "ephemeral-env.sh"));
        var env = Map(PrJob["env"]);

        Assert.Contains(Formula, create.Run);
        Assert.Contains(Formula, script);
        Assert.Equal(["CLIENT_ID", "EPHEMERAL_PREFIX", "PR_NUMBER", "REPO_ID", "RUN_ATTEMPT", "RUN_ID", "TENANT_ID"], env.Keys.Cast<string>().Order());
        Assert.Contains("github.repository_id", Text(env, "REPO_ID"));
        Assert.Contains("'powerops-pr'", Text(env, "EPHEMERAL_PREFIX"));
        var variables = Map(create.Raw["env"]);
        Assert.Contains("|| 'Sandbox'", Text(variables, "ENV_TYPE"));
        Assert.Contains("|| 'unitedstates'", Text(variables, "ENV_REGION"));
        Assert.Contains("|| 'USD'", Text(variables, "ENV_CURRENCY"));
        Assert.Contains("|| '1033'", Text(variables, "ENV_LANGUAGE"));
    }

    [Theory]
    [InlineData("Sandbox", true)]
    [InlineData("Trial", true)]
    [InlineData("Production", false)]
    [InlineData("Developer", false)]
    [InlineData("sandbox", false)]
    [InlineData("Sandbox ", false)]
    [InlineData("", false)]
    [InlineData("Trial;Production", false)]
    public void The_type_check_allows_only_sandbox_and_trial(string type, bool allowed)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The check is bash.");
        var create = PrSteps.Single(s => s.Run.Contains("pac admin create", StringComparison.Ordinal));
        var check = Regex.Match(create.Run, @"case ""\$ENV_TYPE"" in.*?esac", RegexOptions.Singleline).Value;
        Assert.NotEmpty(check);
        Assert.Equal(["Sandbox", "Trial"], Regex.Matches(check, @"^\s*([A-Za-z]+(?: \| [A-Za-z]+)*)\)", RegexOptions.Multiline).SelectMany(m => m.Groups[1].Value.Split(" | ")).Order());

        var start = new ProcessStartInfo("bash") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(check);
        start.Environment["ENV_TYPE"] = type;
        using var process = Process.Start(start)!;
        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.Equal(allowed, process.ExitCode == 0);
    }

    // ---- ephemeral-sweep.yml ----

    [Fact]
    public void The_sweep_runs_daily_or_by_hand_on_the_default_branch_and_deletes_only_when_told_to()
    {
        var on = Map(SweepWorkflow["on"]);
        var job = Map(JobsOf(SweepWorkflow)["sweep"]);
        var env = Map(job["env"]);
        var permissions = Map(job["permissions"]);
        var input = Map(Map(Map(on["workflow_dispatch"])["inputs"])["delete"]);

        Assert.Equal(["schedule", "workflow_dispatch"], on.Keys.Cast<string>().Order());
        Assert.Equal("false", Text(input, "default"));
        Assert.Equal("boolean", Text(input, "type"));
        Assert.Equal("github.ref == format('refs/heads/{0}', github.event.repository.default_branch)", Text(job, "if"));
        Assert.Equal(["actions", "contents", "id-token"], permissions.Keys.Cast<string>().Order());
        Assert.Equal("write", Text(permissions, "id-token"));
        Assert.Equal("read", Text(permissions, "actions"));
        Assert.Equal("read", Text(permissions, "contents"));
        Assert.Equal("${{ github.repository }}", Text(env, "GH_REPO"));
        Assert.Equal("${{ github.token }}", Text(env, "GH_TOKEN"));
        // A manual run follows its input only; the schedule follows the variable only.
        Assert.Equal(
            "${{ (github.event_name == 'workflow_dispatch' && inputs.delete == true) || (github.event_name == 'schedule' && vars.POWEROPS_SWEEP_DELETE == 'true') }}",
            Text(env, "POWEROPS_SWEEP_DELETE"));
        Assert.Contains(StepsOf(SweepWorkflow), s => s.Run.Contains("bash docs/examples/powerops/ephemeral-sweep.sh", StringComparison.Ordinal));
    }

    [Fact]
    public void The_workflow_runs_only_for_pull_requests_into_main()
    {
        var trigger = (Dictionary<object, object>)((Dictionary<object, object>)Workflow["on"])["pull_request"];

        Assert.Equal(["main"], ((List<object>)trigger["branches"]).Cast<string>());
    }

    [Fact]
    public void The_apply_waits_for_the_find_job_and_the_test_environment_and_only_it_applies()
    {
        Assert.Equal("find", Job("apply")["needs"]);
        Assert.Equal("test", Job("apply")["environment"]);
        var applying = Steps().Where(s => s.With.TryGetValue("apply", out var v) && (string)v == "true").Select(s => s.Job).ToList();

        Assert.Equal(["apply"], applying);
    }

    [Fact]
    public void Only_a_merged_pull_request_from_this_repository_is_found_and_only_its_head_commit_counts()
    {
        var find = Steps("find").Single(s => s.Run.Contains("actions/artifacts"));
        var findIf = (string)Job("find")["if"];

        Assert.Contains("github.event.pull_request.merged == true", findIf);
        Assert.Contains("github.event.pull_request.head.repo.full_name == github.repository", findIf);
        Assert.Contains("head_sha == $sha", find.Run);
        Assert.Contains("head_repository_id == $repo_id", find.Run);
        var planIf = (string)Job("plan")["if"];
        Assert.Contains("github.event.action != 'closed'", planIf);
        Assert.Contains("github.event.pull_request.head.repo.full_name == github.repository", planIf);
    }

    [Fact]
    public void The_artifact_never_includes_a_settings_file()
    {
        var uploads = Steps().Where(s => s.Uses?.StartsWith("actions/upload-artifact@", StringComparison.Ordinal) == true).ToList();

        Assert.NotEmpty(uploads);
        Assert.All(uploads, s => Assert.DoesNotContain(".settings.json", (string)s.With["path"]));
    }

    [Fact]
    public void Plan_and_apply_run_on_ubuntu_latest()
    {
        Assert.All(Jobs.Keys.Cast<string>(), job => Assert.Equal("ubuntu-latest", Job(job)["runs-on"]));
        Assert.Contains("plan", Jobs.Keys);
        Assert.Contains("apply", Jobs.Keys);
    }
}