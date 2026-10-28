using System.IO.Pipelines;
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
    private static readonly string[] ToolNames = ["analyze", "rules", "baseline", "diff", "test", "settings_generate", "settings_validate", "plan_status", "plan_facts", "drift", "deploy_plan"];

    private const string SolutionTable = """
        Unique Name           Friendly Name                         Version Managed
        ContosoCore           Contoso Core                          3.0.1.0 True
        """;

    private const string Secret = "https://secret-host-7f3a.crm.dynamics.com/";

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-mcp-").FullName;
    private readonly Fakes _fakes = new();
    private McpServer _server = null!;
    private Task _run = null!;
    private McpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var options = McpCommand.Options(_fakes.Pac, _fakes.Live, _fakes.Target);
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
        var result = await Call(tool, arguments);

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
                _ => [true, false, true, false],
            };
            Assert.Equal(expected, [tool.Annotations!.ReadOnlyHint, tool.Annotations.DestructiveHint, tool.Annotations.IdempotentHint, tool.Annotations.OpenWorldHint]);
        }

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

    // 5, with fakes

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

    [Fact]
    public async Task Text_forwarded_from_a_failed_live_read_never_reaches_the_result()
    {
        _fakes.LiveBehavior = (_, _) => throw new DriftReadException("the read failed", $"az said {Secret} account admin@contoso.com");

        var result = await Call("drift", new() { ["solution"] = "PowerOpsSeed", ["envs"] = new[] { "live-env", DevFile } });

        Assert.True(result.IsError);
        Assert.Equal(1, _fakes.Lives);
        var all = string.Concat(result.Content.Select(c => ((TextContentBlock)c).Text));
        Assert.DoesNotContain("secret-host", all);
        Assert.DoesNotContain("admin@contoso.com", all);
        Assert.DoesNotContain("az said", all);
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
}