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

    /// <summary>One "note:" line on <paramref name="error"/> for each link a load skipped, so a check does not pass with less read than the user thinks.</summary>
    public static void WriteNotes(IEnumerable<string> skippedLinks, TextWriter error)
    {
        foreach (var link in skippedLinks)
        {
            error.WriteLine($"note: {Clean(link)}: a link inside the solution, not followed and not read");
        }
    }

    internal static string Summary(AnalysisResult result) =>
        $"{Count(result.Solutions.Count, "solution")}, {Count(result.Errors, "error")}, {Count(result.Warnings, "warning")}{(result.Baselined > 0 ? $", {result.Baselined} baselined" : "")}";

    // A hostile solution must not forge lines (or "::error::" workflow commands) or reorder the text (bidi controls) in the report.
    // Control characters and the line and paragraph separators become spaces; format characters (category Cf) are dropped.
    public static string Clean(string? text) => string.Concat((text ?? "").Select(c => char.IsControl(c) || c is '\u2028' or '\u2029' ? " " : char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format ? "" : c.ToString()));

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}