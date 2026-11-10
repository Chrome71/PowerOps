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
    int AdminQuestions,
    string? BodyHash,
    bool Malformed,
    IReadOnlyList<string> Violations,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<PlanReportFile> Reports,
    PlanWrites? Writes);

/// <summary>One agent report in a plan folder. State and Stale are null when the report does not say, or could not be read. Input (a relative path) and SolutionHash (64 lowercase hex characters, else "unknown") are null for review.md; for the two solution reports Input is null when there is no valid input: line. SolutionStale is null for review.md and when nothing was compared (no valid input and hash, or a solution that could not be read); else true when the digest of the solution Input names differs from SolutionHash.</summary>
public sealed record PlanReportFile(string File, string? State, bool? Stale, string? Input = null, string? SolutionHash = null, bool? SolutionStale = null);

/// <summary>The two solution reports of one plan folder under docs/plans: Plan is the folder's name; a report is null when the folder has none.</summary>
public sealed record PlanReviews(string Plan, PlanReportFile? Verify, PlanReportFile? Security);

/// <summary>The write journal (writes.md) of a plan folder: its table rows and those whose cleanup is still open. Both are null when the file could not be read.</summary>
public sealed record PlanWrites(int? Entries, int? OpenCleanup);

/// <summary>What the prototype gate found: Line problems (no valid prototype: line, while Screens names something), File problems (mockups/prototype.html fails a check), the person-mark Warnings, and whether the deciding line is a valid approved one.</summary>
internal sealed record PrototypeCheck(IReadOnlyList<string> Line, IReadOnlyList<string> File, IReadOnlyList<string> Warnings, bool Approved);

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

    /// <summary>The plan files under <paramref name="path"/>: the path itself when it is a file, the plan.md of a folder directly inside a docs/plans folder, or else every docs/plans/*/plan.md (ordinal order), so a plan.md at a repository root does not hide them. A plan folder that is a link is left out and added to <paramref name="skippedLinks"/>.</summary>
    /// <exception cref="InvalidDataException"><paramref name="path"/>, its docs folder or its docs/plans folder is a link; the message starts with that path.</exception>
    public static IReadOnlyList<string> Discover(string path, ICollection<string>? skippedLinks = null) =>
        ResolvePlanFile(path) is { } single && (File.Exists(path) || RepoRoot(System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(path))) is not null)
            ? [single]
            : PlansUnder(path, skippedLinks);

    // The repository root of a plan folder (a full path) directly inside docs/plans; null for any other folder.
    private static string? RepoRoot(string folder) =>
        System.IO.Path.GetDirectoryName(folder) is { } plans
        && System.IO.Path.GetDirectoryName(plans) is { } docs
        && System.IO.Path.GetFileName(plans).Equals("plans", StringComparison.OrdinalIgnoreCase)
        && System.IO.Path.GetFileName(docs).Equals("docs", StringComparison.OrdinalIgnoreCase)
            ? System.IO.Path.GetDirectoryName(docs)
            : null;

    // Every docs/plans/*/plan.md under a repository root, whether or not the root holds a plan.md itself.
    private static IReadOnlyList<string> PlansUnder(string root, ICollection<string>? skippedLinks)
    {
        var docs = System.IO.Path.Combine(root, "docs");
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
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            // One unreadable plan.md is that plan's violation; the other plans are still checked. The runtime's own messages hold the full path, so they are not printed.
            var problem = ex is InvalidDataException ? ex.Message.Replace(planFile, Display(planFile, baseDir), StringComparison.Ordinal) : $"{Display(planFile, baseDir)}: could not be read";
            return new PlanReport(Display(folder, baseDir), null, null, null, null, null, null, 0, 0, null, false, [problem], [], [], null);
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

        // The two solution reports usually name the same input; each distinct input is read once.
        var compared = new Dictionary<string, Comparison>(StringComparer.Ordinal);
        var reports = ReadReports(folder, baseDir, doc.BodyHash, warnings)
            .Select(r => r.SolutionHash is null ? r : r with { SolutionStale = SolutionStale(r, folder, compared, warnings) })
            .ToList();
        var writes = ReadWrites(folder, baseDir, warnings);
        warnings.AddRange(AgentAnswers(folder, baseDir, warnings));
        if (doc.Header is { Status: "draft" or "approved" })
        {
            var prototype = PrototypeProblems(folder, doc, ReadDecisions(folder, baseDir, warnings));
            warnings.AddRange(prototype.Line);
            warnings.AddRange(prototype.File);
            warnings.AddRange(prototype.Warnings);
        }

        return new PlanReport(
            Display(folder, baseDir),
            doc.Header?.Status,
            doc.Header?.Review,
            doc.Header?.SignedOff?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            doc.Header?.Environment,
            doc.Header?.Depth,
            doc.Header?.Supersedes,
            doc.OpenQuestions,
            doc.AdminQuestions,
            doc.BodyHash,
            doc.Header is null,
            violations,
            warnings,
            reports,
            writes);
    }

    // The digest of an input, or why there is none: Problem is null for a path that is not there (no warning).
    private sealed record Comparison(string? Digest, string? Problem);

    // The roles write input relative to the repository root, the folder that holds docs/plans. Warnings name the report and the input, never a full path.
    private static bool? SolutionStale(PlanReportFile report, string folder, Dictionary<string, Comparison> compared, ICollection<string> warnings)
    {
        if (report.Input is null || report.SolutionHash == UnknownHash)
        {
            return null;
        }

        if (RepoRoot(folder) is not { } root)
        {
            warnings.Add($"{report.File}: the plan folder is not in docs/plans, so the solution was not compared");
            return null;
        }

        if (!compared.TryGetValue(report.Input, out var outcome))
        {
            compared[report.Input] = outcome = Compare(root, report.Input);
        }

        if (outcome.Digest is null)
        {
            if (outcome.Problem is not null)
            {
                warnings.Add($"{report.File}: input '{report.Input}' {outcome.Problem}, so the solution was not compared");
            }

            return null;
        }

        var stale = outcome.Digest != report.SolutionHash;
        if (stale)
        {
            warnings.Add($"{report.File} was written for a different version of the solution in '{report.Input}' (stale solution)");
        }

        return stale;
    }

    private static Comparison Compare(string root, string input)
    {
        var (path, problem) = Walk(root, input);
        if (path is null)
        {
            return new Comparison(null, problem);
        }

        try
        {
            return new Comparison(Manifest.Read(path).Digest, null);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return new Comparison(null, "could not be read");
        }
    }

    /// <summary>The full path of a report's <c>input:</c> (a relative path with / between names) under <paramref name="root"/>, or null when it is not such a path, a name is a reserved Windows name, a segment is a symbolic link, the walk fails or nothing is there.</summary>
    public static string? InputPath(string root, string input) => IsLocator(input) ? Walk(root, input).Path : null;

    // The input's path under root, or why there is none: both are null for a path that is not there.
    private static (string? Path, string? Problem) Walk(string root, string input)
    {
        var names = input.Split('/');
        // Reserved names open devices on Windows; refuse them before any file is touched.
        if (OperatingSystem.IsWindows() && names.Any(WindowsReservedName))
        {
            return (null, "could not be read");
        }

        // The walk is inside the try too: a name the file system rejects is a report problem, not an internal error.
        try
        {
            var path = root;
            foreach (var name in names)
            {
                path = System.IO.Path.Combine(path, name);
                if (new FileInfo(path).LinkTarget is not null)
                {
                    return (null, "passes through a symbolic link");
                }
            }

            return File.Exists(path) || Directory.Exists(path) ? (path, null) : (null, null);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return (null, "could not be read");
        }
    }

    /// <summary>A name Windows treats as a device (CON, PRN, AUX, NUL, COM1-9, LPT1-9, alone or before a dot, any case) or trims (ending in a dot or a space).</summary>
    internal static bool WindowsReservedName(string name) =>
        name.EndsWith('.') || name.EndsWith(' ') || ReservedNameRegex().IsMatch(name.Split('.')[0]);

    [GeneratedRegex("^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase)]
    private static partial Regex ReservedNameRegex();

    /// <summary>The verify.md and security-solution.md of each plan folder under <paramref name="root"/> (docs/plans/*/, ordinal order) that holds at least one of them, or null when there is no docs/plans folder. Only the reports' headers and plan.md are read; a report's Stale is against the plan's current hash (null when plan.md can't be read or parsed), and SolutionStale is not filled in.</summary>
    /// <exception cref="InvalidDataException">docs or docs/plans is a link, as <see cref="Discover"/> throws; the message starts with the path. A plan.md in <paramref name="root"/> itself does not hide them.</exception>
    public static IReadOnlyList<PlanReviews>? Reviews(string root)
    {
        if (!Directory.Exists(System.IO.Path.Combine(root, "docs", "plans")))
        {
            return null;
        }

        var reviews = new List<PlanReviews>();
        foreach (var planFile in PlansUnder(root, null))
        {
            string? hash;
            try
            {
                hash = PlanDocument.Parse(ReadPlan(planFile)).BodyHash;
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                hash = null;
            }

            var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(planFile))!;
            var reports = ReadReports(folder, root, hash, new List<string>());
            var verify = reports.Find(r => r.File == "verify.md");
            var security = reports.Find(r => r.File == "security-solution.md");
            if (verify is not null || security is not null)
            {
                reviews.Add(new PlanReviews(System.IO.Path.GetFileName(folder), verify, security));
            }
        }

        return reviews;
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
        if (ReadDecisions(folder, baseDir, warnings) is not { } text)
        {
            return found;
        }

        var where = Display(System.IO.Path.Combine(folder, "decisions.md"), baseDir);
        foreach (var (start, item) in LogItems(text).Where(i => AgentAnswerRegex().IsMatch(i.Item)))
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

        return found;
    }

    /// <summary>The text of decisions.md, or null when there is none or it is a link, binary or over 1 MB (with a warning).</summary>
    internal static string? ReadDecisions(string folder, string baseDir, ICollection<string> warnings)
    {
        var file = System.IO.Path.Combine(folder, "decisions.md");
        return File.Exists(file) ? ReadScannable(file, baseDir, warnings) : null;
    }

    // The Log runs from `## Log` to the next `## ` heading. An item is a `- ` line and the non-blank lines that continue it;
    // Line is the number of its first line (1-based).
    private static IEnumerable<(int Line, string Item)> LogItems(string text)
    {
        var inLog = false;
        var (start, item) = (0, "");
        var lines = Lines(text).ToList();
        for (var i = 0; i <= lines.Count; i++)
        {
            var line = i < lines.Count ? lines[i] : "";
            var heading = line.StartsWith("## ", StringComparison.Ordinal);
            if (heading || line.TrimStart().StartsWith("- ", StringComparison.Ordinal) || line.Trim().Length == 0)
            {
                if (item.Length > 0)
                {
                    yield return (start, item);
                }

                (start, item) = inLog && !heading && line.Trim().Length > 0 ? (i + 1, line) : (0, "");
                inLog = heading ? line.Trim().Equals("## Log", StringComparison.OrdinalIgnoreCase) : inLog;
            }
            else if (item.Length > 0)
            {
                item += " " + line.Trim();
            }
        }
    }

    private static readonly string[] ReportFiles = ["review.md", "security-solution.md", "verify.md"];

    private static readonly string[] ReportStates = ["ready", "blocked", "failed", "dropped"];

    // State, plan-hash, input and solution-hash come from a report's first six lines (review.md's plan-hash, as before, from the first such line anywhere).
    // In plan status a stale or unknown-state report only warns and changes no plan's status: the report is data. The deploy routes read the two solution reports through Reviews.
    internal static List<PlanReportFile> ReadReports(string folder, string baseDir, string? bodyHash, ICollection<string> warnings)
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
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                skipped = "could not be read";
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

                foreach (var (kind, regex) in new[] { ("URL", UrlRegex()), ("GUID", GuidRegex()), ("email", EmailRegex()), ("personal environment name", PersonalEnvironmentRegex()) })
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        yield return $"{Display(file, baseDir)}:{i + 1}: looks like a {kind}; remove it, or add {AllowMarker} to the line";
                    }
                }
            }
        }
    }

    private const string PrototypePath = "mockups/prototype.html";
    internal const int MaxPrototypeBytes = 40_960;

    private static readonly string[] BannedTags = ["script", "link", "meta", "base", "iframe", "frame", "object", "embed", "img", "picture", "svg", "form", "video", "audio", "source"];
    private static readonly string[] BannedCss = ["url(", "@import", "@font-face", "image-set(", "expression("];
    private static readonly string[] PrototypeStates = ["empty", "loading", "error", "failed-save"];

    /// <summary>The prototype gate (M70, Goal lines 1 and 2). Nothing is checked when the plan's Screens names nothing. <paramref name="decisions"/> is the text of decisions.md, or null. The file checks are text checks after comments are removed: they catch the honest mistake, and the file is never rendered. A problem names its rule and line, never the matched text.</summary>
    internal static PrototypeCheck PrototypeProblems(string folder, PlanDocument plan, string? decisions)
    {
        if (ScreensText(plan) is not { } screens)
        {
            return new PrototypeCheck([], [], [], false);
        }

        // The last prototype: item under ## Log decides.
        var items = LogItems(decisions ?? "").Where(i => PrototypeItemRegex().IsMatch(i.Item)).ToList();
        var warnings = new List<string>();
        var (valid, approved) = (false, false);
        string? at = null;
        if (items.Count > 0)
        {
            var (line, item) = items[^1];
            at = $":{line}";
            var rest = PrototypeItemRegex().Match(item).Groups[1].Value.Trim();
            var marked = PersonMarkRegex().IsMatch(rest);
            if (ApprovedRegex().IsMatch(rest))
            {
                valid = approved = true;
                if (!marked)
                {
                    warnings.Add($"prototype approval is not marked (answered by: person) (decisions.md:{line}): only a person should approve the sketch");
                }
            }
            else
            {
                valid = marked && SkippedRegex().Match(rest) is { Success: true } skip && skip.Groups[1].Value.Trim().Length > 0;
            }
        }

        string[] lineProblems = valid ? [] : [$"decisions.md{at}: no valid prototype line under ## Log although Screens names something: log `prototype: approved {PrototypePath}`, or `prototype: skipped (<reason>) (answered by: person)`"];
        return new PrototypeCheck(lineProblems, PrototypeFileProblems(folder, screens, approved), warnings, approved);
    }

    // The text of the Screens section, or null when it names nothing: no heading, an empty body, or a first non-blank line starting with "None".
    private static string? ScreensText(PlanDocument plan)
    {
        var body = new List<string>();
        var inside = false;
        foreach (var raw in plan.Lines.Skip(plan.Close + 1))
        {
            var line = raw.TrimEnd('\r', '\n');
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inside = line.Trim().Equals("## Screens", StringComparison.OrdinalIgnoreCase);
            }
            else if (inside)
            {
                body.Add(line);
            }
        }

        var first = body.Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
        return first is null || first.StartsWith("None", StringComparison.OrdinalIgnoreCase) ? null : string.Join('\n', body);
    }

    private static List<string> PrototypeFileProblems(string folder, string screens, bool approved)
    {
        var problems = new List<string>();
        var file = System.IO.Path.Combine(folder, "mockups", "prototype.html");
        if (!File.Exists(file))
        {
            if (approved)
            {
                problems.Add($"{PrototypePath}: not found although the log says approved");
            }

            return problems;
        }

        // A link anywhere on the path, a binary file or one over 1 MB is a problem, not a pass.
        var skipped = new List<string>();
        var text = new DirectoryInfo(System.IO.Path.GetDirectoryName(file)!).LinkTarget is null ? ReadScannable(file, folder, skipped) : null;
        if (text is null)
        {
            problems.Add(skipped.Count > 0 ? skipped[0] : $"{PrototypePath}: the mockups folder is a symbolic link, not read");
            return problems;
        }

        void Add(int line, string rule)
        {
            var problem = $"{PrototypePath}:{line}: {rule}";
            if (!problems.Contains(problem))
            {
                problems.Add(problem);
            }
        }

        if (new FileInfo(file).Length > MaxPrototypeBytes)
        {
            Add(1, $"larger than {MaxPrototypeBytes} bytes");
        }

        if (CommentRegex().Match(text) is not { Success: true } first || !RouteRegex().IsMatch(first.Value))
        {
            Add(1, "no route: line in the first comment");
        }

        // Comments become blanks of the same shape, so line numbers hold.
        var html = CommentRegex().Replace(text, m => BlankRegex().Replace(m.Value, " "));
        int LineAt(int index) => 1 + html.AsSpan(0, index).Count('\n');

        void CheckCss(string css, int line)
        {
            foreach (var banned in BannedCss.Where(b => css.Contains(b, StringComparison.OrdinalIgnoreCase)))
            {
                Add(line, $"CSS {banned} is not allowed");
            }
        }

        foreach (Match m in StyleRegex().Matches(html))
        {
            CheckCss(m.Groups[1].Value, LineAt(m.Groups[1].Index));
        }

        var screenNames = new List<(int Line, string Name)>();
        var states = new List<HashSet<string>>();
        foreach (Match tag in TagRegex().Matches(html))
        {
            var name = tag.Groups["tag"].Value.ToLowerInvariant();
            if (BannedTags.Contains(name))
            {
                Add(LineAt(tag.Index), $"tag <{name}> is not allowed");
            }

            var attrs = tag.Groups["attrs"];
            foreach (Match a in AttributeRegex().Matches(attrs.Value))
            {
                var (key, value, line) = (a.Groups["name"].Value.ToLowerInvariant(), System.Net.WebUtility.HtmlDecode(a.Groups["value"].Value).Trim(), LineAt(attrs.Index + a.Index));
                if (key is "src" or "srcset" or "action")
                {
                    Add(line, $"attribute {key} is not allowed");
                }
                else if (key.Length > 2 && key.StartsWith("on", StringComparison.Ordinal))
                {
                    Add(line, "event-handler attribute (on...) is not allowed");
                }
                else if (key == "href" && !value.StartsWith('#'))
                {
                    Add(line, "href must start with #");
                }
                else if (key == "style")
                {
                    CheckCss(value, line);
                }
                else if (key == "data-screen")
                {
                    screenNames.Add((line, value));
                    states.Add([]);
                }
                else if (key == "data-state" && states.Count > 0)
                {
                    states[^1].Add(value.ToLowerInvariant());
                }
            }
        }

        // "Sketch" must show in the body text: outside comments (already blank), <style> and tags.
        if (!SketchRegex().IsMatch(TagRegex().Replace(StyleRegex().Replace(html, " "), " ")))
        {
            Add(1, "the word \"Sketch\" is missing from the body text");
        }

        if (screenNames.Count is < 1 or > 8)
        {
            Add(1, $"{screenNames.Count} data-screen attributes; need 1 to 8");
        }

        for (var i = 0; i < screenNames.Count; i++)
        {
            var (line, name) = screenNames[i];
            var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape);
            var phrase = $@"(?<![\p{{L}}\p{{N}}]){string.Join(@"\s+", words)}(?![\p{{L}}\p{{N}}])";
            if (name.Length == 0 || !Regex.IsMatch(screens, phrase, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
            {
                Add(line, "data-screen value is not a phrase of the Screens text");
            }

            if (PrototypeStates.Except(states[i]).ToList() is { Count: > 0 } missing)
            {
                Add(line, $"data-screen is missing data-state {string.Join(", ", missing)} before the next data-screen");
            }
        }

        return problems;
    }

    // Everything between <!-- and --> (an unclosed comment runs to the end, as in a browser).
    [GeneratedRegex(@"<!--.*?(?:-->|\z)", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"[^\n]")]
    private static partial Regex BlankRegex();

    [GeneratedRegex(@"(?:^|<!--)[ \t]*route:", RegexOptions.Multiline)]
    private static partial Regex RouteRegex();

    // A start tag, whitespace allowed after the <, quoted values may hold a >.
    [GeneratedRegex(@"<\s*(?<tag>[A-Za-z][A-Za-z0-9-]*)(?<attrs>(?:[^>""']|""[^""]*""|'[^']*')*)>?")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"(?<name>[^\s""'<>/=]+)(?:\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s""'>]+)))?")]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"<\s*style\b[^>]*>(.*?)(?:<\s*/\s*style\s*>|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex StyleRegex();

    [GeneratedRegex(@"\bsketch\b", RegexOptions.IgnoreCase)]
    private static partial Regex SketchRegex();

    [GeneratedRegex(@"^\s*-\s*prototype:(.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PrototypeItemRegex();

    [GeneratedRegex(@"^approved\s+mockups/prototype\.html(?:\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex ApprovedRegex();

    // The reason is the first parenthesised text; a quote of the answer may sit between it and the mark.
    [GeneratedRegex(@"^skipped\s*\((.*?)\).*\(answered by:\s*person\)\s*$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SkippedRegex();

    [GeneratedRegex(@"\(answered by:\s*person\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex PersonMarkRegex();

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

    // The default name of a Developer environment is its owner's name plus 's Environment (Learn shows both casings, and a second one as 's (1) environment). Case-sensitive, so prose such as "the user's environment" stays quiet unless it is on an environment: line.
    [GeneratedRegex(@"['’]s(?: \(\d+\))? Environment\b|^\s*environment:.*['’]s(?: \(\d+\))? environment\b")]
    private static partial Regex PersonalEnvironmentRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();
}