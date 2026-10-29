using System.CommandLine;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public partial class HandoffDocTests
{
    // `powerops` at the start of a word, then groups of one space and a lowercase word, never across a line break.
    [GeneratedRegex(@"\bpowerops((?: [a-z][a-z-]*)+)")]
    private static partial Regex Mention();

    // A step of the flow: a line that starts with its number and a bold title.
    [GeneratedRegex(@"^(\d+)\. \*\*")]
    private static partial Regex Step();

    internal static List<string[]> WordLists(string text) =>
        [.. Mention().Matches(text).Select(m => m.Groups[1].Value.Trim().Split(' ')).DistinctBy(w => string.Join(' ', w))];

    private static string Page() => File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "HANDOFF.md")).Replace("\r\n", "\n");

    // The PreToolUse groups of the page's one JSON block, the Claude Code hooks.
    private static JsonArray HookGroups()
    {
        var page = Page();
        var start = page.IndexOf("```json\n", StringComparison.Ordinal) + "```json\n".Length;
        return JsonNode.Parse(page[start..page.IndexOf("```", start, StringComparison.Ordinal)])!["hooks"]!["PreToolUse"]!.AsArray();
    }

    // The words up to and including the first one that is not a subcommand, all of them when they stop at a command
    // that still has subcommands, or null when they reach a command.
    internal static string? Unknown(IReadOnlyList<string> words)
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

        foreach (var command in new[] { "plan status", "plan sign-off", "skills install", "analyze", "test", "diff", "settings generate", "settings validate", "deploy plan", "deploy apply", "drift" })
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

    [Fact]
    public void The_hooks_run_the_gate_and_block_when_it_fails()
    {
        var groups = HookGroups();

        Assert.Equal(["mcp__pac-cli__solution_import", "Bash", "Monitor", "PowerShell"], groups.Select(g => (string?)g!["matcher"]));
        foreach (var hook in groups.SelectMany(g => g!["hooks"]!.AsArray()))
        {
            Assert.Equal("powerops deploy gate \"$CLAUDE_PROJECT_DIR/deploy/powerops-plan.json\" || exit 2", (string?)hook!["command"]);
            Assert.Equal("block", (string?)hook["onFailure"]);
            Assert.Equal(600, (int?)hook["timeout"]);
        }
    }

    [Fact]
    public void Step_4_also_runs_the_flow_tests_and_no_step_is_renumbered()
    {
        var lines = Page().Split('\n');
        var numbers = lines.Select(l => Step().Match(l)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(Enumerable.Range(1, 10), numbers);
        Assert.Contains("`powerops test ", lines.Single(l => l.StartsWith("4. **Analyze", StringComparison.Ordinal)));
    }
}