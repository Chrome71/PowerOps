using System.CommandLine;

using PowerOps.Core.Diff;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Cli;

public static class DiffCommand
{
    public static Command Create(TextWriter output, TextWriter error)
    {
        var oldPath = new Argument<string>("old") { Description = "The older solution: an exported zip or an unpacked folder." };
        var newPath = new Argument<string>("new") { Description = "The newer solution: an exported zip or an unpacked folder." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json", "markdown");
        var outFile = new Option<string?>("--out") { Description = "Write the report to this file instead of stdout." };

        var showValues = new Option<bool>("--show-values") { Description = "Print a changed defaultValue and the URLs in the report and stderr; by default a changed defaultValue is printed as added, removed or changed and each URL is replaced with a placeholder." };
        // Declared, never read: redaction is the default, so only --show-values changes the output.
        var redact = new Option<bool>("--redact") { Description = "The default: print a changed defaultValue only as added, removed or changed, and replace URLs with a placeholder. Kept so scripts that pass it keep working." };

        var command = new Command("diff", "Compare two versions of a solution component by component (offline, read-only).") { oldPath, newPath, format, outFile, showValues, redact };
        command.SetAction(parse => Execute(parse.GetValue(oldPath)!, parse.GetValue(newPath)!, parse.GetValue(format)!, parse.GetValue(outFile), !parse.GetValue(showValues), output, error));
        return command;
    }

    private static int Execute(string oldPath, string newPath, string format, string? outFile, bool redact, TextWriter output, TextWriter error)
    {
        if (redact)
        {
            error = new Redaction.Writer(error);
        }

        var paths = new[] { oldPath, newPath };
        for (var i = 0; i < paths.Length; i++)
        {
            var p = paths[i];
            if (!File.Exists(p) && !Directory.Exists(p))
            {
                error.WriteLine($"error: path not found: {TextReport.Clean(p)}");
                return 2;
            }

            // A folder can be a YAML layout root: resolve it the way `analyze` does. With none found, the loader reports why.
            IReadOnlyList<string> found;
            try
            {
                found = Directory.Exists(p) ? SolutionDiscovery.Find(p) : [];
            }
            catch (InvalidDataException ex)
            {
                error.WriteLine($"error: {TextReport.Clean(ex.Message)}");
                return 3;
            }
            catch (Exception ex)
            {
                // What Program.Run would write for it, but through the (possibly redacting) writer.
                error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {TextReport.Clean(ex.Message)}");
                return 3;
            }

            if (found.Count > 1)
            {
                error.WriteLine($"error: {TextReport.Clean(p)} holds {found.Count} solutions; pass one of them: {string.Join(", ", found.Select(TextReport.Clean))}");
                return 2;
            }

            if (found.Count == 1)
            {
                paths[i] = found[0];
            }
        }

        if (outFile is not null && SafeWrite.Refusal(outFile, [oldPath, newPath, .. paths]) is { } refusal)
        {
            error.WriteLine($"error: {refusal.Text}");
            return 2;
        }

        try
        {
            var context = new LoadContext();
            var result = SolutionDiff.Compare(SolutionLoader.Load(paths[0], context), SolutionLoader.Load(paths[1], context), redact);
            TextReport.WriteNotes(context.SkippedLinks, error);
            TextReport.WriteNotCompared(result.NotCompared, error);

            var report = new StringWriter();
            if (format == "json")
            {
                DiffReport.WriteJson(result, report);
            }
            else if (format == "markdown")
            {
                DiffReport.WriteMarkdown(result, report);
            }
            else
            {
                DiffReport.WriteText(result, report);
            }

            if (outFile is null)
            {
                output.Write(report.ToString());
            }
            else
            {
                SafeWrite.WriteText(outFile, report.ToString(), [oldPath, newPath, .. paths]);
            }

            return result.Changes.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {TextReport.Clean(ex.Message)}");
            return 3;
        }
    }
}