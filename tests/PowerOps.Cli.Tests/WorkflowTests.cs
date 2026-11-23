using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    private static string InstallScript() => File.ReadAllText(Path.Combine(Golden.RepoRoot, "action", "install.sh"));

    [Fact]
    public void The_build_job_builds_in_locked_mode_and_checks_the_actions_own_build_on_every_system()
    {
        var build = Steps("ci.yml", "build");

        Assert.Contains(build, s => Text(s, "run") == "dotnet build -c Release -p:RestoreLockedMode=true");
        // The Action's source build adds a revision argument after this line, so CI checks the line and not the whole command.
        var line = Text(build.Single(s => Text(s, "name") == "The Action's build, in locked mode"), "run")!;
        Assert.Contains("-p:RestoreLockedMode=true", line);
        Assert.Contains(InstallScript().ReplaceLineEndings("\n").Split('\n'), l => l.TrimStart().StartsWith(line, StringComparison.Ordinal));
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
        var pack = Steps("release.yml", "assets").Select(s => Text(s, "run")).Single(r => r?.StartsWith("dotnet pack", StringComparison.Ordinal) == true);
        Assert.Contains("-p:RestoreLockedMode=true", pack);
        Assert.Contains("-p:IncludeSourceRevisionInInformationalVersion=false", pack);
    }

    public static TheoryData<string> Workflows => ["ci.yml", "release.yml", "skills-smoke.yml", "pin-drift.yml", "ecosystem-watch.yml", "protected-paths.yml"];

    private static List<Dictionary<object, object>> AllSteps(string workflow) =>
        [.. Map(Load(".github", "workflows", workflow)["jobs"]).Values.SelectMany(job => ((List<object>)Map(job)["steps"]).Select(Map))];

    [Fact]
    public void The_skills_smoke_job_times_out_after_sixty_minutes()
    {
        var jobs = Map(Load(".github", "workflows", "skills-smoke.yml")["jobs"]);

        Assert.Equal("60", Text(Map(jobs["smoke"]), "timeout-minutes"));
    }

    [Fact]
    public void Each_agent_runs_every_smoke_scenario_in_exactly_one_shard()
    {
        var job = Map(Map(Load(".github", "workflows", "skills-smoke.yml")["jobs"])["smoke"]);
        var matrix = Map(Map(job["strategy"])["matrix"]);
        Assert.Equal(["agent", "shard"], matrix.Keys.Cast<string>().Order(StringComparer.Ordinal));
        var agents = ((List<object>)matrix["agent"]).Select(Map);
        Assert.Equal(["claude", "codex"], agents.Select(a => Text(a, "name")).Order(StringComparer.Ordinal));
        var shards = ((List<object>)matrix["shard"]).Select(Map).ToList();
        Assert.Equal(["part-1", "part-2"], shards.Select(s => Text(s, "name")).Order(StringComparer.Ordinal));

        var script = File.ReadLines(Path.Combine(Golden.RepoRoot, "tests", "smoke", "skills-smoke.sh"));
        var declaration = Assert.Single(script, line => line.StartsWith("scenarios=", StringComparison.Ordinal));
        var scenarios = Regex.Match(declaration, "^scenarios=\"([^\"]+)\"$");
        Assert.True(scenarios.Success, declaration);
        var expected = scenarios.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.All(shards, s => Assert.False(string.IsNullOrWhiteSpace(Text(s, "scenarios"))));
        var actual = shards.SelectMany(s => Text(s, "scenarios")!.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(expected.Distinct().Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));

        var run = Steps("skills-smoke.yml", "smoke").Single(s => Text(s, "name") == "Run the smoke test");
        Assert.Equal("${{ matrix.shard.scenarios }}", Text(Map(run["env"]), "SMOKE_ONLY"));
    }

    [Fact]
    public void Skills_smoke_exposes_each_agents_key_only_to_the_credential_check_and_smoke_run()
    {
        var workflow = Load(".github", "workflows", "skills-smoke.yml");
        var job = Map(Map(workflow["jobs"])["smoke"]);
        var steps = Steps("skills-smoke.yml", "smoke");
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read" }, Permissions(workflow));
        Assert.False(workflow.ContainsKey("env"));
        Assert.Equal(new Dictionary<object, object> { ["AGENT"] = "${{ matrix.agent.name }}", ["PACKAGE"] = "${{ matrix.agent.package }}" }, Map(job["env"]));
        var scoped = steps.Where(s => s.ContainsKey("env")).ToList();
        Assert.Equal(["Check credentials", "Run the smoke test"], scoped.Select(s => Text(s, "name")));
        foreach (var step in scoped)
        {
            var env = Map(step["env"]);
            Assert.Equal("${{ matrix.agent.name == 'claude' && secrets.ANTHROPIC_API_KEY || '' }}", Text(env, "ANTHROPIC_API_KEY"));
            Assert.Equal("${{ matrix.agent.name == 'codex' && secrets.OPENAI_API_KEY || '' }}", Text(env, "OPENAI_API_KEY"));
        }
        Assert.Collection(steps.Where(s => s.ContainsKey("uses")),
            s => Assert.Matches("^actions/checkout@[0-9a-f]{40}$", Text(s, "uses")),
            s => Assert.Matches("^actions/setup-dotnet@[0-9a-f]{40}$", Text(s, "uses")));
    }

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
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read" }, Permissions(jobs["assets"]));
        Assert.Empty(Permissions(jobs["windows"]));
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "write", ["id-token"] = "write", ["attestations"] = "write" }, Permissions(jobs["publish"]));
    }

    private const string ReleaseCreated = "needs.release-please.outputs.release_created == 'true'";

    private const string LinuxRun = """
        ./artifacts/powerops-linux-x64 --version
        unzip -q artifacts/powerops-sample.zip -d sample
        ./artifacts/powerops-linux-x64 analyze sample/powerops-sample/solution --fail-on none

        """;

    private static List<string> Needs(Dictionary<object, object> job) =>
        job["needs"] is List<object> list ? [.. list.Cast<string>()] : [(string)job["needs"]];

    private static Dictionary<object, object> With(Dictionary<object, object> step) => Map(step["with"]);

    [Fact]
    public void The_release_builds_the_files_only_for_a_new_release_checks_them_and_runs_the_linux_binary_before_keeping_them()
    {
        var jobs = Map(Load(".github", "workflows", "release.yml")["jobs"]);
        var assets = Map(jobs["assets"]);
        var steps = Steps("release.yml", "assets");
        int Index(Func<Dictionary<object, object>, bool> match) => steps.FindIndex(s => match(s));
        var checkout = Index(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true);
        var pack = Index(s => Text(s, "run")?.StartsWith("dotnet pack", StringComparison.Ordinal) == true);
        var build = Index(s => Text(s, "name") == "Build the binaries and the sample");
        var check = Index(s => Text(s, "name") == "Check the files");
        var linux = Index(s => Text(s, "name") == "Run the Linux binary");
        var keep = Index(s => Text(s, "uses")?.StartsWith("actions/upload-artifact@", StringComparison.Ordinal) == true);

        Assert.Equal("${{ steps.release.outputs.version }}", (string)Map(Map(jobs["release-please"])["outputs"])["version"]);
        Assert.Equal(["release-please"], Needs(assets));
        Assert.Equal(ReleaseCreated, Text(assets, "if"));
        Assert.Equal("${{ needs.release-please.outputs.version }}", (string)Map(assets["env"])["VERSION"]);
        Assert.Equal("${{ needs.release-please.outputs.tag_name }}", (string)With(steps[checkout])["ref"]);
        Assert.True(checkout < pack && pack < build && build < check && check < linux && linux < keep && keep == steps.Count - 1, "checkout, pack, build, check, run, keep, in that order");
        Assert.Equal("bash .github/scripts/release-assets.sh artifacts -p:IncludeSourceRevisionInInformationalVersion=false", Text(steps[build], "run"));
        Assert.Equal("bash .github/scripts/release-check.sh \"$VERSION\" artifacts", Text(steps[check], "run"));
        Assert.Equal(LinuxRun.ReplaceLineEndings("\n"), Text(steps[linux], "run"));
        Assert.Equal("release-assets", (string)With(steps[keep])["name"]);
        Assert.Equal("artifacts", (string)With(steps[keep])["path"]);
        Assert.Equal("error", (string)With(steps[keep])["if-no-files-found"]);
        // GitHub lets a failed run be re-run for 30 days; the re-run's publish needs these files.
        Assert.Equal("30", (string)With(steps[keep])["retention-days"]);
    }

    [Fact]
    public void The_release_runs_the_windows_binary_from_the_kept_files_and_refuses_another_version()
    {
        var windows = Map(Map(Load(".github", "workflows", "release.yml")["jobs"])["windows"]);
        var steps = Steps("release.yml", "windows");

        Assert.Equal("windows-latest", Text(windows, "runs-on"));
        Assert.Equal(["release-please", "assets"], Needs(windows));
        Assert.Equal(ReleaseCreated, Text(windows, "if"));
        Assert.Equal("${{ needs.release-please.outputs.version }}", (string)Map(windows["env"])["VERSION"]);
        Assert.Equal(2, steps.Count);
        Assert.StartsWith("actions/download-artifact@", Text(steps[0], "uses"));
        Assert.Equal("release-assets", (string)With(steps[0])["name"]);
        Assert.Equal("bash", Text(steps[1], "shell"));
        Assert.Equal("""
            printed="$(./powerops-win-x64.exe --version | tr -d '\r')"
            [ "$printed" = "$VERSION" ] || { echo "::error::powerops-win-x64.exe prints $printed, not $VERSION"; exit 1; }
            unzip -q powerops-sample.zip -d sample
            ./powerops-win-x64.exe analyze sample/powerops-sample/solution --fail-on none

            """.ReplaceLineEndings("\n"), Text(steps[1], "run"));
    }

    // The files a release carries, as the publish job attaches them.
    private static readonly string[] ReleaseFiles = ["\"artifacts/PowerOps.$VERSION.nupkg\"", "artifacts/powerops-linux-x64", "artifacts/powerops-win-x64.exe", "artifacts/powerops-sample.zip", "artifacts/SHA256SUMS"];

    [Fact]
    public void The_publish_job_builds_nothing_checks_the_sums_attests_them_and_attaches_the_five_files_without_a_clobber_and_never_touches_nuget()
    {
        var publish = Map(Map(Load(".github", "workflows", "release.yml")["jobs"])["publish"]);
        var steps = Steps("release.yml", "publish");

        Assert.Equal(["release-please", "assets", "windows"], Needs(publish));
        Assert.Equal(ReleaseCreated, Text(publish, "if"));
        Assert.Equal("${{ needs.release-please.outputs.tag_name }}", (string)Map(publish["env"])["TAG"]);
        Assert.Equal("${{ needs.release-please.outputs.version }}", (string)Map(publish["env"])["VERSION"]);
        Assert.Equal(4, steps.Count);
        Assert.StartsWith("actions/download-artifact@", Text(steps[0], "uses"));
        Assert.Equal("release-assets", (string)With(steps[0])["name"]);
        Assert.Equal("artifacts", (string)With(steps[0])["path"]);
        Assert.Equal("sha256sum -c SHA256SUMS", Text(steps[1], "run"));
        Assert.Equal("artifacts", Text(steps[1], "working-directory"));
        Assert.StartsWith("actions/attest-build-provenance@", Text(steps[2], "uses"));
        Assert.Equal(new Dictionary<object, object> { ["subject-checksums"] = "artifacts/SHA256SUMS" }, With(steps[2]));
        Assert.Equal("Attach to the release", Text(steps[3], "name"));
        var upload = Text(steps[3], "run")!;
        Assert.Equal($"gh release upload \"$TAG\" {string.Join(' ', ReleaseFiles)} --repo \"$GITHUB_REPOSITORY\"", upload.TrimEnd());
        Assert.DoesNotContain("--clobber", upload);
        Assert.Equal(["GH_TOKEN"], Map(steps[3]["env"]).Keys.Cast<string>());
        Assert.Equal("${{ github.token }}", (string)Map(steps[3]["env"])["GH_TOKEN"]);
        Assert.DoesNotContain(steps.Take(3), s => s.ContainsKey("env"));
        Assert.DoesNotContain(steps, s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(steps, s => (Text(s, "run") ?? "").Contains("dotnet", StringComparison.Ordinal) || (Text(s, "run") ?? "").Contains("./", StringComparison.Ordinal));
        Assert.DoesNotContain("nuget", string.Concat(AllSteps("release.yml").Select(s => (Text(s, "uses") ?? "") + (Text(s, "run") ?? ""))), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_package_job_builds_the_release_files_runs_the_linux_binary_and_keeps_them_for_the_windows_run()
    {
        var steps = Steps("ci.yml", "package");
        int Index(Func<Dictionary<object, object>, bool> match) => steps.FindIndex(s => match(s));
        var pack = Index(s => Text(s, "run")?.StartsWith("dotnet pack", StringComparison.Ordinal) == true);
        var build = Index(s => Text(s, "name") == "Build the binaries and the sample");
        var linux = Index(s => Text(s, "name") == "Run the Linux binary");
        var keep = Index(s => Text(s, "uses")?.StartsWith("actions/upload-artifact@", StringComparison.Ordinal) == true);

        Assert.True(pack >= 0 && pack < build && build < linux && linux < keep && keep == steps.Count - 1, "pack, build, run, keep, in that order");
        Assert.Equal("bash .github/scripts/release-assets.sh artifacts", Text(steps[build], "run"));
        Assert.Equal(LinuxRun.ReplaceLineEndings("\n"), Text(steps[linux], "run"));
        Assert.Equal("release-assets", (string)With(steps[keep])["name"]);
        Assert.Equal("artifacts", (string)With(steps[keep])["path"]);
        Assert.Equal("1", (string)With(steps[keep])["retention-days"]);

        var windows = Map(Map(Load(".github", "workflows", "ci.yml")["jobs"])["windows-binary"]);
        var run = Steps("ci.yml", "windows-binary");
        Assert.Equal("windows-latest", Text(windows, "runs-on"));
        Assert.Equal(["package"], Needs(windows));
        Assert.Equal(2, run.Count);
        Assert.StartsWith("actions/download-artifact@", Text(run[0], "uses"));
        Assert.Equal("release-assets", (string)With(run[0])["name"]);
        Assert.Equal("bash", Text(run[1], "shell"));
        // A build that is not a release prints its version and commit; the release job compares the same output with its version.
        Assert.Equal("""
            printed="$(./powerops-win-x64.exe --version | tr -d '\r')"
            [[ "$printed" =~ ^[0-9]+\.[0-9]+\.[0-9]+\+[0-9a-f]{40}$ ]] || { echo "::error::powerops-win-x64.exe prints $printed"; exit 1; }
            unzip -q powerops-sample.zip -d sample
            ./powerops-win-x64.exe analyze sample/powerops-sample/solution --fail-on none

            """.ReplaceLineEndings("\n"), Text(run[1], "run"));
    }

    [Fact]
    public void The_required_checks_keep_their_names_and_the_windows_binary_run_is_a_job_of_its_own()
    {
        var jobs = Map(Load(".github", "workflows", "ci.yml")["jobs"]);

        // The main ruleset requires build (ubuntu-latest), build (windows-latest), package, action and playwright-example by these names.
        Assert.Equal(["action", "build", "package", "playwright-example", "windows-binary"], jobs.Keys.Cast<string>().Order(StringComparer.Ordinal));
        Assert.All(jobs.Values, job => Assert.False(Map(job).ContainsKey("name")));
        Assert.Equal(["ubuntu-latest", "windows-latest"], ((List<object>)Map(Map(Map(jobs["build"])["strategy"])["matrix"])["os"]).Cast<string>());
        Assert.False(Map(jobs["package"]).ContainsKey("strategy"));
    }

    [Fact]
    public void Nothing_trims_the_binaries()
    {
        string[] files = [".github/scripts/release-assets.sh", "src/PowerOps.Cli/PowerOps.Cli.csproj", "src/PowerOps.Core/PowerOps.Core.csproj", "Directory.Build.props", ".github/workflows/ci.yml", ".github/workflows/release.yml"];

        Assert.All(files, f => Assert.DoesNotMatch("(?i)PublishTrimmed|TrimMode|PublishAot", File.ReadAllText(Path.Combine([Golden.RepoRoot, .. f.Split('/')]))));
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

    [Fact]
    public void The_pin_drift_workflow_runs_on_main_pushes_only_and_has_a_name_unlike_the_required_checks()
    {
        var workflow = Load(".github", "workflows", "pin-drift.yml");
        var on = Map(workflow["on"]);
        var jobs = Map(workflow["jobs"]);
        var checkout = AllSteps("pin-drift.yml").Single(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true);

        Assert.Equal(["push", "workflow_dispatch"], on.Keys.Cast<string>().Order());
        Assert.Equal(["main"], ((List<object>)Map(on["push"])["branches"]).Cast<string>());
        Assert.Equal(["pin-drift"], jobs.Keys.Cast<string>());
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read" }, Permissions(workflow));
        Assert.Equal(Text(AllSteps("ci.yml").First(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true), "uses"), Text(checkout, "uses"));
        Assert.Equal("0", (string)Map(checkout["with"])["fetch-depth"]);
    }

    // ---- ecosystem-watch.yml ----

    [Fact]
    public void The_ecosystem_watch_runs_weekly_reads_only_and_may_write_issues_and_nothing_else()
    {
        var workflow = Load(".github", "workflows", "ecosystem-watch.yml");
        var on = Map(workflow["on"]);
        var jobs = Map(workflow["jobs"]);
        var steps = Steps("ecosystem-watch.yml", "watch");
        var checkout = steps.Single(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true);
        var collect = steps.Single(s => Text(s, "id") == "collect");
        var issue = steps.Single(s => Text(s, "name") == "Update the tracking issue");
        static bool HasEnv(Dictionary<object, object> map, string key) => map.TryGetValue("env", out var env) && Map(env).ContainsKey(key);

        Assert.Equal(["schedule", "workflow_dispatch"], on.Keys.Cast<string>().Order());
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read" }, Permissions(workflow));
        Assert.Equal(["watch"], jobs.Keys.Cast<string>());
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read", ["issues"] = "write" }, Permissions(jobs["watch"]));
        Assert.DoesNotContain(steps, s => s.ContainsKey("shell"));
        Assert.False(Map(jobs["watch"]).ContainsKey("defaults") || workflow.ContainsKey("defaults"), "no default shell either");
        Assert.Equal(Text(AllSteps("ci.yml").First(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true), "uses"), Text(checkout, "uses"));
        Assert.Equal([issue], steps.Where(s => HasEnv(s, "GH_TOKEN")));
        Assert.Equal([collect], steps.Where(s => HasEnv(s, "GITHUB_TOKEN")));
        Assert.False(HasEnv(workflow, "GH_TOKEN") || HasEnv(workflow, "GITHUB_TOKEN") || HasEnv(Map(jobs["watch"]), "GH_TOKEN") || HasEnv(Map(jobs["watch"]), "GITHUB_TOKEN"));
        Assert.Equal("${{ github.token }}", (string)Map(collect["env"])["GITHUB_TOKEN"]);
        Assert.Equal("${{ github.token }}", (string)Map(issue["env"])["GH_TOKEN"]);
        Assert.Contains("bash .github/scripts/ecosystem-watch.sh", Text(collect, "run"));
        foreach (var forbidden in new[] { "git push", "git commit", "gh pr", "--update" })
        {
            Assert.All(steps, s => Assert.DoesNotContain(forbidden, Text(s, "run") ?? ""));
        }
    }

    // Logs each call's argv as [arg][arg]...; answers `issue list` from issues.json; fails `issue <STUB_FAIL>` when set.
    private const string GhStub = """
        #!/usr/bin/env bash
        { printf '[%s]' "$@"; echo; } >> "$STUB_DIR/gh.log"
        if [ "$1" = issue ] && [ "$2" = "${STUB_FAIL:-}" ]; then echo "error: stub failure" >&2; exit 1; fi
        if [ "$1" = issue ] && [ "$2" = list ]; then cat "$STUB_DIR/issues.json"; fi
        """;

    private const string List = "[issue][list][--state][open][--limit][200][--json][number,title,author]";
    private const string Edit = "[issue][edit][7][--body-file][ecosystem-watch.md]";
    private const string Close = "[issue][close][7]";
    private const string Create = "[issue][create][--title][Ecosystem watch][--body-file][ecosystem-watch.md]";

    // The login form gh prints for a bot author: api/queries_issue.go, Author.MarshalJSON, in GitHub CLI v2.103.3.
    private const string BotIssue = """[{"number":3,"title":"Ecosystem watch (old)","author":{"is_bot":true,"login":"app/github-actions"}},{"number":7,"title":"Ecosystem watch","author":{"is_bot":true,"login":"app/github-actions"}}]""";
    private const string OtherAuthor = """[{"number":5,"title":"Ecosystem watch","author":{"id":"U_kgDOAAAAAA","is_bot":false,"login":"someone","name":"Someone"}}]""";

    // Runs the issue step's own run: text as the runner does, `bash --noprofile --norc -eo pipefail {0}`, with a gh stub first on PATH.
    private static (int Exit, string[] Calls) IssueStep(string issues, string @new, string failed, string due, string? fail = null)
    {
        Tools.Require("bash", "jq");
        var run = Text(Steps("ecosystem-watch.yml", "watch").Single(s => Text(s, "name") == "Update the tracking issue"), "run")!;
        var root = Directory.CreateTempSubdirectory("powerops-ecosystem-issue-").FullName;
        try
        {
            var bin = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
            var stub = Path.Combine(bin, "gh");
            File.WriteAllText(stub, GhStub.Replace("\r\n", "\n") + "\n");
            var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
            if (posix)
            {
                File.SetUnixFileMode(stub, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            File.WriteAllText(Path.Combine(root, "issues.json"), issues);
            File.WriteAllText(Path.Combine(root, "ecosystem-watch.md"), "# Ecosystem watch, 2026-11-10 UTC\n");
            var script = Path.Combine(root, "step.sh");
            File.WriteAllText(script, run.ReplaceLineEndings("\n"));

            var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var a in new[] { "--noprofile", "--norc", "-eo", "pipefail", script })
            {
                start.ArgumentList.Add(a);
            }

            start.Environment["PATH"] = bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            start.Environment["STUB_DIR"] = root;
            start.Environment["STUB_FAIL"] = fail ?? "";
            start.Environment["GH_REPO"] = "example/repo";
            start.Environment["NEW"] = @new;
            start.Environment["FAILED"] = failed;
            start.Environment["DUE"] = due;
            using var process = Process.Start(start)!;
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();

            var log = Path.Combine(root, "gh.log");
            return (process.ExitCode, File.Exists(log) ? File.ReadAllLines(log) : []);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    public static TheoryData<string, string, string, string, string[]> IssueCases => new()
    {
        { BotIssue, "2", "0", "0", [Edit] }, // an open bot issue and something: edit
        { BotIssue, "0", "1", "0", [Edit] },
        { BotIssue, "0", "0", "0", [Edit, Close] }, // an open bot issue and nothing: edit, then close
        { "[]", "1", "0", "0", [Create] }, // no open bot issue and something: create
        { "[]", "0", "0", "0", [] }, // no open bot issue and nothing: no write
        { "[]", "0", "0", "1", [Create] }, // a due manual source is something
        { BotIssue, "0", "0", "1", [Edit] },
        { OtherAuthor, "1", "0", "0", [Create] }, // another author's issue of the same title is ignored
        { OtherAuthor, "0", "0", "0", [] },
    };

    [Theory]
    [MemberData(nameof(IssueCases))]
    public void The_ecosystem_watch_issue_step_makes_exactly_the_writes_its_case_needs(string issues, string @new, string failed, string due, string[] writes)
    {
        var run = IssueStep(issues, @new, failed, due);

        Assert.Equal(0, run.Exit);
        Assert.Equal([List, .. writes], run.Calls);
    }

    [Fact]
    public void The_ecosystem_watch_issue_step_closes_only_after_the_edit_succeeds()
    {
        var run = IssueStep(BotIssue, "0", "0", "0", fail: "edit");

        Assert.NotEqual(0, run.Exit);
        Assert.Equal([List, Edit], run.Calls);
    }

    private static string Git(string dir, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardOutput = true };
        foreach (var a in new[] { "-c", "user.name=t", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false" }.Concat(args))
        {
            start.ArgumentList.Add(a);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        return stdout;
    }

    // Writes the files, commits them and returns the new commit.
    private static string Commit(string dir, string message, params (string Path, string Text)[] files)
    {
        foreach (var (path, text) in files)
        {
            var full = Path.Combine(dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }

        Git(dir, "add", "-A");
        Git(dir, "commit", "-q", "-m", message);
        return Git(dir, "rev-parse", "HEAD");
    }

    private static (string Readme, string Action) Pins(string readme, string action) =>
        ($"```sh\ngit -C \"$po/PowerOps\" checkout {readme} # main\n```\n", $"- uses: Chrome71/PowerOps@{action} # main\n");

    // Runs pin-drift.sh in a fresh repo that `arrange` fills (it returns the README's pin and the action docs' pin); returns the exit code, stdout and the job summary.
    private static (int Exit, string Stdout, string Summary) PinDrift(Func<string, (string Readme, string Action)> arrange)
    {
        Tools.Require("bash", "git");
        var root = Directory.CreateTempSubdirectory("powerops-pin-drift-").FullName;
        try
        {
            var dir = Directory.CreateDirectory(Path.Combine(root, "repo")).FullName;
            Git(dir, "init", "-q");
            var (readme, action) = arrange(dir);
            Commit(dir, "docs: pins", ("README.md", readme), ("docs/GITHUB-ACTION.md", action));
            return Run(dir, Path.Combine(root, "summary.md"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static (int Exit, string Stdout, string Summary) Run(string dir, string summary)
    {
        var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = dir, RedirectStandardOutput = true };
        start.ArgumentList.Add(Path.Combine(Golden.RepoRoot, ".github", "scripts", "pin-drift.sh"));
        start.Environment["GITHUB_STEP_SUMMARY"] = summary;

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout, File.Exists(summary) ? File.ReadAllText(summary) : "");
    }

    private static void Warns(string expected, (int Exit, string Stdout, string Summary) run)
    {
        Assert.Equal(0, run.Exit);
        Assert.Contains("::warning::", run.Stdout);
        Assert.Contains(expected, run.Stdout);
        Assert.Contains(expected, run.Summary);
    }

    [Fact]
    public void The_pin_drift_script_is_quiet_when_every_pin_is_one_ancestor_and_only_docs_changed_since()
    {
        var run = PinDrift(dir =>
        {
            var pin = Commit(dir, "feat: code", ("src/a.cs", "a"));
            Commit(dir, "docs: later", ("docs/x.md", "x"));
            return Pins(pin, pin);
        });

        Assert.Equal(0, run.Exit);
        Assert.Equal("", run.Stdout);
        Assert.Equal("", run.Summary);
    }

    [Fact]
    public void The_pin_drift_script_warns_and_names_a_later_commit_under_src()
    {
        var later = "";
        var docs = "";
        var run = PinDrift(dir =>
        {
            var pin = Commit(dir, "feat: code", ("src/a.cs", "a"));
            later = Commit(dir, "fix: later change under src", ("src/a.cs", "b"));
            docs = Commit(dir, "docs: later", ("docs/x.md", "x"));
            return Pins(pin, pin);
        });

        Warns("1 commit(s) behind", run);
        Warns(later[..7], run);
        Assert.DoesNotContain(docs[..7], run.Stdout);
        Assert.DoesNotContain("fix: later change under src", run.Stdout);
    }

    [Fact]
    public void The_pin_drift_script_does_not_let_a_commit_subject_start_a_workflow_command()
    {
        var later = "";
        var run = PinDrift(dir =>
        {
            var pin = Commit(dir, "feat: code", ("src/a.cs", "a"));
            later = Commit(dir, "feat: x\r::error::INJECTED", ("src/a.cs", "b"));
            return Pins(pin, pin);
        });

        Warns("1 commit(s) behind", run);
        Warns(later[..7], run);
        foreach (var text in new[] { run.Stdout, run.Summary })
        {
            Assert.DoesNotContain("INJECTED", text);
            Assert.DoesNotContain(text.Split('\r', '\n'), line => line.StartsWith("::error::", StringComparison.Ordinal));
        }

        Assert.Contains(run.Stdout.Split('\n'), line => line.StartsWith("::warning::", StringComparison.Ordinal));
    }

    // A subject is untrusted: the warning and the Markdown summary may carry only hashes, numbers and the script's own text.
    [Fact]
    public void The_pin_drift_script_prints_no_commit_subject_so_it_adds_no_markdown_or_encoded_control()
    {
        var later = "";
        var run = PinDrift(dir =>
        {
            var pin = Commit(dir, "feat: code", ("src/a.cs", "a"));
            later = Commit(dir, "fix: [trusted update](https://example.invalid/update) ![badge](https://example.invalid/image) %0Aforged second line", ("src/a.cs", "b"));
            return Pins(pin, pin);
        });

        Warns("1 commit(s) behind", run);
        Warns(later[..7], run);
        foreach (var text in new[] { run.Stdout, run.Summary })
        {
            foreach (var forbidden in new[] { "[trusted", "](", "![", "%0A", "example.invalid", "forged" })
            {
                Assert.DoesNotContain(forbidden, text);
            }
        }
    }

    // inputs: and outputs: share the key plan-file, and outputs: has a key no input has: the read must stop at outputs:.
    private static string ActionYml(params string[] inputs) =>
        "name: x\ninputs:\n" + string.Concat(inputs.Select(i => $"  {i}:\n    description: d\n    default: ''\n")) +
        "outputs:\n  plan-file:\n    value: v\n  only-output:\n    value: v\nruns:\n  using: composite\n";

    [Fact]
    public void The_pin_drift_script_warns_and_names_an_input_the_pinned_action_lacks()
    {
        var run = PinDrift(dir =>
        {
            var pin = Commit(dir, "feat: action", ("action.yml", ActionYml("path", "tests", "plan-file")));
            Commit(dir, "docs: later", ("action.yml", ActionYml("path", "tests", "plan-file", "redact")));
            return Pins(pin, pin);
        });

        Warns("has no input(s) redact that action.yml on main declares", run);
        Assert.DoesNotContain("only-output", run.Stdout);
        Assert.DoesNotContain("plan-file that", run.Stdout);
    }

    [Fact]
    public void The_pin_drift_script_is_quiet_when_the_pinned_action_has_every_input_of_head()
    {
        var run = PinDrift(dir =>
        {
            var pin = Commit(dir, "feat: action", ("action.yml", ActionYml("path", "tests", "plan-file")));
            Commit(dir, "docs: later", ("docs/x.md", "x"));
            return Pins(pin, pin);
        });

        Assert.DoesNotContain("has no input", run.Stdout);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_the_pins_differ()
    {
        var run = PinDrift(dir =>
        {
            var first = Commit(dir, "feat: one", ("docs/x.md", "1"));
            var second = Commit(dir, "feat: two", ("docs/x.md", "2"));
            return Pins(first, second);
        });

        Warns("name different commits", run);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_a_pin_is_not_an_ancestor_of_head()
    {
        var run = PinDrift(dir =>
        {
            Commit(dir, "feat: base", ("docs/x.md", "1"));
            Git(dir, "checkout", "-q", "-b", "side");
            var side = Commit(dir, "feat: side", ("docs/x.md", "2"));
            Git(dir, "checkout", "-q", "-");
            return Pins(side, side);
        });

        Warns("is not an ancestor of HEAD", run);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_it_finds_no_pin()
    {
        var run = PinDrift(dir =>
        {
            Commit(dir, "feat: base", ("docs/x.md", "1"));
            return ("nothing\n", "nothing\n");
        });

        Warns("no PowerOps pin found", run);
    }

    // ---- the Action's release pin ----

    private static string ActionYml(string pin) => $"      env:\n        PO_RELEASE: '{pin}'\n        PO_RELEASE_SHA256: '{(pin == "" ? "" : new string('a', 64))}'\n";

    private static string Manifest(string version) => $"{{\n  \".\": \"{version}\"\n}}\n";

    // The pins in the docs are the last commit, so only the release pin can warn. `arrange` may add commits and tags; it returns nothing.
    private static (int Exit, string Stdout, string Summary) ReleasePin(string pin, string latest, Action<string>? arrange = null, bool tag = true)
    {
        return PinDrift(dir =>
        {
            var first = Commit(dir, "feat: code", ("src/a.cs", "a"), ("action/a.sh", "a"), ("action.yml", ActionYml(pin)), (".release-please-manifest.json", Manifest(latest)));
            if (tag)
            {
                Git(dir, "tag", "PowerOps-v0.1.0");
            }

            arrange?.Invoke(dir);
            var head = Git(dir, "rev-parse", "HEAD");
            return Pins(head, head);
        });
    }

    [Fact]
    public void The_pin_drift_script_is_quiet_about_the_release_pin_before_the_first_release_and_when_it_is_the_latest_one()
    {
        Assert.Equal("", ReleasePin("", "0.0.0").Stdout);
        Assert.Equal("", ReleasePin("0.1.0", "0.1.0").Stdout);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_a_release_exists_and_the_action_builds_from_source()
    {
        var run = ReleasePin("", "0.1.0");

        Warns("The Action runs a build from source, but the latest release is 0.1.0", run);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_the_pin_is_not_the_latest_release()
    {
        var run = ReleasePin("0.1.0", "0.2.0");

        Warns("The Action runs release 0.1.0, but the latest release is 0.2.0", run);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_the_action_scripts_changed_after_the_pinned_release()
    {
        var run = ReleasePin("0.1.0", "0.1.0", dir => Commit(dir, "fix: script", ("action/a.sh", "b")));

        Warns("The Action's scripts (action/) changed after release 0.1.0", run);
        Assert.DoesNotContain("latest release", run.Stdout);
    }

    [Fact]
    public void The_pin_drift_script_stays_quiet_when_only_other_files_changed_after_the_pinned_release()
    {
        var run = ReleasePin("0.1.0", "0.1.0", dir => Commit(dir, "docs: x", ("docs/x.md", "x")));

        Assert.Equal("", run.Stdout);
    }

    [Fact]
    public void The_pin_drift_script_warns_when_the_clone_has_no_tag_for_the_pinned_release()
    {
        var run = ReleasePin("0.1.0", "0.1.0", tag: false);

        Warns("This clone has no tag PowerOps-v0.1.0", run);
    }

    // ---- the release assets: release-assets.sh, release-check.sh and the sample ----

    // The files SHA256SUMS covers, in the order release.yml attaches them; SHA256SUMS is the fifth file.
    private static readonly string[] ChecksummedAssets = ["PowerOps.1.2.3.nupkg", "powerops-linux-x64", "powerops-win-x64.exe", "powerops-sample.zip"];

    private static string Sha256Line(string folder, string name) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(folder, name)))) + "  " + name + "\n";

    private static void WriteScript(string file, string text)
    {
        File.WriteAllText(file, text.Replace("\r\n", "\n") + "\n");
        var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
        if (posix)
        {
            File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private sealed record ScriptRun(int Exit, string Stdout, string[] Calls);

    // Runs a script of .github/scripts in root, with root/bin (the stub dotnet) first on PATH.
    private static ScriptRun RunScript(string root, string script, IEnumerable<string> args, IReadOnlyDictionary<string, string> env)
    {
        var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = root, RedirectStandardOutput = true };
        start.ArgumentList.Add(Path.Combine(Golden.RepoRoot, ".github", "scripts", script));
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment["PATH"] = Path.Combine(root, "bin") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        start.Environment["STUB_DIR"] = root;
        foreach (var (key, value) in env)
        {
            start.Environment[key] = value;
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var log = Path.Combine(root, "dotnet.log");
        return new ScriptRun(process.ExitCode, stdout, File.Exists(log) ? File.ReadAllLines(log) : []);
    }

    private static void AssertOneErrorLine(string stdout) =>
        Assert.Single(stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => l.StartsWith("::error::", StringComparison.Ordinal));

    // The stub dotnet logs its arguments; a `tool install` makes <tool-path>/powerops, which prints STUB_PRINTED, unless STUB_INSTALL_EXIT is set.
    private const string InstallStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/dotnet.log"
        if [ "${STUB_INSTALL_EXIT:-0}" != "0" ]; then echo "error: no such package" >&2; exit "$STUB_INSTALL_EXIT"; fi
        while [ $# -gt 0 ]; do
          if [ "$1" = "--tool-path" ]; then path="$2"; fi
          shift
        done
        mkdir -p "$path"
        printf '#!/usr/bin/env bash\necho "%s"\n' "$STUB_PRINTED" > "$path/powerops"
        chmod +x "$path/powerops"
        """;

    private sealed record CheckRun(int Exit, string Stdout, string[] Calls, bool FolderUnchanged);

    // A folder of the five release files for version 1.2.3; powerops-linux-x64 prints binaryPrinted. `arrange` changes it after SHA256SUMS is written.
    private static CheckRun ReleaseCheck(string version, string printed, bool package = true, string installExit = "0", string? binaryPrinted = null, Action<string>? arrange = null)
    {
        Tools.Require("bash", "sha256sum");
        var root = Directory.CreateTempSubdirectory("powerops-release-check-").FullName;
        try
        {
            WriteScript(Path.Combine(Directory.CreateDirectory(Path.Combine(root, "bin")).FullName, "dotnet"), InstallStub);
            var folder = Directory.CreateDirectory(Path.Combine(root, "artifacts")).FullName;
            File.WriteAllText(Path.Combine(folder, "PowerOps.1.2.3.nupkg"), "the package");
            WriteScript(Path.Combine(folder, "powerops-linux-x64"), $"#!/usr/bin/env bash\necho \"{binaryPrinted ?? printed}\"");
            File.WriteAllText(Path.Combine(folder, "powerops-win-x64.exe"), "the exe");
            File.WriteAllText(Path.Combine(folder, "powerops-sample.zip"), "the sample");
            File.WriteAllText(Path.Combine(folder, "SHA256SUMS"), string.Concat(ChecksummedAssets.Select(a => Sha256Line(folder, a))));
            if (!package)
            {
                File.Delete(Path.Combine(folder, "PowerOps.1.2.3.nupkg"));
            }

            arrange?.Invoke(folder);
            string Snapshot() => string.Join("\n", Directory.GetFiles(folder).Order(StringComparer.Ordinal).Select(f => Path.GetFileName(f) + " " + Sha256Line(folder, Path.GetFileName(f))));
            var before = Snapshot();

            var run = RunScript(root, "release-check.sh", [version, "artifacts"], new Dictionary<string, string> { ["STUB_PRINTED"] = printed, ["STUB_INSTALL_EXIT"] = installExit });
            return new CheckRun(run.Exit, run.Stdout, run.Calls, Snapshot() == before);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void The_release_check_passes_the_five_files_installs_the_package_alone_and_writes_nothing()
    {
        var run = ReleaseCheck("1.2.3", "1.2.3");

        Assert.Equal(0, run.Exit);
        Assert.Equal("", run.Stdout);
        var call = Assert.Single(run.Calls);
        Assert.Matches(@"^tool install --tool-path \S+ --source artifacts PowerOps --version 1\.2\.3$", call);
        Assert.True(run.FolderUnchanged);
    }

    [Theory]
    [InlineData("1.2", "1.2", true, "0")] // not major.minor.patch
    [InlineData("v1.2.3", "1.2.3", true, "0")]
    [InlineData("1.2.3-rc.1", "1.2.3-rc.1", true, "0")]
    [InlineData("1.2.3", "1.2.3", false, "0")] // no package
    [InlineData("1.2.3", "1.2.3", true, "1")] // the install fails
    [InlineData("1.2.3", "1.2.4", true, "0")] // the tool prints another version
    [InlineData("1.2.3", "1.2.3+4a5b6c7d", true, "0")] // or the version with a commit
    public void The_release_check_refuses_a_bad_version_or_package_with_one_error_line(string version, string printed, bool package, string installExit)
    {
        var run = ReleaseCheck(version, printed, package, installExit);

        Assert.Equal(1, run.Exit);
        AssertOneErrorLine(run.Stdout);
        Assert.True(run.FolderUnchanged);
    }

    public static TheoryData<string> AssetProblems => ["missing", "extra", "tampered", "unlisted", "binary-version", "binary-commit"];

    [Theory]
    [MemberData(nameof(AssetProblems))]
    public void The_release_check_refuses_a_missing_extra_or_changed_file_and_a_binary_of_another_version(string problem)
    {
        Action<string>? arrange = problem switch
        {
            "missing" => f => File.Delete(Path.Combine(f, "powerops-win-x64.exe")),
            "extra" => f => File.WriteAllText(Path.Combine(f, "notes.txt"), "x"),
            "tampered" => f => File.WriteAllText(Path.Combine(f, "powerops-sample.zip"), "another sample"),
            "unlisted" => f => File.WriteAllText(Path.Combine(f, "SHA256SUMS"), string.Concat(ChecksummedAssets.SkipLast(1).Select(a => Sha256Line(f, a)))),
            _ => null,
        };
        var binary = problem switch { "binary-version" => "1.2.4", "binary-commit" => "1.2.3+4a5b6c7d", _ => null };

        var run = ReleaseCheck("1.2.3", "1.2.3", binaryPrinted: binary, arrange: arrange);

        Assert.Equal(1, run.Exit);
        AssertOneErrorLine(run.Stdout);
        Assert.True(run.FolderUnchanged);
    }

    // The stub dotnet for release-assets.sh logs its arguments. `restore` writes each project's lock at -p:NuGetLockFilePath: the committed lock
    // plus an empty section for the runtime, unless STUB_LOCK (differ, runtime) is set for the runtime STUB_LOCK_RID. `publish` writes the
    // executable and a .pdb into -o, and the build's runtimeconfig.json naming runtime STUB_RUNTIME.
    private const string AssetsStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/dotnet.log"
        cmd="$1"; shift
        while [ $# -gt 0 ]; do
          case "$1" in
            -r) rid="$2"; shift ;;
            -o) out="$2"; shift ;;
            -p:NuGetLockFilePath=*) lock="${1#-p:NuGetLockFilePath=}" ;;
          esac
          shift
        done
        if [ "$cmd" = restore ]; then
          mode=same
          if [ "$rid" = "${STUB_LOCK_RID:-}" ]; then mode="$STUB_LOCK"; fi
          for p in src/PowerOps.Cli src/PowerOps.Core; do
            mkdir -p "$(dirname "$p/$lock")"
            case "$mode" in
              same) jq --arg r "net10.0/$rid" '.dependencies[$r] = {}' "$p/packages.lock.json" ;;
              differ) jq --arg r "net10.0/$rid" '.dependencies[$r] = {} | .dependencies["net10.0"].Extra = {"type": "Transitive", "resolved": "1.0.0"}' "$p/packages.lock.json" ;;
              runtime) jq --arg r "net10.0/$rid" '.dependencies[$r] = {"runtime.extra": {"type": "Transitive", "resolved": "1.0.0"}}' "$p/packages.lock.json" ;;
            esac > "$p/$lock"
          done
          exit 0
        fi
        bin="src/PowerOps.Cli/bin/Release/net10.0/$rid"
        mkdir -p "$out" "$bin"
        if [ "$rid" = win-x64 ]; then exe=powerops.exe; else exe=powerops; fi
        echo "binary $rid" > "$out/$exe"
        echo "symbols" > "$out/powerops.pdb"
        printf '{"runtimeOptions":{"includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"%s"}]}}' "${STUB_RUNTIME-10.0.14}" > "$bin/powerops.runtimeconfig.json"
        """;

    private sealed record AssetsRun(int Exit, string Stdout, string[] Calls, string[] Files, string? Sums, int SumsCheckExit, string[] ZipEntries, string? ZipReadme);

    private static void CopyTree(string from, string to)
    {
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    // A temp root with the lock files, the two fixtures and the sample README; artifacts/ holds PowerOps.1.2.3.nupkg unless arrange changes it.
    private static AssetsRun ReleaseAssets(IReadOnlyDictionary<string, string>? env = null, Action<string>? arrange = null)
    {
        Tools.Require("bash", "sha256sum", "jq", "zip");
        var root = Directory.CreateTempSubdirectory("powerops-release-assets-").FullName;
        try
        {
            WriteScript(Path.Combine(Directory.CreateDirectory(Path.Combine(root, "bin")).FullName, "dotnet"), AssetsStub);
            foreach (var project in new[] { "PowerOps.Cli", "PowerOps.Core" })
            {
                Directory.CreateDirectory(Path.Combine(root, "src", project));
                File.Copy(Path.Combine(Golden.RepoRoot, "src", project, "packages.lock.json"), Path.Combine(root, "src", project, "packages.lock.json"));
            }

            CopyTree(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v2"), Path.Combine(root, "tests", "fixtures", "powerops-seed", "v2"));
            CopyTree(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "flowtests", "seed"), Path.Combine(root, "tests", "fixtures", "flowtests", "seed"));
            Directory.CreateDirectory(Path.Combine(root, ".github", "scripts"));
            File.Copy(Path.Combine(Golden.RepoRoot, ".github", "scripts", "sample-README.md"), Path.Combine(root, ".github", "scripts", "sample-README.md"));
            var folder = Directory.CreateDirectory(Path.Combine(root, "artifacts")).FullName;
            File.WriteAllText(Path.Combine(folder, "PowerOps.1.2.3.nupkg"), "the package");
            arrange?.Invoke(folder);

            var run = RunScript(root, "release-assets.sh", ["artifacts", "-p:Extra=1"], env ?? new Dictionary<string, string>());

            var sums = Path.Combine(folder, "SHA256SUMS");
            var sumsCheck = -1;
            if (File.Exists(sums))
            {
                using var verify = Process.Start(new ProcessStartInfo("sha256sum") { WorkingDirectory = folder, RedirectStandardOutput = true, ArgumentList = { "-c", "SHA256SUMS" } })!;
                verify.StandardOutput.ReadToEnd();
                verify.WaitForExit();
                sumsCheck = verify.ExitCode;
            }

            var zip = Path.Combine(folder, "powerops-sample.zip");
            string[] entries = [];
            string? readme = null;
            if (File.Exists(zip))
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(zip);
                entries = [.. archive.Entries.Select(e => e.FullName)];
                using var reader = new StreamReader(archive.GetEntry("powerops-sample/README.md")!.Open());
                readme = reader.ReadToEnd();
            }

            return new AssetsRun(run.Exit, run.Stdout, run.Calls, [.. Directory.GetFiles(folder).Select(f => Path.GetFileName(f)!).Order(StringComparer.Ordinal)],
                File.Exists(sums) ? File.ReadAllText(sums) : null, sumsCheck, entries, readme);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string ProjectArgs(string rid) => $"-p:PublishSingleFile=true -p:EnableSingleFileAnalyzer=false -p:NuGetLockFilePath=obj/{rid}.lock.json";

    [Fact]
    public void Release_assets_restores_checks_the_lock_then_publishes_both_binaries_untrimmed_and_sums_exactly_the_four_files()
    {
        var run = ReleaseAssets();

        Assert.Equal(0, run.Exit);
        Assert.Collection(
            run.Calls,
            c => Assert.Equal($"restore src/PowerOps.Cli -r linux-x64 -p:SelfContained=true {ProjectArgs("linux-x64")}", c),
            c => Assert.Matches($@"^publish src/PowerOps\.Cli -c Release -r linux-x64 --self-contained --no-restore {Regex.Escape(ProjectArgs("linux-x64"))} -o \S+/linux-x64 -p:Extra=1$", c),
            c => Assert.Equal($"restore src/PowerOps.Cli -r win-x64 -p:SelfContained=true {ProjectArgs("win-x64")}", c),
            c => Assert.Matches($@"^publish src/PowerOps\.Cli -c Release -r win-x64 --self-contained --no-restore {Regex.Escape(ProjectArgs("win-x64"))} -o \S+/win-x64 -p:Extra=1$", c));
        Assert.DoesNotContain(run.Calls, c => c.Contains("Trim", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["linux-x64 bundles Microsoft.NETCore.App 10.0.14", "win-x64 bundles Microsoft.NETCore.App 10.0.14"], run.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries));

        Assert.Equal([.. ChecksummedAssets.Append("SHA256SUMS").Order(StringComparer.Ordinal)], run.Files);
        Assert.Equal(ChecksummedAssets, run.Sums!.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l[66..]));
        Assert.Equal(0, run.SumsCheckExit);

        var fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
        var expected = Directory.GetFiles(Path.Combine(fixtures, "powerops-seed", "v2"), "*", SearchOption.AllDirectories)
            .Select(f => "powerops-sample/solution/" + Path.GetRelativePath(Path.Combine(fixtures, "powerops-seed", "v2"), f).Replace('\\', '/'))
            .Concat(Directory.GetFiles(Path.Combine(fixtures, "flowtests", "seed")).Select(f => "powerops-sample/tests/" + Path.GetFileName(f)))
            .Append("powerops-sample/README.md")
            .Order(StringComparer.Ordinal);
        Assert.Equal(expected, run.ZipEntries.Where(e => !e.EndsWith('/')).Order(StringComparer.Ordinal));
        Assert.All(run.ZipEntries, e => Assert.StartsWith("powerops-sample/", e, StringComparison.Ordinal));
        Assert.Equal(File.ReadAllText(Path.Combine(Golden.RepoRoot, ".github", "scripts", "sample-README.md")), run.ZipReadme);
    }

    public static TheoryData<string> AssetsRefusals => ["no-package", "two-packages", "lock-differs", "runtime-section", "no-runtime-version"];

    [Theory]
    [MemberData(nameof(AssetsRefusals))]
    public void Release_assets_refuses_with_one_error_line_and_writes_no_SHA256SUMS(string problem)
    {
        var env = problem switch
        {
            "lock-differs" => new Dictionary<string, string> { ["STUB_LOCK"] = "differ", ["STUB_LOCK_RID"] = "win-x64" },
            "runtime-section" => new Dictionary<string, string> { ["STUB_LOCK"] = "runtime", ["STUB_LOCK_RID"] = "linux-x64" },
            "no-runtime-version" => new Dictionary<string, string> { ["STUB_RUNTIME"] = "" },
            _ => new Dictionary<string, string>(),
        };
        Action<string>? arrange = problem switch
        {
            "no-package" => f => File.Delete(Path.Combine(f, "PowerOps.1.2.3.nupkg")),
            "two-packages" => f => File.WriteAllText(Path.Combine(f, "PowerOps.1.2.4.nupkg"), "another package"),
            _ => null,
        };

        var run = ReleaseAssets(env, arrange);

        Assert.Equal(1, run.Exit);
        AssertOneErrorLine(run.Stdout);
        Assert.Null(run.Sums);
        // The lock is checked before that runtime is published.
        if (env.TryGetValue("STUB_LOCK_RID", out var rid))
        {
            Assert.DoesNotContain(run.Calls, c => c.StartsWith("publish", StringComparison.Ordinal) && c.Contains($"-r {rid} ", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void The_sample_readme_names_the_result_that_analyze_and_test_give_on_its_files()
    {
        var readme = File.ReadAllText(Path.Combine(Golden.RepoRoot, ".github", "scripts", "sample-README.md"));
        var fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
        var solution = Path.Combine(fixtures, "powerops-seed", "v2");
        static string LastLine(params string[] args)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            Assert.Equal(0, Program.Run(args, output, error));
            return output.ToString().ReplaceLineEndings("\n").TrimEnd().Split('\n')[^1];
        }

        Assert.Contains($"`{LastLine("analyze", solution)}`", readme);
        Assert.Contains($"`{LastLine("test", solution, "--tests", Path.Combine(fixtures, "flowtests", "seed"))}`", readme);
        Assert.Contains("PO005", readme);
        Assert.Contains("PO006", readme);
    }

    // ---- protected-paths.yml and protected-paths.sh ----

    [Fact]
    public void The_protected_paths_check_runs_mains_copy_on_pull_request_target_reads_only_and_checks_out_no_pull_request_code()
    {
        var workflow = Load(".github", "workflows", "protected-paths.yml");
        var target = Map(Map(workflow["on"])["pull_request_target"]);
        var jobs = Map(workflow["jobs"]);
        var job = Map(jobs["protected-paths"]);
        var steps = Steps("protected-paths.yml", "protected-paths");
        var text = File.ReadAllText(Path.Combine(Golden.RepoRoot, ".github", "workflows", "protected-paths.yml"));

        Assert.Equal("Protected paths", Text(workflow, "name"));
        Assert.Equal(["pull_request_target"], Map(workflow["on"]).Keys.Cast<string>());
        // edited runs it again when the base branch changes.
        Assert.Equal(["edited", "labeled", "opened", "reopened", "synchronize", "unlabeled"], ((List<object>)target["types"]).Cast<string>().Order(StringComparer.Ordinal));
        Assert.Equal(["main"], ((List<object>)target["branches"]).Cast<string>());
        // issues: read lets it read the label's issue events, to check who applied tier-L.
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "read", ["issues"] = "read", ["pull-requests"] = "read" }, Permissions(workflow));
        // The check context is the job id, and the job neither widens the permissions nor skips itself.
        Assert.Equal(["protected-paths"], jobs.Keys.Cast<string>());
        Assert.False(job.ContainsKey("permissions") || job.ContainsKey("name") || job.ContainsKey("if") || job.ContainsKey("continue-on-error"));
        Assert.Equal("ubuntu-latest", Text(job, "runs-on"));
        Assert.Equal("10", Text(job, "timeout-minutes"));
        Assert.Equal(2, steps.Count);
        Assert.Equal(Text(AllSteps("ci.yml").First(s => Text(s, "uses")?.StartsWith("actions/checkout@", StringComparison.Ordinal) == true), "uses"), Text(steps[0], "uses"));
        // No ref, repository or path: the checkout is main's own commit, never the pull request's head. The whole history lets the
        // script find the merge base; it fetches the head commit as objects and checks nothing else out.
        Assert.Equal(new Dictionary<object, object> { ["persist-credentials"] = "false", ["fetch-depth"] = "0" }, With(steps[0]));
        Assert.Equal("bash .github/scripts/protected-paths.sh", Text(steps[1], "run"));
        Assert.Equal(
            new Dictionary<object, object>
            {
                ["GH_TOKEN"] = "${{ github.token }}",
                ["GITHUB_REPOSITORY"] = "${{ github.repository }}",
                ["PR_NUMBER"] = "${{ github.event.pull_request.number }}",
                ["PR_HEAD_SHA"] = "${{ github.event.pull_request.head.sha }}",
                ["PR_BASE_REF"] = "${{ github.event.pull_request.base.ref }}",
                ["PR_BASE_SHA"] = "${{ github.event.pull_request.base.sha }}",
                ["PR_OWNER"] = "${{ github.repository_owner }}",
            },
            Map(steps[1]["env"]));
        Assert.False(steps[1].ContainsKey("continue-on-error") || steps[1].ContainsKey("if"));
        foreach (var forbidden in new[] { "secrets.", "head.ref", "head.repo", "ref:", "repository:", "path:", "write" })
        {
            Assert.DoesNotContain(forbidden, text);
        }
    }

    [Fact]
    public void The_protected_paths_script_is_LF_and_greps_nothing()
    {
        var text = File.ReadAllText(Path.Combine(Golden.RepoRoot, ".github", "scripts", "protected-paths.sh"));

        Assert.StartsWith("#!/usr/bin/env bash\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);
        Assert.DoesNotContain("grep", text);
    }

    private const string OtherSha = "fedcba9876543210fedcba9876543210fedcba98";
    private const string Owner = "repo-owner";

    private static string PrJson(bool tierL, string head, string baseSha, string baseRef = "main")
    {
        List<object> labels = [new { name = "documentation" }];
        if (tierL)
        {
            labels.Add(new { name = "tier-L" });
        }

        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["number"] = 7,
            ["head"] = new { sha = head },
            ["base"] = new { @ref = baseRef, sha = baseSha },
            ["labels"] = labels,
        });
    }

    // Issue events, oldest first, as the events API lists them: (event, label name or null for an event with no label, actor login or null).
    private static string Events(params (string Event, string? Label, string? Login)[] events) =>
        JsonSerializer.Serialize(events.Select(e =>
        {
            var item = new Dictionary<string, object?> { ["event"] = e.Event, ["actor"] = e.Login is null ? null : new { login = e.Login } };
            if (e.Label is not null)
            {
                item["label"] = new { name = e.Label, color = "ededed" };
            }

            return item;
        }));

    // Someone else labeled documentation, then the owner labeled tier-L.
    private const string OwnerTierL = """[{"event":"labeled","label":{"name":"documentation"},"actor":{"login":"someone-else"}},{"event":"labeled","label":{"name":"tier-L"},"actor":{"login":"repo-owner"}}]""";

    private sealed record GuardRun(int Exit, string Stdout, string[] GhCalls);

    // Logs its calls and fails: the offline mode may make none.
    private const string FailingGh = "#!/usr/bin/env bash\n{ printf '[%s]' \"$@\"; echo; } >> \"$STUB_DIR/gh.log\"\nexit 1";

    // Runs protected-paths.sh in cwd with root/bin, which holds the gh stub, first on PATH.
    private static GuardRun Guard(string root, string cwd, IEnumerable<string> args, IReadOnlyDictionary<string, string> env, string gh)
    {
        Tools.Require("bash", "jq", "git");
        WriteScript(Path.Combine(Directory.CreateDirectory(Path.Combine(root, "bin")).FullName, "gh"), gh);
        var log = Path.Combine(root, "gh.log");
        File.Delete(log);
        File.Delete(Path.Combine(root, "reads"));
        var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = cwd, RedirectStandardOutput = true };
        start.ArgumentList.Add(Path.Combine(Golden.RepoRoot, ".github", "scripts", "protected-paths.sh"));
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment["PATH"] = Path.Combine(root, "bin") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        start.Environment["STUB_DIR"] = root;
        foreach (var (key, value) in env)
        {
            start.Environment[key] = value;
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new GuardRun(process.ExitCode, stdout, File.Exists(log) ? File.ReadAllLines(log) : []);
    }

    // A temp git repository for the check, in Root/repo: main starts with seed.txt.
    private sealed class GuardRepo : IDisposable
    {
        public GuardRepo()
        {
            Tools.Require("bash", "jq", "git");
            Root = Directory.CreateTempSubdirectory("powerops-protected-paths-").FullName;
            Dir = Directory.CreateDirectory(Path.Combine(Root, "repo")).FullName;
            Run("init", "-q", "-b", "main");
            Seed = Commit("seed", r => r.Write("seed.txt", "seed\n"));
        }

        public string Root { get; }

        public string Dir { get; }

        public string Seed { get; }

        public string Run(params string[] args) => Git(Dir, args);

        public void Write(string path, string text) => WriteBytes(path, System.Text.Encoding.UTF8.GetBytes(text));

        public void WriteBytes(string path, byte[] bytes)
        {
            var full = Path.Combine(Dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, bytes);
        }

        public void Move(string from, string to)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Dir, to))!);
            Run("mv", from, to);
        }

        public void Remove(string path) => Run("rm", "-q", path);

        // Makes the edit, stages the whole tree (unless the edit staged by hand) and commits; returns the commit.
        public string Commit(string message, Action<GuardRepo>? edit = null, bool stage = true)
        {
            edit?.Invoke(this);
            if (stage)
            {
                Run("add", "-A");
            }

            Run("commit", "-q", "--allow-empty", "-m", message);
            return Run("rev-parse", "HEAD");
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    // main holds `before` on top of seed.txt; the pull request's branch starts there and commits `change`.
    private static (GuardRepo Repo, string Base, string Head) Change(Action<GuardRepo> change, Action<GuardRepo>? before = null)
    {
        var repo = new GuardRepo();
        var @base = before is null ? repo.Seed : repo.Commit("before", before);
        repo.Run("checkout", "-q", "-b", "pr");
        return (repo, @base, repo.Commit("change", change));
    }

    // The offline mode on a saved pull request and the repository. Its gh stub fails, and no call may reach it. A null events or
    // owner leaves its argument out.
    private static GuardRun Offline(GuardRepo repo, string pr, string head, string baseSha, string baseRef = "main", string? events = OwnerTierL,
        string? owner = Owner, string? repoDir = null)
    {
        File.WriteAllText(Path.Combine(repo.Root, "pr.json"), pr);
        List<string> args = ["--repo", repoDir ?? repo.Dir, "--pr-json", "pr.json", "--expected-head-sha", head, "--expected-base-ref", baseRef, "--expected-base-sha", baseSha];
        if (events is not null)
        {
            File.WriteAllText(Path.Combine(repo.Root, "events.json"), events);
            args.AddRange(["--events-json", "events.json"]);
        }

        if (owner is not null)
        {
            args.AddRange(["--owner", owner]);
        }

        var run = Guard(repo.Root, repo.Root, args, new Dictionary<string, string>(), FailingGh);
        Assert.Empty(run.GhCalls);
        return run;
    }

    // The offline check of a pull request whose saved JSON names its own head and base.
    private static GuardRun Check(bool tierL, Action<GuardRepo> change, Action<GuardRepo>? before = null, string? events = OwnerTierL, string? owner = Owner)
    {
        var (repo, @base, head) = Change(change, before);
        using (repo)
        {
            return Offline(repo, PrJson(tierL, head, @base), head, @base, events: events, owner: owner);
        }
    }

    private static void Passes(GuardRun run)
    {
        Assert.Equal(0, run.Exit);
        Assert.DoesNotContain("::", run.Stdout);
        Assert.EndsWith("result: pass\n", run.Stdout, StringComparison.Ordinal);
    }

    private static void Fails(GuardRun run, params string[] named)
    {
        Assert.Equal(1, run.Exit);
        Assert.Contains(run.Stdout.Split('\n'), l => l.StartsWith("::error::protected-paths: ", StringComparison.Ordinal));
        foreach (var name in named)
        {
            Assert.Contains(run.Stdout.Split('\n'), l => l.TrimStart().EndsWith(" " + name, StringComparison.Ordinal) || l.Trim() == name);
        }
    }

    private static void FailsInEveryTier(GuardRun run, params string[] named)
    {
        Fails(run, named);
        Assert.Contains("tier-L label: yes, result: fail", run.Stdout);
        Assert.DoesNotContain("someone", run.Stdout);
    }

    // Five lines, so that git sees a moved file as a rename.
    private const string Lines = "one\ntwo\nthree\nfour\nfive\n";

    public static TheoryData<string> GuardedChanges => ["add", "edit", "delete", "rename-out", "rename-in", "both-names", "self-workflow", "self-script",
        "plugin-copy", "plugin-manifest-tests"];

    // (main before the pull request, its change, a name the failure lists)
    private static (Action<GuardRepo> Before, Action<GuardRepo> Change, string Named) Guarded(string change) => change switch
    {
        "add" => (_ => { }, r => r.Write("bench/run.sh", "echo\n"), "bench/run.sh"),
        "edit" => (r => { r.Write("src/PowerOps.Cli/Program.cs", "a\n"); r.Write("src/PowerOps.Core/Deploy/DeployPlan.cs", "a\n"); },
            r => { r.Write("src/PowerOps.Cli/Program.cs", "b\n"); r.Write("src/PowerOps.Core/Deploy/DeployPlan.cs", "b\n"); }, "src/PowerOps.Core/Deploy/DeployPlan.cs"),
        "delete" => (r => r.Write("docs/examples/powerops/ephemeral-sweep.sh", "#!/usr/bin/env bash\necho sweep\n"),
            r => r.Remove("docs/examples/powerops/ephemeral-sweep.sh"), "docs/examples/powerops/ephemeral-sweep.sh"),
        "rename-out" => (r => r.Write("src/PowerOps.Core/Deploy/Gate.cs", Lines), r => r.Move("src/PowerOps.Core/Deploy/Gate.cs", "src/PowerOps.Core/Plan/Gate.cs"),
            "src/PowerOps.Core/Deploy/Gate.cs"),
        "rename-in" => (r => r.Write("notes.md", Lines), r => r.Move("notes.md", "bench/notes.md"), "bench/notes.md"),
        "both-names" => (r => r.Write("docs/schemas/a.schema.json", Lines), r => r.Move("docs/schemas/a.schema.json", "docs/schemas/b.schema.json"),
            "docs/schemas/a.schema.json"),
        "self-workflow" => (r => r.Write(".github/workflows/protected-paths.yml", "a\n"), r => r.Write(".github/workflows/protected-paths.yml", "b\n"),
            ".github/workflows/protected-paths.yml"),
        "self-script" => (r => r.Write(".github/scripts/protected-paths.sh", "a\n"), r => r.Write(".github/scripts/protected-paths.sh", "b\n"),
            ".github/scripts/protected-paths.sh"),
        "plugin-copy" => (r => r.Write("plugins/codex/skills/powerops-verify/SKILL.md", "a\n"), r => r.Write("plugins/codex/skills/powerops-verify/SKILL.md", "b\n"),
            "plugins/codex/skills/powerops-verify/SKILL.md"),
        _ => (r => r.Write("tests/PowerOps.Cli.Tests/PluginManifestTests.cs", "a\n"), r => r.Write("tests/PowerOps.Cli.Tests/PluginManifestTests.cs", "b\n"),
            "tests/PowerOps.Cli.Tests/PluginManifestTests.cs"),
    };

    [Theory]
    [MemberData(nameof(GuardedChanges))]
    public void The_protected_paths_script_fails_a_guarded_change_without_tier_L_and_passes_it_with_tier_L(string change)
    {
        var (before, edit, named) = Guarded(change);
        var without = Check(false, edit, before);

        Fails(without, named);
        if (change is "rename-out" or "rename-in" or "both-names")
        {
            // git found the rename: one changed file, checked by both of its names.
            Assert.Contains("protected-paths: 1 changed file(s)", without.Stdout);
        }

        Passes(Check(true, edit, before));
    }

    [Fact]
    public void The_protected_paths_script_passes_an_unprotected_change_without_tier_L()
    {
        var run = Check(false,
            r =>
            {
                r.Write("src/PowerOps.Cli/Program.cs", "b\n");
                r.Write("docs/COMMANDS.md", "b\n");
                r.Write("README.md", "b\n");
                r.Write("skills/powerops-plan/SKILL.md", "b\n");
                r.Move("docs/b.md", "docs/a.md");
                r.Remove("docs/old.md");
            },
            r => { r.Write("docs/b.md", Lines); r.Write("docs/old.md", "old\n"); });
        var empty = Check(false, _ => { });

        Passes(run);
        Assert.Contains("6 changed file(s), 0 protected path(s)", run.Stdout);
        Passes(empty);
        Assert.Contains("0 changed file(s)", empty.Stdout);
    }

    [Theory]
    [InlineData("bench", true)]
    [InlineData("bench/run.sh", true)]
    [InlineData(".github/CODEOWNERS", true)]
    [InlineData(".github/workflows/ci.yml", true)]
    [InlineData("CODEOWNERS", true)]
    [InlineData("AGENTS.md", true)]
    [InlineData("docs/schemas", true)]
    [InlineData("docs/schemas/analyze.schema.json", true)]
    [InlineData("src/PowerOps.Core/Deploy/DeployGate.cs", true)]
    [InlineData("src/PowerOps.Core/Output/SafeWrite.cs", true)]
    [InlineData("skills/powerops-verify", true)]
    [InlineData("skills/powerops-verify/SKILL.md", true)]
    [InlineData("skills/powerops-review-security/SKILL.md", true)]
    [InlineData("tests/PowerOps.Cli.Tests/RoleEvalTests.cs", true)]
    [InlineData("plugins/claude/skills/powerops-verify/SKILL.md", true)]
    [InlineData("plugins/claude/skills/powerops-review-security/SKILL.md", true)]
    [InlineData("plugins/codex/skills/powerops-verify/SKILL.md", true)]
    [InlineData("plugins/codex/skills/powerops-review-security/SKILL.md", true)]
    [InlineData("tests/PowerOps.Cli.Tests/PluginManifestTests.cs", true)]
    [InlineData("docs/examples/ephemeral-pr.yml", true)]
    [InlineData("docs/examples/powerops/ephemeral-env.sh", true)]
    [InlineData("benchmarks/run.sh", false)]
    [InlineData("agents.md", false)]
    [InlineData("docs/AGENTS.md", false)]
    [InlineData("docs/schemas.md", false)]
    [InlineData("src/.github/x.yml", false)]
    [InlineData("src/PowerOps.Core/DeployTools/a.cs", false)]
    [InlineData("src/PowerOps.Core/Output/SafeWriteExtra.cs", false)]
    [InlineData("skills/powerops-verify-extra/SKILL.md", false)]
    [InlineData("tests/PowerOps.Cli.Tests/RoleEvalTestsExtra.cs", false)]
    [InlineData("plugins/claude/skills/powerops-plan/SKILL.md", false)]
    [InlineData("plugins/codex/skills/powerops-verify-extra/SKILL.md", false)]
    [InlineData("plugins/claude/.claude-plugin/plugin.json", false)]
    [InlineData("tests/PowerOps.Cli.Tests/PluginManifestTestsExtra.cs", false)]
    [InlineData("docs/examples/powerops/other.sh", false)]
    [InlineData("docs/examples/playwright/ephemeral-x.yml", false)]
    public void The_protected_paths_script_matches_exactly_the_protected_paths(string path, bool guarded)
    {
        var run = Check(false, r => r.Write(path, "x\n"));

        if (guarded)
        {
            Fails(run, path);
        }
        else
        {
            Passes(run);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_protected_paths_script_fails_a_head_or_base_that_is_not_the_expected_one_even_with_tier_L(bool guarded)
    {
        var (repo, @base, head) = Change(r => r.Write(guarded ? "AGENTS.md" : "src/PowerOps.Cli/Program.cs", "x\n"));
        using (repo)
        {
            Passes(Offline(repo, PrJson(true, head, @base), head, @base));
            Fails(Offline(repo, PrJson(true, OtherSha, @base), head, @base)); // pushed after the event
            Fails(Offline(repo, PrJson(true, head, @base), OtherSha, @base));
            Fails(Offline(repo, PrJson(true, head, @base, baseRef: "release"), head, @base)); // retargeted after the event
            Fails(Offline(repo, PrJson(true, head, @base, baseRef: "release"), head, @base, baseRef: "release")); // the event's base is not main
            Fails(Offline(repo, PrJson(true, head, @base), head, @base, baseRef: "release"));
            Fails(Offline(repo, PrJson(true, head, OtherSha), head, @base)); // the base moved after the event
            Fails(Offline(repo, PrJson(true, head, @base), head, OtherSha));
            Fails(Offline(repo, PrJson(true, head, @base), head, "main"));
        }
    }

    // GitHub shows a pull request as the diff from the merge base: an edit main made after the branch is not the pull request's.
    [Fact]
    public void The_protected_paths_script_diffs_from_the_merge_base_so_a_later_main_edit_is_not_the_pull_requests()
    {
        using var repo = new GuardRepo();
        repo.Commit("main", r => r.Write("AGENTS.md", "a\n"));
        var fork = repo.Run("rev-parse", "HEAD");
        var later = repo.Commit("main moves on", r => { r.Write("AGENTS.md", "b\n"); r.Write("docs/USE.md", "see /home/someone\n"); });
        repo.Run("checkout", "-q", "-b", "pr", fork);
        var head = repo.Commit("pull request", r => r.Write("src/a.cs", "a\n"));
        var guarded = repo.Commit("pull request edits a protected path", r => r.Write(".github/x.yml", "a\n"));

        var plain = Offline(repo, PrJson(false, head, later), head, later);
        var own = Offline(repo, PrJson(false, guarded, later), guarded, later);

        Passes(plain);
        Assert.Contains("1 changed file(s), 0 protected path(s)", plain.Stdout);
        Fails(own, ".github/x.yml");
        Assert.DoesNotContain("AGENTS.md", own.Stdout);
        Assert.DoesNotContain("docs/USE.md", own.Stdout);
    }

    [Fact]
    public void The_protected_paths_script_fails_unrelated_histories_or_two_merge_bases_even_with_tier_L()
    {
        using var repo = new GuardRepo();
        repo.Run("checkout", "-q", "--orphan", "unrelated");
        var unrelated = repo.Commit("unrelated", r => r.Write("src/a.cs", "a\n"));
        // A criss-cross merge: x and y each merge the other's first commit, so they have two merge bases.
        repo.Run("checkout", "-q", "-b", "x", repo.Seed);
        var a = repo.Commit("a", r => r.Write("src/a.cs", "a\n"));
        repo.Run("checkout", "-q", "-b", "y", repo.Seed);
        var b = repo.Commit("b", r => r.Write("src/b.cs", "b\n"));
        repo.Run("merge", "-q", "--no-ff", "--no-edit", a);
        var y = repo.Run("rev-parse", "HEAD");
        repo.Run("checkout", "-q", "x");
        repo.Run("merge", "-q", "--no-ff", "--no-edit", b);
        var x = repo.Run("rev-parse", "HEAD");

        var none = Offline(repo, PrJson(true, unrelated, repo.Seed), unrelated, repo.Seed);
        var two = Offline(repo, PrJson(true, y, x), y, x);

        Fails(none);
        Assert.Contains("have no merge base", none.Stdout);
        Fails(two);
        Assert.Contains("have 2 merge bases", two.Stdout);
    }

    [Fact]
    public void The_protected_paths_script_passes_a_rename_inside_docs_and_reads_a_file_renamed_into_docs_whole()
    {
        const string old = "notes\nsee /home/someone/x\nopen file:///tmp/a\nmore\n";
        var inside = Check(false, r => r.Move("docs/a.md", "docs/moved/a.md"), r => r.Write("docs/a.md", old));

        Passes(inside);
        Assert.Contains("1 changed file(s)", inside.Stdout);
        Passes(Check(false, r => r.Move("skills/a.md", "docs/a.md"), r => r.Write("skills/a.md", old)));
        // main's own lines are not the pull request's: an added clean line, or removed lines, pass.
        Passes(Check(false, r => r.Write("docs/a.md", old + "a new line\n"), r => r.Write("docs/a.md", old)));
        Passes(Check(false, r => r.Write("docs/a.md", "notes\nmore\n"), r => r.Write("docs/a.md", old)));
        foreach (var leak in new[] { "see /home/someone/x\n", "open file://host/share/x\n", "open file:///tmp/a\n" })
        {
            var run = Check(true, r => r.Move("src/notes.txt", "docs/notes.md"), r => r.Write("src/notes.txt", "notes\n" + leak + "more\nlines\n"));

            FailsInEveryTier(run, "docs/notes.md");
            Assert.Contains("1 changed file(s)", run.Stdout); // a pure rename, with no added line in its diff
        }
    }

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0];

    [Fact]
    public void The_protected_paths_script_passes_a_clean_binary_doc_and_reads_a_nul_disguised_one_whole_even_with_tier_L()
    {
        Passes(Check(false, r => r.WriteBytes("docs/images/flow.png", Png)));
        Passes(Check(false, r => r.WriteBytes("docs/images/flow.png", [.. Png, 0, 1]), r => r.WriteBytes("docs/images/flow.png", Png)));
        foreach (var leak in new[] { "/home/someone/x", "file:///tmp/a", "file://host/share/x" })
        {
            FailsInEveryTier(Check(true, r => r.Write("docs/notes.md", "# notes\0\nsee " + leak + "\n")), "docs/notes.md");
            FailsInEveryTier(Check(true, r => r.Write("skills/a/SKILL.md", "# a\0\nclean\nsee " + leak + "\n"), r => r.Write("skills/a/SKILL.md", "# a\0\nclean\n")),
                "skills/a/SKILL.md");
        }
    }

    // A submodule entry, staged by hand; its commit need not exist. ignoreAll sets the repository's diff.ignoreSubmodules to all.
    private static GuardRun Submodule(string path, bool tierL, bool ignoreAll = false)
    {
        var (repo, @base, _) = Change(_ => { });
        using (repo)
        {
            repo.Run("update-index", "--add", "--cacheinfo", $"160000,{OtherSha},{path}");
            var head = repo.Commit("submodule", stage: false);
            if (ignoreAll)
            {
                repo.Run("config", "diff.ignoreSubmodules", "all");
            }

            return Offline(repo, PrJson(tierL, head, @base), head, @base);
        }
    }

    [Fact]
    public void The_protected_paths_script_fails_a_submodule_in_docs_or_skills_as_unscannable_even_with_tier_L()
    {
        foreach (var path in new[] { "docs/vendor", "skills/vendor", "docs" })
        {
            var run = Submodule(path, tierL: true);

            FailsInEveryTier(run, path);
            Assert.Contains("  cannot scan: " + path + "\n", run.Stdout);
        }

        Passes(Submodule("src/vendor", tierL: false));
    }

    [Fact]
    public void The_protected_paths_script_sees_a_submodule_in_docs_when_the_repository_ignores_submodules_in_diffs()
    {
        var run = Submodule("docs/vendor", tierL: true, ignoreAll: true);

        FailsInEveryTier(run, "docs/vendor");
        Assert.Contains("  cannot scan: docs/vendor\n", run.Stdout);
    }

    [Fact]
    public void The_protected_paths_script_reads_a_committed_blob_and_not_a_local_replacement_of_it()
    {
        var (repo, @base, head) = Change(r => r.Write("docs/notes.md", "see /home/someone/x\n"));
        using (repo)
        {
            var leaky = repo.Run("rev-parse", head + ":docs/notes.md");
            File.WriteAllText(Path.Combine(repo.Root, "clean.md"), "clean\n");
            var clean = repo.Run("hash-object", "-w", Path.Combine(repo.Root, "clean.md"));
            repo.Run("replace", leaky, clean);
            Assert.Equal("clean", repo.Run("cat-file", "blob", leaky)); // git reads the replacement by default

            FailsInEveryTier(Offline(repo, PrJson(true, head, @base), head, @base), "docs/notes.md");
        }
    }

    [Fact]
    public void The_protected_paths_script_fetches_no_missing_blob_of_a_partial_clone_offline()
    {
        // A clean new doc, so only a fetch of its blob could pass the check.
        var (repo, @base, head) = Change(r => r.Write("docs/new.md", "clean\n"));
        using (repo)
        {
            var blob = repo.Run("rev-parse", head + ":docs/new.md");
            var origin = Path.Combine(repo.Root, "origin.git");
            var partial = Path.Combine(repo.Root, "partial");
            Git(repo.Root, "clone", "-q", "--bare", repo.Dir, origin);
            Git(origin, "config", "uploadpack.allowFilter", "true");
            // A working origin, so the check would pass if it fetched the blob.
            Git(repo.Root, "clone", "-q", "--no-local", "--no-checkout", "--filter=blob:none", new Uri(origin).AbsoluteUri, partial);
            Assert.Equal("true", Git(partial, "config", "remote.origin.promisor"));
            bool Missing() => Git(partial, "rev-list", "--objects", "--missing=print", head).Split('\n').Contains("?" + blob);
            Assert.True(Missing());

            var run = Offline(repo, PrJson(false, head, @base), head, @base, repoDir: partial);

            Fails(run);
            Assert.Contains("cannot read a changed file's content", run.Stdout);
            Assert.True(Missing());
        }
    }

    [Theory]
    [InlineData("docs/USE.md", "see /home/someone/powerops", 1)]
    [InlineData("skills/powerops-plan/SKILL.md", "open file:///C:/plans/x.md", 1)]
    [InlineData("README.md", "[plan](file:///tmp/plan.md)", 1)]
    [InlineData("docs/USE.md", "open file://host/share/x", 1)] // a file URL with an authority
    [InlineData("skills/powerops-plan/SKILL.md", "file:///tmp/a", 1)]
    [InlineData("docs/USE.md", "see file:relative", 0)]
    [InlineData("docs/USE.md", "++ /home/someone", 1)] // the diff's line is +++ /home/someone, an added line inside a hunk
    [InlineData("skills/powerops-plan/SKILL.md", "++ file://host/share/x", 1)]
    [InlineData("README.md", "++ /home/someone", 1)]
    [InlineData("docs", "see /home/someone", 1)] // a file named docs
    [InlineData("skills", "file:///tmp/a", 1)]
    [InlineData("src/PowerOps.Core/Output/Sarif.cs", "var uri = \"file:///\";", 0)]
    [InlineData("tests/PowerOps.Core.Tests/SarifTests.cs", "Assert.StartsWith(\"file:///home/\", uri);", 0)]
    [InlineData("docs.md", "see /home/someone", 0)]
    public void The_protected_paths_script_fails_an_added_path_leak_in_docs_skills_or_the_readme_even_with_tier_L(string path, string line, int exit)
    {
        // The file is on main; the pull request adds the line to it.
        var run = Check(true, r => r.Write(path, "context\n" + line + "\nend\n"), r => r.Write(path, "context\nend\n"));

        if (exit == 1)
        {
            FailsInEveryTier(run, path);
        }
        else
        {
            Passes(run);
        }
    }

    [Fact]
    public void The_protected_paths_script_reads_a_new_file_or_link_in_docs_whole()
    {
        FailsInEveryTier(Check(true, r => r.Write("docs/new.md", "a\nsee /home/someone/x\n")), "docs/new.md");
        FailsInEveryTier(Check(true, r => r.Write("README.md", "[a](file://host/share/x)\n")), "README.md");
        FailsInEveryTier(Check(true, r =>
        {
            Directory.CreateDirectory(Path.Combine(r.Dir, "docs"));
            File.CreateSymbolicLink(Path.Combine(r.Dir, "docs", "link"), "/home/someone/x");
        }), "docs/link");
        Passes(Check(false, r => r.Write("docs/new.md", "clean\n")));
    }

    [Fact]
    public void The_protected_paths_script_lets_no_file_name_or_line_start_a_workflow_command()
    {
        var run = Check(false, r =>
        {
            r.Write(".github/x\n::error::INJECTED", "x\n");
            r.Write("docs/a%0A::warning::FORGED name.md", "::error::PATCH /home/x\n");
        });

        Fails(run, ".github/x???error??INJECTED", "docs/a?0A??warning??FORGED?name.md");
        Assert.DoesNotContain("PATCH", run.Stdout);
        Assert.DoesNotContain("%0A", run.Stdout);
        Assert.All(run.Stdout.Split('\n').Where(l => l.Contains("::", StringComparison.Ordinal)), l => Assert.StartsWith("::error::protected-paths: ", l, StringComparison.Ordinal));
        Assert.Equal(2, run.Stdout.Split('\n').Count(l => l.StartsWith("::", StringComparison.Ordinal)));
    }

    private static GuardRun Labelled(string path, string? events, string? owner = Owner) => Check(true, r => r.Write(path, "x\n"), events: events, owner: owner);

    // The label counts only when the latest labeled or unlabeled event of tier-L is the owner's labeled event.
    public static TheoryData<string, string, bool> LabelHistories => new()
    {
        { "owner", Events(("labeled", "tier-L", Owner)), true },
        { "owner, other case", Events(("labeled", "tier-L", "Repo-Owner")), true },
        { "owner, then others", Events(("labeled", "tier-L", Owner), ("labeled", "documentation", "triager"), ("assigned", null, "triager"), ("unlabeled", "documentation", "triager")), true },
        { "owner again", Events(("labeled", "tier-L", "triager"), ("unlabeled", "tier-L", Owner), ("labeled", "tier-L", Owner)), true },
        { "triager", Events(("labeled", "tier-L", "triager")), false },
        { "owner, unlabeled, triager", Events(("labeled", "tier-L", Owner), ("unlabeled", "tier-L", "triager"), ("labeled", "tier-L", "triager")), false },
        { "owner, then triager", Events(("labeled", "tier-L", Owner), ("labeled", "tier-L", "triager")), false },
        { "a name that holds the owner's", Events(("labeled", "tier-L", "repo-owner2")), false },
        { "a deleted actor", Events(("labeled", "tier-L", null)), false },
        { "last unlabeled", Events(("labeled", "tier-L", Owner), ("unlabeled", "tier-L", Owner)), false },
        { "no tier-L event", Events(("labeled", "documentation", Owner)), false },
        { "no events", "[]", false },
    };

    [Theory]
    [MemberData(nameof(LabelHistories))]
    public void The_protected_paths_script_takes_tier_L_only_when_the_owner_applied_it_last(string history, string events, bool owned)
    {
        Assert.NotEmpty(history);
        var guardedRun = Labelled("AGENTS.md", events);
        var plainRun = Labelled("src/PowerOps.Cli/Program.cs", events);

        if (owned)
        {
            Passes(guardedRun);
            Passes(plainRun);
        }
        else
        {
            // A tier-L the owner did not apply last fails, whatever the files.
            Fails(guardedRun);
            Fails(plainRun);
            Assert.Contains("the tier-L label was not applied by the owner", guardedRun.Stdout);
            Assert.DoesNotContain("triager", guardedRun.Stdout);
        }
    }

    [Fact]
    public void The_protected_paths_script_reads_tier_L_events_over_pages()
    {
        Passes(Labelled("AGENTS.md", Events(("labeled", "tier-L", "triager")) + Events(("unlabeled", "tier-L", Owner), ("labeled", "tier-L", Owner))));
        Fails(Labelled("AGENTS.md", Events(("labeled", "tier-L", Owner)) + Events(("labeled", "tier-L", "triager"))));
    }

    private const string OwnerEvent = """{"event":"labeled","label":{"name":"tier-L"},"actor":{"login":"repo-owner"}}""";

    // Each bad event comes before the owner's valid one, so only a strict read of every event fails it.
    public static TheoryData<string> MissingLabelChecks => ["no-events", "no-owner", "owner-not-login", "events-not-json", "events-empty", "events-object",
        "event-no-name", "event-false", "labeled-no-label", "label-string", "label-false", "label-name-number", "label-name-false", "actor-string",
        "actor-false", "login-number", "login-false", "event-number", "page-object"];

    [Theory]
    [MemberData(nameof(MissingLabelChecks))]
    public void The_protected_paths_script_fails_closed_when_it_cannot_check_who_applied_tier_L(string input)
    {
        static string Before(string bad) => "[" + bad + "," + OwnerEvent + "]";
        var run = input switch
        {
            "no-events" => Labelled("AGENTS.md", events: null),
            "no-owner" => Labelled("AGENTS.md", OwnerTierL, owner: null),
            "owner-not-login" => Labelled("AGENTS.md", OwnerTierL, owner: "repo owner"),
            "events-not-json" => Labelled("AGENTS.md", "not json"),
            "events-empty" => Labelled("AGENTS.md", ""),
            "events-object" => Labelled("AGENTS.md", OwnerEvent),
            "event-no-name" => Labelled("AGENTS.md", Before("""{"label":{"name":"tier-L"},"actor":{"login":"repo-owner"}}""")),
            "event-false" => Labelled("AGENTS.md", Before("""{"event":false}""")),
            "labeled-no-label" => Labelled("AGENTS.md", Before("""{"event":"labeled","actor":{"login":"repo-owner"}}""")),
            "label-string" => Labelled("AGENTS.md", Before("""{"event":"labeled","label":"tier-L","actor":{"login":"repo-owner"}}""")),
            "label-false" => Labelled("AGENTS.md", Before("""{"event":"labeled","label":false,"actor":{"login":"repo-owner"}}""")),
            "label-name-number" => Labelled("AGENTS.md", Before("""{"event":"labeled","label":{"name":1},"actor":{"login":"repo-owner"}}""")),
            "label-name-false" => Labelled("AGENTS.md", Before("""{"event":"unlabeled","label":{"name":false},"actor":{"login":"repo-owner"}}""")),
            "actor-string" => Labelled("AGENTS.md", Before("""{"event":"labeled","label":{"name":"tier-L"},"actor":"repo-owner"}""")),
            "actor-false" => Labelled("AGENTS.md", Before("""{"event":"unlabeled","label":{"name":"documentation"},"actor":false}""")),
            "login-number" => Labelled("AGENTS.md", Before("""{"event":"labeled","label":{"name":"tier-L"},"actor":{"login":1}}""")),
            "login-false" => Labelled("AGENTS.md", Before("""{"event":"labeled","label":{"name":"documentation"},"actor":{"login":false}}""")),
            "event-number" => Labelled("AGENTS.md", Before("1")),
            _ => Labelled("AGENTS.md", "[" + OwnerEvent + "]" + OwnerEvent),
        };

        Fails(run);
    }

    [Fact]
    public void The_protected_paths_script_needs_no_events_or_owner_without_tier_L()
    {
        Passes(Check(false, r => r.Write("src/PowerOps.Cli/Program.cs", "x\n"), events: null, owner: null));
        Fails(Check(false, r => r.Write("AGENTS.md", "x\n"), events: null, owner: null), "AGENTS.md");
    }

    public static TheoryData<string> MalformedInputs => ["pr-not-json", "pr-array", "pr-no-head", "pr-head-false", "pr-head-sha-number", "pr-no-base",
        "pr-base-false", "pr-base-ref-number", "pr-labels-object", "pr-labels-false", "pr-labels-null", "pr-no-labels", "pr-label-false",
        "pr-label-name-false", "expected-not-sha", "base-not-sha", "missing-pr-file", "no-repo", "not-a-repo", "head-not-in-repo",
        "base-not-in-repo", "head-is-a-blob"];

    [Theory]
    [MemberData(nameof(MalformedInputs))]
    public void The_protected_paths_script_fails_closed_on_input_it_cannot_read(string input)
    {
        var (repo, @base, head) = Change(r => r.Write("src/a.cs", "a\n"));
        using (repo)
        {
            var h = $$"""{"sha":"{{head}}"}""";
            var b = $$"""{"ref":"main","sha":"{{@base}}"}""";
            string Pr(string headJson, string baseJson, string labels = ",\"labels\":[]") => "{\"head\":" + headJson + ",\"base\":" + baseJson + labels + "}";
            GuardRun Read(string pr) => Offline(repo, pr, head, @base);
            var blob = repo.Run("rev-parse", "HEAD:seed.txt");

            // A pull request JSON with no tier-L passes no false, null or missing field as an empty one.
            var run = input switch
            {
                "pr-not-json" => Read("{"),
                "pr-array" => Read("[" + Pr(h, b) + "]"),
                "pr-no-head" => Read("{\"base\":" + b + ",\"labels\":[]}"),
                "pr-head-false" => Read(Pr("false", b)),
                "pr-head-sha-number" => Read(Pr("""{"sha":1}""", b)),
                "pr-no-base" => Read("{\"head\":" + h + ",\"labels\":[]}"),
                "pr-base-false" => Read(Pr(h, "false")),
                "pr-base-ref-number" => Read(Pr(h, $$"""{"ref":1,"sha":"{{@base}}"}""")),
                "pr-labels-object" => Read(Pr(h, b, ",\"labels\":{\"name\":\"tier-L\"}")),
                "pr-labels-false" => Read(Pr(h, b, ",\"labels\":false")),
                "pr-labels-null" => Read(Pr(h, b, ",\"labels\":null")),
                "pr-no-labels" => Read(Pr(h, b, "")),
                "pr-label-false" => Read(Pr(h, b, ",\"labels\":[false]")),
                "pr-label-name-false" => Read(Pr(h, b, ",\"labels\":[{\"name\":false}]")),
                "expected-not-sha" => Offline(repo, Pr(h, b), "main", @base),
                "base-not-sha" => Offline(repo, Pr(h, b), head, "main"),
                "missing-pr-file" => Guard(repo.Root, repo.Root, ["--repo", repo.Dir, "--pr-json", "missing.json", "--expected-head-sha", head, "--expected-base-ref", "main",
                    "--expected-base-sha", @base], new Dictionary<string, string>(), FailingGh),
                "no-repo" => Offline(repo, Pr(h, b), head, @base, repoDir: Path.Combine(repo.Root, "missing")),
                "not-a-repo" => Offline(repo, Pr(h, b), head, @base, repoDir: Directory.CreateDirectory(Path.Combine(repo.Root, "empty")).FullName),
                "head-not-in-repo" => Offline(repo, PrJson(false, OtherSha, @base), OtherSha, @base),
                "base-not-in-repo" => Offline(repo, PrJson(false, head, OtherSha), head, OtherSha),
                _ => Offline(repo, PrJson(false, blob, @base), blob, @base), // a blob, not a commit
            };

            Fails(run);
            Assert.Equal(0, Read(Pr(h, b)).Exit);
        }
    }

    [Fact]
    public void The_protected_paths_script_needs_all_five_offline_arguments_or_a_pull_request_number()
    {
        var (repo, @base, head) = Change(r => r.Write("src/a.cs", "a\n"));
        using (repo)
        {
            File.WriteAllText(Path.Combine(repo.Root, "pr.json"), PrJson(false, head, @base));
            var noPr = new Dictionary<string, string> { ["PR_NUMBER"] = "" };
            var live = new Dictionary<string, string> { ["GH_TOKEN"] = "fake-token", ["GITHUB_REPOSITORY"] = "example/repo", ["PR_NUMBER"] = "7" };
            string[] all = ["--repo", repo.Dir, "--pr-json", "pr.json", "--expected-head-sha", head, "--expected-base-ref", "main", "--expected-base-sha", @base];
            GuardRun Usage(IEnumerable<string> args, Dictionary<string, string> env) => Guard(repo.Root, repo.Root, args, env, FailingGh);

            Assert.Equal(0, Usage(all, noPr).Exit);
            for (var i = 0; i < all.Length; i += 2)
            {
                Assert.Equal(2, Usage(all.Take(i).Concat(all.Skip(i + 2)), noPr).Exit);
            }

            Assert.Equal(2, Usage([], noPr).Exit);
            Assert.Equal(2, Usage(["--expected-head-sha", head], noPr).Exit);
            // The live mode takes its head, base and owner from the event alone, and its repository is the checkout.
            Assert.Equal(2, Usage(["--owner", Owner], live).Exit);
            Assert.Equal(2, Usage(["--expected-base-ref", "main"], live).Exit);
            Assert.Equal(2, Usage(["--repo", repo.Dir], live).Exit);
            Assert.Empty(Usage(["--owner", Owner], live).GhCalls);
        }
    }

    // Answers the events call with every events-*.json in order (or with late-n.json when it exists and the pull request has been
    // read n times), and the n-th pull request read with pr-n.json.
    private const string LiveGh = """
        #!/usr/bin/env bash
        { printf '[%s]' "$@"; echo; } >> "$STUB_DIR/gh.log"
        case "$*" in
          *"/events?per_page=100")
            r=$(cat "$STUB_DIR/reads" 2>/dev/null || echo 0)
            if [ -f "$STUB_DIR/late-$r.json" ]; then cat "$STUB_DIR/late-$r.json"; else cat "$STUB_DIR"/events-*.json; fi ;;
          *) n=$(( $(cat "$STUB_DIR/reads" 2>/dev/null || echo 0) + 1 )); echo "$n" > "$STUB_DIR/reads"; cat "$STUB_DIR/pr-$n.json" ;;
        esac
        """;

    private const string PrCall = "[api][repos/example/repo/pulls/7]";
    private const string EventsCall = "[api][--paginate][repos/example/repo/issues/7/events?per_page=100]";

    private sealed record LiveRun(GuardRun Run, bool StaysOnMain, bool Fetched);

    private static bool HasCommit(string dir, string sha)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = dir, RedirectStandardError = true };
        foreach (var a in new[] { "cat-file", "-e", sha + "^{commit}" })
        {
            start.ArgumentList.Add(a);
        }

        using var process = Process.Start(start)!;
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    // A bare origin whose refs/pull/7/head is the pull request's commit (or, force pushed, another one) and whose main moved on after
    // the checkout. The check runs live in a clone of main that has neither the head nor the new base. The issue events are two pages:
    // someone else's documentation label, then `lastLabeler`'s tier-L; `lateEvents`, when given, is what the events read returns once
    // the pull request has been read twice.
    private static LiveRun Live(bool tierL, bool guarded = true, string? firstHead = null, string? secondHead = null, string? eventHead = null,
        string firstBaseRef = "main", string eventBaseRef = "main", string? secondBase = null, string? eventBase = null, bool? secondTierL = null,
        string lastLabeler = Owner, string owner = Owner, bool forcePushed = false, string? lateEvents = null)
    {
        using var repo = new GuardRepo();
        repo.Run("checkout", "-q", "-b", "pr");
        var head = repo.Commit("pull request", r => r.Write(guarded ? "AGENTS.md" : "src/a.cs", "x\n"));
        repo.Run("checkout", "-q", "-b", "other", repo.Seed);
        var other = repo.Commit("another push", r => r.Write("src/b.cs", "x\n"));
        repo.Run("checkout", "-q", "main");
        var origin = Path.Combine(repo.Root, "origin.git");
        var work = Path.Combine(repo.Root, "work");
        Git(repo.Root, "clone", "-q", "--bare", repo.Dir, origin);
        Git(origin, "update-ref", "refs/pull/7/head", forcePushed ? other : head);
        Git(origin, "update-ref", "-d", "refs/heads/pr");
        Git(origin, "update-ref", "-d", "refs/heads/other");
        Git(repo.Root, "clone", "-q", "--no-local", origin, work);
        var checkedOut = Git(work, "rev-parse", "HEAD");
        var @base = repo.Commit("main moves on", r => r.Write("src/later.cs", "x\n"));
        repo.Run("push", "-q", origin, "main");
        Assert.False(HasCommit(work, head) || HasCommit(work, @base));
        File.WriteAllText(Path.Combine(repo.Root, "pr-1.json"), PrJson(tierL, firstHead ?? head, @base, firstBaseRef));
        File.WriteAllText(Path.Combine(repo.Root, "pr-2.json"), PrJson(secondTierL ?? tierL, secondHead ?? head, secondBase ?? @base));
        File.WriteAllText(Path.Combine(repo.Root, "events-1.json"), Events(("labeled", "documentation", "someone-else")));
        File.WriteAllText(Path.Combine(repo.Root, "events-2.json"), Events(("labeled", "tier-L", lastLabeler)));
        if (lateEvents is not null)
        {
            File.WriteAllText(Path.Combine(repo.Root, "late-2.json"), lateEvents);
        }

        var run = Guard(repo.Root, work, [], new Dictionary<string, string>
        {
            ["GH_TOKEN"] = "fake-token",
            ["GITHUB_REPOSITORY"] = "example/repo",
            ["PR_NUMBER"] = "7",
            ["PR_HEAD_SHA"] = eventHead ?? head,
            ["PR_BASE_REF"] = eventBaseRef,
            ["PR_BASE_SHA"] = eventBase ?? @base,
            ["PR_OWNER"] = owner,
        }, LiveGh);
        var stays = Git(work, "rev-parse", "HEAD") == checkedOut && Git(work, "status", "--porcelain") == "";
        return new LiveRun(run, stays, HasCommit(work, head));
    }

    [Fact]
    public void The_protected_paths_script_fetches_the_head_and_base_commits_live_and_checks_neither_out()
    {
        var without = Live(tierL: false);
        var with = Live(tierL: true);

        Fails(without.Run, "AGENTS.md");
        Assert.Equal([PrCall, PrCall], without.Run.GhCalls);
        Passes(with.Run);
        // main's later commit is not the pull request's.
        Assert.Contains("1 changed file(s), 1 protected path(s), tier-L label: yes", with.Run.Stdout);
        // The events are read only for a pull request with the label, after both reads of it: the last word on who applied it.
        Assert.Equal([PrCall, PrCall, EventsCall], with.Run.GhCalls);
        Assert.All(new[] { without, with }, r => Assert.True(r.StaysOnMain && r.Fetched));
        Passes(Live(tierL: false, guarded: false).Run);
    }

    [Fact]
    public void The_protected_paths_script_fails_live_on_a_head_base_or_label_that_moved()
    {
        var stale = Live(tierL: true, eventHead: OtherSha);
        var pushed = Live(tierL: true, forcePushed: true);

        // A stale head fails on the first read, before any fetch.
        Fails(stale.Run);
        Assert.Equal([PrCall], stale.Run.GhCalls);
        Assert.False(stale.Fetched);
        Fails(pushed.Run);
        Assert.Contains("is not in the repository", pushed.Run.Stdout);
        Fails(Live(tierL: true, firstHead: OtherSha).Run);
        Fails(Live(tierL: true, secondHead: OtherSha).Run);
        Fails(Live(tierL: true, firstBaseRef: "release").Run);
        Fails(Live(tierL: true, eventBaseRef: "release").Run);
        Fails(Live(tierL: true, secondBase: OtherSha).Run);
        Fails(Live(tierL: true, eventBase: OtherSha).Run);
        Fails(Live(tierL: true, secondTierL: false).Run);
        Fails(Live(tierL: false, guarded: false, secondTierL: true).Run);
    }

    [Fact]
    public void The_protected_paths_script_fails_live_on_a_tier_L_the_owner_did_not_apply()
    {
        var triager = Live(tierL: true, lastLabeler: "triager");

        Fails(triager.Run);
        Assert.Equal([PrCall, PrCall, EventsCall], triager.Run.GhCalls);
        Fails(Live(tierL: true, owner: "").Run);
        Passes(Live(tierL: true, owner: "Repo-Owner").Run);
    }

    [Fact]
    public void The_protected_paths_script_fails_live_on_a_tier_L_someone_else_removed_and_applied_again_during_the_run()
    {
        // The label is there on both reads, but after the second one the events end with a triager's removal and labeled event.
        var reapplied = Live(tierL: true, secondTierL: true, lateEvents: Events(("labeled", "documentation", "someone-else"), ("labeled", "tier-L", Owner),
            ("unlabeled", "tier-L", "triager"), ("labeled", "tier-L", "triager")));

        Fails(reapplied.Run);
        Assert.Contains("the tier-L label was not applied by the owner", reapplied.Run.Stdout);
        Assert.DoesNotContain("triager", reapplied.Run.Stdout);
        Assert.Equal([PrCall, PrCall, EventsCall], reapplied.Run.GhCalls);
    }
}