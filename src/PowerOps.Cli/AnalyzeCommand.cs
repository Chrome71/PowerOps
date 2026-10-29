using System.CommandLine;
using System.Reflection;

using PowerOps.Core.Output;
using PowerOps.Core.Readers;

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
        format.AcceptOnlyFromAmong("text", "json", "sarif", "markdown");
        var outFile = new Option<string?>("--out") { Description = "Write the report to this file instead of stdout." };
        var failOn = new Option<string>("--fail-on") { Description = "Lowest severity that makes the exit code 1.", DefaultValueFactory = _ => "error" };
        failOn.AcceptOnlyFromAmong("error", "warning", "none");
        var baseline = new Option<string?>("--baseline") { Description = "Ignore findings listed in this baseline file (see 'powerops baseline')." };

        var command = new Command("analyze", "Check unpacked or exported solutions for common problems (offline, read-only).")
        {
            paths, format, outFile, failOn, baseline,
        };
        command.SetAction(parse => Execute(
            parse.GetValue(paths)!, parse.GetValue(format)!, parse.GetValue(outFile), parse.GetValue(failOn)!, parse.GetValue(baseline), output, error));
        return command;
    }

    // Same value `--version` prints.
    internal static string ToolVersion() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    private static int Execute(string[] paths, string format, string? outFile, string failOn, string? baselineFile, TextWriter output, TextWriter error) =>
        Run(paths, baselineFile, outFile, error, (result, inputs) =>
        {
            var report = new StringWriter();
            if (format == "json")
            {
                JsonReport.Write(result, report);
            }
            else if (format == "sarif")
            {
                SarifReport.Write(result, report, ToolVersion());
            }
            else if (format == "markdown")
            {
                MarkdownReport.Write(result, report);
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
                SafeWrite.WriteText(outFile, report.ToString(), inputs);
            }

            var failed = failOn switch
            {
                "error" => result.Errors > 0,
                "warning" => result.Errors + result.Warnings > 0,
                _ => false,
            };
            return failed ? 1 : 0;
        });

    // The files the user named (a zip among the paths, the baseline) and the files the scan found: zips and solution.yml files. A folder is not an input, so a report may be written into it.
    private static List<string> Inputs(string[] paths, string? baselineFile, IEnumerable<string> found) =>
        [.. paths.Where(File.Exists), .. baselineFile is null ? Array.Empty<string>() : [baselineFile], .. found.Where(File.Exists)];

    /// <summary>Shared by analyze and baseline: checks the paths, the baseline file and the output file (<paramref name="outFile"/>, null for stdout), analyzes, drops baselined findings, then hands the result to <paramref name="emit"/> (which returns the exit code). Maps failures to exit codes 2 and 3.</summary>
    internal static int Run(string[] paths, string? baselineFile, string? outFile, TextWriter error, Func<AnalysisResult, int> emit) =>
        Run(paths, baselineFile, outFile, error, (result, _) => emit(result));

    /// <summary>As above; <paramref name="emit"/> also gets the files a write must not replace: those named and those the scan found.</summary>
    internal static int Run(string[] paths, string? baselineFile, string? outFile, TextWriter error, Func<AnalysisResult, IReadOnlyList<string>, int> emit)
    {
        foreach (var p in paths)
        {
            if (!File.Exists(p) && !Directory.Exists(p))
            {
                error.WriteLine($"error: path not found: {p}");
                return UsageError;
            }
        }

        if (baselineFile is not null && !File.Exists(baselineFile))
        {
            error.WriteLine($"error: baseline file not found: {baselineFile}");
            return UsageError;
        }

        // Listed before the refusal, which must come before the work; the analysis below reuses the list.
        IReadOnlyList<string> found;
        try
        {
            found = [.. paths.SelectMany(p => SolutionDiscovery.Find(p))];
        }
        catch (Exception ex)
        {
            return Fail(ex, error);
        }

        var inputs = Inputs(paths, baselineFile, found);
        if (outFile is not null && SafeWrite.Refusal(outFile, inputs) is { } refusal)
        {
            error.WriteLine($"error: {refusal.Text}");
            return UsageError;
        }

        Baseline? baseline = null;
        if (baselineFile is not null)
        {
            try
            {
                baseline = Baseline.Load(baselineFile);
            }
            catch (InvalidDataException ex)
            {
                error.WriteLine($"error: {TextReport.Clean(ex.Message)}");
                return UsageError;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error.WriteLine($"error: cannot read baseline {TextReport.Clean(baselineFile)}: {TextReport.Clean(ex.Message)}");
                return UsageError;
            }
        }

        try
        {
            var context = new LoadContext();
            var result = AnalysisResult.RunFound(found, Directory.GetCurrentDirectory(), context);
            if (result.Solutions.Count == 0)
            {
                error.WriteLine($"error: no solutions found under {string.Join(", ", paths)}. To analyze a YAML layout, point at the folder that holds solutions/.");
                return UsageError;
            }

            TextReport.WriteNotes(context.SkippedLinks, error);
            return emit(baseline?.Apply(result) ?? result, inputs);
        }
        catch (Exception ex)
        {
            return Fail(ex, error);
        }
    }

    private static int Fail(Exception ex, TextWriter error)
    {
        error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {TextReport.Clean(ex.Message)}");
        return InternalError;
    }
}