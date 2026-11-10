using System.CommandLine;

using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Cli;

public static class ManifestCommand
{
    public static Command Create(TextWriter output, TextWriter error)
    {
        var first = new Argument<string>("path") { Description = "An unpacked solution folder or an exported .zip." };
        var second = new Argument<string?>("new") { Description = "A second unpacked folder to compare the first with, file by file.", Arity = ArgumentArity.ZeroOrOne };

        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json");

        var command = new Command("manifest", "List a solution's files with a SHA-256 for each and one digest, or compare two unpacked folders file by file (offline, read-only).") { first, second, format };
        command.SetAction(parse => Execute(parse.GetValue(first)!, parse.GetValue(second), parse.GetValue(format) == "json", output, error));
        return command;
    }

    private static int Execute(string first, string? second, bool json, TextWriter output, TextWriter error)
    {
        var paths = second is null ? new[] { first } : [first, second];
        foreach (var p in paths)
        {
            var problem = !File.Exists(p) && !Directory.Exists(p) ? "path not found"
                : new FileInfo(p).LinkTarget is not null ? "a symbolic link, not read"
                : second is not null && !Directory.Exists(p) ? "not an unpacked folder; comparing takes two folders"
                : Directory.Exists(p) || p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? null
                : "not a folder or a .zip file";
            if (problem is not null)
            {
                error.WriteLine($"error: {TextReport.Clean(p)}: {TextReport.Clean(problem)}");
                return 2;
            }
        }

        try
        {
            var links = new List<string>();
            var manifests = paths.Select(p => Manifest.Read(p, links)).ToList();
            links.ForEach(l => error.WriteLine($"note: {TextReport.Clean(l)}: a symbolic link, not read"));

            if (second is null)
            {
                if (json)
                {
                    ManifestReport.WriteJson(output, [(first, manifests[0])], null);
                }
                else
                {
                    ManifestReport.WriteText(output, first, manifests[0]);
                }

                return 0;
            }

            var comparison = Manifest.Compare(manifests[0], manifests[1]);
            if (json)
            {
                ManifestReport.WriteJson(output, [(first, manifests[0]), (second, manifests[1])], comparison);
            }
            else
            {
                ManifestReport.WriteComparison(output, first, manifests[0], second, manifests[1], comparison);
            }

            return comparison.Added.Count + comparison.Removed.Count + comparison.Changed.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {TextReport.Clean(ex.Message)}");
            return 3;
        }
    }
}