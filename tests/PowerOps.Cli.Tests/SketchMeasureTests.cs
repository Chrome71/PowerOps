using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The sketch judge's measuring script and its calibration key (M97, rows 2 and 16 of docs/plans/m97-visual-baseline/plan.md).
public sealed partial class SketchMeasureTests
{
    private static readonly string SkillFolder = Path.Combine(Golden.RepoRoot, "skills", "powerops-review-sketch");

    [Fact]
    public void The_self_test_finds_each_planted_fault_and_leaves_no_file_in_the_skill()
    {
        var python = FindPython();
        Assert.SkipWhen(python is null, "No Python 3 found as python3, py -3 or python.");

        var start = new ProcessStartInfo(python!.Value.File) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in python.Value.Args.Append(Path.Combine(SkillFolder, "measure.py")).Append("--self-test"))
        {
            start.ArgumentList.Add(arg);
        }

        var (code, stdout, stderr) = Execute(start, TimeSpan.FromMinutes(2));

        // 77: no Chrome found, or a Chrome that can't render here; the script's message names which.
        Assert.SkipWhen(code == 77, stderr.Trim());
        Assert.True(code == 0, $"exit {code}\n{stdout}\n{stderr}");
        Assert.Contains("self-test: passed", stdout, StringComparison.Ordinal);
        Assert.Equal(["SKILL.md", "measure.py"], Directory.EnumerateFileSystemEntries(SkillFolder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Every_file_the_calibration_key_names_exists()
    {
        var key = File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "sketch-eval", "key.md"));
        var paths = KeyPath().Matches(key).Select(m => m.Groups[1].Value).ToList();

        Assert.Equal(9, paths.Count);
        Assert.All(paths, p => Assert.True(File.Exists(Path.Combine([Golden.RepoRoot, .. p.Split('/')])), p + " is missing"));
        Assert.All(Enumerable.Range(1, 5), n => Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, "tests", "sketch-eval", $"row-{n}", "plan.md")), $"row-{n}/plan.md is missing"));
    }

    // A Python 3, as the skills say to start it: python3, else py -3, else python; each checked by --version.
    private static (string File, string[] Args)? FindPython()
    {
        foreach (var (file, args) in new[] { ("python3", Array.Empty<string>()), ("py", ["-3"]), ("python", []) })
        {
            var start = new ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in args.Append("--version"))
            {
                start.ArgumentList.Add(arg);
            }

            try
            {
                var (code, stdout, stderr) = Execute(start, TimeSpan.FromSeconds(30));
                if (code == 0 && (stdout + stderr).TrimStart().StartsWith("Python 3", StringComparison.Ordinal))
                {
                    return (file, args);
                }
            }
            catch (Win32Exception)
            {
                // Not installed: try the next name.
            }
        }

        return null;
    }

    private static (int Code, string Out, string Err) Execute(ProcessStartInfo start, TimeSpan timeout)
    {
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(start.FileName + " did not finish in " + timeout + ".");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }

    // A path in backticks that names a file: tests/... or skills/..., ending .html or .md.
    [GeneratedRegex(@"`((?:tests|skills)/[^`<>]+\.(?:html|md))`")]
    private static partial Regex KeyPath();
}