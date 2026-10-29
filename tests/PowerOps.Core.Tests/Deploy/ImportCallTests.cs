using System.Text;
using System.Text.Json.Nodes;

using PowerOps.Core.Deploy;

namespace PowerOps.Core.Tests.Deploy;

public class ImportCallTests
{
    private const string Env = "00000000-0000-0000-0000-000000000001";
    private const string Url = "https://contoso-test.crm.dynamics.com";
    private const string PathHint = "must be a literal absolute path (no $, backtick, escape, ~ or wildcard)";
    private const string CantRead = "the command holds an import that the gate can't read";
    private const string Stray = "the command has a word the gate doesn't read";

    // Paths are built per OS, so the Windows leg of CI reads them as fully qualified.
    private static readonly string Dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "powerops-gate"));
    private static readonly string Zip = Path.Combine(Dir, "PowerOpsSeed.zip");
    private static readonly string Settings = Path.Combine(Dir, "PowerOpsSeed.settings.json");

    // Single quotes keep a Windows backslash literal.
    private static string Q(string path) => $"'{path}'";

    private static string Import(string extra = "") => $"pac solution import --path {Q(Zip)} --environment {Env} {extra}".TrimEnd();

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    // The common fields and the order of the inputs recorded from Claude Code 2.1.312 and Codex 0.163.0 (ids and paths scrubbed).
    private static JsonObject Claude(string tool, JsonObject input, JsonObject? mcpServer = null)
    {
        var doc = new JsonObject
        {
            ["session_id"] = "<id>",
            ["transcript_path"] = "<path>",
            ["cwd"] = "<cwd>",
            ["scratchpad_dir"] = "<dir>",
            ["prompt_id"] = "<id>",
            ["permission_mode"] = "default",
            ["effort"] = new JsonObject { ["level"] = "medium" },
            ["hook_event_name"] = "PreToolUse",
            ["tool_name"] = tool,
            ["tool_input"] = input,
            ["tool_use_id"] = "<id>",
        };
        if (mcpServer is not null)
        {
            doc["mcp_server"] = mcpServer;
        }

        return doc;
    }

    private static JsonObject Codex(string tool, JsonObject input) => new()
    {
        ["session_id"] = "<id>",
        ["turn_id"] = "<id>",
        ["transcript_path"] = "<path>",
        ["cwd"] = "<cwd>",
        ["hook_event_name"] = "PreToolUse",
        ["model"] = "<model>",
        ["permission_mode"] = "default",
        ["tool_name"] = tool,
        ["tool_input"] = input,
        ["tool_use_id"] = "<id>",
    };

    private static ImportRead Bash(string command, string tool = "Bash") => ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = tool, ["tool_input"] = new JsonObject { ["command"] = command } }));

    private static ImportRead Mcp(JsonObject input, string tool = "mcp__pac-cli__solution_import") => ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = tool, ["tool_input"] = input }));

    private static JsonObject McpInput() => new() { ["--path"] = Zip, ["--environment"] = Env };

    private static ImportCall Checked(ImportRead read) => Assert.IsType<ImportRead.Check>(read).Call;

    private static ImportRead.Blocked Blocked(ImportRead read) => Assert.IsType<ImportRead.Blocked>(read);

    // ---- 1. not an import --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("ls -la")]
    [InlineData("git status")]
    [InlineData("dotnet test")]
    [InlineData("pac solution list")]
    [InlineData("pac auth list --verbose")]
    [InlineData("pac solution export --name Seed --path /x/seed.zip")]
    [InlineData("node \"${PLUGIN_ROOT}/scripts/solution-settings.js\" template --env https://contoso-test.crm.dynamics.com")]
    [InlineData("powerops deploy apply deploy/powerops-plan.json --target 00000000-0000-0000-0000-000000000001 --apply")]
    [InlineData("echo \"it's fine")]
    public void A_shell_command_without_an_import_is_not_one(string command)
    {
        Assert.IsType<ImportRead.NotImport>(Bash(command));
    }

    [Theory]
    [InlineData("Write")]
    [InlineData("Read")]
    [InlineData("spawn_agent")]
    [InlineData("mcp__other__list")]
    public void Another_tool_is_not_an_import(string tool)
    {
        Assert.IsType<ImportRead.NotImport>(ImportCall.Read(Bytes(Codex(tool, new JsonObject { ["command"] = "pac solution import" }))));
    }

    [Fact]
    public void An_apply_patch_whose_text_holds_an_import_is_not_one()
    {
        Assert.IsType<ImportRead.NotImport>(ImportCall.Read(Bytes(Codex("apply_patch", new JsonObject { ["command"] = "*** Begin Patch\n+pac solution import\n*** End Patch" }))));
    }

    // ---- 2. readable forms -------------------------------------------------------------------------------------

    [Fact]
    public void Claudes_Bash_input_with_all_four_fields_gives_the_call()
    {
        var read = ImportCall.Read(Bytes(Claude("Bash", new JsonObject { ["command"] = $"pac solution import --environment {Env} --path {Q(Zip)} --settings-file {Q(Settings)} --stage-and-upgrade", ["description"] = "Import" })));

        Assert.Equal(new ImportCall(Zip, Env, Settings, true), Checked(read));
    }

    [Fact]
    public void Codexs_Bash_input_with_the_command_only_gives_the_call()
    {
        var read = ImportCall.Read(Bytes(Codex("Bash", new JsonObject { ["command"] = Import() })));

        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(read));
    }

    public static TheoryData<string> Readable()
    {
        var z = Q(Zip);
        var qz = $"\"{Zip}\"";
        return new()
        {
            { $"pac solution import --path {z} --environment {Env}" },
            { $"pac solution import -p {z} -env {Env}" },
            { $"pac solution import --path {z} --environment {Url} --async" },
            { $"pac solution import \\\n  --path {z} \\\n  --environment {Env} \\\n  --async --max-async-wait-time 30" },
            { $"pac solution import --path {qz} --environment '{Env}'" },
            { $"\"pac\" solution import --path {z} --environment {Env}" },
            { $"{Import()} | tail -5" },
            { $"{Import()} && pac solution list" },
            { $"{Import()} 2>&1" },
            { $"{Import()} 2>&1 | tail -5" },
            { $"cd /work && {Import()} >&2" },
            { $"pac sol\"\"ution im''port --path {z} --environment {Env}" },
            { $"pac solution import --path {z} --environment {Env} -wt 30 -a" },
        };
    }

    [Theory]
    [MemberData(nameof(Readable))]
    public void A_readable_cli_import_gives_its_call(string command)
    {
        var call = Checked(Bash(command));

        Assert.Equal(Zip, call.Zip);
        Assert.True(call.Environment is Env or Url);
        Assert.Null(call.Settings);
        Assert.False(call.StageAndUpgrade);
    }

    [Theory]
    [InlineData("--stage-and-upgrade")]
    [InlineData("-up")]
    public void Stage_and_upgrade_by_either_name_is_read(string flag)
    {
        Assert.True(Checked(Bash(Import($"{flag} --settings-file {Q(Settings)}"))).StageAndUpgrade);
    }

    [Theory]
    [InlineData("mcp__pac-cli__solution_import")]
    [InlineData("mcp__pac_cli__solution_import")]
    [InlineData("mcp__plugin_dataverse_pac-cli__solution_import")]
    public void The_mcp_form_gives_its_call_under_any_servers_name(string tool)
    {
        var input = McpInput();
        input["--settings-file"] = Settings;
        input["--stage-and-upgrade"] = "true";
        input["--max-async-wait-time"] = "30";
        input["--async"] = "true";

        Assert.Equal(new ImportCall(Zip, Env, Settings, true), Checked(Mcp(input, tool)));
    }

    [Fact]
    public void The_mcp_server_key_is_ignored_beside_tool_input_and_inside_it()
    {
        var server = new JsonObject { ["name"] = "pac-cli", ["source"] = "dynamic" };
        var beside = ImportCall.Read(Bytes(Claude("mcp__pac-cli__solution_import", McpInput(), server)));
        var input = McpInput();
        input["mcp_server"] = server.DeepClone();
        var inside = ImportCall.Read(Bytes(Claude("mcp__pac-cli__solution_import", input)));
        var codex = ImportCall.Read(Bytes(Codex("mcp__pac_cli__solution_import", McpInput())));

        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(beside));
        Assert.Equal(Checked(beside), Checked(inside));
        Assert.Equal(Checked(beside), Checked(codex));
    }

    [Fact]
    public void The_mcp_environment_may_be_an_https_url()
    {
        var input = McpInput();
        input["--environment"] = Url;

        Assert.Equal(Url, Checked(Mcp(input)).Environment);
    }

    [Fact]
    public void A_quoted_windows_path_is_read_on_windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows paths");

        Assert.Equal(Zip, Checked(Bash($"pac solution import --path \"{Zip}\" --environment {Env}")).Zip);
    }

    // ---- 3. blocks ---------------------------------------------------------------------------------------------

    private static readonly string[] Unread =
    [
        "--force-overwrite", "--activate-plugins", "--publish-changes", "--import-as-holding", "--convert-to-managed", "--skip-dependency-check",
        "--skip-lower-version", "--activate-flows", "--update-canvas-app-ownership", "--import-mode", "--Path",
    ];

    public static TheoryData<string, string> Blocks()
    {
        var z = Q(Zip);
        var data = new TheoryData<string, string>
        {
            { $"pac solution import --environment {Env}", "--path is missing" },
            { $"pac solution import --path {z}", "--environment is missing" },
            { $"pac solution import --path {z} --environment", "--environment is missing" },
            { $"pac solution import --path ./solutions/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path $HOME/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path \"$ZIP\" --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path \"`x`\" --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path ~/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path *.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path /tmp/a\\ b.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path /tmp/$X/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path /tmp/*.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path \"/tmp/`x`.zip\" --environment {Env}", $"--path {PathHint}" },
            { "echo pac sol\"\"ution im''port", CantRead },
            { $"pac solution import --path //host/x.zip --environment {Env}", "--path must not be a network or device path" },
            { $"pac solution import --path {z} --environment {Env} --settings-file ./s.json", $"--settings-file {PathHint}" },
            { "pac sol\"\"ution im''port", "--path is missing" },
            { "pac $s import", CantRead },
            { $"$X/pac solution import --path {z} --environment {Env}", CantRead },
            { $"$'pac' $'solution' $'import' --path {z} --environment {Env}", CantRead },
            // Before \r\n a backslash escapes the \r, so the newline ends the command and --environment is not pac's.
            { $"pac solution import --path {z} \\\r\n  --environment {Env}", "--environment is missing" },
            { $"pac solution import --path {z} --environment {Env}\r", "--environment must be an environment id or https URL" },
            { $"{Import()} > log", Stray },
            { $"{Import()} < in", Stray },
            { $"{Import()} extra", Stray },
            { $"{Import()} 2>log", Stray },
            { $"pac solution import --path {z} --environment Test", "--environment must be an environment id or https URL" },
            { $"pac solution import --path {z} --environment http://contoso-test.crm.dynamics.com", "--environment must be an environment id or https URL" },
            { $"pac solution import --path {z} --environment -env", "--environment must be an environment id or https URL" },
            { $"pac solution import --path {z} --environment $E", "--environment must be an environment id or https URL" },
            { $"{Import()} -wt '$(x)'", "--max-async-wait-time must be a whole number of minutes" },
            { $"{Import()} -wt \"`x`\"", "--max-async-wait-time must be a whole number of minutes" },
            { $"{Import()} -wt soon", "--max-async-wait-time must be a whole number of minutes" },
            { $"{Import()} --path=/x.zip", "the gate doesn't read --path" },
            { $"{Import()} && {Import()}", "the command holds more than one import" },
            { $"sh -c '{Import()}'", CantRead },
            { $"eval \"{Import()}\"", CantRead },
            { $"X=1 {Import()}", CantRead },
            { "echo \"pac solution import\"", CantRead },
            { "git commit -m \"docs: say how pac solution import works\"", CantRead },
            { "grep \"pac solution import\" docs", CantRead },
            { "grep ImportSolution src/", CantRead },
            { $"{Import()} 'unclosed", "the quotes are not closed" },
            { "echo it's; pac solution import", "the quotes are not closed" },
            { $"curl -X POST \"$URL/api/data/v9.2/ImportSolutionAsync\" -d @body.json", CantRead },
            { $"pac --verbose solution import --path {z} --environment {Env}", CantRead },
            { $"pac solution import --path ./solutions/x.zip --environment {Url} --async --activate-plugins", "the gate doesn't read --activate-plugins" },
            { $"pac solution import --path ./solutions/x.zip --environment {Url} --async --activate-plugins", $"--path {PathHint}" },
            // The always-blocked routes, with the skills' own command lines.
            { $"node \"${{PLUGIN_ROOT}}/scripts/solution-settings.js\" import --env {Url} --package {z} --settings {Q(Settings)}", "model-apps' solution-settings.js import builds its own import request, so its options can't be checked" },
            { $"node \"${{CLAUDE_PLUGIN_ROOT}}/scripts/encode-solution-file.js\" --zipPath {z}", "power-pages' import-solution sends the zip in a request body, so its zip and environment can't be checked" },
            { $"node \"${{CLAUDE_PLUGIN_ROOT}}/scripts/import-template-solution.js\" --zipPath {z} --envUrl {Url} --publishChanges true", "create-site's template import imports a template solution that no plan holds, and overwrites unmanaged customizations" },
            { $"pac pipeline deploy --environment {Env} --solutionName Seed --stageId {Env} --currentVersion 1.0.0.0 --newVersion 1.0.0.1 --wait", "pac pipeline deploy has no zip, and its target is a pipeline stage" },
            { $"pac package deploy --package {z} --environment {Url}", "pac package deploy imports a package that no plan holds" },
            { $"pac solution upgrade --solution-name Seed --environment {Env}", "pac solution upgrade applies a staged upgrade that no plan holds" },
        };

        foreach (var flag in Unread)
        {
            data.Add($"{Import()} {flag}", $"the gate doesn't read {flag}");
        }

        // A flag given twice under any two of its names, in both orders.
        string[][] names = [["--path", "-p"], ["--environment", "-env"], ["--stage-and-upgrade", "-up"], ["--async", "-a"], ["--max-async-wait-time", "-wt"]];
        foreach (var pair in names)
        {
            var value = pair[0] switch { "--path" => z, "--environment" => Env, "--max-async-wait-time" => "5", _ => "" };
            foreach (var (first, second) in new[] { (pair[0], pair[0]), (pair[0], pair[1]), (pair[1], pair[0]), (pair[1], pair[1]) })
            {
                var others = pair[0] switch { "--path" => $"--environment {Env}", "--environment" => $"--path {z}", _ => $"--path {z} --environment {Env}" };
                data.Add($"pac solution import {others} {first} {value} {second} {value}", $"{pair[0]} is given twice");
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Blocks))]
    public void A_shell_block_gives_its_line(string command, string line)
    {
        Assert.Contains(line, Blocked(Bash(command)).Lines);
    }

    [Fact]
    public void A_reading_block_asks_for_a_readable_import_and_a_route_asks_for_the_plan()
    {
        Assert.True(Blocked(Bash("pac $s import")).Unreadable);
        Assert.False(Blocked(Bash("pac package deploy --package /x.zip --environment " + Url)).Unreadable);
    }

    [Fact]
    public void A_flag_of_300_characters_with_a_control_character_is_printed_cleaned_and_cut()
    {
        var flag = "--" + new string('a', 296) + "\u0007bc";

        var line = Assert.Single(Blocked(Bash($"{Import()} {flag}")).Lines);

        Assert.Equal("the gate doesn't read " + flag[..100], line);
        Assert.DoesNotContain('\u0007', line);
    }

    [Fact]
    public void A_dv_solution_import_with_a_relative_path_and_an_unread_flag_gives_two_lines()
    {
        var lines = Blocked(Bash($"pac solution import --path ./solutions/x.zip --environment {Url} --async --activate-plugins")).Lines;

        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void A_posix_path_with_a_nul_is_not_a_literal_path()
    {
        var input = McpInput();
        input["--path"] = Zip + "\0";

        Assert.Contains($"--path {PathHint}", Blocked(Mcp(input)).Lines);
    }

    [Fact]
    public void Windows_forms_that_are_not_fully_qualified_or_name_a_network_or_device_are_blocked_on_windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows paths");

        Assert.Contains($"--path {PathHint}", Blocked(Bash($"pac solution import --path /c/work/x.zip --environment {Env}")).Lines);
        Assert.Contains($"--path {PathHint}", Blocked(Bash($"pac solution import --path C:\\work\\x.zip --environment {Env}")).Lines);
        foreach (var path in new[] { @"\\host\share\x.zip", @"\\?\C:\x.zip", @"\\.\CON" })
        {
            var input = McpInput();
            input["--path"] = path;
            Assert.Contains("--path must not be a network or device path", Blocked(Mcp(input)).Lines);
        }
    }

    [Fact]
    public void An_import_in_powershell_is_blocked_whatever_its_form()
    {
        Assert.Contains("imports are read only in Bash commands, not in PowerShell", Blocked(Bash(Import(), "PowerShell")).Lines);
        Assert.Contains("imports are read only in Bash commands, not in PowerShell", Blocked(Bash("& pac.exe $args", "PowerShell")).Lines);
        Assert.IsType<ImportRead.NotImport>(Bash("Get-ChildItem", "PowerShell"));
    }

    [Fact]
    public void Two_names_of_a_value_flag_each_need_a_value()
    {
        Assert.Contains("--path is missing", Blocked(Bash($"pac solution import --environment {Env} -p")).Lines);
    }

    // ---- 3. MCP blocks -----------------------------------------------------------------------------------------

    [Fact]
    public void Missing_mcp_arguments_are_named()
    {
        var lines = Blocked(Mcp([])).Lines;

        Assert.Contains("--path is missing", lines);
        Assert.Contains("--environment is missing", lines);
    }

    [Theory]
    [InlineData("--max-async-wait-time", "soon", "--max-async-wait-time must be a whole number of minutes")]
    [InlineData("--max-async-wait-time", "$(x)", "--max-async-wait-time must be a whole number of minutes")]
    [InlineData("--environment", "Test", "--environment must be an environment id or https URL")]
    [InlineData("--environment", "http://contoso-test.crm.dynamics.com", "--environment must be an environment id or https URL")]
    [InlineData("--stage-and-upgrade", "false", "--stage-and-upgrade must be \"true\"")]
    [InlineData("--force-overwrite", "true", "the gate doesn't read --force-overwrite")]
    [InlineData("-p", "x", "the gate doesn't read -p")]
    [InlineData("--path", "./solutions/x.zip", $"--path {PathHint}")]
    [InlineData("--path", "//host/x.zip", "--path must not be a network or device path")]
    [InlineData("--settings-file", "s.json", $"--settings-file {PathHint}")]
    public void An_mcp_argument_the_gate_cannot_pass_gives_its_line(string key, string value, string line)
    {
        var input = McpInput();
        input[key] = value;

        Assert.Contains(line, Blocked(Mcp(input)).Lines);
    }

    [Fact]
    public void An_mcp_number_value_blocks()
    {
        var input = McpInput();
        input["--max-async-wait-time"] = 30;

        Assert.Contains("--max-async-wait-time must be a string", Blocked(Mcp(input)).Lines);
    }

    [Fact]
    public void An_mcp_tool_input_that_is_a_string_blocks()
    {
        var read = ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = "mcp__pac_cli__solution_import", ["tool_input"] = "{bad" }));

        Assert.Equal(["the hook input has no tool_input object"], Blocked(read).Lines);
    }

    [Fact]
    public void A_key_that_appears_twice_blocks()
    {
        var json = "{\"tool_name\":\"mcp__pac-cli__solution_import\",\"tool_input\":{\"--path\":" + System.Text.Json.JsonSerializer.Serialize(Zip)
            + $",\"--environment\":\"{Env}\",\"--environment\":\"{Url}\"}}}}";

        Assert.Equal(["a key appears twice in the hook input"], Blocked(ImportCall.Read(Encoding.UTF8.GetBytes(json))).Lines);
    }

    // ---- 3. input ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("not json", "the hook input is not JSON")]
    [InlineData("", "the hook input is not JSON")]
    [InlineData("[]", "the hook input has no tool_name")]
    [InlineData("{}", "the hook input has no tool_name")]
    [InlineData("""{"tool_name":7}""", "the hook input has no tool_name")]
    [InlineData("""{"tool_name":"Bash","tool_input":{}}""", "the hook input has no command string")]
    [InlineData("""{"tool_name":"Bash","tool_input":{"command":7}}""", "the hook input has no command string")]
    [InlineData("""{"tool_name":"Bash"}""", "the hook input has no tool_input object")]
    [InlineData("""{"tool_name":"\ud800"}""", "the hook input is not JSON")]
    public void An_unreadable_input_blocks_with_the_reading_guidance(string json, string line)
    {
        var blocked = Blocked(ImportCall.Read(Encoding.UTF8.GetBytes(json)));

        Assert.Equal([line], blocked.Lines);
        Assert.True(blocked.Unreadable);
    }

    [Fact]
    public void An_input_over_1_mb_blocks_whatever_the_tool()
    {
        var json = Encoding.UTF8.GetBytes("{\"tool_name\":\"Write\",\"tool_input\":{\"content\":\"" + new string('x', ImportCall.MaxInputBytes) + "\"}}");

        Assert.Equal(["the hook input is larger than 1 MB"], Blocked(ImportCall.Read(json)).Lines);
    }

    [Fact]
    public void A_name_is_cleaned_and_cut_to_100_characters()
    {
        Assert.Equal("a b", ImportCall.Printable("a\u0007b"));
        Assert.Equal(100, ImportCall.Printable(new string('x', 300)).Length);
    }
}