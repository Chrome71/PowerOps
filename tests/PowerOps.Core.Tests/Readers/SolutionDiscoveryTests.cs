using System.IO.Compression;

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
                "powerops-seed-yaml/v1/solutions/PowerOpsSeed/solution.yml",
                "powerops-seed-yaml/v2/solutions/PowerOpsSeed/solution.yml",
                "powerops-seed/v1",
                "powerops-seed/v2",
                "zips/ColumnLevelSecuritySampleSolution.zip",
                "zips/ColumnLevelSecuritySampleSolution_managed.zip",
                "zips/Contoso_sample_1_0_0_1_managed.zip",
                "zips/polymorphiclookupexamplesolution.zip",
            ],
            found);
    }

    [Fact]
    public void Yaml_root_returns_each_solution_yml_without_descending()
    {
        using var t = new TempFolder()
            .Write("repo/solutions/A/solution.yml", "x")
            .Write("repo/solutions/B/SOLUTION.YML", "x")
            .Write("repo/solutions/C/readme.md", "x")
            .Write("repo/nested/solutions/D/solution.yml", "x");

        var found = SolutionDiscovery.Find(t.Path).Select(p => Path.GetRelativePath(t.Path, p).Replace('\\', '/'));

        Assert.Equal(["repo/solutions/A/solution.yml", "repo/solutions/B/SOLUTION.YML"], found);
    }

    [Fact]
    public void Paths_below_a_yaml_root_find_nothing()
    {
        using var t = new TempFolder().Write("repo/solutions/A/solution.yml", "x");

        Assert.Empty(SolutionDiscovery.Find(Path.Combine(t.Path, "repo", "solutions")));
        Assert.Empty(SolutionDiscovery.Find(Path.Combine(t.Path, "repo", "solutions", "A")));
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

    [Fact]
    public void Results_are_sorted_by_forward_slash_path()
    {
        // '/' sorts before '0', but '\\' (a Windows separator) sorts after it.
        using var t = new TempFolder()
            .Write("a0/Other/Solution.xml", SolutionXml.Manifest())
            .Write("a/x/Other/Solution.xml", SolutionXml.Manifest());

        var found = SolutionDiscovery.Find(t.Path).Select(p => Path.GetRelativePath(t.Path, p).Replace('\\', '/'));

        Assert.Equal(["a/x", "a0"], found);
    }

    [Fact]
    public void Symlink_loops_do_not_hang_or_throw()
    {
        using var t = new TempFolder().Write("ok/Other/Solution.xml", SolutionXml.Manifest());
        Directory.CreateDirectory(Path.Combine(t.Path, "loop"));
        if (!XmlUnpackReaderTests.TrySymlink(Path.Combine(t.Path, "loop", "back"), t.Path))
        {
            Assert.Skip("Symbolic links are not available here.");
        }

        Assert.Equal([Path.Combine(t.Path, "ok")], SolutionDiscovery.Find(t.Path));
    }

    [Fact]
    public void Finds_upper_case_zip_and_skips_zips_without_a_solution_and_files_that_are_not_zips()
    {
        // A Git LFS pointer and an empty placeholder are named *.zip but do not start with the zip signature.
        using var t = new TempFolder().Write("lfs.zip", "version https://git-lfs.github.com/spec/v1\noid sha256:" + new string('a', 64) + "\nsize 12345\n").Write("empty.zip", "");
        CreateZip(Path.Combine(t.Path, "SOL.ZIP"), "solution.xml");
        CreateZip(Path.Combine(t.Path, "other.zip"), "readme.txt");

        Assert.Equal([Path.Combine(t.Path, "SOL.ZIP")], SolutionDiscovery.Find(t.Path));
    }

    [Fact]
    public void A_zip_that_starts_with_the_signature_and_cannot_be_opened_is_an_error_naming_it()
    {
        using var t = new TempFolder().Write("good/Other/Solution.xml", "x");
        var zip = Path.Combine(t.Path, "corrupt.zip");
        CreateZip(Path.Combine(t.Path, "whole.zip"), "solution.xml");
        File.WriteAllBytes(zip, File.ReadAllBytes(Path.Combine(t.Path, "whole.zip"))[..30]); // "PK", then the central directory is missing
        File.Delete(Path.Combine(t.Path, "whole.zip"));

        var e = Assert.Throws<InvalidDataException>(() => SolutionDiscovery.Find(t.Path));

        Assert.StartsWith(zip + ":", e.Message, StringComparison.Ordinal);
    }

    private static void CreateZip(string path, string entry)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        zip.CreateEntry(entry);
    }
}