using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Holds docs/ecosystem/changes.md and compatibility.md to their formats, compatibility.md's pins to the files that set them, and
// the README and the AGENTS.md section to what they must say. Each check takes text, so the last tests can feed it planted wrong text.
public partial class EcosystemDocsTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static readonly string[] Fields = ["Priority", "Disposition", "Status", "Release status", "Found", "Verified", "Affects", "Sources", "Evidence", "Why", "Link"];

    private static readonly Dictionary<string, string[]> Closed = new()
    {
        ["Priority"] = ["P0", "P1", "P2", "P3"],
        ["Disposition"] = ["implement now", "plan for later", "monitor", "no action"],
        ["Status"] = ["open", "done", "closed"],
        ["Release status"] = ["announced", "preview", "GA", "deprecated", "retired", "unverified"],
        ["Evidence"] = ["documented", "tested", "documented and tested"],
    };

    [GeneratedRegex(@"^## ECO-(\d{4}): \S.*$")]
    private static partial Regex EntryHeading();

    [GeneratedRegex(@"^- (Priority|Disposition|Status|Release status|Found|Verified|Affects|Sources|Evidence|Why|Link|Supersedes): (\S.*)$")]
    private static partial Regex FieldLine();

    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2}), mode [ABC]$")]
    private static partial Regex Found();

    [GeneratedRegex(@"^(none|[^\s,]+(, [^\s,]+)*)$")]
    private static partial Regex Affects();

    [GeneratedRegex(@"\[[^\]]+\]\(([^)\s]*)\)")]
    private static partial Regex AnyLink();

    [GeneratedRegex(@"\[[^\]]+\]\(https://[^)\s]+\) \((?:published|updated|observed) (\d{4}-\d{2}-\d{2})\)")]
    private static partial Regex DatedLink();

    [GeneratedRegex(@"^ECO-\d{4}$")]
    private static partial Regex EntryId();

    // The ids may be followed by a free tail (cheap-path observation ids, sources with no baseline) that starts with . ; , or :.
    [GeneratedRegex(@"^- (\d{4}-\d{2}-\d{2}), mode [ABC]: \d+ automated read, \d+ not checked, \d+ by hand; ((?>no entries|ECO-\d{4}(?:, ECO-\d{4})*))(?:[.;,:].*)?$")]
    private static partial Regex LogLine();

    [GeneratedRegex(@"ECO-\d{4}")]
    private static partial Regex Id();

    [GeneratedRegex(@"(?m)^(?=## )")]
    private static partial Regex SectionStart();

    [GeneratedRegex(@"(?m)^\s*PAC_VERSION:\s*(\S+)\s*$")]
    private static partial Regex PacVersion();

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Golden.RepoRoot, .. parts])).Replace("\r\n", "\n");

    private static bool RealDate(string text, DateOnly today) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date <= today;

    private static string[] Sections(string text) => SectionStart().Split(text);

    private static bool IsLog(string section) => section.StartsWith("## Review log\n", StringComparison.Ordinal) || section == "## Review log";

    // The entries of the register and its review log: ids rising by one from ECO-0001, every field once and in order from its closed list,
    // real dates that are not after today, dated https sources, and a well-formed log line for each review.
    internal static List<string> ChangesProblems(string text, DateOnly today)
    {
        var problems = new List<string>();
        var sections = Sections(text).Skip(1).ToList();
        var logs = sections.Where(IsLog).ToList();
        if (logs.Count != 1 || !IsLog(sections[^1]))
        {
            problems.Add("the file must end with one '## Review log' section");
        }

        var entries = sections.Where(s => !IsLog(s)).ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            problems.AddRange(EntryProblems(entries[i], i + 1, today));
        }

        foreach (var line in logs.SelectMany(l => l.Split('\n').Skip(1)).Where(l => l.Trim().Length > 0))
        {
            var match = LogLine().Match(line);
            if (!match.Success || !RealDate(match.Groups[1].Value, today))
            {
                problems.Add($"review log line is malformed or dated after today: {line}");
                continue;
            }

            problems.AddRange(Id().Matches(match.Groups[2].Value).Select(m => m.Value).Where(id => int.Parse(id[4..], CultureInfo.InvariantCulture) > entries.Count).Select(id => $"review log names {id}, which is not an entry"));
        }

        return problems;
    }

    private static List<string> EntryProblems(string section, int number, DateOnly today)
    {
        var lines = section.Split('\n');
        var id = $"ECO-{number:D4}";
        var heading = EntryHeading().Match(lines[0]);
        if (!heading.Success)
        {
            return [$"entry {number}: the heading is not '## ECO-<4 digits>: <title>'"];
        }

        var problems = new List<string>();
        if (heading.Groups[1].Value != number.ToString("D4", CultureInfo.InvariantCulture))
        {
            problems.Add($"ECO-{heading.Groups[1].Value} should be {id}");
        }

        var body = lines.Skip(1).Where(l => l.Trim().Length > 0).ToList();
        var parsed = body.Select(l => FieldLine().Match(l)).ToList();
        if (parsed.Any(m => !m.Success))
        {
            return [.. problems, $"{id}: a line is not '- <Field>: <value>': {body[parsed.FindIndex(m => !m.Success)]}"];
        }

        var names = parsed.Select(m => m.Groups[1].Value).ToList();
        if (!names.SequenceEqual(Fields) && !names.SequenceEqual(Fields.Append("Supersedes")))
        {
            return [.. problems, $"{id}: the fields are not each present once, in order ({string.Join(", ", Fields)}, then Supersedes if any)"];
        }

        var v = parsed.ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        problems.AddRange(Closed.Where(c => !c.Value.Contains(v[c.Key])).Select(c => $"{id}: {c.Key} '{v[c.Key]}' is not one of {string.Join(", ", c.Value)}"));
        if (!Found().IsMatch(v["Found"]) || !RealDate(v["Found"][..10], today))
        {
            problems.Add($"{id}: Found is not '<date>, mode A|B|C' with a real date that is not after today");
        }

        if (!RealDate(v["Verified"], today))
        {
            problems.Add($"{id}: Verified is not a real date that is not after today");
        }

        if (!Affects().IsMatch(v["Affects"]))
        {
            problems.Add($"{id}: Affects is not 'none' or paths separated by commas");
        }

        problems.AddRange(SourceProblems(v["Sources"], today).Select(p => $"{id}: {p}"));
        if (v["Status"] == "done" && v["Link"] == "none")
        {
            problems.Add($"{id}: a done entry needs a Link other than none");
        }

        if (v["Disposition"] == "no action" && v["Status"] != "closed")
        {
            problems.Add($"{id}: a no action entry needs Status closed");
        }

        if (v.TryGetValue("Supersedes", out var earlier) && (!EntryId().IsMatch(earlier) || int.Parse(earlier[4..], CultureInfo.InvariantCulture) is var n && (n < 1 || n >= number)))
        {
            problems.Add($"{id}: Supersedes must name an earlier entry");
        }

        return problems;
    }

    private static List<string> SourceProblems(string sources, DateOnly today)
    {
        var links = AnyLink().Matches(sources);
        var dated = DatedLink().Matches(sources);
        var problems = new List<string>();
        if (links.Count == 0 || links.Any(l => !l.Groups[1].Value.StartsWith("https://", StringComparison.Ordinal)) || dated.Count != links.Count)
        {
            problems.Add("Sources needs at least one https link, each followed by (published|updated|observed <date>)");
        }

        problems.AddRange(dated.Where(d => !RealDate(d.Groups[1].Value, today)).Select(d => $"a source date {d.Groups[1].Value} is not real or is after today"));
        return problems;
    }

    // The table of compatibility.md: its columns, the four rows the repository pins, their Version cell (and only that cell) against the files,
    // and a real Verified date in every row.
    internal sealed record Pins(string Sdk, string Claude, string Codex, IReadOnlyList<string> Pac);

    private static readonly string[] Columns = ["Tool", "Version", "Where it is pinned", "Evidence", "Limits", "Verified"];

    private static string[] Cells(string line) => [.. line.Trim().Trim('|').Split('|').Select(c => c.Trim())];

    internal static List<string> CompatibilityProblems(string text, DateOnly today, Pins pins)
    {
        var table = text.Split('\n').Where(l => l.StartsWith('|')).Select(Cells).ToList();
        if (table.Count < 2 || !table[0].SequenceEqual(Columns))
        {
            return [$"the table's columns are not {string.Join(" | ", Columns)}"];
        }

        var problems = new List<string>();
        var rows = table.Skip(2).ToList();
        problems.AddRange(rows.Where(r => r.Length != Columns.Length).Select(r => $"row '{r[0]}' does not have {Columns.Length} cells"));
        rows = [.. rows.Where(r => r.Length == Columns.Length)];
        problems.AddRange(rows.Where(r => !RealDate(r[5], today)).Select(r => $"row '{r[0]}': Verified is not a real date that is not after today"));

        var wanted = new (string Tool, string? Value)[] { (".NET SDK", pins.Sdk), ("pac", pins.Pac.Distinct().Count() == 1 ? pins.Pac[0] : null), ("Claude Code", pins.Claude), ("Codex", pins.Codex) };
        foreach (var (tool, value) in wanted)
        {
            var row = rows.Where(r => r[0] == tool).ToList();
            if (row.Count != 1)
            {
                problems.Add($"there must be one row for {tool}");
            }
            else if (value is null)
            {
                problems.Add($"{tool}: the repository's files do not agree on one version ({string.Join(", ", pins.Pac)})");
            }
            else if (row[0][1] != value)
            {
                problems.Add($"{tool}: Version is '{row[0][1]}', but the file that pins it says '{value}'");
            }
        }

        return problems;
    }

    private static Pins RepoPins()
    {
        var smoke = Read(".github", "workflows", "skills-smoke.yml");
        var sdk = JsonDocument.Parse(Read("global.json")).RootElement.GetProperty("sdk").GetProperty("version").GetString()!;
        string Package(string name) => Regex.Match(smoke, "'" + Regex.Escape(name) + "@([^']+)'").Groups[1].Value;
        var pac = Directory.GetFiles(Path.Combine(Golden.RepoRoot, "docs", "examples"), "*.yml").SelectMany(f => PacVersion().Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value)).ToList();
        return new Pins(sdk, Package("@anthropic-ai/claude-code"), Package("@openai/codex"), pac);
    }

    // The section "## Ecosystem changes" of AGENTS.md: one section, at most ten lines with its heading, and a link to the procedure.
    internal static List<string> AgentsProblems(string text)
    {
        var sections = Sections(text).Where(s => s.StartsWith("## Ecosystem changes\n", StringComparison.Ordinal)).ToList();
        if (sections.Count != 1)
        {
            return ["AGENTS.md needs exactly one '## Ecosystem changes' section"];
        }

        var problems = new List<string>();
        if (sections[0].Split('\n').Count(l => l.Trim().Length > 0) > 10)
        {
            problems.Add("the section is longer than ten lines");
        }

        if (!sections[0].Contains("docs/ecosystem/README.md", StringComparison.Ordinal))
        {
            problems.Add("the section does not link docs/ecosystem/README.md");
        }

        return problems;
    }

    private static readonly string[] ReadmeNames = ["README.md", "sources.json", "changes.md", "compatibility.md", ".github/scripts/ecosystem-watch.sh", "Mode A", "Mode B", "Mode C"];

    internal static List<string> ReadmeProblems(string text) => [.. ReadmeNames.Where(n => !text.Contains(n, StringComparison.Ordinal)).Select(n => $"the README does not name {n}")];

    [Fact]
    public void The_register_follows_its_format()
    {
        var text = Read("docs", "ecosystem", "changes.md");

        Assert.Empty(ChangesProblems(text, Today));
        Assert.Contains("\n## Review log", text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_compatibility_table_matches_the_files_that_pin_it()
    {
        var pins = RepoPins();

        Assert.NotEmpty(pins.Pac);
        Assert.Empty(CompatibilityProblems(Read("docs", "ecosystem", "compatibility.md"), Today, pins));
    }

    [Fact]
    public void The_readme_and_the_agents_section_say_what_they_must()
    {
        Assert.Empty(ReadmeProblems(Read("docs", "ecosystem", "README.md")));
        Assert.Empty(AgentsProblems(Read("AGENTS.md")));
    }

    private static readonly DateOnly Fixed = new(2026, 11, 10);

    private const string Entry1 = "## ECO-0001: First\n- Priority: P2\n- Disposition: monitor\n- Status: open\n- Release status: preview\n- Found: 2026-11-01, mode A\n- Verified: 2026-11-02\n"
        + "- Affects: docs/HANDOFF.md, tests/example.cs\n- Sources: [Page](https://learn.microsoft.com/power-platform/example) (updated 2026-10-30), [Repo](https://github.com/microsoft/example) (observed 2026-11-01)\n"
        + "- Evidence: documented\n- Why: A reason.\n- Link: none\n\n";

    private const string Entry2 = "## ECO-0002: Second\n- Priority: P1\n- Disposition: implement now\n- Status: done\n- Release status: GA\n- Found: 2026-11-03, mode B\n- Verified: 2026-11-04\n"
        + "- Affects: none\n- Sources: [Notes](https://github.com/microsoft/example/releases) (published 2026-11-03)\n- Evidence: documented and tested\n- Why: Another reason.\n- Link: abc1234\n- Supersedes: ECO-0001\n\n";

    private const string Log = "## Review log\n\n- 2026-11-03, mode A: 17 automated read, 0 not checked, 3 by hand; ECO-0001, ECO-0002. Cheap path: pac-cli@2.14.1. No baseline: pa-cli.\n"
        + "- 2026-11-10, mode A: 17 automated read, 1 not checked, 3 by hand; no entries; cheap path: mcp-spec@2026-07-28\n";

    private const string Header = "# Register\n\nText.\n\n";

    private static readonly string Valid = Header + Entry1 + Entry2 + Log;

    [Fact]
    public void The_register_checks_accept_correct_text()
    {
        Assert.Empty(ChangesProblems(Valid, Fixed));
        Assert.Empty(ChangesProblems(Header + "## Review log\n", Fixed));
        Assert.Empty(ChangesProblems(Header + Entry1 + "## Review log\n\n- 2026-11-10, mode C: 0 automated read, 0 not checked, 0 by hand; ECO-0001\n", Fixed));
    }

    [Theory]
    [InlineData("## ECO-0002: Second", "## ECO-0003: Second")]
    [InlineData("## ECO-0002: Second", "## ECO-0001: Second")]
    [InlineData("## ECO-0002: Second", "## ECO-2: Second")]
    [InlineData("## ECO-0002: Second", "## ECO-0002:")]
    [InlineData("- Priority: P2\n- Disposition: monitor\n", "- Disposition: monitor\n- Priority: P2\n")]
    [InlineData("- Why: A reason.\n", "")]
    [InlineData("- Why: A reason.\n", "- Why: A reason.\n- Why: Twice.\n")]
    [InlineData("- Link: none\n\n## ECO-0002", "- Link: none\n- Extra: x\n\n## ECO-0002")]
    [InlineData("- Link: none\n\n## ECO-0002", "- Link: none\n- Supersedes: ECO-0001\n- Link: none\n\n## ECO-0002")]
    [InlineData("- Priority: P2", "- Priority: P4")]
    [InlineData("- Disposition: monitor", "- Disposition: later")]
    [InlineData("- Status: open", "- Status: finished")]
    [InlineData("- Release status: preview", "- Release status: beta")]
    [InlineData("- Evidence: documented\n", "- Evidence: read\n")]
    [InlineData("- Found: 2026-11-01, mode A", "- Found: 2026-11-01, mode D")]
    [InlineData("- Found: 2026-11-01, mode A", "- Found: 2026-02-30, mode A")]
    [InlineData("- Found: 2026-11-01, mode A", "- Found: 2026-11-11, mode A")]
    [InlineData("- Verified: 2026-11-02", "- Verified: 2026-11-2")]
    [InlineData("- Verified: 2026-11-02", "- Verified: 2026-12-01")]
    [InlineData("- Affects: docs/HANDOFF.md, tests/example.cs", "- Affects: docs/HANDOFF.md tests/example.cs")]
    [InlineData("(updated 2026-10-30)", "")]
    [InlineData("(updated 2026-10-30)", "(seen 2026-10-30)")]
    [InlineData("(updated 2026-10-30)", "(updated 2026-11-30)")]
    [InlineData("(https://learn.microsoft.com/power-platform/example)", "(http://learn.microsoft.com/power-platform/example)")]
    [InlineData("- Sources: [Page](https://learn.microsoft.com/power-platform/example) (updated 2026-10-30), [Repo](https://github.com/microsoft/example) (observed 2026-11-01)", "- Sources: the Learn page")]
    [InlineData("- Link: abc1234", "- Link: none")]
    [InlineData("- Disposition: implement now\n- Status: done", "- Disposition: no action\n- Status: done")]
    [InlineData("- Supersedes: ECO-0001", "- Supersedes: ECO-0002")]
    [InlineData("- Supersedes: ECO-0001", "- Supersedes: ECO-0009")]
    [InlineData("- Supersedes: ECO-0001", "- Supersedes: first")]
    [InlineData("; ECO-0001, ECO-0002. Cheap", "; ECO-0001, ECO-0009. Cheap")]
    [InlineData("; ECO-0001, ECO-0002. Cheap", "; nothing. Cheap")]
    [InlineData("; ECO-0001, ECO-0002. Cheap", "; ECO-0001, ECO-0002 and cheap")]
    [InlineData("2026-11-03, mode A: 17 automated read", "2026-11-03, mode D: 17 automated read")]
    [InlineData("2026-11-03, mode A: 17 automated read", "2026-11-31, mode A: 17 automated read")]
    [InlineData("2026-11-10, mode A: 17 automated read", "2026-11-11, mode A: 17 automated read")]
    [InlineData("3 by hand; ECO-0001", "by hand; ECO-0001")]
    [InlineData("## Review log\n\n", "## Review log\n\nA line that is not a review.\n")]
    public void The_register_checks_fail_on_planted_wrong_text(string old, string wrong)
    {
        Assert.Contains(old, Valid, StringComparison.Ordinal);
        Assert.NotEmpty(ChangesProblems(Valid.Replace(old, wrong, StringComparison.Ordinal), Fixed));
    }

    [Fact]
    public void The_register_needs_its_review_log_last_and_once()
    {
        Assert.NotEmpty(ChangesProblems(Header + Entry1 + Entry2, Fixed));
        Assert.NotEmpty(ChangesProblems(Header + Log + Entry1, Fixed));
        Assert.NotEmpty(ChangesProblems(Valid + "## Review log\n", Fixed));
    }

    private static string Table(string pac = "| pac | 2.14.1 | the examples | PowerOps was tested with 2.13.1 | none | 2026-11-10 |") =>
        "| Tool | Version | Where it is pinned | Evidence | Limits | Verified |\n|---|---|---|---|---|---|\n| .NET SDK | 10.0.100 | global.json | x | none | 2026-11-10 |\n" + pac + "\n"
        + "| Claude Code | 2.1.303 | the matrix | x | none | 2026-11-10 |\n| Codex | 0.163.0 | the matrix | x | none | 2026-11-10 |\n| Other | anything | x | x | x | 2026-11-10 |\n";

    private static readonly Pins Known = new("10.0.100", "2.1.303", "0.163.0", ["2.14.1", "2.14.1"]);

    [Fact]
    public void The_compatibility_checks_read_the_version_cell_only()
    {
        Assert.Empty(CompatibilityProblems(Table(), Fixed, Known));
        Assert.Empty(CompatibilityProblems(Table("| pac | 2.14.1 | the examples | tested with 2.12.0 and 2.13.1 | 2.15.0 is untested | 2026-11-10 |"), Fixed, Known));
        Assert.Empty(CompatibilityProblems(Table().Replace("| Other | anything |", "| Other | 9.9.9 |"), Fixed, Known));
    }

    [Theory]
    [InlineData("| pac | 2.14.1 |", "| pac | 2.13.1 |")]
    [InlineData("| .NET SDK | 10.0.100 |", "| .NET SDK | 10.0.403 |")]
    [InlineData("| Claude Code | 2.1.303 |", "| Claude Code | 2.1.322 |")]
    [InlineData("| Codex | 0.163.0 |", "| Codex | 0.167.0 |")]
    [InlineData("| Codex | 0.163.0 |", "| Codex | 0.163.0 or later |")]
    [InlineData("| Codex | 0.163.0 | the matrix | x | none | 2026-11-10 |\n", "")]
    [InlineData("| pac | 2.14.1 | the examples | PowerOps was tested with 2.13.1 | none | 2026-11-10 |\n", "")]
    [InlineData("| pac | 2.14.1 | the examples | PowerOps was tested with 2.13.1 | none | 2026-11-10 |\n", "| pac | 2.14.1 | the examples | PowerOps was tested with 2.13.1 | none | 2026-11-10 |\n| pac | 2.14.1 | again | x | none | 2026-11-10 |\n")]
    [InlineData("| Claude Code | 2.1.303 | the matrix | x | none | 2026-11-10 |", "| Claude Code | 2.1.303 | the matrix | x | none |")]
    [InlineData("| Claude Code | 2.1.303 | the matrix | x | none | 2026-11-10 |", "| Claude Code | 2.1.303 | the matrix | x | none | 2026-11-11 |")]
    [InlineData("| Other | anything | x | x | x | 2026-11-10 |", "| Other | anything | x | x | x | soon |")]
    [InlineData("| Tool | Version | Where it is pinned | Evidence | Limits | Verified |", "| Tool | Version | Pinned in | Evidence | Limits | Verified |")]
    public void The_compatibility_checks_fail_on_planted_wrong_text(string old, string wrong)
    {
        var table = Table();

        Assert.Contains(old, table, StringComparison.Ordinal);
        Assert.NotEmpty(CompatibilityProblems(table.Replace(old, wrong, StringComparison.Ordinal), Fixed, Known));
    }

    [Fact]
    public void The_compatibility_checks_fail_when_the_examples_disagree_or_the_table_is_missing()
    {
        Assert.NotEmpty(CompatibilityProblems(Table(), Fixed, Known with { Pac = ["2.14.1", "2.13.1"] }));
        Assert.NotEmpty(CompatibilityProblems(Table(), Fixed, Known with { Pac = [] }));
        Assert.NotEmpty(CompatibilityProblems("No table here.\n", Fixed, Known));
    }

    [Fact]
    public void The_real_pac_row_keeps_the_tested_version_in_evidence_and_the_pin_in_version()
    {
        var row = Read("docs", "ecosystem", "compatibility.md").Split('\n').Select(Cells).Single(c => c[0] == "pac");
        var pins = RepoPins();

        Assert.Equal(pins.Pac[0], row[1]);
        Assert.Contains("2.13.1", row[3], StringComparison.Ordinal);
        Assert.DoesNotContain("2.13.1", row[1], StringComparison.Ordinal);
    }

    [Fact]
    public void The_agents_and_readme_checks_fail_on_planted_wrong_text()
    {
        var agents = Read("AGENTS.md");
        var section = Sections(agents).Single(s => s.StartsWith("## Ecosystem changes\n", StringComparison.Ordinal));
        var readme = Read("docs", "ecosystem", "README.md");

        Assert.NotEmpty(AgentsProblems(agents.Replace("## Ecosystem changes", "## Ecosystem")));
        Assert.NotEmpty(AgentsProblems(agents.Replace(section, section.TrimEnd() + string.Concat(Enumerable.Repeat("\nAnother line.", 10)) + "\n\n")));
        Assert.NotEmpty(AgentsProblems(agents.Replace("docs/ecosystem/README.md", "docs/ecosystem")));
        Assert.NotEmpty(AgentsProblems(agents + "\n## Ecosystem changes\n\nTwice.\n"));
        Assert.NotEmpty(ReadmeProblems(readme.Replace("Mode B", "Mode X")));
        Assert.NotEmpty(ReadmeProblems(readme.Replace(".github/scripts/ecosystem-watch.sh", "the script")));
        Assert.NotEmpty(ReadmeProblems(readme.Replace("compatibility.md", "versions.md")));
    }
}