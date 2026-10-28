using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Nodes;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

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
    private static readonly string[] ToolNames = ["analyze", "rules", "baseline", "diff", "test", "settings_generate", "settings_validate", "plan_status"];

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-mcp-").FullName;
    private int _liveCalls;
    private McpServer _server = null!;
    private Task _run = null!;
    private McpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        // Every tool of this commit is offline: a live function that is called is a bug.
        var options = McpCommand.Options(
            (_, _) => { Interlocked.Increment(ref _liveCalls); throw new InvalidOperationException("pac was called"); },
            (_, _) => { Interlocked.Increment(ref _liveCalls); throw new InvalidOperationException("the live read was called"); },
            (_, _, _) => { Interlocked.Increment(ref _liveCalls); throw new InvalidOperationException("the target read was called"); });
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
        Assert.Equal(0, _liveCalls);
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
            Assert.Equal(new bool?[] { true, false, true, false }, [tool.Annotations!.ReadOnlyHint, tool.Annotations.DestructiveHint, tool.Annotations.IdempotentHint, tool.Annotations.OpenWorldHint]);
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