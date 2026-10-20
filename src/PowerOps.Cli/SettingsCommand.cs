using System.CommandLine;
using System.Text;

using PowerOps.Core.Output;
using PowerOps.Core.Readers;
using PowerOps.Core.Settings;

namespace PowerOps.Cli;

public static class SettingsCommand
{
    private const int UsageError = 2;
    private const int InternalError = 3;

    public static Command Create(TextWriter output, TextWriter error)
    {
        var command = new Command("settings", "Generate and check pac deployment settings files (offline).");
        command.Subcommands.Add(Generate(output, error));
        command.Subcommands.Add(Validate(output, error));
        return command;
    }

    private static Command Generate(TextWriter output, TextWriter error)
    {
        var solution = new Argument<string>("solution") { Description = "An unpacked solution folder or an exported zip." };
        var outFile = new Option<string?>("--out") { Description = "Write the settings file here instead of stdout." };
        var command = new Command("generate", "Print a deployment settings file with empty values to fill in.") { solution, outFile };
        command.SetAction(parse => Guard(error, () =>
        {
            var path = parse.GetValue(solution)!;
            if (NotFound(path, error))
            {
                return UsageError;
            }

            var json = DeploymentSettings.Generate(SolutionLoader.Load(path));
            if (parse.GetValue(outFile) is { } file)
            {
                File.WriteAllText(file, json, new UTF8Encoding(false));
            }
            else
            {
                output.Write(json);
            }

            return 0;
        }));
        return command;
    }

    private static Command Validate(TextWriter output, TextWriter error)
    {
        var solution = new Argument<string>("solution") { Description = "An unpacked solution folder or an exported zip." };
        var settings = new Option<string>("--settings") { Description = "The deployment settings file to check.", Required = true };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json");
        var command = new Command("validate", "Check a deployment settings file against a solution: missing or extra entries, empty connection ids and values.") { solution, settings, format };
        command.SetAction(parse => Guard(error, () =>
        {
            var path = parse.GetValue(solution)!;
            var file = parse.GetValue(settings)!;
            if (NotFound(path, error))
            {
                return UsageError;
            }

            if (!File.Exists(file))
            {
                error.WriteLine($"error: settings file not found: {file}");
                return UsageError;
            }

            var loaded = SolutionLoader.Load(path);
            IReadOnlyList<SettingsProblem> problems;
            try
            {
                problems = DeploymentSettings.Validate(loaded, File.ReadAllText(file));
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                error.WriteLine($"error: {file}: {ex.Message}");
                return InternalError;
            }

            if (parse.GetValue(format) == "json")
            {
                SettingsReport.WriteJson(loaded.UniqueName, file, problems, output);
            }
            else
            {
                SettingsReport.WriteText(problems, output);
            }

            return problems.Count > 0 ? 1 : 0;
        }));
        return command;
    }

    private static bool NotFound(string path, TextWriter error)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            return false;
        }

        error.WriteLine($"error: path not found: {path}");
        return true;
    }

    // Same mapping as AnalyzeCommand.Run: any failure is exit 3 (POWEROPS_DEBUG=1 for the stack trace).
    private static int Guard(TextWriter error, Func<int> run)
    {
        try
        {
            return run();
        }
        catch (Exception ex)
        {
            error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {ex.Message}");
            return InternalError;
        }
    }
}