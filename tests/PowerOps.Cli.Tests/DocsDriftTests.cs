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

    [GeneratedRegex(@"\s#\s.*$")]
    private static partial Regex TrailingComment();

    [GeneratedRegex(@"--[a-z][a-z-]*")]
    private static partial Regex Flag();

    [GeneratedRegex(@"\|\||&&|\||;")]
    private static partial Regex Separator();

    [GeneratedRegex(@"^\| \[(PO\d{3})\]\([^)]*\) \| (\w+) \|", RegexOptions.Multiline)]
    private static partial Regex RuleRow();

    [GeneratedRegex(@"^\s*#?\s*([a-z][a-z-]*):")]
    private static partial Regex Key();

    [GeneratedRegex(@"^[a-z]+(?:_[a-z]+)+$")]
    private static partial Regex SnakeCase();

    [GeneratedRegex(@"mcp__(?:plugin_powerops_)?powerops__([a-z_]*)")]
    private static partial Regex McpName();

    // An action tag (@v7) or a release number (v1.2.3). Third-party URLs are cut out first, and a version comment
    // after a full-SHA pin (the pinning rule asks for one) is not a release of this repository.
    [GeneratedRegex(@"@v\d|(?<![\w-])v\d+\.\d+\.\d+")]
    private static partial Regex Version();

    [GeneratedRegex(@"https://(?!github\.com/Chrome71/PowerOps)\S+")]
    private static partial Regex ForeignUrl();

    [GeneratedRegex(@"@[0-9a-f]{40}\b")]
    private static partial Regex Pin();

    private static readonly RootCommand Tree = Program.Root(TextWriter.Null, TextWriter.Null, (_, _) => throw new InvalidOperationException("no pac call"));

    private static string Repo(params string[] parts) => Path.Combine([Golden.RepoRoot, .. parts]);

    // README, AGENTS.md, CONTRIBUTING.md, SECURITY.md, the skills and every docs page, except the plans and BUILD-PLAN.
    private static IEnumerable<(string Name, string Text)> Pages()
    {
        var files = new[] { "README.md", "AGENTS.md", "CONTRIBUTING.md", "SECURITY.md" }.Select(f => Repo(f))
            .Concat(Directory.GetFiles(Repo("skills"), "SKILL.md", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Repo("docs"), "*.md", SearchOption.AllDirectories));
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(Golden.RepoRoot, file).Replace('\\', '/');
            if (!relative.StartsWith("docs/plans/", StringComparison.Ordinal) && relative != "docs/BUILD-PLAN.md" && !relative.Contains("node_modules", StringComparison.Ordinal))
            {
                yield return (relative, File.ReadAllText(file));
            }
        }
    }

    // Inline code spans, and each line of a fenced block (without a trailing "# comment").
    private static IEnumerable<string> CodeUnits(string text)
    {
        var fenced = false;
        foreach (var line in text.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
            }
            else if (fenced)
            {
                yield return TrailingComment().Replace(line, "");
            }
            else
            {
                foreach (Match m in Span().Matches(line))
                {
                    yield return m.Groups[1].Value;
                }
            }
        }
    }

    internal static List<string> UnknownCommands(string text) =>
        [.. CodeUnits(text).SelectMany(HandoffDocTests.WordLists).Select(HandoffDocTests.Unknown).OfType<string>().Distinct()];

    // A `powerops <command> ... --flag` in one code span: the flag must be an option of that command.
    internal static List<string> UnknownFlags(string text)
    {
        var problems = new List<string>();
        foreach (var segment in CodeUnits(text).SelectMany(u => Separator().Split(u)))
        {
            var at = segment.IndexOf("powerops ", StringComparison.Ordinal);
            if (at < 0 || HandoffDocTests.WordLists(segment).FirstOrDefault() is not { } words || HandoffDocTests.Unknown(words) is not null)
            {
                continue;
            }

            var command = (Command)Tree;
            var path = new List<string>();
            foreach (var next in words.Select(w => command.Subcommands.FirstOrDefault(s => s.Name == w)).TakeWhile(s => s is not null))
            {
                command = next!;
                path.Add(next!.Name);
            }

            var known = command.Options.SelectMany(o => o.Aliases.Append(o.Name)).Concat(["--help", "--version"]).ToHashSet();
            problems.AddRange(Flag().Matches(segment[at..]).Select(f => f.Value).Where(f => !known.Contains(f)).Select(f => $"{string.Join(' ', path)} {f}"));
        }

        return problems;
    }

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

    // The keys under `with:` of every `uses: Chrome71/PowerOps@...` step, commented-out keys included.
    private static IEnumerable<string> WithKeys(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("uses: Chrome71/PowerOps@", StringComparison.Ordinal))
            {
                continue;
            }

            var indent = lines[i].IndexOf("- uses:", StringComparison.Ordinal);
            var inWith = false;
            for (var j = i + 1; j < lines.Length && (lines[j].Trim().Length == 0 || lines[j].Length - lines[j].TrimStart().Length >= indent + 2); j++)
            {
                inWith = inWith || lines[j].Trim() == "with:";
                if (inWith && Key().Match(lines[j]) is { Success: true } key && key.Groups[1].Value != "with")
                {
                    yield return key.Groups[1].Value;
                }
            }
        }
    }

    internal static List<string> UnknownInputs(string text, IEnumerable<string> inputs) => [.. WithKeys(text).Except(inputs).Distinct()];

    internal static List<string> UndocumentedInputs(string text, IEnumerable<string> inputs)
    {
        var documented = WithKeys(text).Concat(CodeUnits(text)).ToHashSet();
        return [.. inputs.Where(i => !documented.Contains(i))];
    }

    // Snake-case code spans that start with a command name (deploy_apply) and mcp__powerops__<tool> names must be tools.
    internal static List<string> UnknownTools(string text, IReadOnlyCollection<string> tools)
    {
        var commands = Tree.Subcommands.Select(c => c.Name).ToHashSet();
        var spans = CodeUnits(text).Where(u => SnakeCase().IsMatch(u) && commands.Contains(u[..u.IndexOf('_')]));
        var prefixed = CodeUnits(text).SelectMany(u => McpName().Matches(u).Select(m => m.Groups[1].Value)).Where(n => n.Length > 0);
        return [.. spans.Concat(prefixed).Except(tools).Distinct()];
    }

    // No release tag exists yet, so no page may name a release or an action tag. When a tag exists, allow that version here.
    internal static List<string> VersionProblems(string text) =>
        [.. text.Split('\n').SelectMany(line =>
            Version().Matches(ForeignUrl().Replace(line, "")).Where(m => m.Value[0] == '@' || !Pin().IsMatch(line)).Select(m => m.Value))];

    private static string[] ToolNames()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Repo("tests", "golden", "mcp-tools.txt")));
        return [.. doc.RootElement.EnumerateArray().Select(t => t.GetProperty("name").GetString()!)];
    }

    private static void NoProblems(Func<string, IEnumerable<string>> check) =>
        Assert.Empty(Pages().SelectMany(p => check(p.Text).Select(x => $"{p.Name}: {x}")));

    [Fact]
    public void Every_command_the_docs_name_exists() => NoProblems(UnknownCommands);

    [Fact]
    public void Every_flag_the_docs_name_exists_on_its_command() => NoProblems(UnknownFlags);

    [Fact]
    public void The_rule_table_matches_the_analyzer()
    {
        NoProblems(RuleTableProblems);
        Assert.Contains(Pages(), p => RuleRow().IsMatch(p.Text));
    }

    [Fact]
    public void The_action_inputs_match_the_docs()
    {
        var inputs = ActionInputs().ToList();
        Assert.Contains("category", inputs);
        NoProblems(t => UnknownInputs(t, inputs));
        Assert.Empty(UndocumentedInputs(Pages().Single(p => p.Name == "docs/GITHUB-ACTION.md").Text, inputs));
    }

    [Fact]
    public void The_mcp_tool_names_match_the_golden_list()
    {
        var tools = ToolNames();
        NoProblems(t => UnknownTools(t, tools));
        var page = Pages().Single(p => p.Name == "docs/USE-WITH-AGENTS.md").Text;
        Assert.Empty(tools.Except(CodeUnits(page)));
    }

    [Fact]
    public void No_page_names_a_release_or_an_action_tag() => NoProblems(VersionProblems);

    [Fact]
    public void The_checks_accept_correct_text()
    {
        Assert.Empty(UnknownCommands("`powerops deploy apply plan.json --apply`"));
        Assert.Empty(UnknownFlags("`powerops analyze --format sarif --out a.sarif` then `powerops settings apply z.zip --apply --target x | jq --raw-output .`"));
        Assert.Empty(VersionProblems("- uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1\n[tag](https://github.com/ALM4Dataverse/x/releases/tag/v1.18.0), `rust-v0.163.0`"));
        Assert.Empty(UnknownTools("`deploy_apply`, `mcp__powerops__deploy_apply`, `tool_timeout_sec`", ToolNames()));
    }

    [Fact]
    public void A_flag_that_does_not_exist_fails()
    {
        Assert.Equal(["analyze --nope"], UnknownFlags("Run `powerops analyze --nope`."));
        Assert.Equal(["deploy plan --apply"], UnknownFlags("```sh\npowerops deploy plan exports/ --apply   # a comment\n```"));
        Assert.Equal(["analyse"], UnknownCommands("`powerops analyse`"));
    }

    [Fact]
    public void A_rule_row_with_the_wrong_severity_fails()
    {
        var table = Pages().Single(p => p.Name == "docs/rules/README.md").Text;

        Assert.Empty(RuleTableProblems(table));
        Assert.NotEmpty(RuleTableProblems(table.Replace("| [PO001](PO001.md) | error |", "| [PO001](PO001.md) | warning |")));
        Assert.NotEmpty(RuleTableProblems(table.Replace("| [PO014](PO014.md) | warning | Canvas app formula contains a hard-coded environment or SharePoint URL |\n", "")));
    }

    [Fact]
    public void An_unknown_action_input_fails_and_so_does_a_missing_one()
    {
        var inputs = ActionInputs().ToList();
        const string Step = "      - uses: Chrome71/PowerOps@b4abce4cb00421c7017076de7b53e53a7ce063c7\n        with:\n          path: x\n          # baseline: b.json\n          nope: 1\n";

        Assert.Equal(["nope"], UnknownInputs(Step, inputs));
        Assert.Empty(UnknownInputs(Step.Replace("nope: 1", "tests: t"), inputs));
        Assert.Contains("category", UndocumentedInputs(Step, inputs));
        Assert.DoesNotContain("category", UndocumentedInputs(Step + "The `category` input.", inputs));
    }

    [Fact]
    public void An_unknown_mcp_tool_name_fails()
    {
        Assert.Equal(["deploy_aply", "plan_check"], UnknownTools("`deploy_aply` and `mcp__plugin_powerops_powerops__plan_check`", ToolNames()));
    }

    [Fact]
    public void A_release_number_or_an_action_tag_fails()
    {
        Assert.Equal(["v0.4.0"], VersionProblems("shipped in v0.4.0"));
        Assert.Equal(["@v7"], VersionProblems("- uses: actions/checkout@v7"));
        Assert.Equal(["@v4"], VersionProblems("- uses: x/y@v4 and https://example.com/v2.0.0"));
    }
}