using System.Text.RegularExpressions;

using PowerOps.Core.Diff;
using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Tests.Rules;

namespace PowerOps.Core.Tests.Output;

public class DiffReportTests
{
    private static DiffResult Many(int n) =>
        SolutionDiff.Compare(B.Empty(), B.Empty() with { ConnectionReferences = Enumerable.Range(0, n).Select(i => B.Ref($"demo_ref{i:D5}")).ToList() });

    private static string Markdown(DiffResult r, int? max = null)
    {
        var w = new StringWriter();
        if (max is { } m)
        {
            DiffReport.WriteMarkdown(r, w, m);
        }
        else
        {
            DiffReport.WriteMarkdown(r, w);
        }

        return w.ToString();
    }

    [Fact]
    public void Markdown_of_a_huge_diff_fits_in_a_pull_request_comment_and_says_how_many_changes_are_left_out()
    {
        var text = Markdown(Many(2000));

        Assert.True(text.Length <= DiffReport.MarkdownLimit, $"{text.Length} chars");
        var shown = Regex.Matches(text, @"^\| added ", RegexOptions.Multiline).Count;
        var omitted = int.Parse(Regex.Match(text, @"_(\d+) more changes not shown").Groups[1].Value);
        Assert.True(shown > 0 && omitted > 0);
        Assert.Equal(2000, shown + omitted);
        Assert.EndsWith("full list._\n", text);
    }

    [Fact]
    public void Markdown_cuts_at_a_whole_row()
    {
        var text = Markdown(Many(50), max: 1500);

        Assert.True(text.Length <= 1500);
        Assert.All(text.Split('\n').Where(l => l.StartsWith("| added", StringComparison.Ordinal)), l => Assert.EndsWith(" |", l));
        Assert.Contains("more changes not shown", text);
    }

    [Fact]
    public void Markdown_of_a_small_diff_has_no_truncation_note()
    {
        var text = Markdown(Many(3));

        Assert.DoesNotContain("not shown", text);
        Assert.Equal(3, Regex.Matches(text, @"^\| added ", RegexOptions.Multiline).Count);
    }

    [Fact]
    public void Markdown_escapes_values_from_the_solution()
    {
        var a = B.Empty();
        var b = B.Empty() with { ConnectionReferences = [B.Ref("x|<b>\n[l](u)")] };

        var text = Markdown(SolutionDiff.Compare(a, b));

        Assert.Contains(@"x\|\<b\> \[l\](u)", text);
    }

    [Fact]
    public void Text_without_changes_says_so_and_with_changes_lists_them()
    {
        var w = new StringWriter();
        DiffReport.WriteText(SolutionDiff.Compare(B.Empty(), B.Empty()), w);
        Assert.Equal("Demo 1.0.0.0 → Demo 1.0.0.0\nno changes\n", w.ToString());

        w = new StringWriter();
        DiffReport.WriteText(Many(1), w);
        Assert.Equal("Demo 1.0.0.0 → Demo 1.0.0.0\n  added connectionReference demo_ref00000 [risk: connection-reference]\n1 change (1 added, 0 removed, 0 changed)\n", w.ToString());
    }

    [Fact]
    public void Markdown_cells_are_cut_without_splitting_a_surrogate_pair()
    {
        var key = new string('a', 199) + "\U0001F600tail";

        var text = Markdown(SolutionDiff.Compare(B.Empty(), B.Empty() with { ConnectionReferences = [B.Ref(key)] }));

        Assert.Contains("| " + new string('a', 199) + "… |", text);
    }

    [Fact]
    public void Markdown_cells_stay_within_the_cap_after_escaping()
    {
        var key = new string('_', 300);

        var text = Markdown(SolutionDiff.Compare(B.Empty(), B.Empty() with { ConnectionReferences = [B.Ref(key)] }));

        var cell = text.Split('\n').Single(l => l.StartsWith("| added", StringComparison.Ordinal)).Split(" | ")[2];
        Assert.Equal(200 + 1, cell.Length); // 100 escaped underscores (200 chars) and the ellipsis
        Assert.DoesNotContain("\\…", cell);
    }
}