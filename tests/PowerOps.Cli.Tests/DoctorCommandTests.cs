using System.Text.Json;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// doctor with fake hosts: no test sets an environment variable or starts a real pac or az.
public sealed class DoctorCommandTests : IDisposable
{
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-doctor-").FullName;
    private readonly List<IReadOnlyList<string>> _pacCalls = [];
    private readonly List<IReadOnlyList<string>> _azCalls = [];

    private static readonly string OrgWho = File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "org-who.txt"));

    private const string Record = """
        {"version": 2, "plugins": {
          "powerops@powerops": [{"scope": "user", "version": "0.1.0", "installPath": "/x/powerops"}],
          "model-apps@power-platform-skills": [{"scope": "user", "version": "2.19.0", "installPath": "/x/model-apps"}],
          "dataverse@dataverse-skills": [{"scope": "project", "version": "1.16.1", "installPath": "/x/dv", "projectPath": "/p"}, {"scope": "project", "version": "1.16.1", "installPath": "/x/dv", "projectPath": "/q"}],
          "ponytail@ponytail": [{"scope": "user", "version": "5.3.3", "installPath": "/x/ponytail"}]
        }}
        """;

    private const string Graph = "https://graph.microsoft.com/v1.0/me/licenseDetails";

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private string Plugins(string? text)
    {
        var path = Path.Combine(_temp, "installed_plugins.json");
        if (text is not null)
        {
            File.WriteAllText(path, text);
        }

        return path;
    }

    // Fake pac and az files in a temp folder that the host gives as PATH: the shipped lookup finds them, nothing runs them.
    private string FakeTools()
    {
        var bin = Directory.CreateDirectory(Path.Combine(_temp, "bin")).FullName;
        var windows = OperatingSystem.IsWindows();
        foreach (var name in windows ? new[] { "pac.exe", "az.cmd" } : ["pac", "az"])
        {
            var file = Path.Combine(bin, name);
            File.WriteAllText(file, "exit 9\n");
            if (!windows)
            {
                File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            }
        }

        return bin;
    }

    // A host whose az runner fails the test when called; with no path, nothing is on PATH.
    private static DoctorHost Host(string record, string? path = null) =>
        new("0.0.0-test", path, ".EXE;.CMD", record, (_, _) => throw new InvalidOperationException("no az call"));

    private (int Code, string Out, string Err) Run(DoctorHost host, Func<IReadOnlyList<string>, PacResult>? pac = null, Func<IReadOnlyList<string>, PacResult>? az = null, params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var withAz = az is null ? host : host with
        {
            Az = (a, _) =>
            {
                _azCalls.Add(a);
                return az(a);
            },
        };
        var code = Program.Run(["doctor", .. args], o, e, (a, _) =>
        {
            _pacCalls.Add(a);
            return pac is null ? throw new InvalidOperationException("no pac call") : pac(a);
        }, doctor: withAz);
        return (code, o.ToString(), e.ToString());
    }

    private (int Code, string Out, string Err) Live(Func<IReadOnlyList<string>, PacResult> pac, Func<IReadOnlyList<string>, PacResult> az, params string[] args) =>
        Run(Host(Plugins(Record), FakeTools()), pac, az, ["--live", .. args]);

    private static PacResult Ok(string stdout = "") => new(true, false, 0, stdout);

    // A licence read that returns these lines, after a passing az probe and pac sign-in.
    private (int Code, string Out, string Err) Licences(string stdout, params string[] args) =>
        Live(_ => Ok(OrgWho), a => a[0] == "rest" ? Ok(stdout) : Ok(), args);

    [Fact]
    public void Plain_doctor_with_nothing_on_path_starts_no_process_and_exits_1()
    {
        var (code, output, err) = Run(Host(Plugins(null)));

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Assert.Empty(_pacCalls);
        Assert.Empty(_azCalls);
        Assert.StartsWith("powerops: ok - 0.0.0-test\npac: missing - needed by", output);
        Assert.Contains("powerops plugin: not installed - not installed in Claude Code.", output);
        Assert.EndsWith("Not checked: the sign-ins, licences and environment type. `powerops doctor --live` reads them through pac, az and Microsoft Graph.\n", output);
    }

    [Fact]
    public void Plain_doctor_finds_fake_pac_and_az_on_a_path_without_running_them()
    {
        var (code, output, _) = Run(Host(Plugins(Record), FakeTools()));

        Assert.Equal(0, code);
        Assert.Empty(_pacCalls);
        Assert.Empty(_azCalls);
        Assert.Contains("pac: ok - found on PATH\naz: ok - found on PATH\n", output);
        Assert.Contains("powerops plugin: ok - 0.1.0 (user) installed in Claude Code\nMicrosoft plugins: ok - dataverse@dataverse-skills 1.16.1, model-apps@power-platform-skills 2.19.0\n", output);
        Assert.DoesNotContain("ponytail", output);
        Assert.DoesNotContain("/x/", output);
    }

    [Fact]
    public void Plain_doctor_matches_the_goldens()
    {
        // pac and az not found and no Microsoft plugin, so the goldens hold missing and not-installed lines with their next steps.
        const string NoMicrosoft = """{"version": 2, "plugins": {"powerops@powerops": [{"scope": "user", "version": "0.1.0"}]}}""";
        var text = Run(Host(Plugins(NoMicrosoft)));
        var json = Run(Host(Plugins(NoMicrosoft)), args: ["--format", "json"]);

        Assert.Equal(1, text.Code);
        Assert.Equal(1, json.Code);
        Golden.Verify(text.Out, "doctor.txt");
        Golden.Verify(json.Out, "doctor.json");
        using var doc = JsonDocument.Parse(json.Out);
        Assert.Equal("schemaVersion", doc.RootElement.EnumerateObject().First().Name);
    }

    [Fact]
    public void Live_doctor_runs_the_three_calls_and_prints_product_names_only()
    {
        var (code, output, _) = Licences("POWERAPPS_PER_USER\nPOWER_BI_PRO\nSPE_E3\nPOWERAPPS_PER_USER\n", "--format", "json");

        Assert.Equal(0, code);
        Assert.Equal([["org", "who"]], _pacCalls);
        Assert.Equal([["account", "show", "-o", "none"], ["rest", "--method", "get", "--url", Graph, "--query", "value[].skuPartNumber", "-o", "tsv"]], _azCalls);
        Assert.DoesNotContain("POWERAPPS_PER_USER", output);
        Assert.DoesNotContain("SPE_E3", output);
        Assert.DoesNotContain("admin@contoso.com", output);
        Assert.DoesNotContain("crm.dynamics.com", output);
        Assert.DoesNotContain("00000000", output);
        Golden.Verify(output, "doctor-live.json");
    }

    [Theory]
    [InlineData("Microsoft_365_E3_(no_Teams)\nO365_w/o Teams Bundle_M3\n", "none in PowerOps' table (2 other)")]
    [InlineData("Microsoft_365_E3_(no_Teams)\nPOWERAPPS_PER_USER\nO365_w/o Teams Bundle_M3\n", "Power Apps Premium; 2 not in PowerOps' table")]
    [InlineData("POWERAPPS_PER_USER\r\nPOWER_BI_PRO\r\n", "Power Apps Premium, Power BI Pro")]
    public void Part_numbers_map_to_products_and_the_rest_is_a_count(string stdout, string detail)
    {
        var (code, output, _) = Licences(stdout);

        Assert.Equal(0, code);
        Assert.Contains($"licences: ok - {detail}\n", output);
        Assert.DoesNotContain("Teams", output);
    }

    [Theory]
    [InlineData("POWERAPPS_PER_USER\nBAD\u0007LINE\n")]
    [InlineData("POWERAPPS_PER_USER\nA\tB\n")]
    [InlineData("\n\n")]
    [InlineData("")]
    public void A_licence_line_that_is_no_part_number_is_a_failed_read_and_never_echoed(string stdout)
    {
        var (code, output, _) = Licences(stdout);

        Assert.Equal(0, code);
        Assert.Contains("licences: not read - the licence read failed; the planning skill asks which products you hold instead\n", output);
        Assert.DoesNotContain("BAD", output);
        Assert.DoesNotContain("Power Apps Premium", output);
    }

    [Fact]
    public void A_part_number_over_256_characters_is_a_failed_read() =>
        Assert.Contains("licences: not read - the licence read failed", Licences("POWERAPPS_PER_USER\n" + new string('A', 257) + "\n").Out);

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, false, -1)]
    public void A_probe_that_passes_and_a_read_that_fails_print_only_the_failure_notice(bool started, bool timedOut, int exit)
    {
        const string Secret = "AADSTS135010 user@contoso.com tenant 00000000-0000-0000-0000-000000000001";
        var (code, output, err) = Live(_ => Ok(OrgWho), a => a[0] == "rest" ? new PacResult(started, timedOut, exit, "POWERAPPS_PER_USER\n" + Secret, Secret) : Ok());

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Assert.Contains("az sign-in: ok - an account is selected (no token requested)\n", output);
        Assert.Contains("licences: not read - the licence read failed; the planning skill asks which products you hold instead\n", output);
        Assert.DoesNotContain("AADSTS", output);
        Assert.DoesNotContain("Power Apps Premium", output);
        Assert.Equal(2, _azCalls.Count);
    }

    [Fact]
    public void Failed_signins_print_the_fix_not_the_tool_output_and_skip_the_licence_read()
    {
        var (code, output, err) = Live(_ => new PacResult(true, false, 1, "Error: user@contoso.com has no profile", "stderr user@contoso.com"),
            _ => new PacResult(true, false, 1, "", "ERROR: Please run 'az login' for user@contoso.com"));

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Assert.Single(_azCalls);
        Assert.Contains("pac sign-in: failed - Sign in with pac auth create, then pick the environment with pac org select.\n", output);
        Assert.Contains("az sign-in: failed - Sign in with az login.\n", output);
        Assert.Contains("licences: not read - needs the az sign-in\n", output);
        Assert.Contains("environment type: not read - no documented read", output);
        Assert.DoesNotContain("contoso", output);
    }

    [Fact]
    public void Timed_out_probes_fail_without_their_output()
    {
        var (code, output, _) = Live(_ => new PacResult(true, true, 0, OrgWho), _ => new PacResult(true, true, 0, "secret-out", "secret-err"));

        Assert.Equal(1, code);
        Assert.Contains("pac sign-in: failed", output);
        Assert.Contains("az sign-in: failed", output);
        Assert.DoesNotContain("Contoso", output);
        Assert.DoesNotContain("secret", output);
    }

    [Fact]
    public void A_control_character_in_the_environment_name_is_cleaned()
    {
        var (_, output, _) = Live(_ => Ok(OrgWho.Replace("Friendly Name:              Contoso Dev", "Friendly Name:              Contoso\u001b[31mDev")), _ => Ok("POWER_BI_PRO\n"));

        Assert.DoesNotContain('\u001b', output); // a char: a culture compare would ignore it
        Assert.Contains("pac sign-in: ok - Contoso", output);
    }

    [Fact]
    public void Live_doctor_without_pac_or_az_calls_neither()
    {
        var (code, output, _) = Run(Host(Plugins(null)), args: "--live");

        Assert.Equal(1, code);
        Assert.Empty(_pacCalls);
        Assert.Empty(_azCalls);
        Assert.Contains("pac sign-in: not read - pac is missing\naz sign-in: not read - az is missing\nlicences: not read - needs the az sign-in\nenvironment type: not read - ", output);
        Assert.DoesNotContain("Not checked:", output);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"version": 3, "plugins": {}}""")]
    [InlineData("""{"version": 2, "plugins": []}""")]
    [InlineData("""{"version": 2, "plugins": {"powerops@powerops": {"version": "1"}}}""")]
    [InlineData("""{"version": 2, "plugins": {"powerops@powerops": [{"version": 1, "scope": "user"}]}}""")]
    [InlineData("""{"version": 2, "plugins": {"powerops": [{"version": "1", "scope": "user"}]}}""")]
    [InlineData("""{"version": 2, "plugins": {"x@y": [{"scope": "user", "version": "1", "a": 1, "a": 2}]}}""")]
    [InlineData("""{"version": 2, "version": 2, "plugins": {}}""")]
    public void An_unknown_plugin_record_is_not_read_and_keeps_exit_0(string text)
    {
        var (code, output, _) = Run(Host(Plugins(text), FakeTools()));

        Assert.Equal(0, code);
        Assert.Contains("powerops plugin: not read - Claude Code's plugin record has a shape PowerOps doesn't read\nMicrosoft plugins: not read - ", output);
    }

    [Fact]
    public void A_plugin_record_over_1_mb_is_not_read()
    {
        var (code, output, _) = Run(Host(Plugins("{\"version\": 2, \"plugins\": {}, \"pad\": \"" + new string('x', 1024 * 1024) + "\"}"), FakeTools()));

        Assert.Equal(0, code);
        Assert.Contains("powerops plugin: not read", output);
    }

    [Fact]
    public void A_control_character_in_a_plugin_version_is_cleaned()
    {
        var (_, output, _) = Run(Host(Plugins("""{"version": 2, "plugins": {"powerops@powerops": [{"scope": "user", "version": "1.0\u001b[2J"}]}}"""), FakeTools()));

        Assert.DoesNotContain('\u001b', output); // a char: a culture compare would ignore it
        Assert.Contains("powerops plugin: ok - 1.0", output);
    }

    [Fact]
    public void A_record_without_powerops_or_microsoft_plugins_says_not_installed()
    {
        var (code, output, _) = Run(Host(Plugins("""{"version": 2, "plugins": {"ponytail@ponytail": [{"scope": "user", "version": "5.3.3"}]}}"""), FakeTools()));

        Assert.Equal(0, code);
        Assert.Contains("powerops plugin: not installed - not installed in Claude Code. claude plugin marketplace add Chrome71/PowerOps, then claude plugin install powerops@powerops; or powerops skills install in the repo\n", output);
        Assert.Contains("Microsoft plugins: not installed - none of Microsoft's plugins in Claude Code; the build steps use them (docs/HANDOFF.md)\n", output);
    }

    [Fact]
    public void The_plugin_record_path_follows_claude_codes_variables()
    {
        Assert.Equal(Path.Combine("c", "installed_plugins.json"), DoctorHost.PluginRecordPath(v => v == "CLAUDE_CODE_PLUGIN_CACHE_DIR" ? "c" : "d", "h"));
        Assert.Equal(Path.Combine("d", "plugins", "installed_plugins.json"), DoctorHost.PluginRecordPath(v => v == "CLAUDE_CONFIG_DIR" ? "d" : null, "h"));
        Assert.Equal(Path.Combine("h", ".claude", "plugins", "installed_plugins.json"), DoctorHost.PluginRecordPath(_ => "", "h"));
    }

    [Fact]
    public void The_licence_table_is_every_row_of_licensing_md_section_3()
    {
        var text = File.ReadAllText(Path.Combine(Golden.RepoRoot, "skills", "powerops-plan", "references", "licensing.md")).Replace("\r\n", "\n");
        var section = text[text.IndexOf("\n## 3.", StringComparison.Ordinal)..];
        section = section[..section.IndexOf("\n## ", 1, StringComparison.Ordinal)];
        var rows = section.Split('\n').Count(l => l.StartsWith("| `", StringComparison.Ordinal));
        var table = DoctorCommand.PartNumbers();

        Assert.True(rows >= 14, $"{rows} rows");
        Assert.Equal(rows, table.Count);
        Assert.All(table, t => Assert.Matches("^[A-Za-z0-9_]+$", t.Part));
        Assert.All(table, t => Assert.False(string.IsNullOrWhiteSpace(t.Product)));
        Assert.Contains(("POWERAPPS_PER_USER", "Power Apps Premium"), table);
    }

    [Fact]
    public void An_unknown_format_is_a_usage_error() =>
        Assert.Equal(2, Run(Host(Plugins(null)), args: ["--format", "sarif"]).Code);
}