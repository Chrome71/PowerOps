using System.CommandLine;
using System.Text;

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

        var command = new Command("diff", "Compare two versions of a solution component by component (offline, read-only).") { oldPath, newPath, format, outFile };
        command.SetAction(parse => Execute(parse.GetValue(oldPath)!, parse.GetValue(newPath)!, parse.GetValue(format)!, parse.GetValue(outFile), output, error));
        return command;
    }

    private static int Execute(string oldPath, string newPath, string format, string? outFile, TextWriter output, TextWriter error)
    {
        foreach (var p in new[] { oldPath, newPath })
        {
            if (!File.Exists(p) && !Directory.Exists(p))
            {
                error.WriteLine($"error: path not found: {p}");
                return 2;
            }
        }

        try
        {
            var result = SolutionDiff.Compare(SolutionLoader.Load(oldPath), SolutionLoader.Load(newPath));
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
                File.WriteAllText(outFile, report.ToString(), new UTF8Encoding(false));
            }

            return result.Changes.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {ex.Message}");
            return 3;
        }
    }
}