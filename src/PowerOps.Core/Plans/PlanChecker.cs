using System.Globalization;
using System.Text.RegularExpressions;

using PowerOps.Core.Readers;

namespace PowerOps.Core.Plans;

/// <summary>The result of checking one plan. Header fields are null when the header is malformed (or the value is null).</summary>
public sealed record PlanReport(
    string Path,
    string? Status,
    string? Review,
    string? SignedOff,
    string? Environment,
    string? Depth,
    string? Supersedes,
    int OpenQuestions,
    string? BodyHash,
    bool Malformed,
    IReadOnlyList<string> Violations,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<PlanReportFile> Reports,
    PlanWrites? Writes);

/// <summary>One agent report in a plan folder. State and Stale are null when the report does not say, or could not be read. Input (a relative path) and SolutionHash (64 lowercase hex characters, else "unknown") are null for review.md; for the two solution reports Input is null when there is no valid input: line.</summary>
public sealed record PlanReportFile(string File, string? State, bool? Stale, string? Input = null, string? SolutionHash = null);

/// <summary>The write journal (writes.md) of a plan folder: its table rows and those whose cleanup is still open. Both are null when the file could not be read.</summary>
public sealed record PlanWrites(int? Entries, int? OpenCleanup);

public static partial class PlanChecker
{
    private const string AllowMarker = "<!-- plan-status: allow -->";

    // A plan, a review and each scanned file are text of a page or two; a larger or binary file (a mockup image) is skipped with a warning.
    internal const long MaxFileBytes = 1024 * 1024;

