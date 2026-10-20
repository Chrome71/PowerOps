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
}