using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class SolutionDiscoveryTests
{
    [Fact]
    public void Finds_fixture_folders_and_zips_sorted()
    {
        var root = Path.Combine(Golden.RepoRoot, "tests", "fixtures");

        var found = SolutionDiscovery.Find(root).Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToList();

        Assert.Equal(
            [
                "alm-makers",
                "alm-sample",
                "coe-auditlogs",
                "zips/ColumnLevelSecuritySampleSolution.zip",
                "zips/ColumnLevelSecuritySampleSolution_managed.zip",
                "zips/Contoso_sample_1_0_0_1_managed.zip",
                "zips/polymorphiclookupexamplesolution.zip",
            ],
            found);
    }

    [Fact]
    public void Solution_folder_returns_itself_without_descending()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("nested/Other/Solution.xml", SolutionXml.Manifest());

        Assert.Equal([t.Path], SolutionDiscovery.Find(t.Path));
    }

    [Fact]
    public void Skips_bin_obj_git_and_node_modules()
    {
        using var t = new TempFolder()
            .Write("bin/a/Other/Solution.xml", SolutionXml.Manifest())
            .Write("obj/a/Other/Solution.xml", SolutionXml.Manifest())
            .Write(".git/a/Other/Solution.xml", SolutionXml.Manifest())
            .Write("node_modules/a/Other/Solution.xml", SolutionXml.Manifest())
            .Write("ok/Other/Solution.xml", SolutionXml.Manifest());

        var found = SolutionDiscovery.Find(t.Path);

        Assert.Equal([Path.Combine(t.Path, "ok")], found);
    }

    [Fact]
    public void Zip_path_is_returned_as_is_and_missing_path_gives_nothing()
    {
        Assert.Equal(["whatever.zip"], SolutionDiscovery.Find("whatever.zip"));
        Assert.Empty(SolutionDiscovery.Find(Path.Combine(Path.GetTempPath(), "powerops-does-not-exist")));
    }
}