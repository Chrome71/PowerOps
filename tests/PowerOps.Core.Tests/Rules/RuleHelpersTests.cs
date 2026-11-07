using PowerOps.Core.Rules;

namespace PowerOps.Core.Tests.Rules;

public class RuleHelpersTests
{
    [Fact]
    public void CloudFlows_parses_a_solution_once_and_keeps_only_cloud_flows_with_clientdata()
    {
        var s = B.Empty() with { Workflows = [B.Flow(refs: ["demo_a"]), B.Flow(id: B.Guid(2), category: 0), B.Flow(id: B.Guid(3)) with { ClientData = null }] };

        var flow = Assert.Single(RuleHelpers.CloudFlows(s)).Flow;

        Assert.Equal("demo_a", Assert.Single(flow.ConnectionReferences).LogicalName);
        Assert.Same(flow, Assert.Single(RuleHelpers.CloudFlows(s)).Flow);
        Assert.NotSame(flow, Assert.Single(RuleHelpers.CloudFlows(s with { })).Flow);
    }

    [Fact]
    public void CloudFlows_names_the_flow_file_for_invalid_json_on_every_call()
    {
        var s = B.Empty() with { Workflows = [B.Flow() with { ClientData = "[]" }] };

        for (var i = 0; i < 2; i++)
        {
            var e = Assert.Throws<InvalidDataException>(() => RuleHelpers.CloudFlows(s).ToList());
            Assert.StartsWith("Workflows/Flow-11111111-1111-1111-1111-111111111111.json: ", e.Message);
        }
    }
}