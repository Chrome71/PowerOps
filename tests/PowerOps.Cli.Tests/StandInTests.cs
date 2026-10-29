using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Stand-in pac and az files in a temp folder, started by the real PacRunner under the production names.
// On Unix each prints its arguments one per line, which proves that the argument lists PlanFacts and DriftLiveReader build reach a program unchanged.
// On Windows they are pac.cmd, pac.bat and az.cmd, found through PATHEXT (pac.bat first in the default order) and started; each echoes the command line cmd.exe received,
// so there the check compares the arguments joined by spaces and does not tell ["org who"] from ["org", "who"].
public sealed class StandInTests : IDisposable
{
    private static readonly string OrgWho = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "org-who.txt");

    private readonly string _folder = Directory.CreateTempSubdirectory("powerops-standin-").FullName;

    public StandInTests()
    {
        if (OperatingSystem.IsWindows())
        {
            foreach (var name in new[] { "pac.cmd", "pac.bat", "az.cmd" })
            {
                File.WriteAllText(Path.Combine(_folder, name), "@echo off\r\necho %*\r\n");
            }

            return;
        }

        foreach (var name in new[] { "pac", "az" })
        {
            var path = Path.Combine(_folder, name);
            File.WriteAllText(path, "#!/bin/sh\nprintf '%s\\n' \"$@\"\n");
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string? Resolve(string name) => PacRunner.Resolve(name, _folder, OperatingSystem.IsWindows(), Environment.GetEnvironmentVariable("PATHEXT"));

    // Runs the stand-in for real, records the arguments and the real result, and returns the real Started, TimedOut and ExitCode with the stdout the consumer parses.
    // With no canned text the real result passes through.
    private Func<IReadOnlyList<string>, PacResult> Through(string name, List<Call> log, Func<string, string>? canned = null) => args =>
    {
        var real = new PacRunner(Resolve(name) ?? throw new FileNotFoundException(name)).Run(args);
        log.Add(new Call(string.Join(" ", args), args, real));
        return canned is null ? real : real with { Stdout = canned(log[^1].Args) };
    };

    private sealed record Call(string Args, IReadOnlyList<string> Argv, PacResult Real);

    private static void AssertEchoed(List<Call> log)
    {
        Assert.All(log, call =>
        {
            Assert.True(call.Real.Started, call.Args);
            Assert.Equal(0, call.Real.ExitCode);
            var stdout = call.Real.Stdout.Replace("\r\n", "\n").TrimEnd('\n');
            Assert.Equal(OperatingSystem.IsWindows() ? call.Args : string.Join("\n", call.Argv), stdout);
        });
    }

    [Fact]
    public void PlanFacts_sends_both_pac_lists_to_the_program_unchanged()
    {
        var pac = new List<Call>();

        var json = PlanFacts.Run(Through("pac", pac, args => args == "org who" ? File.ReadAllText(OrgWho) : PlanFactsTests.SolutionList));

        Assert.Equal(["org who", "solution list"], pac.Select(c => c.Args));
        AssertEchoed(pac);
        Assert.True(JsonDocument.Parse(json).RootElement.GetProperty("available").GetBoolean(), json);
    }

    [Fact]
    public void DriftLiveReader_sends_the_pac_list_and_the_az_list_to_the_programs_unchanged()
    {
        var pac = new List<Call>();
        var az = new List<Call>();
        var fakes = new LiveFakes
        {
            PacFn = Through("pac", pac, _ => File.ReadAllText(OrgWho)),
            AzFn = Through(DriftLiveReader.AzExecutable(OperatingSystem.IsWindows()), az),
        };

        fakes.Reader.Read("PowerOpsSeed", "ENV-VALUE-DEV");

        Assert.Equal(["org who --environment ENV-VALUE-DEV"], pac.Select(c => c.Args));
        Assert.Single(az);
        Assert.StartsWith("account get-access-token --resource https://", az[0].Args);
        Assert.EndsWith(" --query accessToken -o tsv", az[0].Args);
        AssertEchoed(pac);
        AssertEchoed(az);
    }

    // The resolver already runs on Windows with explicit PATHEXT strings (PacRunnerResolveTests); this uses the host's own value and order.
    [Fact]
    public void The_resolver_picks_the_file_that_the_real_PATHEXT_lists_first()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "PATHEXT is a Windows setting.");

        var extensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var bat = extensions.FindIndex(e => e.Equals(".bat", StringComparison.OrdinalIgnoreCase));
        var cmd = extensions.FindIndex(e => e.Equals(".cmd", StringComparison.OrdinalIgnoreCase));
        Assert.True(bat >= 0 && cmd >= 0, "PATHEXT lists both .bat and .cmd");

        Assert.Equal(bat < cmd ? "pac.bat" : "pac.cmd", Path.GetFileName(Resolve("pac")), ignoreCase: true);
        Assert.Equal("az.cmd", Path.GetFileName(Resolve(DriftLiveReader.AzExecutable(true))), ignoreCase: true);
    }
}