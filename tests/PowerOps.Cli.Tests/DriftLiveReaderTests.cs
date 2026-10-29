using System.Net;
using System.Text;

using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Fake pac, az and web functions for the live reader. None of them echoes a secret.
internal sealed class LiveFakes
{
    public const string Token = "TOKEN-SECRET-123";

    private static readonly string Pac = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac");

    public List<IReadOnlyList<string>> PacCalls { get; } = [];

    public List<IReadOnlyList<string>> AzCalls { get; } = [];

    public List<(Uri Url, string Token)> WebCalls { get; } = [];

    // An environment value containing "test" (any case) is Contoso Test (managed samples); anything else is Contoso Dev.
    public Func<IReadOnlyList<string>, PacResult> PacFn { get; set; } = args =>
    {
        var text = File.ReadAllText(Path.Combine(Pac, "org-who.txt"));
        return new PacResult(true, false, 0, args[^1].Contains("test", StringComparison.OrdinalIgnoreCase) ? text.Replace("Contoso Dev", "Contoso Test").Replace("contoso-dev", "contoso-test") : text);
    };

    public Func<IReadOnlyList<string>, PacResult> AzFn { get; set; } = _ => new PacResult(true, false, 0, Token + "\n");

    public Func<Uri, WebResult> WebFn { get; set; } = url => Ok(Sample(url));

    public DriftLiveReader Reader => new(
        (a, _) =>
        {
            PacCalls.Add(a);
            return PacFn(a);
        },
        (a, _) =>
        {
            AzCalls.Add(a);
            return AzFn(a);
        },
        (u, t, _) =>
        {
            WebCalls.Add((u, t));
            return WebFn(u);
        });

    public static WebResult Ok(string body) => new(200, body, false);

    public static string EntitySet(Uri url) => url.AbsolutePath[(url.AbsolutePath.LastIndexOf('/') + 1)..];

    public static string LayerBody(bool active) =>
        $$"""{"value":[{{(active ? """{"msdyn_solutionname":"Active","msdyn_order":2},""" : "")}}{"msdyn_solutionname":"PowerOpsSeed","msdyn_order":1}]}""";

    // The component id in a layer request's FetchXML.
    public static string LayerId(Uri url) => Uri.UnescapeDataString(url.Query).Split("msdyn_componentid\" operator=\"eq\" value=\"")[1][..36];

    // The sample response for a request: dev or test by host, read 2 to 6 by entity set; a clean layer for read 7.
    public static string Sample(Uri url)
    {
        if (EntitySet(url) == "msdyn_componentlayers")
        {
            return LayerBody(false);
        }

        var read = EntitySet(url) switch
        {
            "solutions" => 2,
            "solutioncomponents" => 3,
            "workflows" => 4,
            "environmentvariabledefinitions" => 5,
            "connectionreferences" => 6,
            var other => throw new Xunit.Sdk.XunitException($"unexpected entity set {other}"),
        };
        return File.ReadAllText(Path.Combine(Pac, "drift", "sample", $"{(url.Host.Contains("test") ? "test" : "dev")}-read{read}.json"));
    }

    public static string SampleFile(string name) => File.ReadAllText(Path.Combine(Pac, "drift", "sample", name));
}

public class DriftLiveReaderTests
{
    private const string Env = "ENV-VALUE-DEV";
    private const string Solution = "PowerOpsSeed";

    private static readonly (string Table, string Set)[] Reads =
    [
        ("solution", "solutions"),
        ("solutioncomponent", "solutioncomponents"),
        ("workflow", "workflows"),
        ("environmentvariabledefinition", "environmentvariabledefinitions"),
        ("connectionreference", "connectionreferences"),
    ];

    private static DriftReadException Fails(LiveFakes f, string env = Env)
    {
        var e = Assert.Throws<DriftReadException>(() => f.Reader.Read(Solution, env));
        AssertNoSecrets(e.Message);
        AssertNoSecrets(e.Forwarded ?? "");
        return e;
    }

