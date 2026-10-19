using System.CommandLine;
using System.Reflection;
using System.Text;

using PowerOps.Core.Output;

namespace PowerOps.Cli;

public static class AnalyzeCommand
{
    private const int UsageError = 2;
    private const int InternalError = 3;

    public static Command Create(TextWriter output, TextWriter error)
    {
        var paths = new Argument<string[]>("path")
        {
            Description = "Solution folders or zips to analyze; folders are searched recursively.",
            Arity = ArgumentArity.ZeroOrMore,
            DefaultValueFactory = _ => ["."],
        };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json", "sarif");
        var outFile = new Option<string?>("--out") { Description = "Write the report to this file instead of stdout." };
        var failOn = new Option<string>("--fail-on") { Description = "Lowest severity that makes the exit code 1.", DefaultValueFactory = _ => "error" };
        failOn.AcceptOnlyFromAmong("error", "warning", "none");

        var command = new Command("analyze", "Check unpacked or exported solutions for common problems (offline, read-only).")
        {
            paths, format, outFile, failOn,
        };
        command.SetAction(parse => Execute(
            parse.GetValue(paths)!, parse.GetValue(format)!, parse.GetValue(outFile), parse.GetValue(failOn)!, output, error));
        return command;
    }

    // Same value `--version` prints.
    private static string ToolVersion() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    private static int Execute(string[] paths, string format, string? outFile, string failOn, TextWriter output, TextWriter error)
    {
        foreach (var p in paths)
        {
            if (!File.Exists(p) && !Directory.Exists(p))
            {
                error.WriteLine($"error: path not found: {p}");
                return UsageError;
            }
        }

        try
        {
            var result = AnalysisResult.Run(paths, Directory.GetCurrentDirectory());
            if (result.Solutions.Count == 0)
            {
                error.WriteLine($"error: no solutions found under {string.Join(", ", paths)} (the YAML layout is not supported yet)");
                return UsageError;
            }

            var report = new StringWriter();
            if (format == "json")
            {
                JsonReport.Write(result, report);
            }
            else if (format == "sarif")
            {
                SarifReport.Write(result, report, ToolVersion());
            }
            else
            {
                TextReport.Write(result, report);
            }

            if (outFile is null)
            {
                output.Write(report.ToString());
            }
            else
            {
                File.WriteAllText(outFile, report.ToString(), new UTF8Encoding(false));
            }

            var failed = failOn switch
            {
                "error" => result.Errors > 0,
                "warning" => result.Errors + result.Warnings > 0,
                _ => false,
            };
            return failed ? 1 : 0;
        }
        catch (Exception ex)
        {
            error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {ex.Message}");
            return InternalError;
        }
    }
}