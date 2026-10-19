using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

public class RepoRootTests
{
    [Fact]
    public void RepoRoot_contains_solution_file()
    {
        Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, "PowerOps.slnx")));
    }
}