    private static void AssertNoSecrets(string text)
    {
        Assert.DoesNotContain(LiveFakes.Token, text);
        Assert.DoesNotContain(Env, text);
        Assert.DoesNotContain("crm.dynamics.com", text);
    }

    [Fact]
    public void A_dev_read_builds_the_snapshot_of_the_samples()
    {
        var s = new LiveFakes().Reader.Read(Solution, Env);

        Assert.Equal(("Contoso Dev", Solution, true, "1.0.0.0", false, true), (s.Environment, s.Solution, s.Installed, s.Version, s.Managed, s.LayersRead));
        Assert.Equal(
            [(1, "00000000-0000-0000-0000-0000000000d1"), (29, "4c8dd007-dfcb-f111-a05c-0022480697a8"), (380, "00000000-0000-0000-0000-0000000000d2")],
            s.Components.Select(c => (c.Type, c.Id.ToString())));
        Assert.All(s.Components, c => Assert.Null(c.UnmanagedLayer));
        Assert.Equal(Snapshot.Hash("Open, then\nreview"), s.Variables.Single().ValueHash);
        Assert.True(s.References.Single().Bound);
        Assert.Equal("on", s.Flows.Single().State);
    }

    private const string TestEnv = "ENV-VALUE-TEST";
    private const string FlowId = "4c8dd007-dfcb-f111-a05c-0022480697a8";

    private static string ComponentsBody(IEnumerable<(int Type, string Id)> rows) =>
        $$"""{"value":[{{string.Join(",", rows.Select(r => $$"""{"componenttype":{{r.Type}},"objectid":"{{r.Id}}"}"""))}}]}""";

    private static string Id(int n) => $"00000000-0000-0000-0000-{n:x12}";

    // Reads of a managed solution whose components are given; layers come from the fake (active for the ids in the set).
    private static LiveFakes ManagedWith(IEnumerable<(int Type, string Id)> rows, params string[] activeIds) => new()
    {
        WebFn = url => EntitySetIs(url, "solutioncomponents") ? LiveFakes.Ok(ComponentsBody(rows))
            : EntitySetIs(url, "msdyn_componentlayers") ? LiveFakes.Ok(LiveFakes.LayerBody(activeIds.Contains(LiveFakes.LayerId(url))))
            : LiveFakes.Ok(LiveFakes.Sample(url)),
    };

    private static bool EntitySetIs(Uri url, string set) => LiveFakes.EntitySet(url) == set;

    private static List<Uri> LayerCalls(LiveFakes f) => [.. f.WebCalls.Select(c => c.Url).Where(u => EntitySetIs(u, "msdyn_componentlayers"))];

    [Fact]
    public void A_managed_solution_reads_the_layers_of_each_mapped_component()
    {
        var f = new LiveFakes();

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.Equal(("Contoso Test", true, true), (s.Environment, s.Managed, s.LayersRead));
        Assert.All(s.Components, c => Assert.False(c.UnmanagedLayer));
        Assert.Equal(3, LayerCalls(f).Count);
    }

    [Fact]
    public void Each_layer_request_has_the_component_name_and_id_encoded_in_its_fetchxml()
    {
        var f = new LiveFakes();

        f.Reader.Read(Solution, TestEnv);

        var expected = new[] { (1, "Entity", Id(0xd1)), (29, "Workflow", FlowId), (380, "EnvironmentVariableDefinition", Id(0xd2)) }
            .Select(c => $"https://contoso-test.crm.dynamics.com/api/data/v9.2/msdyn_componentlayers?fetchXml={Uri.EscapeDataString(DriftQueries.Layers(Guid.Parse(c.Item3), c.Item2))}");
        Assert.Equal(expected.Order(), LayerCalls(f).Select(u => u.OriginalString).Order());
        Assert.All(f.WebCalls, c => Assert.Equal(LiveFakes.Token, c.Token));
    }

    [Fact]
    public void An_active_layer_sets_the_flag_and_other_components_stay_false()
    {
        var f = ManagedWith([(1, Id(1)), (29, FlowId), (300, Id(3))], FlowId);

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.Equal([(1, false), (29, true), (300, false)], s.Components.Select(c => (c.Type, c.UnmanagedLayer!.Value)));
    }

