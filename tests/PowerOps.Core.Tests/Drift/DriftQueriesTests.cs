using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Drift;

public class DriftQueriesTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static string Stored(string name) =>
        File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "drift", "queries", name)).TrimEnd('\n');

    [Fact]
    public void Solution_query_equals_the_stored_one() =>
        Assert.Equal(Stored("read2-solution.xml").Replace("PowerOpsSeed", "My_Sol2"), DriftQueries.Solution("My_Sol2"));

    [Fact]
    public void Id_queries_equal_the_stored_ones()
    {
        var id = Id.ToString("D");
        Assert.Equal(Stored("read3-components.xml").Replace("{solutionId}", id), DriftQueries.Components(Id));
        Assert.Equal(Stored("read4-flows.xml").Replace("{solutionId}", id), DriftQueries.Flows(Id));
        Assert.Equal(Stored("read5-variables.xml").Replace("{solutionId}", id), DriftQueries.Variables(Id));
        Assert.Equal(Stored("read6-references.xml").Replace("{solutionId}", id), DriftQueries.References(Id));
    }

    [Fact]
    public void Other_solutions_query_equals_the_stored_one()
    {
        var ids = new[] { Id, Guid.Parse("00000000-0000-0000-0000-000000000002") };
        var values = string.Concat(ids.Select(i => $"<value>{i:D}</value>"));

        Assert.Equal(Stored("read8-other-solutions.xml").Replace("{componentValues}", values).Replace("{solutionId}", Id.ToString("D")), DriftQueries.OtherSolutions(Id, ids));
    }

    [Fact]
    public void Layer_query_equals_the_stored_one() =>
        Assert.Equal(Stored("read7-layers.xml").Replace("{componentIds}", Id.ToString("D")).Replace("{componentName}", "CanvasApp"), DriftQueries.Layers(Id, "CanvasApp"));

    [Theory]
    [InlineData("")]
    [InlineData("a\"b")]
    [InlineData("Entity\n")]
    public void An_invalid_component_name_is_refused(string name) => Assert.Throws<ArgumentException>(() => DriftQueries.Layers(Id, name));

    [Theory]
    [InlineData(1, "Entity")]
    [InlineData(29, "Workflow")]
    [InlineData(300, "CanvasApp")]
    [InlineData(380, "EnvironmentVariableDefinition")]
    [InlineData(381, null)]
    [InlineData(10164, null)]
    [InlineData(0, null)]
    public void The_layer_names_are_the_captured_ones(int type, string? name) => Assert.Equal(name, DriftQueries.LayerName(type));

    [Fact]
    public void Connection_references_use_their_own_layer_name() => Assert.Equal("connectionreference", DriftQueries.ReferenceLayerName);

    [Theory]
    [InlineData("PowerOpsSeed\n")]
    [InlineData("a b")]
    [InlineData("x'y")]
    [InlineData("")]
    public void Invalid_names_are_refused(string name)
    {
        Assert.False(DriftQueries.IsValidName(name));
        Assert.Throws<ArgumentException>(() => DriftQueries.Solution(name));
    }

    [Fact]
    public void A_valid_name_is_accepted() => Assert.True(DriftQueries.IsValidName("My_Sol2"));
}