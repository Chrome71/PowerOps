using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Core.Rules;

namespace PowerOps.Core.Output;

/// <summary>One analyzed solution. <paramref name="Path"/> is the display path (forward slashes, no absolute paths when avoidable).</summary>
public sealed record SolutionResult(string Path, Solution Solution, IReadOnlyList<Finding> Findings)
{
    /// <summary>Distinct, non-empty owning solutions of declared missing dependencies, sorted ordinal.</summary>
    public IReadOnlyList<string> Requires { get; } = Solution.MissingDependencies
        .Select(d => d.Required.Solution)
        .Where(s => !string.IsNullOrEmpty(s))
        .Select(s => s!)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>What the rules did not check because the export lacks the input; <c>analyze</c> prints each as a note.</summary>
    public IReadOnlyList<string> NotChecked { get; init; } = [];
}

/// <summary>One loaded solution: <paramref name="Path"/> is the display path (a YAML solution's root folder), <paramref name="FromYaml"/> true when it was read from <c>solution.yml</c>.</summary>
public sealed record LoadedSolution(string Path, Solution Solution, bool FromYaml);

public sealed record AnalysisResult(IReadOnlyList<SolutionResult> Solutions)
{
    /// <summary>Findings dropped because they are in the baseline.</summary>
    public int Baselined { get; init; }

    public int Errors => Count(Severity.Error);

    public int Warnings => Count(Severity.Warning);

    private int Count(Severity severity) => Solutions.Sum(s => s.Findings.Count(f => f.Severity == severity));

    /// <summary>Discovers, loads and analyzes every solution under <paramref name="paths"/>; display paths are relative to <paramref name="baseDir"/> when underneath it.</summary>
    public static AnalysisResult Run(IEnumerable<string> paths, string baseDir, LoadContext? context = null) =>
        RunFound(paths.SelectMany(p => SolutionDiscovery.Find(p)), baseDir, context);

    /// <summary>As <see cref="Run"/>, for solutions already found with <see cref="SolutionDiscovery.Find"/>, so a caller that needs the list first walks the folders once.</summary>
    public static AnalysisResult RunFound(IEnumerable<string> found, string baseDir, LoadContext? context = null) =>
        new(Each(found, baseDir, context, (shown, solution, _) => new SolutionResult(shown, solution, Analyzer.Run(solution)) { NotChecked = Analyzer.NotChecked(solution) }));

    /// <summary>Loads the solutions found as <see cref="RunFound"/> does (each full path once, the same display paths) without running the rules; the cloud flows are parsed here, so an unreadable flow's error names the solution, then the flow file, as <c>analyze</c>'s does.</summary>
    public static IReadOnlyList<LoadedSolution> LoadFound(IEnumerable<string> found, string baseDir, LoadContext? context = null) =>
        Each(found, baseDir, context, (shown, solution, yaml) =>
        {
            _ = RuleHelpers.CloudFlows(solution).Count();
            return new LoadedSolution(shown, solution, yaml);
        });

    private static List<T> Each<T>(IEnumerable<string> found, string baseDir, LoadContext? context, Func<string, Solution, bool, T> make)
    {
        var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var results = new List<T>();
        context ??= new LoadContext();
        foreach (var path in found)
        {
            if (!seen.Add(System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path))))
            {
                continue;
            }

            var solution = SolutionLoader.Load(path, context);
            // A YAML solution is reported as its root: finding files are relative to it.
            var yaml = YamlReader.IsSolutionYml(path);
            var shown = yaml ? YamlReader.RootFolder(path) : path;
            try
            {
                results.Add(make(DisplayPath(shown, baseDir), solution, yaml));
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException($"{path}: {e.Message}", e);
            }
        }

        return results;
    }

    private static string DisplayPath(string path, string baseDir)
    {
        var rel = System.IO.Path.GetRelativePath(baseDir, path);
        var underBase = !System.IO.Path.IsPathRooted(rel) && rel != ".." &&
            !rel.StartsWith(".." + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal);
        return System.IO.Path.TrimEndingDirectorySeparator(underBase ? rel : path).Replace('\\', '/');
    }
}