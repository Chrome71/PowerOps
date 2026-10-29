using System.CommandLine;

using PowerOps.Core.Output;

namespace PowerOps.Cli;

public static class BaselineCommand
{
    public static Command Create(TextWriter error)
    {
        var paths = new Argument<string[]>("path")
        {
            Description = "Solution folders or zips to analyze; folders are searched recursively.",
            Arity = ArgumentArity.ZeroOrMore,
            DefaultValueFactory = _ => ["."],
        };
        var outFile = new Option<string>("--out") { Description = "Baseline file to write.", DefaultValueFactory = _ => "powerops-baseline.json" };

        var command = new Command("baseline", "Record the current findings in a baseline file so 'analyze --baseline' only reports new ones.")
        {
            paths, outFile,
        };
        command.SetAction(parse =>
        {
            var file = parse.GetValue(outFile)!;
            return AnalyzeCommand.Run(parse.GetValue(paths)!, null, file, error, (result, inputs) =>
            {
                var json = new StringWriter();
                Baseline.Write(result, json);
                SafeWrite.WriteText(file, json.ToString(), inputs);
                return 0;
            });
        });
        return command;
    }
}