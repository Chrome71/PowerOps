using PowerOps.Tests.Shared;

using YamlDotNet.Serialization;

namespace PowerOps.Cli.Tests;

// Reads .github/workflows/*.yml and .github/dependabot.yml: the supply-chain settings (M29) must stay in them.
public sealed class WorkflowTests
{
    private static Dictionary<object, object> Load(params string[] parts) =>
        new DeserializerBuilder().Build().Deserialize<Dictionary<object, object>>(File.ReadAllText(Path.Combine([Golden.RepoRoot, .. parts])));

    private static Dictionary<object, object> Map(object value) => (Dictionary<object, object>)value;

    private static string? Text(Dictionary<object, object> map, string key) => map.TryGetValue(key, out var v) ? (string)v : null;

    private static List<Dictionary<object, object>> Steps(string workflow, string job) =>
        [.. ((List<object>)Map(Map(Load(".github", "workflows", workflow)["jobs"])[job])["steps"]).Select(Map)];

    private static string ActionBuild() =>
        Text(((List<object>)Map(Load("action.yml")["runs"])["steps"]).Select(Map).Single(s => Text(s, "name") == "Build PowerOps"), "run")!;

    [Fact]
    public void The_build_job_builds_in_locked_mode_and_checks_the_actions_own_build_on_every_system()
    {
        var build = Steps("ci.yml", "build");

        Assert.Contains(build, s => Text(s, "run") == "dotnet build -c Release -p:RestoreLockedMode=true");
        Assert.Equal(ActionBuild(), Text(build.Single(s => Text(s, "name") == "The Action's build, in locked mode"), "run"));
        Assert.Contains("-p:RestoreLockedMode=true", ActionBuild());
    }

    [Fact]
    public void The_package_job_restores_locked_before_it_formats_and_packs_and_the_release_pack_is_locked_too()
    {
        var run = Steps("ci.yml", "package").Select(s => Text(s, "run")).ToList();

        var restore = run.IndexOf("dotnet restore --locked-mode");

        Assert.True(restore >= 0 && restore < run.FindIndex(r => r?.StartsWith("dotnet format", StringComparison.Ordinal) == true));
        var vulnerable = Text(Steps("ci.yml", "package").Single(s => Text(s, "name") == "Fail on a vulnerable package"), "run")!;
        Assert.Contains("--vulnerable --include-transitive", vulnerable);
        Assert.Contains("jq -e", vulnerable);
        Assert.Contains("-p:RestoreLockedMode=true", Steps("release.yml", "publish").Select(s => Text(s, "run")).Single(r => r?.StartsWith("dotnet pack", StringComparison.Ordinal) == true));
    }

    public static TheoryData<string> Workflows => ["ci.yml", "release.yml", "skills-smoke.yml"];

    private static List<Dictionary<object, object>> AllSteps(string workflow) =>
        [.. Map(Load(".github", "workflows", workflow)["jobs"]).Values.SelectMany(job => ((List<object>)Map(job)["steps"]).Select(Map))];

    [Theory]
    [MemberData(nameof(Workflows))]
    public void Every_job_has_a_timeout_and_every_checkout_keeps_the_token_out_of_git_config(string workflow)
    {
        var jobs = Map(Load(".github", "workflows", workflow)["jobs"]);
        var checkouts = AllSteps(workflow).Where(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true).ToList();

        Assert.All(jobs, job => Assert.True(Map(job.Value).ContainsKey("timeout-minutes"), $"{job.Key}"));
        Assert.NotEmpty(checkouts);
        Assert.All(checkouts, s => Assert.Equal("false", (string)Map(s["with"])["persist-credentials"]));
    }

    [Theory]
    [MemberData(nameof(Workflows))]
    public void No_run_text_holds_an_expression(string workflow)
    {
        Assert.All(AllSteps(workflow), s => Assert.DoesNotContain("${{", Text(s, "run") ?? ""));
    }

    [Fact]
    public void The_release_workflow_starts_with_no_permissions()
    {
        Assert.Empty(Map(Load(".github", "workflows", "release.yml")["permissions"]));
    }

    [Fact]
    public void Shell_scripts_stay_LF_and_no_action_script_greps_with_a_GNU_only_escape()
    {
        Assert.Contains("*.sh text eol=lf", File.ReadAllLines(Path.Combine(Golden.RepoRoot, ".gitattributes")).Select(l => l.Trim()));
        var grepLines = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "action"), "*.sh").SelectMany(File.ReadAllLines).Where(l => l.Contains("grep "));

        Assert.NotEmpty(grepLines);
        Assert.All(grepLines, l => Assert.DoesNotContain("\\+", l));
    }
}