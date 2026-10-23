using System.CommandLine;
using System.Text;
using System.Text.RegularExpressions;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Cli;

public static partial class DeployCommand
{
    private const int UsageError = 2;
    private const int InternalError = 3;
    private const long MaxSettingsBytes = 1024 * 1024;
    private const string SettingsSuffix = ".settings.json";

    /// <param name="target">Reads the target: the <c>--target</c> value, the solution names of the folder and the dependency names not in it. Throws <see cref="DriftReadException"/>.</param>
    public static Command Create(TextWriter output, TextWriter error, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> target)
    {
        var folder = new Argument<string>("folder") { Description = "A folder of exported solution zips, each with an optional <zip name>.settings.json beside it." };
        var targetOption = new Option<string?>("--target") { Description = "The environment to plan against (an id or URL, as pac --environment). Never printed." };
        var save = new Option<string?>("--save") { Description = "Also write the JSON report (the plan file) here, whatever --format is." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json", "markdown");
        var outFile = new Option<string?>("--out") { Description = "Write the report here instead of stdout." };
        var plan = new Command("plan", "Say what importing the solution zips in a folder into the target would do. Reads the target only.")
        {
            folder, targetOption, save, format, outFile,
        };
        plan.SetAction(parse => SettingsCommand.Guard(error, () =>
            Run(parse.GetValue(folder)!, parse.GetValue(targetOption), parse.GetValue(save), parse.GetValue(format)!, parse.GetValue(outFile), output, error, target)));
        return new Command("deploy", "Plan a deployment of solution zips.") { plan };
    }

    [GeneratedRegex(@"\A[A-Za-z0-9._-]+\z")]
    private static partial Regex SafeName();

    private static string Clean(string? text) => TextReport.Clean(text);

    private static int Run(
        string folder, string? target, string? save, string format, string? outFile, TextWriter output, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        // The --target value and the folder's full path are never printed.
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(target))
        {
            problems.Add("--target is required");
        }

        foreach (var (name, value) in new[] { ("--save", save), ("--out", outFile) })
        {
            if (value is not null && Directory.Exists(value))
            {
                problems.Add($"{name} names an existing folder, not a file");
            }
        }

        var zips = new List<(string Name, string Path)>();
        if (!Directory.Exists(folder))
        {
            problems.Add("the folder argument is not an existing folder");
        }
        else
        {
            zips = ListZips(folder, problems);
        }

        if (problems.Count > 0)
        {
            problems.ForEach(p => error.WriteLine($"error: {p}"));
            return UsageError;
        }

        var loaded = new List<DeployZip>();
        foreach (var (name, path) in zips)
        {
            if (Load(name, path, folder, error) is not { } zip)
            {
                return InternalError;
            }

            loaded.Add(zip);
        }

        foreach (var g in loaded.GroupBy(z => z.Solution.UniqueName, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            error.WriteLine($"error: {string.Join(" and ", g.Select(z => Clean(z.File)))} hold the same solution ({Clean(g.Key)})");
            return UsageError;
        }

        DeployTarget got;
        try
        {
            // Build checks every zip (unique name, version, flow ids, settings file) before any read, against a target that holds none of them.
            DeployPlan.Build(loaded, new DeployTarget("", Guid.Empty, [.. loaded.Select(z => new Snapshot("", z.Solution.UniqueName, false, null, null, true, [], [], [], []))], new Dictionary<string, string?>()));
            got = read(target!, [.. loaded.Select(z => z.Solution.UniqueName)], DeployPlan.TargetDependencies(loaded.Select(z => z.Solution)));
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            return InternalError;
        }
        catch (DriftReadException ex)
        {
            error.WriteLine($"error: --target: {ex.Message}");
            if (!string.IsNullOrEmpty(ex.Forwarded))
            {
                error.Write(ex.Forwarded.EndsWith('\n') ? ex.Forwarded : ex.Forwarded + "\n");
            }

            return ex.ExitCode;
        }

        DeployPlanResult result;
        try
        {
            result = DeployPlan.Build(loaded, got);
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            return InternalError;
        }

        var report = new StringWriter();
        switch (format)
        {
            case "json":
                DeployPlanReport.WriteJson(result, AnalyzeCommand.ToolVersion(), report);
                break;
            case "markdown":
                DeployPlanReport.WriteMarkdown(result, report);
                break;
            default:
                DeployPlanReport.WriteText(result, report);
                break;
        }

        if (save is not null)
        {
            var json = new StringWriter();
            DeployPlanReport.WriteJson(result, AnalyzeCommand.ToolVersion(), json);
            File.WriteAllText(save, json.ToString(), new UTF8Encoding(false));
        }

        if (outFile is not null)
        {
            File.WriteAllText(outFile, report.ToString(), new UTF8Encoding(false));
        }
        else
        {
            output.Write(report.ToString());
        }

        return result.Blocked ? 1 : 0;
    }

    // The folder's zips in ordinal order; problems are added for every file name, case or link that breaks the rules.
    private static List<(string Name, string Path)> ListZips(string folder, List<string> problems)
    {
        var files = Directory.EnumerateFiles(folder)
            .Select(f => (Name: System.IO.Path.GetFileName(f), Path: f))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();
        var zips = files.Where(f => f.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
        var settings = files.Where(f => f.Name.EndsWith(SettingsSuffix, StringComparison.OrdinalIgnoreCase)).ToList();
        if (zips.Count == 0)
        {
            problems.Add("the folder holds no zip file");
        }

        var bad = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in zips.Concat(settings))
        {
            if (!SafeName().IsMatch(f.Name))
            {
                problems.Add($"{Clean(f.Name)}: a zip or settings file name may hold only letters, digits, '.', '_' and '-'");
                bad.Add(f.Name);
            }
            else if (new FileInfo(f.Path).LinkTarget is not null)
            {
                problems.Add($"{Clean(f.Name)}: symbolic links are not read");
                bad.Add(f.Name);
            }
        }

        var expected = zips.Where(z => !bad.Contains(z.Name)).Select(z => (Zip: z.Name, Settings: System.IO.Path.GetFileNameWithoutExtension(z.Name) + SettingsSuffix)).ToList();
        foreach (var s in settings.Where(s => !bad.Contains(s.Name) && !expected.Any(e => e.Settings == s.Name)))
        {
            var near = expected.FirstOrDefault(e => string.Equals(e.Settings, s.Name, StringComparison.OrdinalIgnoreCase));
            problems.Add(near.Zip is null
                ? $"{Clean(s.Name)}: no zip file has this settings file name"
                : $"{Clean(s.Name)}: differs only in case from {Clean(near.Settings)}, the settings file name of {Clean(near.Zip)}");
        }

        return [.. zips.Select(z => (z.Name, z.Path))];
    }

    // Returns null (after printing the reason) when the zip or its settings file cannot be read.
    private static DeployZip? Load(string name, string path, string folder, TextWriter error)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            var solution = SolutionLoader.Load(path);
            var settingsName = System.IO.Path.GetFileNameWithoutExtension(name) + SettingsSuffix;
            var settingsPath = System.IO.Path.Combine(folder, settingsName);
            if (!File.Exists(settingsPath))
            {
                return new DeployZip(name, DeployPlan.Sha256Hex(bytes), solution, null, null, null);
            }

            if (new FileInfo(settingsPath).Length > MaxSettingsBytes)
            {
                error.WriteLine($"error: {Clean(settingsName)}: the settings file is larger than 1 MB");
                return null;
            }

            var settings = File.ReadAllBytes(settingsPath);
            using var reader = new StreamReader(new MemoryStream(settings));
            return new DeployZip(name, DeployPlan.Sha256Hex(bytes), solution, settingsName, DeployPlan.Sha256Hex(settings), reader.ReadToEnd());
        }
        catch (InvalidDataException ex)
        {
            // The loader's message starts with the path as given; only the file name is printed.
            error.WriteLine($"error: {Clean(ex.Message.Replace(path, name, StringComparison.Ordinal))}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"error: {Clean(name)}: the file could not be read");
        }

        return null;
    }
}