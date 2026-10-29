using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Core.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Fails when the docs or the skills name something the code does not have. Each check takes text, so the last
// tests can feed it planted wrong text and expect a failure.
public partial class DocsDriftTests
{
    [GeneratedRegex(@"`([^`\n]+)`")]
    private static partial Regex Span();

    // A fenced block with its info string (sh, text, ...) and body.
    [GeneratedRegex(@"^[ \t]*```(\w*)\n(.*?)^[ \t]*```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex Fence();

    [GeneratedRegex(@"\s#\s.*$")]
    private static partial Regex TrailingComment();

    [GeneratedRegex(@"(?<![\w-])--[a-z][a-z-]*")]
    private static partial Regex Flag();

    // The words after a `powerops` that starts a code unit. Inside a message (error: powerops exited ...) it is text.
    [GeneratedRegex(@"^powerops((?: [a-z][a-z-]*)+)")]
    private static partial Regex CommandWords();

    [GeneratedRegex(@"^\| \[(PO\d{3})\]\([^)]*\) \| (\w+) \|", RegexOptions.Multiline)]
    private static partial Regex RuleRow();

    [GeneratedRegex(@"^\s*([a-z][a-z-]*):")]
    private static partial Regex Key();

    [GeneratedRegex(@"^[a-z]+(?:_[a-z]+)+$")]
    private static partial Regex SnakeCase();

    [GeneratedRegex(@"mcp__(?:plugin_powerops_)?powerops__([a-z_]*)")]
    private static partial Regex McpName();

    // PowerOps' own action tag (PowerOps@v1) or release (PowerOps v0.4.0); other tools' versions are not ours to check.
    [GeneratedRegex(@"powerops(?:@| )v\d", RegexOptions.IgnoreCase)]
    private static partial Regex Version();

    private static readonly RootCommand Tree = Program.Root(TextWriter.Null, TextWriter.Null, (_, _) => throw new InvalidOperationException("no pac call"));

    // Every option of every command, and the flags of pac that docs/COMMANDS.md describes.
    private static readonly HashSet<string> KnownFlags = [.. Every(Tree).SelectMany(c => c.Options).SelectMany(o => o.Aliases.Append(o.Name)),
        "--help", "--version", "--path", "--settings-file", "--stage-and-upgrade", "--skip-lower-version", "--environment", "--async", "--max-async-wait-time"];

    private static IEnumerable<Command> Every(Command command) => command.Subcommands.SelectMany(Every).Prepend(command);

    private static string Repo(params string[] parts) => Path.Combine([Golden.RepoRoot, .. parts]);

    // README, AGENTS.md, CONTRIBUTING.md, SECURITY.md, the skills, the docs pages and example workflows, except the plans and BUILD-PLAN.
    private static IEnumerable<(string Name, string Text)> Pages() =>
        new[] { "README.md", "AGENTS.md", "CONTRIBUTING.md", "SECURITY.md" }.Select(f => Repo(f))
            .Concat(Directory.GetFiles(Repo("skills"), "SKILL.md", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Repo("docs"), "*.md", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Repo("docs", "examples"), "*.yml"))
            .Select(f => (Name: Path.GetRelativePath(Golden.RepoRoot, f).Replace('\\', '/'), File: f))
            .Where(p => !p.Name.StartsWith("docs/plans/", StringComparison.Ordinal) && p.Name != "docs/BUILD-PLAN.md" && !p.Name.Contains("node_modules", StringComparison.Ordinal))
            .Select(p => (p.Name, File.ReadAllText(p.File).Replace("\r\n", "\n")));

    // Inline code spans outside fences, and the logical lines of sh, bash and shell fences (a trailing \ joins the next
    // line, and a trailing "# comment" is cut). Other fences hold output or other languages, and are skipped.
    private static IEnumerable<string> CodeUnits(string text) =>
        Span().Matches(Fence().Replace(text, "")).Select(m => m.Groups[1].Value)
            .Concat(Fence().Matches(text).Where(f => f.Groups[1].Value is "sh" or "bash" or "shell")
                .SelectMany(f => f.Groups[2].Value.Replace("\\\n", " ").Split('\n')).Select(l => TrailingComment().Replace(l, "")));

    private static IEnumerable<string> Invocations(string text) =>
        CodeUnits(text).Select(u => u.TrimStart()).Where(u => u.StartsWith("powerops ", StringComparison.Ordinal));

    // The words up to and including the first one that is not a subcommand of the command reached, or null. A group named alone is fine.
    private static string? Unknown(string[] words)
    {
        Command command = Tree;
        for (var i = 0; i < words.Length && command.Subcommands.Count > 0; i++)
        {
            if (command.Subcommands.FirstOrDefault(s => s.Name == words[i]) is not { } next)
            {
                return string.Join(' ', words[..(i + 1)]);
            }

            command = next;
        }

        return null;
    }

    internal static List<string> UnknownCommands(string text) =>
        [.. Invocations(text).Select(u => CommandWords().Match(u)).Where(m => m.Success).Select(m => Unknown(m.Groups[1].Value.Trim().Split(' '))).OfType<string>().Distinct()];

    // Flags are not tied to the command they follow: all of a text, or those of each `powerops ...` unit up to its first | or &&.
    internal static List<string> UnknownFlags(string text, bool whole = false) =>
        [.. (whole ? [text] : Invocations(text).Select(u => u.Split('|', '&', ';')[0])).SelectMany(s => Flag().Matches(s)).Select(m => m.Value).Where(f => !KnownFlags.Contains(f)).Distinct()];

    // The rows of a rule table (| [PO001](PO001.md) | error | ...) against Analyzer.Rules: ids in order, and severities.
    internal static List<string> RuleTableProblems(string text)
    {
        var rows = RuleRow().Matches(text).Select(m => (Id: m.Groups[1].Value, Severity: m.Groups[2].Value)).ToList();
        var rules = Analyzer.Rules.Select(r => (r.Id, Severity: r.Severity.ToString().ToLowerInvariant())).ToList();
        return rows.Count == 0 ? [] : [.. rows.Except(rules).Select(r => $"{r.Id} {r.Severity}"), .. rules.Except(rows).Select(r => $"{r.Id} {r.Severity} is missing or differs")];
    }

    private static IEnumerable<string> ActionInputs() =>
        File.ReadAllLines(Repo("action.yml")).SkipWhile(l => l != "inputs:").Skip(1).TakeWhile(l => l.StartsWith(' ') || l.Length == 0)
            .Select(l => Regex.Match(l, @"^  ([a-z][a-z-]*):\s*$")).Where(m => m.Success).Select(m => m.Groups[1].Value);

    private static int Indent(string line) => line.Length - line.TrimStart().Length;

    // The keys under `with:` of every step that has `uses: Chrome71/PowerOps@...`. A step is the lines after it that are blank or
    // indented at least to the column of `uses:`; the keys are the lines under `with:` that are indented deeper than it.
    private static IEnumerable<string> WithKeys(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var column = lines[i].IndexOf("uses: Chrome71/PowerOps@", StringComparison.Ordinal);
            if (column < 0)
            {
                continue;
            }

            var step = lines.Skip(i + 1).TakeWhile(l => l.Trim().Length == 0 || Indent(l) >= column).SkipWhile(l => l.Trim() != "with:").ToList();
            foreach (var line in step.Skip(1).TakeWhile(l => l.Trim().Length == 0 || Indent(l) > Indent(step[0])))
            {
                if (Key().Match(line) is { Success: true } key)
                {
                    yield return key.Groups[1].Value;
                }
            }
        }
    }

    internal static List<string> UnknownInputs(string text, IEnumerable<string> inputs) => [.. WithKeys(text).Except(inputs).Distinct()];

    internal static List<string> UndocumentedInputs(string text, IEnumerable<string> inputs) => [.. inputs.Except(WithKeys(text).Concat(CodeUnits(text)))];

    // Snake-case code spans that start with a command name (deploy_apply) and mcp__powerops__<tool> names must be tools.
    internal static List<string> UnknownTools(string text, IReadOnlyCollection<string> tools)
    {
        var commands = Tree.Subcommands.Select(c => c.Name).ToHashSet();
        var spans = CodeUnits(text).Where(u => SnakeCase().IsMatch(u) && commands.Contains(u[..u.IndexOf('_')]));
        var prefixed = CodeUnits(text).SelectMany(u => McpName().Matches(u).Select(m => m.Groups[1].Value)).Where(n => n.Length > 0);
        return [.. spans.Concat(prefixed).Except(tools).Distinct()];
    }

    // No release tag exists yet, so no page may name a PowerOps release or action tag. When a tag exists, allow that version here.
    internal static List<string> VersionProblems(string text) => [.. Version().Matches(text).Select(m => m.Value)];

    private static string[] ToolNames() => [.. JsonDocument.Parse(File.ReadAllText(Repo("tests", "golden", "mcp-tools.txt"))).RootElement.EnumerateArray().Select(t => t.GetProperty("name").GetString()!)];

    private static string Page(string name) => Pages().Single(p => p.Name == name).Text;

    private static void NoProblems(Func<string, IEnumerable<string>> check) => Assert.Empty(Pages().SelectMany(p => check(p.Text).Select(x => $"{p.Name}: {x}")));

    [Fact]
    public void Every_command_the_docs_name_exists() => NoProblems(UnknownCommands);

    [Fact]
    public void Every_flag_the_docs_name_exists()
    {
        NoProblems(t => UnknownFlags(t));
        Assert.Empty(UnknownFlags(Page("docs/COMMANDS.md"), whole: true));
    }

    [Fact]
    public void The_rule_table_matches_the_analyzer()
    {
        NoProblems(RuleTableProblems);
        Assert.Contains(Pages(), p => RuleRow().IsMatch(p.Text));
    }

    [Fact]
    public void The_action_inputs_match_the_docs()
    {
        Assert.Contains("category", ActionInputs());
        NoProblems(t => UnknownInputs(t, ActionInputs()));
        Assert.Empty(UndocumentedInputs(Page("docs/GITHUB-ACTION.md"), ActionInputs()));
    }

    [Fact]
    public void The_mcp_tool_names_match_the_golden_list()
    {
        NoProblems(t => UnknownTools(t, ToolNames()));
        Assert.Empty(ToolNames().Except(CodeUnits(Page("docs/USE-WITH-AGENTS.md"))));
    }

    [Fact]
    public void No_page_names_a_powerops_release_or_an_action_tag() => NoProblems(VersionProblems);

    // A with: block with a comment that holds a colon, then a sibling key: neither is an input.
    private const string Step = "- uses: Chrome71/PowerOps@b4abce4cb00421c7017076de7b53e53a7ce063c7\n  with:\n    path: x\n    # note: keep off for forks\n  env:\n    token: y\n";

    [Fact]
    public void The_checks_accept_correct_text()
    {
        Assert.Empty(UnknownCommands("`powerops settings --help`, `powerops deploy`, `powerops deploy apply p.json`, `error: powerops exited with code N`\n```text\npowerops found 2 findings --nope\n```"));
        Assert.Empty(UnknownFlags("`powerops settings apply z.zip --apply | jq --raw-output .`, `powerops analyze a && git commit --amend`"));
        Assert.Empty(UnknownFlags("`--fail-on error|warning|none` and `--path`", whole: true));
        Assert.Empty(UnknownInputs(Step, ActionInputs()));
        Assert.Empty(VersionProblems("`pac v2.13.1`, uses: actions/checkout@v4, `rust-v0.163.0`"));
        Assert.Empty(UnknownTools("`deploy_apply`, `mcp__powerops__deploy_apply`, `tool_timeout_sec`", ToolNames()));
    }

    [Fact]
    public void Wrong_text_fails()
    {
        var table = Page("docs/rules/README.md");

        Assert.Equal(["analyse", "deploy aply"], UnknownCommands("`powerops analyse`\n```sh\npowerops deploy aply x\n```"));
        Assert.Equal(["--nope", "--zzz"], UnknownFlags("`powerops analyze --nope`\n```sh\npowerops analyze x \\\n  --zzz  # a comment\n```"));
        Assert.Equal(["--fail-onn"], UnknownFlags("`--fail-onn error|warning|none`", whole: true));
        Assert.NotEmpty(RuleTableProblems(table.Replace("| [PO001](PO001.md) | error |", "| [PO001](PO001.md) | warning |")));
        Assert.NotEmpty(RuleTableProblems(table.Replace("| [PO014](PO014.md) | warning | Canvas app formula contains a hard-coded environment or SharePoint URL |\n", "")));
        Assert.Equal(["nope"], UnknownInputs(Step.Replace("path: x", "nope: 1"), ActionInputs()));
        Assert.Equal(["nope"], UnknownInputs("- name: x\n  uses: Chrome71/PowerOps@b4abce4cb00421c7017076de7b53e53a7ce063c7\n  with:\n    nope: 1\n", ActionInputs()));
        Assert.Contains("category", UndocumentedInputs(Step, ActionInputs()));
        Assert.DoesNotContain("category", UndocumentedInputs(Step + "The `category` input.", ActionInputs()));
        Assert.Equal(["deploy_aply", "plan_check"], UnknownTools("`deploy_aply` and `mcp__plugin_powerops_powerops__plan_check`", ToolNames()));
        Assert.Equal(["PowerOps@v1", "PowerOps v0"], VersionProblems("- uses: Chrome71/PowerOps@v1, shipped in PowerOps v0.4.0"));
    }
}