using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class AgentReaderTests
{
    private const string Config = """{"$kind":"BotConfiguration","settings":{"GenerativeActionsEnabled":true},"gPTSettings":{"$kind":"GPTSettings","defaultSchemaName":"demo_agent.gpt.default"},"recognizer":{"$kind":"GenerativeAIRecognizer"}}""";

    private const string Gpt = "kind: GptComponentMetadata\ninstructions: Answer questions about orders.\ngptCapabilities:\n  webBrowsing: true\n";

    private const string McpTool = "kind: TaskDialog\nmodelDisplayName: orders\nmodelDescription: Order server\naction:\n  kind: InvokeExternalAgentTaskAction\n  connectionReference: demo_agent.shared_demo-orders.abc\n  connectionProperties:\n    mode: Maker\n\n  operationDetails:\n    kind: ModelContextProtocolMetadata\n    operationId: InvokeServer\n";

    private const string ConnectorTool = "kind: TaskDialog\naction:\n  kind: InvokeConnectorTaskAction\n  connectionReference: demo_agent.shared_msnweather.x\n  connectionProperties:\n    mode: Invoker\n  operationId: TodaysForecast\n";

    private const string FlowTool = "kind: TaskDialog\naction:\n  kind: InvokeFlowTaskAction\n  flowId: 00000000-0000-0000-0000-000000000001\n";

    private const string AgentTool = "kind: TaskDialog\naction:\n  kind: InvokeConnectedAgentTaskAction\n  botSchemaName: demo_other\n";

    private const string Topic = "kind: AdaptiveDialog\nbeginDialog:\n  kind: OnRecognizedIntent\n  id: main\n";

    private static string Bot(string schema = "demo_agent", string? auth = "2") =>
        $"<bot schemaname=\"{schema}\">" + (auth is null ? "" : $"<authenticationmode>{auth}</authenticationmode>") + "<authenticationtrigger>1</authenticationtrigger><name>Demo</name></bot>";

    private static string Component(string schema, string type, string? parent = "demo_agent") =>
        $"<botcomponent schemaname=\"{schema}\"><componenttype>{type}</componenttype><name>Demo</name>"
        + (parent is null ? "" : $"<parentbotid><schemaname>{parent}</schemaname></parentbotid>")
        + "<statecode>0</statecode><statuscode>1</statuscode></botcomponent>";

    private static TempFolder Base() => new TempFolder()
        .Write("Other/Solution.xml", SolutionXml.Manifest())
        .Write("Other/Customizations.xml", SolutionXml.Customizations(""));

    private static TempFolder Agent(TempFolder t, string folder = "demo_agent", string? bot = null, string? config = Config)
    {
        t.Write($"bots/{folder}/bot.xml", bot ?? Bot());
        return config is null ? t : t.Write($"bots/{folder}/configuration.json", config);
    }

    private static TempFolder Comp(TempFolder t, string folder, string type, string? data, string? parent = "demo_agent", string? xml = null)
    {
        t.Write($"botcomponents/{folder}/botcomponent.xml", xml ?? Component(folder, type, parent));
        return data is null ? t : t.Write($"botcomponents/{folder}/data", data);
    }

    private static TempFolder Full() => Comp(Comp(Comp(Comp(Comp(Comp(Comp(Agent(Base()),
        "demo_agent.gpt.default", "15", Gpt),
        "demo_agent.topic.Orders", "9", McpTool),
        "demo_agent.topic.Weather", "9", ConnectorTool),
        "demo_agent.topic.Flow", "9", FlowTool),
        "demo_agent.topic.Child", "9", AgentTool),
        "demo_agent.topic.Greeting", "9", Topic),
        "demo_agent.topic.NoText", "9", "not: [yaml"); // no TaskDialog in its text, so it is never parsed

    /// <summary>The solution read from the folder and from the same folder as an exported zip.</summary>
    private static Solution[] Both(TempFolder t)
    {
        using var dir = new TempFolder();
        var zip = Path.Combine(dir.Path, "agent.zip");
        SolutionZip.Create(t.Path, zip);
        return [SolutionLoader.Load(t.Path), SolutionLoader.Load(zip)];
    }

    private static Agent One(TempFolder t)
    {
        using (t)
        {
            var both = Both(t);
            var (folder, zip) = (Assert.Single(both[0].Agents), Assert.Single(both[1].Agents));
            Assert.Equal(folder with { Tools = [], UnreadFiles = [], OtherComponents = [], Capabilities = [] }, zip with { Tools = [], UnreadFiles = [], OtherComponents = [], Capabilities = [] });
            Assert.Equal(folder.Tools, zip.Tools);
            Assert.Equal(folder.Capabilities, zip.Capabilities);
            Assert.Equal(folder.UnreadFiles, zip.UnreadFiles);
            Assert.Equal(folder.OtherComponents, zip.OtherComponents);
            return folder;
        }
    }

    private static void Refused(TempFolder t, string path, long? maxNodes = null)
    {
        using (t)
        {
            using var dir = new TempFolder();
            var zip = Path.Combine(dir.Path, "agent.zip");
            SolutionZip.Create(t.Path, zip);
            ReadBudget Budget() => maxNodes is { } n ? new ReadBudget(maxNodes: n) : new ReadBudget();

            Assert.StartsWith(path + ": ", Assert.Throws<InvalidDataException>(() => XmlUnpackReader.Read(t.Path, Budget())).Message);
            Assert.StartsWith(path + ": ", Assert.Throws<InvalidDataException>(() => ZipReader.Read(zip, Budget())).Message);
        }
    }

    [Fact]
    public void A_folder_and_a_zip_give_the_same_agent_tools_and_instructions_with_their_paths()
    {
        using var t = Full();

        foreach (var s in Both(t))
        {
            var agent = Assert.Single(s.Agents);

            Assert.Equal("demo_agent", agent.SchemaName);
            Assert.Equal("bots/demo_agent/bot.xml", agent.File);
            Assert.Equal(2, agent.AuthenticationMode);
            Assert.Equal(new AgentConfiguration("GenerativeAIRecognizer", true), agent.Configuration);
            Assert.Equal("Answer questions about orders.", agent.Instructions);
            Assert.Equal("botcomponents/demo_agent.gpt.default/data", agent.InstructionsFile);
            Assert.Empty(agent.UnreadFiles);
            Assert.Equal(
                [
                    new AgentTool("demo_agent.topic.Child", "InvokeConnectedAgentTaskAction", false, null, "botcomponents/demo_agent.topic.Child/data"),
                    new AgentTool("demo_agent.topic.Flow", "InvokeFlowTaskAction", false, null, "botcomponents/demo_agent.topic.Flow/data"),
                    new AgentTool("demo_agent.topic.Orders", "InvokeExternalAgentTaskAction", true, "Maker", "botcomponents/demo_agent.topic.Orders/data"),
                    new AgentTool("demo_agent.topic.Weather", "InvokeConnectorTaskAction", false, "Invoker", "botcomponents/demo_agent.topic.Weather/data"),
                ],
                agent.Tools);
        }
    }

    [Fact]
    public void A_solution_without_bots_has_no_agents()
    {
        using var t = Base();

        Assert.All(Both(t), s => Assert.Empty(s.Agents));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData(null, null)]
    [InlineData("x", null)]
    public void The_authentication_mode_is_the_integer_in_bot_xml_or_null(string? text, int? mode) =>
        Assert.Equal(mode, One(Agent(Base(), bot: Bot(auth: text))).AuthenticationMode);

    [Fact]
    public void A_missing_configuration_gives_a_null_configuration()
    {
        Assert.Null(One(Agent(Base(), config: null)).Configuration);
    }

    [Theory]
    [InlineData("[1]")]
    [InlineData("\"text\"")]
    [InlineData("null")]
    public void A_configuration_that_is_not_a_json_object_gives_a_null_configuration(string json)
    {
        Assert.Null(One(Agent(Base(), config: json)).Configuration);
    }

    [Theory]
    [InlineData("""{"settings":{"GenerativeActionsEnabled":true}}""", null, true)]
    [InlineData("""{"recognizer":{"$kind":"CLIAgentRecognizer"},"settings":{"GenerativeActionsEnabled":true}}""", "CLIAgentRecognizer", true)]
    [InlineData("""{"recognizer":{"$kind":"GenerativeAIRecognizer"},"settings":{"GenerativeActionsEnabled":false}}""", "GenerativeAIRecognizer", false)]
    [InlineData("""{"recognizer":{"$kind":"GenerativeAIRecognizer"},"settings":{}}""", "GenerativeAIRecognizer", null)]
    [InlineData("""{"recognizer":{"$kind":"GenerativeAIRecognizer"}}""", "GenerativeAIRecognizer", null)]
    [InlineData("""{"recognizer":{"$kind":"GenerativeAIRecognizer"},"settings":{"GenerativeActionsEnabled":"false"}}""", "GenerativeAIRecognizer", null)]
    [InlineData("""{"recognizer":"GenerativeAIRecognizer","settings":[]}""", "", null)]
    [InlineData("""{"recognizer":{"$kind":1},"settings":{"GenerativeActionsEnabled":1}}""", "", null)]
    [InlineData("""{"recognizer":{}}""", "", null)]
    [InlineData("""{"recognizer":[]}""", "", null)]
    [InlineData("""{"recognizer":{"$kind":"\ud800"}}""", "", null)]
    public void The_recognizer_and_generative_orchestration_are_read_or_null(string json, string? recognizer, bool? generative)
    {
        Assert.Equal(new AgentConfiguration(recognizer, generative), One(Agent(Base(), config: json)).Configuration);
    }

    [Theory]
    [InlineData("kind: GptComponentMetadata\ninstructions:\n", "")]
    [InlineData("kind: GptComponentMetadata\ninstructions: \"\"\n", "")]
    [InlineData("kind: GptComponentMetadata\n", "")]
    [InlineData("kind: GptComponentMetadata\ninstructions: |\n  Line one.\n  Line two.\n", "Line one.\nLine two.\n")]
    [InlineData("kind: GptComponentMetadata\r\ninstructions: |\r\n  Line one.\r\n  Line two.\r\n", "Line one.\nLine two.\n")]
    [InlineData("kind: GptComponentMetadata\ninstructions:\n  nested: text\n", null)]
    [InlineData("kind: GptComponentMetadata\ninstructions: [a, b]\n", null)]
    public void The_instructions_are_the_top_level_text_of_the_one_custom_gpt_component(string data, string? instructions)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.gpt.default", "15", data));

        Assert.Equal(instructions, agent.Instructions);
        Assert.Equal("botcomponents/demo_agent.gpt.default/data", agent.InstructionsFile);
    }

    [Fact]
    public void Two_custom_gpt_components_or_none_give_null_instructions()
    {
        var two = One(Comp(Comp(Agent(Base()), "demo_agent.gpt.a", "15", Gpt), "demo_agent.gpt.b", "15", Gpt));
        var none = One(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", McpTool));
        var noData = One(Comp(Agent(Base()), "demo_agent.gpt.default", "15", null));

        foreach (var agent in new[] { two, none, noData })
        {
            Assert.Null(agent.Instructions);
            Assert.Null(agent.InstructionsFile);
        }
    }

    [Fact]
    public void A_topic_is_not_a_tool_and_a_topic_without_taskdialog_in_its_text_is_not_parsed()
    {
        var agent = One(Comp(Comp(Agent(Base()), "demo_agent.topic.Greeting", "9", Topic), "demo_agent.topic.Broken", "9", "a: [b\n"));

        Assert.Empty(agent.Tools);
        Assert.Empty(agent.UnreadFiles);
    }

    [Theory]
    [InlineData("kind: AdaptiveDialog\nnote: TaskDialog\n")]
    [InlineData("kind: AgentDialog\nsettings:\n  instructions: Use TaskDialog tools such as demo_agent.topic.Orders.\n")]
    public void A_type_9_data_whose_top_level_kind_is_not_taskdialog_is_not_a_tool(string data)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.topic.Other", "9", data));

        Assert.Empty(agent.Tools);
        Assert.Empty(agent.UnreadFiles);
    }

    [Theory]
    [InlineData("demo_agent", "demo_two")]
    [InlineData("demo_two", "demo_agent")]
    public void An_unread_file_is_listed_under_its_own_agent_only(string oddAgent, string okAgent)
    {
        using var t = Comp(Comp(Comp(Agent(Agent(Base()), "demo_two", Bot("demo_two")),
            $"{oddAgent}.topic.Odd", "9", "kind: TaskDialog\n" + OddKey, oddAgent),
            $"{okAgent}.topic.Ok", "9", FlowTool, okAgent),
            $"{okAgent}.topic.Flow", "9", ConnectorTool, okAgent);

        foreach (var solution in Both(t))
        {
            var odd = Assert.Single(solution.Agents, a => a.SchemaName == oddAgent);
            var ok = Assert.Single(solution.Agents, a => a.SchemaName == okAgent);
            Assert.Equal([$"botcomponents/{oddAgent}.topic.Odd/data"], odd.UnreadFiles);
            Assert.Empty(odd.Tools);
            Assert.Empty(ok.UnreadFiles);
            Assert.Equal(2, ok.Tools.Count);
        }
    }

    [Fact]
    public void Components_of_a_kind_no_reader_knows_are_listed_with_their_kind_in_file_order_and_are_not_tools()
    {
        var agent = One(Comp(Comp(Comp(Comp(Comp(Comp(Comp(Comp(Comp(Agent(Base()),
            "demo_agent.tool.Docs", "9", "kind: McpTool\nauthMode: Invoker\nconnectorId: /providers/Microsoft.PowerApps/apis/shared_demo\n"),
            "demo_agent.iq.mail", "9", "kind:  'IQCapability'  # Work IQ\ncapability: mail\naccess: Write\n"),
            "demo_agent.tool.Widget", "9", "kind:  'ConnectorTool'  # a kind no reader knows\nname: x\n"),
            "demo_agent.tool.Quoted", "9", "kind: \"Widget\"\nnote: TaskDialog\n"), // holds TaskDialog, so it is parsed
            "demo_agent.topic.Greeting", "9", Topic),
            "demo_agent.agent.Child", "9", "kind: AgentDialog\n"),
            "demo_agent.topic.Orders", "9", FlowTool),
            "demo_agent.topic.NoKind", "9", "not: [yaml"), // no kind line and malformed: listed, not parsed
            "demo_agent.topic.Gadget", "9", "kind: Gadget\n"));

        Assert.Equal(["demo_agent.tool.Docs", "demo_agent.topic.Orders"], agent.Tools.Select(x => x.SchemaName));
        Assert.Equal(["demo_agent.iq.mail"], agent.Capabilities.Select(x => x.SchemaName));
        Assert.Empty(agent.UnreadFiles);
        Assert.Equal(
            [
                new AgentComponent("Widget", "botcomponents/demo_agent.tool.Quoted/data"),
                new AgentComponent("ConnectorTool", "botcomponents/demo_agent.tool.Widget/data"),
                new AgentComponent("Gadget", "botcomponents/demo_agent.topic.Gadget/data"),
                new AgentComponent(null, "botcomponents/demo_agent.topic.NoKind/data"),
            ],
            agent.OtherComponents);
    }

    [Theory]
    [InlineData("kind:\nx: y\n")]
    [InlineData("  kind: McpTool\n")]
    [InlineData("kindly: McpTool\n")]
    public void A_data_with_no_top_level_kind_line_has_a_null_kind(string data)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.x", "9", data));

        Assert.Equal([new AgentComponent(null, "botcomponents/demo_agent.x/data")], agent.OtherComponents);
    }

    [Theory]
    [InlineData("beginDialog:\n  kind: OnRecognizedIntent\nkind: AdaptiveDialog\n", false)] // a nested kind is not the top-level one
    [InlineData("# a comment\n---\nkind: AdaptiveDialog\n", false)]
    [InlineData("\uFEFFkind: AdaptiveDialog\n", false)] // the byte order mark is dropped when the file is read
    [InlineData("beginDialog:\n  kind: OnRecognizedIntent\n", true)] // no top-level kind
    [InlineData("kind: AdaptiveDialog # a topic\n", false)] // a trailing comment is not part of the kind
    public void The_kind_is_the_first_line_that_starts_with_kind_at_column_zero(string data, bool listed)
    {
        using var t = Comp(Agent(Base()), "demo_agent.topic.T", "9", null);
        File.WriteAllText(Path.Combine(t.Path, "botcomponents", "demo_agent.topic.T", "data"), data.TrimStart('\uFEFF'), new System.Text.UTF8Encoding(data.StartsWith('\uFEFF')));

        foreach (var solution in Both(t))
        {
            var agent = Assert.Single(solution.Agents);
            Assert.Equal(listed ? [new AgentComponent(null, "botcomponents/demo_agent.topic.T/data")] : [], agent.OtherComponents);
        }
    }

    [Fact]
    public void A_standard_agent_has_no_other_components()
    {
        var agent = One(Comp(Comp(Comp(Agent(Base()), "demo_agent.gpt.default", "15", Gpt), "demo_agent.topic.Greeting", "9", Topic), "demo_agent.topic.Orders", "9", McpTool));

        Assert.Empty(agent.OtherComponents);
    }

    [Fact]
    public void A_child_agent_component_beside_two_tools_leaves_two_tools()
    {
        var agent = One(Comp(Comp(Comp(Agent(Base()),
            "demo_agent.topic.A", "9", FlowTool),
            "demo_agent.agent.Child", "9", "kind: AgentDialog\nsettings:\n  instructions: TaskDialog\n"),
            "demo_agent.topic.B", "9", ConnectorTool));

        Assert.Equal(["demo_agent.topic.A", "demo_agent.topic.B"], agent.Tools.Select(x => x.SchemaName));
    }

    public const string OddKey = "? [a, b]\n: c\n";

    // Keys the layout converter refuses and agent files read as elements (M74).
    public static TheoryData<string> ReadableKeys => new()
    {
        "'@odata.nextLink':\n  a: b\n",
        "'#text':\n  a: b\n",
        "'@xmlns': http://www.w3.org/2000/xmlns/\n",
    };

    public static TheoryData<string> OddKeys => new()
    {
        OddKey,
        "\"\": x\n",
    };

    [Theory]
    [MemberData(nameof(OddKeys))]
    public void Valid_yaml_with_a_key_the_layout_converter_cannot_map_leaves_a_tool_out_of_the_agent(string extra)
    {
        var data = "kind: TaskDialog\naction:\n  kind: InvokeExternalAgentTaskAction\n  connectionProperties:\n    mode: Maker\n" + extra;

        var agent = One(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", data));

        Assert.Empty(agent.Tools);
        Assert.Equal(["botcomponents/demo_agent.topic.Orders/data"], agent.UnreadFiles);
    }

    [Theory]
    [MemberData(nameof(OddKeys))]
    public void Valid_yaml_with_a_key_the_layout_converter_cannot_map_gives_null_instructions(string extra)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\ninstructions: Answer.\n" + extra));

        Assert.Null(agent.Instructions);
        Assert.Equal("botcomponents/demo_agent.gpt.default/data", agent.InstructionsFile);
        Assert.Empty(agent.UnreadFiles); // PO028 notes it
    }

    [Theory]
    [MemberData(nameof(ReadableKeys))]
    public void A_key_starting_with_at_or_hash_is_an_element_in_an_agent_tool_file(string extra)
    {
        var data = "kind: TaskDialog\naction:\n  kind: InvokeExternalAgentTaskAction\n  connectionProperties:\n    mode: Maker\n" + extra;

        var agent = One(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", data));

        var tool = Assert.Single(agent.Tools);
        Assert.Equal("Maker", tool.Mode);
        Assert.Empty(agent.UnreadFiles);
    }

    [Theory]
    [MemberData(nameof(ReadableKeys))]
    public void A_key_starting_with_at_or_hash_is_an_element_in_a_custom_gpt_file(string extra)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\ninstructions: Answer.\n" + extra));

        Assert.Equal("Answer.", agent.Instructions);
        Assert.Empty(agent.UnreadFiles);
    }

    [Fact]
    public void A_tool_file_of_many_odata_mappings_is_charged_to_the_node_budget()
    {
        var many = "kind: TaskDialog\n" + string.Concat(Enumerable.Range(0, 2000).Select(i => $"'@odata.id':\n  k{i}: 1\n"));

        Refused(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", many), "botcomponents/demo_agent.topic.Orders/data", maxNodes: 1000);
    }

    [Fact]
    public void Unread_files_are_the_tool_data_paths_in_ordinal_order_and_a_mappable_tool_is_not_one()
    {
        const string Odd = "kind: TaskDialog\n" + OddKey;
        var agent = One(Comp(Comp(Comp(Agent(Base()),
            "demo_agent.topic.Z", "9", Odd),
            "demo_agent.topic.A", "9", Odd),
            "demo_agent.topic.Ok", "9", FlowTool));

        Assert.Equal(["botcomponents/demo_agent.topic.A/data", "botcomponents/demo_agent.topic.Z/data"], agent.UnreadFiles);
        Assert.Equal(["demo_agent.topic.Ok"], agent.Tools.Select(x => x.SchemaName));
    }

    [Fact]
    public void A_tool_nested_deeper_than_the_xml_limit_is_refused_with_the_path_not_skipped()
    {
        var levels = SolutionParser.MaxXmlDepth + 1;
        var deep = "kind: TaskDialog\n" + string.Concat(Enumerable.Range(0, levels).Select(i => new string(' ', i) + "a:\n")) + new string(' ', levels) + "b: 1\n";

        Refused(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", deep), "botcomponents/demo_agent.topic.Orders/data");
    }

    [Fact]
    public void An_agent_data_file_is_charged_to_the_node_budget_and_refused_with_the_path()
    {
        const string Path = "botcomponents/demo_agent.topic.Orders/data";
        var many = "kind: TaskDialog\n" + string.Concat(Enumerable.Range(0, 2000).Select(i => $"k{i}: 1\n"));
        using (var small = Comp(Agent(Base()), "demo_agent.topic.Orders", "9", FlowTool))
        {
            // The same budget is enough for the rest of the solution, so only the 2,000 keys can spend it.
            Assert.Single(Assert.Single(XmlUnpackReader.Read(small.Path, new ReadBudget(maxNodes: 1000)).Agents).Tools);
        }

        Refused(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", many), Path, maxNodes: 1000);
        Refused(Comp(Agent(Base()), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\n" + many.Replace("kind: TaskDialog\n", "")), "botcomponents/demo_agent.gpt.default/data", maxNodes: 1000);
    }

    [Fact]
    public void A_tool_takes_its_schema_name_from_the_attribute_and_its_file_from_the_folder()
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.topic.Folder", "9", FlowTool, xml: Component("demo_agent.topic.Named", "9")));

        var tool = Assert.Single(agent.Tools);
        Assert.Equal("demo_agent.topic.Named", tool.SchemaName);
        Assert.Equal("botcomponents/demo_agent.topic.Folder/data", tool.File);
    }

    [Fact]
    public void A_component_with_no_data_is_not_a_tool()
    {
        Assert.Empty(One(Comp(Agent(Base()), "demo_agent.topic.Orders", "9", null)).Tools);
    }

    [Fact]
    public void Other_component_types_are_not_read()
    {
        var agent = One(Comp(Comp(Agent(Base()), "demo_agent.trigger.A", "17", "kind: TaskDialog\naction:\n  kind: X\n"), "demo_agent.x", "0", "a: [b\n"));

        Assert.Empty(agent.Tools);
        Assert.Null(agent.Instructions);
    }

    [Fact]
    public void A_component_with_no_parent_or_a_parent_that_is_no_read_agent_is_not_read()
    {
        var t = Comp(Comp(Comp(Comp(Agent(Base()),
            "demo_agent.topic.A", "9", "kind: TaskDialog\naction:\n  kind: InvokeFlowTaskAction\n", parent: null),
            "demo_agent.topic.B", "9", "kind: TaskDialog\naction: [broken\n", parent: "demo_unknown"),
            "demo_agent.gpt.c", "15", "kind: GptComponentMetadata\ninstructions: [broken\n", parent: null),
            "demo_agent.topic.D", "9", "kind: TaskDialog\naction:\n  kind: InvokeFlowTaskAction\n", parent: "DEMO_AGENT");

        var agent = One(t);

        Assert.Equal(["demo_agent.topic.D"], agent.Tools.Select(x => x.SchemaName));
        Assert.Null(agent.Instructions);
    }

    [Fact]
    public void Components_go_to_the_agent_they_name()
    {
        using var t = Comp(Comp(Comp(Comp(Comp(Comp(Agent(Agent(Base()), "demo_other", Bot("demo_other"), Config),
            "demo_agent.topic.A", "9", FlowTool),
            "demo_other.topic.B", "9", FlowTool, parent: "demo_other"),
            "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\ninstructions: First agent.\n"),
            "demo_other.gpt.default", "15", "kind: GptComponentMetadata\ninstructions: Second agent.\n", parent: "demo_other"),
            "demo_agent.topic.Greeting", "9", Topic),
            "demo_other.topic.Greeting", "9", Topic, parent: "demo_other");

        foreach (var s in Both(t))
        {
            var agents = s.Agents;

            Assert.Equal(["demo_agent", "demo_other"], agents.Select(a => a.SchemaName));
            Assert.Equal(["demo_agent.topic.A"], agents[0].Tools.Select(x => x.SchemaName));
            Assert.Equal(["demo_other.topic.B"], agents[1].Tools.Select(x => x.SchemaName));
            Assert.Equal("First agent.", agents[0].Instructions);
            Assert.Equal("Second agent.", agents[1].Instructions);
        }
    }

    [Fact]
    public void The_schema_name_is_the_attribute_and_the_folder_only_when_there_is_none()
    {
        var named = One(Comp(Agent(Base(), "folder_x", Bot("demo_agent"), """{"recognizer":{"$kind":"CLIAgentRecognizer"}}"""), "demo_agent.topic.A", "9", FlowTool));
        var unnamed = One(Agent(Base(), "folder_y", "<bot><authenticationmode>1</authenticationmode></bot>"));

        Assert.Equal("demo_agent", named.SchemaName);
        Assert.Equal("bots/folder_x/bot.xml", named.File);
        Assert.Equal("CLIAgentRecognizer", named.Configuration!.Recognizer);
        Assert.Single(named.Tools);
        Assert.Equal("folder_y", unnamed.SchemaName);
    }

    [Fact]
    public void The_agent_mapping_file_still_gives_its_connection_references()
    {
        using var t = Full().Write("Assets/botcomponent_connectionreferenceset.xml", """
            <botcomponent_connectionreferenceset>
              <botcomponent_connectionreference botcomponentid.schemaname="demo_agent.topic.Orders" connectionreferenceid.connectionreferencelogicalname="DEMO_MAPPED" />
            </botcomponent_connectionreferenceset>
            """);

        Assert.All(Both(t), s =>
        {
            Assert.Equal(["DEMO_MAPPED"], s.AgentConnectionReferences);
            Assert.Single(s.Agents);
        });
    }

    [Fact]
    public void A_root_element_of_another_name_is_refused_with_the_path()
    {
        Refused(Agent(Base(), bot: "<bots/>"), "bots/demo_agent/bot.xml");
        Refused(Comp(Agent(Base()), "demo_agent.topic.A", "9", FlowTool, xml: "<x/>"), "botcomponents/demo_agent.topic.A/botcomponent.xml");
    }

    [Fact]
    public void Malformed_xml_json_and_yaml_are_refused_with_the_path()
    {
        Refused(Agent(Base(), bot: "<bot><oops>"), "bots/demo_agent/bot.xml");
        Refused(Agent(Base(), config: "{not json"), "bots/demo_agent/configuration.json");
        Refused(Agent(Base(), config: ""), "bots/demo_agent/configuration.json");
        Refused(Comp(Agent(Base()), "demo_agent.topic.A", "9", null, xml: "<botcomponent><oops>"), "botcomponents/demo_agent.topic.A/botcomponent.xml");
        Refused(Comp(Agent(Base()), "demo_agent.gpt.default", "15", "kind: [unclosed\n"), "botcomponents/demo_agent.gpt.default/data");
        Refused(Comp(Agent(Base()), "demo_agent.topic.A", "9", "kind: TaskDialog\naction: [unclosed\n"), "botcomponents/demo_agent.topic.A/data");
    }

    [Fact]
    public void Yaml_with_an_anchor_is_refused_with_the_path()
    {
        Refused(Comp(Agent(Base()), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\ninstructions: &a text\n"), "botcomponents/demo_agent.gpt.default/data");
        Refused(Comp(Agent(Base()), "demo_agent.topic.A", "9", "kind: TaskDialog\nx: &a 1\ny: *a\n"), "botcomponents/demo_agent.topic.A/data");
    }

    [Fact]
    public void An_unreadable_component_of_an_agent_that_is_not_read_is_still_refused_when_its_xml_is_malformed()
    {
        Refused(Comp(Agent(Base()), "demo_agent.topic.A", "9", null, parent: null, xml: "<botcomponent><oops>"), "botcomponents/demo_agent.topic.A/botcomponent.xml");
    }

    // ---- M51: the layout of Copilot Studio's new authoring UI ----

    private const string Cli = "CLICopilotRecognizer";

    private static string CliConfig(string agentSettings) => """{"$kind":"BotConfiguration","recognizer":{"$kind":"CLICopilotRecognizer"},"agentSettings":""" + agentSettings + "}";

    private static string Segments(params string[] values) =>
        """{"instructions":{"$kind":"Instructions","segments":[""" + string.Join(",", values.Select(v => "{\"$kind\":\"StaticSegment\",\"value\":" + System.Text.Json.JsonSerializer.Serialize(v) + "}")) + "]}}";

    private static AgentConfiguration? Configuration(string config) => One(Agent(Base(), config: config)).Configuration;

    [Theory]
    [InlineData("Be brief.", "Be brief.")]
    [InlineData("", "")]
    [InlineData("Line one.\nLine two.\n", "Line one.\nLine two.\n")]
    public void One_static_segment_is_the_instructions(string value, string expected) =>
        Assert.Equal(new AgentConfiguration(Cli, null, expected), Configuration(CliConfig(Segments(value))));

    [Fact]
    public void Several_static_segments_are_joined_with_no_separator()
    {
        Assert.Equal(new AgentConfiguration(Cli, null, "First.Second. Third"), Configuration(CliConfig(Segments("First.", "Second.", " Third"))));
    }

    [Fact]
    public void An_empty_segments_array_gives_empty_instructions()
    {
        Assert.Equal(new AgentConfiguration(Cli, null, ""), Configuration(CliConfig("""{"instructions":{"segments":[]}}""")));
    }

    [Theory]
    [InlineData("""{"$kind":"AgentSettings"}""")] // no instructions key
    [InlineData("""{"model":{"series":"x"}}""")]
    [InlineData("[]")] // agentSettings is not an object
    [InlineData("\"text\"")]
    [InlineData("null")]
    public void Without_an_agentSettings_object_holding_instructions_both_fields_keep_their_defaults(string agentSettings)
    {
        Assert.Equal(new AgentConfiguration(Cli, null), Configuration(CliConfig(agentSettings)));
        Assert.Equal(new AgentConfiguration(Cli, null), Configuration("""{"recognizer":{"$kind":"CLICopilotRecognizer"}}"""));
    }

    [Theory]
    [InlineData("""{"instructions":{}}""")] // no segments
    [InlineData("""{"instructions":{"segments":{"$kind":"StaticSegment","value":"x"}}}""")] // segments is an object
    [InlineData("""{"instructions":{"segments":"x"}}""")]
    [InlineData("""{"instructions":{"segments":["x"]}}""")] // an item is a string
    [InlineData("""{"instructions":{"segments":[{"value":"x"}]}}""")] // no $kind
    [InlineData("""{"instructions":{"segments":[{"$kind":1,"value":"x"}]}}""")] // $kind is a number
    [InlineData("""{"instructions":{"segments":[{"$kind":"StaticSegment","value":"x"},{"$kind":"ToolReference","value":"y"}]}}""")] // another $kind
    [InlineData("""{"instructions":{"segments":[{"$kind":"StaticSegment","value":7}]}}""")] // value is a number
    [InlineData("""{"instructions":{"segments":[{"$kind":"StaticSegment"}]}}""")] // no value
    [InlineData("""{"instructions":{"segments":[{"$kind":"StaticSegment","value":"\ud800"}]}}""")] // not valid UTF-16
    [InlineData("""{"instructions":"text"}""")]
    [InlineData("""{"instructions":null}""")]
    public void Instructions_in_a_shape_the_reader_does_not_know_are_unread(string agentSettings)
    {
        Assert.Equal(new AgentConfiguration(Cli, null, null, true), Configuration(CliConfig(agentSettings)));
    }

    [Fact]
    public void An_agent_with_no_custom_gpt_component_takes_its_instructions_from_configuration_json()
    {
        var agent = One(Agent(Base(), config: CliConfig(Segments("Be brief."))));

        Assert.Equal("Be brief.", agent.Instructions);
        Assert.Equal("bots/demo_agent/configuration.json", agent.InstructionsFile);
        Assert.True(agent.InstructionsFromConfiguration);
        Assert.False(agent.InstructionsConflict);
    }

    [Fact]
    public void Unread_or_absent_configuration_instructions_give_null_instructions_and_no_file()
    {
        foreach (var config in new[] { CliConfig("""{"instructions":{}}"""), CliConfig("{}"), Config })
        {
            var agent = One(Agent(Base(), config: config));

            Assert.Null(agent.Instructions);
            Assert.Null(agent.InstructionsFile);
            Assert.False(agent.InstructionsFromConfiguration);
            Assert.False(agent.InstructionsConflict);
        }

        Assert.Null(One(Agent(Base(), config: null)).Instructions);
    }

    [Theory]
    [InlineData("Answer questions about orders.")]
    [InlineData("Answer questions about orders.\r\n")]
    public void A_custom_gpt_component_beside_equal_segment_instructions_is_no_conflict(string segment)
    {
        var agent = One(Comp(Agent(Base(), config: CliConfig(Segments(segment))), "demo_agent.gpt.default", "15", Gpt));

        Assert.Equal("Answer questions about orders.", agent.Instructions);
        Assert.Equal("botcomponents/demo_agent.gpt.default/data", agent.InstructionsFile);
        Assert.False(agent.InstructionsFromConfiguration);
        Assert.False(agent.InstructionsConflict);
    }

    [Fact]
    public void A_block_scalar_and_crlf_line_breaks_are_no_conflict()
    {
        var agent = One(Comp(Agent(Base(), config: CliConfig(Segments("Line one.\r\nLine two."))), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\ninstructions: |\n  Line one.\n  Line two.\n"));

        Assert.Equal("Line one.\nLine two.\n", agent.Instructions);
        Assert.False(agent.InstructionsConflict);
    }

    [Theory]
    [InlineData("""{"instructions":{"segments":[{"$kind":"StaticSegment","value":"Other text."}]}}""")] // differs
    [InlineData("""{"instructions":{"segments":[{"$kind":"Other","value":"Answer questions about orders."}]}}""")] // unread
    [InlineData("""{"instructions":{}}""")]
    public void A_custom_gpt_component_beside_different_or_unread_segment_instructions_is_a_conflict_and_the_component_wins(string agentSettings)
    {
        var agent = One(Comp(Agent(Base(), config: CliConfig(agentSettings)), "demo_agent.gpt.default", "15", Gpt));

        Assert.Equal("Answer questions about orders.", agent.Instructions);
        Assert.Equal("botcomponents/demo_agent.gpt.default/data", agent.InstructionsFile);
        Assert.False(agent.InstructionsFromConfiguration);
        Assert.True(agent.InstructionsConflict);
    }

    [Theory]
    [InlineData("""{"$kind":"AgentSettings"}""")] // no instructions key
    [InlineData("[]")] // no agentSettings object
    public void A_custom_gpt_component_beside_configuration_without_instructions_is_no_conflict(string agentSettings)
    {
        Assert.False(One(Comp(Agent(Base(), config: CliConfig(agentSettings)), "demo_agent.gpt.default", "15", Gpt)).InstructionsConflict);
        Assert.False(One(Comp(Agent(Base()), "demo_agent.gpt.default", "15", Gpt)).InstructionsConflict);
        Assert.False(One(Comp(Agent(Base(), config: null), "demo_agent.gpt.default", "15", Gpt)).InstructionsConflict);
    }

    [Fact]
    public void Two_custom_gpt_components_beside_segment_instructions_give_null_and_no_conflict()
    {
        var agent = One(Comp(Comp(Agent(Base(), config: CliConfig(Segments("Text."))), "demo_agent.gpt.a", "15", Gpt), "demo_agent.gpt.b", "15", Gpt));

        Assert.Null(agent.Instructions);
        Assert.Null(agent.InstructionsFile);
        Assert.False(agent.InstructionsFromConfiguration);
        Assert.False(agent.InstructionsConflict);
    }

    [Fact]
    public void A_custom_gpt_component_with_instructions_that_are_not_text_gives_null_and_no_conflict()
    {
        var agent = One(Comp(Agent(Base(), config: CliConfig(Segments("Text."))), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\ninstructions:\n  nested: text\n"));

        Assert.Null(agent.Instructions);
        Assert.False(agent.InstructionsFromConfiguration);
        Assert.False(agent.InstructionsConflict);
    }

    [Fact]
    public void A_custom_gpt_component_with_no_instructions_key_beside_segment_text_gives_empty_instructions_and_a_conflict()
    {
        var agent = One(Comp(Agent(Base(), config: CliConfig(Segments("Text."))), "demo_agent.gpt.default", "15", "kind: GptComponentMetadata\n"));

        Assert.Equal("", agent.Instructions);
        Assert.True(agent.InstructionsConflict);
    }

    private const string Docs = "kind: McpTool\nauthMode: Invoker\nconnectionReference: demo_agent.cr.shared_docs.abc\nconnectorId: /providers/Microsoft.PowerApps/apis/shared_docs\noperationId: search\n";

    [Fact]
    public void An_mcptool_component_is_a_tool_with_its_mode_connection_reference_and_operation_id()
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.tool.Docs", "9", Docs));

        var tool = Assert.Single(agent.Tools);
        Assert.Equal(new AgentTool("demo_agent.tool.Docs", null, true, "Invoker", "botcomponents/demo_agent.tool.Docs/data", "demo_agent.cr.shared_docs.abc", "search", "McpTool"), tool);
        Assert.Empty(agent.OtherComponents);
        Assert.Empty(agent.UnreadFiles);
    }

    [Theory]
    [InlineData("kind: McpTool\n")]
    [InlineData("kind: McpTool\nauthMode:\n  a: b\nconnectionReference:\n  a: b\noperationId: \"\"\n")] // not text
    public void An_mcptool_with_no_readable_fields_is_a_tool_with_null_fields(string data)
    {
        var tool = Assert.Single(One(Comp(Agent(Base()), "demo_agent.tool.Docs", "9", data)).Tools);

        Assert.Equal(new AgentTool("demo_agent.tool.Docs", null, true, null, "botcomponents/demo_agent.tool.Docs/data", null, null, "McpTool"), tool);
    }

    [Fact]
    public void An_mcptool_whose_text_holds_taskdialog_is_still_read_by_its_yaml_kind()
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.tool.Docs", "9", "kind: \"McpTool\"\nauthMode: Maker\nnote: TaskDialog\n"));

        Assert.Equal(["demo_agent.tool.Docs"], agent.Tools.Select(t => t.SchemaName));
        Assert.Equal("McpTool", agent.Tools[0].Kind);
        Assert.Equal("Maker", agent.Tools[0].Mode);
    }

    [Fact]
    public void Taskdialog_and_mcptool_tools_of_one_agent_are_both_read_and_a_taskdialog_leaves_the_new_fields_null()
    {
        var agent = One(Comp(Comp(Agent(Base()), "demo_agent.tool.Docs", "9", Docs), "demo_agent.topic.Orders", "9", McpTool));

        Assert.Equal(["demo_agent.tool.Docs", "demo_agent.topic.Orders"], agent.Tools.Select(t => t.SchemaName));
        var task = agent.Tools[1];
        Assert.Equal("TaskDialog", task.Kind);
        Assert.Null(task.ConnectionReference);
        Assert.Null(task.OperationId);
        Assert.Equal("McpTool", agent.Tools[0].Kind);
    }

    [Fact]
    public void Malformed_mcptool_and_iqcapability_yaml_is_refused_with_the_path()
    {
        Refused(Comp(Agent(Base()), "demo_agent.tool.Docs", "9", "kind: McpTool\nauthMode: [unclosed\n"), "botcomponents/demo_agent.tool.Docs/data");
        Refused(Comp(Agent(Base()), "demo_agent.iq.mail", "9", "kind: IQCapability\ncapability: [unclosed\n"), "botcomponents/demo_agent.iq.mail/data");
    }

    [Theory]
    [InlineData("kind: McpTool\n\"\": x\n", "McpTool")]
    [InlineData("kind: McpTool\nnote: TaskDialog\n\"\": x\n", "McpTool")]
    [InlineData("kind: IQCapability\ncapability: mail\naccess: Write\n\"\": x\n", "IQCapability")]
    [InlineData("kind: IQCapability\nnote: TaskDialog\n\"\": x\n", "IQCapability")]
    public void Unmappable_mcptool_or_iqcapability_yaml_is_listed_as_its_kind_and_not_as_unread(string data, string kind)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.tool.Odd", "9", data));

        Assert.Empty(agent.Tools);
        Assert.Empty(agent.Capabilities);
        Assert.Empty(agent.UnreadFiles);
        Assert.Equal([new AgentComponent(kind, "botcomponents/demo_agent.tool.Odd/data")], agent.OtherComponents);
    }

    [Fact]
    public void Unmappable_yaml_of_another_kind_that_holds_taskdialog_is_still_unread()
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.tool.Odd", "9", "kind: AdaptiveDialog\nnote: TaskDialog\n\"\": x\n"));

        Assert.Equal(["botcomponents/demo_agent.tool.Odd/data"], agent.UnreadFiles);
        Assert.Empty(agent.OtherComponents);
    }

    private static TempFolder SixCapabilities(TempFolder t)
    {
        foreach (var name in new[] { "teams", "mail", "sites", "calendar", "people", "files" })
        {
            Comp(t, $"demo_agent.iq.{name}", "9", $"kind: IQCapability\ncapability: {name}\naccess: Write");
        }

        return t;
    }

    [Fact]
    public void Iqcapability_components_are_workloads_in_ordinal_order_of_their_file()
    {
        var agent = One(SixCapabilities(Agent(Base())));

        Assert.Equal(
            ["calendar", "files", "mail", "people", "sites", "teams"],
            agent.Capabilities.Select(c => c.Capability));
        Assert.Equal(new AgentCapability("demo_agent.iq.calendar", "calendar", "Write", "botcomponents/demo_agent.iq.calendar/data"), agent.Capabilities[0]);
        Assert.Empty(agent.OtherComponents);
        Assert.Empty(agent.Tools);
    }

    [Theory]
    [InlineData("kind: IQCapability\ncapability: mail\n")]
    [InlineData("kind: IQCapability\naccess: Write\n")]
    [InlineData("kind: IQCapability\ncapability: mail\naccess:\n  a: b\n")]
    [InlineData("kind: IQCapability\ncapability:\n  a: b\naccess: Write\n")]
    [InlineData("kind: IQCapability\ncapability: mail\naccess: \"\"\n")]
    public void An_iqcapability_without_text_capability_and_access_stays_in_other_components(string data)
    {
        var agent = One(Comp(Agent(Base()), "demo_agent.iq.mail", "9", data));

        Assert.Empty(agent.Capabilities);
        Assert.Equal([new AgentComponent("IQCapability", "botcomponents/demo_agent.iq.mail/data")], agent.OtherComponents);
    }
}