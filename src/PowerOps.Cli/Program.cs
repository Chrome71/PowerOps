using System.CommandLine;
using System.Globalization;
using System.Text;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Output;

namespace PowerOps.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        UseInvariantCulture();
        try
        {
            Console.OutputEncoding = new UTF8Encoding(false);
        }
        catch (IOException)
        {
            // On Windows this throws when no console is attached, for example an MCP server a GUI client started.
        }

        return Run(args, Console.Out, Console.Error);
    }

    // Messages must not depend on the machine's culture ("index -1" is "index U+2212 1" under sv-SE). Main and Run both call this: the MCP handlers and the tests enter at Run.
    internal static void UseInvariantCulture() =>
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    // Output and error writers are injectable so tests can capture them.
    public static int Run(string[] args, TextWriter output, TextWriter error) =>
        Run(args, output, error, (pacArgs, timeout) => new PacRunner(timeout: timeout).Run(pacArgs));

    // The command tree; tests read it too (HandoffDocTests checks the commands a page names).
    internal static RootCommand Root(TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot>? driftLive = null,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget>? deployTarget = null)
    {
        // The live reader (az runner, HttpClient) is built only when a run has a live --env.
        var reader = new Lazy<DriftLiveReader>(() => new DriftLiveReader(
            pac,
            (azArgs, timeout) => new PacRunner(DriftLiveReader.AzExecutable(OperatingSystem.IsWindows()), timeout).Run(azArgs),
            DriftLiveReader.Web(DriftLiveReader.Handler())));
        driftLive ??= (solution, env) => reader.Value.Read(solution, env);
        deployTarget ??= (target, solutions, dependencies) => reader.Value.ReadTarget(target, solutions, dependencies);
        var root = new RootCommand(
            "A free, open-source, cross-platform toolkit that brings software-engineering discipline " +
            "(analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform.");

        root.Subcommands.Add(AnalyzeCommand.Create(output, error));
        root.Subcommands.Add(BaselineCommand.Create(error));
        root.Subcommands.Add(DiffCommand.Create(output, error));
        root.Subcommands.Add(PlanCommand.Create(output, error, pac));
        root.Subcommands.Add(TestCommand.Create(output, error));
        root.Subcommands.Add(SkillsCommand.Create(output, error));
        root.Subcommands.Add(RulesCommand.Create(output));
        root.Subcommands.Add(SettingsCommand.Create(output, error, pac));
        root.Subcommands.Add(DriftCommand.Create(output, error, driftLive));

        root.Subcommands.Add(DeployCommand.Create(output, error, pac, deployTarget));
        root.Subcommands.Add(McpCommand.Create(error, pac, driftLive, deployTarget));

        return root;
    }

    // The pac runner is injectable so tests of `settings apply` never call a real pac; so are the live reads of `drift` and `deploy plan`.
    public static int Run(string[] args, TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot>? driftLive = null,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget>? deployTarget = null)
    {
        UseInvariantCulture();
        try
        {
            // No response files: `@file` is an ordinary argument, so a file cannot supply options.
            var parse = Root(output, error, pac, driftLive, deployTarget).Parse(args, new ParserConfiguration { ResponseFileTokenReplacer = null });
            if (parse.Errors.Count > 0)
            {
                // System.CommandLine returns 1 for parse errors; usage errors are 2 here.
                foreach (var e in parse.Errors)
                {
                    error.WriteLine($"error: {TextReport.Clean(e.Message)}");
                }

                return 2;
            }

            // Its default handler would exit 1 ("findings") for an exception a command did not catch.
            return parse.Invoke(new InvocationConfiguration { Output = output, Error = error, EnableDefaultExceptionHandler = false });
        }
        catch (Exception ex)
        {
            error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {TextReport.Clean(ex.Message)}");
            return 3;
        }
    }
}