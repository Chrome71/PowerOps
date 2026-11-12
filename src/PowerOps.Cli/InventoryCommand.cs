using System.CommandLine;

using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Cli;

/// <summary><c>powerops inventory</c> (M107): what solutions connect to and what makes them premium, offline. Not a check: it exits 0 with a report, never 1.</summary>
public static class InventoryCommand
{
    private const int UsageError = 2;

    public static Command Create(TextWriter output, TextWriter error)
    {
        var paths = new Argument<string[]>("path")
        {
            Description = "Solution folders or zips to list; folders are searched recursively.",
            Arity = ArgumentArity.ZeroOrMore,
            DefaultValueFactory = _ => ["."],
        };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json", "markdown");
        var outFile = new Option<string?>("--out") { Description = "Write the report to this file instead of stdout." };
        var showValues = new Option<bool>("--show-values") { Description = "Print literal HTTP hosts, and names as written; by default URLs, GUIDs and addresses in names are replaced." };

        var command = new Command("inventory", "List what solutions connect to and what makes them premium (offline, read-only).")
        {
            paths, format, outFile, showValues,
        };
        command.SetAction(parse =>
        {
            var show = parse.GetValue(showValues);
            return Execute(parse.GetValue(paths)!, parse.GetValue(format)!, parse.GetValue(outFile), show, output, show ? error : new Redaction.Writer(error));
        });
        return command;
    }

    private static int Execute(string[] paths, string format, string? outFile, bool showValues, TextWriter output, TextWriter error)
    {
        foreach (var p in paths)
        {
            if (!File.Exists(p) && !Directory.Exists(p))
            {
                error.WriteLine($"error: path not found: {TextReport.Clean(p)}");
                return UsageError;
            }
        }

        IReadOnlyList<string> found;
        try
        {
            found = [.. paths.SelectMany(p => SolutionDiscovery.Find(p))];
        }
        catch (Exception ex)
        {
            return AnalyzeCommand.Fail(ex, error);
        }

        // The inputs analyze uses: the zips named and the zips and solution.yml files found. Refused before anything is loaded.
        string[] inputs = [.. paths.Where(File.Exists), .. found.Where(File.Exists)];
        if (outFile is not null && SafeWrite.Refusal(outFile, inputs) is { } refusal)
        {
            error.WriteLine($"error: {refusal.Text}");
            return UsageError;
        }

        try
        {
            var context = new LoadContext();
            var loaded = AnalysisResult.LoadFound(found, Directory.GetCurrentDirectory(), context);
            if (loaded.Count == 0)
            {
                error.WriteLine($"error: no solutions found under {string.Join(", ", paths.Select(p => $"\"{TextReport.Clean(p)}\""))}. To analyze a YAML layout, point at the folder that holds solutions/.");
                return UsageError;
            }

            var inventory = InventoryReport.Build(loaded, showValues);
            TextReport.WriteNotes(context.SkippedLinks, error);
            InventoryReport.WriteNotes(error, inventory);
            var report = new StringWriter();
            if (format == "json")
            {
                InventoryReport.WriteJson(report, inventory);
            }
            else if (format == "markdown")
            {
                InventoryReport.WriteMarkdown(report, inventory);
            }
            else
            {
                InventoryReport.WriteText(report, inventory);
            }

            if (outFile is null)
            {
                output.Write(report.ToString());
            }
            else
            {
                SafeWrite.WriteText(outFile, report.ToString(), inputs);
            }

            return 0;
        }
        catch (Exception ex)
        {
            return AnalyzeCommand.Fail(ex, error);
        }
    }
}