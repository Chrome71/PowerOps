using System.Diagnostics;
using System.Text;
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

            Mcp(exe);
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

    // `powerops mcp` over real stdio: one request at a time, each response read before the next write, because the server drops
    // responses to requests still in flight when stdin closes.
    private static void Mcp(string exe)
    {
        var info = new ProcessStartInfo(exe, "mcp")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            Environment = { ["DOTNET_NOLOGO"] = "1", ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1" },
        };
        using var process = Process.Start(info)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var lines = new List<string>();
        try
        {
            string Exchange(string request)
            {
                process.StandardInput.WriteLine(request);
                process.StandardInput.Flush();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var line = process.StandardOutput.ReadLineAsync(cts.Token).AsTask().GetAwaiter().GetResult() ?? throw new InvalidOperationException("the server closed stdout");
                lines.Add(line);
                return line;
            }

            Exchange("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"powerops-tests","version":"1"}}}""");
            process.StandardInput.WriteLine("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
            var tools = Exchange("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");
            var rules = Exchange("""{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"rules","arguments":{}}}""");
            process.StandardInput.Close();
            if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
            {
                throw new TimeoutException("powerops mcp did not exit after stdin closed");
            }

            Assert.Equal(0, process.ExitCode);
            lines.AddRange(process.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries));
            using var list = JsonDocument.Parse(tools);
            Assert.Equal(11, list.RootElement.GetProperty("result").GetProperty("tools").GetArrayLength());
            using var call = JsonDocument.Parse(rules);
            Assert.Equal(Run(exe, ["rules", "--json"]).Out, call.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString());
            Assert.Equal("", stderr.GetAwaiter().GetResult());
            Assert.All(lines, l =>
            {
                using var doc = JsonDocument.Parse(l);
                Assert.Equal("2.0", doc.RootElement.GetProperty("jsonrpc").GetString());
            });
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
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