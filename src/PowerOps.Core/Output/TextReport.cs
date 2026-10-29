using System.Globalization;
using System.Text;

using PowerOps.Core.Rules;

namespace PowerOps.Core.Output;

public static class TextReport
{
    /// <summary>Writes the human-readable report with "\n" line endings on every OS.</summary>
    public static void Write(AnalysisResult result, TextWriter writer)
    {
        foreach (var s in result.Solutions)
        {
            var sol = s.Solution;
            writer.Write($"{Clean(s.Path)}: {Clean(sol.UniqueName)} {Clean(sol.Version)} (publisher {Clean(sol.Publisher.UniqueName)}, prefix {Clean(sol.Publisher.Prefix)})\n");
            foreach (var required in s.Requires)
            {
                writer.Write($"  requires {Clean(required)}\n");
            }

            foreach (var f in s.Findings)
            {
                var severity = f.Severity == Severity.Error ? "error" : "warning";
                writer.Write($"  {severity} {f.RuleId} {Clean(f.File)}: {Clean(f.Message)}\n");
            }

            if (s.Findings.Count == 0)
            {
                writer.Write("  no findings\n");
            }
        }

        writer.Write(Summary(result) + "\n");
    }

    private const int MaxNotes = 20;

    /// <summary>One "note:" line on <paramref name="error"/> for each link a load skipped, so a check does not pass with less read than the user thinks. At most 20; a last line counts the rest.</summary>
    public static void WriteNotes(IEnumerable<string> skippedLinks, TextWriter error)
    {
        var links = skippedLinks.ToList();
        foreach (var link in links.Take(MaxNotes))
        {
            error.WriteLine($"note: {Clean(link)}: a link inside the solution, not followed and not read");
        }

        if (links.Count > MaxNotes)
        {
            error.WriteLine($"note: and {links.Count - MaxNotes} more links, not followed and not read");
        }
    }

    internal static string Summary(AnalysisResult result) =>
        $"{Count(result.Solutions.Count, "solution")}, {Count(result.Errors, "error")}, {Count(result.Warnings, "warning")}{(result.Baselined > 0 ? $", {result.Baselined} baselined" : "")}";

    // A hostile solution must not forge lines (or "::error::" workflow commands) or reorder the text (bidi controls) in the report.
    // Walks code points, so characters outside the BMP (the tag characters U+E0000..) are judged whole. Control characters and the line and
    // paragraph separators become spaces; format characters (category Cf) are dropped, except the joiners U+200C and U+200D, which emoji sequences and Persian words need.
    public static string Clean(string? text)
    {
        var result = new StringBuilder();
        foreach (var rune in (text ?? "").EnumerateRunes())
        {
            switch (Rune.GetUnicodeCategory(rune))
            {
                case UnicodeCategory.Control or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator:
                    result.Append(' ');
                    break;
                case UnicodeCategory.Format when rune.Value is not (0x200C or 0x200D):
                    break;
                default:
                    result.Append(rune.ToString());
                    break;
            }
        }

        return result.ToString();
    }

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}