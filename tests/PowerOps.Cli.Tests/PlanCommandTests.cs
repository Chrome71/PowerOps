using System.Text.Json;

namespace PowerOps.Cli.Tests;

public class PlanCommandTests : IDisposable
{
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-plan-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    private string PlanDir(string name = "site-visits") => Path.Combine(_temp, "docs", "plans", name);

    private void Plan(string header, string body = "# Plan\n", string name = "site-visits")
    {
        Directory.CreateDirectory(PlanDir(name));
        File.WriteAllText(Path.Combine(PlanDir(name), "plan.md"), $"---\n{header}\n---\n{body}");
    }

    [Fact]
    public void No_plans_prints_a_message_and_exits_zero()
    {
        var (code, output, _) = Run("plan", "status", _temp);

        Assert.Equal(0, code);
        Assert.Equal($"no plans found under {_temp}\n", output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void No_plans_json_has_an_empty_list()
    {
        var (code, output, _) = Run("plan", "status", _temp, "--json");

        using var doc = JsonDocument.Parse(output);
        Assert.Equal(0, code);
        Assert.Equal(0, doc.RootElement.GetProperty("plans").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("summary").GetProperty("plans").GetInt32());
    }

    [Fact]
    public void Missing_path_is_a_usage_error()
    {
        var (code, _, err) = Run("plan", "status", Path.Combine(_temp, "nope"));

        Assert.Equal(2, code);
        Assert.StartsWith("error: path not found:", err);
    }

    [Fact]
    public void Draft_plan_prints_one_line_and_a_summary()
    {
        Plan("status: draft\nreview: pending", "# Plan\n## Open questions\n- one\n");

        var (code, output, _) = Run("plan", "status", _temp);

        Assert.Equal(0, code);
        Assert.Contains(": draft, review pending, 1 open question\n", output.Replace("\r\n", "\n"));
        Assert.EndsWith("1 plan, 0 violations, 0 warnings\n", output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Malformed_headers_exit_two_for_each_kind()
    {
        string[] headers =
        [
            "status: aproved\nreview: pending",
            "status: draft\nreview: pending\nmood: good",
            "status: draft\nstatus: draft\nreview: pending",
            "status: draft\nreview: pending\nsigned-off: yesterday",
            "status: draft\nreview: pending\nsigned-off-hash: xyz",
            "status: draft\nreview: pending\ndepth: shallow",
            "status: draft\nreview: pending\nsupersedes: docs/plans/old",
        ];
        foreach (var header in headers)
        {
            Plan(header);
            var (code, output, _) = Run("plan", "status", _temp);
            Assert.Equal(2, code);
            Assert.Contains("malformed header", output);
        }

        File.WriteAllText(Path.Combine(PlanDir(), "plan.md"), "# no fences\n");
        Assert.Equal(2, Run("plan", "status", _temp).Code);
    }

    [Fact]
    public void Sign_off_then_status_is_clean_and_editing_the_body_fails()
    {
        Plan("status: approved\nreview: skipped");
        var plan = Path.Combine(PlanDir(), "plan.md");

        var (signCode, signOut, _) = Run("plan", "sign-off", PlanDir());
        Assert.Equal(0, signCode);
        Assert.StartsWith("signed off ", signOut);
        Assert.Contains($"signed-off: {DateTime.Now:yyyy-MM-dd}", File.ReadAllText(plan));
        Assert.Equal(0, Run("plan", "status", _temp).Code);

        File.AppendAllText(plan, "late edit\n");
        var (code, output, _) = Run("plan", "status", _temp);
        Assert.Equal(1, code);
        Assert.Contains("  error: changed since sign-off", output);
        Assert.EndsWith("1 plan, 1 violation, 0 warnings\n", output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Hand_written_sign_off_fails_and_names_the_command()
    {
        Plan("status: signed-off\nreview: skipped");

        var (code, output, _) = Run("plan", "status", _temp);

        Assert.Equal(1, code);
        Assert.Contains("run `powerops plan sign-off`", output);
    }

    [Fact]
    public void Json_has_the_documented_shape_and_is_printed_on_failure_too()
    {
        Plan("status: approved\nreview: done\nenvironment: Contoso Dev", "# Plan\n## Open questions\n- a\n- b\n");
        File.WriteAllText(Path.Combine(PlanDir(), "review.md"), $"plan-hash: {new string('a', 64)}\n");
        File.WriteAllText(Path.Combine(PlanDir(), "notes.md"), "mail me at a@contoso.example\n");

        var (code, output, _) = Run("plan", "status", _temp, "--json");

        Assert.Equal(1, code);
        Assert.EndsWith("}\n", output);
        using var doc = JsonDocument.Parse(output);
        var plan = doc.RootElement.GetProperty("plans")[0];
        Assert.EndsWith("docs/plans/site-visits", plan.GetProperty("path").GetString());
        Assert.Equal("approved", plan.GetProperty("status").GetString());
        Assert.Equal("done", plan.GetProperty("review").GetString());
        Assert.Equal(JsonValueKind.Null, plan.GetProperty("signedOff").ValueKind);
        Assert.Equal("Contoso Dev", plan.GetProperty("environment").GetString());
        Assert.Equal(2, plan.GetProperty("openQuestions").GetInt32());
        Assert.Matches("^[0-9a-f]{64}$", plan.GetProperty("bodyHash").GetString());
        Assert.Single(plan.GetProperty("violations").EnumerateArray());
        Assert.Single(plan.GetProperty("warnings").EnumerateArray());
        Assert.Equal(1, doc.RootElement.GetProperty("summary").GetProperty("violations").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("summary").GetProperty("warnings").GetInt32());
        Assert.DoesNotContain("a@contoso", output);
    }

    [Fact]
    public void A_stale_verify_report_is_a_warning_and_a_listed_report_and_the_exit_code_stays_zero()
    {
        Plan("status: draft\nreview: pending");
        File.WriteAllText(Path.Combine(PlanDir(), "verify.md"), $"state: ready\nplan-hash: {new string('a', 64)}\n");

        var (code, output, _) = Run("plan", "status", _temp);
        var (_, json, _) = Run("plan", "status", _temp, "--json");

        Assert.Equal(0, code);
        Assert.Contains("  report verify.md: state 'ready', stale true\n", output.Replace("\r\n", "\n"));
        Assert.Contains("  warning: verify.md was written for a different version of the plan (stale report)\n", output.Replace("\r\n", "\n"));
        Assert.EndsWith("0 violations, 1 warning\n", output.Replace("\r\n", "\n"));
        using var doc = JsonDocument.Parse(json);
        var report = Assert.Single(doc.RootElement.GetProperty("plans")[0].GetProperty("reports").EnumerateArray());
        Assert.Equal("verify.md", report.GetProperty("file").GetString());
        Assert.Equal("ready", report.GetProperty("state").GetString());
        Assert.True(report.GetProperty("stale").GetBoolean());
    }

    [Fact]
    public void Depth_and_supersedes_are_in_the_json_and_a_missing_superseded_plan_exits_one()
    {
        Plan("status: draft\nreview: pending\ndepth: quick\nsupersedes: old");

        var (code, output, _) = Run("plan", "status", _temp, "--json");
        Assert.Equal(1, code);
        using (var doc = JsonDocument.Parse(output))
        {
            var plan = doc.RootElement.GetProperty("plans")[0];
            Assert.Equal("quick", plan.GetProperty("depth").GetString());
            Assert.Equal("old", plan.GetProperty("supersedes").GetString());
            Assert.Contains("supersedes 'old' but", plan.GetProperty("violations")[0].GetString());
        }

        Plan("status: draft\nreview: pending", name: "old");
        Assert.Equal(0, Run("plan", "status", _temp).Code);
    }

    [Fact]
    public void Json_for_a_malformed_plan_has_null_header_fields_and_exits_two()
    {
        Plan("status: nope\nreview: done");

        var (code, output, _) = Run("plan", "status", _temp, "--json");

        using var doc = JsonDocument.Parse(output);
        var plan = doc.RootElement.GetProperty("plans")[0];
        Assert.Equal(2, code);
        Assert.Equal(JsonValueKind.Null, plan.GetProperty("status").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, plan.GetProperty("bodyHash").ValueKind);
    }

    [Fact]
    public void Sign_off_refusals_exit_one_and_missing_plan_exits_two()
    {
        Plan("status: draft\nreview: pending");
        var before = File.ReadAllText(Path.Combine(PlanDir(), "plan.md"));

        var (code, _, err) = Run("plan", "sign-off", Path.Combine(PlanDir(), "plan.md"));

        Assert.Equal(1, code);
        Assert.Contains("error: status is draft; sign-off needs status: approved", err);
        Assert.Equal(before, File.ReadAllText(Path.Combine(PlanDir(), "plan.md")));
        Assert.Equal(2, Run("plan", "sign-off", Path.Combine(_temp, "missing")).Code);
    }

    [Theory]
    [InlineData("status")]
    [InlineData("sign-off")]
    public void A_file_argument_must_be_named_plan_md(string verb)
    {
        var readme = Path.Combine(_temp, "README.md");
        File.WriteAllText(readme, "# readme\n");

        var (code, output, err) = Run("plan", verb, readme);

        Assert.Equal(2, code);
        Assert.Equal($"error: not a plan file: {readme}\n", err.Replace("\r\n", "\n"));
        Assert.Equal("", output);
    }

    [Fact]
    public void Sign_off_refuses_tenant_data_without_printing_it_or_touching_the_file()
    {
        Plan("status: approved\nreview: skipped", "# Plan\nsee https://contoso.example/x\nid 00000000-0000-0000-0000-000000000000\nmail a@contoso.example\n");
        var plan = Path.Combine(PlanDir(), "plan.md");
        var before = File.ReadAllBytes(plan);

        var (code, output, err) = Run("plan", "sign-off", PlanDir());

        Assert.Equal(1, code);
        Assert.Equal(before, File.ReadAllBytes(plan));
        foreach (var value in new[] { "contoso", "00000000-0000", "https" })
        {
            Assert.DoesNotContain(value, output);
            Assert.DoesNotContain(value, err);
        }
    }

    // The shape of the plan folder of the 2026-11-04 live run, marked as M54 asks: an orchestrating agent answered the
    // route, signed off and accepted findings, and the build left a stray in Dev.
    [Fact]
    public void Status_lists_the_journal_and_warns_on_an_agent_sign_off_or_acceptance_without_failing()
    {
        Plan("status: approved\nreview: skipped");
        Assert.Equal(0, Run("plan", "sign-off", PlanDir()).Code);
        File.WriteAllText(Path.Combine(PlanDir(), "decisions.md"), string.Join("\n",
            "# Decisions: site-visits",
            "",
            "## Round 1",
            "",
            "### Q1. Which route?",
            "1. Unmanaged in Dev, then managed to Test (recommended)",
            "2. In your own words",
            "Answer: 1 (answered by: agent orchestrator)",
            "",
            "## Log",
            "",
            "- approved (answered by: agent orchestrator)",
            "- signed off (answered by: agent orchestrator)",
            "- accepted S1, S2, S6 and V1 for Test (answered by: agent orchestrator)",
            ""));
        File.WriteAllText(Path.Combine(PlanDir(), "writes.md"), string.Join("\n",
            "# Writes: site-visits",
            "",
            "| # | Step | Actor | Authority | Operation | Outcome | Cleanup |",
            "|---|---|---|---|---|---|---|",
            "| 1 | 2 | agent orchestrator child | person: yes to Dev writes | create the Visit table with dv-metadata | done | none |",
            "| 2 | 3 | agent orchestrator child | person: yes to Dev writes | create two app modules with raw Web API | failed, two drafts left in Dev | open: two draft app modules; the person removes them |",
            ""));

        var (code, output, _) = Run("plan", "status", PlanDir());
        var (_, json, _) = Run("plan", "status", PlanDir(), "--json");

        var text = output.Replace("\r\n", "\n");
        Assert.Equal(0, code);
        Assert.Contains(": signed-off, review skipped, 0 open questions\n  writes writes.md: 2 entries, 1 open cleanup\n", text);
        Assert.Contains($"  warning: sign-off answered by an agent ({Path.Combine(PlanDir(), "decisions.md").Replace('\\', '/')}:13): only a person signs off a plan\n", text);
        Assert.Contains($"  warning: acceptance answered by an agent ({Path.Combine(PlanDir(), "decisions.md").Replace('\\', '/')}:14): only a person accepts a High finding or a security gap\n", text);
        Assert.EndsWith("1 plan, 0 violations, 2 warnings\n", text);
        using var doc = JsonDocument.Parse(json);
        var writes = doc.RootElement.GetProperty("plans")[0].GetProperty("writes");
        Assert.Equal(2, writes.GetProperty("entries").GetInt32());
        Assert.Equal(1, writes.GetProperty("openCleanup").GetInt32());
    }
}