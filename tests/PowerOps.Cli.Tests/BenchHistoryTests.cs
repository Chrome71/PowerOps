using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class BenchHistoryTests
{
    private static readonly string History = Path.Combine(Golden.RepoRoot, "bench", "history");

    [Fact]
    public async Task Converter_reproduces_the_committed_records()
    {
        var check = await CheckRecords();
        Assert.True(check.Code == 0, check.Output);
    }

    [Fact]
    public async Task Converter_accepts_crlf_and_rejects_a_single_changed_byte()
    {
        var copy = Directory.CreateTempSubdirectory("powerops-history-");
        try
        {
            foreach (var path in Directory.GetFiles(Path.Combine(History, "records"), "*.json"))
            {
                File.WriteAllText(Path.Combine(copy.FullName, Path.GetFileName(path)),
                    File.ReadAllText(path).ReplaceLineEndings("\r\n"));
            }

            var check = await CheckRecords("--out", copy.FullName);
            Assert.True(check.Code == 0, check.Output);

            var changed = Path.Combine(copy.FullName, "m38-codex-review-plan.json");
            var bytes = File.ReadAllBytes(changed);
            bytes[Array.IndexOf(bytes, (byte)' ')] = (byte)'\t';
            File.WriteAllBytes(changed, bytes);
            check = await CheckRecords("--out", copy.FullName);
            Assert.NotEqual(0, check.Code);
            Assert.Contains("byte mismatch: " + Path.GetFileName(changed), check.Output);
        }
        finally
        {
            copy.Delete(recursive: true);
        }
    }

    private static async Task<(int Code, string Output)> CheckRecords(params string[] args)
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

            return await Run(file, [.. prefix, Path.Combine(History, "convert.py"), "--check", .. args]);
        }

        const string reason = "Python 3 is missing (tried python3, py -3 and python).";
        Assert.False(Environment.GetEnvironmentVariable("POWEROPS_REQUIRE_TOOLS") == "1", reason);
        throw Xunit.Sdk.SkipException.ForSkip(reason);
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
            var id = Path.GetFileNameWithoutExtension(file);
            Assert.Equal(id, record.GetProperty("record_id").GetString());
            Assert.Equal("historical_aggregate", record.GetProperty("granularity").GetString());
            var family = record.GetProperty("client").GetProperty("family").GetString();
            Assert.Contains($"-{family}-", Path.GetFileName(file), StringComparison.Ordinal);
            var claude = family == "claude";
            var cost = record.GetProperty("cost");
            Assert.Equal(claude ? "ratecard" : "unknown", cost.GetProperty("usd_basis").GetString());
            Assert.Equal(claude ? JsonValueKind.Number : JsonValueKind.Null, cost.GetProperty("usd").ValueKind);
            Assert.Equal(claude ? JsonValueKind.Null : JsonValueKind.Number, cost.GetProperty("tokens_in").ValueKind);
            Assert.Equal(claude ? JsonValueKind.Number : JsonValueKind.Null, cost.GetProperty("uncached_in_combined").ValueKind);
            NullFields(cost, "cached_write", "judge_usd", "judge_tokens", "runner_s", "copilot_credits");
            var reliability = record.GetProperty("reliability");
            var retries = id switch
            {
                "m38-codex-review-plan" => 4,
                "m38-codex-security-plan" or "m38-codex-security-solution" => 2,
                _ => 0
            };
            Assert.Equal(retries, reliability.GetProperty("retries").GetInt32());
            Assert.Equal(0, reliability.GetProperty("excluded_runs").GetInt32());
            NullFields(reliability, "pass^3", "pass@1", "safety_violations");
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
                Assert.Matches("`" + Regex.Escape(folder + "/" + Path.GetFileName(path)) + "[/`]", inventory);
            }
        }

        Assert.DoesNotContain("/" + "home/", inventory, StringComparison.Ordinal);
        Assert.DoesNotContain("file:" + "//", inventory, StringComparison.OrdinalIgnoreCase);
    }
}