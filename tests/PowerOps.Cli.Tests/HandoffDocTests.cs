using System.CommandLine;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public partial class HandoffDocTests
{
    // `powerops` at the start of a word, then groups of one space and a lowercase word, never across a line break.
    [GeneratedRegex(@"\bpowerops((?: [a-z][a-z-]*)+)")]
    private static partial Regex Mention();

    private static List<string[]> WordLists(string text) =>
        [.. Mention().Matches(text).Select(m => m.Groups[1].Value.Trim().Split(' ')).DistinctBy(w => string.Join(' ', w))];

    private static string Page() => File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "HANDOFF.md"));

    // The words up to and including the first one that is not a subcommand, all of them when they stop at a command
    // that still has subcommands, or null when they reach a command.
    private static string? Unknown(IReadOnlyList<string> words)
    {
        Command current = Program.Root(TextWriter.Null, TextWriter.Null, (_, _) => throw new InvalidOperationException("no pac call"));
        for (var i = 0; i < words.Count && current.Subcommands.Count > 0; i++)
        {
            var next = current.Subcommands.FirstOrDefault(c => c.Name == words[i]);
            if (next is null)
            {
                return string.Join(' ', words.Take(i + 1));
            }

            current = next;
        }

        return current.Subcommands.Count > 0 ? string.Join(' ', words) : null;
    }

    [Fact]
    public void Every_command_the_page_names_exists()
    {
        var lists = WordLists(Page());

        Assert.NotEmpty(lists);
        Assert.Empty(lists.Select(Unknown).OfType<string>());
    }

    [Fact]
    public void The_page_names_each_command_of_the_flow()
    {
        var lists = WordLists(Page()).Select(w => string.Join(' ', w)).ToList();

        foreach (var command in new[] { "plan status", "plan sign-off", "skills install", "analyze", "diff", "settings generate", "settings validate", "deploy plan", "deploy apply", "drift" })
        {
            Assert.Contains(lists, l => l == command || l.StartsWith(command + " ", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void The_readme_links_the_page()
    {
        Assert.Contains("docs/HANDOFF.md", File.ReadAllText(Path.Combine(Golden.RepoRoot, "README.md")));
    }

    [Fact]
    public void The_check_reports_unknown_words_and_accepts_known_ones()
    {
        Assert.Equal("deploy nope", Unknown(["deploy", "nope"]));
        Assert.Equal("nope", Unknown(["nope"]));
        Assert.Equal("deploy", Unknown(["deploy"]));
        Assert.Null(Unknown(["deploy", "apply"]));
        Assert.Null(Unknown(["plan", "sign-off", "docs"]));

        var misspelled = WordLists(Page().Replace("deploy apply", "deploy aply"));
        Assert.Contains("deploy aply", misspelled.Select(Unknown));
    }
}