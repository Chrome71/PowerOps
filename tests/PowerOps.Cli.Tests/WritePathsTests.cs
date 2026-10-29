using System.Diagnostics;

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
}