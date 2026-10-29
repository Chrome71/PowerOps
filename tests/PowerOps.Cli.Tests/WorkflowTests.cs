using System.Diagnostics;

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
        Assert.Contains("bash .github/scripts/vulnerable.sh vulnerable.json", vulnerable);
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
    public void The_release_jobs_have_exactly_the_permissions_they_need()
    {
        var jobs = Map(Load(".github", "workflows", "release.yml")["jobs"]);

        Assert.Equal(new Dictionary<string, string> { ["contents"] = "write", ["issues"] = "write", ["pull-requests"] = "write" }, Permissions(jobs["release-please"]));
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read", ["id-token"] = "write", ["attestations"] = "write" }, Permissions(jobs["publish"]));
    }

    private static Dictionary<string, string> Permissions(object job) =>
        Map(Map(job)["permissions"]).ToDictionary(p => (string)p.Key, p => (string)p.Value);

    private const string Direct = """{"version":1,"parameters":"--vulnerable --include-transitive","sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"a.csproj","frameworks":[{"framework":"net10.0","topLevelPackages":[{"id":"System.Text.Json","requestedVersion":"8.0.0","resolvedVersion":"8.0.0","vulnerabilities":[{"severity":"High","advisoryurl":"https://github.com/advisories/GHSA-hh2w-p6rv-4g7w"}]}]}]}]}""";

    private const string Transitive = """{"version":1,"parameters":"--vulnerable --include-transitive","sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"a.csproj","frameworks":[{"framework":"net10.0","transitivePackages":[{"id":"System.Text.Json","resolvedVersion":"8.0.0","vulnerabilities":[{"severity":"High","advisoryurl":"https://github.com/advisories/GHSA-hh2w-p6rv-4g7w"}]}]}]}]}""";

    private const string Clean = """{"version":1,"parameters":"--vulnerable --include-transitive","sources":["https://api.nuget.org/v3/index.json"],"projects":[{"path":"a.csproj","frameworks":[{"framework":"net10.0","topLevelPackages":[{"id":"YamlDotNet","requestedVersion":"18.1.0","resolvedVersion":"18.1.0"}],"transitivePackages":[{"id":"System.Memory","resolvedVersion":"4.6.0"}]}]},{"path":"b.csproj"}]}""";

    private const string RestoreFailure = """{"version":1,"problems":[{"text":"Restore failed.","level":"error"}]}""";

    [Theory]
    [InlineData(Direct, 1)]
    [InlineData(Transitive, 1)]
    [InlineData(Clean, 0)]
    [InlineData(RestoreFailure, 1)]
    public void The_vulnerable_package_script_fails_on_a_vulnerability_and_on_output_it_cannot_read(string json, int exitCode)
    {
        Tools.Require("bash", "jq");
        var dir = Directory.CreateTempSubdirectory("powerops-vulnerable-").FullName;
        try
        {
            var file = Path.Combine(dir, "vulnerable.json");
            File.WriteAllText(file, json);
            var start = new ProcessStartInfo(Tools.Bash) { RedirectStandardOutput = true };
            start.ArgumentList.Add(Path.Combine(Golden.RepoRoot, ".github", "scripts", "vulnerable.sh"));
            start.ArgumentList.Add(file);

            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Assert.Equal(exitCode, process.ExitCode);
            Assert.Equal(exitCode == 1 ? json : "", stdout);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Shell_scripts_stay_LF_and_no_action_script_greps_with_a_GNU_only_escape()
    {
        Assert.Contains("*.sh text eol=lf", File.ReadAllLines(Path.Combine(Golden.RepoRoot, ".gitattributes")).Select(l => l.Trim()));
        var grepLines = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "action"), "*.sh").SelectMany(File.ReadAllLines).Where(l => l.Contains("grep "));

        Assert.NotEmpty(grepLines);
        Assert.All(grepLines, l => Assert.DoesNotContain("\\+", l));
    }

    [Fact]
    public void Dependabot_titles_start_with_chore_deps_and_the_playwright_example_is_watched()
    {
        var updates = ((List<object>)Load(".github", "dependabot.yml")["updates"]).Select(Map).ToList();
        var npm = updates.Single(u => Text(u, "package-ecosystem") == "npm");
        var ignore = Map(((List<object>)Map(updates.Single(u => Text(u, "package-ecosystem") == "nuget"))["ignore"]).Single());

        Assert.Equal(["github-actions", "npm", "nuget"], updates.Select(u => Text(u, "package-ecosystem")!).Order());
        Assert.All(updates, u => Assert.Equal("chore(deps)", (string)Map(u["commit-message"])["prefix"]));
        Assert.Equal("/docs/examples/playwright", Text(npm, "directory"));
        Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, "docs", "examples", "playwright", "package-lock.json")));
        Assert.Equal("JsonSchema.Net", Text(ignore, "dependency-name"));
        Assert.Equal(["version-update:semver-major", "version-update:semver-minor"], ((List<object>)ignore["update-types"]).Cast<string>());
        var npmIgnore = ((List<object>)npm["ignore"]).Select(Map).ToDictionary(i => Text(i, "dependency-name")!, i => ((List<object>)i["versions"]).Cast<string>().Single());
        Assert.Equal(new Dictionary<string, string> { ["typescript"] = ">= 7", ["@types/node"] = ">= 25" }, npmIgnore);
    }

    [Fact]
    public void The_ci_workflow_turns_test_telemetry_off()
    {
        Assert.Equal("1", (string)Map(Load(".github", "workflows", "ci.yml")["env"])["TESTINGPLATFORM_TELEMETRY_OPTOUT"]);
    }
}