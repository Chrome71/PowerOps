using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class DiffCommandTests : IDisposable
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private static readonly string V1 = Path.Combine(Fixtures, "powerops-seed", "v1");
    private static readonly string V2 = Path.Combine(Fixtures, "powerops-seed", "v2");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-cli-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    [Fact]
    public void Same_solution_exits_zero_with_no_changes()
    {
        var (code, output, _) = Run("diff", V1, V1);

        Assert.Equal(0, code);
        Assert.Contains("no changes", output);
    }

    [Fact]
    public void Unpacked_folder_and_its_exported_zip_exit_zero()
    {
        var zip = Path.Combine(_temp, "seed.zip");
        SolutionZip.Create(V1, zip);

        Assert.Equal(0, Run("diff", V1, zip).Code);
    }

    [Fact]
    public void Differences_exit_one_and_match_the_text_golden()
    {
        var (code, output, err) = Run("diff", V1, V2);

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Golden.Verify(output, "diff-seed.txt");
    }

    [Fact]
    public void Json_matches_golden() => Golden.Verify(Run("diff", V1, V2, "--format", "json").Out, "diff-seed.json");

    [Fact]
    public void Markdown_matches_golden() => Golden.Verify(Run("diff", V1, V2, "--format", "markdown").Out, "diff-seed.md");

    [Fact]
    public void Out_writes_the_report_to_a_file_and_still_exits_one()
    {
        var file = Path.Combine(_temp, "diff.json");

        var (code, output, _) = Run("diff", V1, V2, "--format", "json", "--out", file);

        Assert.Equal(1, code);
        Assert.Equal("", output);
        Assert.Contains("\"schemaVersion\": 2", File.ReadAllText(file));
    }

    [Fact]
    public void Missing_path_bad_format_and_missing_argument_exit_two()
    {
        var (code, _, err) = Run("diff", V1, Path.Combine(_temp, "nope.zip"));

        Assert.Equal(2, code);
        Assert.Contains("path not found", err);
        Assert.Equal(2, Run("diff", V1, V2, "--format", "sarif").Code);
        Assert.Equal(2, Run("diff", V1).Code);
    }

    [Fact]
    public void Yaml_layout_root_folders_are_diffed_with_the_canvas_app_named()
    {
        var yaml = Path.Combine(Fixtures, "powerops-seed-yaml");

        var (code, output, err) = Run("diff", Path.Combine(yaml, "v1"), Path.Combine(yaml, "v2"));

        Assert.Equal(1, code);
        Assert.Equal("", err);
        // Unlike powerops-seed, the YAML v2 canvas app was edited (a button added) and republished.
        Assert.Equal(
            [
                "  added connectionReference pops_SpareDataverse [risk: connection-reference]",
                "  changed workflow Stamp work item status (4c8dd007-dfcb-f111-a05c-0022480697a8): definition changed",
                "  changed canvasApp pops_workitems_2af60: definition changed; republished (AppVersion 2026-10-19T17:09:47.0000000Z → 2026-10-20T04:41:27.0000000Z)",
            ],
            output.Split('\n').Where(l => l.StartsWith("  ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Folder_with_several_solutions_exits_two()
    {
        var (code, _, err) = Run("diff", Path.Combine(Fixtures, "powerops-seed-yaml"), V1);

        Assert.Equal(2, code);
        Assert.Contains("holds 2 solutions", err);
    }

    [Fact]
    public void Unreadable_input_exits_three_and_names_the_file()
    {
        var bad = Path.Combine(_temp, "bad.zip");
        File.WriteAllText(bad, "not a zip");

        var (code, _, err) = Run("diff", V1, bad);

        Assert.Equal(3, code);
        Assert.StartsWith($"error: {bad}", err);
    }

    /// <summary>A copy of `powerops-seed/v1` with a hand-built agent: authentication 2, generative orchestration on, the given instructions and tools.</summary>
    private string SeedWithAgent(string name, string instructions, params (string Schema, string Data)[] tools)
    {
        var copy = Path.Combine(_temp, name);
        foreach (var file in Directory.EnumerateFiles(V1, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(copy, Path.GetRelativePath(V1, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        void Write(string path, string text)
        {
            var full = Path.Combine(copy, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }

        const string Component = "<botcomponent schemaname=\"{0}\"><componenttype>{1}</componenttype><parentbotid><schemaname>demo_agent</schemaname></parentbotid></botcomponent>";
        Write("bots/demo_agent/bot.xml", "<bot schemaname=\"demo_agent\"><authenticationmode>2</authenticationmode></bot>");
        Write("bots/demo_agent/configuration.json", """{"settings":{"GenerativeActionsEnabled":true},"recognizer":{"$kind":"GenerativeAIRecognizer"}}""");
        Write("botcomponents/demo_agent.gpt.default/botcomponent.xml", string.Format(Component, "demo_agent.gpt.default", 15));
        Write("botcomponents/demo_agent.gpt.default/data", "kind: GptComponentMetadata\ninstructions: " + instructions + "\n");
        foreach (var (schema, data) in tools)
        {
            Write($"botcomponents/{schema}/botcomponent.xml", string.Format(Component, schema, 9));
            Write($"botcomponents/{schema}/data", data);
        }

        return copy;
    }

    private (string Old, string New) AgentPair() =>
    (
        SeedWithAgent("agent-old", "Answer questions about orders.",
            ("demo_agent.topic.Orders", "kind: TaskDialog\naction:\n  kind: InvokeConnectorTaskAction\n  connectionProperties:\n    mode: Invoker\n")),
        SeedWithAgent("agent-new", "Answer questions about orders and refunds.",
            ("demo_agent.topic.Orders", "kind: TaskDialog\naction:\n  kind: InvokeConnectorTaskAction\n  connectionProperties:\n    mode: Maker\n"),
            ("demo_agent.topic.Refunds", "kind: TaskDialog\naction:\n  kind: InvokeExternalAgentTaskAction\n  connectionProperties:\n    mode: Invoker\n  operationDetails:\n    kind: ModelContextProtocolMetadata\n"))
    );

    /// <summary>
    /// A copy of `powerops-seed/v1` with an agent shaped like one from Copilot Studio's new authoring UI that M51 still doesn't fully read: no Custom GPT component, segments with no `$kind` in `configuration.json`, `McpTool` tools,
    /// and a component of a kind no reader knows.
    /// </summary>
    private string SeedWithNewUiAgent(string name, string instructions, params string[] mcpTools)
    {
        var copy = SeedWithAgent(name, "unused");
        File.Delete(Path.Combine(copy, "botcomponents", "demo_agent.gpt.default", "data"));
        File.Delete(Path.Combine(copy, "botcomponents", "demo_agent.gpt.default", "botcomponent.xml"));
        void Write(string path, string text)
        {
            var full = Path.Combine(copy, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, text);
        }

        const string Component = "<botcomponent schemaname=\"{0}\"><componenttype>9</componenttype><parentbotid><schemaname>demo_agent</schemaname></parentbotid></botcomponent>";
        Write("bots/demo_agent/configuration.json", """{"recognizer":{"$kind":"CLICopilotRecognizer"},"agentSettings":{"instructions":{"segments":[{"value":""" + System.Text.Json.JsonSerializer.Serialize(instructions) + "}]}}}");
        Write("botcomponents/demo_agent.widget.mail/botcomponent.xml", string.Format(Component, "demo_agent.widget.mail"));
        Write("botcomponents/demo_agent.widget.mail/data", "kind: Widget\nname: mail\n");
        foreach (var tool in mcpTools)
        {
            Write($"botcomponents/{tool}/botcomponent.xml", string.Format(Component, tool));
            Write($"botcomponents/{tool}/data", "kind: McpTool\nauthMode: Invoker\nconnectorId: /providers/Microsoft.PowerApps/apis/shared_demo\noperationId: search\n");
        }

        return copy;
    }

    [Fact]
    public void An_unread_agent_is_listed_in_notCompared()
    {
        var old = SeedWithNewUiAgent("new-ui-old", "Answer questions about loans.", "demo_agent.tool.Docs", "demo_agent.tool.Work");
        var @new = SeedWithNewUiAgent("new-ui-new", "Answer questions about loans. Be brief.", "demo_agent.tool.Docs");
        string[] notes =
        [
            "agent 'demo_agent': instructions not compared: neither version has instructions PowerOps can read (the text of a single Custom GPT component, or of configuration.json's segments when the agent has no Custom GPT component)",
            "agent 'demo_agent': 1 component not read as tools, so a change to it is not compared: 1 'Widget'",
        ];

        foreach (var (a, b, line) in new[] { (old, @new, "removed agentTool demo_agent.tool.Work [risk: removed-component, agent-tool]"), (@new, old, "added agentTool demo_agent.tool.Work [risk: agent-tool]") })
        {
            var (code, output, err) = Run("diff", a, b);
            Assert.Equal(1, code); // the McpTool tools are read, the segments are not
            Assert.Contains(line, output);
            Assert.Equal(string.Concat(notes.Select(n => $"note: {n}\n")), err.ReplaceLineEndings("\n"));

            using var json = System.Text.Json.JsonDocument.Parse(Run("diff", a, b, "--format", "json").Out);
            Assert.Single(json.RootElement.GetProperty("changes").EnumerateArray());
            Assert.Equal(notes, json.RootElement.GetProperty("notCompared").EnumerateArray().Select(e => e.GetString()));

            var markdown = Run("diff", a, b, "--format", "markdown").Out;
            Assert.Contains("**Not compared:**\n\n" + string.Concat(notes.Select(n => $"- {n.Replace("_", "\\_")}\n")), markdown);
        }
    }

    private static readonly string NewUi = Path.Combine(Fixtures, "agents", "new-ui");

    private static readonly string Learn = "cr123_loanhelper_abc123.tool.MicrosoftLearnDocsMCP-MicrosoftLearnDocsMCPServer_Liw";

    [Theory]
    [InlineData("v1.zip", "v2.zip")]
    [InlineData("v2.zip", "v1.zip")]
    public void Changed_instructions_of_a_new_ui_agent_exit_one_with_the_risk_and_no_note(string from, string to)
    {
        var (code, output, err) = Run("diff", Path.Combine(NewUi, from), Path.Combine(NewUi, to));

        Assert.Equal(1, code);
        Assert.Contains("  changed agent cr123_loanhelper_abc123: instructions changed [risk: agent-instructions]\n", output.ReplaceLineEndings("\n"));
        Assert.Contains("1 change (0 added, 0 removed, 1 changed)", output);
        Assert.Equal("", err);
        using var json = System.Text.Json.JsonDocument.Parse(Run("diff", Path.Combine(NewUi, from), Path.Combine(NewUi, to), "--format", "json").Out);
        Assert.Empty(json.RootElement.GetProperty("notCompared").EnumerateArray());
        Assert.Equal(["agent-instructions"], json.RootElement.GetProperty("changes")[0].GetProperty("risks").EnumerateArray().Select(e => e.GetString()));
    }

    [Theory]
    [InlineData("v1.zip", "v1-no-learn-tool.zip", "  removed agentTool cr123_loanhelper_abc123.tool.MicrosoftLearnDocsMCP-MicrosoftLearnDocsMCPServer_Liw [risk: removed-component, agent-tool]\n", "1 change (0 added, 1 removed, 0 changed)")]
    [InlineData("v1-no-learn-tool.zip", "v1.zip", "  added agentTool cr123_loanhelper_abc123.tool.MicrosoftLearnDocsMCP-MicrosoftLearnDocsMCPServer_Liw [risk: agent-tool]\n", "1 change (1 added, 0 removed, 0 changed)")]
    public void A_removed_or_added_learn_tool_of_a_new_ui_agent_exits_one_with_the_tool_risks_and_no_note(string from, string to, string line, string summary)
    {
        var (code, output, err) = Run("diff", Path.Combine(NewUi, from), Path.Combine(NewUi, to));

        Assert.Equal(1, code);
        Assert.Contains(line, output.ReplaceLineEndings("\n"));
        Assert.Contains(summary, output);
        Assert.Equal("", err);
        using var json = System.Text.Json.JsonDocument.Parse(Run("diff", Path.Combine(NewUi, from), Path.Combine(NewUi, to), "--format", "json").Out);
        Assert.Empty(json.RootElement.GetProperty("notCompared").EnumerateArray());
        var change = Assert.Single(json.RootElement.GetProperty("changes").EnumerateArray());
        Assert.Equal(Learn, change.GetProperty("key").GetString());
    }

    [Fact]
    public void A_new_ui_agent_against_itself_exits_zero_with_no_note()
    {
        var (code, output, err) = Run("diff", Path.Combine(NewUi, "v1.zip"), Path.Combine(NewUi, "v1.zip"));

        Assert.Equal(0, code);
        Assert.Contains("no changes", output);
        Assert.Equal("", err);
    }

    [Fact]
    public void Agent_instructions_and_tools_exit_one_and_match_the_text_golden()
    {
        var (old, @new) = AgentPair();

        var (code, output, err) = Run("diff", old, @new);

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Golden.Verify(output, "diff-agents.txt");
    }

    [Fact]
    public void Agent_diff_json_matches_golden()
    {
        var (old, @new) = AgentPair();

        Golden.Verify(Run("diff", old, @new, "--format", "json").Out, "diff-agents.json");
    }

    [Fact]
    public void Agent_diff_markdown_matches_golden()
    {
        var (old, @new) = AgentPair();

        Golden.Verify(Run("diff", old, @new, "--format", "markdown").Out, "diff-agents.md");
    }
}