using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class BenchHistoryTests
{
    private static readonly string History = Path.Combine(Golden.RepoRoot, "bench", "history");

    [Fact]
    public async Task Converter_reproduces_the_committed_records_byte_for_byte()
    {
        foreach (var (file, prefix) in new[] { ("python3", Array.Empty<string>()), ("py", ["-3"]), ("python", []) })
        {
            try
            {
                var version = await Run(file, [.. prefix, "--version"]);
                if (version.Code != 0 || !version.Output.TrimStart().StartsWith("Python 3.", StringComparison.Ordinal))
                {
                    continue;
                }
            }
            catch (Win32Exception)
            {
                continue;
            }

            var check = await Run(file, [.. prefix, Path.Combine(History, "convert.py"), "--check"]);
            Assert.True(check.Code == 0, check.Output);
            return;
        }

        const string reason = "Python 3 is missing (tried python3, py -3 and python).";
        Assert.False(Environment.GetEnvironmentVariable("POWEROPS_REQUIRE_TOOLS") == "1", reason);
        Assert.Skip(reason);
    }

    private static async Task<(int Code, string Output)> Run(string file, string[] args)
    {
        var start = new ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await stdout + await stderr);
    }

    [Fact]
    public void Historical_records_preserve_cost_bases_and_unmeasured_fields()
    {
        var expected = from milestone in new[] { "m38", "m55", "m83", "m96" }
                       from family in new[] { "claude", "codex" }
                       from pass in (milestone == "m38"
                           ? new[] { "review-plan", "security-plan", "security-solution", "verify" }
                           : ["security-solution", "verify"])
                       select $"{milestone}-{family}-{pass}";
        var files = Directory.GetFiles(Path.Combine(History, "records"), "*.json");
        Assert.Equal(expected.Order(StringComparer.Ordinal), files.Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal));

        foreach (var file in files)
        {
            using var json = JsonDocument.Parse(File.ReadAllBytes(file));
            var record = json.RootElement;
            Assert.Equal(Path.GetFileNameWithoutExtension(file), record.GetProperty("record_id").GetString());
            Assert.Equal("historical_aggregate", record.GetProperty("granularity").GetString());
            var family = record.GetProperty("client").GetProperty("family").GetString();
            Assert.Contains($"-{family}-", Path.GetFileName(file), StringComparison.Ordinal);
            var claude = family == "claude";
            var cost = record.GetProperty("cost");
            Assert.Equal(claude ? "ratecard" : "unknown", cost.GetProperty("usd_basis").GetString());
            Assert.Equal(claude ? JsonValueKind.Number : JsonValueKind.Null, cost.GetProperty("usd").ValueKind);
            Assert.Equal(claude ? JsonValueKind.Null : JsonValueKind.Number, cost.GetProperty("tokens_in").ValueKind);
            Assert.Equal(claude ? JsonValueKind.Number : JsonValueKind.Null, cost.GetProperty("uncached_in_combined").ValueKind);
            NullFields(cost, "cached_write", "judge_usd", "runner_s", "copilot_credits");
            NullFields(record.GetProperty("reliability"), "pass^3", "pass@1", "safety_violations");
            NullFields(record.GetProperty("quality"), "checks", "highs_true", "mutant_kill_rate", "critical_fail");
            NullFields(record.GetProperty("time"), "first_artifact_s", "tool_wait_s", "approval_wait_s",
                "human_active_min", "turns", "tool_calls", "person_questions");
        }
    }

    private static void NullFields(JsonElement parent, params string[] fields) =>
        Assert.All(fields, field => Assert.Equal(JsonValueKind.Null, parent.GetProperty(field).ValueKind));

    [Fact]
    public void Lineage_inventory_names_each_fixture_case_pilot_and_sketch_row()
    {
        var inventory = File.ReadAllText(Path.Combine(Golden.RepoRoot, "maintainers", "lineages.md"));
        foreach (var folder in new[] { "tests/fixtures", "tests/eval/roles/cases", "tests/eval/roles/pilot", "tests/sketch-eval" })
        {
            foreach (var path in Directory.GetDirectories(Path.Combine([Golden.RepoRoot, .. folder.Split('/')])))
            {
                Assert.Contains(folder + "/" + Path.GetFileName(path), inventory, StringComparison.Ordinal);
            }
        }

        Assert.DoesNotContain("/" + "home/", inventory, StringComparison.Ordinal);
        Assert.DoesNotContain("file:" + "//", inventory, StringComparison.OrdinalIgnoreCase);
    }
}