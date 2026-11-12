using PowerOps.Core.Drift;
using PowerOps.Core.Settings;

namespace PowerOps.Core.Deploy;

/// <summary>What <c>deploy apply</c> did with one planned solution. <paramref name="PacExitCode"/> is set only when <c>pac</c> failed. <paramref name="After"/> is what the read after the imports showed, set only on an <c>ok</c> row when that read ran.</summary>
public sealed record AppliedSolution(int Order, string Solution, string Zip, string Action, string Import, int? PacExitCode, SolutionAfter? After = null);

/// <summary>The solution as the read after the imports showed it. <paramref name="Version"/> is null unless it is four numeric parts; <paramref name="Managed"/> is reported, not compared.</summary>
public sealed record SolutionAfter(bool Installed, string? Version, bool? Managed);

/// <summary>One planned cloud flow and its state after the imports (null in a dry run or when the second read failed).</summary>
public sealed record AppliedFlow(string Solution, string Name, string ExpectedState, string? StateAfter);

/// <summary>One agent of an imported zip and what the read-back found: <paramref name="State"/> is an <see cref="AgentState"/> (null in a dry run), <paramref name="PublishedOn"/> whether the row's <c>publishedon</c> is set (null unless found).</summary>
public sealed record AppliedAgent(string Solution, string Name, string? State, bool? PublishedOn);

/// <summary>How an apply was approved, as the caller reports it: provenance, not authentication. <paramref name="Receipt"/> is opaque text (the Action's run, attempt and environment), null for the CLI and the form.</summary>
public sealed record Approval(string Mode, string? Receipt);

/// <summary>The apply report. Holds no environment id or URL, folder path, connection id, setting value or <c>pac</c> text. <paramref name="AppliedAt"/> (UTC, to the second) is set once an <c>--apply</c> run started importing. <paramref name="AgentsReadAt"/> (UTC, to the second) is when the agent read-back returned, set only when an agent came back found or missing.</summary>
public sealed record DeployApplyResult(
    string Tool, bool Applied, string Target, string Hash, string Status, string? Message, string? AppliedAt, string Executor, Approval? Approval,
    IReadOnlyList<AppliedSolution> Solutions, IReadOnlyList<AppliedFlow> Flows, IReadOnlyList<AppliedAgent> Agents, string? AgentsReadAt = null);

/// <summary>The route a <c>deploy apply</c> ran by, as the caller reports it.</summary>
public static class ApplyExecutor
{
    public const string Cli = "cli";
    public const string Mcp = "mcp";
    public const string Action = "action";
}

/// <summary>How the yes to an apply was given, as the caller reports it.</summary>
public static class ApprovalMode
{
    public const string Cli = "cli";
    public const string Form = "form";
    public const string Action = "action";
}

public static class ApplyStatus
{
    public const string Refused = "refused";
    public const string Planned = "planned";
    public const string Imported = "imported";
    public const string Failed = "failed";
}

/// <summary>What the agent read-back found for one agent.</summary>
public static class AgentState
{
    public const string Found = "found";
    public const string Missing = "missing";
    public const string NotRead = "not-read";
}

public static class ImportStatus
{
    public const string Ok = "ok";
    public const string Failed = "failed";
    public const string Unknown = "unknown";
    public const string NotRun = "not-run";
    public const string Planned = "planned";
    public const string Skipped = "skipped";
}

/// <summary>One <c>pac solution import</c> to run: the plan position, the zip and settings file names (no paths) and whether it takes <c>--stage-and-upgrade</c>.</summary>
public sealed record ImportStep(int Order, string Solution, string Zip, string? Settings, bool StageAndUpgrade);

/// <summary>A planned flow paired with the id of its cloud flow in the zip.</summary>
public sealed record FlowRef(string Solution, PlannedFlow Flow, Guid Id);

/// <summary>The flows with their states after the imports, and whether every one is in its expected state.</summary>
public sealed record FlowsAfterResult(IReadOnlyList<AppliedFlow> Flows, bool AsExpected);

/// <summary>The decisions of <c>deploy apply</c> that need no I/O.</summary>
public static class DeployApply
{
    /// <summary>The message for plans whose hashes differ although every input is equal.</summary>
    public const string InputsEqualMessage = "the target environment, or the plan file's hash, differs";

