using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

public class FixtureTests
{
    private static Solution Load(string zip) => SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "agents", "new-ui", zip));

    [Fact]
    public void The_new_ui_agent_fixture_reads_its_segment_instructions_tools_and_work_iq_workloads()
    {
        const string Agent = "cr123_loanhelper_abc123";
        const string Learn = Agent + ".tool.MicrosoftLearnDocsMCP-MicrosoftLearnDocsMCPServer_Liw";
        const string Work = "cr123_draft_def456.tool.WorkIQMCPServer_mVi";

        var agent = Assert.Single(Load("v1.zip").Agents);

        Assert.Equal(Agent, agent.SchemaName);
        Assert.Equal("CLICopilotRecognizer", agent.Configuration!.Recognizer);
        Assert.StartsWith("You help IT staff answer questions about lending equipment", agent.Instructions);
        Assert.Equal($"bots/{Agent}/configuration.json", agent.InstructionsFile);
        Assert.True(agent.InstructionsFromConfiguration);
        Assert.False(agent.InstructionsConflict);
        Assert.Equal(
            [
                new AgentTool(Work, null, true, "Invoker", $"botcomponents/{Work}/data", "cr123_draft_def456.cr.shared_workiqmcp.shared-workiqmcp-00000000-0000-0000-0000-000000000002", "InvokeWorkIQMCP", "McpTool"),
                new AgentTool(Learn, null, true, "Invoker", $"botcomponents/{Learn}/data", "cr123_loanhelper_abc123.cr.shared_microsoftlearndocsmcpserver.00000000000000000000000000000001", "microsoft_docs_search", "McpTool"),
            ],
            agent.Tools.OrderBy(t => t.SchemaName, StringComparer.Ordinal));
        Assert.Equal(
            ["calendar", "files", "mail", "people", "sites", "teams"],
            agent.Capabilities.Select(c => c.Capability));
        Assert.All(agent.Capabilities, c => Assert.Equal("Write", c.Access));
        Assert.Empty(agent.OtherComponents);
        Assert.Empty(agent.UnreadFiles);
    }

    [Fact]
    public void The_changed_fixture_differs_only_in_its_instructions_and_the_third_lacks_the_learn_tool()
    {
        var v1 = Assert.Single(Load("v1.zip").Agents);
        var v2 = Assert.Single(Load("v2.zip").Agents);
        var noLearn = Assert.Single(Load("v1-no-learn-tool.zip").Agents);

        Assert.NotEqual(v1.Instructions, v2.Instructions);
        Assert.Equal(v1.Tools, v2.Tools);
        Assert.Equal(v1.Capabilities, v2.Capabilities);
        Assert.Equal(v1.Instructions, noLearn.Instructions);
        Assert.Equal(["cr123_draft_def456.tool.WorkIQMCPServer_mVi"], noLearn.Tools.Select(t => t.SchemaName));
    }
}