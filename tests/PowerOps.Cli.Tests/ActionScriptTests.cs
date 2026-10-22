using System.Diagnostics;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Runs action/plan.sh, action/diff.sh and action/comment.sh with bash, against stub `gh` and `dotnet` programs
// that come first on PATH. Skipped on Windows and where bash (or jq or git, which a test needs) is not found.
public sealed class ActionScriptTests : IDisposable
{
    private const string Marker = "<!-- powerops -->";

    private static readonly string Scripts = Path.Combine(Golden.RepoRoot, "action");

    private readonly string _root = Directory.CreateTempSubdirectory("powerops-action-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    // ---- comment.sh ----

    [Fact]
    public void Comment_is_created_when_none_has_the_marker()
    {
        var run = RunComment(list: """[{"id":1,"user":{"login":"someone"},"body":"hello"}]""");

        Assert.Equal(0, run.Code);
        var calls = run.GhCalls;
        Assert.Contains("api --paginate repos/contoso/widgets/issues/7/comments --jq", calls[0]);
        Assert.Equal(2, calls.Length);
        Assert.StartsWith("api repos/contoso/widgets/issues/7/comments -F body=@", calls[1]);
        Assert.DoesNotContain("PATCH", calls[1]);
        Assert.Equal(Marker + "\n## PowerOps diff\n\nbody\n", run.Posted);
        Assert.False(File.Exists(run.Summary));
    }

    [Fact]
    public void Comment_by_the_bot_with_the_marker_is_updated_and_the_others_are_ignored()
    {
        var list = $$"""
            [
              {"id":1,"user":{"login":"someone"},"body":"{{Marker}}\nnot the bot"},
              {"id":2,"user":{"login":"github-actions[bot]"},"body":"a comment without the marker"},
              {"id":3,"user":{"login":"github-actions[bot]"},"body":null},
              {"id":4,"user":{"login":"github-actions[bot]"},"body":"{{Marker}}\nold"},
              {"id":5,"user":{"login":"github-actions[bot]"},"body":"{{Marker}}\nolder"}
            ]
            """;

        var run = RunComment(list);

        Assert.Equal(0, run.Code);
        Assert.Equal(2, run.GhCalls.Length);
        Assert.StartsWith("api -X PATCH repos/contoso/widgets/issues/comments/4 -F body=@", run.GhCalls[1]);
        Assert.StartsWith(Marker + "\n", run.Posted);
    }

    [Fact]
    public void Comment_on_a_fork_goes_to_the_job_summary_only()
    {
        var run = RunComment(list: "[]", fork: "true");

        Assert.Equal(0, run.Code);
        Assert.Empty(run.GhCalls);
        Assert.Equal("## PowerOps diff\n\nbody\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void Oversize_comment_is_replaced_by_a_note_and_the_summary_has_the_report()
    {
        var markdown = "## PowerOps diff\n\n" + new string('x', 70000) + "\n";

        var run = RunComment(list: "[]", markdown: markdown);

        Assert.Equal(0, run.Code);
        Assert.Equal(Marker + "\nThe PowerOps report is too long for a comment; it is in the job summary of this run.\n", run.Posted);
        Assert.Equal(markdown, File.ReadAllText(run.Summary));
    }

    [Fact]
    public void A_failing_gh_gives_a_warning_and_the_job_summary_and_does_not_fail()
    {
        var run = RunComment(list: "[]", ghExit: "1");

        Assert.Equal(0, run.Code);
        Assert.Contains("::warning::", run.Out);
        Assert.Equal("## PowerOps diff\n\nbody\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void A_failing_update_gives_a_warning_and_one_copy_in_the_job_summary()
    {
        var run = RunComment(list: """[{"id":9,"user":{"login":"github-actions[bot]"},"body":"<!-- powerops -->\nold"}]""", ghFailOn: "PATCH");

        Assert.Equal(0, run.Code);
        Assert.Contains("::warning::", run.Out);
        Assert.Equal("## PowerOps diff\n\nbody\n", File.ReadAllText(run.Summary));
    }

    [Fact]
    public void Nothing_happens_without_a_comment_file()
    {
        var run = RunComment(list: "[]", markdown: null);

        Assert.Equal(0, run.Code);
        Assert.Empty(run.GhCalls);
        Assert.False(File.Exists(run.Summary));
        Assert.Equal("", run.Out);
    }

    // ---- plan.sh ----

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Plan_with_exit_0_or_1_writes_the_summary_and_the_outputs(int exit)
    {
        var run = RunPlan(exit: exit, save: true);

        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\nplan-file={run.Folder}/powerops-plan.json\n", run.Output);
        Assert.Equal("## PowerOps deploy plan\n\nthe plan\n", File.ReadAllText(run.Summary));
        Assert.Equal("## PowerOps deploy plan\n\nthe plan\n", File.ReadAllText(run.Comment));
        Assert.Equal($"deploy plan {run.Folder} --target Contoso Test --save {run.Folder}/powerops-plan.json --format markdown --out {run.Comment}", run.DotnetCalls.Single().Replace("powerops.dll ", ""));
    }

    [Fact]
    public void Plan_output_names_the_plan_file_only_when_it_exists()
    {
        var run = RunPlan(exit: 1, save: false);

        Assert.Equal("exit-code=1\n", run.Output);
        Assert.Equal("## PowerOps deploy plan\n\nthe plan\n", File.ReadAllText(run.Summary));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Plan_with_exit_2_or_3_writes_the_fixed_text_and_logs_only_error_lines(int exit)
    {
        var run = RunPlan(exit: exit, save: false, stderr: "error: --target: pac failed\nForwarded: tenant 0000 at https://contoso.crm.dynamics.com\n");

        var fixedText = $"## PowerOps deploy plan\n\nThe plan could not run (exit code {exit}). See the job log.\n";
        Assert.Equal(0, run.Code);
        Assert.Equal($"exit-code={exit}\n", run.Output);
        Assert.Equal(fixedText, File.ReadAllText(run.Comment));
        Assert.Equal(fixedText, File.ReadAllText(run.Summary));
        Assert.Equal("error: --target: pac failed\n", run.Out);
    }

    [Fact]
    public void Plan_in_debug_mode_prints_the_forwarded_text()
    {
        var run = RunPlan(exit: 3, save: false, stderr: "error: x\nForwarded: tenant 0000\n", debug: true);

        Assert.Equal("error: x\nForwarded: tenant 0000\n", run.Out);
    }

    [Fact]
    public void Plan_with_an_empty_folder_exits_2_without_running_powerops()
    {
        var run = RunPlan(exit: 0, save: false, folder: "");

        Assert.Equal(0, run.Code);
        Assert.Equal("::error::deploy-folder is required when target is set\n", run.Out);
        Assert.Equal("exit-code=2\n", run.Output);
        Assert.Empty(run.DotnetCalls);
        Assert.Contains("(exit code 2)", File.ReadAllText(run.Comment));
    }

    // ---- diff.sh ----

    [Fact]
    public void Diff_compares_the_first_parent_with_the_work_tree()
    {
        SkipUnless("git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 1);

        Assert.Equal(0, run.Code);
        var baseTree = Path.Combine(run.Temp, "powerops-base");
        Assert.Equal($"diff {baseTree}/sol sol --format markdown --out {run.Comment}", run.DotnetCalls.Single().Replace("powerops.dll ", ""));
        // The worktree holds the first parent (v1); the work tree holds the merge (v2).
        var seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed");
        Assert.Equal(File.ReadAllText(Path.Combine(seed, "v1", "Other", "Solution.xml")), File.ReadAllText(Path.Combine(baseTree, "sol", "Other", "Solution.xml")));
        Assert.Equal(File.ReadAllText(Path.Combine(seed, "v2", "Other", "Solution.xml")), File.ReadAllText(Path.Combine(clone, "sol", "Other", "Solution.xml")));
        Assert.Equal("## PowerOps diff\n\nthe diff\n", File.ReadAllText(run.Comment));
    }

    [Fact]
    public void Diff_without_a_parent_writes_the_fixed_text()
    {
        SkipUnless("git");
        var repo = Path.Combine(_root, "single");
        Git(_root, "init", "-q", "-b", "main", repo);
        File.WriteAllText(Path.Combine(repo, "a.txt"), "a");
        Commit(repo, "A");

        var run = RunDiff(repo, exit: 0);

        Assert.Equal(0, run.Code);
        Assert.Equal("## PowerOps diff\n\nThe diff of the base and head solution could not run (no base commit, exit code 1). See the job log.\n", File.ReadAllText(run.Comment));
        Assert.Empty(run.DotnetCalls);
    }

    [Fact]
    public void Diff_with_a_failed_fetch_writes_the_fixed_text()
    {
        SkipUnless("git");
        var clone = MakeMergeClone();
        Git(clone, "remote", "set-url", "origin", "file://" + Path.Combine(_root, "missing"));

        var run = RunDiff(clone, exit: 0);

        Assert.Equal(0, run.Code);
        Assert.StartsWith("## PowerOps diff\n\nThe diff of the base and head solution could not run (fetch, exit code ", File.ReadAllText(run.Comment));
        Assert.Empty(run.DotnetCalls);
    }

    [Fact]
    public void Diff_with_exit_3_writes_the_fixed_text_without_the_tool_text()
    {
        SkipUnless("git");
        var clone = MakeMergeClone();

        var run = RunDiff(clone, exit: 3, stderr: "secret tool text");

        Assert.Equal(0, run.Code);
        Assert.Equal("## PowerOps diff\n\nThe diff of the base and head solution could not run (diff, exit code 3). See the job log.\n", File.ReadAllText(run.Comment));
        Assert.DoesNotContain("secret", File.ReadAllText(run.Comment));
    }

    // ---- helpers ----

    private sealed record Result(int Code, string Out, string Folder, string Temp, string Comment, string Summary, string Output, string Posted)
    {
        public string[] GhCalls => ReadLines(Path.Combine(Temp, "gh.log"));

        public string[] DotnetCalls => ReadLines(Path.Combine(Temp, "dotnet.log"));
    }

    private static string[] ReadLines(string path) => File.Exists(path) ? File.ReadAllLines(path) : [];

    // The stub gh logs its arguments and, for a call with --paginate, runs the real --jq filter over the canned list.
    // GH_FAIL_ON makes a call fail when its arguments contain that text; GH_EXIT makes every call fail.
    private const string GhStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/gh.log"
        if [ "${GH_EXIT:-0}" != "0" ]; then echo "gh: failed" >&2; exit "$GH_EXIT"; fi
        if [ -n "${GH_FAIL_ON:-}" ] && [[ "$*" == *"$GH_FAIL_ON"* ]]; then echo "gh: failed" >&2; exit 1; fi
        filter=
        paginate=0
        for ((i = 1; i <= $#; i++)); do
          arg="${!i}"
          if [ "$arg" = "--paginate" ]; then paginate=1; fi
          if [ "$arg" = "--jq" ]; then j=$((i + 1)); filter="${!j}"; fi
          case "$arg" in body=@*) cp "${arg#body=@}" "$STUB_DIR/posted.md";; esac
        done
        if [ "$paginate" = 1 ]; then jq -r "$filter" < "$STUB_DIR/list.json"; fi
        """;

    // The stub dotnet logs its arguments, writes the canned Markdown to --out and an empty plan to --save.
    private const string DotnetStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/dotnet.log"
        out=
        save=
        while [ $# -gt 0 ]; do
          case "$1" in --out) out="$2";; --save) save="$2";; esac
          shift
        done
        if [ -n "$out" ]; then cp "$STUB_DIR/markdown.md" "$out"; fi
        if [ -n "$save" ] && [ "${STUB_SAVE:-0}" = "1" ]; then echo '{}' > "$save"; fi
        if [ -f "$STUB_DIR/stderr.txt" ]; then cat "$STUB_DIR/stderr.txt" >&2; fi
        exit "${STUB_EXIT:-0}"
        """;

    private Result RunComment(string list, string? markdown = "## PowerOps diff\n\nbody\n", string fork = "false", string ghExit = "0", string ghFailOn = "")
    {
        Require("bash", "jq");
        var temp = NewDir("comment");
        WriteStub(temp, "gh", GhStub);
        File.WriteAllText(Path.Combine(temp, "list.json"), list);
        var comment = Path.Combine(temp, "comment.md");
        if (markdown is not null)
        {
            File.WriteAllText(comment, markdown);
        }

        var summary = Path.Combine(temp, "summary.md");
        var run = Run("comment.sh", temp, temp, new()
        {
            ["PO_COMMENT"] = comment,
            ["PO_FORK"] = fork,
            ["PO_PR"] = "7",
            ["GITHUB_REPOSITORY"] = "contoso/widgets",
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["GH_TOKEN"] = "fake",
            ["GH_EXIT"] = ghExit,
            ["GH_FAIL_ON"] = ghFailOn,
        });
        return run with { Posted = ReadOrEmpty(Path.Combine(temp, "posted.md")), Summary = summary, Comment = comment };
    }

    private Result RunPlan(int exit, bool save, string? stderr = null, bool debug = false, string? folder = null)
    {
        Require("bash");
        var temp = NewDir("plan");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "## PowerOps deploy plan\n\nthe plan\n");
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        folder ??= Directory.CreateDirectory(Path.Combine(temp, "deploy")).FullName;
        var comment = Path.Combine(temp, "comment.md");
        var summary = Path.Combine(temp, "summary.md");
        var output = Path.Combine(temp, "output.txt");
        var run = Run("plan.sh", temp, temp, new()
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_FOLDER"] = folder,
            ["PO_TARGET"] = "Contoso Test",
            ["PO_COMMENT"] = comment,
            ["GITHUB_OUTPUT"] = output,
            ["GITHUB_STEP_SUMMARY"] = summary,
            ["RUNNER_DEBUG"] = debug ? "1" : "",
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["STUB_SAVE"] = save ? "1" : "0",
        });
        return run with { Folder = folder, Comment = comment, Summary = summary, Output = ReadOrEmpty(output) };
    }

    private Result RunDiff(string workDir, int exit, string? stderr = null)
    {
        Require("bash", "git");
        var temp = NewDir("diff");
        WriteStub(temp, "dotnet", DotnetStub);
        File.WriteAllText(Path.Combine(temp, "markdown.md"), "## PowerOps diff\n\nthe diff\n");
        if (stderr is not null)
        {
            File.WriteAllText(Path.Combine(temp, "stderr.txt"), stderr);
        }

        var comment = Path.Combine(temp, "comment.md");
        var run = Run("diff.sh", temp, workDir, new()
        {
            ["PO_DLL"] = "powerops.dll",
            ["PO_PATH"] = "sol",
            ["PO_COMMENT"] = comment,
            ["RUNNER_TEMP"] = temp,
            ["STUB_EXIT"] = exit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        return run with { Comment = comment };
    }

    // A repository with base commit A (solution v1), a branch commit B (v2) and a --no-ff merge M on top of A,
    // cloned shallow, as actions/checkout does for a pull_request merge commit.
    private string MakeMergeClone()
    {
        var seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed");
        var repo = Path.Combine(_root, "repo");
        Git(_root, "init", "-q", "-b", "main", repo);
        CopyDirectory(Path.Combine(seed, "v1"), Path.Combine(repo, "sol"));
        Commit(repo, "A");
        Git(repo, "checkout", "-q", "-b", "feature");
        Directory.Delete(Path.Combine(repo, "sol"), recursive: true);
        CopyDirectory(Path.Combine(seed, "v2"), Path.Combine(repo, "sol"));
        Commit(repo, "B");
        Git(repo, "checkout", "-q", "main");
        Git(repo, "merge", "-q", "--no-ff", "-m", "M", "feature");
        Git(repo, "config", "uploadpack.allowAnySHA1InWant", "true");

        var clone = Path.Combine(_root, "clone");
        Git(_root, "clone", "-q", "--depth", "1", "--no-local", "file://" + repo, clone);
        return clone;
    }

    private static void Commit(string repo, string message)
    {
        Git(repo, "add", "-A");
        Git(repo, "commit", "-q", "-m", message);
    }

    private static void Git(string workDir, params string[] args)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = workDir, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "commit.gpgsign=false" }.Concat(args))
        {
            start.ArgumentList.Add(arg);
        }

        var result = Execute(start);
        Assert.True(result.Code == 0, $"git {string.Join(' ', args)}: {result.Out}{result.Err}");
    }

    private static void CopyDirectory(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from))
        {
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.GetDirectories(from))
        {
            CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
        }
    }

    private string NewDir(string name) => Directory.CreateDirectory(Path.Combine(_root, name + "-" + Guid.NewGuid().ToString("N")[..8])).FullName;

    private static void WriteStub(string temp, string name, string body)
    {
        var bin = Directory.CreateDirectory(Path.Combine(temp, "bin")).FullName;
        var path = Path.Combine(bin, name);
        File.WriteAllText(path, body.Replace("\r\n", "\n") + "\n");
        var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
        if (posix)
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static string ReadOrEmpty(string path) => File.Exists(path) ? File.ReadAllText(path) : "";

    private static void Require(params string[] programs)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The scripts need bash.");
        foreach (var program in programs)
        {
            Assert.SkipWhen(!OnPath(program), program + " is not on PATH.");
        }
    }

    private static void SkipUnless(string program) => Assert.SkipWhen(!OnPath(program), program + " is not on PATH.");

    private static bool OnPath(string program) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => File.Exists(Path.Combine(dir, program)));

    private Result Run(string script, string temp, string workDir, Dictionary<string, string> env)
    {
        var start = new ProcessStartInfo("bash") { WorkingDirectory = workDir, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(Scripts, script));
        start.Environment["PATH"] = Path.Combine(temp, "bin") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        start.Environment["STUB_DIR"] = temp;
        foreach (var (key, value) in env)
        {
            start.Environment[key] = value;
        }

        var result = Execute(start);
        return new Result(result.Code, result.Out, "", temp, "", "", "", "");
    }

    private static (int Code, string Out, string Err) Execute(ProcessStartInfo start)
    {
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(start.FileName + " did not finish.");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }
}