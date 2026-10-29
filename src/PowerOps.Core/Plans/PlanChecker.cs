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
    IReadOnlyList<string> Warnings);

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
    public static string? ResolvePlanFile(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }

        var file = System.IO.Path.Combine(path, "plan.md");
        return Directory.Exists(path) && File.Exists(file) ? file : null;
    }

    /// <summary>The plan files under <paramref name="path"/>: the path itself, a plan.md in it, or every docs/plans/*/plan.md (ordinal order).</summary>
    public static IReadOnlyList<string> Discover(string path)
    {
        if (ResolvePlanFile(path) is { } single)
        {
            return [single];
        }

        var plans = System.IO.Path.Combine(path, "docs", "plans");
        if (!Directory.Exists(plans))
        {
            return [];
        }

        return Directory.EnumerateDirectories(plans, "*", new EnumerationOptions { AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = true })
            .Select(d => System.IO.Path.Combine(d, "plan.md"))
            .Where(File.Exists)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public static PlanReport Check(string planFile, string baseDir)
    {
        var folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(planFile))!;
        var doc = PlanDocument.Parse(ReadPlan(planFile));
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

        if (doc.BodyHash is not null && File.Exists(reviewFile) && ReadScannable(reviewFile, baseDir, warnings) is { } review)
        {
            var line = Lines(review).FirstOrDefault(l => l.StartsWith("plan-hash:", StringComparison.Ordinal));
            if (line is null)
            {
                warnings.Add("review.md has no plan-hash line");
            }
            else if (line["plan-hash:".Length..].Trim() != doc.BodyHash)
            {
                warnings.Add("review.md was written for a different version of the plan (stale review)");
            }
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
            doc.BodyHash,
            doc.Header is null,
            violations,
            warnings);
    }

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
    internal static string ReadPlan(string planFile) =>
        new FileInfo(planFile).LinkTarget is not null
            ? throw new InvalidDataException($"{planFile}: is a symbolic link; symbolic links are not read.")
            : BoundedRead.ReadAllText(planFile, MaxFileBytes);

    /// <summary>The text of a file in a plan folder, or null with a warning when it is a link, binary or over 1 MB.</summary>
    private static string? ReadScannable(string file, string baseDir, ICollection<string> warnings)
    {
        string? skipped = null;
        byte[] bytes = [];
        if (new FileInfo(file).LinkTarget is not null)
        {
            skipped = "a symbolic link";
        }
        else
        {
            try
            {
                bytes = BoundedRead.ReadAllBytes(file, MaxFileBytes);
                skipped = Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8192)) >= 0 ? "a binary file" : null;
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

        using var reader = new StreamReader(new MemoryStream(bytes));
        return reader.ReadToEnd();
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
    internal static string Display(string path, string baseDir)
    {
        var rel = System.IO.Path.GetRelativePath(baseDir, path);
        var under = !System.IO.Path.IsPathRooted(rel) && rel != ".." && !rel.StartsWith(".." + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal);
        return (under ? rel : path).Replace('\\', '/');
    }

    [GeneratedRegex("https?://")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b")]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();
}