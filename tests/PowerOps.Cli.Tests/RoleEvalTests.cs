using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using PowerOps.Core.Plans;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Checks the cases, the key and the baseline in tests/eval/roles/, and the two role skills in skills/ that the cases are run against. Each check that
// reads a file takes its text, so the last tests can feed it good and bad samples.
public sealed partial class RoleEvalTests
{
    private static readonly string Roles = Path.Combine(Golden.RepoRoot, "tests", "eval", "roles");

    private static readonly string Cases = Path.Combine(Roles, "cases");

    private static readonly string Pilot = Path.Combine(Roles, "pilot");

    private static readonly string[] Drafts = ["powerops-review-security", "powerops-verify"];

    private static readonly string[] PlanSections =
    [
        "Goal", "Users", "Data", "Automation", "Screens", "Environments and route", "Solution and publisher",
        "Constraints", "Assumptions", "Open questions", "Questions for your admin", "Steps",
    ];

    private static readonly string[] Points =
    [
        "data policies", "security roles", "app users", "sharing", "connection references", "secrets in environment variables", "column security",
        "Power Pages table permissions and web roles", .. Enumerable.Range(1, 10).Select(n => $"SE:{n:00}"), "plan checklist",
    ];

    // The commands of row 5, run from the case folder with these relative paths.
    private static readonly string[][] BaselineCommands =
    [
        ["analyze", "solution/current"],
        ["test", "solution/current", "--tests", "flowtests"],
        ["diff", "solution/previous", "solution/current"],
        ["settings", "validate", "solution/current", "--settings", "settings.json"],
    ];

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^\| `([^`]+)` \| `([0-9a-f]{40})` \|\s*$", RegexOptions.Multiline)]
    private static partial Regex PinnedRow();

    [GeneratedRegex(@"^- \d{4}-\d{2}-\d{2}: comparison recorded: `([^`]+)` is now blob `([0-9a-f]{40})`", RegexOptions.Multiline)]
    private static partial Regex RecordedLine();

    [GeneratedRegex(@"^[ \t]*```\w*\n(.*?)^[ \t]*```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex Fence();

    [GeneratedRegex(@"`([^`\n]+)`")]
    private static partial Regex Span();

    [GeneratedRegex(@"Bash\((powerops [a-z][a-z -]*[a-z]) \*\)")]
    private static partial Regex AllowedEntry();

    [GeneratedRegex(@"\b(flaw|flawed|planted|seeded|eval|m38|key|keys|baseline)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Marker();

    [GeneratedRegex(@"claude-(opus|sonnet|haiku|fable|\d)|gpt-\d|gemini-\d|\b(opus|sonnet|haiku)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ModelId();

    [GeneratedRegex(@"https?://([A-Za-z0-9.-]+)")]
    private static partial Regex UrlHost();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@([A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,})")]
    private static partial Regex EmailDomain();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex Guid();

    private sealed record Flaw(string Id, string Case, string Pass, string Point, string Severity, string Sentence, string? Baseline);

    // ---- the key ----

    private static List<Flaw> ReadKey(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("the key is not a JSON array");
        }

        return [.. doc.RootElement.EnumerateArray().Select(e =>
        {
            string? Field(string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            return new Flaw(Field("id") ?? "", Field("case") ?? "", Field("pass") ?? "", Field("point") ?? "", Field("severity") ?? "", Field("sentence") ?? "", Field("baseline"));
        })];
    }

    // Each entry: a unique id, a case that exists, a pass that fits the case, one of the points, a severity and a sentence.
    internal static List<string> KeyProblems(string json, IReadOnlyCollection<string> cases)
    {
        List<Flaw> key;
        try
        {
            key = ReadKey(json);
        }
        catch (Exception e) when (e is JsonException or InvalidDataException)
        {
            return [e.Message];
        }

        var problems = new List<string>();
        problems.AddRange(key.GroupBy(f => f.Id).Where(g => g.Key.Length == 0 || g.Count() > 1).Select(g => $"id '{g.Key}' is empty or used {g.Count()} times"));
        foreach (var f in key)
        {
            var at = $"{f.Id}:";
            if (!cases.Contains(f.Case))
            {
                problems.Add($"{at} case '{f.Case}' is not a case folder");
            }

            if (f.Pass is not ("plan" or "security-solution" or "verify"))
            {
                problems.Add($"{at} pass '{f.Pass}' is not plan, security-solution or verify");
            }
            else if ((f.Pass == "plan") != f.Case.StartsWith("plan-", StringComparison.Ordinal))
            {
                problems.Add($"{at} pass '{f.Pass}' does not fit case '{f.Case}'");
            }

            if (!Points.Contains(f.Point))
            {
                problems.Add($"{at} point '{f.Point}' is not one of the points");
            }

            if (f.Severity is not ("High" or "Medium" or "Low"))
            {
                problems.Add($"{at} severity '{f.Severity}' is not High, Medium or Low");
            }

            if (f.Sentence.Trim().Length < 20)
            {
                problems.Add($"{at} the sentence is missing or too short");
            }

            if (f.Pass == "plan" && f.Baseline is not null)
            {
                problems.Add($"{at} a plan flaw has no baseline field");
            }
        }

        return problems;
    }

    // Every solution flaw says shown or not-shown.
    internal static List<string> BaselineFieldProblems(string json) =>
        [.. ReadKey(json).Where(f => f.Pass != "plan" && f.Baseline is not ("shown" or "not-shown")).Select(f => $"{f.Id}: baseline is '{f.Baseline}', not shown or not-shown")];

    // At least this many flaws of each solution pass must be not-shown, so the baseline leaves room for a role to add something.
    internal static List<string> NotShownProblems(string json, int least) =>
        [.. ReadKey(json).Where(f => f.Pass != "plan").GroupBy(f => f.Pass).Where(g => g.Count(f => f.Baseline == "not-shown") < least)
            .Select(g => $"{g.Key}: {g.Count(f => f.Baseline == "not-shown")} of {g.Count()} flaws are not-shown, at least {least} needed")];

    // How many flaws each case holds (perCase, or the case's entry in `exceptions`), and how the cases and passes add up.
    internal static List<string> CountProblems(string json, IReadOnlyCollection<string> planCases, IReadOnlyCollection<string> solutionCases, int perCase, int flawedPlan, int flawedSolution, int? security, int? verify, IReadOnlyDictionary<string, int>? exceptions = null)
    {
        var key = ReadKey(json);
        var problems = new List<string>();
        var perCaseCount = key.GroupBy(f => f.Case).ToDictionary(g => g.Key, g => g.Count());
        int Expected(string name) => exceptions is not null && exceptions.TryGetValue(name, out var e) ? e : perCase;
        foreach (var name in planCases.Concat(solutionCases))
        {
            var n = perCaseCount.GetValueOrDefault(name);
            if (n != 0 && n != Expected(name))
            {
                problems.Add($"{name} has {n} flaws, expected {Expected(name)} or none");
            }
        }

        var flawedPlans = planCases.Count(c => perCaseCount.GetValueOrDefault(c) == Expected(c));
        var flawedSolutions = solutionCases.Count(c => perCaseCount.GetValueOrDefault(c) == Expected(c));
        if (flawedPlans != flawedPlan)
        {
            problems.Add($"{flawedPlans} plan cases have flaws, expected {flawedPlan}");
        }

        if (flawedSolutions != flawedSolution)
        {
            problems.Add($"{flawedSolutions} solution cases have flaws, expected {flawedSolution}");
        }

        var planFlaws = key.Count(f => f.Pass == "plan");
        var planExpected = planCases.Where(c => perCaseCount.GetValueOrDefault(c) == Expected(c)).Sum(Expected);
        if (planFlaws != planExpected)
        {
            problems.Add($"{planFlaws} plan flaws, expected {planExpected}");
        }

        var solutionFlaws = key.Count(f => f.Pass != "plan");
        var solutionExpected = solutionCases.Where(c => perCaseCount.GetValueOrDefault(c) == Expected(c)).Sum(Expected);
        if (solutionFlaws != solutionExpected)
        {
            problems.Add($"{solutionFlaws} solution flaws, expected {solutionExpected}");
        }

        if (security is { } s && key.Count(f => f.Pass == "security-solution") != s)
        {
            problems.Add($"{key.Count(f => f.Pass == "security-solution")} security-solution flaws, expected {s}");
        }

        if (verify is { } v && key.Count(f => f.Pass == "verify") != v)
        {
            problems.Add($"{key.Count(f => f.Pass == "verify")} verify flaws, expected {v}");
        }

        return problems;
    }

    // ---- the cases ----

    private static string[] CaseNames(string folder, string prefix) =>
        Directory.Exists(folder) ? [.. Directory.GetDirectories(folder, prefix + "-*").Select(d => Path.GetFileName(d)!).Order(StringComparer.Ordinal)] : [];

    private static string Rel(string root, string file) => Path.GetRelativePath(root, file).Replace('\\', '/');

    private static string Read(string file) => File.ReadAllText(file).Replace("\r\n", "\n");

    // The one plan folder of a case, or null.
    private static string? PlanFolder(string caseDir)
    {
        var plans = Path.Combine(caseDir, "docs", "plans");
        return Directory.Exists(plans) && Directory.GetDirectories(plans) is [var only] ? only : null;
    }

    // The sections of plan.md in order, after the header: "## " headings.
    internal static List<string> SectionProblems(string plan)
    {
        var found = plan.Split('\n').Where(l => l.StartsWith("## ", StringComparison.Ordinal)).Select(l => l[3..].TrimEnd()).ToList();
        return found.SequenceEqual(PlanSections) ? [] : [$"sections are [{string.Join("; ", found)}], expected [{string.Join("; ", PlanSections)}]"];
    }

    // A plan case: approved, review pending, not signed, the sections in order, and decisions.md with a round and a log.
    internal static List<string> PlanCaseProblems(string caseDir)
    {
        var folder = PlanFolder(caseDir);
        if (folder is null)
        {
            return ["docs/plans does not hold exactly one plan folder"];
        }

        var problems = new List<string>();
        var files = Directory.GetFiles(caseDir, "*", SearchOption.AllDirectories).Select(f => Rel(caseDir, f)).Order(StringComparer.Ordinal).ToList();
        var name = Path.GetFileName(folder);
        if (!files.SequenceEqual([$"docs/plans/{name}/decisions.md", $"docs/plans/{name}/plan.md"]))
        {
            problems.Add("a plan case holds plan.md and decisions.md and nothing else, but has " + string.Join(", ", files));
            return problems;
        }

        var plan = Read(Path.Combine(folder, "plan.md"));
        var doc = PlanDocument.Parse(plan);
        if (doc.Header is not { } h)
        {
            problems.Add("malformed header: " + doc.Error);
        }
        else
        {
            if (h.Status != "approved" || h.Review != "pending" || h.SignedOff is not null || h.SignedOffHash is not null || h.Environment is not null)
            {
                problems.Add($"header is {h.Status}, review {h.Review}; expected approved, pending, not signed, no environment");
            }
        }

        problems.AddRange(SectionProblems(plan));
        var decisions = Read(Path.Combine(folder, "decisions.md"));
        if (!decisions.StartsWith($"# Decisions: {name}\n", StringComparison.Ordinal) || !decisions.Contains("\n## Round 1\n", StringComparison.Ordinal) || !decisions.Contains("\n## Log\n", StringComparison.Ordinal))
        {
            problems.Add("decisions.md lacks its title, Round 1 or Log");
        }

        return problems;
    }

    // Placed values are fake: contoso.example, example.invalid and all-zero GUIDs (a last digit may differ), in the files a reviewer reads as text.
    internal static List<string> FakeValueProblems(string path, string text)
    {
        var problems = new List<string>();
        problems.AddRange(UrlHost().Matches(text).Select(m => m.Groups[1].Value).Where(h => !(h == "contoso.example" || h.EndsWith(".contoso.example", StringComparison.Ordinal))).Select(h => $"{path}: URL host '{h}' is not contoso.example"));
        problems.AddRange(EmailDomain().Matches(text).Select(m => m.Groups[1].Value).Where(d => d != "example.invalid").Select(d => $"{path}: email domain '{d}' is not example.invalid"));
        problems.AddRange(Guid().Matches(text).Select(m => m.Value).Where(g => !Regex.IsMatch(g, "^0{8}-0{4}-0{4}-0{4}-0{11}[0-9]$")).Select(_ => $"{path}: a GUID that is not all zeros"));
        return problems;
    }

    // No word that marks the case's role in a file name or in its text.
    internal static List<string> MarkerProblems(string path, string text) =>
        [.. Marker().Matches(path + "\n" + text).Select(m => $"{path}: the word '{m.Value}'").Distinct()];

    internal static List<string> ModelIdProblems(string path, string text) =>
        [.. ModelId().Matches(text).Select(m => $"{path}: '{m.Value}' looks like a model id").Distinct()];

    // Every file of a case folder (and of the pilot) through the file-wide checks.
    private static IEnumerable<string> CaseFiles(string root) =>
        Directory.Exists(root) ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal) : [];

    private static string SeedV1() => Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1");

    // These cases start from the seed plus security roles and a model-driven app, so their root components can stay the same while a role and the app change.
    private static readonly string[] RoleAppCases = ["solution-07", "solution-08"];

    private static readonly string[] RoleAppFolders = ["Roles/", "AppModules/", "AppModuleSiteMaps/"];

    // The RootComponent elements of two Other/Solution.xml texts, in order, must be the same.
    internal static List<string> RootComponentProblems(string previousSolutionXml, string currentSolutionXml)
    {
        static List<string> Roots(string xml) =>
            [.. XDocument.Parse(xml).Descendants("RootComponent").Select(e => e.ToString(SaveOptions.DisableFormatting))];

        return Roots(previousSolutionXml).SequenceEqual(Roots(currentSolutionXml)) ? [] : ["the root components of solution/previous and solution/current differ"];
    }

    // The previous Other/Solution.xml is the seed's, apart from its version and the root components it adds.
    internal static List<string> PreviousManifestProblems(string seedSolutionXml, string previousSolutionXml)
    {
        static string Key(XElement e) => e.ToString(SaveOptions.DisableFormatting);
        var seed = XDocument.Parse(seedSolutionXml);
        var previous = XDocument.Parse(previousSolutionXml);
        var seedRoots = seed.Descendants("RootComponent").Select(Key).ToHashSet(StringComparer.Ordinal);
        seed.Descendants("Version").Remove();
        previous.Descendants("Version").Remove();
        previous.Descendants("RootComponent").Where(e => !seedRoots.Contains(Key(e))).Remove();
        return seed.ToString(SaveOptions.DisableFormatting) == previous.ToString(SaveOptions.DisableFormatting)
            ? [] : ["solution/previous's Other/Solution.xml differs from the seed's beyond its version and added root components"];
    }

    private static string SolutionVersion(string solutionFolder) =>
        Regex.Match(Read(Path.Combine(solutionFolder, "Other", "Solution.xml")), @"<Version>([\d.]+)</Version>").Groups[1].Value;

    // A solution case: a plan signed by the tool and then set to implementing, the clean previous solution, a current one with a higher version, settings and flow tests.
    internal static List<string> SolutionCaseProblems(string caseDir)
    {
        var problems = new List<string>();
        if (PlanFolder(caseDir) is not { } folder)
        {
            return ["docs/plans does not hold exactly one plan folder"];
        }

        var planFile = Path.Combine(folder, "plan.md");
        if (!File.Exists(planFile) || !File.Exists(Path.Combine(folder, "decisions.md")) || Directory.GetFiles(folder).Length != 2)
        {
            return ["the plan folder holds plan.md and decisions.md and nothing else"];
        }

        var report = PlanChecker.Check(planFile, caseDir);
        problems.AddRange(report.Violations.Select(v => "plan: " + v));
        if (report.Status != "implementing" || report.Review != "skipped" || report.SignedOff is null)
        {
            problems.Add($"plan is {report.Status}, review {report.Review}; expected implementing, skipped, signed");
        }

        problems.AddRange(SectionProblems(Read(planFile)));
        var seed = SeedV1();
        var previous = Path.Combine(caseDir, "solution", "previous");
        var current = Path.Combine(caseDir, "solution", "current");
        var seedFiles = CaseFiles(seed).Select(f => Rel(seed, f)).ToList();
        var previousFiles = CaseFiles(previous).Select(f => Rel(previous, f)).ToList();
        if (!RoleAppCases.Contains(Path.GetFileName(caseDir)))
        {
            if (!previousFiles.SequenceEqual(seedFiles) || seedFiles.Any(f => Read(Path.Combine(seed, f)) != Read(Path.Combine(previous, f))))
            {
                problems.Add("solution/previous is not a copy of tests/fixtures/powerops-seed/v1");
            }
        }
        else
        {
            // The seed's files, Other/Solution.xml aside, byte for byte; anything else only under the role and app folders.
            const string Manifest = "Other/Solution.xml";
            if (seedFiles.Any(f => f != Manifest && (!previousFiles.Contains(f) || Read(Path.Combine(seed, f)) != Read(Path.Combine(previous, f))))
                || previousFiles.Except(seedFiles).Any(f => !RoleAppFolders.Any(d => f.StartsWith(d, StringComparison.Ordinal))))
            {
                problems.Add("solution/previous is not tests/fixtures/powerops-seed/v1 plus roles and a model-driven app");
            }

            if (File.Exists(Path.Combine(previous, "Other", "Solution.xml")) && File.Exists(Path.Combine(current, "Other", "Solution.xml")))
            {
                problems.AddRange(RootComponentProblems(Read(Path.Combine(previous, "Other", "Solution.xml")), Read(Path.Combine(current, "Other", "Solution.xml"))));
                problems.AddRange(PreviousManifestProblems(Read(Path.Combine(seed, "Other", "Solution.xml")), Read(Path.Combine(previous, "Other", "Solution.xml"))));
            }
        }

        if (!File.Exists(Path.Combine(current, "Other", "Solution.xml")) || !File.Exists(Path.Combine(previous, "Other", "Solution.xml")))
        {
            problems.Add("solution/current or solution/previous has no Other/Solution.xml");
        }
        else if (!Version.TryParse(SolutionVersion(current), out var raised) || !Version.TryParse(SolutionVersion(previous), out var old) || raised <= old)
        {
            problems.Add("solution/current does not have a higher version than solution/previous");
        }

        if (!File.Exists(Path.Combine(caseDir, "settings.json")))
        {
            problems.Add("settings.json is missing");
        }

        if (Directory.Exists(Path.Combine(caseDir, "flowtests")) is false || Directory.GetFiles(Path.Combine(caseDir, "flowtests"), "*.flowtest.json").Length == 0)
        {
            problems.Add("flowtests/ holds no *.flowtest.json");
        }

        return problems;
    }

    // ---- the baseline ----

    private static (int Code, string Out, string Err) RunCli(string workingDirectory, string[] args)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromSeconds(120)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("powerops did not finish.");
        }

        return (process.ExitCode, stdout, stderr.GetAwaiter().GetResult());
    }

    // Line endings and path separators are not part of the comparison, so the baseline holds on every OS.
    internal static string NormalizeBaseline(string text) => text.Replace("\r\n", "\n").Replace('\\', '/');

    private static string LiveBaseline(string caseDir)
    {
        var text = new StringBuilder();
        foreach (var args in BaselineCommands)
        {
            // shortcut: tests/eval/roles/baseline/*.txt were recorded before M92 hid values by default; M96 regenerates them and drops --show-values here
            var (code, output, error) = RunCli(caseDir, args[0] == "diff" ? [.. args, "--show-values"] : args);
            text.Append("$ powerops ").Append(string.Join(' ', args)).Append('\n').Append(output);
            if (output.Length > 0 && !output.EndsWith('\n'))
            {
                text.Append('\n');
            }

            if (error.Length > 0)
            {
                text.Append("stderr:\n").Append(error).Append(error.EndsWith('\n') ? "" : "\n");
            }

            text.Append("exit code: ").Append(code).Append("\n\n");
        }

        return NormalizeBaseline(text.ToString());
    }

    // ---- the drafts ----

    // Only name, description, license and compatibility; the name is the folder's.
    internal static List<string> FrontMatterProblems(string skillText, string folder)
    {
        var lines = skillText.Replace("\r\n", "\n").Split('\n');
        var close = lines[0] == "---" ? Array.IndexOf(lines, "---", 1) : -1;
        if (close < 0)
        {
            return ["no front matter between two --- lines"];
        }

        var header = lines.Skip(1).Take(close - 1).Where(l => l.Length > 0 && !char.IsWhiteSpace(l[0]) && l.Contains(':', StringComparison.Ordinal)).ToList();
        var fields = header.GroupBy(l => l[..l.IndexOf(':')]).ToDictionary(g => g.Key, g => g.Last()[(g.Last().IndexOf(':') + 1)..].Trim());
        var problems = new List<string>();
        if (!fields.Keys.Order(StringComparer.Ordinal).SequenceEqual(["compatibility", "description", "license", "name"]))
        {
            problems.Add("front matter keys are [" + string.Join(", ", fields.Keys.Order(StringComparer.Ordinal)) + "], expected name, description, license, compatibility");
            return problems;
        }

        if (fields["name"] != folder || !NamePattern().IsMatch(fields["name"]))
        {
            problems.Add($"name '{fields["name"]}' is not the folder name '{folder}' in lowercase words with hyphens");
        }

        if (fields["description"].Length is 0 or > 1024)
        {
            problems.Add("description is empty or over 1024 characters");
        }

        if (fields["license"].Length == 0 || fields["compatibility"].Length == 0)
        {
            problems.Add("license or compatibility is empty");
        }

        return problems;
    }

    // A fenced block with the seven header lines on consecutive lines, in this order, each at the start of its line.
    internal static List<string> HeaderLineProblems(string skillText)
    {
        string[] prefixes = ["plan-hash: ", "model: ", "fresh-context: ", "state: ", "input: ", "solution-hash: ", "deploy-hash: "];
        var found = Fence().Matches(skillText.Replace("\r\n", "\n")).Select(m => m.Groups[1].Value.Split('\n')).Any(lines =>
            Enumerable.Range(0, Math.Max(0, lines.Length - prefixes.Length + 1)).Any(i => prefixes.Select((p, k) => lines[i + k].StartsWith(p, StringComparison.Ordinal)).All(x => x)));
        return found ? [] : ["no fenced block has the lines plan-hash: , model: , fresh-context: , state: , input: , solution-hash: and deploy-hash: one after the other"];
    }

    // The first code span of each bullet under "## Commands you may run" that starts with "powerops ". Null when the section is missing.
    internal static List<string>? ListedCommands(string skillText)
    {
        var lines = skillText.Replace("\r\n", "\n").Split('\n');
        var start = Array.IndexOf(lines, "## Commands you may run");
        if (start < 0)
        {
            return null;
        }

        return [.. lines.Skip(start + 1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal))
            .Where(l => l.StartsWith("- ", StringComparison.Ordinal) || l.StartsWith("* ", StringComparison.Ordinal))
            .Select(l => Span().Match(l)).Where(m => m.Success).Select(m => m.Groups[1].Value.Trim()).Where(c => c.StartsWith("powerops ", StringComparison.Ordinal))];
    }

    // The commands of the Claude allowlist in run.sh: the words between Bash( and the trailing *).
    internal static List<string> Allowlist(string runScript) => [.. AllowedEntry().Matches(runScript).Select(m => m.Groups[1].Value).Distinct()];

    // Listed commands, plan facts aside, that no allowlist entry covers.
    internal static List<string> NotAllowed(IEnumerable<string> listed, IReadOnlyCollection<string> allowlist) =>
        [.. listed.Where(c => c != "powerops plan facts" && !c.StartsWith("powerops plan facts ", StringComparison.Ordinal))
            .Where(c => !allowlist.Any(a => c == a || c.StartsWith(a + " ", StringComparison.Ordinal)))];

    private static string Draft(string name) => Path.Combine(Golden.RepoRoot, "skills", name, "SKILL.md");

    // ---- tests of the real files ----

    private static void KeyChecks(string keyFile, string root, string[] planCases, string[] solutionCases, int perCase, int flawedPlan, int flawedSolution, int? security, int? verify, IReadOnlyDictionary<string, int>? exceptions = null)
    {
        var json = File.ReadAllText(keyFile);
        var cases = planCases.Concat(solutionCases).ToList();

        Assert.Equal(planCases.Length, CaseNames(root, "plan").Length);
        Assert.Equal(solutionCases.Length, CaseNames(root, "solution").Length);
        Assert.Empty(KeyProblems(json, cases));
        Assert.Empty(CountProblems(json, planCases, solutionCases, perCase, flawedPlan, flawedSolution, security, verify, exceptions));
    }

    private static string[] MainPlans => [.. Enumerable.Range(1, 14).Select(n => $"plan-{n:00}")];

    private static string[] MainSolutions => [.. Enumerable.Range(1, 8).Select(n => $"solution-{n:00}")];

    [Fact]
    public void Every_key_entry_names_a_real_case_and_pass_and_every_case_and_pass_has_its_flaw_count()
    {
        KeyChecks(Path.Combine(Roles, "key.json"), Cases, MainPlans, MainSolutions, perCase: 3, flawedPlan: 10, flawedSolution: 6, security: 8, verify: 8, RoleAppCases.ToDictionary(c => c, _ => 2));
    }

    [Fact]
    public void Every_pilot_key_entry_names_a_real_case_and_pass_and_each_pilot_case_has_one_flaw()
    {
        KeyChecks(Path.Combine(Pilot, "key.json"), Pilot, ["plan-pilot"], ["solution-pilot"], perCase: 1, flawedPlan: 1, flawedSolution: 1, security: null, verify: null);
    }

    [Fact]
    public void Every_solution_flaw_has_its_baseline_field_and_at_least_4_in_each_pass_are_not_shown()
    {
        var main = File.ReadAllText(Path.Combine(Roles, "key.json"));
        var pilot = File.ReadAllText(Path.Combine(Pilot, "key.json"));

        Assert.Empty(BaselineFieldProblems(main));
        Assert.Empty(BaselineFieldProblems(pilot));
        Assert.Empty(NotShownProblems(main, least: 4));
    }

    [Fact]
    public void Every_solution_case_has_a_baseline_file_that_matches_the_live_output()
    {
        var folders = CaseNames(Cases, "solution").Select(n => (Name: n, Dir: Path.Combine(Cases, n))).Concat(CaseNames(Pilot, "solution").Select(n => (Name: n, Dir: Path.Combine(Pilot, n)))).ToList();
        var files = Directory.Exists(Path.Combine(Roles, "baseline")) ? Directory.GetFiles(Path.Combine(Roles, "baseline"), "*.txt").Select(f => Path.GetFileNameWithoutExtension(f)!).Order(StringComparer.Ordinal).ToList() : [];

        Assert.Equal(9, folders.Count);
        Assert.Equal(folders.Select(f => f.Name).Order(StringComparer.Ordinal), files);
        foreach (var (name, dir) in folders)
        {
            Assert.Equal(NormalizeBaseline(File.ReadAllText(Path.Combine(Roles, "baseline", name + ".txt"))), LiveBaseline(dir));
        }
    }

    [Fact]
    public void The_plan_cases_parse_with_the_plan_document_and_hold_only_fake_values()
    {
        var cases = CaseNames(Cases, "plan").Select(n => Path.Combine(Cases, n)).Concat(CaseNames(Pilot, "plan").Select(n => Path.Combine(Pilot, n))).ToList();

        Assert.Equal(15, cases.Count);
        Assert.All(cases, c => Assert.Empty(PlanCaseProblems(c)));
        Assert.All(cases.SelectMany(CaseFiles), f => Assert.Empty(FakeValueProblems(f, Read(f))));
    }

    [Fact]
    public void A_plan_case_without_a_flaw_holds_no_url_guid_or_email()
    {
        var key = ReadKey(File.ReadAllText(Path.Combine(Roles, "key.json")));
        var clean = CaseNames(Cases, "plan").Where(n => key.All(f => f.Case != n)).ToList();

        Assert.Equal(4, clean.Count);
        foreach (var name in clean)
        {
            var dir = Path.Combine(Cases, name);
            Assert.Empty(PlanChecker.Check(Path.Combine(PlanFolder(dir)!, "plan.md"), dir).Violations);
        }
    }

    [Fact]
    public void The_solution_case_plans_are_signed_and_implementing_and_the_cases_are_complete()
    {
        var cases = CaseNames(Cases, "solution").Select(n => Path.Combine(Cases, n)).Concat(CaseNames(Pilot, "solution").Select(n => Path.Combine(Pilot, n))).ToList();

        Assert.Equal(9, cases.Count);
        Assert.All(cases, c => Assert.Empty(SolutionCaseProblems(c)));
        Assert.All(cases.SelectMany(c => new[] { Path.Combine(c, "docs"), Path.Combine(c, "settings.json"), Path.Combine(c, "flowtests") }).SelectMany(CaseFilesOrFile), f => Assert.Empty(FakeValueProblems(f, Read(f))));
    }

    private static IEnumerable<string> CaseFilesOrFile(string path) => File.Exists(path) ? [path] : CaseFiles(path);

    [Fact]
    public void Analyze_test_and_settings_validate_report_nothing_on_the_clean_solution_cases()
    {
        var key = ReadKey(File.ReadAllText(Path.Combine(Roles, "key.json")));
        var clean = CaseNames(Cases, "solution").Where(n => key.All(f => f.Case != n)).ToList();

        Assert.Equal(2, clean.Count);
        foreach (var name in clean)
        {
            var dir = Path.Combine(Cases, name);
            var analyze = Run("analyze", Path.Combine(dir, "solution", "current"));
            var test = Run("test", Path.Combine(dir, "solution", "current"), "--tests", Path.Combine(dir, "flowtests"));
            var settings = Run("settings", "validate", Path.Combine(dir, "solution", "current"), "--settings", Path.Combine(dir, "settings.json"));

            Assert.Equal(0, analyze.Code);
            Assert.Contains("no findings", analyze.Out);
            Assert.Contains("0 errors, 0 warnings", analyze.Out);
            Assert.Equal(0, test.Code);
            Assert.Equal(0, settings.Code);
            Assert.Contains("no problems", settings.Out);
        }
    }

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    [Fact]
    public void No_case_file_or_name_holds_a_word_that_marks_its_role()
    {
        var files = CaseFiles(Cases).Concat(CaseFiles(Pilot)).Where(f => !Path.GetFileName(f).Equals("key.json", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.Empty(MarkerProblems(Rel(Roles, f), Read(f))));
    }

    [Fact]
    public void No_committed_file_of_the_evaluation_holds_a_model_id()
    {
        var files = CaseFiles(Roles).ToList();

        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.Empty(ModelIdProblems(Rel(Roles, f), Read(f))));
    }

    [Fact]
    public void The_drafts_have_portable_front_matter_and_the_seven_header_lines()
    {
        foreach (var name in Drafts)
        {
            var text = File.ReadAllText(Draft(name));

            Assert.Empty(FrontMatterProblems(text, name));
            Assert.Empty(HeaderLineProblems(text));
        }
    }

    [Fact]
    public void The_drafts_name_no_command_or_flag_that_does_not_exist()
    {
        foreach (var name in Drafts)
        {
            var text = File.ReadAllText(Draft(name)).Replace("\r\n", "\n");

            Assert.Empty(DocsDriftTests.UnknownCommands(text));
            Assert.Empty(DocsDriftTests.UnknownFlags(text));
        }
    }

    [Fact]
    public void Every_command_the_drafts_list_is_in_the_run_scripts_allowlist()
    {
        var allowlist = Allowlist(File.ReadAllText(Path.Combine(Roles, "run.sh")));

        Assert.NotEmpty(allowlist);
        foreach (var name in Drafts)
        {
            var listed = ListedCommands(File.ReadAllText(Draft(name)));

            Assert.NotNull(listed);
            Assert.NotEmpty(listed);
            Assert.Empty(NotAllowed(listed, allowlist));
        }
    }

    // ---- run.sh ----

    [Fact]
    public void The_run_script_lists_a_case_without_a_model_and_names_no_key_or_baseline_file()
    {
        Tools.Require("bash");
        var script = Path.Combine(Roles, "run.sh");
        var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
        if (posix)
        {
            Assert.True(File.GetUnixFileMode(script).HasFlag(UnixFileMode.UserExecute));
        }

        Assert.Equal(0, Bash("-n", script).Code);
        var text = File.ReadAllText(script);
        Assert.Contains("set -euo pipefail", text);
        Assert.DoesNotMatch(@"(?m)(^|[;&|(])\s*eval\s", text); // the command, not the tests/eval folder
        Assert.DoesNotContain("mapfile", text);

        // A copy in a folder with a space, as a maintainer's repo path can have.
        var temp = Directory.CreateTempSubdirectory("powerops-roleeval-").FullName;
        try
        {
            foreach (var name in new[] { "plan-pilot", "solution-pilot" })
            {
                var source = Path.Combine(Pilot, name);
                var copy = Path.Combine(temp, "a case", name);
                foreach (var file in CaseFiles(source))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(copy, Rel(source, file)))!);
                    File.Copy(file, Path.Combine(copy, Rel(source, file)));
                }

                var listed = Bash(script, "--list", copy);
                var expected = CaseFiles(copy).Select(f => Rel(copy, f)).ToList();

                Assert.Equal(0, listed.Code);
                Assert.Equal(expected, listed.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList());
                Assert.DoesNotContain("key.json", listed.Out);
                Assert.DoesNotContain("baseline", listed.Out);
                Assert.Equal("", listed.Err);
            }

            // Without a model or an effort it stops with 2 before it builds or starts anything.
            var stopped = Bash(script, "claude", "review-plan", Path.Combine(temp, "a case", "plan-pilot"));
            Assert.Equal(2, stopped.Code);
            Assert.Contains("EVAL_MODEL", stopped.Err);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    private static (int Code, string Out, string Err) Bash(params string[] args)
    {
        var start = new ProcessStartInfo(Tools.Bash) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        start.Environment.Remove("EVAL_MODEL");
        start.Environment.Remove("EVAL_EFFORT");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("run.sh did not finish.");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
    }

    // ---- the checks on good and bad samples ----

    private const string GoodKey = """
        [
          {"id":"F01","case":"plan-01","pass":"plan","point":"SE:05","severity":"High","sentence":"Every maker gets the admin role in Prod, see Constraints."},
          {"id":"F02","case":"solution-01","pass":"verify","point":"plan checklist","severity":"Medium","sentence":"The flow named in Automation step 2 is missing from the solution.","baseline":"not-shown"}
        ]
        """;

    [Fact]
    public void The_key_checks_accept_a_good_key()
    {
        string[] cases = ["plan-01", "solution-01"];

        Assert.Empty(KeyProblems(GoodKey, cases));
        Assert.Empty(BaselineFieldProblems(GoodKey));
        Assert.Empty(CountProblems(GoodKey, ["plan-01"], ["solution-01"], perCase: 1, flawedPlan: 1, flawedSolution: 1, security: 0, verify: 1));
        Assert.Empty(NotShownProblems(GoodKey, least: 1));
        Assert.Empty(CountProblems(GoodKey, ["plan-01"], ["solution-01"], perCase: 3, flawedPlan: 1, flawedSolution: 1, security: 0, verify: 1, new Dictionary<string, int> { ["plan-01"] = 1, ["solution-01"] = 1 }));
    }

    [Fact]
    public void The_key_checks_catch_a_wrong_key()
    {
        string[] cases = ["plan-01", "solution-01"];

        Assert.NotEmpty(KeyProblems("{}", cases));
        Assert.NotEmpty(KeyProblems("[", cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("\"plan-01\"", "\"plan-09\""), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("\"pass\":\"plan\"", "\"pass\":\"verify\""), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("\"pass\":\"verify\"", "\"pass\":\"review\""), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("SE:05", "SE:11"), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("\"High\"", "\"Critical\""), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("\"id\":\"F02\"", "\"id\":\"F01\""), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("The flow named in Automation step 2 is missing from the solution.", "short"), cases));
        Assert.NotEmpty(KeyProblems(GoodKey.Replace("see Constraints.\"", "see Constraints.\",\"baseline\":\"shown\""), cases));
        Assert.NotEmpty(BaselineFieldProblems(GoodKey.Replace(",\"baseline\":\"not-shown\"", "")));
        Assert.NotEmpty(BaselineFieldProblems(GoodKey.Replace("not-shown", "maybe")));
        Assert.NotEmpty(NotShownProblems(GoodKey.Replace("not-shown", "shown"), least: 1));
        Assert.NotEmpty(CountProblems(GoodKey, ["plan-01", "plan-02"], ["solution-01"], perCase: 1, flawedPlan: 2, flawedSolution: 1, security: null, verify: null));
        Assert.NotEmpty(CountProblems(GoodKey, ["plan-01"], ["solution-01"], perCase: 2, flawedPlan: 1, flawedSolution: 1, security: null, verify: null));
        Assert.NotEmpty(CountProblems(GoodKey, ["plan-01"], ["solution-01"], perCase: 1, flawedPlan: 1, flawedSolution: 1, security: 1, verify: null));
        Assert.NotEmpty(CountProblems(GoodKey, ["plan-01"], ["solution-01"], perCase: 1, flawedPlan: 1, flawedSolution: 1, security: null, verify: null, new Dictionary<string, int> { ["solution-01"] = 2 }));
    }

    // A raw string takes the source file's line endings, and a Windows checkout gives it CRLF.
    private static readonly string GoodSkill = """
        ---
        name: powerops-verify
        description: Use to verify a built solution against its plan.
        license: Apache-2.0
        compatibility: Needs the powerops CLI on PATH.
        ---

        # powerops-verify

        ## Commands you may run

        - `powerops analyze <current solution>`: findings.
        - `powerops settings validate <current solution> --settings <settings file>`
        - `powerops plan facts`, only under the rule of the plan reviewer.

        ## Write the report

        ```
        plan-hash: <bodyHash>
        model: <the model>
        fresh-context: yes|no
        state: ready|blocked|failed|dropped
        input: <the current solution>
        solution-hash: <the digest>
        deploy-hash: <the zip digest>
        ```
        """.Replace("\r\n", "\n");

    [Fact]
    public void The_draft_checks_accept_a_good_skill()
    {
        var listed = ListedCommands(GoodSkill);

        Assert.Empty(FrontMatterProblems(GoodSkill, "powerops-verify"));
        Assert.Empty(HeaderLineProblems(GoodSkill));
        Assert.Equal(["powerops analyze <current solution>", "powerops settings validate <current solution> --settings <settings file>", "powerops plan facts"], listed);
        Assert.Empty(NotAllowed(listed!, Allowlist("--allowedTools 'Bash(powerops analyze *)' 'Bash(powerops settings validate *)' 'Bash(powerops plan status *)'")));
        Assert.Empty(ModelIdProblems("x", GoodSkill));
        Assert.Empty(DocsDriftTests.UnknownCommands(GoodSkill));
        Assert.Empty(DocsDriftTests.UnknownFlags(GoodSkill));
    }

    [Fact]
    public void The_draft_checks_catch_a_wrong_skill()
    {
        Assert.NotEmpty(FrontMatterProblems(GoodSkill.Replace("name: powerops-verify", "name: verify"), "powerops-verify"));
        Assert.NotEmpty(FrontMatterProblems(GoodSkill.Replace("license: Apache-2.0", "license: Apache-2.0\nmodel: x"), "powerops-verify"));
        Assert.NotEmpty(FrontMatterProblems(GoodSkill.Replace("compatibility: Needs the powerops CLI on PATH.\n", ""), "powerops-verify"));
        Assert.NotEmpty(FrontMatterProblems("# no front matter\n", "powerops-verify"));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("model: <the model>\n", "")));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("model: <the model>\nfresh-context: yes|no", "fresh-context: yes|no\nmodel: <the model>")));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("\nstate: ", "\n state: ")));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("solution-hash: <the digest>\n", "")));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("input: <the current solution>\nsolution-hash: <the digest>", "solution-hash: <the digest>\ninput: <the current solution>")));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("deploy-hash: <the zip digest>\n", "")));
        Assert.NotEmpty(HeaderLineProblems(GoodSkill.Replace("solution-hash: <the digest>\ndeploy-hash: <the zip digest>", "deploy-hash: <the zip digest>\nsolution-hash: <the digest>")));
        Assert.Null(ListedCommands(GoodSkill.Replace("## Commands you may run", "## Commands")));
        Assert.Equal(["powerops analyze <current solution>"], ListedCommands(GoodSkill.Replace("- `powerops settings validate <current solution> --settings <settings file>`", "- no code here").Replace("- `powerops plan facts`", "- `plan facts`")));
        Assert.Equal(["powerops deploy apply x --apply"], NotAllowed(["powerops analyze x", "powerops deploy apply x --apply", "powerops plan facts"], ["powerops analyze"]));
        Assert.Equal(["powerops analyzer x"], NotAllowed(["powerops analyzer x"], ["powerops analyze"]));
        Assert.Equal(["powerops analyze"], Allowlist("a=('Bash(powerops analyze *)' 'Bash(powerops analyze *)' 'Bash(ls *)')"));
        Assert.NotEmpty(ModelIdProblems("x", "Runs on claude-" + "opus-9 or an o" + "pus tier."));
        Assert.NotEmpty(DocsDriftTests.UnknownCommands("`powerops analyse x`"));
        Assert.NotEmpty(DocsDriftTests.UnknownFlags("`powerops analyze x --nope`"));
    }

    [Fact]
    public void The_case_checks_accept_good_text_and_catch_a_wrong_one()
    {
        const string Sections = "## Goal\n## Users\n## Data\n## Automation\n## Screens\n## Environments and route\n## Solution and publisher\n## Constraints\n## Assumptions\n## Open questions\n## Questions for your admin\n## Steps\n";

        Assert.Empty(SectionProblems("# T\n\n" + Sections));
        Assert.NotEmpty(SectionProblems("# T\n\n" + Sections.Replace("## Users\n", "")));
        Assert.NotEmpty(SectionProblems("# T\n\n" + Sections.Replace("## Users\n## Data\n", "## Data\n## Users\n")));
        Assert.Empty(MarkerProblems("cases/plan-01/docs/plans/inspections/plan.md", "Secrets live in a vault; the evaluation of a flow is mocked."));
        Assert.NotEmpty(MarkerProblems("cases/plan-01/docs/plans/inspections/plan.md", "The key is in the baseline."));
        Assert.NotEmpty(MarkerProblems("cases/plan-01/flawed/plan.md", "text"));
        Assert.Empty(FakeValueProblems("x", "https://contoso.example/a, https://api.contoso.example, a@example.invalid, 00000000-0000-0000-0000-000000000002"));
        Assert.Equal(3, FakeValueProblems("x", "https://www.example.com/a a@example.com 11111111-1111-1111-1111-111111111111").Count);
        Assert.Equal("a/b\nc/d\n", NormalizeBaseline("a\\b\r\nc\\d\r\n"));

        const string Roots = "<ImportExportXml><SolutionManifest><RootComponents><RootComponent type=\"20\" id=\"{a}\" behavior=\"0\" /><RootComponent type=\"80\" id=\"{b}\" behavior=\"0\" /></RootComponents></SolutionManifest></ImportExportXml>";
        Assert.Empty(RootComponentProblems(Roots, Roots.Replace("<SolutionManifest>", "<SolutionManifest><Version>1.2.0.0</Version>")));
        Assert.NotEmpty(RootComponentProblems(Roots, Roots.Replace("<RootComponent type=\"80\" id=\"{b}\" behavior=\"0\" />", "")));
        Assert.NotEmpty(RootComponentProblems(Roots, Roots.Replace("behavior=\"0\" /><RootComponent type=\"80\"", "behavior=\"2\" /><RootComponent type=\"80\"")));
        const string Seed = "<ImportExportXml><SolutionManifest><UniqueName>S</UniqueName><Version>1.0.0.0</Version><RootComponents><RootComponent type=\"20\" id=\"{a}\" behavior=\"0\" /></RootComponents></SolutionManifest></ImportExportXml>";
        var grown = Seed.Replace("1.0.0.0", "1.1.0.0").Replace("</RootComponents>", "<RootComponent type=\"80\" id=\"{b}\" behavior=\"0\" /></RootComponents>");
        Assert.Empty(PreviousManifestProblems(Seed, grown));
        Assert.NotEmpty(PreviousManifestProblems(Seed, grown.Replace("<UniqueName>S</UniqueName>", "<UniqueName>T</UniqueName>")));
        Assert.NotEmpty(PreviousManifestProblems(Seed, grown.Replace("<RootComponent type=\"20\" id=\"{a}\" behavior=\"0\" />", "")));
    }

    private static string BlobId(byte[] bytes)
    {
        var head = Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA1.HashData([.. head, .. bytes]));
    }

    // Latin-1 maps every byte to one char, so only CR LF changes: a BOM or any encoding is hashed as git hashes it.
    private static byte[] LfBytes(string path) => Encoding.Latin1.GetBytes(Encoding.Latin1.GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n"));

    [Fact]
    public void The_kept_roles_text_is_the_pinned_or_a_recorded_blob()
    {
        var freeze = Path.Combine(Golden.RepoRoot, "docs", "plans", "m83-zip-linked-reviews");
        var pinned = PinnedRow().Matches(File.ReadAllText(Path.Combine(freeze, "plan.md"))).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        var recorded = RecordedLine().Matches(File.ReadAllText(Path.Combine(freeze, "decisions.md"))).Select(m => (Path: m.Groups[1].Value, Id: m.Groups[2].Value)).ToList();
        Assert.Equal(3, pinned.Count);

        foreach (var (path, id) in pinned)
        {
            var bytes = LfBytes(Path.Combine([Golden.RepoRoot, .. path.Split('/')]));
            var actual = BlobId(bytes);
            Assert.True(actual == id || recorded.Contains((path, actual)), $"{path}: a kept role's text changed; rerun its eval or record the comparison in m83's decisions.md (AGENTS.md). Its blob id is {actual}.");

            bytes[0] ^= 1;
            Assert.NotEqual(id, BlobId(bytes));
        }
    }
}