    /// <summary>One plain sentence naming the first input that differs, in this order: a zip added, removed or changed; a settings file; the target's state of a solution; a dependency's version. Null when all are equal. Names only, no hash or value.</summary>
    public static string? ChangedInput(PlanInputs reviewed, PlanInputs now)
    {
        var then = reviewed.Zips.ToDictionary(z => z.Zip, StringComparer.Ordinal);
        var current = now.Zips.ToDictionary(z => z.Zip, StringComparer.Ordinal);
        var names = then.Keys.Union(current.Keys).Order(StringComparer.Ordinal).ToList();
        foreach (var n in names)
        {
            if (!then.TryGetValue(n, out var was))
            {
                return $"the zip {n} is not in the reviewed plan";
            }

            if (!current.TryGetValue(n, out var has))
            {
                return $"the zip {n} of the reviewed plan is gone";
            }

            if (was.ZipSha256 != has.ZipSha256)
            {
                return $"the zip {n} changed since the plan";
            }
        }

        foreach (var n in names)
        {
            var (was, has) = (then[n], current[n]);
            if (was.Settings is null != has.Settings is null)
            {
                return $"the settings file of {n} was {(was.Settings is null ? "added" : "removed")} since the plan";
            }

            if (was.Settings != has.Settings || was.SettingsSha256 != has.SettingsSha256)
            {
                return $"the settings file of {n} changed since the plan";
            }
        }

        if (names.FirstOrDefault(n => then[n].SnapshotSha256 != current[n].SnapshotSha256) is { } changed)
        {
            return $"the target's state of the solution in {changed} changed since the plan";
        }

        var oldDeps = reviewed.Dependencies.ToDictionary(d => d.Name, StringComparer.Ordinal);
        var newDeps = now.Dependencies.ToDictionary(d => d.Name, StringComparer.Ordinal);
        foreach (var n in oldDeps.Keys.Union(newDeps.Keys).Order(StringComparer.Ordinal))
        {
            if (!oldDeps.TryGetValue(n, out var was))
            {
                return $"the dependency {n} is not in the reviewed plan";
            }

            if (!newDeps.TryGetValue(n, out var has))
            {
                return $"the dependency {n} of the reviewed plan is no longer read";
            }

            if (was.Version != has.Version)
            {
                return $"the installed version of the dependency {n} changed since the plan";
            }
        }

        return null;
    }

    /// <summary>The imports to run, in plan order: an <c>install</c> or <c>upgrade</c> each, a <c>skip</c> none. Only an upgrade of a managed zip stages and upgrades.</summary>
    /// <exception cref="InvalidDataException">A planned solution's zip is not among <paramref name="zips"/>.</exception>
    public static IReadOnlyList<ImportStep> Imports(DeployPlanResult plan, IReadOnlyList<DeployZip> zips) =>
        plan.Solutions
            .Where(s => s.Action != PlanAction.Skip)
            .Select(s =>
            {
                var zip = ZipOf(s, zips);
                return new ImportStep(s.Order, s.Solution, zip.File, zip.SettingsFile, s.Action == PlanAction.Upgrade && zip.Solution.Managed);
            })
            .ToList();

    /// <summary>Pairs each planned flow with its cloud flow id: the n-th planned flow of a solution is the n-th of <see cref="FlowState.CloudFlows"/> (as <see cref="DeployPlan.Build"/> made them). Never matches by name.</summary>
    /// <exception cref="InvalidDataException">A zip is missing, or holds another number of cloud flows than the plan lists.</exception>
    public static IReadOnlyList<FlowRef> FlowIds(DeployPlanResult plan, IReadOnlyList<DeployZip> zips)
    {
        var refs = new List<FlowRef>();
        foreach (var s in plan.Solutions.Where(s => s.Flows.Count > 0))
        {
            var zip = ZipOf(s, zips);
            var cloud = FlowState.CloudFlows(zip.Solution);
            if (cloud.Count != s.Flows.Count || cloud.Any(f => !Guid.TryParse(f.Id, out _)))
            {
                throw new InvalidDataException($"{zip.File}: the zip's cloud flows are not the plan's");
            }

            refs.AddRange(s.Flows.Select((f, i) => new FlowRef(s.Solution, f, Guid.Parse(cloud[i].Id))));
        }

        return refs;
    }

    /// <summary>Each flow's state after the imports, read by id from the second read's snapshot of its solution (<see cref="FlowState.Missing"/> when absent), and whether all equal their expected state.</summary>
    public static FlowsAfterResult FlowsAfter(IReadOnlyList<FlowRef> flows, IReadOnlyList<Snapshot> after)
    {
        var applied = flows.Select(f =>
        {
            var snap = after.FirstOrDefault(s => string.Equals(s.Solution, f.Solution, StringComparison.OrdinalIgnoreCase));
            var state = snap?.Flows.FirstOrDefault(x => x.Id == f.Id)?.State ?? FlowState.Missing;
            return new AppliedFlow(f.Solution, f.Flow.Name, f.Flow.ExpectedState, state);
        }).ToList();
        return new FlowsAfterResult(applied, applied.All(f => f.StateAfter == f.ExpectedState));
    }

