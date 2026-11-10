using System.Text.RegularExpressions;
using System.Xml.Linq;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Makes the Claude plugin's copy of a specialist (plan M82): its repo file with every repo-only reference
// rewritten, plus the team's shared rules from CLAUDE.md. Inputs use \n line endings.
public static class PluginAgents
{
    public static readonly string[] Specialists = ["powerops-apps", "powerops-automate", "powerops-bi", "powerops-design-lead", "powerops-workflow-logic", "powerops-copilot-studio"];

    public const string Sentence = "Advisory and record-only (its findings never block).";

    private static readonly string RepositoryUrl = XDocument.Load(Path.Combine(Golden.RepoRoot, "Directory.Build.props")).Descendants("RepositoryUrl").Single().Value;

    // The private research note each source names, by its file name.
    private static readonly Dictionary<string, string> Notes = new()
    {
        ["powerops-apps"] = "apps",
        ["powerops-automate"] = "automate",
        ["powerops-bi"] = "bi",
        ["powerops-design-lead"] = "design",
        ["powerops-workflow-logic"] = "workflow",
        ["powerops-copilot-studio"] = "copilot",
    };

    private const string Lead = "These are the team's rules, written for the main session that briefs you: where they say \"you\", they mean that session. Read them for the brief you should get and the reply you owe. A brief with no `tenant:` line means `tenant: offline`, and one with no `authority` line grants none. A brief that names no phase or no plan folder gets `state: blocked`, naming what is missing. The `powerops-plan` skill's files are in `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/`, its reference files in `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/references/`.";

    public static string ForPlugin(string name, string source, string claudeMd)
    {
        var file = $".claude/agents/{name}.md";
        var end = source.StartsWith("---\n", StringComparison.Ordinal) ? source.IndexOf("\n---\n", 3, StringComparison.Ordinal) : -1;
        if (end < 0)
        {
            throw new InvalidOperationException($"{file}: no frontmatter");
        }

        var front = source[..(end + 5)];
        var description = Upto(front, "\ndescription: ", file);
        front = Replace(front, description, $"{description.TrimEnd('\n')} {Sentence}\n", file);

        var body = source[(end + 5)..];
        body = Replace(body, "Follow `AGENTS.md` and the \"Power Platform specialist team\" section of `CLAUDE.md`, which hold the rules every specialist shares.", "Follow the shared rules at the end of this file, which every specialist shares.", file);
        body = Replace(body, $" Sources for the checklist are in `docs/research/agent-team-2026-11/{Notes[name]}.md`.", "", file);

        var researcher = Regex.Matches(body, @"^- `?powerops-researcher`?(.*)$", RegexOptions.Multiline);
        if (researcher.Count != 1)
        {
            throw new InvalidOperationException($"{file}: {researcher.Count} hand-off lines start with \"- powerops-researcher\", expected one");
        }

        var rest = researcher[0].Groups[1].Value;
        var cut = rest.IndexOf("unsourced.", StringComparison.Ordinal);
        if (cut < 0)
        {
            throw Missing(file, "unsourced.");
        }

        body = Replace(body, researcher[0].Value, "- The person" + rest[..(cut + "unsourced.".Length)], file);

        // bi, design-lead and copilot-studio name them on the researcher line, which the cut above drops.
        if (name is "powerops-apps" or "powerops-automate" or "powerops-workflow-logic")
        {
            var builders = Regex.Matches(body, @"^- `?powerops-(implementer|reviewer)\b.*\n", RegexOptions.Multiline);
            if (builders.Count == 0)
            {
                throw Missing(file, "- powerops-implementer or - powerops-reviewer");
            }

            foreach (var line in builders.Select(m => m.Value))
            {
                body = Replace(body, line, "", file);
            }
        }

        body = Replace(body, "The brief's fields and the reply format are in the \"Power Platform specialist team\" section of `CLAUDE.md`.", "The brief's fields and the reply format are in the shared rules below.", file);
        body = Replace(body, " (AGENTS.md rule)", "", file);
        switch (name)
        {
            case "powerops-design-lead":
                body = Replace(body, " You are not `powerops-designer`, which designs code changes to PowerOps itself.", "", file);
                body = Replace(body, "Read `AGENTS.md`, `skills/powerops-plan/SKILL.md` and `skills/powerops-plan/references/route.md` first", "Read `skills/powerops-plan/SKILL.md` and `skills/powerops-plan/references/route.md` first", file);
                break;
            case "powerops-copilot-studio":
                body = Replace(body, "step 11 of `docs/HANDOFF.md`", $"step 11 of PowerOps' HANDOFF guide ({RepositoryUrl}/blob/main/docs/HANDOFF.md)", file);
                break;
            case "powerops-automate":
                body = Replace(body, "the flow rules in `docs/rules/`", "the flow rules (`powerops rules`)", file);
                break;
            case "powerops-bi":
                body = Replace(body, "(`powerbi-authoring-local`, registered with `--readonly`, which is what blocks its writes)", "(`powerbi-authoring-local`, which blocks writes only when it is registered with `--readonly`; if it isn't, or you can't tell, say so under Not checked and use only the reads listed here)", file);
                break;
        }

        // Every such path is to the powerops-plan skill; a missed one fails the repo-only reference test.
        body = body
            .Replace("`skills/", "`${CLAUDE_PLUGIN_ROOT}/skills/", StringComparison.Ordinal)
            .Replace("`references/", "`${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/references/", StringComparison.Ordinal);

        return front + body + "\n## Shared rules\n\n" + SharedRules(claudeMd);
    }

