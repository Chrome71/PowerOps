using System.CommandLine;
using System.Text;

using PowerOps.Core.Drift;

namespace PowerOps.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        return Run(args, Console.Out, Console.Error);
    }

    // Output and error writers are injectable so tests can capture them.
    public static int Run(string[] args, TextWriter output, TextWriter error) =>
        Run(args, output, error, (pacArgs, timeout) => new PacRunner(timeout: timeout).Run(pacArgs));

    // The pac runner is injectable so tests of `settings apply` never call a real pac; so is the live read of `drift`.
    public static int Run(string[] args, TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot>? driftLive = null)
    {
        // The live reader (az runner, HttpClient) is built only when a run has a live --env.
        var reader = new Lazy<DriftLiveReader>(() => new DriftLiveReader(
            pac,
            (azArgs, timeout) => new PacRunner(DriftLiveReader.AzExecutable(OperatingSystem.IsWindows()), timeout).Run(azArgs),
            DriftLiveReader.Web(DriftLiveReader.Handler())));
        var root = new RootCommand(
            "A free, open-source, cross-platform toolkit that brings software-engineering discipline " +
            "(analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform.");

        root.Subcommands.Add(AnalyzeCommand.Create(output, error));
        root.Subcommands.Add(BaselineCommand.Create(error));
        root.Subcommands.Add(DiffCommand.Create(output, error));
        root.Subcommands.Add(PlanCommand.Create(output, error));
        root.Subcommands.Add(SkillsCommand.Create(output, error));
        root.Subcommands.Add(RulesCommand.Create(output));
        root.Subcommands.Add(SettingsCommand.Create(output, error, pac));
        root.Subcommands.Add(DriftCommand.Create(output, error, driftLive ?? ((solution, env) => reader.Value.Read(solution, env))));

        var parse = root.Parse(args);
        if (parse.Errors.Count > 0)
        {
            // System.CommandLine returns 1 for parse errors; usage errors are 2 here.
            foreach (var e in parse.Errors)
            {
                error.WriteLine($"error: {e.Message}");
            }

            return 2;
        }

        return parse.Invoke(new InvocationConfiguration { Output = output, Error = error });
    }
}