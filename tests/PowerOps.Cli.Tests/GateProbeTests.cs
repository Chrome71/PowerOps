using System.Diagnostics;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The gate probe of docs/examples/gate-probe/: the pac stand-in, run only as a copy in a temp folder by its absolute path, and its README.
public sealed class GateProbeTests : IDisposable
{
    // The probe's arguments after the stand-in's absolute path, as the README's step (e) gives them and the gate test feeds them.
    internal const string ProbeArgs = "solution import --path /nonexistent/powerops-gate-probe.zip";

    internal static string ProbeCommand(string folder) => folder + "/pac " + ProbeArgs;

    private static readonly string Probe = Path.Combine(Golden.RepoRoot, "docs", "examples", "gate-probe");

    private readonly string _folder = Directory.CreateTempSubdirectory("powerops-gate-probe-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private (int Code, string Out, string Err) RunCopy()
    {
        var start = new ProcessStartInfo(Path.Combine(_folder, "pac")) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in ProbeArgs.Split(' '))
        {
            start.ArgumentList.Add(arg);
        }

        using var p = Process.Start(start)!;
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout.Result, stderr);
    }

    [Fact]
    public void The_stand_in_appends_a_marker_line_prints_one_probe_line_on_stderr_and_exits_1()
    {
        Tools.Require("bash");
        // This process never opens an executable for writing (a fork in a parallel test would then fail to start it with ETXTBSY): cp writes it.
        var start = new ProcessStartInfo("/bin/cp");
        start.ArgumentList.Add(Path.Combine(Probe, "pac"));
        start.ArgumentList.Add(Path.Combine(_folder, "pac"));
        using (var cp = Process.Start(start)!)
        {
            cp.WaitForExit();
            Assert.Equal(0, cp.ExitCode);
        }

        var marker = Path.Combine(_folder, "gate-probe.marker");
        var first = RunCopy();

        Assert.Equal(1, first.Code);
        Assert.Equal("", first.Out);
        Assert.StartsWith("gate probe: ", Assert.Single(first.Err.Split('\n', StringSplitOptions.RemoveEmptyEntries)), StringComparison.Ordinal);
        Assert.EndsWith(" pac " + ProbeArgs, Assert.Single(File.ReadAllLines(marker)), StringComparison.Ordinal);

        Assert.Equal(1, RunCopy().Code);
        Assert.Equal(2, File.ReadAllLines(marker).Length);
    }

    [Fact]
    public void The_stand_in_is_executable_and_kept_with_LF_endings()
    {
        var path = Path.Combine(Probe, "pac");
        Assert.DoesNotContain('\r', File.ReadAllText(path));
        if (!OperatingSystem.IsWindows())
        {
            Assert.True(File.GetUnixFileMode(path).HasFlag(UnixFileMode.UserExecute));
        }

        Assert.Contains("docs/examples/gate-probe/pac text eol=lf", File.ReadAllLines(Path.Combine(Golden.RepoRoot, ".gitattributes")));
    }

    [Fact]
    public void The_README_gives_the_probe_command_names_the_marker_and_links_a_HANDOFF_heading()
    {
        var readme = File.ReadAllText(Path.Combine(Probe, "README.md"));
        Assert.Contains(ProbeCommand("/home/<you>/powerops-gate-probe"), readme);
        Assert.Contains("gate-probe.marker", readme);
        Assert.Contains("](../../HANDOFF.md#gate-microsofts-imports)", readme);

        // GitHub's anchor of a heading: lowercase, punctuation dropped, spaces to hyphens.
        var anchors = File.ReadAllLines(Path.Combine(Golden.RepoRoot, "docs", "HANDOFF.md"))
            .Where(l => l.StartsWith('#'))
            .Select(l => Regex.Replace(l.TrimStart('#').Trim().ToLowerInvariant(), @"[^a-z0-9 -]", "").Replace(' ', '-'));
        Assert.Contains("gate-microsofts-imports", anchors);
    }
}