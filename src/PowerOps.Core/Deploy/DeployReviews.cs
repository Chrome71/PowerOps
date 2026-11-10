using PowerOps.Core.Diff;
using PowerOps.Core.Model;
using PowerOps.Core.Plans;
using PowerOps.Core.Readers;

namespace PowerOps.Core.Deploy;

/// <summary>What a pac solution unpack of a zip gave: the digest of the unpacked folder, or why there is none (one of the two is set).</summary>
public sealed record ZipUnpack(string? Digest, string? Failure);

/// <summary>Which review reports under docs/plans cover a zip that a deploy imports, and what the other reviews of its solution say about it.</summary>
public static class DeployReviews
{
    /// <summary>What <c>diff</c> doesn't compare, in the words of the verify role (skills/powerops-verify/SKILL.md, Verify step 3).</summary>
    public static readonly IReadOnlyList<string> NotComparedByDiff = ["security roles", "tables", "columns", "forms", "views", "app modules and their role maps"];

    private const string Verify = "verify.md";
    private const string Security = "security-solution.md";

    /// <summary>Null when one plan folder covers the zip: both its reports exist, each is linked to it (see <see cref="Cover"/>), is ready and was written for the plan's current hash. Otherwise one sentence: why the first folder (ordinal) that records <paramref name="sha256"/> as a deploy-hash or a solution-hash doesn't cover it, or that none records it. Reads no file.</summary>
    /// <param name="unpack">What a pac solution unpack of the zip gave; null when none was run.</param>
    public static string? Problem(IReadOnlyList<PlanReviews> plans, string sha256, ZipUnpack? unpack = null)
    {
        if (Cover(plans, sha256, unpack) is not null)
        {
            return null;
        }

        if (plans.Where(p => Reports(p).Any(r => r.SolutionHash == sha256 || r.DeployHash == sha256)).MinBy(p => p.Plan, StringComparer.Ordinal) is not { } first)
        {
            return "no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its deploy-hash or its solution-hash";
        }

        return $"docs/plans/{first.Plan}: " + string.Join("; ", new[] { Fault(Verify, first.Verify, sha256, unpack), Fault(Security, first.Security, sha256, unpack) }.OfType<string>());
    }

    /// <summary>The first plan folder (ordinal) whose two reports are both ready, current and linked to the zip, or null. A report is linked when its solution-hash is <paramref name="sha256"/>, or when its deploy-hash is and <paramref name="unpack"/>'s digest is its solution-hash.</summary>
    public static PlanReviews? Cover(IReadOnlyList<PlanReviews> plans, string sha256, ZipUnpack? unpack) =>
        // Both reports of one folder: a verify.md of one plan and a security-solution.md of another are no cover.
        plans.Where(p => Fault(Verify, p.Verify, sha256, unpack) is null && Fault(Security, p.Security, sha256, unpack) is null).MinBy(p => p.Plan, StringComparer.Ordinal);

    /// <summary>Whether some verify.md or security-solution.md records <paramref name="sha256"/> as its deploy-hash, so a check needs the zip's unpack.</summary>
    public static bool NeedsUnpack(IReadOnlyList<PlanReviews> plans, string sha256) => plans.Any(p => Reports(p).Any(r => r.DeployHash == sha256));

    /// <summary>Whether any verify.md or security-solution.md, in any state, records <paramref name="sha256"/> as its solution-hash. A deploy-hash doesn't count, so a zip whose deploy-hash link fails still gets <see cref="Evidence"/>.</summary>
    public static bool Recorded(IReadOnlyList<PlanReviews> plans, string sha256) => plans.Any(p => Reports(p).Any(r => r.SolutionHash == sha256));

    private static IEnumerable<PlanReportFile> Reports(PlanReviews plan) => new[] { plan.Verify, plan.Security }.OfType<PlanReportFile>();

    // The first reason the report doesn't cover the zip, or null when it does. A Stale of null (no plan-hash line, or a plan that can't be read) is not current.
    private static string? Fault(string file, PlanReportFile? report, string sha256, ZipUnpack? unpack) =>
        report is null ? $"{file} is missing"
        : report.SolutionHash == sha256 ? Ready(file, report)
        : report.DeployHash != sha256 ? $"{file} records another deploy-hash and solution-hash"
        : unpack?.Digest is null ? $"{file} records this zip as its deploy-hash, but the zip's unpack could not be checked: {unpack?.Failure ?? "no unpack was given"}"
        : unpack.Digest != report.SolutionHash ? $"{file}'s solution-hash is not the digest of this zip's unpack, so it reviewed another folder"
        : Ready(file, report);

