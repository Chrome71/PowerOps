using System.CommandLine;

using PowerOps.Core.Output;

namespace PowerOps.Cli;

public static class RulesCommand
{
    public static Command Create(TextWriter output)
    {
        var json = new Option<bool>("--json") { Description = "Print the rules as JSON." };
        var format = new Option<string>("--format") { Description = "Report format (--json is the older spelling of --format json).", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json");
        var command = new Command("rules", "List every analysis rule with its severity, title, fix and docs link.") { json, format };
        command.SetAction(parse =>
        {
            if (parse.GetValue(json) || parse.GetValue(format) == "json")
            {
                RulesReport.WriteJson(output);
            }
            else
            {
                RulesReport.WriteText(output);
            }

            return 0;
        });
        return command;
    }
}