using System.Diagnostics;
using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class PackedToolTests
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    // Packs, installs and runs the real tool, which proves the Core assembly ships in the package.
    [Fact]
    public void Packed_tool_analyzes_fixtures()
    {
        Assert.SkipWhen(Environment.GetEnvironmentVariable("POWEROPS_SKIP_E2E") == "1", "POWEROPS_SKIP_E2E=1");

        var temp = Directory.CreateTempSubdirectory("powerops-e2e-").FullName;
        try
        {
            var pkg = Path.Combine(temp, "pkg");
            var tool = Path.Combine(temp, "tool");
            var project = Path.Combine(Golden.RepoRoot, "src", "PowerOps.Cli");
            var fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");

            var pack = Run("dotnet", ["pack", project, "-c", Configuration, "-o", pkg]);
            Assert.True(pack.Code == 0, pack.Out + pack.Err);

            // The version comes from Directory.Build.props (release-please bumps it), so read it off the package name.
            var nupkg = Path.GetFileNameWithoutExtension(Directory.GetFiles(pkg, "PowerOps.*.nupkg").Single());
            var version = nupkg["PowerOps.".Length..];
            var install = Run("dotnet", ["tool", "install", "--tool-path", tool, "--add-source", pkg, "--ignore-failed-sources", "PowerOps", "--version", version]);
            Assert.True(install.Code == 0, install.Out + install.Err);

            var exe = Path.Combine(tool, OperatingSystem.IsWindows() ? "powerops.exe" : "powerops");

            var analyze = Run(exe, ["analyze", fixtures, "--format", "json", "--fail-on", "warning"]);
            Assert.True(analyze.Code == 1, analyze.Out + analyze.Err);
            using var doc = JsonDocument.Parse(analyze.Out);
            Assert.Equal(11, doc.RootElement.GetProperty("solutions").GetArrayLength());

            Assert.Equal(2, Run(exe, ["analyze", Path.Combine(temp, "missing")]).Code);

            // The skills are embedded in the package.
            var repo = Directory.CreateDirectory(Path.Combine(temp, "repo")).FullName;
            var skills = Run(exe, ["skills", "install", repo]);
            Assert.True(skills.Code == 0, skills.Out + skills.Err);
            Assert.True(File.Exists(Path.Combine(repo, ".agents", "skills", "powerops-plan", "SKILL.md")));
        }
        finally
        {
            try
            {
                Directory.Delete(temp, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best effort: cleanup must not mask the real test failure.
            }
        }
    }

    private static (int Code, string Out, string Err) Run(string file, string[] args)
    {
        var info = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Environment = { ["DOTNET_NOLOGO"] = "1", ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1" },
        };
        foreach (var a in args)
        {
            info.ArgumentList.Add(a);
        }

        using var process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{file} {string.Join(' ', args)} timed out");
        }

        process.WaitForExit(); // flush the async readers
        return (process.ExitCode, stdout.Result, stderr.Result);
    }
}