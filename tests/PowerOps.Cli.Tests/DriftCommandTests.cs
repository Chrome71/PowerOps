using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class DriftCommandTests : IDisposable
{
    private const string Url = "https://contoso-test.crm.dynamics.com/";

    private static readonly string DevFile = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-dev.json");
    private static readonly string TestFile = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-test.json");

    private readonly string _dir = Directory.CreateTempSubdirectory("powerops-drift-").FullName;

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static Snapshot Dev => SnapshotFile.Load(DevFile);

    private static Snapshot Test => SnapshotFile.Load(TestFile);

    // Dev's content under the name "Contoso Test": equal to Dev as far as drift goes.
    private static Snapshot Twin => Dev with { Environment = "Contoso Test" };

    private sealed record Outcome(int Code, string Out, string Err, List<string> Calls);

    private static Outcome Run(IDictionary<string, Snapshot>? live, params string[] args)
    {
        var (o, e, calls) = (new StringWriter(), new StringWriter(), new List<string>());
        var code = Program.Run(
            args,
            o,
            e,
            (_, _) => throw new Xunit.Sdk.XunitException("pac must not be called"),
            live is null
                ? null
                : (solution, value) =>
                {
                    calls.Add($"{solution}|{value}");
                    return live.TryGetValue(value, out var s) ? s : throw new DriftReadException("the read failed");
                });
        return new Outcome(code, o.ToString(), e.ToString(), calls);
    }

    private static string[] Args(params string[] envs) =>
        ["drift", "--solution", "PowerOpsSeed", .. envs.SelectMany(e => new[] { "--env", e })];

    private string Write(string name, string text)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, text);
        return path;
    }

    private static Dictionary<string, Snapshot> Pair(Snapshot a, Snapshot b) => new() { ["a"] = a, ["b"] = b };

    private static IEnumerable<(string Name, Func<Snapshot, Snapshot> Change, string Expected)> Changes()
    {
        yield return ("version", s => s with { Version = "2.0.0.0" }, "version PowerOpsSeed: 1.0.0.0 -> 2.0.0.0");
        yield return ("extra component", s => s with { Components = [.. s.Components, new SnapshotComponent(1, new Guid("00000000-0000-0000-0000-0000000000e1"), null)] }, "component-extra 1 00000000-0000-0000-0000-0000000000e1");
        yield return ("unmanaged layer", s => s with { Managed = true, Components = [.. s.Components.Select(c => c.Type == 29 ? c with { UnmanagedLayer = true } : c with { UnmanagedLayer = false })] }, "unmanaged-layer Notify on new work item");
        yield return ("variable value", s => s with { Variables = [new SnapshotVariable("pops_DefaultStatus", Snapshot.Hash("Other"))] }, "variable-value pops_DefaultStatus");
        yield return ("unbound reference", s => s with { References = [.. s.References.Select(r => r with { Bound = false })] }, "reference-unbound pops_sharedoffice365_6a2f1");
        yield return ("flow off", s => s with { Flows = [.. s.Flows.Select(f => f with { State = "off" })] }, "flow-state Notify on new work item: on -> off");
    }

    public static IEnumerable<object[]> ChangeNames() => Changes().Select(c => new object[] { c.Name });

    [Fact]
    public void Equal_environments_exit_0_and_match_the_none_golden()
    {
        var r = Run(Pair(Dev, Twin), [.. Args("a", "b"), "--format", "json"]);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Golden.Verify(r.Out, "drift-none.json");
        Assert.Equal(["PowerOpsSeed|a", "PowerOpsSeed|b"], r.Calls);
        Assert.Equal(0, Run(Pair(Dev, Twin), Args("a", "b")).Code);
        Assert.EndsWith("no drift\n", Run(Pair(Dev, Twin), Args("a", "b")).Out);
    }

    [Theory]
    [MemberData(nameof(ChangeNames))]
    public void A_difference_exits_1_and_is_named(string name)
    {
        var (_, change, expected) = Changes().Single(c => c.Name == name);

        var r = Run(Pair(Dev, change(Twin)), Args("a", "b"));

        Assert.Equal(1, r.Code);
        Assert.Contains(expected, r.Out);
    }

    [Fact]
    public void Seed_files_give_the_text_json_and_markdown_goldens()
    {
        foreach (var (format, golden) in new[] { ("text", "drift-seed.txt"), ("json", "drift-seed.json"), ("markdown", "drift-seed.md") })
        {
            var r = Run(null, "drift", "--solution", "PowerOpsSeed", "--env", DevFile, "--env", TestFile, "--format", format);
            Assert.Equal(1, r.Code);
            Assert.Equal("", r.Err);
            Golden.Verify(r.Out, golden);
        }
    }

    public static IEnumerable<object[]> UsageErrors()
    {
        yield return new object[] { new[] { "drift", "--solution", "PowerOpsSeed", "--env", "a" } };
        yield return new object[] { new[] { "drift", "--env", "a", "--env", "b" } };
        yield return new object[] { new[] { "drift", "--solution", "bad name", "--env", "a", "--env", "b" } };
        yield return new object[] { new[] { "drift", "--solution", "Sol\n", "--env", "a", "--env", "b" } };
        yield return new object[] { new[] { "drift", "--solution", "PowerOpsSeed", "--env", "a", "--env", "missing/Nope.JSON" } };
    }

    [Theory]
    [MemberData(nameof(UsageErrors))]
    public void Usage_errors_exit_2_before_any_live_call(string[] args)
    {
        var r = Run(Pair(Dev, Twin), args);

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
        Assert.StartsWith("error: ", r.Err);
        Assert.DoesNotContain("Nope", r.Err);
        Assert.DoesNotContain("Sol", r.Err);
    }

    [Fact]
    public void A_missing_snapshot_file_is_named_by_position()
    {
        var r = Run(Pair(Dev, Twin), Args("a", Path.Combine(_dir, "nope.json")));

        Assert.Equal(2, r.Code);
        Assert.Equal("error: --env 2: no snapshot file at that path\n", r.Err);
    }

    [Fact]
    public void Save_on_a_file_or_with_only_snapshot_inputs_exits_2()
    {
        var file = Write("file.txt", "x");

        var onFile = Run(Pair(Dev, Twin), [.. Args("a", "b"), "--save", file]);
        var onlySnapshots = Run(Pair(Dev, Twin), [.. Args(DevFile, TestFile), "--save", Path.Combine(_dir, "out")]);

        Assert.Equal(2, onFile.Code);
        Assert.Equal(2, onlySnapshots.Code);
        Assert.Empty(onFile.Calls);
        Assert.False(Directory.Exists(Path.Combine(_dir, "out")));
    }

    [Fact]
    public void A_snapshot_for_another_solution_exits_2_without_a_live_call()
    {
        var other = Write("other.json", SnapshotFile.Write(Dev with { Solution = "Other" }));

        var r = Run(Pair(Dev, Twin), Args(other, "b"));

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
        Assert.Equal("error: --env 1: the snapshot is for another solution\n", r.Err);
    }

    [Fact]
    public void A_solution_name_in_another_case_is_the_same_solution()
    {
        var lower = Write("lower.json", SnapshotFile.Write(Dev with { Solution = "POWEROPSSEED" }));

        Assert.Equal(0, Run(null, Args(lower, DevFile)).Code);
    }

    [Fact]
    public void A_snapshot_with_a_wrong_schemaVersion_exits_3_without_a_live_call()
    {
        var bad = Write("bad.json", SnapshotFile.Write(Dev).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));

        var r = Run(Pair(Dev, Twin), Args("a", bad));

        Assert.Equal(3, r.Code);
        Assert.Empty(r.Calls);
        Assert.StartsWith("error: --env 2: ", r.Err);
        Assert.DoesNotContain(bad, r.Err);
        Assert.Equal("", r.Out);
    }

    [Fact]
    public void A_usage_error_and_an_unreadable_snapshot_in_one_run_exit_2()
    {
        var bad = Write("bad.json", "{}");

        var r = Run(Pair(Dev, Twin), "drift", "--solution", "bad name", "--env", bad, "--env", "b");

        Assert.Equal(2, r.Code);
        Assert.Empty(r.Calls);
    }

    [Fact]
    public void A_failed_live_read_exits_3_with_no_report_and_no_files()
    {
        var save = Path.Combine(_dir, "saved");

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev }, [.. Args("a", Url), "--save", save]);

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Out);
        Assert.Equal("error: --env 2: the read failed\n", r.Err);
        Assert.False(Directory.Exists(save));
        Assert.DoesNotContain(Url, r.Err);
    }

    [Fact]
    public void Without_a_live_reader_a_live_env_exits_3_with_the_not_built_message()
    {
        var r = Run(null, Args(DevFile, "contoso-test"));

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Out);
        Assert.Equal("error: --env 2: reading a live environment is not built yet; pass snapshot files (.json) as --env.\n", r.Err);
    }

    [Fact]
    public void A_solution_installed_nowhere_exits_2()
    {
        var none = new Snapshot("Contoso Dev", "PowerOpsSeed", false, null, null, true, [], [], [], []);

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = none, ["b"] = none with { Environment = "Contoso Test" } }, Args("a", "b"));

        Assert.Equal(2, r.Code);
        Assert.Equal("error: solution `PowerOpsSeed` is not installed in any environment\n", r.Err);
        Assert.Equal("", r.Out);
    }

    [Fact]
    public void A_url_env_reaches_the_live_reader_unchanged_and_is_printed_nowhere()
    {
        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev, [Url] = Twin }, Args("a", Url));

        Assert.Equal(0, r.Code);
        Assert.Equal(["PowerOpsSeed|a", $"PowerOpsSeed|{Url}"], r.Calls);
        Assert.DoesNotContain("crm.dynamics.com", r.Out + r.Err);
    }

    [Fact]
    public void Save_writes_one_file_per_live_env_and_none_for_snapshot_inputs()
    {
        var save = Path.Combine(_dir, "saved");

        var r = Run(new Dictionary<string, Snapshot> { ["b"] = Test }, [.. Args(DevFile, "b"), "--save", save]);

        Assert.Equal(1, r.Code);
        Assert.Equal([Path.Combine(save, "PowerOpsSeed.Contoso Test.json")], Directory.GetFiles(save));
        Assert.Equal(SnapshotFile.Write(Test), File.ReadAllText(Path.Combine(save, "PowerOpsSeed.Contoso Test.json")));
    }

    [Fact]
    public void Save_overwrites_and_replaces_characters_outside_the_safe_set()
    {
        var save = Path.Combine(_dir, "saved");
        Directory.CreateDirectory(save);
        File.WriteAllText(Path.Combine(save, "PowerOpsSeed.Contoso-Dev- 1_x.json"), "old");

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev with { Environment = "Contoso/Dev: 1_x" }, ["b"] = Twin }, [.. Args("a", "b"), "--save", save]);

        Assert.Equal(0, r.Code);
        Assert.Equal(SnapshotFile.Write(Dev with { Environment = "Contoso/Dev: 1_x" }), File.ReadAllText(Path.Combine(save, "PowerOpsSeed.Contoso-Dev- 1_x.json")));
        Assert.Equal(2, Directory.GetFiles(save).Length);
    }

    [Fact]
    public void Save_of_a_seed_snapshot_matches_the_snapshot_golden()
    {
        var save = Path.Combine(_dir, "saved");

        Run(new Dictionary<string, Snapshot> { ["a"] = Dev, ["b"] = Test }, [.. Args("a", "b"), "--save", save]);

        Golden.Verify(File.ReadAllText(Path.Combine(save, "PowerOpsSeed.Contoso Dev.json")), "snapshot-seed.json");
    }

    [Fact]
    public void Two_live_envs_with_the_same_file_name_exit_2_and_write_nothing()
    {
        var save = Path.Combine(_dir, "saved");

        var r = Run(new Dictionary<string, Snapshot> { ["a"] = Dev, ["b"] = Dev with { Environment = "contoso dev" } }, [.. Args("a", "b"), "--save", save]);

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.False(Directory.Exists(save));
    }

    [Fact]
    public void A_saved_pair_compared_offline_gives_the_same_text_and_exit_code_as_the_live_run()
    {
        var save = Path.Combine(_dir, "saved");
        var liveRun = Run(Pair(Dev, Test), [.. Args("a", "b"), "--save", save]);

        var offline = Run(null, Args(Path.Combine(save, "PowerOpsSeed.Contoso Dev.json"), Path.Combine(save, "PowerOpsSeed.Contoso Test.json")));

        Assert.Equal(1, liveRun.Code);
        Assert.Equal(liveRun.Code, offline.Code);
        Assert.Equal(liveRun.Out, offline.Out);
    }

    [Fact]
    public void Out_writes_the_report_to_the_file_and_nothing_to_stdout()
    {
        var file = Path.Combine(_dir, "report.md");

        var r = Run(null, [.. Args(DevFile, TestFile), "--format", "markdown", "--out", file]);

        Assert.Equal(1, r.Code);
        Assert.Equal("", r.Out);
        Assert.StartsWith("## PowerOps drift", File.ReadAllText(file));
        Assert.False(File.ReadAllBytes(file).AsSpan().StartsWith("﻿"u8.ToArray()));
    }
}