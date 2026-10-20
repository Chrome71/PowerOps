using System.Text.Json;
using System.Text.RegularExpressions;

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

    private const string SolutionList = """
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