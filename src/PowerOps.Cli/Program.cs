using System.CommandLine;
using System.Text;

namespace PowerOps.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        return Run(args, Console.Out, Console.Error);
    }

    // Output and error writers are injectable so tests can capture them.
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var root = new RootCommand(
            "A free, open-source, cross-platform toolkit that brings software-engineering discipline " +
            "(analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform.");

        root.Subcommands.Add(AnalyzeCommand.Create(output, error));
        root.Subcommands.Add(BaselineCommand.Create(error));
        root.Subcommands.Add(PlanCommand.Create(output, error));
        root.Subcommands.Add(SkillsCommand.Create(output, error));

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