using System.Text.Encodings.Web;
using System.Text.Json;

namespace PowerOps.Core.Plans;

public static class PlanStatusReport
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void WriteText(IReadOnlyList<PlanReport> plans, TextWriter writer)
    {
        foreach (var p in plans)
        {
            // File names come from the folder being checked; Clean keeps a name with a line break from starting a line of its own.
            writer.WriteLine(p.Malformed
                ? $"{Clean(p.Path)}: malformed header"
                : p.Status is null
                    ? $"{Clean(p.Path)}: plan.md not read"
                    : $"{Clean(p.Path)}: {p.Status}, review {p.Review}, {Plural(p.OpenQuestions, "open question")}");
            foreach (var v in p.Violations)
            {
                writer.WriteLine($"  error: {Clean(v)}");
            }

            foreach (var w in p.Warnings)
            {
                writer.WriteLine($"  warning: {Clean(w)}");
            }
        }

        writer.WriteLine($"{Plural(plans.Count, "plan")}, {Plural(plans.Sum(p => p.Violations.Count), "violation")}, {Plural(plans.Sum(p => p.Warnings.Count), "warning")}");
    }

    public static void WriteJson(IReadOnlyList<PlanReport> plans, TextWriter writer)
    {
        var doc = new
        {
            schemaVersion = Output.JsonOutput.SchemaVersion,
            plans = plans.Select(p => new
            {
                path = p.Path,
                status = p.Status,
                review = p.Review,
                signedOff = p.SignedOff,
                environment = p.Environment,
                depth = p.Depth,
                supersedes = p.Supersedes,
                openQuestions = p.OpenQuestions,
                bodyHash = p.BodyHash,
                violations = p.Violations,
                warnings = p.Warnings,
            }),
            summary = new { plans = plans.Count, violations = plans.Sum(p => p.Violations.Count), warnings = plans.Sum(p => p.Warnings.Count) },
        };
        writer.Write(JsonSerializer.Serialize(doc, Options) + "\n");
    }

    private static string Clean(string text) => Output.TextReport.Clean(text);

    private static string Plural(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}