using PowerOps.Core.Settings;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Settings;

public class FlowStateTests
{
    // The seed flow's id as the exported zip spells it (braces, upper case); pac prints it lower case without braces.
    private const string SeedId = "{4C8DD007-DFCB-F111-A05C-0022480697A8}";

    private static string Sample(string name) => File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", name));

    [Theory]
    [InlineData("list-cloud-flows-on.json", FlowState.On)]
    [InlineData("list-cloud-flows-off.json", FlowState.Off)]
    [InlineData("list-cloud-flows-suspended.json", FlowState.Suspended)]
    [InlineData("list-cloud-flows-missing.json", FlowState.Missing)]
    public void Read_maps_each_captured_sample(string sample, string expected) =>
        Assert.Equal(expected, FlowState.Read(Sample(sample), SeedId));

    [Theory]
    [InlineData("""{"value":[]}""")]
    [InlineData("""{"value":[{"workflowId":"11111111-1111-1111-1111-111111111111","stateCode":"Published"}]}""")]
    public void A_result_without_the_flow_is_missing(string json) =>
        Assert.Equal(FlowState.Missing, FlowState.Read(json, SeedId));

    [Theory]
    [InlineData("list-cloud-flows-bad-id.txt")]
    public void Pac_error_text_is_unreadable(string sample) =>
        Assert.Throws<InvalidDataException>(() => FlowState.Read(Sample(sample), SeedId));

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{"value":{}}""")]
    [InlineData("""{"value":[{"workflowId":"4c8dd007-dfcb-f111-a05c-0022480697a8"}]}""")]
    [InlineData("""{"value":[{"workflowId":"4c8dd007-dfcb-f111-a05c-0022480697a8","stateCode":"Started"}]}""")]
    [InlineData("""{"value":[{"workflowId":"4c8dd007-dfcb-f111-a05c-0022480697a8","stateCode":1}]}""")]
    public void Other_shapes_and_unknown_states_are_unreadable(string json) =>
        Assert.Throws<InvalidDataException>(() => FlowState.Read(json, SeedId));

    [Theory]
    [InlineData(FlowState.Missing, 1, false, null, FlowState.On)]
    [InlineData(FlowState.Missing, 0, false, null, FlowState.Off)]
    [InlineData(FlowState.Missing, null, false, null, FlowState.Off)]
    [InlineData(FlowState.Missing, 2, false, null, FlowState.Off)]
    [InlineData(FlowState.Off, 1, true, FlowState.Off, FlowState.Off)]
    [InlineData(FlowState.On, 0, true, FlowState.On, FlowState.On)]
    [InlineData(FlowState.Suspended, 1, true, FlowState.Suspended, FlowState.Suspended)]
    public void Plan_predicts_a_new_flow_from_its_export_state_and_keeps_an_existing_one(string before, int? export, bool exists, string? stateBefore, string expected)
    {
        var flow = FlowState.Plan(B.Flow(name: "Seed", state: export), before);

        Assert.Equal(new ApplyFlow("Seed", FlowState.Export(export), exists, stateBefore, expected, null), flow);
    }

    [Theory]
    [InlineData(1, FlowState.On)]
    [InlineData(0, FlowState.Off)]
    [InlineData(2, FlowState.Suspended)]
    [InlineData(null, FlowState.Off)]
    public void Export_names_the_state_in_the_zip(int? stateCode, string expected) =>
        Assert.Equal(expected, FlowState.Export(stateCode));

    [Fact]
    public void Cloud_flows_are_category_5_sorted_by_name()
    {
        var s = B.Empty() with { Workflows = [B.Flow(name: "b"), B.Flow(name: "classic", category: 0), B.Flow(name: "a")] };

        Assert.Equal(["a", "b"], FlowState.CloudFlows(s).Select(w => w.Name));
    }
}