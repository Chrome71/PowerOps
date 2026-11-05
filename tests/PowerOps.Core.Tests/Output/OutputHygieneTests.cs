using PowerOps.Core.Diff;
using PowerOps.Core.Drift;
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
    public void Markdown_cells_lose_bidi_controls_and_line_breaks_but_keep_the_defusing_space()
    {
        Assert.Equal("ab c", MarkdownReport.Escape("a\u202Eb\u2028c"));
        Assert.Equal("a b", MarkdownReport.Escape("a\nb"));
        Assert.Equal("@" + Zwsp + "x", MarkdownReport.Escape("\u202E@x"));
    }

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
    public void Clean_drops_supplementary_plane_format_characters_and_keeps_joiners()
    {
        // U+E0041 and U+E007F are tag characters (category Cf, written as surrogate pairs).
        Assert.Equal("ab", TextReport.Clean("a" + char.ConvertFromUtf32(0xE0041) + "b" + char.ConvertFromUtf32(0xE007F)));
        Assert.Equal("\U0001F469\u200D\U0001F4BB", TextReport.Clean("\U0001F469\u200D\U0001F4BB"));
        Assert.Equal("\u0645\u06CC\u200C\u062E\u0648\u0627\u0647\u0645", TextReport.Clean("\u0645\u06CC\u200C\u062E\u0648\u0627\u0647\u0645"));
        Assert.Equal("ab c", TextReport.Clean("a\u202Eb\u2028c"));
        Assert.Equal("x\U0001F600y", TextReport.Clean("x\U0001F600y"));
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

    [Fact]
    public void Only_links_inside_the_solutions_own_files_get_a_note()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Creating links needs privileges on Windows.");
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("o.yml", "x: 1").Write("dir/inner.yml", "x: 1");
        const string Yml = "ImportExportXml:\n  SolutionManifest:\n    UniqueName: A\n    Version: 1.0.0.0\n    Managed: 0\n    Publisher:\n      UniqueName: demopub\n";
        folder.Write("solutions/A/solution.yml", Yml)
            .Write("solutions/A/solutioncomponents.yml", "SolutionComponents:\n  Component:\n  - '@path': /entities\n  - '@path': /entities\n")
            .Write("publishers/demopub/publisher.yml", "Publisher:\n  UniqueName: demopub\n  CustomizationPrefix: demo\n")
            .Write("entities/a/entity.yml", "x: 1").Write("README.md", "readme").Write(".github/x.yml", "x: 1");
        File.CreateSymbolicLink(Path.Combine(folder.Path, "CLAUDE.md"), Path.Combine(folder.Path, "README.md"));
        Directory.CreateSymbolicLink(Path.Combine(folder.Path, ".github", "etc-link"), Path.Combine(outside.Path, "dir"));
        File.CreateSymbolicLink(Path.Combine(folder.Path, "entities", "a", "inside.yml"), Path.Combine(outside.Path, "o.yml"));
        var context = new LoadContext();

        SolutionLoader.Load(Path.Combine(folder.Path, "solutions", "A", "solution.yml"), context);

        Assert.Equal([folder.Path.Replace('\\', '/') + "/entities/a/inside.yml"], context.SkippedLinks);
    }

    [Fact]
    public void Notes_are_capped_at_20_then_a_count_follows()
    {
        var links = Enumerable.Range(1, 21).Select(i => $"/r/link{i}");
        var error = new StringWriter();

        TextReport.WriteNotes(links, error);

        var lines = error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToList();
        Assert.Equal(21, lines.Count);
        Assert.All(lines.Take(20), l => Assert.StartsWith("note: /r/link", l));
        Assert.Equal("note: and 1 more links, not followed and not read", lines[20]);
    }

    [Fact]
    public void Twenty_notes_have_no_count_line()
    {
        var error = new StringWriter();

        TextReport.WriteNotes(Enumerable.Range(1, 20).Select(i => $"/r/link{i}"), error);

        Assert.Equal(20, error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    private static AnalysisResult WithNotes(params string[] notes) =>
        new([new SolutionResult("a/b", B.Empty(), []) { NotChecked = notes }]);

    [Fact]
    public void A_not_checked_note_is_one_cleaned_line()
    {
        var error = new StringWriter();

        TextReport.WriteNotChecked(WithNotes("PO023 not checked for table permission 'x\n::error::forged': web role 'r' is not in the solution."), error);

        var line = Assert.Single(error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("note: a/b: PO023 not checked for table permission 'x ::error::forged'", line);
    }

    [Fact]
    public void Not_checked_notes_are_capped_at_20_then_a_count_follows()
    {
        var error = new StringWriter();

        TextReport.WriteNotChecked(WithNotes([.. Enumerable.Range(1, 20).Select(i => $"PO022 check {i}"), .. Enumerable.Range(1, 5).Select(i => $"PO023 check {i}"), .. Enumerable.Range(21, 5).Select(i => $"PO022 check {i}")]), error);

        var lines = error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToList();
        Assert.Equal(21, lines.Count);
        Assert.All(lines.Take(20), l => Assert.StartsWith("note: a/b: PO022 check", l));
        Assert.Equal("note: and 10 more checks not done (PO022, PO023)", lines[20]);
    }

    [Fact]
    public void Unread_file_notes_past_the_cap_are_counted_under_the_rule_id_alone()
    {
        var error = new StringWriter();
        var solution = B.Empty() with { Agents = [B.Agent(unread: Enumerable.Range(1, 25).Select(i => $"botcomponents/demo_agent.topic.T{i}/data"))] };

        TextReport.WriteNotChecked(WithNotes([.. new PO027McpToolWithoutGenerativeOrchestration().NotChecked(solution)]), error);

        var lines = error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToList();
        Assert.Equal(21, lines.Count);
        Assert.Equal("note: and 5 more checks not done (PO027)", lines[20]);
    }

    [Fact]
    public void A_not_compared_line_is_one_cleaned_note_and_more_than_20_are_counted()
    {
        var error = new StringWriter();

        TextReport.WriteNotCompared(["agent 'x\n::error::forged\u202E': instructions not compared", .. Enumerable.Range(1, 21).Select(i => $"agent 'a{i}': instructions not compared")], error);

        var lines = error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToList();
        Assert.Equal(21, lines.Count);
        Assert.Equal("note: agent 'x ::error::forged': instructions not compared", lines[0]);
        Assert.Equal("note: and 2 more not compared (--format json lists them all)", lines[20]);
    }

    [Fact]
    public void Not_compared_lines_in_markdown_are_escaped_and_stay_one_line()
    {
        var diff = new StringWriter();
        DiffReport.WriteMarkdown(SolutionDiff.Compare(B.Empty(), B.Empty()) with { NotCompared = ["agent '@x\n<b>|#1': instructions not compared"] }, diff);
        var drift = new StringWriter();
        Snapshot[] envs = [new("Dev", "demo", true, "1.0.0.0", false, true, [], [], [], []), new("T\n<b>@x", "demo", true, "1.0.0.0", true, false, [], [], [], [])];
        DriftReport.WriteMarkdown("demo", envs, ["live", "live"], [], drift);

        Assert.Contains("- agent '@\u200Bx \\<b\\>\\|#\u200B1': instructions not compared\n", diff.ToString());
        Assert.Contains("- 2 T \\<b\\>@\u200Bx: layers not read, so unmanaged layers there are not checked\n", drift.ToString());
    }

    [Fact]
    public void Twenty_not_checked_notes_have_no_count_line()
    {
        var error = new StringWriter();

        TextReport.WriteNotChecked(WithNotes([.. Enumerable.Range(1, 20).Select(i => $"PO022 check {i}")]), error);

        Assert.Equal(20, error.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    // M61: opt-in redaction of URLs and of diff's default values.
    private const string Sentinel = "https://m61sentinel.crm.dynamics.com";

    private static AnalysisResult Analysis(bool redact)
    {
        var s = B.Empty() with
        {
            Workflows = [B.Flow(literals: [Sentinel])],
            CanvasFormulas = [B.Formula($"=Launch(\"{Sentinel}\")")],
        };
        var result = new AnalysisResult([new SolutionResult("a/b", s, [.. Analyzer.Run(s)]) { NotChecked = [$"PO022 not checked at {Sentinel}/x"] }]);
        return redact ? Redaction.Apply(result) : result;
    }

    [Theory]
    [InlineData("hard-codes the URL 'https://contoso.sharepoint.com/sites/ops'.", "hard-codes the URL '[redacted URL]'.")]
    [InlineData("see https://a.example/p/q?x=1&y=2 now", "see [redacted URL] now")]
    [InlineData("https://a.example and http://b.example/c", "[redacted URL] and [redacted URL]")]
    [InlineData("file:///etc/passwd", "[redacted URL]")]
    [InlineData("HTTPS://A.EXAMPLE/x", "[redacted URL]")]
    [InlineData("no url here, only host.example and a:b", "no url here, only host.example and a:b")]
    public void Redaction_text_replaces_every_url(string text, string expected) => Assert.Equal(expected, Redaction.Text(text));

    [Fact]
    public void Redaction_text_returns_a_long_run_of_letters_unchanged_and_fast()
    {
        var text = new string('a', 100_000);

        Assert.Equal(text, Redaction.Text(text));
    }

    [Fact]
    public void Redaction_writer_redacts_strings_and_passes_characters_on()
    {
        var inner = new StringWriter();
        using var writer = new Redaction.Writer(inner);

        writer.WriteLine($"note: {Sentinel}/a");
        writer.Write($"x {Sentinel}");
        writer.Write('!');
        writer.Write($"c {Sentinel}".ToCharArray(), 0, Sentinel.Length + 2);
        writer.Write($"s {Sentinel}".AsSpan());

        Assert.Equal("note: [redacted URL]" + inner.NewLine + "x [redacted URL]!c [redacted URL]s [redacted URL]", inner.ToString());
    }

    [Fact]
    public void Redaction_apply_changes_messages_components_and_notes_only()
    {
        var plain = Analysis(redact: false);
        var redacted = Redaction.Apply(plain);

        var (a, b) = (plain.Solutions.Single(), redacted.Solutions.Single());
        Assert.Equal(a.Findings.Count, b.Findings.Count);
        Assert.All(a.Findings, f => Assert.Contains("m61sentinel", f.Message + f.Component, StringComparison.Ordinal));
        Assert.All(b.Findings, f => Assert.DoesNotContain("m61sentinel", f.Message + f.Component + f.File, StringComparison.Ordinal));
        Assert.Equal(a.Findings.Select(f => (f.RuleId, f.Severity, f.File)), b.Findings.Select(f => (f.RuleId, f.Severity, f.File)));
        Assert.Equal(["PO022 not checked at [redacted URL]"], b.NotChecked);
        Assert.Equal(a.Path, b.Path);
        Assert.Same(a.Solution, b.Solution);
        Assert.Equal(plain.Errors + plain.Warnings, redacted.Errors + redacted.Warnings);
    }

    [Fact]
    public void Every_analyze_writer_drops_the_sentinel_with_redaction_and_shows_it_without()
    {
        var writers = new (string Name, Action<AnalysisResult, TextWriter> Write)[]
        {
            ("text", TextReport.Write),
            ("json", JsonReport.Write),
            ("sarif", (r, w) => SarifReport.Write(r, w, "1.2.3")),
            ("markdown", MarkdownReport.Write),
        };

        foreach (var (name, write) in writers)
        {
            var plain = new StringWriter();
            var redacted = new StringWriter();
            write(Analysis(redact: false), plain);
            write(Analysis(redact: true), redacted);

            Assert.True(plain.ToString().Contains("m61sentinel", StringComparison.Ordinal), name);
            Assert.False(redacted.ToString().Contains("m61sentinel", StringComparison.Ordinal), name);
            Assert.True(redacted.ToString().Contains("redacted URL", StringComparison.Ordinal), name); // the Markdown writer escapes the brackets
        }
    }

    [Fact]
    public void Help_links_stay_in_json_and_sarif_with_redaction()
    {
        var json = new StringWriter();
        var sarif = new StringWriter();
        JsonReport.Write(Analysis(redact: true), json);
        SarifReport.Write(Analysis(redact: true), sarif, "1.2.3");

        Assert.Contains("\"helpUri\"", json.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"helpUri\"", sarif.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"informationUri\"", sarif.ToString(), StringComparison.Ordinal);
        Assert.Contains(SarifReport.Repo, sarif.ToString(), StringComparison.Ordinal);
    }

    private static (Solution Old, Solution New) SentinelPair() => (
        B.Empty() with { EnvironmentVariables = [B.Env("demo_v", def: "m61sentinel-old")], Workflows = [B.Flow(name: Sentinel)] },
        B.Empty() with { EnvironmentVariables = [B.Env("demo_v", def: "m61sentinel-new")], Workflows = [B.Flow(name: Sentinel, state: 0)] });

    [Fact]
    public void Every_diff_writer_drops_the_sentinel_with_redaction_and_shows_it_without()
    {
        var (old, @new) = SentinelPair();
        var writers = new (string Name, Action<DiffResult, TextWriter> Write)[]
        {
            ("text", DiffReport.WriteText),
            ("json", DiffReport.WriteJson),
            ("markdown", (r, w) => DiffReport.WriteMarkdown(r, w)),
        };

        foreach (var (name, write) in writers)
        {
            var plain = new StringWriter();
            var redacted = new StringWriter();
            write(SolutionDiff.Compare(old, @new), plain);
            write(SolutionDiff.Compare(old, @new, redact: true), redacted);

            Assert.True(plain.ToString().Contains("m61sentinel", StringComparison.Ordinal), name);
            Assert.False(redacted.ToString().Contains("m61sentinel", StringComparison.Ordinal), name);
            Assert.True(redacted.ToString().Contains("defaultValue changed", StringComparison.Ordinal), name);
        }
    }

    [Fact]
    public void Diff_redaction_also_covers_the_not_compared_lines_it_finds_itself()
    {
        var agent = B.Agent(name: Sentinel, instructions: null);

        var plain = SolutionDiff.Compare(B.Empty() with { Agents = [agent] }, B.Empty() with { Agents = [agent] });
        var redacted = SolutionDiff.Compare(B.Empty() with { Agents = [agent] }, B.Empty() with { Agents = [agent] }, redact: true);

        Assert.Contains(plain.NotCompared, l => l.Contains("m61sentinel", StringComparison.Ordinal));
        Assert.NotEmpty(redacted.NotCompared);
        Assert.DoesNotContain(redacted.NotCompared, l => l.Contains("m61sentinel", StringComparison.Ordinal));
    }

    [Fact]
    public void The_stderr_note_writers_drop_the_sentinel_through_the_redacting_writer_and_show_it_without()
    {
        var result = Analysis(redact: false);
        var plain = new StringWriter();
        var inner = new StringWriter();
        using var redacting = new Redaction.Writer(inner);
        foreach (var error in new TextWriter[] { plain, redacting })
        {
            TextReport.WriteNotes([$"{Sentinel}/link"], error);
            TextReport.WriteNotChecked(result, error);
            TextReport.WriteNotCompared([$"agent '{Sentinel}': not compared"], error);
        }

        Assert.True(plain.ToString().Contains("m61sentinel", StringComparison.Ordinal));
        Assert.False(inner.ToString().Contains("m61sentinel", StringComparison.Ordinal));
        Assert.Equal(3, inner.ToString().Split("[redacted URL]").Length - 1);
    }
}