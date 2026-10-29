using System.Diagnostics;
using System.Runtime.Versioning;

using PowerOps.Core.Drift;
using PowerOps.Core.Output;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// M28: R-9. Nothing is written through a link or over a file the command was given as an input, and a refusal is exit 2 before the command does its work.
public class WritePathsTests : IDisposable
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private static readonly string V1 = Path.Combine(Fixtures, "powerops-seed", "v1");
    private static readonly string V2 = Path.Combine(Fixtures, "powerops-seed", "v2");
    private static readonly string SeedTests = Path.Combine(Fixtures, "flowtests", "seed");
    private static readonly string DevFile = Path.Combine(Fixtures, "drift", "seed-dev.json");
    private static readonly string TestFile = Path.Combine(Fixtures, "drift", "seed-test.json");

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-write-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(Func<string, string, Snapshot>? live, params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var code = Program.Run(args, o, e, (_, _) => throw new Xunit.Sdk.XunitException("pac must not be called"), live);
        return (code, o.ToString(), e.ToString());
    }

    private static (int Code, string Out, string Err) Run(params string[] args) => Run(null, args);

    private static string[] Lines(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();

    private string Put(string name, string content)
    {
        var path = Path.Combine(_temp, name);
        File.WriteAllText(path, content);
        return path;
    }

    private string ZipOf(string folder, string name)
    {
        var zip = Path.Combine(_temp, name);
        SolutionZip.Create(folder, zip);
        return zip;
    }

    private static string[] DriftArgs(string first, string second, string outFile) =>
        ["drift", "--solution", "PowerOpsSeed", "--env", first, "--env", second, "--out", outFile];

    private static string[] ArgsFor(string command, string outFile) => command switch
    {
        "analyze" => ["analyze", V2, "--out", outFile],
        "baseline" => ["baseline", V2, "--out", outFile],
        "diff" => ["diff", V1, V2, "--out", outFile],
        "test" => ["test", V2, "--tests", SeedTests, "--out", outFile],
        "settings generate" => ["settings", "generate", V2, "--out", outFile],
        _ => DriftArgs(DevFile, TestFile, outFile),
    };

    [Theory]
    [InlineData("analyze")]
    [InlineData("baseline")]
    [InlineData("diff")]
    [InlineData("test")]
    [InlineData("settings generate")]
    [InlineData("drift")]
    public void Out_through_a_link_is_refused_with_exit_2_and_the_file_behind_it_is_not_touched(string command)
    {
        var victim = Put("victim.txt", "victim");
        var link = Path.Combine(_temp, "report.json");
        Links.Create(link, victim);

        var (code, output, error) = Run(ArgsFor(command, link));

        Assert.Equal(2, code);
        Assert.Equal("", output);
        var line = Assert.Single(Lines(error));
        Assert.Equal(command == "drift" ? $"error: --out {SafeWrite.LinkReason}" : $"error: {link}: {SafeWrite.LinkReason}", line);
        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.Equal(victim, new FileInfo(link).LinkTarget);
    }

    [Theory]
    [InlineData("analyze path")]
    [InlineData("analyze baseline")]
    [InlineData("baseline")]
    [InlineData("diff old")]
    [InlineData("diff new")]
    [InlineData("test")]
    [InlineData("settings generate")]
    [InlineData("drift")]
    public void Out_over_an_input_is_refused_with_exit_2_and_the_input_is_not_touched(string kind)
    {
        var zipA = ZipOf(V1, "a.zip");
        var zipB = ZipOf(V2, "b.zip");
        var baselineFile = Path.Combine(_temp, "base.json");
        Assert.Equal(0, Run("baseline", V2, "--out", baselineFile).Code);
        var snapshot = Path.Combine(_temp, "snap.json");
        File.Copy(DevFile, snapshot);
        var (args, input) = kind switch
        {
            "analyze path" => (["analyze", zipA, "--out", zipA], zipA),
            "analyze baseline" => (["analyze", V2, "--baseline", baselineFile, "--out", baselineFile], baselineFile),
            "baseline" => (["baseline", zipA, "--out", zipA], zipA),
            "diff old" => (["diff", zipA, zipB, "--out", zipA], zipA),
            "diff new" => (["diff", zipA, zipB, "--out", zipB], zipB),
            "test" => (["test", zipB, "--tests", SeedTests, "--out", zipB], zipB),
            "settings generate" => (["settings", "generate", zipA, "--out", zipA], zipA),
            _ => (DriftArgs(snapshot, TestFile, snapshot), snapshot),
        };
        var before = File.ReadAllBytes(input);

        var (code, output, error) = Run(args);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal(kind == "drift" ? $"error: --out {SafeWrite.InputReason}" : $"error: {input}: {SafeWrite.InputReason}", Assert.Single(Lines(error)));
        Assert.Equal(before, File.ReadAllBytes(input));
    }

    // M33 U-2: a file a scan found in a folder counts as an input, though the user named only the folder.
    private (string Scan, string Zip, string Tests) ScanFolder()
    {
        var scan = Directory.CreateDirectory(Path.Combine(_temp, "scan")).FullName;
        var zip = Path.Combine(scan, "found.zip");
        SolutionZip.Create(V2, zip);
        var tests = Directory.CreateDirectory(Path.Combine(_temp, "tests")).FullName;
        foreach (var f in Directory.EnumerateFiles(SeedTests, "*.flowtest.json"))
        {
            File.Copy(f, Path.Combine(tests, Path.GetFileName(f)));
        }

        return (scan, zip, tests);
    }

    private static string[] FoundArgs(string kind, string scan, string tests, string outFile) =>
        kind switch
        {
            "analyze" => ["analyze", scan, "--out", outFile],
            "baseline" => ["baseline", scan, "--out", outFile],
            "diff old" => ["diff", scan, V1, "--out", outFile],
            "diff new" => ["diff", V1, scan, "--out", outFile],
            _ => ["test", scan, "--tests", tests, "--out", outFile],
        };

    [Theory]
    [InlineData("analyze")]
    [InlineData("baseline")]
    [InlineData("diff old")]
    [InlineData("diff new")]
    [InlineData("test zip")]
    [InlineData("test flowtest")]
    public void Out_over_a_file_the_scan_found_is_refused_with_exit_2_and_the_file_is_not_touched(string kind)
    {
        var (scan, zip, tests) = ScanFolder();
        var victim = kind == "test flowtest" ? Directory.GetFiles(tests).Order().First() : zip;
        var before = File.ReadAllBytes(victim);

        var (code, output, error) = Run(FoundArgs(kind.StartsWith("test") ? "test" : kind, scan, tests, victim));

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: {victim}: {SafeWrite.InputReason}", Assert.Single(Lines(error)));
        Assert.Equal(before, File.ReadAllBytes(victim));
        Assert.Equal([zip], Directory.GetFiles(scan));
    }

    [Theory]
    [InlineData("analyze")]
    [InlineData("baseline")]
    [InlineData("diff old")]
    [InlineData("diff new")]
    [InlineData("test")]
    public void Out_into_the_scanned_folder_is_still_written(string kind)
    {
        var (scan, _, tests) = ScanFolder();
        var report = Path.Combine(scan, "report.txt");

        var (code, _, error) = Run(FoundArgs(kind, scan, tests, report));

        Assert.True(code is 0 or 1, error);
        Assert.True(File.Exists(report));
        Assert.DoesNotContain(SafeWrite.InputReason, error);
    }

    // The reviewer's repro: a link to the input's own folder, then the input's name under it.
    [Fact]
    public void Out_over_an_input_through_a_link_to_its_folder_is_refused_with_exit_2_and_the_input_is_not_touched()
    {
        var zip = ZipOf(V2, "in.zip");
        Links.Create(Path.Combine(_temp, "self"), ".", folder: true);
        var before = File.ReadAllBytes(zip);

        var (code, output, error) = Run("settings", "generate", zip, "--out", Path.Combine(_temp, "self", "in.zip"));

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: {Path.Combine(_temp, "self", "in.zip")}: {SafeWrite.InputReason}", Assert.Single(Lines(error)));
        Assert.Equal(before, File.ReadAllBytes(zip));
    }

    // dl -> sub/deep. A ".." after the link is collapsed first, as .NET opens every path on every OS: dl/../in.zip is the top in.zip, where a shell would take sub/in.zip.
    private void DeepLink()
    {
        Directory.CreateDirectory(Path.Combine(_temp, "sub", "deep"));
        Links.Create(Path.Combine(_temp, "dl"), Path.Combine("sub", "deep"), folder: true);
    }

    // The first review's repro: the target names the input the way the reader opened it, so it is refused.
    [Fact]
    public void Out_through_a_link_and_dotdot_over_the_input_is_refused_on_every_OS()
    {
        DeepLink();
        var zip = ZipOf(V2, "in.zip");
        var before = File.ReadAllBytes(zip);
        var target = Path.Combine(_temp, "dl", "..", "in.zip");

        var (code, output, error) = Run("settings", "generate", zip, "--out", target);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: {target}: {SafeWrite.InputReason}", Assert.Single(Lines(error)));
        Assert.Equal(before, File.ReadAllBytes(zip));
        Assert.False(File.Exists(Path.Combine(_temp, "sub", "in.zip")));
    }

    [Fact]
    public void Out_through_a_link_and_dotdot_lands_beside_the_link_as_NET_opens_it()
    {
        DeepLink();
        var zip = Path.Combine(_temp, "sub", "in.zip");
        SolutionZip.Create(V2, zip);
        var before = File.ReadAllBytes(zip);
        var expected = Run("settings", "generate", zip).Out;
        var target = Path.Combine(_temp, "dl", "..", "in.zip");

        var (code, output, error) = Run("settings", "generate", zip, "--out", target);

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Equal("", error);
        Assert.Equal(expected, File.ReadAllText(Path.Combine(_temp, "in.zip")));
        Assert.Equal(before, File.ReadAllBytes(zip));
    }

    // The second review's finding 1: the reader opened the top in.zip for this spelling, so the input check must see the same file.
    [Fact]
    public void An_input_spelled_through_a_link_and_dotdot_is_the_file_the_reader_opened_so_out_over_it_is_refused()
    {
        DeepLink();
        var zip = ZipOf(V2, "in.zip");
        var before = File.ReadAllBytes(zip);
        var input = Path.Combine(_temp, "dl", "..", "in.zip");

        var (code, output, error) = Run("settings", "generate", input, "--out", zip);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: {zip}: {SafeWrite.InputReason}", Assert.Single(Lines(error)));
        Assert.Equal(before, File.ReadAllBytes(zip));
    }

    [Fact]
    public void Out_through_more_than_40_links_or_a_link_loop_exits_2_and_writes_nothing()
    {
        var real = Directory.CreateDirectory(Path.Combine(_temp, "realdir")).FullName;
        Links.Create(Path.Combine(_temp, "l0"), real, folder: true);
        for (var i = 1; i <= 40; i++)
        {
            Links.Create(Path.Combine(_temp, $"l{i}"), Path.Combine(_temp, $"l{i - 1}"), folder: true);
        }

        Links.Create(Path.Combine(_temp, "loop"), "loop", folder: true);

        foreach (var folder in new[] { "l40", "loop" })
        {
            var target = Path.Combine(_temp, folder, "out.json");

            var (code, output, error) = Run("settings", "generate", V2, "--out", target);

            Assert.Equal(2, code);
            Assert.Equal("", output);
            Assert.Equal($"error: {target}: {SafeWrite.LoopReason}", Assert.Single(Lines(error)));
            Assert.Empty(Directory.GetFileSystemEntries(real));
        }
    }

    // On Linux /dev/fd is a link to /proc/self/fd, so /dev/fd/1 is a link: only the typed names /dev/stdout and /dev/stderr are exempt.
    [Fact]
    public void Out_dev_fd_1_is_a_link_and_refused_with_exit_2()
    {
        Assert.SkipWhen(!OperatingSystem.IsLinux(), "Linux only.");

        var (code, output, error) = Run("analyze", V2, "--out", "/dev/fd/1");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal($"error: /dev/fd/1: {SafeWrite.LinkReason}", Assert.Single(Lines(error)));
    }

    [Theory]
    [InlineData("analyze", "")]
    [InlineData("analyze", "folder")]
    [InlineData("drift", "")]
    [InlineData("drift", "folder")]
    public void Out_empty_or_an_existing_folder_exits_2_with_a_message_and_writes_nothing(string command, string kind)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_temp, "dir")).FullName;
        var outFile = kind == "" ? "" : folder;

        var (code, output, error) = Run(ArgsFor(command, outFile));

        Assert.Equal(2, code);
        Assert.Equal("", output);
        var reason = kind == "" ? SafeWrite.EmptyReason : SafeWrite.FolderReason;
        var expected = (command, kind) switch
        {
            ("drift", _) => $"error: --out {reason}",
            (_, "") => "error: the file name is empty; name a file.",
            _ => $"error: {folder}: {reason}",
        };
        Assert.Equal(expected, Assert.Single(Lines(error)));
        Assert.Empty(Directory.GetFileSystemEntries(folder));
    }

    [Theory]
    [InlineData("analyze")]
    [InlineData("settings generate")]
    public void Out_dev_null_still_works(string command)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix devices.");
        string[] args = command == "analyze" ? ["analyze", V2, "--fail-on", "none", "--out", "/dev/null"] : ["settings", "generate", V2, "--out", "/dev/null"];

        var (code, output, error) = Run(args);

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Equal("", error);
    }

    [Fact]
    public void Out_NUL_still_works_on_windows_and_leaves_no_temp_file()
    {
        Assert.SkipWhen(!OperatingSystem.IsWindows(), "Windows reserved names.");

        var (code, output, error) = Run("settings", "generate", V2, "--out", "NUL");

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Equal("", error);
        Assert.Empty(Directory.GetFiles(Directory.GetCurrentDirectory(), ".powerops-*.tmp"));
    }

    // `--out /dev/stdout` is written in place: with stdout a pipe, and with stdout redirected to a regular file, which a link rule would refuse.
    [Fact]
    public void Out_dev_stdout_writes_to_a_pipe()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix devices.");
        var expected = Run("settings", "generate", V2).Out;
        var start = Child("settings", "generate", V2, "--out", "/dev/stdout");
        start.RedirectStandardOutput = true;

        var (code, output, error) = Execute(start);

        Assert.Equal(0, code);
        Assert.Equal(expected, output);
        Assert.Equal("", error);
    }

    [Fact]
    public void Out_dev_stdout_writes_to_a_file_stdout_is_redirected_to()
    {
        Tools.Require("bash");
        var expected = Run("settings", "generate", V2).Out;
        var captured = Path.Combine(_temp, "captured.txt");
        var start = new ProcessStartInfo(Tools.Bash);
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("dotnet \"$0\" settings generate \"$1\" --out /dev/stdout > \"$2\"");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "powerops.dll"));
        start.ArgumentList.Add(V2);
        start.ArgumentList.Add(captured);

        var (code, _, error) = Execute(start);

        Assert.Equal(0, code);
        Assert.Equal("", error);
        Assert.Equal(expected, File.ReadAllText(captured));
    }

    // Standard error is always read; standard output only when the start info redirects it.
    private static (int Code, string Out, string Err) Execute(ProcessStartInfo start)
    {
        start.RedirectStandardError = true;
        using var process = Process.Start(start)!;
        var stdout = start.RedirectStandardOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(120)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(start.FileName + " did not finish.");
        }

        process.WaitForExit();
        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }

    private static ProcessStartInfo Child(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet") { Environment = { ["DOTNET_NOLOGO"] = "1", ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1" } };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "powerops.dll"));
        foreach (var a in args)
        {
            start.ArgumentList.Add(a);
        }

        return start;
    }

    // The live read of drift runs after the up-front check, so a link planted in it is found only by the write's own check: exit 3, nothing written through it.
    [Fact]
    public void A_link_planted_after_the_up_front_check_is_found_at_the_write_with_exit_3()
    {
        var victim = Put("victim.txt", "victim");
        var report = Path.Combine(_temp, "report.json");
        Links.Create(Path.Combine(_temp, "probe"), victim);
        File.Delete(Path.Combine(_temp, "probe"));
        var dev = SnapshotFile.Load(DevFile);

        var (code, output, error) = Run(
            (_, value) =>
            {
                if (!File.Exists(report) && new FileInfo(report).LinkTarget is null)
                {
                    File.CreateSymbolicLink(report, victim);
                }

                return value == "a" ? dev : dev with { Environment = "Contoso Test" };
            },
            DriftArgs("a", "b", report));

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.Equal($"error: {report}: {SafeWrite.LinkReason}", Assert.Single(Lines(error)));
        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.Equal(victim, new FileInfo(report).LinkTarget);
    }

    // ---- drift --save -------------------------------------------------------------------------------------------

    private static Snapshot Named(string environment) => SnapshotFile.Load(DevFile) with { Environment = environment };

    [Fact]
    public void Save_into_a_folder_with_a_link_at_a_file_name_it_would_write_exits_2_and_saves_nothing()
    {
        var victim = Put("victim.txt", "victim");
        var save = Directory.CreateDirectory(Path.Combine(_temp, "saved")).FullName;
        var link = Path.Combine(save, "PowerOpsSeed.Beta.json");
        Links.Create(link, victim);

        var (code, output, error) = Run(
            (_, value) => Named(value == "a" ? "Alpha" : "Beta"),
            "drift", "--solution", "PowerOpsSeed", "--env", "a", "--env", "b", "--save", save);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal("error: --env 2 would be saved to a symbolic link; nothing was saved", Assert.Single(Lines(error)));
        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.Equal([link], Directory.GetFileSystemEntries(save));
    }

    [Fact]
    public void Save_with_a_folder_at_a_file_name_it_would_write_exits_2_and_saves_nothing()
    {
        var save = Directory.CreateDirectory(Path.Combine(_temp, "saved")).FullName;
        var folder = Directory.CreateDirectory(Path.Combine(save, "PowerOpsSeed.Beta.json")).FullName;

        var (code, output, error) = Run(
            (_, value) => Named(value == "a" ? "Alpha" : "Beta"),
            "drift", "--solution", "PowerOpsSeed", "--env", "a", "--env", "b", "--save", save);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal("error: --env 2 would be saved to a folder; nothing was saved", Assert.Single(Lines(error)));
        Assert.Equal([folder], Directory.GetFileSystemEntries(save));
    }

    // The folder is made and the files are written under the same convention, so nothing lands in the link's target.
    [Fact]
    public void Save_through_a_link_and_dotdot_makes_the_folder_beside_the_link_as_NET_opens_it()
    {
        DeepLink();
        var save = Path.Combine(_temp, "dl", "..", "snaps");

        var (code, _, error) = Run(
            (_, value) => Named(value == "a" ? "Alpha" : "Beta"),
            "drift", "--solution", "PowerOpsSeed", "--env", "a", "--env", "b", "--save", save);

        Assert.Equal(0, code);
        Assert.Equal("", error);
        Assert.True(File.Exists(Path.Combine(_temp, "snaps", "PowerOpsSeed.Alpha.json")));
        Assert.Equal([Path.Combine(_temp, "sub", "deep")], Directory.GetFileSystemEntries(Path.Combine(_temp, "sub")));
        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(_temp, "sub", "deep")));
    }

    [Fact]
    public void Save_over_an_env_snapshot_file_exits_2_and_leaves_the_file()
    {
        var save = Directory.CreateDirectory(Path.Combine(_temp, "saved")).FullName;
        var snapshot = Path.Combine(save, "PowerOpsSeed.Alpha.json");
        File.Copy(DevFile, snapshot);
        var before = File.ReadAllBytes(snapshot);

        var (code, output, error) = Run(
            (_, _) => Named("Alpha"),
            "drift", "--solution", "PowerOpsSeed", "--env", snapshot, "--env", "b", "--save", save);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Equal("error: --env 2 would be saved over a snapshot file given as an --env; nothing was saved", Assert.Single(Lines(error)));
        Assert.Equal(before, File.ReadAllBytes(snapshot));
    }

    // ---- skills install -------------------------------------------------------------------------------------------

    private string Repo => Directory.CreateDirectory(Path.Combine(_temp, "repo")).FullName;

    private string Outside => Directory.CreateDirectory(Path.Combine(_temp, "outside")).FullName;

    private static string FirstSkill => SkillsCommandShipped().Keys.First();

    private static IReadOnlyDictionary<string, string> SkillsCommandShipped() => SkillsCommand.Shipped();

    // Each row plants a link on the way (or at a file) that leads outside the repo; the repo folder is given in different spellings.
    [Theory]
    [InlineData("claude", "plain")]
    [InlineData("claude/skills", "plain")]
    [InlineData("skill folder", "plain")]
    [InlineData("SKILL.md", "plain")]
    [InlineData("agents unchanged", "plain")]
    [InlineData("claude", "dot-dot")]
    [InlineData("claude", "dot-slash")]
    [InlineData("claude", "trailing")]
    public void Skills_install_refuses_a_link_on_the_way_with_exit_2_and_writes_nothing_outside_the_repo(string link, string spelling)
    {
        var (repo, outside) = (Repo, Outside);
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, repo);
        Assert.SkipWhen(spelling is "dot-dot" or "dot-slash" && Path.IsPathRooted(relative), "The temp folder is on another drive than the current folder.");
        var skill = FirstSkill.Split('/')[0];
        string refused;
        switch (link)
        {
            case "claude":
                Links.Create(Path.Combine(repo, ".claude"), outside, folder: true);
                refused = Path.Combine(repo, ".claude");
                break;
            case "claude/skills":
                Directory.CreateDirectory(Path.Combine(repo, ".claude"));
                Links.Create(Path.Combine(repo, ".claude", "skills"), outside, folder: true);
                refused = Path.Combine(repo, ".claude", "skills");
                break;
            case "skill folder":
                Directory.CreateDirectory(Path.Combine(repo, ".claude", "skills"));
                Links.Create(Path.Combine(repo, ".claude", "skills", skill), outside, folder: true);
                refused = Path.Combine(repo, ".claude", "skills", skill);
                break;
            case "SKILL.md":
                var victim = Put("victim.txt", "victim");
                Directory.CreateDirectory(Path.Combine(repo, ".claude", "skills", skill));
                Links.Create(Path.Combine(repo, ".claude", "skills", FirstSkill.Replace('/', Path.DirectorySeparatorChar)), victim);
                refused = Path.Combine(repo, ".claude", "skills", FirstSkill.Replace('/', Path.DirectorySeparatorChar));
                break;
            case "agents unchanged":
                Assert.Equal(0, Run("skills", "install", repo, "--agent", "claude").Code);
                Directory.CreateDirectory(Path.Combine(repo, ".agents"));
                Links.Create(Path.Combine(repo, ".agents", "skills"), Path.Combine(repo, ".claude", "skills"), folder: true);
                refused = Path.Combine(repo, ".agents", "skills");
                break;
            default:
                throw new InvalidOperationException(link);
        }

        var given = spelling switch
        {
            "dot-dot" => relative,
            "dot-slash" => "." + Path.DirectorySeparatorChar + relative,
            "trailing" => repo + Path.DirectorySeparatorChar,
            _ => repo,
        };
        var before = Directory.GetFileSystemEntries(repo, "*", SearchOption.AllDirectories).Order().ToArray();

        var (code, output, error) = Run("skills", "install", given);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.All(Lines(error), l => Assert.Equal($"error: {refused.Replace('\\', '/')}: {SafeWrite.LinkReason}", l));
        Assert.Empty(Directory.GetFileSystemEntries(outside));
        Assert.Equal(before, Directory.GetFileSystemEntries(repo, "*", SearchOption.AllDirectories).Order().ToArray());
    }

    [Fact]
    public void Skills_install_into_a_repo_folder_that_is_itself_a_link_is_allowed()
    {
        var real = Directory.CreateDirectory(Path.Combine(_temp, "real")).FullName;
        var link = Path.Combine(_temp, "repo-link");
        Links.Create(link, real, folder: true);

        var (code, _, error) = Run("skills", "install", link, "--agent", "claude");

        Assert.Equal(0, code);
        Assert.Equal("", error);
        Assert.True(File.Exists(Path.Combine(real, ".claude", "skills", FirstSkill.Replace('/', Path.DirectorySeparatorChar))));
    }

    // The second review's finding 3: with dl a link out of the repo, dl/.. is the repo folder for the folders made and the files written alike.
    [Fact]
    public void Skills_install_through_a_link_and_dotdot_writes_beside_the_link_and_nothing_in_its_target()
    {
        Directory.CreateDirectory(Path.Combine(_temp, "sub", "deep"));
        var repo = Directory.CreateDirectory(Path.Combine(_temp, "sk")).FullName;
        Links.Create(Path.Combine(repo, "dl"), Path.Combine("..", "sub", "deep"), folder: true);

        var (code, _, error) = Run("skills", "install", Path.Combine(repo, "dl", ".."), "--agent", "claude");

        Assert.Equal(0, code);
        Assert.Equal("", error);
        Assert.True(File.Exists(Path.Combine(repo, ".claude", "skills", FirstSkill.Replace('/', Path.DirectorySeparatorChar))));
        Assert.Equal([Path.Combine(_temp, "sub", "deep")], Directory.GetFileSystemEntries(Path.Combine(_temp, "sub")));
        Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(_temp, "sub", "deep")));
    }

    // ---- plan sign-off --------------------------------------------------------------------------------------------

    private const string Approved = "---\nstatus: approved\nreview: skipped\n---\n# Plan\n";

    private string PlanFolder() => Directory.CreateDirectory(Path.Combine(_temp, "docs", "plans", "site-visits")).FullName;

    // The second review's finding 2: the plan that was read is the plan that is written, and the draft behind the link is untouched.
    [Fact]
    public void Plan_sign_off_through_a_link_and_dotdot_writes_the_plan_it_read()
    {
        const string draft = "---\nstatus: draft\nreview: skipped\n---\n# Draft\n";
        DeepLink();
        var plan = Path.Combine(PlanFolder(), "plan.md");
        File.WriteAllText(plan, Approved);
        var other = Path.Combine(Directory.CreateDirectory(Path.Combine(_temp, "sub", "docs", "plans", "site-visits")).FullName, "plan.md");
        File.WriteAllText(other, draft);

        var (code, output, error) = Run("plan", "sign-off", Path.Combine(_temp, "dl", "..", "docs", "plans", "site-visits"));

        Assert.Equal(0, code);
        Assert.Equal("", error);
        Assert.StartsWith("signed off ", output);
        Assert.Contains("status: signed-off", File.ReadAllText(plan));
        Assert.Equal(draft, File.ReadAllText(other));
    }

    [Fact]
    public void Plan_sign_off_through_a_linked_plan_md_still_exits_3_and_leaves_the_file_behind_it()
    {
        var victim = Put("victim.md", Approved);
        var folder = PlanFolder();
        Links.Create(Path.Combine(folder, "plan.md"), victim);

        var (code, output, error) = Run("plan", "sign-off", folder);

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.Contains("symbolic links are not read.", error);
        Assert.Equal(Approved, File.ReadAllText(victim));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void Plan_sign_off_keeps_the_file_mode_and_leaves_no_temp_file()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix modes.");
        var folder = PlanFolder();
        var plan = Path.Combine(folder, "plan.md");
        File.WriteAllText(plan, Approved);
        File.SetUnixFileMode(plan, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);

        var (code, output, _) = Run("plan", "sign-off", folder);

        Assert.Equal(0, code);
        Assert.StartsWith("signed off ", output);
        Assert.Contains("status: signed-off", File.ReadAllText(plan));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead, File.GetUnixFileMode(plan));
        Assert.Empty(Directory.GetFiles(folder, ".powerops-*.tmp"));
    }
}