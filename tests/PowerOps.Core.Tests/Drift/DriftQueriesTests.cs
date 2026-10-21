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