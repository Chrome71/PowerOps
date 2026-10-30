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

    public static TheoryData<string> Workflows => ["ci.yml", "release.yml", "skills-smoke.yml", "pin-drift.yml"];

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
}