    // The "Power Platform specialist team" section of CLAUDE.md, rewritten for a plugin user, after a lead-in for the specialist.
    public static string SharedRules(string claudeMd)
    {
        const string file = "CLAUDE.md";
        var lines = claudeMd.Split('\n');
        var start = Array.IndexOf(lines, "### Power Platform specialist team");
        var stop = start < 0 ? -1 : Array.FindIndex(lines, start, l => l.StartsWith("Saved workflows in", StringComparison.Ordinal));
        if (stop < 0)
        {
            throw Missing(file, start < 0 ? "### Power Platform specialist team" : "Saved workflows in");
        }

        var s = string.Join('\n', lines[(start + 1)..stop]).Trim('\n') + "\n";
        s = Replace(s, Upto(s, "Their research and sources are private notes", file), "No research notes ship with the plugin: a specialist lists each claim it can't source on Microsoft Learn under hand-offs, for the person to check, or marks it unsourced.\n", file);
        s = Replace(s, Upto(s, "The specialists load this file too", file) + "\n", "", file);
        s = Replace(s, Upto(s, "| A Microsoft claim to check |", file), "| A Microsoft claim to check | the person, from the specialist's hand-offs | none |\n", file);
        s = Replace(s, Upto(s, "| A change to PowerOps itself |", file), "", file);
        s = Replace(s, "`subagent_type: \"<name>\"`", "`subagent_type: \"powerops:<name>\"`", file);
        s = Replace(s, "each claim for powerops-researcher", "each claim for the person to check", file);
        s = Replace(s, "In this repo the routing table", "Under PowerOps the routing table", file);
        s = Replace(s, Upto(s, "- Record-only until measured", file),
            "- Record-only until measured: specialist findings never block a plan, a build or an import. No evaluation has measured what these specialists find; treat their findings as advice for the person.\n"
            + "- Never run `powerops deploy apply --apply` or `powerops settings apply --apply`, or call the MCP tool `deploy_apply` with `apply: true`: only the person says yes to an import, in the tool's form or by running the command.\n", file);
        return Lead + "\n\n" + s;
    }

    private static string Replace(string text, string old, string replacement, string file) =>
        text.Contains(old, StringComparison.Ordinal) ? text.Replace(old, replacement, StringComparison.Ordinal) : throw Missing(file, old);

    // From the first `start` to the end of its line, newline included.
    private static string Upto(string text, string start, string file)
    {
        var i = text.IndexOf(start, StringComparison.Ordinal);
        if (i < 0)
        {
            throw Missing(file, start);
        }

        var end = text.IndexOf('\n', i + 1);
        return end < 0 ? text[i..] : text[i..(end + 1)];
    }

    private static InvalidOperationException Missing(string file, string text) =>
        new($"{file}: \"{text}\" not found; update PluginAgents to match the source");
}