    // Links are never followed: a link can point at a device or at a file outside the plan folder.
    private static readonly EnumerationOptions NoLinks = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = true,
        RecurseSubdirectories = true,
    };

    /// <summary>The plan.md for a plan file or a folder holding one; null if there is none.</summary>
    /// <exception cref="InvalidDataException">The path is a link; the message starts with the path.</exception>
    public static string? ResolvePlanFile(string path)
    {
        RefuseLink(path);
        if (File.Exists(path))
        {
            return path;
        }

        var file = System.IO.Path.Combine(path, "plan.md");
        return Directory.Exists(path) && File.Exists(file) ? file : null;
    }

    /// <summary>The plan files under <paramref name="path"/>: the path itself, a plan.md in it, or every docs/plans/*/plan.md (ordinal order). A plan folder that is a link is left out and added to <paramref name="skippedLinks"/>.</summary>
    /// <exception cref="InvalidDataException"><paramref name="path"/>, its docs folder or its docs/plans folder is a link; the message starts with that path.</exception>
    public static IReadOnlyList<string> Discover(string path, ICollection<string>? skippedLinks = null)
    {
        if (ResolvePlanFile(path) is { } single)
        {
            return [single];
        }

        var docs = System.IO.Path.Combine(path, "docs");
        RefuseLink(docs);
        var plans = System.IO.Path.Combine(docs, "plans");
        RefuseLink(plans);
        if (!Directory.Exists(plans))
        {
            return [];
        }

        var found = new List<string>();
        foreach (var folder in Directory.EnumerateDirectories(plans, "*", new EnumerationOptions { IgnoreInaccessible = true }))
        {
            var plan = System.IO.Path.Combine(folder, "plan.md");
            if (!File.Exists(plan))
            {
                continue;
            }

            if (new DirectoryInfo(folder).LinkTarget is not null)
            {
                skippedLinks?.Add(folder);
            }
            else
            {
                found.Add(plan);
            }
        }

        return [.. found.Order(StringComparer.Ordinal)];
    }

    // Links are never followed: a link can point at a device or at a folder outside the repo, and its files' names and line numbers would be printed.
    private static void RefuseLink(string path)
    {
        if (new FileInfo(path).LinkTarget is not null)
        {
            throw new InvalidDataException($"{path}: is a symbolic link; symbolic links are not read.");
        }
    }

    public static PlanReport Check(string planFile, string baseDir)
    {
        var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(planFile))!;
        PlanDocument doc;
        try
        {
            doc = PlanDocument.Parse(ReadPlan(planFile));
        }
        catch (InvalidDataException ex)
        {
            // One unreadable plan.md is that plan's violation; the other plans are still checked.
            return new PlanReport(Display(folder, baseDir), null, null, null, null, null, null, 0, null, false, [ex.Message.Replace(planFile, Display(planFile, baseDir), StringComparison.Ordinal)], [], [], null);
        }

        var violations = new List<string>();
        var warnings = new List<string>();
        var reviewFile = System.IO.Path.Combine(folder, "review.md");

        if (doc.Header is not { } h)
        {
            violations.Add($"malformed header: {doc.Error}");
        }
        else if (h.Status is "signed-off" or "implementing" or "done")
        {
            if (h.SignedOff is null || h.SignedOffHash is null)
            {
                violations.Add("signed off without the tool: run `powerops plan sign-off`");
            }
            else if (h.SignedOffHash != doc.BodyHash)
            {
                violations.Add("changed since sign-off (or signed by hand): set status to approved and run `powerops plan sign-off` again, or revert the edits");
            }

            violations.AddRange(ReviewProblems(h.Review, reviewFile));
        }

        if (doc.Header?.Supersedes is { } old)
        {
            violations.AddRange(SupersedesProblems(old, folder, baseDir));
        }

        violations.AddRange(ScanTenantData(folder, baseDir, warnings));

        var reports = ReadReports(folder, baseDir, doc.BodyHash, warnings);
        var writes = ReadWrites(folder, baseDir, warnings);
        warnings.AddRange(AgentAnswers(folder, baseDir, warnings));

        return new PlanReport(
            Display(folder, baseDir),
            doc.Header?.Status,
            doc.Header?.Review,
            doc.Header?.SignedOff?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            doc.Header?.Environment,
            doc.Header?.Depth,
            doc.Header?.Supersedes,
            doc.OpenQuestions,
            doc.BodyHash,
            doc.Header is null,
            violations,
            warnings,
            reports,
            writes);
    }

    // A row is a table line that is neither a header (the line before a separator) nor a separator. Its cleanup, the last
    // cell without backticks or a final dot, is open unless it is `none` or starts with `done`, so an empty one is open.
    private static PlanWrites? ReadWrites(string folder, string baseDir, ICollection<string> warnings)
    {
        var file = System.IO.Path.Combine(folder, "writes.md");
        if (!File.Exists(file))
        {
            return null;
        }

        if (ReadScannable(file, baseDir, warnings) is not { } text)
        {
            return new PlanWrites(null, null);
        }

        var lines = Lines(text).Select(l => l.Trim()).ToList();
        var rows = lines
            .Where((l, i) => l.StartsWith('|') && !TableSeparatorRegex().IsMatch(l) && !(i + 1 < lines.Count && TableSeparatorRegex().IsMatch(lines[i + 1])))
            .ToList();
        var open = rows.Count(r =>
        {
            // Split on the row's leading `|`, so cells[0] is empty; the last cell is empty when the row ends in a `|`.
            var cells = CellRegex().Split(r);
            var cleanup = (cells[^1].Length == 0 ? cells[^2] : cells[^1]).Trim(' ', '`', '.');
            return !cleanup.Equals("none", StringComparison.OrdinalIgnoreCase) && !cleanup.StartsWith("done", StringComparison.OrdinalIgnoreCase);
        });
        return new PlanWrites(rows.Count, open);
    }

    // The skills log every sign-off and acceptance as a line under `## Log`; the reasons of a product answer may mention
    // either, so only Log lines are read. The marker is the agent's own claim: this catches honest marking, nothing more.
    private static List<string> AgentAnswers(string folder, string baseDir, ICollection<string> warnings)
    {
        var found = new List<string>();
        var file = System.IO.Path.Combine(folder, "decisions.md");
        if (!File.Exists(file) || ReadScannable(file, baseDir, warnings) is not { } text)
        {
            return found;
        }

        // The Log runs from `## Log` to the next `## ` heading. An item is a `- ` line and the non-blank lines that continue it;
        // its warning names its first line.
        var where = Display(file, baseDir);
        var inLog = false;
        var (start, item) = (0, "");
        var lines = Lines(text).ToList();
        for (var i = 0; i <= lines.Count; i++)
        {
            var line = i < lines.Count ? lines[i] : "";
            var heading = line.StartsWith("## ", StringComparison.Ordinal);
            if (heading || line.TrimStart().StartsWith("- ", StringComparison.Ordinal) || line.Trim().Length == 0)
            {
                if (item.Length > 0 && AgentAnswerRegex().IsMatch(item))
                {
                    if (SignOffRegex().IsMatch(item))
                    {
                        found.Add($"sign-off answered by an agent ({where}:{start}): only a person signs off a plan");
                    }
                    else if (AcceptanceRegex().IsMatch(item))
                    {
                        found.Add($"acceptance answered by an agent ({where}:{start}): only a person accepts a High finding or a security gap");
                    }
                }

                (start, item) = inLog && !heading && line.Trim().Length > 0 ? (i + 1, line) : (0, "");
                inLog = heading ? line.Trim().Equals("## Log", StringComparison.OrdinalIgnoreCase) : inLog;
            }
            else if (item.Length > 0)
            {
                item += " " + line.Trim();
            }
        }

        return found;
    }

    private static readonly string[] ReportFiles = ["review.md", "security-solution.md", "verify.md"];

    private static readonly string[] ReportStates = ["ready", "blocked", "failed", "dropped"];

    // State, plan-hash, input and solution-hash come from a report's first six lines (review.md's plan-hash, as before, from the first such line anywhere).
    // A stale or unknown-state report only warns: the report is data, and neither one gates anything.
    private static List<PlanReportFile> ReadReports(string folder, string baseDir, string? bodyHash, ICollection<string> warnings)
    {
        var reports = new List<PlanReportFile>();
        foreach (var name in ReportFiles)
        {
            var file = System.IO.Path.Combine(folder, name);
            if (!File.Exists(file))
            {
                continue;
            }

            if (ReadScannable(file, baseDir, warnings) is not { } text)
            {
                reports.Add(new PlanReportFile(name, null, null, null, name == "review.md" ? null : UnknownHash));
                continue;
            }

            var head = Lines(text).Take(6).Select(l => l.Trim()).ToList();
            var state = HeadValue(head, "state:");
            bool? stale = null;
            if (bodyHash is not null && name == "review.md")
            {
                var line = Lines(text).FirstOrDefault(l => l.StartsWith("plan-hash:", StringComparison.Ordinal));
                if (line is null)
                {
                    warnings.Add("review.md has no plan-hash line");
                }
                else
                {
                    stale = line["plan-hash:".Length..].Trim() != bodyHash;
                    if (stale == true)
                    {
                        warnings.Add("review.md was written for a different version of the plan (stale review)");
                    }
                }
            }
            else if (bodyHash is not null && HeadValue(head, "plan-hash:") is { } hash)
            {
                stale = hash != bodyHash;
                if (stale == true)
                {
                    warnings.Add($"{name} was written for a different version of the plan (stale report)");
                }
            }

            if (state is not null && !ReportStates.Contains(state))
            {
                warnings.Add($"{name} has an unknown state '{state}' (expected {string.Join(", ", ReportStates[..^1])} or {ReportStates[^1]})");
            }

            string? input = null;
            string? solutionHash = null;
            if (name != "review.md")
            {
                solutionHash = HeadValue(head, "solution-hash:") is { Length: 64 } h && h.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')) ? h : UnknownHash;
                if (HeadValue(head, "input:") is { } value)
                {
                    input = IsLocator(value) ? value.TrimEnd('/') : null;
                    if (input is null)
                    {
                        warnings.Add($"{name} has an input: line that is not a relative path");
                    }
                }
            }

            reports.Add(new PlanReportFile(name, state, stale, input, solutionHash));
        }

        return reports;
    }

    private const string UnknownHash = "unknown";

    // A relative path with / between names, as the roles write it: no drive, no backslash, no leading /, no empty, . or .. segment.
    private static bool IsLocator(string value)
    {
        var path = value.EndsWith('/') ? value[..^1] : value;
        return path.Length is >= 1 and <= 260
            && !path.StartsWith('/')
            && !path.Any(c => char.IsControl(c) || c is ':' or '\\')
            && path.Split('/').All(s => s.Length > 0 && s is not ("." or ".."));
    }

    private static string? HeadValue(List<string> head, string key) =>
        head.FirstOrDefault(l => l.StartsWith(key, StringComparison.Ordinal))?[key.Length..].Trim();

    // The name is echoed (and put in a path) only when it is a plain word.
    private static IEnumerable<string> SupersedesProblems(string name, string folder, string baseDir)
    {
        var previous = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(folder)!, name);
        if (string.Equals(System.IO.Path.GetFullPath(previous), folder, OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            yield return "supersedes names the plan itself";
        }
        else if (!File.Exists(System.IO.Path.Combine(previous, "plan.md")))
        {
            // The header parser only accepts plain folder names, so echoing it is safe.
            yield return $"supersedes '{name}' but {Display(System.IO.Path.Combine(previous, "plan.md"), baseDir)} does not exist";
        }
    }

    /// <summary>The review-state problems that block a sign-off (and fail the check once signed off).</summary>
    internal static IEnumerable<string> ReviewProblems(string review, string reviewFile)
    {
        if (review == "pending")
        {
            yield return "review is pending: run powerops-review-plan, or set review: skipped before signing off";
        }
        else if (review == "done" && !File.Exists(reviewFile))
        {
            yield return "review: done but review.md is missing";
        }
    }

    /// <summary>The text of a plan.md (also read by sign-off). A link is refused: it can point outside the folder, or at a device.</summary>
    /// <exception cref="InvalidDataException">The file is a link or over 1 MB; the message starts with the path.</exception>
    internal static string ReadPlan(string planFile)
    {
        RefuseLink(planFile);
        return BoundedRead.ReadAllText(planFile, MaxFileBytes);
    }

    /// <summary>The text of a file in a plan folder, or null with a warning when it is a link, binary or over 1 MB.</summary>
    private static string? ReadScannable(string file, string baseDir, ICollection<string> warnings)
    {
        string? skipped = null;
        string? text = null;
        if (new FileInfo(file).LinkTarget is not null)
        {
            skipped = "a symbolic link";
        }
        else
        {
            try
            {
                // Decoded by byte order mark first (UTF-16 text is full of NULs); a NUL left in the text means a binary file.
                text = BoundedRead.ReadAllText(file, MaxFileBytes);
                if (text.AsSpan(0, Math.Min(text.Length, 8192)).Contains('\0'))
                {
                    skipped = "a binary file";
                }
            }
            catch (InvalidDataException)
            {
                skipped = "larger than 1 MB";
            }
        }

        if (skipped is not null)
        {
            var warning = $"{Display(file, baseDir)}: {skipped}, not read";
            if (!warnings.Contains(warning))
            {
                warnings.Add(warning);
            }

            return null;
        }

        return text;
    }

    private static IEnumerable<string> Lines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    /// <summary>One violation per line per kind; the matched text is never included. Links are not followed; a binary file or one over 1 MB adds a warning to <paramref name="warnings"/> and is not scanned.</summary>
    internal static IEnumerable<string> ScanTenantData(string folder, string baseDir, ICollection<string> warnings)
    {
        var files = Directory.EnumerateFiles(folder, "*", NoLinks).Order(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (ReadScannable(file, baseDir, warnings) is not { } text)
            {
                continue;
            }

            var lines = Lines(text).ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(AllowMarker, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var (kind, regex) in new[] { ("URL", UrlRegex()), ("GUID", GuidRegex()), ("email", EmailRegex()) })
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        yield return $"{Display(file, baseDir)}:{i + 1}: looks like a {kind}; remove it, or add {AllowMarker} to the line";
                    }
                }
            }
        }
    }

    /// <summary>Relative to <paramref name="baseDir"/> when underneath it, otherwise the full path; forward slashes.</summary>
    public static string Display(string path, string baseDir)
    {
        var rel = System.IO.Path.GetRelativePath(baseDir, path);
        var under = !System.IO.Path.IsPathRooted(rel) && rel != ".." && !rel.StartsWith(".." + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal);
        return (under ? rel : path).Replace('\\', '/');
    }

    [GeneratedRegex(@"^\|[\s|:-]*-[\s|:-]*$")]
    private static partial Regex TableSeparatorRegex();

    // A cell boundary is a `|` that no backslash escapes.
    [GeneratedRegex(@"(?<!\\)\|")]
    private static partial Regex CellRegex();

    [GeneratedRegex(@"answered by:\s*agent\b", RegexOptions.IgnoreCase)]
    private static partial Regex AgentAnswerRegex();

    [GeneratedRegex(@"\bsign(?:ed|s|ing)?[- ]?off\b", RegexOptions.IgnoreCase)]
    private static partial Regex SignOffRegex();

    [GeneratedRegex(@"\baccept(?:s|ed|ing|ance)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex AcceptanceRegex();

    [GeneratedRegex("https?://")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();
}