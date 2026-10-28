using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public sealed class McpCommandTests : IAsyncLifetime
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private static readonly string V1 = Path.Combine(Fixtures, "powerops-seed", "v1");
    private static readonly string V2 = Path.Combine(Fixtures, "powerops-seed", "v2");
    private static readonly string Alm = Path.Combine(Fixtures, "alm-makers");
    private static readonly string AlmTests = Path.Combine(Fixtures, "flowtests", "alm-makers");
    private static readonly string Valid = Path.Combine(Fixtures, "settings", "powerops-seed-v1.valid.json");
    private static readonly string DevFile = Path.Combine(Fixtures, "drift", "seed-dev.json");
    private static readonly string TestFile = Path.Combine(Fixtures, "drift", "seed-test.json");
    private static readonly string OrgWho = Path.Combine(Fixtures, "pac", "org-who.txt");
    private static readonly string[] ToolNames = ["analyze", "rules", "baseline", "diff", "test", "settings_generate", "settings_validate", "plan_status", "plan_facts", "drift", "deploy_plan", "deploy_apply"];

    private const string SolutionTable = """
        Unique Name           Friendly Name                         Version Managed
        ContosoCore           Contoso Core                          3.0.1.0 True
        """;

    private const string Secret = "https://secret-host-7f3a.crm.dynamics.com/";

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-mcp-").FullName;
    private readonly Fakes _fakes = new();
    private readonly ConcurrentQueue<string[]> _ran = [];
    private McpServer _server = null!;
    private Task _run = null!;
    private McpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var options = McpCommand.Options(_fakes.Pac, _fakes.Live, _fakes.Target, _ran.Enqueue);
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();
        _server = McpServer.Create(new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()), options);
        _run = _server.RunAsync();
        _client = await McpClient.CreateAsync(new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()));
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _server.DisposeAsync();
        try
        {
            await _run;
        }
        catch (OperationCanceledException)
        {
        }

        Directory.Delete(_temp, recursive: true);
        Assert.Equal(0, _fakes.Unexpected);
    }

    // The three functions a tool reaches a tenant through. A function with no behavior set is a call the test did not expect: it fails the test.
    private sealed class Fakes
    {
        private int _pac;
        private int _live;
        private int _target;
        private int _unexpected;

        public Func<IReadOnlyList<string>, PacResult>? PacBehavior { get; set; }

        public Func<string, string, Snapshot>? LiveBehavior { get; set; }

        public Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget>? TargetBehavior { get; set; }

        public int Unexpected => _unexpected;

        public int Pacs => _pac;

        public int Lives => _live;

        public int Targets => _target;

        public int Total => _pac + _live + _target;

        public PacResult Pac(IReadOnlyList<string> args, TimeSpan timeout)
        {
            Interlocked.Increment(ref _pac);
            return PacBehavior?.Invoke(args) ?? Unexpect<PacResult>("pac");
        }

        public Snapshot Live(string solution, string env)
        {
            Interlocked.Increment(ref _live);
            return LiveBehavior?.Invoke(solution, env) ?? Unexpect<Snapshot>("the live read");
        }

        public DeployTarget Target(string target, IReadOnlyList<string> solutions, IReadOnlyList<string> dependencies)
        {
            Interlocked.Increment(ref _target);
            return TargetBehavior?.Invoke(target, solutions, dependencies) ?? Unexpect<DeployTarget>("the target read");
        }

        private T Unexpect<T>(string what)
        {
            Interlocked.Increment(ref _unexpected);
            throw new InvalidOperationException($"{what} was called");
        }
    }

    private static PacResult Ok(string stdout) => new(true, false, 0, stdout);

    private static Fakes PlanFactsFakes() => new() { PacBehavior = a => Ok(a[0] == "org" ? File.ReadAllText(OrgWho) : SolutionTable) };

    private static Fakes DriftFakes() => new() { LiveBehavior = (_, _) => SnapshotFile.Load(TestFile) };

    private static DeployTarget ReadTarget(string target, IReadOnlyList<string> solutions, IReadOnlyList<string> dependencies) =>
        new("Contoso Test", Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), [.. solutions.Select(_ => SnapshotFile.Load(TestFile))], dependencies.ToDictionary(d => d, _ => (string?)null));

    private static Fakes TargetFakes() => new() { TargetBehavior = ReadTarget };

    // The seed as a managed zip one version higher, with its valid settings file, in a new folder.
    private string DeployFolder()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_temp, "deploy-" + Guid.NewGuid().ToString("N"))).FullName;
        var dir = Path.Combine(_temp, "seed-" + Guid.NewGuid().ToString("N"));
        foreach (var f in Directory.EnumerateFiles(V1, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(dir, Path.GetRelativePath(V1, f));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(f, to);
        }

        var xml = Path.Combine(dir, "Other", "Solution.xml");
        File.WriteAllText(xml, File.ReadAllText(xml).Replace("<Managed>0</Managed>", "<Managed>1</Managed>").Replace("<Version>1.0.0.0</Version>", "<Version>1.0.0.1</Version>"));
        SolutionZip.Create(dir, Path.Combine(folder, "PowerOpsSeed.zip"));
        File.Copy(Valid, Path.Combine(folder, "PowerOpsSeed.settings.json"));
        return folder;
    }

    private static (int Code, string Out, string Err) CliWith(Fakes fakes, params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e, fakes.Pac, fakes.Live, fakes.Target), o.ToString(), e.ToString());
    }

    private async Task<CallToolResult> Call(string tool, Dictionary<string, object?>? arguments = null) => await _client.CallToolAsync(tool, arguments, cancellationToken: TestContext.Current.CancellationToken);

    private static string Text(CallToolResult result, int block = 0) => ((TextContentBlock)result.Content[block]).Text;

    private static (int Code, string Out, string Err) Cli(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    private string PlanFolder(string header)
    {
        var folder = Path.Combine(_temp, "repo", "docs", "plans", "x");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "plan.md"), $"---\n{header}\n---\n# Plan\n");
        return Path.Combine(_temp, "repo");
    }

    private async Task Refused(string tool, Dictionary<string, object?>? arguments, params string[] lines)
    {
        AssertRefused(await Call(tool, arguments), lines);
    }

    private static void AssertRefused(CallToolResult result, params string[] lines)
    {
        Assert.True(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal(string.Join("\n", lines), Text(result));
    }

    // 1

    [Fact]
    public async Task The_tool_list_equals_the_golden_and_every_tool_has_all_four_annotations()
    {
        var tools = (await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken)).Select(t => t.ProtocolTool).ToList();

        Assert.Equal(ToolNames, tools.Select(t => t.Name));
        foreach (var tool in tools)
        {
            // readOnlyHint, destructiveHint, idempotentHint, openWorldHint
            var expected = tool.Name switch
            {
                "plan_facts" or "drift" => new bool?[] { true, false, true, true },
                "deploy_plan" => [false, false, false, true],
                "deploy_apply" => [false, true, false, true],
                _ => [true, false, true, false],
            };
            Assert.Equal(expected, [tool.Annotations!.ReadOnlyHint, tool.Annotations.DestructiveHint, tool.Annotations.IdempotentHint, tool.Annotations.OpenWorldHint]);
        }

        Assert.Contains("deploy_apply", tools.Single(t => t.Name == "deploy_plan").Description);
        Assert.Equal("2026-07-28", _client.NegotiatedProtocolVersion);
        Golden.Verify(JsonSerializer.Serialize(tools, new JsonSerializerOptions(McpJsonUtilities.DefaultOptions) { WriteIndented = true }), "mcp-tools.txt");
    }

    // 2

    [Theory]
    [InlineData("analyze", "analyze.schema.json")]
    [InlineData("rules", "rules.schema.json")]
    [InlineData("diff", "diff.schema.json")]
    [InlineData("test", "test.schema.json")]
    [InlineData("settings_validate", "settings-validate.schema.json")]
    [InlineData("plan_status", "plan-status.schema.json")]
    [InlineData("drift", "drift.schema.json")]
    public async Task A_tool_returns_the_stdout_of_the_command_and_it_matches_its_schema(string tool, string schema)
    {
        var plans = PlanFolder("status: draft\nreview: pending");
        var (arguments, cli) = tool switch
        {
            "analyze" => (new Dictionary<string, object?> { ["paths"] = new[] { Fixtures } }, new[] { "analyze", "--format", "json", "--", Fixtures }),
            "rules" => ([], ["rules", "--json"]),
            "diff" => (new Dictionary<string, object?> { ["old"] = V1, ["new"] = V2 }, ["diff", "--format", "json", "--", V1, V2]),
            "test" => (new Dictionary<string, object?> { ["path"] = Alm, ["tests"] = AlmTests }, ["test", "--format", "json", "--tests", AlmTests, "--", Alm]),
            "settings_validate" => (new Dictionary<string, object?> { ["solution"] = V1, ["settings"] = Valid }, ["settings", "validate", "--settings", Valid, "--format", "json", "--", V1]),
            "drift" => (new Dictionary<string, object?> { ["solution"] = "PowerOpsSeed", ["envs"] = new[] { DevFile, TestFile } }, ["drift", "--solution", "PowerOpsSeed", "--env", DevFile, "--env", TestFile, "--format", "json"]),
            _ => (new Dictionary<string, object?> { ["path"] = plans }, ["plan", "status", "--json", "--", plans]),
        };

        var result = await Call(tool, arguments);

        var expected = Cli(cli);
        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal(expected.Out, Text(result));
        Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load(schema), JsonNode.Parse(Text(result))!));
    }

    [Fact]
    public async Task Settings_generate_returns_the_settings_file_the_cli_prints()
    {
        var result = await Call("settings_generate", new() { ["solution"] = V1 });

        Assert.False(result.IsError);
        Assert.Equal(Cli("settings", "generate", "--", V1).Out, Text(result));
    }

    [Fact]
    public async Task Baseline_returns_the_file_the_cli_writes()
    {
        var file = Path.Combine(_temp, "baseline.json");
        Assert.Equal(0, Cli("baseline", Fixtures, "--out", file).Code);

        var result = await Call("baseline", new() { ["paths"] = new[] { Fixtures } });

        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal(File.ReadAllText(file), Text(result));
    }

    // 3

    [Fact]
    public async Task Plan_facts_equals_the_cli_run_with_the_same_fake_and_matches_its_schema()
    {
        _fakes.PacBehavior = PlanFactsFakes().PacBehavior;

        var result = await Call("plan_facts");

        var expected = CliWith(PlanFactsFakes(), "plan", "facts");
        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal(expected.Out, Text(result));
        Assert.Equal(2, _fakes.Pacs);
        Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load("plan-facts.schema.json"), JsonNode.Parse(Text(result))!));
    }

    [Fact]
    public async Task Drift_with_one_live_environment_and_one_snapshot_file_equals_the_cli_and_matches_its_schema()
    {
        _fakes.LiveBehavior = DriftFakes().LiveBehavior;

        var result = await Call("drift", new() { ["solution"] = "PowerOpsSeed", ["envs"] = new[] { "live-env", DevFile } });

        var expected = CliWith(DriftFakes(), "drift", "--solution", "PowerOpsSeed", "--env", "live-env", "--env", DevFile, "--format", "json");
        Assert.False(result.IsError);
        Assert.Equal(expected.Out, Text(result));
        Assert.Equal(1, _fakes.Lives);
        Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load("drift.schema.json"), JsonNode.Parse(Text(result))!));
    }

    [Fact]
    public async Task Deploy_plan_equals_the_cli_and_matches_its_schema_and_writes_nothing_without_save()
    {
        var folder = DeployFolder();
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;

        var result = await Call("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret });

        var expected = CliWith(TargetFakes(), "deploy", "plan", "--target", Secret, "--format", "json", "--", folder);
        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal(expected.Out, Text(result));
        Assert.Equal(1, _fakes.Targets);
        Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load("deploy-plan.schema.json"), JsonNode.Parse(Text(result))!));
        Assert.Equal(["PowerOpsSeed.settings.json", "PowerOpsSeed.zip"], Files(folder));
    }

    private static string[] Files(string folder) => [.. Directory.EnumerateFileSystemEntries(folder).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

    // 4

    [Fact]
    public async Task Exit_two_and_three_are_errors_with_the_error_line()
    {
        var empty = Directory.CreateDirectory(Path.Combine(_temp, "empty")).FullName;
        var notZip = Path.Combine(_temp, "not.zip");
        File.WriteAllText(notZip, "not a zip");

        var analyze = await Call("analyze", new() { ["paths"] = new[] { empty } });
        var diff = await Call("diff", new() { ["old"] = notZip, ["new"] = V1 });

        Assert.True(analyze.IsError);
        Assert.StartsWith("error: no solutions found", Text(analyze));
        Assert.True(diff.IsError);
        Assert.StartsWith("error:", Text(diff));
    }

    [Fact]
    public async Task A_malformed_plan_is_an_error_that_still_holds_the_json()
    {
        var plans = PlanFolder("status: aproved\nreview: pending");

        var result = await Call("plan_status", new() { ["path"] = plans });

        Assert.True(result.IsError);
        Assert.Equal(2, result.Content.Count);
        Assert.Equal(Cli("plan", "status", "--json", "--", plans).Out, Text(result));
        using var doc = JsonDocument.Parse(Text(result));
        Assert.True(doc.RootElement.TryGetProperty("plans", out _));
        Assert.Equal("error: the command failed (exit code 2)", Text(result, 1));
    }

    [Fact]
    public async Task A_path_that_looks_like_a_help_option_is_a_path_not_found()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "/h is not an absolute path on Windows");

        await Refused("analyze", new() { ["paths"] = new[] { "/h" } }, "error: path not found: /h");
        await Refused("diff", new() { ["old"] = "/?", ["new"] = "/?" }, "error: path not found: /?");
        await Refused("settings_validate", new() { ["solution"] = "/h", ["settings"] = Valid }, "error: path not found: /h");
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;
        await Refused("deploy_plan", new() { ["folder"] = "/?", ["target"] = Secret }, "error: the folder argument is not an existing folder");
        Assert.Equal(0, _fakes.Targets);
    }

    private static int Findings(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("solutions").EnumerateArray().Sum(s => s.GetProperty("findings").GetArrayLength());
    }

    [Fact]
    public async Task Analyze_with_a_baseline_equals_the_cli_and_lists_fewer_findings()
    {
        var baseline = Path.Combine(_temp, "baseline.json");
        File.WriteAllText(baseline, Text(await Call("baseline", new() { ["paths"] = new[] { Fixtures } })));

        var without = await Call("analyze", new() { ["paths"] = new[] { Fixtures } });
        var with = await Call("analyze", new() { ["paths"] = new[] { Fixtures }, ["baseline"] = baseline });

        Assert.False(with.IsError);
        Assert.Equal(Cli("analyze", "--format", "json", "--baseline", baseline, "--", Fixtures).Out, Text(with));
        Assert.True(Findings(Text(with)) < Findings(Text(without)));
    }

    // 5

    [Fact]
    public async Task A_value_that_starts_with_a_dash_is_refused()
    {
        await Refused("analyze", new() { ["paths"] = new[] { "--help" } }, "error: paths 1: a value that starts with - or @ is refused");
        await Refused("analyze", new() { ["paths"] = new[] { V1 }, ["baseline"] = "-x" }, "error: baseline: a value that starts with - or @ is refused");
    }

    [Fact]
    public async Task A_response_file_value_is_refused_and_nothing_is_written()
    {
        var written = Path.Combine(_temp, "written.json");
        var response = Path.Combine(_temp, "args.rsp");
        File.WriteAllText(response, $"--out {written}\n--format json\n");

        await Refused("analyze", new() { ["paths"] = new[] { "@" + response } }, "error: paths 1: a value that starts with - or @ is refused");

        Assert.False(File.Exists(written));
    }

    [Fact]
    public async Task Problems_are_all_reported_at_once_with_field_names_only()
    {
        await Refused("diff", new() { ["old"] = 5, ["extra"] = 1 }, "error: extra: unknown field", "error: old: must be a string", "error: new: missing");
        await Refused("analyze", new() { ["paths"] = new[] { V1, "relative", "" } }, "error: paths 3: must not be empty", "error: paths 2: must be an absolute path");
    }

    [Fact]
    public async Task A_relative_path_is_refused()
    {
        await Refused("diff", new() { ["old"] = "old.zip", ["new"] = V2 }, "error: old: must be an absolute path");
    }

    [Fact]
    public async Task A_wrong_type_is_refused()
    {
        await Refused("analyze", new() { ["paths"] = V1 }, "error: paths: must be a list of strings");
        await Refused("analyze", new() { ["paths"] = new object[] { V1, 1 } }, "error: paths: must be a list of strings");
        await Refused("diff", new() { ["old"] = 5, ["new"] = V2 }, "error: old: must be a string");
    }

    [Fact]
    public async Task A_missing_or_null_required_field_is_refused()
    {
        await Refused("diff", new() { ["old"] = V1 }, "error: new: missing");
        await Refused("diff", new() { ["old"] = V1, ["new"] = null }, "error: new: missing");
        await Refused("plan_status", null, "error: path: missing");
        await Refused("analyze", new() { ["paths"] = Array.Empty<string>() }, "error: paths: give at least 1");
    }

    [Fact]
    public async Task An_empty_string_is_refused()
    {
        await Refused("plan_status", new() { ["path"] = "" }, "error: path: must not be empty");
        await Refused("plan_status", new() { ["path"] = "  " }, "error: path: must not be empty");
    }

    [Fact]
    public async Task An_unknown_field_is_refused()
    {
        await Refused("rules", new() { ["format"] = "text" }, "error: format: unknown field");
    }

    [Fact]
    public async Task Null_for_an_optional_field_is_the_same_as_leaving_it_out()
    {
        var absent = await Call("analyze", new() { ["paths"] = new[] { V1 } });
        var given = await Call("analyze", new() { ["paths"] = new[] { V1 }, ["baseline"] = null });

        Assert.False(given.IsError);
        Assert.Equal(Text(absent), Text(given));
    }

    [Fact]
    public async Task An_unknown_tool_is_a_protocol_error_not_a_tool_error()
    {
        await Assert.ThrowsAsync<McpProtocolException>(async () => await Call("nope"));
    }

    [Theory]
    [InlineData(@"\\host\share\x", true)]
    [InlineData("//host/share/x", true)]
    [InlineData(@"\\.\pipe\x", true)]
    [InlineData(@"\\?\C:\x", true)]
    [InlineData("/tmp/x", false)]
    [InlineData(@"C:\x", false)]
    public void A_network_or_device_path_is_recognised(string value, bool expected)
    {
        Assert.Equal(expected, McpCommand.IsNetworkPath(value));
    }

    // 5, with fakes

    [Theory]
    [InlineData(@"\\host\share\x")]
    [InlineData("//host/share/x")]
    public async Task A_network_path_is_refused_as_a_path_and_as_an_env_entry_and_nothing_runs(string path)
    {
        await Refused("analyze", new() { ["paths"] = new[] { path } }, @"error: paths 1: a network or device path (starting with \\ or //) is refused");
        await Refused("drift", new() { ["solution"] = "S", ["envs"] = new[] { path, DevFile } }, @"error: envs 1: a network or device path (starting with \\ or //) is refused");

        Assert.Equal(0, _fakes.Total);
    }

    [Fact]
    public async Task A_response_file_in_target_or_folder_is_refused_and_nothing_runs_or_is_written()
    {
        var folder = DeployFolder();
        var (a, b) = (Path.Combine(_temp, "a.json"), Path.Combine(_temp, "b.json"));
        var response = Path.Combine(_temp, "args.rsp");
        File.WriteAllText(response, string.Join("\n", "--save", a, "--out", b, "--apply") + "\n");

        await Refused("deploy_plan", new() { ["folder"] = folder, ["target"] = "@" + response }, "error: target: a value that starts with - or @ is refused");
        await Refused("deploy_plan", new() { ["folder"] = "@" + response, ["target"] = Secret }, "error: folder: a value that starts with - or @ is refused");

        Assert.Equal(0, _fakes.Total);
        Assert.False(File.Exists(a));
        Assert.False(File.Exists(b));
    }

    [Fact]
    public async Task A_bad_env_entry_is_refused_and_nothing_runs()
    {
        var file = Path.Combine(_temp, "snapshot.txt");
        File.WriteAllText(file, "{}");

        await Refused("drift", new() { ["solution"] = "S", ["envs"] = new[] { "-x", DevFile } }, "error: envs 1: a value that starts with - or @ is refused");
        await Refused("drift", new() { ["solution"] = "S", ["envs"] = new[] { DevFile, "" } }, "error: envs 2: must not be empty");
        await Refused("drift", new() { ["solution"] = "S", ["envs"] = new[] { "rel/snap.json", DevFile } }, "error: envs 1: must be an absolute path");
        await Refused("drift", new() { ["solution"] = "S", ["envs"] = new[] { file, DevFile } }, "error: envs 1: an entry that names a file must end in .json");
        await Refused("drift", new() { ["solution"] = "S", ["envs"] = new[] { DevFile } }, "error: envs: give at least 2");

        Assert.Equal(0, _fakes.Total);
    }

    [Fact]
    public async Task The_same_calls_with_clean_values_reach_the_fakes_once()
    {
        _fakes.LiveBehavior = DriftFakes().LiveBehavior;
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;

        Assert.False((await Call("drift", new() { ["solution"] = "PowerOpsSeed", ["envs"] = new[] { "live-env", DevFile } })).IsError);
        Assert.Equal((1, 0), (_fakes.Lives, _fakes.Targets));
        Assert.False((await Call("deploy_plan", new() { ["folder"] = DeployFolder(), ["target"] = Secret })).IsError);
        Assert.Equal((1, 1), (_fakes.Lives, _fakes.Targets));
    }

    [Fact]
    public async Task Null_for_save_runs_like_an_absent_save()
    {
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;
        var folder = DeployFolder();

        var result = await Call("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = null });

        Assert.False(result.IsError);
        Assert.Equal(["PowerOpsSeed.settings.json", "PowerOpsSeed.zip"], Files(folder));
    }

    // 6

    [Fact]
    public async Task Save_writes_exactly_one_new_file_holding_the_result()
    {
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;
        var folder = DeployFolder();
        var before = Files(folder);

        var result = await Call("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = "plan.json" });

        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal([.. before, "plan.json"], Files(folder));
        var bytes = File.ReadAllBytes(Path.Combine(folder, "plan.json"));
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal(Text(result), new System.Text.UTF8Encoding(false).GetString(bytes));
    }

    [Fact]
    public async Task Save_refuses_an_existing_file_or_folder_before_the_target_is_read()
    {
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;
        var folder = DeployFolder();
        File.WriteAllText(Path.Combine(folder, "plan.json"), "keep");
        Directory.CreateDirectory(Path.Combine(folder, "dir.json"));

        await Refused("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = "plan.json" }, "error: save: the file already exists in folder");
        await Refused("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = "dir.json" }, "error: save: the file already exists in folder");

        Assert.Equal(0, _fakes.Targets);
        Assert.Equal("keep", File.ReadAllText(Path.Combine(folder, "plan.json")));
    }

    [Theory]
    [InlineData("sub/plan.json", "error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json")]
    [InlineData("..\\plan.json", "error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json")]
    [InlineData("../plan.json", "error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json")]
    [InlineData("plan.txt", "error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json")]
    [InlineData("plan.json\n", "error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json")]
    [InlineData("plan.settings.json", "error: save: a name that ends in .settings.json is refused")]
    [InlineData("plan.Settings.json", "error: save: a name that ends in .settings.json is refused")]
    [InlineData("CON.json", "error: save: a Windows device name is refused")]
    [InlineData("nul.plan.json", "error: save: a Windows device name is refused")]
    [InlineData("Lpt1.json", "error: save: a Windows device name is refused")]
    [InlineData("Plan.Settings.JSON", "error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json")]
    public async Task Save_refuses_a_bad_name_before_the_target_is_read(string save, string line)
    {
        _fakes.TargetBehavior = TargetFakes().TargetBehavior;
        var folder = DeployFolder();

        await Refused("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = save }, line);

        Assert.Equal(0, _fakes.Targets);
        Assert.Equal(["PowerOpsSeed.settings.json", "PowerOpsSeed.zip"], Files(folder));
    }

    [Fact]
    public async Task A_save_that_cannot_be_written_is_an_error_after_the_json_and_never_replaces_the_file()
    {
        var folder = DeployFolder();
        _fakes.TargetBehavior = (t, s, d) =>
        {
            // The file appears between the check and the write: CreateNew must not replace it.
            File.WriteAllText(Path.Combine(folder, "plan.json"), "late");
            return ReadTarget(t, s, d);
        };

        var result = await Call("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = "plan.json" });

        Assert.True(result.IsError);
        Assert.Equal(2, result.Content.Count);
        Assert.StartsWith("{", Text(result));
        Assert.Equal("error: save: the plan file was not saved (it exists, or the folder is not writable)", Text(result, 1));
        Assert.Equal("late", File.ReadAllText(Path.Combine(folder, "plan.json")));
    }

    [Fact]
    public async Task A_plan_that_exits_2_writes_no_file()
    {
        var folder = Directory.CreateDirectory(Path.Combine(_temp, "no-zips")).FullName;

        var result = await Call("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret, ["save"] = "plan.json" });

        Assert.True(result.IsError);
        Assert.Empty(Files(folder));
        Assert.Equal(0, _fakes.Targets);
    }

    // 7, through the server

    // A forwarded line that starts with "error:" must not pass the filter: the CLI prefixes every forwarded line.
    private static readonly string Forwarded = $"az said {Secret} account admin@contoso.com\nerror: {Secret} admin@contoso.com";

    private static void AssertNoForwardedText(CallToolResult result)
    {
        Assert.True(result.IsError);
        var all = string.Concat(result.Content.Select(c => ((TextContentBlock)c).Text));
        Assert.DoesNotContain("secret-host", all);
        Assert.DoesNotContain("admin@contoso.com", all);
        Assert.DoesNotContain("az said", all);
    }

    [Fact]
    public async Task Text_forwarded_from_a_failed_live_read_never_reaches_the_result()
    {
        _fakes.LiveBehavior = (_, _) => throw new DriftReadException("the read failed", Forwarded);

        var result = await Call("drift", new() { ["solution"] = "PowerOpsSeed", ["envs"] = new[] { "live-env", DevFile } });

        Assert.Equal(1, _fakes.Lives);
        AssertNoForwardedText(result);
    }

    [Fact]
    public async Task Text_forwarded_from_a_failed_target_read_never_reaches_the_result()
    {
        _fakes.TargetBehavior = (_, _, _) => throw new DriftReadException("the read failed", Forwarded);

        var result = await Call("deploy_plan", new() { ["folder"] = DeployFolder(), ["target"] = Secret });

        Assert.Equal(1, _fakes.Targets);
        AssertNoForwardedText(result);
    }

    // 8

    [Fact]
    public async Task A_blocked_deploy_plan_does_not_hold_up_the_other_requests()
    {
        var folder = DeployFolder();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var timeout = TimeSpan.FromSeconds(20);
        _fakes.TargetBehavior = (t, s, d) =>
        {
            entered.Set();
            Assert.True(release.Wait(timeout, TestContext.Current.CancellationToken), "the test never released the target read");
            return ReadTarget(t, s, d);
        };

        var first = Call("deploy_plan", new() { ["folder"] = folder, ["target"] = Secret });
        try
        {
            Assert.True(entered.Wait(timeout, TestContext.Current.CancellationToken));
            var tools = await _client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken).AsTask().WaitAsync(timeout, TestContext.Current.CancellationToken);
            var rules = await Call("rules").WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.Equal(ToolNames.Length, tools.Count);
            Assert.False(rules.IsError);
            Assert.False(first.IsCompleted);
        }
        finally
        {
            release.Set();
        }

        Assert.False((await first.WaitAsync(timeout, TestContext.Current.CancellationToken)).IsError);
    }

    // The SDK's own client sends no notifications/cancelled, so this speaks JSON-RPC over the pipes.
    [Fact]
    public async Task A_cancelled_deploy_plan_does_not_write_the_plan_file()
    {
        var folder = DeployFolder();
        var timeout = TimeSpan.FromSeconds(20);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var done = new ManualResetEventSlim();
        var seen = new TaskCompletionSource<CancellationToken>();
        var fakes = new Fakes
        {
            TargetBehavior = (t, s, d) =>
            {
                entered.Set();
                Assert.True(release.Wait(timeout), "the test never released the target read");
                return ReadTarget(t, s, d);
            },
        };
        var options = McpCommand.Options(fakes.Pac, fakes.Live, fakes.Target);
        var inner = options.Handlers.CallToolHandler!;
        options.Handlers.CallToolHandler = async (context, token) =>
        {
            seen.TrySetResult(token);
            try
            {
                return await inner(context, token);
            }
            finally
            {
                done.Set();
            }
        };
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();
        await using var server = McpServer.Create(new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()), options);
        var run = server.RunAsync(TestContext.Current.CancellationToken);
        using var writer = new StreamWriter(clientToServer.Writer.AsStream(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(serverToClient.Reader.AsStream(), Encoding.UTF8);
        try
        {
            await Send(writer, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}""");
            Assert.NotNull(await reader.ReadLineAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(timeout, TestContext.Current.CancellationToken));
            await Send(writer, """{"jsonrpc":"2.0","method":"notifications/initialized"}""");
            await Send(writer, JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new { name = "deploy_plan", arguments = new { folder, target = Secret, save = "plan.json" } },
            }));
            Assert.True(entered.Wait(timeout, TestContext.Current.CancellationToken));

            await Send(writer, """{"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":2,"reason":"test"}}""");
            var token = await seen.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.True(token.WaitHandle.WaitOne(timeout), "the handler's token was never cancelled");
        }
        finally
        {
            release.Set();
        }

        Assert.True(done.Wait(timeout, TestContext.Current.CancellationToken));
        Assert.Equal(["PowerOpsSeed.settings.json", "PowerOpsSeed.zip"], Files(folder));
        writer.Close();
        try
        {
            await run.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static Task Send(StreamWriter writer, string line) => writer.WriteLineAsync(line.AsMemory(), TestContext.Current.CancellationToken);

    // 7

    [Fact]
    public void Exit_one_with_no_output_is_an_error()
    {
        var result = McpCommand.Result(1, "", "");

        Assert.True(result.IsError);
        Assert.Equal("error: the command failed (exit code 1)", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    [Fact]
    public void An_error_with_no_error_line_gets_one()
    {
        var result = McpCommand.Result(3, "", "pac said something\n  | and more\n");

        Assert.True(result.IsError);
        Assert.Equal("error: the command failed (exit code 3)", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    [Fact]
    public void Only_the_error_lines_of_stderr_are_kept_after_the_output()
    {
        var result = McpCommand.Result(2, "{}", "forwarded text\r\nerror: one\r\n  | indented\nerror: two\n");

        Assert.True(result.IsError);
        Assert.Equal(["{}", "error: one\nerror: two"], result.Content.Select(c => ((TextContentBlock)c).Text));
    }

    [Fact]
    public void Exit_zero_with_json_is_one_block_and_not_an_error()
    {
        var result = McpCommand.Result(0, "{\"a\":1}\n", "");

        Assert.False(result.IsError);
        Assert.Equal("{\"a\":1}\n", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    [Fact]
    public void Exit_one_with_a_report_is_not_an_error_and_a_code_beyond_three_is()
    {
        Assert.False(McpCommand.Result(1, "{}", "").IsError);
        Assert.True(McpCommand.Result(4, "{}", "").IsError);
    }

    // M19: deploy_apply. Each elicitation test builds its own server and client over the same fakes (a client declares its capabilities at connect).

    private const string TargetName = "Contoso Test";
    private const string GuardLine = "error: apply: an import is already running in this server; nothing was imported";
    private const string Hint = "error: apply: to import without the form, the user runs: powerops deploy apply <plan file> --target <environment id or URL> --apply";
    private const string Declined = "error: apply: the confirmation form was declined; nothing was imported";
    private const string Cancelled = "error: apply: the confirmation form was cancelled; nothing was imported";
    private const string WrongName = "error: apply: the name typed in the form is not the plan's target; nothing was imported";
    private const string FormFailed = "error: apply: the confirmation form failed; nothing was imported";
    private const string NotApplicable = "error: apply: the plan file cannot be applied; nothing was imported";
    private const string DryRunFailed = "error: apply: the dry run did not pass, so no form was sent; nothing was imported";
    private const string OtherName = "Other Name";
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);

    private static readonly string[] Revisions = ["2026-07-28", "2025-06-18"];

    private readonly ConcurrentQueue<string[]> _imports = [];

    // What the client's form handler does: answers at once, or (with no answer function) holds each form open until the test answers it.
    private sealed class Forms(Func<ElicitRequestParams, ElicitResult>? answer = null)
    {
        private readonly ConcurrentQueue<TaskCompletionSource<ElicitResult>> _held = [];
        private readonly SemaphoreSlim _opened = new(0);

        public ConcurrentQueue<ElicitRequestParams> Seen { get; } = [];

        public async ValueTask<ElicitResult> Handle(ElicitRequestParams? request, CancellationToken cancellationToken)
        {
            Seen.Enqueue(request!);
            if (answer is not null)
            {
                return answer(request!);
            }

            var held = new TaskCompletionSource<ElicitResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _held.Enqueue(held);
            _opened.Release();
            return await held.Task.WaitAsync(cancellationToken);
        }

        // The oldest form that is open, to be answered by the test.
        public async Task<TaskCompletionSource<ElicitResult>> Next()
        {
            Assert.True(await _opened.WaitAsync(Wait, TestContext.Current.CancellationToken), "no form was sent");
            Assert.True(_held.TryDequeue(out var held));
            return held;
        }
    }

    // For a call that must not send a form: if it does, the test fails on what it sees instead of waiting for an answer.
    private static Forms NoFormExpected() => new(_ => new ElicitResult { Action = "cancel" });

    private sealed class Pair(McpServer server, Task run, McpClient client, ConcurrentQueue<string[]> ran) : IAsyncDisposable
    {
        public McpClient Client => client;

        // Every argument list the server passed to Program.Run.
        public ConcurrentQueue<string[]> Ran => ran;

        public async ValueTask DisposeAsync()
        {
            await client.DisposeAsync();
            await server.DisposeAsync();
            try
            {
                await run;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private static async Task<Pair> Connect(Fakes fakes, string revision, Forms forms, ElicitationCapability? elicitation = null)
    {
        var ran = new ConcurrentQueue<string[]>();
        var (clientToServer, serverToClient) = (new Pipe(), new Pipe());
        var server = McpServer.Create(new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()), McpCommand.Options(fakes.Pac, fakes.Live, fakes.Target, ran.Enqueue));
        var run = server.RunAsync();
        var client = await McpClient.CreateAsync(
            new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()),
            new McpClientOptions { ProtocolVersion = revision, Capabilities = elicitation is null ? null : new ClientCapabilities { Elicitation = elicitation }, Handlers = new McpClientHandlers { ElicitationHandler = forms.Handle } },
            cancellationToken: TestContext.Current.CancellationToken);
        return new Pair(server, run, client, ran);
    }

    private static Task<CallToolResult> Apply(Pair pair, Dictionary<string, object?> arguments) => pair.Client.CallToolAsync("deploy_apply", arguments, cancellationToken: TestContext.Current.CancellationToken).AsTask();

    private static Dictionary<string, object?> ApplyArgs(string planFile, bool? apply = true)
    {
        var arguments = new Dictionary<string, object?> { ["planFile"] = planFile, ["target"] = Secret };
        if (apply is not null)
        {
            arguments["apply"] = apply;
        }

        return arguments;
    }

    private static ElicitResult Accepted(string name) => new() { Action = "accept", Content = new Dictionary<string, JsonElement> { ["environment"] = JsonSerializer.SerializeToElement(name) } };

    // A plan file saved by deploy plan --save with the fakes, beside the seed zip and its settings file.
    private (string Folder, string PlanFile) SavedPlan()
    {
        var folder = DeployFolder();
        var planFile = Path.Combine(folder, "plan.json");
        Assert.Equal(0, CliWith(TargetFakes(), "deploy", "plan", "--target", Secret, "--save", planFile, "--", folder).Code);
        return (folder, planFile);
    }

    private static void EditPlan(string planFile, Action<JsonObject> edit)
    {
        var plan = JsonNode.Parse(File.ReadAllText(planFile))!.AsObject();
        edit(plan);
        File.WriteAllText(planFile, plan.ToJsonString());
    }

    // The fakes of a tenant that matches the plan, and a pac that records the imports it is asked for.
    private void ImportFakes(Func<int, PacResult>? pac = null)
    {
        _fakes.TargetBehavior = ReadTarget;
        _fakes.PacBehavior = a =>
        {
            _imports.Enqueue([.. a]);
            return pac?.Invoke(_imports.Count) ?? Ok("imported\n");
        };
    }

    private (int Code, string Out, string Err) CliApply(string planFile, bool apply, PacResult? pac = null)
    {
        string[] args = ["deploy", "apply", planFile, "--target", Secret, "--format", "json", .. (apply ? new[] { "--apply" } : [])];
        return CliWith(new Fakes { TargetBehavior = ReadTarget, PacBehavior = _ => pac ?? Ok("imported\n") }, args);
    }

    // What a refusal after a failed dry run holds: the CLI's stdout for the same arguments, then its error lines and the tool's line.
    private static string[] DryRunThen(Fakes fakes, string planFile, string line)
    {
        var dry = CliWith(fakes, "deploy", "apply", planFile, "--target", Secret, "--format", "json");
        var errors = dry.Err.Split('\n').Where(l => l.StartsWith("error:", StringComparison.Ordinal));
        return [.. (dry.Out.Length > 0 ? new[] { dry.Out } : []), string.Join("\n", [.. errors, line])];
    }

    private static void AssertNoApply(Pair pair) => Assert.DoesNotContain(pair.Ran, a => a.Contains("--apply"));

    private static string Status(CallToolResult result) => (string)JsonNode.Parse(Text(result))!["status"]!;

    // 2

    [Theory]
    [InlineData("absent")]
    [InlineData("false")]
    [InlineData("null")]
    public async Task Without_apply_the_tool_is_a_dry_run_that_imports_nothing_and_sends_no_form(string apply)
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        var arguments = ApplyArgs(planFile, apply == "false" ? false : null);
        if (apply == "null")
        {
            arguments["apply"] = null;
        }

        var forms = NoFormExpected();
        await using var pair = await Connect(_fakes, "2026-07-28", forms);

        var results = new[] { await Call("deploy_apply", arguments), await Apply(pair, arguments) };

        string[] ran = ["deploy", "apply", "--target", Secret, "--format", "json", "--", planFile];
        foreach (var result in results)
        {
            Assert.False(result.IsError);
            Assert.Single(result.Content);
            Assert.Equal(CliApply(planFile, apply: false).Out, Text(result));
            Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load("deploy-apply.schema.json"), JsonNode.Parse(Text(result))!));
        }

        Assert.Equal(0, _fakes.Pacs);
        Assert.Empty(forms.Seen);
        Assert.Equal(ran, Assert.Single(_ran));
        Assert.Equal(ran, Assert.Single(pair.Ran));
    }

    // 3

    [Fact]
    public async Task Apply_from_a_client_that_cannot_show_the_form_is_refused_before_anything_runs()
    {
        var (_, planFile) = SavedPlan();

        await Refused("deploy_apply", ApplyArgs(planFile), "error: apply: this client cannot show the confirmation form; nothing was imported", Hint);

        // A client that declares only url elicitation can't show a form either (if the SDK added a form capability, the call would reach the form and these lines would differ).
        var forms = NoFormExpected();
        await using var pair = await Connect(_fakes, "2026-07-28", forms, new ElicitationCapability { Url = new() });
        AssertRefused(await Apply(pair, ApplyArgs(planFile)), "error: apply: this client cannot show the confirmation form; nothing was imported", Hint);

        Assert.Empty(forms.Seen);
        Assert.Equal(0, _fakes.Total);
        Assert.Empty(_ran);
        Assert.Empty(pair.Ran);
    }

    // 4

    public static TheoryData<string, string, string> NotTheYes()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var revision in Revisions)
        {
            foreach (var (kind, line) in new[]
            {
                ("decline", Declined), ("cancel", Cancelled), ("action-case", Cancelled), ("name-case", WrongName), ("trailing-space", WrongName), ("empty-name", WrongName),
                ("no-content", WrongName), ("empty-content", WrongName), ("number", WrongName),
            })
            {
                data.Add(revision, kind, line);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(NotTheYes))]
    public async Task Anything_but_an_accept_with_the_exact_name_imports_nothing(string revision, string kind, string line)
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        var forms = new Forms(_ => kind switch
        {
            "decline" => new ElicitResult { Action = "decline" },
            "cancel" => new ElicitResult { Action = "cancel" },
            "action-case" => new ElicitResult { Action = "Accept", Content = Accepted(TargetName).Content },
            "name-case" => Accepted("contoso test"),
            "trailing-space" => Accepted("Contoso Test "),
            "empty-name" => Accepted(""),
            "no-content" => new ElicitResult { Action = "accept" },
            "empty-content" => new ElicitResult { Action = "accept", Content = new Dictionary<string, JsonElement>() },
            _ => new ElicitResult { Action = "accept", Content = new Dictionary<string, JsonElement> { ["environment"] = JsonSerializer.SerializeToElement(5) } },
        });
        await using var pair = await Connect(_fakes, revision, forms);

        AssertRefused(await Apply(pair, ApplyArgs(planFile)), line, Hint);

        Assert.Single(forms.Seen);
        Assert.Equal(0, _fakes.Pacs);
        AssertNoApply(pair);
    }

    [Fact]
    public async Task A_client_whose_form_handler_fails_imports_nothing()
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        await using var pair = await Connect(_fakes, "2025-06-18", new Forms(_ => throw new InvalidOperationException("boom")));

        AssertRefused(await Apply(pair, ApplyArgs(planFile)), FormFailed, Hint);

        Assert.Equal(0, _fakes.Pacs);
        AssertNoApply(pair);
    }

    // 5

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-06-18")]
    public async Task The_exact_name_in_an_accepted_form_imports_the_plan_and_the_form_holds_what_the_user_must_see(string revision)
    {
        var (folder, planFile) = SavedPlan();
        ImportFakes();
        var forms = new Forms(_ => Accepted(TargetName));
        await using var pair = await Connect(_fakes, revision, forms);

        var result = await Apply(pair, ApplyArgs(planFile));

        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal(CliApply(planFile, apply: true).Out, Text(result));
        Assert.Equal("", OutputSchemaTests.Errors(OutputSchemaTests.Load("deploy-apply.schema.json"), JsonNode.Parse(Text(result))!));
        var import = Assert.Single(_imports);
        Assert.Equal(["solution", "import"], import.Take(2));
        Assert.Equal(Path.Combine(folder, "PowerOpsSeed.settings.json"), import[(Array.IndexOf(import, "--settings-file") + 1)]);
        var apply = Assert.Single(pair.Ran, a => a.Contains("--apply"));
        Assert.Equal(["deploy", "apply", "--target", Secret, "--format", "json", "--apply", "--", planFile], apply);

        var form = Assert.Single(forms.Seen);
        var property = Assert.Single(form.RequestedSchema!.Properties);
        var field = Assert.IsType<ElicitRequestParams.StringSchema>(property.Value);
        var hash = (string)JsonNode.Parse(File.ReadAllText(planFile))!["hash"]!;
        Assert.Equal("form", form.Mode);
        Assert.Equal("environment", property.Key);
        Assert.Null(field.Default);
        Assert.Equal(["environment"], form.RequestedSchema.Required);
        Assert.Null(form.Meta);
        Assert.Contains($"Target environment: {TargetName}\nPlan hash: {hash}\nSolution zips: PowerOpsSeed.zip\n", form.Message);
        Assert.DoesNotContain("secret-host", form.Message);
        Assert.DoesNotContain("plan.json", form.Message);
        Assert.DoesNotContain(folder, form.Message);
    }

    // 6

    private static Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> ChangedTarget(string kind) => (_, solutions, dependencies) =>
        new(kind == "name" ? OtherName : TargetName, Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), [.. solutions.Select(_ => SnapshotFile.Load(kind == "snapshot" ? DevFile : TestFile))], dependencies.ToDictionary(d => d, _ => (string?)null));

    [Theory]
    [InlineData("snapshot")]
    [InlineData("name")]
    public async Task A_target_that_changed_since_the_plan_fails_the_dry_run_so_no_form_is_sent(string kind)
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        _fakes.TargetBehavior = ChangedTarget(kind);
        var forms = NoFormExpected();
        await using var pair = await Connect(_fakes, "2026-07-28", forms);

        var result = await Apply(pair, ApplyArgs(planFile));

        Assert.True(result.IsError);
        Assert.Equal(DryRunThen(new Fakes { TargetBehavior = ChangedTarget(kind) }, planFile, DryRunFailed), result.Content.Select(c => ((TextContentBlock)c).Text));
        Assert.Empty(forms.Seen);
        Assert.Equal(0, _fakes.Pacs);
        AssertNoApply(pair);
    }

    [Theory]
    [InlineData("snapshot")]
    [InlineData("name")]
    public async Task A_target_that_changes_while_the_form_is_open_is_refused_after_the_yes_and_imports_nothing(string kind)
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        await using var pair = await Connect(_fakes, "2026-07-28", new Forms(_ =>
        {
            _fakes.TargetBehavior = ChangedTarget(kind);
            return Accepted(TargetName);
        }));

        var result = await Apply(pair, ApplyArgs(planFile));

        var cli = CliWith(new Fakes { TargetBehavior = ChangedTarget(kind), PacBehavior = _ => Ok("imported\n") }, "deploy", "apply", planFile, "--target", Secret, "--format", "json", "--apply");
        Assert.Equal(kind == "snapshot" ? 1 : 2, cli.Code);
        Assert.Equal(kind == "name", result.IsError);
        Assert.Equal(cli.Code == 1 ? cli.Out : string.Join("\n", cli.Err.Split('\n').Where(l => l.StartsWith("error:", StringComparison.Ordinal))), Text(result));
        if (kind == "snapshot")
        {
            Assert.Equal("refused", Status(result));
        }

        Assert.Equal(0, _fakes.Pacs);
    }

    // 7

    [Fact]
    public async Task A_failed_import_is_an_error_with_the_failed_report()
    {
        var (_, planFile) = SavedPlan();
        ImportFakes(_ => new PacResult(true, false, 1, "", "boom"));
        await using var pair = await Connect(_fakes, "2026-07-28", new Forms(_ => Accepted(TargetName)));

        var result = await Apply(pair, ApplyArgs(planFile));

        Assert.True(result.IsError);
        Assert.Equal("failed", Status(result));
        Assert.DoesNotContain("boom", string.Concat(result.Content.Select(c => ((TextContentBlock)c).Text)));
    }

    // 8

    [Fact]
    public async Task One_import_runs_at_a_time_and_the_guard_is_released_once_by_the_call_that_took_it()
    {
        var (_, planFile) = SavedPlan();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        ImportFakes(n =>
        {
            if (n == 1)
            {
                entered.Set();
                Assert.True(release.Wait(Wait), "the test never released the first import");
            }

            return Ok("imported\n");
        });
        var forms = new Forms();
        await using var pair = await Connect(_fakes, "2025-06-18", forms);
        Task<CallToolResult>? first = null;
        try
        {
            first = Apply(pair, ApplyArgs(planFile));
            var firstForm = await forms.Next();
            var second = Apply(pair, ApplyArgs(planFile));
            var secondForm = await forms.Next();

            firstForm.SetResult(Accepted(TargetName));
            Assert.True(entered.Wait(Wait, TestContext.Current.CancellationToken));
            secondForm.SetResult(Accepted(TargetName));
            AssertRefused(await second.WaitAsync(Wait, TestContext.Current.CancellationToken), GuardLine);

            AssertRefused(await Apply(pair, ApplyArgs(planFile)).WaitAsync(Wait, TestContext.Current.CancellationToken), GuardLine);
            Assert.Equal(2, forms.Seen.Count);
            var dry = await Apply(pair, ApplyArgs(planFile, apply: null)).WaitAsync(Wait, TestContext.Current.CancellationToken);
            Assert.False(dry.IsError);
            Assert.False(first.IsCompleted);
        }
        finally
        {
            release.Set();
        }

        Assert.False((await first.WaitAsync(Wait, TestContext.Current.CancellationToken)).IsError);
        var fourth = Apply(pair, ApplyArgs(planFile));
        (await forms.Next()).SetResult(Accepted(TargetName));
        Assert.False((await fourth.WaitAsync(Wait, TestContext.Current.CancellationToken)).IsError);
        Assert.Equal(2, _imports.Count);
    }

    // 9

    [Theory]
    [InlineData("hash")]
    [InlineData("target")]
    public async Task A_plan_file_rewritten_while_the_form_is_open_is_refused_and_nothing_runs(string field)
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        await using var pair = await Connect(_fakes, "2026-07-28", new Forms(_ =>
        {
            EditPlan(planFile, plan => plan[field] = field == "hash" ? new string('a', 64) : OtherName);
            return Accepted(TargetName);
        }));

        AssertRefused(await Apply(pair, ApplyArgs(planFile)), "error: planFile: the plan file changed while the form was open; nothing was imported");

        Assert.Equal(0, _fakes.Pacs);
        AssertNoApply(pair);
    }

    // 10

    [Theory]
    [InlineData("blocked")]
    [InlineData("version")]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("symlink")]
    [InlineData("surrogate")]
    public async Task A_plan_file_that_cannot_be_applied_gets_the_dry_run_report_and_a_line_and_no_form(string kind)
    {
        var (folder, planFile) = SavedPlan();
        EditPlan(planFile, plan =>
        {
            if (kind == "blocked")
            {
                plan["blocked"] = true;
            }
            else if (kind == "version")
            {
                plan["tool"] = "9.9.9";
            }
            else if (kind == "surrogate")
            {
                plan["target"] = "MARK";
            }
        });
        var file = kind switch
        {
            "missing" => Path.Combine(folder, "nope.json"),
            "symlink" => Path.Combine(folder, "link.json"),
            _ => planFile,
        };
        if (kind == "malformed")
        {
            File.WriteAllText(planFile, "{not json");
        }
        else if (kind == "surrogate")
        {
            File.WriteAllText(planFile, File.ReadAllText(planFile).Replace("MARK", "\\ud800"));
        }
        else if (kind == "symlink")
        {
            try
            {
                File.CreateSymbolicLink(file, planFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                Assert.Skip("This OS refuses to create a symbolic link here.");
            }
        }

        var forms = NoFormExpected();
        await using var pair = await Connect(_fakes, "2026-07-28", forms);

        var result = await Apply(pair, ApplyArgs(file));

        Assert.True(result.IsError);
        Assert.Equal(DryRunThen(new Fakes(), file, NotApplicable), result.Content.Select(c => ((TextContentBlock)c).Text));
        Assert.Empty(forms.Seen);
        Assert.Equal(0, _fakes.Total);
        Assert.DoesNotContain("--apply", Assert.Single(pair.Ran));
    }

    [Theory]
    [InlineData("zero-width")]
    [InlineData("leading-space")]
    [InlineData("trailing-space")]
    [InlineData("long")]
    [InlineData("line-separator")]
    [InlineData("paragraph-separator")]
    [InlineData("private-use")]
    public async Task A_target_name_the_user_cannot_read_and_type_is_refused_without_a_form(string kind)
    {
        var (_, planFile) = SavedPlan();
        EditPlan(planFile, plan => plan["target"] = kind switch
        {
            "zero-width" => "Contoso\u200BTest",
            "leading-space" => " Contoso Test",
            "trailing-space" => "Contoso Test ",
            "line-separator" => "Contoso\u2028Test",
            "paragraph-separator" => "Contoso\u2029Test",
            "private-use" => "Contoso\uE000Test",
            _ => new string('x', 201),
        });
        var forms = NoFormExpected();
        await using var pair = await Connect(_fakes, "2026-07-28", forms);

        AssertRefused(
            await Apply(pair, ApplyArgs(planFile)),
            "error: planFile: the plan's target name cannot be shown in the form (too long, control or format characters, or white space at either end); nothing was imported", Hint);

        Assert.Empty(forms.Seen);
        Assert.Equal(0, _fakes.Total);
        Assert.Empty(pair.Ran);
    }

    // 11: raw lines. The SDK's client resolves forms inside CallToolAsync and sends no cancellation.

    private sealed class Raw : IAsyncDisposable
    {
        private readonly McpServer _server;
        private readonly Task _run;
        private readonly StreamWriter _writer;
        private readonly StreamReader _reader;

        public Raw(Fakes fakes, ConcurrentQueue<string[]> ran)
        {
            var options = McpCommand.Options(fakes.Pac, fakes.Live, fakes.Target, ran.Enqueue);
            var inner = options.Handlers.CallToolHandler!;
            options.Handlers.CallToolHandler = async (context, token) =>
            {
                Token.TrySetResult(token);
                try
                {
                    return await inner(context, token);
                }
                finally
                {
                    Done.Release();
                }
            };
            var (clientToServer, serverToClient) = (new Pipe(), new Pipe());
            _server = McpServer.Create(new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()), options);
            _run = _server.RunAsync();
            _writer = new StreamWriter(clientToServer.Writer.AsStream(), new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            _reader = new StreamReader(serverToClient.Reader.AsStream(), Encoding.UTF8);
        }

        // Released each time a tools/call handler returns.
        public SemaphoreSlim Done { get; } = new(0);

        // The token of the first tools/call handler.
        public TaskCompletionSource<CancellationToken> Token { get; } = new();

        public Task Send(JsonNode message) => _writer.WriteLineAsync(message.ToJsonString().AsMemory(), TestContext.Current.CancellationToken);

        public async Task<JsonNode> Read() => JsonNode.Parse(await _reader.ReadLineAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(Wait, TestContext.Current.CancellationToken) ?? throw new EndOfStreamException())!;

        public async ValueTask DisposeAsync()
        {
            _writer.Close();
            try
            {
                await _run.WaitAsync(Wait);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await _server.DisposeAsync();
                _reader.Dispose();
            }
        }
    }

    private static JsonObject ToolCall(int id, string planFile, JsonObject? meta = null, string? requestState = null)
    {
        var parameters = new JsonObject { ["name"] = "deploy_apply", ["arguments"] = new JsonObject { ["planFile"] = planFile, ["target"] = Secret, ["apply"] = true } };
        if (meta is not null)
        {
            parameters["_meta"] = meta;
        }

        if (requestState is not null)
        {
            parameters["requestState"] = requestState;
        }

        return new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = "tools/call", ["params"] = parameters };
    }

    [Fact]
    public async Task At_2026_07_28_the_form_is_an_input_required_result_and_a_retry_with_no_answer_imports_nothing()
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        var ran = new ConcurrentQueue<string[]>();
        await using var raw = new Raw(_fakes, ran);
        static JsonObject Meta() => new()
        {
            ["io.modelcontextprotocol/protocolVersion"] = "2026-07-28",
            ["io.modelcontextprotocol/clientCapabilities"] = new JsonObject { ["elicitation"] = new JsonObject { ["form"] = new JsonObject() } },
        };

        await raw.Send(ToolCall(1, planFile, Meta()));
        var first = (await raw.Read())["result"]!;
        Assert.Equal("input_required", (string)first["resultType"]!);
        Assert.Equal("elicitation/create", (string)Assert.Single(first["inputRequests"]!.AsObject()).Value!["method"]!);

        await raw.Send(ToolCall(2, planFile, Meta(), (string)first["requestState"]!));
        var second = (await raw.Read())["result"]!;

        Assert.True((bool)second["isError"]!);
        Assert.Equal(string.Join("\n", FormFailed, Hint), (string)second["content"]![0]!["text"]!);
        Assert.Equal(0, _fakes.Pacs);
        Assert.DoesNotContain(ran, a => a.Contains("--apply"));
    }

    [Fact]
    public async Task At_2025_06_18_a_cancelled_call_imports_nothing_even_when_the_form_is_answered_after_it()
    {
        var (_, planFile) = SavedPlan();
        ImportFakes();
        var ran = new ConcurrentQueue<string[]>();
        await using var raw = new Raw(_fakes, ran);
        await raw.Send(JsonNode.Parse("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"elicitation":{"form":{}}},"clientInfo":{"name":"t","version":"1"}}}""")!);
        await raw.Read();
        await raw.Send(JsonNode.Parse("""{"jsonrpc":"2.0","method":"notifications/initialized"}""")!);
        await raw.Send(ToolCall(2, planFile));
        var form = await raw.Read();
        Assert.Equal("elicitation/create", (string)form["method"]!);

        await raw.Send(JsonNode.Parse("""{"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":2,"reason":"test"}}""")!);
        var token = await raw.Token.Task.WaitAsync(Wait, TestContext.Current.CancellationToken);
        Assert.True(token.WaitHandle.WaitOne(Wait), "the handler's token was never cancelled");
        await raw.Send(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = form["id"]!.DeepClone(),
            ["result"] = new JsonObject { ["action"] = "accept", ["content"] = new JsonObject { ["environment"] = TargetName } },
        });

        Assert.True(await raw.Done.WaitAsync(Wait, TestContext.Current.CancellationToken), "the handler never returned");
        Assert.Equal(0, _fakes.Pacs);
        Assert.DoesNotContain(ran, a => a.Contains("--apply"));
    }

    // 12

    [Fact]
    public async Task Input_that_the_checks_refuse_gives_the_servers_error_lines_and_runs_nothing()
    {
        var (_, planFile) = SavedPlan();
        var response = Path.Combine(_temp, "args.rsp");
        File.WriteAllText(response, "--apply\n");

        await Refused("deploy_apply", ApplyArgs("plan.json"), "error: planFile: must be an absolute path");
        await Refused("deploy_apply", ApplyArgs("-plan.json"), "error: planFile: a value that starts with - or @ is refused");
        await Refused("deploy_apply", ApplyArgs("@" + response), "error: planFile: a value that starts with - or @ is refused");
        await Refused("deploy_apply", ApplyArgs(@"\\host\share\x"), @"error: planFile: a network or device path (starting with \\ or //) is refused");
        await Refused("deploy_apply", new() { ["planFile"] = planFile, ["target"] = "@" + response }, "error: target: a value that starts with - or @ is refused");
        await Refused("deploy_apply", new() { ["planFile"] = planFile, ["target"] = Secret, ["apply"] = "true" }, "error: apply: must be true or false");
        await Refused("deploy_apply", new() { ["planFile"] = planFile, ["target"] = Secret, ["extra"] = 1 }, "error: extra: unknown field");

        Assert.Equal(0, _fakes.Total);
        Assert.Empty(_ran);
    }
}