    /// <summary>The rows with <see cref="AppliedSolution.After"/> set on each <c>ok</c> row from the read after the imports, and one sentence per row that is not installed or not at its zip's version, or null.</summary>
    public static (IReadOnlyList<AppliedSolution> Solutions, string? Mismatch) SolutionsAfter(IReadOnlyList<AppliedSolution> rows, IReadOnlyList<DeployZip> zips, IReadOnlyList<Snapshot> after)
    {
        var sentences = new List<string>();
        var applied = rows.Select(row =>
        {
            if (row.Import != ImportStatus.Ok)
            {
                return row;
            }

            var zip = zips.First(z => z.File == row.Zip);
            var snap = after.FirstOrDefault(s => string.Equals(s.Solution, zip.Solution.UniqueName, StringComparison.OrdinalIgnoreCase));
            // Only a version of four numeric parts is kept, so no other text from the target reaches the report.
            var shown = snap is null ? new SolutionAfter(false, null, null)
                : new SolutionAfter(snap.Installed, DeployPlan.EqualVersions(snap.Version, snap.Version) ? snap.Version : null, snap.Managed);
            if (!shown.Installed)
            {
                sentences.Add($"pac reported success, but the target shows {row.Solution} not installed; check the solution's import history.");
            }
            else if (!DeployPlan.EqualVersions(shown.Version, zip.Solution.Version))
            {
                sentences.Add($"pac reported success, but the target shows {row.Solution} at version {shown.Version ?? "unknown"}; check the solution's import history.");
            }

            return row with { After = shown };
        }).ToList();
        return (applied, sentences.Count == 0 ? null : string.Join(' ', sentences));
    }

    /// <summary>The agents of the planned solutions <paramref name="include"/> accepts, as (solution, name) in plan order and then the order of <see cref="Model.Solution.Agents"/>, a name once per solution (ignoring case).</summary>
    /// <exception cref="InvalidDataException">A planned solution's zip is not among <paramref name="zips"/>.</exception>
    public static IReadOnlyList<(string Solution, string Name)> ExpectedAgents(DeployPlanResult plan, IReadOnlyList<DeployZip> zips, Func<PlannedSolution, bool> include) =>
        [.. plan.Solutions.Where(include).SelectMany(s =>
            ZipOf(s, zips).Solution.Agents.Select(a => a.SchemaName).Distinct(StringComparer.OrdinalIgnoreCase).Select(n => (s.Solution, n)))];

    /// <summary>Each expected agent with what the read found. <paramref name="rows"/> is null for a failed read; a failed read, and a name that is not a valid read name (never sent), give <see cref="AgentState.NotRead"/>.</summary>
    public static IReadOnlyList<AppliedAgent> AgentsAfter(IReadOnlyList<(string Solution, string Name)> expected, IReadOnlyList<(string SchemaName, bool PublishedOn)>? rows) =>
        [.. expected.Select(e =>
        {
            if (rows is null || !DriftQueries.IsValidAgentName(e.Name))
            {
                return new AppliedAgent(e.Solution, e.Name, AgentState.NotRead, null);
            }

            var published = rows.Where(r => string.Equals(r.SchemaName, e.Name, StringComparison.OrdinalIgnoreCase)).Select(r => (bool?)r.PublishedOn).FirstOrDefault();
            return published is { } isOn
                ? new AppliedAgent(e.Solution, e.Name, AgentState.Found, isOn)
                : new AppliedAgent(e.Solution, e.Name, AgentState.Missing, null);
        })];

    /// <summary>The agent name for a person to read: <paramref name="name"/> when <see cref="DriftQueries.IsValidAgentName"/> holds, else fixed text, because the name comes unchecked from the zip.</summary>
    public static string ShownAgentName(string name) =>
        DriftQueries.IsValidAgentName(name) ? name : "(name not shown: not only letters, digits and underscores, or over 100 characters)";

    private static DeployZip ZipOf(PlannedSolution s, IReadOnlyList<DeployZip> zips) =>
        zips.FirstOrDefault(z => z.File == s.Zip) ?? throw new InvalidDataException($"{s.Zip}: the zip is not among the loaded zips");
}