    [Fact]
    public void A_component_of_another_type_is_not_read_and_stays_null()
    {
        var f = ManagedWith([(1, Id(1)), (381, Id(2)), (10164, Id(3)), (2, Id(4))]);

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.Equal([(1, false), (2, null), (381, null), (10164, null)], s.Components.Select(c => (c.Type, c.UnmanagedLayer)));
        Assert.Single(LayerCalls(f));
    }

    [Fact]
    public void A_connection_reference_is_read_by_its_own_name_whatever_its_type_number()
    {
        var refId = Id(0xd3);
        var f = new LiveFakes
        {
            WebFn = url => EntitySetIs(url, "solutioncomponents") ? LiveFakes.Ok(ComponentsBody([(10164, refId), (1, Id(1))]))
                : EntitySetIs(url, "connectionreferences") ? LiveFakes.Ok(LiveFakes.Sample(url))
                : EntitySetIs(url, "msdyn_componentlayers") ? LiveFakes.Ok(LiveFakes.LayerBody(LiveFakes.LayerId(url) == refId))
                : LiveFakes.Ok(LiveFakes.Sample(url)),
        };

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.Equal([(1, false), (10164, true)], s.Components.Select(c => (c.Type, c.UnmanagedLayer!.Value)));
        Assert.Contains(DriftQueries.Layers(Guid.Parse(refId), "connectionreference"), LayerCalls(f).Select(u => Uri.UnescapeDataString(u.Query["?fetchXml=".Length..])));
    }

    [Fact]
    public void At_most_fifty_layer_reads_are_made()
    {
        var f = ManagedWith(Enumerable.Range(1, 50).Select(n => (1, Id(n))), Id(7));

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.True(s.LayersRead);
        Assert.Equal(50, LayerCalls(f).Count);
        Assert.Single(s.Components, c => c.UnmanagedLayer == true);
    }

    [Fact]
    public void More_than_fifty_mapped_components_make_no_layer_read_and_layers_not_read()
    {
        var f = ManagedWith(Enumerable.Range(1, 51).Select(n => (1, Id(n))));

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.False(s.LayersRead);
        Assert.Equal(51, s.Components.Count);
        Assert.All(s.Components, c => Assert.Null(c.UnmanagedLayer));
        Assert.Empty(LayerCalls(f));
    }

    [Fact]
    public void Only_mapped_components_count_towards_the_cap()
    {
        var f = ManagedWith(Enumerable.Range(1, 50).Select(n => (1, Id(n))).Concat(Enumerable.Range(100, 20).Select(n => (381, Id(n)))));

        var s = f.Reader.Read(Solution, TestEnv);

        Assert.True(s.LayersRead);
        Assert.Equal(50, LayerCalls(f).Count);
    }

    [Fact]
    public void An_unmanaged_solution_makes_no_layer_read()
    {
        var f = new LiveFakes();

        var s = f.Reader.Read(Solution, Env);

        Assert.True(s.LayersRead);
        Assert.All(s.Components, c => Assert.Null(c.UnmanagedLayer));
        Assert.Empty(LayerCalls(f));
    }

