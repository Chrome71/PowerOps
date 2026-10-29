using System.Diagnostics;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Runs docs/examples/powerops/ephemeral-env.sh and ephemeral-sweep.sh with bash, against stub `pac` and `gh` programs that come first
// on PATH. The `pac admin list --json` fixtures use the field names observed with pac 2.13.1 and fake values. Skipped on Windows and
// where bash or jq is not found, as ActionScriptTests.
public sealed class EphemeralScriptTests : IDisposable
{
    private const string Prefix = "powerops-pr";
    private const string RepoId = "4242";

    private static readonly string Scripts = Path.Combine(Golden.RepoRoot, "docs", "examples", "powerops");

    private readonly string _root = Directory.CreateTempSubdirectory("powerops-ephemeral-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static string Name(int pr = 7, long run = 1000, int attempt = 1, string repo = RepoId) => $"{Prefix}-{repo}-pr{pr}-{run}-{attempt}";

    private static string Id(char hex) => $"{new string(hex, 8)}-{new string(hex, 4)}-{new string(hex, 4)}-{new string(hex, 4)}-{new string(hex, 12)}";

    private static string Url(string label) => $"https://{label}.crm.dynamics.com";

    private static string Item(string name, string id, string? url, string type = "Sandbox") =>
        $$"""{"DisplayName":"{{name}}","DomainName":"{{name}}","EnvironmentId":"{{id}}","EnvironmentUrl":{{(url is null ? "null" : $"\"{url}\"")}},"GroupName":null,"OrganizationId":"{{id}}","Type":"{{type}}","Version":"9.2.0.1"}""";

    private static string List(params string[] items) => "[" + string.Join(",", items) + "]";

    // Environments of other people that no output may name.
    private static string Others() => string.Join(",", Item("contoso-prod-secret", Id('d'), Url("orgprodsecret"), "Production"), Item("contoso-sales-secret", Id('e'), Url("orgsalessecret")));

    private static readonly string[] OtherMarks = ["contoso-prod-secret", "contoso-sales-secret", Id('d'), Id('e'), "orgprodsecret", "orgsalessecret", "leak-delete", "noise"];

    // ---- ephemeral-env.sh find ----

    [Fact]
    public void Find_with_one_match_writes_the_id_and_url_and_nothing_else()
    {
        var run = RunEnv("find", [List(Item(Name(), Id('a'), Url("orgpr7x")), Item("x", Id('f'), Url("orgother")), Others())]);

        Assert.Equal(0, run.Code);
        Assert.Equal($"id={Id('a')}\nurl={Url("orgpr7x")}\n", run.Output);
        Assert.Equal(["admin list --json"], run.PacCalls);
        AssertNoLeak(run, "orgother", Id('f'));
    }

    [Fact]
    public void Find_registers_every_mask_before_it_writes_an_output()
    {
        var run = RunEnv("find", [List(Item(Name(), Id('a'), Url("orgpr7x")))], outputToStdout: true);

        Assert.Equal(0, run.Code);
        var lines = run.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstOutput = Array.FindIndex(lines, l => l.StartsWith("id=", StringComparison.Ordinal) || l.StartsWith("url=", StringComparison.Ordinal));
        Assert.True(firstOutput > 0);
        Assert.All(lines.Take(firstOutput), l => Assert.StartsWith("::add-mask::", l, StringComparison.Ordinal));
        var masks = lines.Take(firstOutput).Select(l => l["::add-mask::".Length..]).ToList();
        foreach (var expected in new[]
        {
            Id('a'), Id('a').ToUpperInvariant(),
            "https://orgpr7x.crm.dynamics.com", "https://orgpr7x.crm.dynamics.com/", "orgpr7x.crm.dynamics.com", "ORGPR7X.CRM.DYNAMICS.COM", "https://ORGPR7X.CRM.DYNAMICS.COM/",
            "orgpr7x", "ORGPR7X",
        })
        {
            Assert.Contains(expected, masks);
        }

        Assert.Equal([$"id={Id('a')}", $"url={Url("orgpr7x")}"], lines.Skip(firstOutput));
    }

    [Fact]
    public void Find_takes_only_the_exact_name_so_attempt_1_is_not_attempt_10()
    {
        var run = RunEnv("find", [List(Item(Name(attempt: 10), Id('b'), Url("orgten")), Item(Name(attempt: 1), Id('a'), Url("orgone")), Item(Name() + "-extra", Id('c'), Url("orgextra")))]);

        Assert.Equal(0, run.Code);
        Assert.Equal($"id={Id('a')}\nurl={Url("orgone")}\n", run.Output);
    }

    [Fact]
    public void Find_with_no_match_retries_then_fails_and_writes_no_output()
    {
        var run = RunEnv("find", [List(Item("other", Id('c'), Url("orgother")))], extra: new() { ["EPHEMERAL_FIND_TRIES"] = "3" });

        Assert.Equal(1, run.Code);
        Assert.Contains("::error::", run.Out);
        Assert.Equal(3, run.PacCalls.Length);
        Assert.Equal("", run.Output);
        AssertNoLeak(run, "orgother", Id('c'));
    }

    [Fact]
    public void Find_retries_until_the_environment_shows_up()
    {
        var run = RunEnv("find", [List(), List(), List(Item(Name(), Id('a'), Url("orgpr7x")))]);

        Assert.Equal(0, run.Code);
        Assert.Equal(3, run.PacCalls.Length);
        Assert.Equal($"id={Id('a')}\nurl={Url("orgpr7x")}\n", run.Output);
    }

    [Fact]
    public void Find_retries_a_failed_or_non_json_list_like_zero_matches()
    {
        var failing = RunEnv("find", [List(Item(Name(), Id('a'), Url("orgpr7x")))], extra: new() { ["PAC_LIST_FAILS"] = "2" });
        var banner = RunEnv("find", ["Welcome to pac\n[]", List(Item(Name(), Id('a'), Url("orgpr7x")))]);

        Assert.Equal(0, failing.Code);
        Assert.Equal(3, failing.PacCalls.Length);
        Assert.Equal($"id={Id('a')}\nurl={Url("orgpr7x")}\n", failing.Output);
        Assert.Equal(0, banner.Code);
        Assert.Equal(2, banner.PacCalls.Length);
        Assert.Equal($"id={Id('a')}\nurl={Url("orgpr7x")}\n", banner.Output);
    }

    [Fact]
    public void Find_with_two_matches_fails_at_once_and_writes_no_output()
    {
        var run = RunEnv("find", [List(Item(Name(), Id('a'), Url("orgone")), Item(Name(), Id('b'), Url("orgtwo")))]);

        Assert.Equal(1, run.Code);
        Assert.Contains("::error::", run.Out);
        Assert.Single(run.PacCalls);
        Assert.Equal("", run.Output);
        AssertNoLeak(run, "orgone", "orgtwo", Id('a'), Id('b'));
    }

    [Theory]
    [InlineData("not-a-guid", "https://orgpr7x.crm.dynamics.com")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "http://orgpr7x.crm.dynamics.com")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "https://orgpr7x.example.com")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "https://evil.example.com/.dynamics.com")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "https://orgpr7x.crm.dynamics.com:8443")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "https://user@orgpr7x.crm.dynamics.com")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "https://dynamics.com")]
    public void Find_refuses_an_id_or_url_of_the_wrong_shape(string id, string url)
    {
        var run = RunEnv("find", [List(Item(Name(), id, url))]);

        Assert.Equal(1, run.Code);
        Assert.Contains("::error::", run.Out);
        Assert.Equal("", run.Output);
        Assert.DoesNotContain("add-mask", run.Out);
    }

    [Fact]
    public void Find_stops_when_a_field_is_missing_or_the_list_is_not_json()
    {
        var noUrl = RunEnv("find", [$$"""[{"DisplayName":"{{Name()}}","EnvironmentId":"{{Id('a')}}"}]"""]);
        var banner = RunEnv("find", ["Welcome to pac\n[]"]);
        var notArray = RunEnv("find", ["""{"DisplayName":"x"}"""]);

        Assert.All(new[] { noUrl, banner, notArray }, r =>
        {
            Assert.Equal(1, r.Code);
            Assert.Contains("::error::", r.Out);
            Assert.Equal("", r.Output);
        });
    }

    [Fact]
    public void A_failing_pac_prints_an_error_line_only_and_its_text_with_debug_logging()
    {
        var quiet = RunEnv("find", [List()], extra: new() { ["PAC_LIST_EXIT"] = "1" });
        var debug = RunEnv("find", [List()], extra: new() { ["PAC_LIST_EXIT"] = "1", ["RUNNER_DEBUG"] = "1" });

        Assert.Equal(1, quiet.Code);
        Assert.Contains("::error::", quiet.Out);
        Assert.DoesNotContain("secret", quiet.Out + quiet.Err);
        Assert.Equal(1, debug.Code);
        Assert.Contains("contoso-secret-env", debug.Out + debug.Err);
    }

    [Theory]
    [InlineData("EPHEMERAL_PREFIX", "Bad_Prefix")]
    [InlineData("EPHEMERAL_PREFIX", "a")]
    [InlineData("EPHEMERAL_PREFIX", "powerops-pr-with-a-prefix-too-long")]
    [InlineData("EPHEMERAL_PREFIX", "1abc")]
    [InlineData("REPO_ID", "42 42")]
    [InlineData("PR_NUMBER", "7; rm -rf /")]
    [InlineData("RUN_ID", "")]
    [InlineData("RUN_ATTEMPT", "x")]
    public void The_env_script_refuses_a_bad_input_and_calls_pac_never(string variable, string value)
    {
        var run = RunEnv("find", [List()], extra: new() { [variable] = value });

        Assert.NotEqual(0, run.Code);
        Assert.Empty(run.PacCalls);
    }

    [Theory]
    [InlineData("EPHEMERAL_PREFIX", "Bad_Prefix")]
    [InlineData("REPO_ID", "42 42")]
    [InlineData("GH_REPO", "contoso/widgets; echo")]
    [InlineData("POWEROPS_SWEEP_PR", "7x")]
    public void The_sweep_script_refuses_a_bad_input_and_calls_pac_never(string variable, string value)
    {
        var run = RunSweep([List()], extra: new() { [variable] = value });

        Assert.NotEqual(0, run.Code);
        Assert.Empty(run.PacCalls);
    }

    // ---- ephemeral-env.sh delete ----

    [Fact]
    public void Delete_with_the_id_from_find_reads_the_list_once_and_deletes_that_id()
    {
        var run = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgpr7x")), Others())], extra: new() { ["ENV_ID"] = Id('a') });

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json", $"admin delete --environment {Id('a')} --async"], run.PacCalls);
        AssertNoLeak(run);
    }

    [Fact]
    public void Delete_without_an_id_looks_the_name_up_then_checks_it_again_before_deleting()
    {
        var run = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgpr7x")), Others())]);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json", "admin list --json", $"admin delete --environment {Id('a')} --async"], run.PacCalls);
        AssertNoLeak(run);
    }

    [Fact]
    public void Delete_with_no_environment_by_that_name_does_nothing_and_succeeds()
    {
        var run = RunEnv("delete", [List(Item("someone-else", Id('c'), Url("orgother")), Others())]);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json"], run.PacCalls);
        AssertNoLeak(run, "someone-else", "orgother", Id('c'));
    }

    [Fact]
    public void Delete_whose_lookup_fails_or_finds_two_deletes_nothing_and_fails()
    {
        var failing = RunEnv("delete", [List()], extra: new() { ["PAC_LIST_EXIT"] = "1" });
        var two = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgone")), Item(Name(), Id('b'), Url("orgtwo")))]);

        Assert.All(new[] { failing, two }, r =>
        {
            Assert.Equal(1, r.Code);
            Assert.Contains("::error::", r.Out);
            Assert.DoesNotContain(r.PacCalls, c => c.StartsWith("admin delete", StringComparison.Ordinal));
        });
        AssertNoLeak(two, "orgone", "orgtwo", Id('a'), Id('b'));
    }

    [Fact]
    public void Delete_deletes_nothing_when_the_second_read_disagrees()
    {
        var given = new Dictionary<string, string> { ["ENV_ID"] = Id('a') };
        var renamed = RunEnv("delete", [List(Item("renamed-by-an-admin", Id('a'), Url("orgpr7x")))], extra: given);
        var otherId = RunEnv("delete", [List(Item(Name(), Id('b'), Url("orgpr7x")))], extra: given);
        var gone = RunEnv("delete", [List(Item("someone-else", Id('c'), Url("orgother")))], extra: given);
        var changed = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgpr7x"))), List(Item(Name(), Id('b'), Url("orgpr7x")))]);

        Assert.All(new[] { renamed, otherId, gone, changed }, r =>
        {
            Assert.Equal(1, r.Code);
            Assert.Contains("::error::", r.Out);
            Assert.DoesNotContain(r.PacCalls, c => c.StartsWith("admin delete", StringComparison.Ordinal));
        });
        AssertNoLeak(renamed, "renamed-by-an-admin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://not-dataverse.example.com")]
    public void Delete_by_name_needs_no_valid_url_because_a_provisioning_environment_has_none(string? url)
    {
        var run = RunEnv("delete", [List(Item(Name(), Id('a'), url), Others())]);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json", "admin list --json", $"admin delete --environment {Id('a')} --async"], run.PacCalls);
        AssertNoLeak(run);
    }

    [Fact]
    public void Delete_with_an_id_deletes_nothing_when_two_environments_have_the_name()
    {
        var run = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgone")), Item(Name(), Id('b'), Url("orgtwo")))], extra: new() { ["ENV_ID"] = Id('a') });

        Assert.Equal(1, run.Code);
        Assert.Contains("::error::", run.Out);
        Assert.DoesNotContain(run.PacCalls, c => c.StartsWith("admin delete", StringComparison.Ordinal));
        AssertNoLeak(run, "orgone", "orgtwo", Id('b'));
    }

    [Fact]
    public void Delete_with_an_id_whose_environment_has_another_name_deletes_nothing_even_if_the_exact_name_exists_with_another_id()
    {
        var run = RunEnv("delete", [List(Item("renamed-by-an-admin", Id('a'), Url("orgone")), Item(Name(), Id('b'), Url("orgtwo")))], extra: new() { ["ENV_ID"] = Id('a') });

        Assert.Equal(1, run.Code);
        Assert.Contains("::error::", run.Out);
        Assert.DoesNotContain(run.PacCalls, c => c.StartsWith("admin delete", StringComparison.Ordinal));
        AssertNoLeak(run, "renamed-by-an-admin", "orgone", "orgtwo");
    }

    [Theory]
    [InlineData("Production", false)]
    [InlineData("Developer", false)]
    [InlineData("Default", false)]
    [InlineData("sandbox", false)]
    [InlineData("Sandbox", true)]
    [InlineData("Trial", true)]
    public void Delete_takes_only_a_sandbox_or_a_trial_even_when_the_name_matches(string type, bool deleted)
    {
        var list = List(Item(Name(), Id('a'), Url("orgpr7x"), type));
        var byId = RunEnv("delete", [list], extra: new() { ["ENV_ID"] = Id('a') });
        var byName = RunEnv("delete", [list]);

        Assert.All(new[] { byId, byName }, r =>
        {
            Assert.Equal(deleted ? 0 : 1, r.Code);
            Assert.Equal(deleted, r.PacCalls.Any(c => c.StartsWith("admin delete", StringComparison.Ordinal)));
            if (!deleted)
            {
                Assert.Contains("::error::", r.Out);
                Assert.DoesNotContain(Name(), r.Out + r.Err);
            }
        });
    }

    [Fact]
    public void Delete_fails_when_pac_delete_fails_and_prints_none_of_its_text()
    {
        var run = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgpr7x")))], extra: new() { ["ENV_ID"] = Id('a'), ["PAC_DELETE_EXIT"] = "1" });

        Assert.Equal(1, run.Code);
        Assert.Contains("::error::", run.Out);
        AssertNoLeak(run);
    }

    [Fact]
    public void Delete_refuses_an_id_that_is_not_a_guid_and_calls_pac_never()
    {
        var run = RunEnv("delete", [List(Item(Name(), Id('a'), Url("orgpr7x")))], extra: new() { ["ENV_ID"] = "--all" });

        Assert.Equal(1, run.Code);
        Assert.Empty(run.PacCalls);
    }

    [Fact]
    public void An_unknown_mode_is_a_usage_error()
    {
        var run = RunEnv("create", [List()]);

        Assert.Equal(2, run.Code);
        Assert.Empty(run.PacCalls);
    }

    // ---- ephemeral-sweep.sh ----

    // Runs 1000 and 1001 are completed, 2000 is in progress, 2001 is queued, 2002 is waiting, 3000 cannot be read (gh fails), 4000 prints
    // nothing and 4001 prints text that is not JSON.
    private static readonly Dictionary<long, string> Runs = new()
    {
        [1000] = """{"status":"completed","conclusion":"cancelled"}""",
        [1001] = """{"status":"completed","conclusion":"success"}""",
        [2000] = """{"status":"in_progress","conclusion":""}""",
        [2001] = """{"status":"queued","conclusion":""}""",
        [2002] = """{"status":"waiting","conclusion":""}""",
        [4000] = "",
        [4001] = "not json {",
    };

    [Fact]
    public void Sweep_deletes_the_leftover_of_a_completed_run_and_keeps_a_run_in_progress()
    {
        var run = RunSweep([List(Item(Name(run: 1000), Id('a'), Url("org1")), Item(Name(pr: 8, run: 2000), Id('b'), Url("org2")), Others())], delete: true);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json", $"admin delete --environment {Id('a')} --async"], run.PacCalls);
        Assert.Equal(["run view 1000 --repo contoso/widgets --json status,conclusion", "run view 2000 --repo contoso/widgets --json status,conclusion"], run.GhCalls);
        Assert.Contains($"deleted {Name(run: 1000)}\n", run.Out);
        Assert.Contains($"kept {Name(pr: 8, run: 2000)}", run.Out);
        Assert.Contains("matched 2, leftovers 1, kept 1, failed 0", run.Out);
        AssertNoLeak(run);
    }

    [Fact]
    public void Sweep_leaves_another_repositorys_environment_alone_and_does_not_print_it()
    {
        var other = Name(repo: "9999");
        var run = RunSweep([List(Item(other, Id('c'), Url("orgelse")), Item(Name(), Id('a'), Url("org1")))], delete: true);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json", $"admin delete --environment {Id('a')} --async"], run.PacCalls);
        Assert.Single(run.GhCalls);
        AssertNoLeak(run, other, "orgelse", Id('c'));
    }

    [Fact]
    public void Sweep_tells_attempt_1_from_attempt_10_and_deletes_both_leftovers()
    {
        var run = RunSweep([List(Item(Name(attempt: 1), Id('a'), Url("org1")), Item(Name(attempt: 10), Id('b'), Url("org10")))], delete: true);

        Assert.Equal(0, run.Code);
        Assert.Contains($"admin delete --environment {Id('a')} --async", run.PacCalls);
        Assert.Contains($"admin delete --environment {Id('b')} --async", run.PacCalls);
        Assert.Contains($"deleted {Name(attempt: 1)}\n", run.Out);
        Assert.Contains($"deleted {Name(attempt: 10)}\n", run.Out);
    }

    [Fact]
    public void Sweep_keeps_a_name_that_does_not_parse_and_never_prints_it()
    {
        string[] odd = [$"{Prefix}-{RepoId}-prx-1-1", $"{Name()}-extra", $"x{Name()}", $"{Prefix}-{RepoId}-pr7-1000-", $"{Name()}\n"];
        var items = odd.Select((n, i) => Item(n.Replace("\n", "\\n", StringComparison.Ordinal), Id((char)('1' + i)), Url("orgodd" + i))).ToArray();

        var run = RunSweep([List(items)], delete: true);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json"], run.PacCalls);
        Assert.Empty(run.GhCalls);
        Assert.Contains("matched 0", run.Out);
        AssertNoLeak(run, [.. odd.Select(n => n.TrimEnd('\n')), "orgodd0", "orgodd1", Id('1'), Id('2')]);
    }

    [Fact]
    public void Sweep_keeps_the_environment_when_gh_fails_and_turns_red()
    {
        var run = RunSweep([List(Item(Name(run: 3000), Id('a'), Url("org1")), Item(Name(run: 1000), Id('b'), Url("org2")))], delete: true);

        Assert.Equal(1, run.Code);
        Assert.DoesNotContain($"admin delete --environment {Id('a')} --async", run.PacCalls);
        Assert.Contains($"admin delete --environment {Id('b')} --async", run.PacCalls);
        Assert.Contains($"kept {Name(run: 3000)}", run.Out);
        Assert.Contains("failed 1", run.Out);
        Assert.Contains("::error::", run.Out);
        Assert.DoesNotContain("secret-repo", run.Out + run.Err);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2001)]
    [InlineData(2002)]
    public void Sweep_keeps_the_environment_of_a_run_that_has_not_finished(long runId)
    {
        var run = RunSweep([List(Item(Name(run: runId), Id('a'), Url("org1")))], delete: true);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json"], run.PacCalls);
        Assert.Contains($"kept {Name(run: runId)}", run.Out);
        Assert.Contains("matched 1, leftovers 0, kept 1, failed 0", run.Out);
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(4001)]
    public void Sweep_keeps_the_environment_and_turns_red_when_gh_succeeds_but_prints_no_usable_json(long runId)
    {
        var run = RunSweep([List(Item(Name(run: runId), Id('a'), Url("org1")))], delete: true);

        Assert.Equal(1, run.Code);
        Assert.Equal(["admin list --json"], run.PacCalls);
        Assert.Contains($"kept {Name(run: runId)}", run.Out);
        Assert.Contains("failed 1", run.Out);
    }

    [Fact]
    public void Sweep_matches_the_whole_repository_id_so_repository_42_leaves_repository_4242_alone()
    {
        var run = RunSweep([List(Item(Name(), Id('a'), Url("org1")))], delete: true, extra: new() { ["REPO_ID"] = "42" });

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json"], run.PacCalls);
        Assert.Empty(run.GhCalls);
        Assert.Contains("matched 0", run.Out);
        AssertNoLeak(run, Name(), Id('a'), "org1");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Developer")]
    [InlineData("Default")]
    public void Sweep_keeps_an_environment_that_is_not_a_sandbox_or_a_trial_and_turns_red_without_naming_it(string type)
    {
        var list = List(Item(Name(run: 1000), Id('a'), Url("org1"), type));
        var deleting = RunSweep([list], delete: true);
        var dry = RunSweep([list]);

        Assert.All(new[] { deleting, dry }, r =>
        {
            Assert.Equal(1, r.Code);
            Assert.Equal(["admin list --json"], r.PacCalls);
            Assert.DoesNotContain("would delete", r.Out);
            Assert.DoesNotContain("deleted ", r.Out);
            Assert.Contains("failed 1", r.Out);
            AssertNoLeak(r, Name(run: 1000), Id('a'), "org1");
        });
    }

    [Fact]
    public void Sweep_deletes_a_trial()
    {
        var run = RunSweep([List(Item(Name(run: 1000), Id('a'), Url("org1"), "Trial"))], delete: true);

        Assert.Equal(0, run.Code);
        Assert.Contains($"admin delete --environment {Id('a')} --async", run.PacCalls);
    }

    [Fact]
    public void Sweep_turns_red_when_a_delete_fails_or_a_matching_environment_has_no_usable_id()
    {
        var deleteFails = RunSweep([List(Item(Name(), Id('a'), Url("org1")))], delete: true, extra: new() { ["PAC_DELETE_EXIT"] = "1" });
        var badId = RunSweep([List(Item(Name(), "not-a-guid", Url("org1")))], delete: true);

        Assert.Equal(1, deleteFails.Code);
        Assert.Contains($"failed {Name()}", deleteFails.Out);
        Assert.Equal(1, badId.Code);
        Assert.DoesNotContain(badId.PacCalls, c => c.StartsWith("admin delete", StringComparison.Ordinal));
        AssertNoLeak(deleteFails);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("TRUE")]
    [InlineData("1")]
    public void Sweep_is_a_dry_run_unless_delete_is_exactly_true(string? value)
    {
        var run = RunSweep([List(Item(Name(run: 1000), Id('a'), Url("org1")), Item(Name(pr: 8, run: 2000), Id('b'), Url("org2")))], deleteValue: value);

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json"], run.PacCalls);
        Assert.Contains($"would delete {Name(run: 1000)}", run.Out);
        Assert.Contains($"kept {Name(pr: 8, run: 2000)}", run.Out);
        Assert.DoesNotContain("deleted ", run.Out);
        Assert.Contains("dry run", run.Out);
    }

    [Fact]
    public void Sweep_with_a_pr_number_handles_only_that_prs_environments()
    {
        var run = RunSweep([List(Item(Name(pr: 7, run: 1000), Id('a'), Url("org1")), Item(Name(pr: 70, run: 1001), Id('b'), Url("org2")), Item(Name(pr: 8, run: 1000), Id('c'), Url("org3")))], delete: true, extra: new() { ["POWEROPS_SWEEP_PR"] = "7" });

        Assert.Equal(0, run.Code);
        Assert.Equal(["admin list --json", $"admin delete --environment {Id('a')} --async"], run.PacCalls);
        Assert.Contains("matched 1, leftovers 1, kept 0, failed 0", run.Out);
        Assert.DoesNotContain(Name(pr: 8, run: 1000), run.Out);
        Assert.DoesNotContain(Name(pr: 70, run: 1001), run.Out);
    }

    [Fact]
    public void Sweep_fails_with_an_error_line_when_the_list_cannot_be_read()
    {
        var failing = RunSweep([List()], delete: true, extra: new() { ["PAC_LIST_EXIT"] = "1" });
        var banner = RunSweep(["Welcome to pac\n[]"], delete: true);

        Assert.All(new[] { failing, banner }, r =>
        {
            Assert.Equal(1, r.Code);
            Assert.Contains("::error::", r.Out);
            Assert.DoesNotContain(r.PacCalls, c => c.StartsWith("admin delete", StringComparison.Ordinal));
        });
        Assert.DoesNotContain("secret", failing.Out + failing.Err);
    }

    // ---- both scripts ----

    [Theory]
    [InlineData("ephemeral-env.sh")]
    [InlineData("ephemeral-sweep.sh")]
    public void The_scripts_parse_with_bash_and_follow_the_rules_for_scripts(string script)
    {
        Tools.Require("bash");
        var path = Path.Combine(Scripts, script);
        var start = new ProcessStartInfo(Tools.Bash) { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-n");
        start.ArgumentList.Add(path);
        var syntax = Execute(start);
        var text = File.ReadAllText(path);

        Assert.Equal(0, syntax.Code);
        Assert.Contains("set -euo pipefail", text);
        Assert.DoesNotContain("set -x", text);
        Assert.DoesNotContain("bash -x", text);
        Assert.DoesNotContain("${{", text);
        Assert.DoesNotContain("eval ", text);
    }

    // ---- helpers ----

    private sealed record Result(int Code, string Out, string Err, string Temp)
    {
        public string[] PacCalls => ReadLines(Path.Combine(Temp, "pac.log"));

        public string[] GhCalls => ReadLines(Path.Combine(Temp, "gh.log"));

        public string Output => File.Exists(Path.Combine(Temp, "output.txt")) ? File.ReadAllText(Path.Combine(Temp, "output.txt")) : "";
    }

    private static string[] ReadLines(string path) => File.Exists(path) ? File.ReadAllLines(path) : [];

    // The stub pac logs its arguments. `admin list --json` prints list.<n>.json for the n-th call (the last one again after that) and
    // PAC_LIST_EXIT makes it fail, PAC_LIST_FAILS makes the first n calls fail; `admin delete` prints text that names other environments, and PAC_DELETE_EXIT makes it fail.
    private const string PacStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/pac.log"
        case "$1 $2" in
          "admin list")
            n=0
            if [ -f "$STUB_DIR/list.count" ]; then n="$(cat "$STUB_DIR/list.count")"; fi
            n=$((n + 1))
            echo "$n" > "$STUB_DIR/list.count"
            code="${PAC_LIST_EXIT:-0}"
            if [ "$n" -le "${PAC_LIST_FAILS:-0}" ]; then code=1; fi
            if [ "$code" != "0" ]; then echo "pac: contoso-secret-env could not be listed (noise)" >&2; exit "$code"; fi
            for ((i = n; i >= 1; i--)); do
              if [ -f "$STUB_DIR/list.$i.json" ]; then cat "$STUB_DIR/list.$i.json"; break; fi
            done
            ;;
          "admin delete")
            echo "Deleting leak-delete at https://leak-delete.crm.dynamics.com"
            echo "pac: delete noise" >&2
            exit "${PAC_DELETE_EXIT:-0}"
            ;;
          *) echo "pac: unexpected call" >&2; exit 64 ;;
        esac
        """;

    // The stub gh logs its arguments and answers `run view <id>` from run-<id>.json; any other call, or an id without a file, fails.
    private const string GhStub = """
        #!/usr/bin/env bash
        echo "$*" >> "$STUB_DIR/gh.log"
        if [ "$1 $2" = "run view" ] && [ -f "$STUB_DIR/run-$3.json" ]; then cat "$STUB_DIR/run-$3.json"; exit 0; fi
        echo "gh: HTTP 404 in secret-repo" >&2
        exit 1
        """;

    private Result RunEnv(string mode, string[] lists, Dictionary<string, string>? extra = null, bool outputToStdout = false)
    {
        var env = new Dictionary<string, string>
        {
            ["EPHEMERAL_FIND_TRIES"] = "5",
            ["EPHEMERAL_FIND_WAIT"] = "0",
        };
        foreach (var (key, value) in extra ?? [])
        {
            env[key] = value;
        }

        return Run("ephemeral-env.sh", [mode], lists, env, outputToStdout);
    }

    private Result RunSweep(string[] lists, bool delete = false, string? deleteValue = null, Dictionary<string, string>? extra = null)
    {
        var env = new Dictionary<string, string> { ["GH_REPO"] = "contoso/widgets", ["GH_TOKEN"] = "fake" };
        if (delete || deleteValue is not null)
        {
            env["POWEROPS_SWEEP_DELETE"] = deleteValue ?? "true";
        }

        foreach (var (key, value) in extra ?? [])
        {
            env[key] = value;
        }

        return Run("ephemeral-sweep.sh", [], lists, env, outputToStdout: false);
    }

    private Result Run(string script, string[] args, string[] lists, Dictionary<string, string> extra, bool outputToStdout)
    {
        Tools.Require("bash", "jq");
        var temp = Directory.CreateDirectory(Path.Combine(_root, Guid.NewGuid().ToString("N")[..8])).FullName;
        WriteStub(temp, "pac", PacStub);
        WriteStub(temp, "gh", GhStub);
        for (var i = 0; i < lists.Length; i++)
        {
            File.WriteAllText(Path.Combine(temp, $"list.{i + 1}.json"), lists[i]);
        }

        foreach (var (id, json) in Runs)
        {
            File.WriteAllText(Path.Combine(temp, $"run-{id}.json"), json);
        }

        var runnerTemp = Directory.CreateDirectory(Path.Combine(temp, "runner")).FullName;
        var start = new ProcessStartInfo(Tools.Bash) { WorkingDirectory = temp, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(Scripts, script));
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment["PATH"] = Path.Combine(temp, "bin") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
        start.Environment["STUB_DIR"] = temp;
        var env = new Dictionary<string, string>
        {
            ["EPHEMERAL_PREFIX"] = Prefix,
            ["REPO_ID"] = RepoId,
            ["PR_NUMBER"] = "7",
            ["RUN_ID"] = "1000",
            ["RUN_ATTEMPT"] = "1",
            ["RUNNER_TEMP"] = runnerTemp,
            ["RUNNER_DEBUG"] = "",
            ["GITHUB_OUTPUT"] = outputToStdout ? "/dev/stdout" : Path.Combine(temp, "output.txt"),
        };
        foreach (var (key, value) in extra)
        {
            env[key] = value;
        }

        foreach (var (key, value) in env)
        {
            start.Environment[key] = value;
        }

        var (code, stdout, stderr) = Execute(start);
        return new Result(code, stdout, stderr, temp);
    }

    // No line of the output names another environment (its name, id or URL), whatever the script did.
    private static void AssertNoLeak(Result run, params string[] more)
    {
        var all = run.Out + run.Err + run.Output;
        Assert.All(OtherMarks.Concat(more), mark => Assert.DoesNotContain(mark, all, StringComparison.OrdinalIgnoreCase));
    }

    private static void WriteStub(string temp, string name, string body)
    {
        var bin = Directory.CreateDirectory(Path.Combine(temp, "bin")).FullName;
        var path = Path.Combine(bin, name);
        File.WriteAllText(path, body.Replace("\r\n", "\n") + "\n");
        var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
        if (posix)
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static (int Code, string Out, string Err) Execute(ProcessStartInfo start)
    {
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(start.FileName + " did not finish.");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }
}