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
            writer.Write($"{s.Path}: {sol.UniqueName} {sol.Version} (publisher {sol.Publisher.UniqueName}, prefix {sol.Publisher.Prefix})\n");
            foreach (var required in s.Requires)
            {
                writer.Write($"  requires {required}\n");
            }

            foreach (var f in s.Findings)
            {
                var severity = f.Severity == Severity.Error ? "error" : "warning";
                writer.Write($"  {severity} {f.RuleId} {f.File}: {f.Message}\n");
            }

            if (s.Findings.Count == 0)
            {
                writer.Write("  no findings\n");
            }
        }

        writer.Write($"{Count(result.Solutions.Count, "solution")}, {Count(result.Errors, "error")}, {Count(result.Warnings, "warning")}\n");
    }

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}