    [Fact]
    public void A_layer_result_with_no_rows_is_output_powerops_cannot_read()
    {
        var f = new LiveFakes { WebFn = url => EntitySetIs(url, "msdyn_componentlayers") ? LiveFakes.Ok("""{"value":[]}""") : LiveFakes.Ok(LiveFakes.Sample(url)) };

        var e = Fails(f, TestEnv);

        Assert.StartsWith("the Web API read of `msdyn_componentlayer` returned output powerops cannot read: ", e.Message);
        Assert.Contains("no layer rows", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void A_layer_read_that_times_out_or_fails_names_the_layer_table()
    {
        Assert.Equal("the Web API read of `msdyn_componentlayer` timed out after 30 s.", Fails(OnLayers(new WebResult(0, "", true)), TestEnv).Message);
        Assert.Equal("the Web API read of `msdyn_componentlayer` failed (HTTP 403). Sign in with az login, and check that the account can read that environment.",
            Fails(OnLayers(new WebResult(403, """{"error":{"code":"1","message":"No access"}}""", false)), TestEnv).Message);
    }

    private static LiveFakes OnLayers(WebResult broken) =>
        new() { WebFn = url => EntitySetIs(url, "msdyn_componentlayers") ? broken : LiveFakes.Ok(LiveFakes.Sample(url)) };

    [Fact]
    public void The_lists_come_out_sorted()
    {
        var f = new LiveFakes
        {
            WebFn = url => LiveFakes.EntitySet(url) == "solutioncomponents"
                ? LiveFakes.Ok("""{"value":[{"componenttype":380,"objectid":"00000000-0000-0000-0000-0000000000d2"},{"componenttype":1,"objectid":"00000000-0000-0000-0000-0000000000d1"}]}""")
                : LiveFakes.Ok(LiveFakes.Sample(url)),
        };

        Assert.Equal([1, 380], f.Reader.Read(Solution, Env).Components.Select(c => c.Type));
    }

    [Fact]
    public void Pac_and_az_get_exact_arguments_and_the_token_goes_only_to_the_web_function()
    {
        var f = new LiveFakes();

        f.Reader.Read(Solution, Env);

        Assert.Equal(["org", "who", "--environment", Env], Assert.Single(f.PacCalls));
        Assert.Equal(["account", "get-access-token", "--resource", "https://contoso-dev.crm.dynamics.com", "--query", "accessToken", "-o", "tsv"], Assert.Single(f.AzCalls));
        Assert.Equal(5, f.WebCalls.Count);
        Assert.All(f.WebCalls, c => Assert.Equal(LiveFakes.Token, c.Token));
        Assert.DoesNotContain(f.PacCalls.Concat(f.AzCalls).SelectMany(a => a), a => a.Contains(LiveFakes.Token));
        Assert.All(f.WebCalls, c => Assert.DoesNotContain(LiveFakes.Token, c.Url.OriginalString));
    }

    [Fact]
    public void The_requests_use_the_entity_sets_and_the_encoded_fetchxml_without_a_double_slash()
    {
        var f = new LiveFakes();
        var id = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

        f.Reader.Read(Solution, Env);

        Assert.Equal(Reads.Select(r => r.Set), f.WebCalls.Select(c => LiveFakes.EntitySet(c.Url)));
        string[] xml = [DriftQueries.Solution(Solution), DriftQueries.Components(id), DriftQueries.Flows(id), DriftQueries.Variables(id), DriftQueries.References(id)];
        for (var i = 0; i < 5; i++)
        {
            var url = f.WebCalls[i].Url.OriginalString;
            Assert.StartsWith($"https://contoso-dev.crm.dynamics.com/api/data/v9.2/{Reads[i].Set}?fetchXml=", url);
            Assert.DoesNotContain("//", url["https://".Length..]);
            Assert.EndsWith(Uri.EscapeDataString(xml[i]), url);
        }
    }

    [Fact]
    public void A_solution_that_is_not_installed_stops_after_read_2()
    {
        var f = new LiveFakes { WebFn = _ => LiveFakes.Ok(LiveFakes.SampleFile("not-installed-read2.json")) };

        var s = f.Reader.Read(Solution, Env);

        Assert.Equal(new Snapshot("Contoso Dev", Solution, false, null, null, true, [], [], [], []).Environment, s.Environment);
        Assert.False(s.Installed);
        Assert.Null(s.Version);
        Assert.Null(s.Managed);
        Assert.True(s.LayersRead);
        Assert.Empty(s.Components.Concat<object>(s.Variables).Concat(s.References).Concat(s.Flows));
        Assert.Single(f.WebCalls);
    }

    [Fact]
    public void Pac_not_found()
    {
        var e = Fails(new LiveFakes { PacFn = _ => new PacResult(false, false, 0, "") });

        Assert.Equal("pac was not found on PATH. Install pac (dotnet tool install --global Microsoft.PowerApps.CLI.Tool) and add the dotnet tools folder to PATH.", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void Pac_timeout_forwards_nothing_and_asks_for_no_token()
    {
        var f = new LiveFakes { PacFn = _ => new PacResult(true, true, 0, "") };

        var e = Fails(f);

        Assert.Equal("`pac org who` timed out after 30 s.", e.Message);
        Assert.Null(e.Forwarded);
        Assert.Empty(f.AzCalls);
    }

    [Fact]
    public void Pac_failure_forwards_stdout_then_stderr()
    {
        var e = Fails(new LiveFakes { PacFn = _ => new PacResult(true, false, 1, "out line\n", "err line\n") });

        Assert.Equal("`pac org who` failed (exit code 1). Sign in with pac auth create, and check that the account can read that environment.", e.Message);
        Assert.Equal("out line\nerr line\n", e.Forwarded);
    }

    [Theory]
    [InlineData("Connected\n  Org URL:                    https://contoso-dev.crm.dynamics.com/\n")]
    [InlineData("  Friendly Name:              Contoso Dev\n")]
    [InlineData("  Friendly Name:              Contoso Dev\n  Org URL:                    http://contoso-dev.crm.dynamics.com/\n")]
    [InlineData("  Friendly Name:              Contoso Dev\n  Org URL:                    https://contoso-dev.crm.dynamics.com/path\n")]
    public void An_unreadable_org_who_stops_before_the_token(string stdout)
    {
        var f = new LiveFakes { PacFn = _ => new PacResult(true, false, 0, stdout) };

        var e = Fails(f);

        Assert.Equal("`pac org who` printed no environment name, or an environment URL powerops does not accept.", e.Message);
        Assert.Null(e.Forwarded);
        Assert.Empty(f.AzCalls);
        Assert.Empty(f.WebCalls);
    }

    [Theory]
    [InlineData("https://contoso&calc.crm.dynamics.com")]
    [InlineData("https://contoso\".crm.dynamics.com")]
    [InlineData("https://contoso.crm.dynamics.com/&calc")]
    [InlineData("http://contoso.crm.dynamics.com")]
    public void A_url_with_cmd_metacharacters_never_reaches_az(string url)
    {
        var f = new LiveFakes();

        var e = Assert.Throws<DriftReadException>(() => f.Reader.Token(url));

        Assert.Equal(2, e.ExitCode);
        Assert.Empty(f.AzCalls);
        Assert.DoesNotContain(url, e.Message);
    }

    [Fact]
    public void Az_not_found()
    {
        var e = Fails(new LiveFakes { AzFn = _ => new PacResult(false, false, 0, "") });

        Assert.Equal("az was not found on PATH. Install the Azure CLI and sign in with az login.", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void Az_timeout_forwards_nothing()
    {
        var e = Fails(new LiveFakes { AzFn = _ => new PacResult(true, true, 0, "") });

        Assert.Equal("`az account get-access-token` timed out after 30 s.", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void Az_failure_forwards_stderr_only()
    {
        var f = new LiveFakes { AzFn = _ => new PacResult(true, false, 1, LiveFakes.Token, "ERROR: please run az login\n") };

        var e = Fails(f);

        Assert.Equal("`az account get-access-token` failed (exit code 1). Sign in with az login, and check that the account can read that environment.", e.Message);
        Assert.Equal("ERROR: please run az login\n", e.Forwarded);
        Assert.Empty(f.WebCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \r\n")]
    public void Az_with_no_token(string stdout)
    {
        var e = Fails(new LiveFakes { AzFn = _ => new PacResult(true, false, 0, stdout, "warning") });

        Assert.Equal("`az account get-access-token` printed no token.", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void The_token_is_trimmed()
    {
        var f = new LiveFakes { AzFn = _ => new PacResult(true, false, 0, "  " + LiveFakes.Token + "\r\n") };

        f.Reader.Read(Solution, Env);

        Assert.All(f.WebCalls, c => Assert.Equal(LiveFakes.Token, c.Token));
    }

    public static TheoryData<int> ReadIndexes() => [0, 1, 2, 3, 4];

    private static LiveFakes FailingAt(int read, WebResult result) =>
        new() { WebFn = url => LiveFakes.EntitySet(url) == Reads[read].Set ? result : LiveFakes.Ok(LiveFakes.Sample(url)) };

    [Theory]
    [MemberData(nameof(ReadIndexes))]
    public void A_web_timeout_names_the_table_and_forwards_nothing(int read)
    {
        var e = Fails(FailingAt(read, new WebResult(0, "", true)));

        Assert.Equal($"the Web API read of `{Reads[read].Table}` timed out after 30 s.", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Theory]
    [MemberData(nameof(ReadIndexes))]
    public void A_web_failure_names_the_table_and_the_fixed_text(int read)
    {
        Assert.Equal($"the Web API read of `{Reads[read].Table}` failed (transport error).", Fails(FailingAt(read, new WebResult(0, "", false, "transport error"))).Message);
        Assert.Equal($"the Web API read of `{Reads[read].Table}` failed (unreadable response).", Fails(FailingAt(read, new WebResult(200, "", false, "unreadable response"))).Message);
    }

    [Fact]
    public void A_400_forwards_the_error_message()
    {
        var e = Fails(FailingAt(1, new WebResult(400, LiveFakes.SampleFile("error-400.json"), false)));

        Assert.Equal("the Web API read of `solutioncomponent` failed (HTTP 400).", e.Message);
        Assert.Equal("Entity 'nosuchtable' With Id = 00000000-0000-0000-0000-000000000000 Does Not Exist", e.Forwarded);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public void A_401_or_403_adds_the_az_login_hint(int status)
    {
        var e = Fails(FailingAt(0, new WebResult(status, """{"error":{"code":"1","message":"No access"}}""", false)));

        Assert.Equal($"the Web API read of `solution` failed (HTTP {status}). Sign in with az login, and check that the account can read that environment.", e.Message);
        Assert.Equal("No access", e.Forwarded);
    }

    [Theory]
    [InlineData(302, "")]
    [InlineData(500, "<html>Server error</html>")]
    [InlineData(404, "")]
    public void Any_other_status_is_a_failed_read_with_no_forwarded_text_when_the_body_is_no_error_body(int status, string body)
    {
        var e = Fails(FailingAt(2, new WebResult(status, body, false)));

        Assert.Equal($"the Web API read of `workflow` failed (HTTP {status}).", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void More_records_is_output_powerops_cannot_read()
    {
        var e = Fails(FailingAt(1, LiveFakes.Ok(LiveFakes.SampleFile("morerecords.json"))));

        Assert.StartsWith("the Web API read of `solutioncomponent` returned output powerops cannot read: ", e.Message);
        Assert.Contains("more than one page of results is not supported", e.Message);
        Assert.Null(e.Forwarded);
    }

    [Fact]
    public void A_200_that_is_not_json_is_output_powerops_cannot_read()
    {
        var e = Fails(FailingAt(3, LiveFakes.Ok("<html>sign in</html>")));

        Assert.StartsWith("the Web API read of `environmentvariabledefinition` returned output powerops cannot read: ", e.Message);
        Assert.DoesNotContain("sign in", e.Message);
    }

    [Fact]
    public void An_error_text_with_status_200_is_output_powerops_cannot_read() =>
        Assert.StartsWith("the Web API read of `connectionreference` returned output", Fails(FailingAt(4, LiveFakes.Ok(LiveFakes.SampleFile("error-400.json")))).Message);

    // ---- ReadTarget (deploy plan) ----

    [Fact]
    public void ReadTarget_runs_pac_and_az_once_reads_each_solution_fully_and_a_dependency_by_its_solution_row_only()
    {
        var f = new LiveFakes
        {
            WebFn = url => LiveFakes.EntitySet(url) == "solutions" && url.OriginalString.Contains("DepLib")
                ? LiveFakes.Ok(LiveFakes.SampleFile("not-installed-read2.json"))
                : LiveFakes.Ok(LiveFakes.Sample(url)),
        };

        var t = f.Reader.ReadTarget(TestEnv, [Solution, "OtherSolution"], ["DepLib", "Lib2"]);

        Assert.Equal(["org", "who", "--environment", TestEnv], Assert.Single(f.PacCalls));
        Assert.Single(f.AzCalls);
        Assert.Equal(2 * 5 + 2, f.WebCalls.Count(c => LiveFakes.EntitySet(c.Url) != "msdyn_componentlayers"));
        Assert.All(f.WebCalls, c => Assert.Equal(LiveFakes.Token, c.Token));
        Assert.Equal(["solutions", "solutioncomponents", "workflows", "environmentvariabledefinitions", "connectionreferences"], f.WebCalls.Take(5).Select(c => LiveFakes.EntitySet(c.Url)));
        Assert.Equal(["solutions", "solutions"], f.WebCalls.TakeLast(2).Select(c => LiveFakes.EntitySet(c.Url)));
        Assert.Equal(("Contoso Test", Guid.Parse("00000000-0000-0000-0000-00000000000c")), (t.Name, t.Id));
        Assert.Equal([Solution, "OtherSolution"], t.Solutions.Select(s => s.Solution));
        Assert.All(t.Solutions, s => Assert.True(s.Installed && s.LayersRead));
        Assert.Equal(new Dictionary<string, string?> { ["DepLib"] = null, ["Lib2"] = "1.0.0.0" }, t.Dependencies);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(401)]
    public void ReadTarget_failure_leaks_neither_the_environment_nor_the_token(int status)
    {
        var f = new LiveFakes { WebFn = _ => new WebResult(status, """{"error":{"code":"1","message":"No access"}}""", false) };

        var e = Assert.Throws<DriftReadException>(() => f.Reader.ReadTarget(TestEnv, [Solution], ["DepLib"]));

        foreach (var text in new[] { e.Message, e.Forwarded ?? "" })
        {
            Assert.DoesNotContain(TestEnv, text);
            Assert.DoesNotContain(LiveFakes.Token, text);
            Assert.DoesNotContain("crm.dynamics.com", text);
        }
    }

    [Fact]
    public void ReadTarget_with_no_dependency_reads_only_the_solutions()
    {
        var f = new LiveFakes();

        var t = f.Reader.ReadTarget(TestEnv, [Solution], []);

        Assert.Equal(5, f.WebCalls.Count(c => LiveFakes.EntitySet(c.Url) != "msdyn_componentlayers"));
        Assert.Empty(t.Dependencies);
    }

    [Theory]
    [InlineData("Connected\n  Friendly Name:              Contoso Test\n  Org URL:                    https://contoso-test.crm.dynamics.com/\n")]
    [InlineData("  Friendly Name:              Contoso Test\n  Org URL:                    https://contoso-test.crm.dynamics.com/\n  Environment ID:             not-a-guid\n")]
    [InlineData("  Friendly Name:              Contoso Test\n  Org URL:                    https://contoso-test.crm.dynamics.com/\n  Environment ID:\n")]
    public void ReadTarget_without_a_readable_environment_id_exits_3_before_the_token(string stdout)
    {
        var f = new LiveFakes { PacFn = _ => new PacResult(true, false, 0, stdout) };

        var e = Assert.Throws<DriftReadException>(() => f.Reader.ReadTarget(TestEnv, [Solution], []));

        Assert.Equal("`pac org who` printed no environment id powerops can read.", e.Message);
        Assert.Equal(3, e.ExitCode);
        Assert.Null(e.Forwarded);
        Assert.Empty(f.AzCalls);
        Assert.Empty(f.WebCalls);
    }

    [Fact]
    public void Read_for_drift_does_not_need_an_environment_id()
    {
        var f = new LiveFakes { PacFn = _ => new PacResult(true, false, 0, "  Friendly Name:  Contoso Dev\n  Org URL:  https://contoso-dev.crm.dynamics.com/\n") };

        Assert.True(f.Reader.Read(Solution, Env).Installed);
    }

    [Fact]
    public void The_az_executable_depends_on_the_platform()
    {
        Assert.Equal("az.cmd", DriftLiveReader.AzExecutable(true));
        Assert.Equal("az", DriftLiveReader.AzExecutable(false));
    }

    // ---- DriftLiveReader.Web over a fake handler (no network) ----

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return send(request, cancellationToken);
        }
    }

    private static readonly Uri Url = new("https://contoso-dev.crm.dynamics.com/api/data/v9.2/solutions?fetchXml=x");

    private static FakeHandler Responding(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") }));

    [Fact]
    public void Web_sends_a_get_with_the_odata_headers_and_the_bearer_token_on_the_request_only()
    {
        var handler = Responding(HttpStatusCode.OK, """{"value":[]}""");
        var web = DriftLiveReader.Web(handler);

        var r = web(Url, "tok-1", TimeSpan.FromSeconds(5));
        web(Url, "tok-2", TimeSpan.FromSeconds(5));

        Assert.Equal(new WebResult(200, """{"value":[]}""", false), r);
        var q = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, q.Method);
        Assert.Equal(Url, q.RequestUri);
        Assert.Equal("application/json", q.Headers.Accept.ToString());
        Assert.Equal("4.0", q.Headers.GetValues("OData-MaxVersion").Single());
        Assert.Equal("4.0", q.Headers.GetValues("OData-Version").Single());
        Assert.Equal("odata.include-annotations=\"Microsoft.Dynamics.CRM.morerecords\"", q.Headers.GetValues("Prefer").Single());
        Assert.Equal(("Bearer", "tok-1"), (q.Headers.Authorization!.Scheme, q.Headers.Authorization.Parameter));
        Assert.Equal("tok-2", handler.Requests[1].Headers.Authorization!.Parameter);
    }

    [Fact]
    public void Web_returns_a_redirect_and_an_error_status_as_they_are()
    {
        Assert.Equal(new WebResult(302, "", false), DriftLiveReader.Web(Responding(HttpStatusCode.Found, ""))(Url, "t", TimeSpan.FromSeconds(5)));
        Assert.Equal(new WebResult(400, "bad", false), DriftLiveReader.Web(Responding(HttpStatusCode.BadRequest, "bad"))(Url, "t", TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Web_reads_a_body_of_exactly_16_MB_and_refuses_a_longer_one()
    {
        var limit = 16 * 1024 * 1024;

        var ok = DriftLiveReader.Web(Responding(HttpStatusCode.OK, new string('a', limit)))(Url, "t", TimeSpan.FromSeconds(30));
        var tooLong = DriftLiveReader.Web(Responding(HttpStatusCode.OK, new string('a', limit + 1)))(Url, "t", TimeSpan.FromSeconds(30));

        Assert.Equal((200, limit, null), (ok.Status, ok.Body.Length, ok.Failure));
        Assert.Equal(new WebResult(200, "", false, "unreadable response"), tooLong);
    }

    [Fact]
    public void Web_maps_every_exception_to_a_fixed_text_without_the_host()
    {
        Exception[] errors =
        [
            new HttpRequestException("failed to reach contoso-dev.crm.dynamics.com"),
            new IOException("contoso-dev.crm.dynamics.com reset"),
            new UriFormatException("contoso-dev.crm.dynamics.com"),
            new TaskCanceledException("contoso-dev.crm.dynamics.com", null, CancellationToken.None),
            new InvalidOperationException("contoso-dev.crm.dynamics.com"),
        ];

        foreach (var error in errors)
        {
            var r = DriftLiveReader.Web(new FakeHandler((_, _) => throw error))(Url, "secret-token", TimeSpan.FromSeconds(5));

            Assert.Equal(new WebResult(0, "", false, "transport error"), r);
        }
    }

    [Fact]
    public void Web_reports_its_own_timeout()
    {
        var handler = new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage();
        });

        var r = DriftLiveReader.Web(handler)(Url, "t", TimeSpan.FromMilliseconds(50));

        Assert.Equal(new WebResult(0, "", true), r);
    }

    [Fact]
    public void The_production_handler_does_not_follow_redirects() => Assert.False(DriftLiveReader.Handler().AllowAutoRedirect);
}