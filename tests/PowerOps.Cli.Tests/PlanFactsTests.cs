using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class PlanFactsTests
{
    private const string OrgWho = """
        Connected as admin@contoso.com
        Connected to... Contoso Dev
        Organization Information
          Org ID:                     00000000-0000-0000-0000-000000000000
          Unique Name:                unq00000000
          Friendly Name:              Contoso Dev
          Org URL:                    https://contoso-dev.crm.dynamics.com/
          User Email:                 admin@contoso.com
          User ID:                    00000000-0000-0000-0000-000000000000
          Environment ID:             00000000-0000-0000-0000-000000000000
        """;

    internal const string SolutionList = """
        Connected as admin@contoso.com
        Connected to... Contoso Dev

        Listing all Solutions from the current Dataverse organization...
        Unique Name           Friendly Name                         Version Managed
        Cr00000               Common Data Services Default Solution 1.0.0.0 False
        ContosoSiteVisits     Contoso Site Visits                   1.2.0.0 False
        ContosoCore           Contoso Core                          3.0.1.0 True
        """;

    private static PacResult Ok(string stdout) => new(true, false, 0, stdout);

    private static (string Json, List<string> Calls) Run(Func<string, PacResult> pac)
    {
        var calls = new List<string>();
        var json = PlanFacts.Run(args =>
        {
            calls.Add(string.Join(" ", args));
            return pac(calls[^1]);
        });
        return (json, calls);
    }

    private static PacResult Healthy(string call) => Ok(call == "org who" ? OrgWho : SolutionList);

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Success_prints_names_only_and_runs_only_the_two_read_only_commands()
    {
        var (json, calls) = Run(Healthy);

        var root = Parse(json);
        Assert.True(root.GetProperty("available").GetBoolean());
        Assert.Equal("Contoso Dev", root.GetProperty("environment").GetString());
        var solutions = root.GetProperty("solutions").EnumerateArray().ToList();
        Assert.Equal(["Cr00000", "ContosoSiteVisits", "ContosoCore"], solutions.Select(s => s.GetProperty("uniqueName").GetString()));
        Assert.Equal(["1.0.0.0", "1.2.0.0", "3.0.1.0"], solutions.Select(s => s.GetProperty("version").GetString()));
        Assert.Equal([false, false, true], solutions.Select(s => s.GetProperty("managed").GetBoolean()));
        Assert.DoesNotContain("@", json);
        Assert.DoesNotContain("http", json);
        Assert.DoesNotMatch(new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-"), json);
        Assert.Equal(["org who", "solution list"], calls);
        Assert.EndsWith("\n", json);
    }

    [Fact]
    public void Success_json_matches_golden() => Golden.Verify(Run(Healthy).Json, "plan-facts.json");

    [Fact]
    public void Unavailable_json_matches_golden() =>
        Golden.Verify(Run(_ => new PacResult(false, false, 0, "")).Json, "plan-facts-unavailable.json");

    [Fact]
    public void Pac_not_found_is_unavailable()
    {
        var (json, calls) = Run(_ => new PacResult(false, false, 0, ""));

        var root = Parse(json);
        Assert.False(root.GetProperty("available").GetBoolean());
        Assert.Equal("pac was not found on PATH", root.GetProperty("reason").GetString());
        Assert.StartsWith("Install pac (dotnet tool install --global Microsoft.PowerApps.CLI.Tool)", root.GetProperty("fix").GetString());
        Assert.Equal(["org who"], calls);
    }

    [Fact]
    public void A_real_runner_with_a_missing_executable_reports_not_started()
    {
        var result = new PacRunner("powerops-no-such-pac").Run(["org", "who"]);

        Assert.False(result.Started);
        Assert.Contains("pac was not found on PATH", PlanFacts.Run(_ => result));
    }

    private static PacResult RunScript(string body, TimeSpan timeout, out TimeSpan elapsed)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Needs a POSIX shell.");
        var dir = Directory.CreateTempSubdirectory("powerops-pac-").FullName;
        try
        {
            var script = Path.Combine(dir, "pac.sh");
            File.WriteAllText(script, "#!/bin/sh\n" + body + "\n");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            // Through the shell: starting a file this process just wrote can fail with ETXTBSY while a parallel test forks.
            var result = new PacRunner("/bin/sh", timeout).Run([script]);
            elapsed = clock.Elapsed;
            return result;
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void A_pac_that_prints_and_exits_is_started_and_not_timed_out()
    {
        var result = RunScript("echo \"Friendly Name: X\"", TimeSpan.FromSeconds(10), out _);

        Assert.True(result.Started);
        Assert.False(result.TimedOut);
        Assert.Contains("Friendly Name: X", result.Stdout);
    }

    [Fact]
    public void A_pac_that_fails_keeps_its_exit_code_and_both_streams()
    {
        var result = RunScript("echo out; echo err >&2; exit 4", TimeSpan.FromSeconds(10), out _);

        Assert.Equal(4, result.ExitCode);
        Assert.Contains("out", result.Stdout);
        Assert.Equal("err", result.Stderr.Trim());
    }

    [Fact]
    public void A_pac_that_outlives_the_timeout_is_timed_out()
    {
        var result = RunScript("sleep 5", TimeSpan.FromSeconds(1), out var elapsed);

        Assert.True(result.TimedOut);
        Assert.True(elapsed < TimeSpan.FromSeconds(3));
    }

    [Theory]
    [InlineData("head -c 16777217 /dev/zero; sleep 30")]
    [InlineData("head -c 16777217 /dev/zero >&2; sleep 30")]
    public void Output_past_16_MB_on_a_stream_fails_the_run_and_stops_the_process(string body)
    {
        var result = RunScript(body, TimeSpan.FromSeconds(20), out var elapsed);

        Assert.True(result.Started);
        Assert.False(result.TimedOut);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal("", result.Stdout);
        Assert.Equal(PacRunner.TooLarge, result.Stderr);
        Assert.True(elapsed < TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Output_of_exactly_16_MB_is_read()
    {
        var result = RunScript("head -c 16777216 /dev/zero | tr '\\0' 'x'", TimeSpan.FromSeconds(20), out _);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(16 * 1024 * 1024, result.Stdout.Length);
    }

    [Fact]
    public void A_UTF8_BOM_on_either_stream_is_not_part_of_the_text()
    {
        var result = RunScript("printf '\\357\\273\\277{\"a\":1}'; printf '\\357\\273\\277err' >&2", TimeSpan.FromSeconds(10), out _);

        Assert.Equal("{\"a\":1}", result.Stdout);
        Assert.Equal("err", result.Stderr);
        using var doc = System.Text.Json.JsonDocument.Parse(result.Stdout);
        Assert.Equal(1, doc.RootElement.GetProperty("a").GetInt32());
    }

    [Fact]
    public void A_child_holding_the_pipe_after_pac_exits_still_times_out()
    {
        var result = RunScript("sleep 5 &\necho \"Friendly Name: X\"", TimeSpan.FromSeconds(1), out var elapsed);

        Assert.True(result.TimedOut);
        Assert.True(elapsed < TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Timeout_names_the_command()
    {
        var (json, _) = Run(call => call == "org who" ? Ok(OrgWho) : new PacResult(true, true, 0, SolutionList));

        var root = Parse(json);
        Assert.False(root.GetProperty("available").GetBoolean());
        Assert.Equal("`pac solution list` timed out after 30 s", root.GetProperty("reason").GetString());
        Assert.Contains("pac auth list", root.GetProperty("fix").GetString());
        Assert.DoesNotContain("Contoso", json);
    }

    [Fact]
    public void Non_zero_exit_is_unavailable_without_raw_output()
    {
        var (json, calls) = Run(_ => new PacResult(true, false, 1, "Error: admin@contoso.com is not signed in"));

        var root = Parse(json);
        Assert.Equal("`pac org who` failed (exit code 1)", root.GetProperty("reason").GetString());
        Assert.Equal("Sign in with pac auth create, then pick the environment with pac org select.", root.GetProperty("fix").GetString());
        Assert.DoesNotContain("contoso", json);
        Assert.Equal(["org who"], calls);
    }

    [Fact]
    public void Unparseable_org_output_is_unavailable()
    {
        var (json, _) = Run(_ => Ok("something unexpected"));

        Assert.Equal("`pac org who` printed no environment name", Parse(json).GetProperty("reason").GetString());
    }

    [Fact]
    public void Unparseable_solution_output_is_unavailable()
    {
        var (json, _) = Run(call => call == "org who" ? Ok(OrgWho) : Ok("nothing useful"));

        Assert.Equal("`pac solution list` printed no solution table", Parse(json).GetProperty("reason").GetString());
    }

    [Fact]
    public void Second_command_failing_after_the_first_succeeded_is_unavailable()
    {
        var (json, calls) = Run(call => call == "org who" ? Ok(OrgWho) : new PacResult(true, false, 3, ""));

        var root = Parse(json);
        Assert.False(root.GetProperty("available").GetBoolean());
        Assert.Equal("`pac solution list` failed (exit code 3)", root.GetProperty("reason").GetString());
        Assert.Equal(["org who", "solution list"], calls);
    }

    [Fact]
    public void Facts_subcommand_is_registered()
    {
        var o = new StringWriter();
        var code = Program.Run(["plan", "facts", "--help"], o, new StringWriter());

        Assert.Equal(0, code);
        Assert.Contains("facts", o.ToString());
    }
}