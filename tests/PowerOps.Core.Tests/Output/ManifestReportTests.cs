using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Core.Tests.Output;

public class ManifestReportTests
{
    private static readonly string A = new('a', 64);
    private static readonly string B = new('b', 64);
    private static readonly string D = new('d', 64);

    private static SolutionManifest Folder(params string[] paths) =>
        new("folder", [.. paths.Select(p => new ManifestEntry(p, A))], D);

    private static string Text(string display, SolutionManifest m)
    {
        var w = new StringWriter();
        ManifestReport.WriteText(w, display, m);
        return w.ToString();
    }

    [Fact]
    public void One_file_is_singular_and_none_is_plural()
    {
        Assert.Equal($"v1: folder, 1 file, digest {D}\n{A}  a.xml\n", Text("v1", Folder("a.xml")));
        Assert.Equal($"v1: folder, 0 files, digest {D}\n", Text("v1", Folder()));
    }

    [Fact]
    public void A_zip_prints_one_line()
    {
        Assert.Equal($"Demo.zip: zip, digest {D}\n", Text("Demo.zip", new SolutionManifest("zip", [], D)));
    }

    [Fact]
    public void A_comparison_prints_both_first_lines_then_added_removed_changed_then_the_same_count()
    {
        var w = new StringWriter();

        ManifestReport.WriteComparison(w, "old", Folder("a", "b", "c"), "new", Folder("a", "b"), new ManifestComparison(["z1", "z2"], ["r"], ["c1", "c2"], 1));

        Assert.Equal(
            $"old: folder, 3 files, digest {D}\nnew: folder, 2 files, digest {D}\nadded z1\nadded z2\nremoved r\nchanged c1\nchanged c2\n1 file the same\n",
            w.ToString());
    }

    [Fact]
    public void A_comparison_of_nothing_says_how_many_are_the_same()
    {
        var w = new StringWriter();

        ManifestReport.WriteComparison(w, "old", Folder("a", "b"), "new", Folder("a", "b"), new ManifestComparison([], [], [], 2));

        Assert.EndsWith("2 files the same\n", w.ToString());
    }

    [Fact]
    public void Paths_and_displays_are_cleaned_so_they_cannot_start_a_line()
    {
        var text = Text("a\nb", Folder("x\u2028::error::y.xml"));

        Assert.Equal($"a b: folder, 1 file, digest {D}\n{A}  x ::error::y.xml\n", text);
        var w = new StringWriter();
        ManifestReport.WriteComparison(w, "o", Folder(), "n", Folder(), new ManifestComparison(["p\nq"], [], [B], 0));
        Assert.Contains("added p q\n", w.ToString());
    }
}