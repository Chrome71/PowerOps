using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;
using PowerOps.Core.Rules;
using PowerOps.Core.Tests.Readers;
using PowerOps.Core.Tests.Rules;

namespace PowerOps.Core.Tests.Output;

// M30: A-8 (Markdown cells defuse @, # and ://), R-14 (Clean strips format characters and line separators), R-12 (links are listed).
public class OutputHygieneTests
{
    private const string Zwsp = "\u200B";

    [Theory]
    [InlineData("@octocat", "@" + Zwsp + "octocat")]
    [InlineData("fixes #123", "fixes #" + Zwsp + "123")]
    [InlineData("see https://contoso.example/x", "see https:/" + Zwsp + "/contoso.example/x")]
    [InlineData("a:b and :/ and /", "a:b and :/ and /")]
    [InlineData("x|y", "x\\|y")]
    public void Markdown_cells_defuse_mentions_references_and_links(string text, string expected) => Assert.Equal(expected, MarkdownReport.Escape(text));

    [Fact]
    public void Markdown_report_keeps_the_zero_width_space_and_the_text_report_has_none()
    {
        var finding = new Finding("PO011", Severity.Warning, "owner @octocat, see #12 and https://x.example", "Workflows/a@b.json", null);
        var result = new AnalysisResult([new SolutionResult("a/b", B.Empty() with { UniqueName = "Demo@team" }, [finding])]);
        var markdown = new StringWriter();
        var text = new StringWriter();

        MarkdownReport.Write(result, markdown);
        TextReport.Write(result, text);

        var md = markdown.ToString();
        Assert.Contains("@" + Zwsp + "octocat", md, StringComparison.Ordinal);
        Assert.Contains("#" + Zwsp + "12", md, StringComparison.Ordinal);
        Assert.Contains("https:/" + Zwsp + "/x.example", md, StringComparison.Ordinal);
        Assert.Contains("Demo@" + Zwsp + "team", md, StringComparison.Ordinal);
        Assert.DoesNotContain(Zwsp, text.ToString(), StringComparison.Ordinal); // culture-sensitive search ignores the character
        Assert.Contains("owner @octocat, see #12 and https://x.example", text.ToString());
    }

    [Fact]
    public void Clean_drops_format_characters_and_turns_line_separators_into_spaces()
    {
        // U+202E right-to-left override, U+200B zero width space, U+2028 line separator, U+2029 paragraph separator, U+0007 bell.
        Assert.Equal("ab c d e", TextReport.Clean("a\u202Eb\u2028c\u2029d\u200B\u0007e"));
        Assert.Equal("Contoso Dev: ok", TextReport.Clean("Contoso\u202E Dev\u200B: ok"));
    }

    [Fact]
    public void Links_inside_a_folder_are_listed_not_followed_and_not_counted_as_files()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Creating links needs privileges on Windows.");
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("o.xml", "<a/>").Write("dir/inner.xml", "<a/>");
        folder.Write("Other/Solution.xml", SolutionXml.Manifest()).Write("bin/x.txt", "x");
        File.CreateSymbolicLink(Path.Combine(folder.Path, "Other", "Customizations.xml"), Path.Combine(outside.Path, "o.xml"));
        Directory.CreateSymbolicLink(Path.Combine(folder.Path, "linked"), Path.Combine(outside.Path, "dir"));
        File.CreateSymbolicLink(Path.Combine(folder.Path, "bin", "skipped-folder-link"), Path.Combine(outside.Path, "o.xml"));
        var links = new List<string>();

        var files = SolutionDiscovery.EnumerateFiles(folder.Path, links).Select(f => Path.GetRelativePath(folder.Path, f).Replace('\\', '/')).ToList();

        Assert.Equal(["Other/Solution.xml"], files);
        Assert.Equal(["Other/Customizations.xml", "linked"], links.Select(l => Path.GetRelativePath(folder.Path, l).Replace('\\', '/')).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void The_load_context_collects_a_note_path_for_each_skipped_link_of_an_unpacked_folder()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Creating links needs privileges on Windows.");
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("o.xml", SolutionXml.Customizations(""));
        folder.Write("Other/Solution.xml", SolutionXml.Manifest());
        File.CreateSymbolicLink(Path.Combine(folder.Path, "Other", "Customizations.xml"), Path.Combine(outside.Path, "o.xml"));
        var context = new LoadContext();

        SolutionLoader.Load(folder.Path, context);

        Assert.Equal([folder.Path.Replace('\\', '/') + "/Other/Customizations.xml"], context.SkippedLinks);
    }

    [Fact]
    public void The_load_context_collects_a_link_inside_a_yaml_root_once_for_all_its_solutions()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Creating links needs privileges on Windows.");
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("o.yml", "x: 1");
        const string Yml = "ImportExportXml:\n  SolutionManifest:\n    UniqueName: {0}\n    Version: 1.0.0.0\n    Managed: 0\n    Publisher:\n      UniqueName: demopub\n";
        folder.Write("solutions/A/solution.yml", string.Format(Yml, "A")).Write("solutions/B/solution.yml", string.Format(Yml, "B"))
            .Write("solutions/A/solutioncomponents.yml", "SolutionComponents:\n").Write("solutions/B/solutioncomponents.yml", "SolutionComponents:\n")
            .Write("publishers/demopub/publisher.yml", "Publisher:\n  UniqueName: demopub\n  CustomizationPrefix: demo\n");
        File.CreateSymbolicLink(Path.Combine(folder.Path, "solutions", "A", "extra.yml"), Path.Combine(outside.Path, "o.yml"));
        var context = new LoadContext();

        SolutionLoader.Load(Path.Combine(folder.Path, "solutions", "A", "solution.yml"), context);
        SolutionLoader.Load(Path.Combine(folder.Path, "solutions", "B", "solution.yml"), context);

        Assert.Equal([folder.Path.Replace('\\', '/') + "/solutions/A/extra.yml"], context.SkippedLinks);
    }
}