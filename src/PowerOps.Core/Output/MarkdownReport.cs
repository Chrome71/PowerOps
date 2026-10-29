using System.Text;

using PowerOps.Core.Rules;

namespace PowerOps.Core.Output;

public static class MarkdownReport
{
    /// <summary>Writes a GitHub-flavoured Markdown report (for job summaries and PR comments) with "\n" line endings.</summary>
    public static void Write(AnalysisResult result, TextWriter writer)
    {
        writer.Write($"## PowerOps analyze\n\n**{TextReport.Summary(result)}**\n");
        foreach (var s in result.Solutions)
        {
            var sol = s.Solution;
            writer.Write($"\n### {Escape(s.Path)}: {Escape(sol.UniqueName)} {Escape(sol.Version)}\n\n");
            if (s.Findings.Count == 0)
            {
                writer.Write("No findings.\n");
                continue;
            }

            writer.Write("| Severity | Rule | File | Message | Fix |\n|---|---|---|---|---|\n");
            foreach (var f in s.Findings)
            {
                var severity = f.Severity == Severity.Error ? "error" : "warning";
                writer.Write($"| {severity} | [{f.RuleId}]({Analyzer.HelpUri(f.RuleId)}) | {Escape(f.File)} | {Escape(f.Message)} | {Escape(Analyzer.Find(f.RuleId)?.Fix)} |\n");
            }
        }
    }

    internal const string Specials = "\\|*_[]<>`";

    // Everything from the solution is untrusted: TextReport.Clean turns control characters into spaces and drops bidi and other format characters, Markdown/HTML syntax is backslash-escaped, and a zero-width
    // space after '@' and '#' and inside '://' stops a mention, an issue reference or an autolink. Clean runs first and never over this output: it strips that space.
    internal static string Escape(string? text)
    {
        text = TextReport.Clean(text);
        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (Specials.Contains(c))
            {
                result.Append('\\').Append(c);
            }
            else if (c is '@' or '#')
            {
                result.Append(c).Append('\u200B');
            }
            else if (c == ':' && string.CompareOrdinal(text, i + 1, "//", 0, 2) == 0)
            {
                result.Append(":/\u200B");
                i++;
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}