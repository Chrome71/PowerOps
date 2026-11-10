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
        var pack = Steps("release.yml", "publish").Select(s => Text(s, "run")).Single(r => r?.StartsWith("dotnet pack", StringComparison.Ordinal) == true);
        Assert.Contains("-p:RestoreLockedMode=true", pack);
        Assert.Contains("-p:IncludeSourceRevisionInInformationalVersion=false", pack);
    }

    public static TheoryData<string> Workflows => ["ci.yml", "release.yml", "skills-smoke.yml", "pin-drift.yml", "ecosystem-watch.yml"];

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
        Assert.Equal(new Dictionary<string, string> { ["contents"] = "write", ["id-token"] = "write", ["attestations"] = "write" }, Permissions(jobs["publish"]));
    }

    [Fact]
    public void The_release_job_checks_the_package_attests_it_and_attaches_both_files_without_a_clobber_and_never_touches_nuget()
    {
        var jobs = Map(Load(".github", "workflows", "release.yml")["jobs"]);
        var publish = Map(jobs["publish"]);
        var steps = Steps("release.yml", "publish");
        int Index(Func<Dictionary<object, object>, bool> match) => steps.FindIndex(s => match(s));
        var pack = Index(s => Text(s, "run")?.StartsWith("dotnet pack", StringComparison.Ordinal) == true);
        var check = Index(s => Text(s, "name") == "Check the package");
        var attest = Index(s => Text(s, "uses")?.StartsWith("actions/attest-build-provenance@", StringComparison.Ordinal) == true);
        var attach = Index(s => Text(s, "name") == "Attach to the release");

        Assert.Equal("${{ steps.release.outputs.version }}", (string)Map(Map(jobs["release-please"])["outputs"])["version"]);
        Assert.Equal("${{ needs.release-please.outputs.tag_name }}", (string)Map(publish["env"])["TAG"]);
        Assert.Equal("${{ needs.release-please.outputs.version }}", (string)Map(publish["env"])["VERSION"]);
        Assert.True(pack >= 0 && pack < check && check < attest && attest < attach && attach == steps.Count - 1, "pack, check, attest, attach, in that order");
        Assert.Equal("bash .github/scripts/release-check.sh \"$VERSION\" artifacts", Text(steps[check], "run"));
        var upload = Text(steps[attach], "run")!;
        Assert.Contains("gh release upload \"$TAG\"", upload);
        Assert.Contains("\"artifacts/PowerOps.$VERSION.nupkg\"", upload);
        Assert.Contains("artifacts/SHA256SUMS", upload);
        Assert.DoesNotContain("--clobber", upload);
        Assert.Equal(["GH_TOKEN"], Map(steps[attach]["env"]).Keys.Cast<string>());
        Assert.Equal("${{ github.token }}", (string)Map(steps[attach]["env"])["GH_TOKEN"]);
        Assert.DoesNotContain(steps.Where(s => s != steps[attach]), s => s.ContainsKey("env") && Map(s["env"]).ContainsKey("GH_TOKEN"));
        Assert.False(publish.TryGetValue("env", out var jobEnv) && Map(jobEnv).ContainsKey("GH_TOKEN"));
        Assert.DoesNotContain("nuget", string.Concat(AllSteps("release.yml").Select(s => (Text(s, "uses") ?? "") + (Text(s, "run") ?? ""))), StringComparison.OrdinalIgnoreCase);
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

    // ---- release-check.sh ----

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

    private sealed record CheckRun(int Exit, string Stdout, string[] Calls, string? Sums, int SumsCheckExit);

    private static CheckRun ReleaseCheck(string version, string printed, bool package = true, string installExit = "0")
    {
        Tools.Require("bash", "sha256sum");
        var root = Directory.CreateTempSubdirectory("powerops-release-check-").FullName;
        try
        {
            var bin = Directory.CreateDirectory(Path.Combine(root, "bin")).FullName;
            var stub = Path.Combine(bin, "dotnet");
            File.WriteAllText(stub, InstallStub.Replace("\r\n", "\n") + "\n");
            var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
            if (posix)
            {
                File.SetUnixFileMode(stub, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            var folder = Directory.CreateDirectory(Path.Combine(root, "artifacts")).FullName;
            if (package)
            {
                File.WriteAllText(Path.Combine(folder, "PowerOps.1.2.3.nupkg"), "the package");
            }

            var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = root, RedirectStandardOutput = true };
            start.ArgumentList.Add(Path.Combine(Golden.RepoRoot, ".github", "scripts", "release-check.sh"));
            start.ArgumentList.Add(version);
            start.ArgumentList.Add("artifacts");
            start.Environment["PATH"] = bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            start.Environment["STUB_DIR"] = root;
            start.Environment["STUB_PRINTED"] = printed;
            start.Environment["STUB_INSTALL_EXIT"] = installExit;
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var sums = Path.Combine(folder, "SHA256SUMS");
            var check = new ProcessStartInfo("sha256sum") { WorkingDirectory = folder, RedirectStandardOutput = true, ArgumentList = { "-c", "SHA256SUMS" } };
            var sumsCheck = -1;
            if (File.Exists(sums))
            {
                using var verify = Process.Start(check)!;
                verify.StandardOutput.ReadToEnd();
                verify.WaitForExit();
                sumsCheck = verify.ExitCode;
            }

            var log = Path.Combine(root, "dotnet.log");
            return new CheckRun(process.ExitCode, stdout, File.Exists(log) ? File.ReadAllLines(log) : [], File.Exists(sums) ? File.ReadAllText(sums) : null, sumsCheck);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void The_release_check_installs_the_package_alone_and_writes_a_SHA256SUMS_that_checks()
    {
        var run = ReleaseCheck("1.2.3", "1.2.3");

        Assert.Equal(0, run.Exit);
        Assert.Equal("", run.Stdout);
        var call = Assert.Single(run.Calls);
        Assert.Matches(@"^tool install --tool-path \S+ --source artifacts PowerOps --version 1\.2\.3$", call);
        Assert.EndsWith("  PowerOps.1.2.3.nupkg\n", run.Sums);
        Assert.Equal(0, run.SumsCheckExit);
    }

    [Theory]
    [InlineData("1.2", "1.2", true, "0")] // not major.minor.patch
    [InlineData("v1.2.3", "1.2.3", true, "0")]
    [InlineData("1.2.3-rc.1", "1.2.3-rc.1", true, "0")]
    [InlineData("1.2.3", "1.2.3", false, "0")] // no package
    [InlineData("1.2.3", "1.2.3", true, "1")] // the install fails
    [InlineData("1.2.3", "1.2.4", true, "0")] // the tool prints another version
    [InlineData("1.2.3", "1.2.3+4a5b6c7d", true, "0")] // or the version with a commit
    public void The_release_check_refuses_with_one_error_line_and_writes_no_SHA256SUMS(string version, string printed, bool package, string installExit)
    {
        var run = ReleaseCheck(version, printed, package, installExit);

        Assert.Equal(1, run.Exit);
        Assert.StartsWith("::error::", run.Stdout);
        Assert.Single(run.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.Null(run.Sums);
    }
}