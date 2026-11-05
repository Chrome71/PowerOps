using System.CommandLine;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Core.Deploy;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public partial class HandoffDocTests
{
    private const string PacTools = "mcp__pac-cli__solution_import|mcp__pac-cli__pages_upload|mcp__pac-cli__pages_upload-code-site|mcp__pac-cli__copilot_publish|mcp__pac-cli__model_genpage_upload";

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

        Assert.Equal([PacTools, "Bash", "Monitor", "PowerShell"], groups.Select(g => (string?)g!["matcher"]));
        foreach (var hook in groups.SelectMany(g => g!["hooks"]!.AsArray()))
        {
            Assert.Equal("powerops deploy gate \"$CLAUDE_PROJECT_DIR/deploy/powerops-plan.json\" || exit 2", (string?)hook!["command"]);
            Assert.Equal("block", (string?)hook["onFailure"]);
            Assert.Equal(600, (int?)hook["timeout"]);
        }
    }

    // Codex turns the - of a server or tool name into _, so its matcher is Claude Code's with each - replaced.
    [Fact]
    public void The_codex_matcher_is_the_claude_one_in_codex_names_and_the_gate_blocks_every_name()
    {
        var codex = PacTools.Replace('-', '_');

        Assert.Contains($"`{codex}`", Page());
        Assert.Contains("Hooks copied from an earlier version of this page match only `solution_import`.", Page());
        foreach (var tool in PacTools.Split('|').Concat(codex.Split('|')))
        {
            var hook = new JsonObject { ["tool_name"] = tool, ["tool_input"] = new JsonObject() };
            Assert.IsType<ImportRead.Blocked>(ImportCall.Read(System.Text.Encoding.UTF8.GetBytes(hook.ToJsonString())));
        }
    }

    [Fact]
    public void The_per_product_table_has_a_row_for_each_product_and_names_its_build_skill()
    {
        var page = Page();
        var section = page[page.IndexOf("## Per product\n", StringComparison.Ordinal)..page.IndexOf("## The steps\n", StringComparison.Ordinal)];
        var rows = section.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal)).Skip(1).ToDictionary(l => l.Split('|')[1].Trim());
        var skills = new Dictionary<string, string>
        {
            ["Canvas app"] = "`canvas-app`",
            ["Model-driven app"] = "`app-builder`",
            ["Code component (PCF)"] = "`pcf`",
            ["Power Pages site"] = "`create-site`",
            ["Cloud flow"] = "`build-flow`",
            ["Code app"] = "`create-code-app`",
            ["Dataverse tables"] = "`dv-metadata`",
            ["Copilot Studio agent"] = "`copilot-studio`",
            ["Power BI"] = "`semantic-model-authoring`",
        };

        Assert.Equal(skills.Keys, rows.Keys);
        Assert.All(skills, s => Assert.Contains(s.Value, rows[s.Key]));
        Assert.Contains("needs a yes", rows["Power BI"]);
        Assert.Contains("(step 11)", rows["Copilot Studio agent"]);
    }

    [Fact]
    public void Step_4_also_runs_the_flow_tests_and_no_step_is_renumbered()
    {
        var lines = Page().Split('\n');
        var numbers = lines.Select(l => Step().Match(l)).Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(Enumerable.Range(1, 11), numbers);
        Assert.Contains("`powerops test ", lines.Single(l => l.StartsWith("4. **Analyze", StringComparison.Ordinal)));
    }

    [Fact]
    public void Step_11_lists_what_a_solution_does_not_carry_for_an_agent()
    {
        var step = Page().Split('\n').Single(l => l.StartsWith("11. **An agent was imported.**", StringComparison.Ordinal));

        Assert.All(
            new[] { "Application Insights", "manual authentication", "Direct Line and web channel security", "channels", "sharing", "Publish the agent before you share it", "publish it again after a value changes", "ask first", "waiting" },
            item => Assert.Contains(item, step));
    }

    // M54: the page claimed a delete rule that the skill did not carry (C-3 of the 2026-11-04 live test).
    [Fact]
    public void The_delete_rule_the_page_claims_is_in_the_implement_skill()
    {
        var page = Page();
        var claim = page.Split('\n').Single(l => l.Contains("Other tenant writes have no route either", StringComparison.Ordinal));
        var skill = File.ReadAllText(Path.Combine(Golden.RepoRoot, "skills", "powerops-implement", "SKILL.md")).Replace("\r\n", "\n");

        Assert.Contains("`powerops-implement` never runs a delete: it lists a stray object in the plan's `writes.md` for the person to remove", claim);
        Assert.DoesNotContain("asks the user before each one", page);
        Assert.Contains("9. Never delete in a tenant, even when the user tells you to: no `DELETE` call, no `delete` or `remove` command or tool, not even for an object this run made.", skill);
        Assert.Contains("give it an `open:` cleanup in `writes.md`", skill);
        Assert.Contains("`powerops-implement` never runs a delete", page.Split('\n').Single(l => l.StartsWith("| Power BI |", StringComparison.Ordinal)));
    }
}