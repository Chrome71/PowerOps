using System.CommandLine;

namespace PowerOps.Cli;

public static class Program
{
    public static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    // Output and error writers are injectable so tests can capture them.
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var root = new RootCommand(
            "A free, open-source, cross-platform toolkit that brings software-engineering discipline " +
            "(analysis, diffing, testing, repeatable deployment) to Microsoft Power Platform.");

        return root.Parse(args).Invoke(new InvocationConfiguration { Output = output, Error = error });
    }
}