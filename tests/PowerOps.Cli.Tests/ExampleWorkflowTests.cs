using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

using YamlDotNet.Serialization;

namespace PowerOps.Cli.Tests;

// Reads docs/examples/promote-dev-to-test.yml and checks what its README and plan say it must hold.
public sealed partial class ExampleWorkflowTests
{
    private static readonly Dictionary<string, object> Workflow = new DeserializerBuilder().Build()
        .Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "examples", "promote-dev-to-test.yml")));

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex Sha();

    private sealed record Step(string Job, Dictionary<object, object> Raw)
    {
        public string? Uses => Raw.TryGetValue("uses", out var v) ? (string)v : null;

        public string Run => Raw.TryGetValue("run", out var v) ? (string)v : "";

        public Dictionary<object, object> With => Raw.TryGetValue("with", out var v) ? (Dictionary<object, object>)v : [];
    }

    private static Dictionary<object, object> Jobs => (Dictionary<object, object>)Workflow["jobs"];

    private static Dictionary<object, object> Job(string name) => (Dictionary<object, object>)Jobs[name];

    private static List<Step> Steps(string? job = null) =>
        Jobs.Keys.Cast<string>().Where(j => job is null || j == job)
            .SelectMany(j => ((List<object>)Job(j)["steps"]).Select(s => new Step(j, (Dictionary<object, object>)s))).ToList();

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

    [Fact]
    public void No_run_text_holds_an_expression()
    {
        Assert.All(Steps(), s => Assert.DoesNotContain("${{", s.Run));
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