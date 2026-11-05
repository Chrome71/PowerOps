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
            foreach (var r in p.Reports)
            {
                // The state is text from the report; null (not stated, or not read) is printed as it is.
                writer.WriteLine($"  report {Clean(r.File)}: state {(r.State is null ? "null" : $"'{Clean(r.State)}'")}, stale {(r.Stale is { } stale ? (stale ? "true" : "false") : "null")}"
                    + (r.SolutionHash is null ? "" : $", input {(r.Input is null ? "null" : $"'{Clean(r.Input)}'")}, solution-hash {r.SolutionHash}"));
            }

            if (p.Writes is { } j)
            {
                writer.WriteLine(j.Entries is { } entries && j.OpenCleanup is { } open
                    ? $"  writes writes.md: {entries} {(entries == 1 ? "entry" : "entries")}, {Plural(open, "open cleanup")}"
                    : "  writes writes.md: not read");
            }

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
                reports = p.Reports.Select(r => new { file = r.File, state = r.State, stale = r.Stale, input = r.Input, solutionHash = r.SolutionHash }),
                writes = p.Writes is { } j ? new { entries = j.Entries, openCleanup = j.OpenCleanup } : null,
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