    private static string? Ready(string file, PlanReportFile report) =>
        report.State is null ? $"{file} has no state"
        : report.State != "ready" ? $"{file} is {report.State}"
        : report.Stale == false ? null
        : $"{file} was not written for the current plan";

    /// <summary>For each zip that no report records (<see cref="Recorded"/> false), keyed by its file name, the notes on the ready reviews of a solution with its unique name: one per input whose digest is still the reviewed one, with what <c>diff</c> finds between it and the zip, or one line counting the reviews whose input changed since. An input that is missing, passes through a link, leaves <paramref name="root"/> or can't be read gives no note. No sentence holds a full path.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Evidence(string root, IReadOnlyList<PlanReviews> plans, IReadOnlyList<DeployZip> zips) =>
        Evidence(root, plans, zips, path => SolutionLoader.Load(path));

    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> Evidence(string root, IReadOnlyList<PlanReviews> plans, IReadOnlyList<DeployZip> zips, Func<string, Solution> load)
    {
        var evidence = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var open = zips.Where(z => !Recorded(plans, z.Sha256)).ToList();
        if (open.Count == 0)
        {
            return evidence;
        }

        var ready = plans.SelectMany(p => Reports(p).Where(r => r.State == "ready" && r.Input is not null).Select(r => (p.Plan, Input: r.Input!, r.SolutionHash))).ToList();

        // Each distinct input is loaded once, and its digest read only when a zip has its unique name.
        var inputs = new Dictionary<string, (Solution Solution, string Digest)>(StringComparer.Ordinal);
        foreach (var input in ready.Select(r => r.Input).Distinct(StringComparer.Ordinal))
        {
            try
            {
                if (PlanChecker.InputPath(root, input) is not { } path)
                {
                    continue;
                }

                var solution = load(path);
                if (open.Any(z => SameName(z, solution)))
                {
                    inputs[input] = (solution, Manifest.Read(path).Digest);
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                // An input that can't be read gives no note; its message would print a full path.
            }
        }

        foreach (var zip in open)
        {
            var mine = ready.Where(r => inputs.TryGetValue(r.Input, out var i) && SameName(zip, i.Solution)).ToList();
            var current = mine.Where(r => r.SolutionHash == inputs[r.Input].Digest).ToList();
            var notes = new List<string>();
            foreach (var reviewed in current.GroupBy(r => r.Input, StringComparer.Ordinal))
            {
                if (Changes(inputs[reviewed.Key].Solution, zip.Solution) is { } changes)
                {
                    notes.Add($"{Folders(reviewed.Select(r => r.Plan))}: reviewed '{reviewed.Key}' (digest current); diff against the zip: {changes}; not compared by diff: {string.Join(", ", NotComparedByDiff)}; this is evidence, not a link");
                }
            }

            if (current.Count == 0 && mine.Count > 0)
            {
                notes.Add($"{Count(mine.Count, "ready report")} under docs/plans reviewed this solution, each in an input that changed since the review; none was compared with the zip");
            }

            evidence[zip.File] = notes;
        }

        return evidence;
    }

    private static bool SameName(DeployZip zip, Solution solution) => string.Equals(zip.Solution.UniqueName, solution.UniqueName, StringComparison.OrdinalIgnoreCase);

    // What diff finds besides the managed flag, or null when it fails.
    private static string? Changes(Solution input, Solution zip)
    {
        try
        {
            var diff = SolutionDiff.Compare(input, zip);
            var changes = diff.Changes.Count(c => !(c.Component == "solution" && c.Key == "managed"));
            return $"{Count(changes, "change")} besides the managed flag" + (diff.NotCompared.Count > 0 ? $"; diff could not read {Count(diff.NotCompared.Count, "part")}" : "");
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Folders(IEnumerable<string> plans)
    {
        var names = plans.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(p => $"docs/plans/{p}").ToList();
        return string.Join(", ", names.Take(3)) + (names.Count > 3 ? $" and {names.Count - 3} more" : "");
    }

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";
}