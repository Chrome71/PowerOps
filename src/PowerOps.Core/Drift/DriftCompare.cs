namespace PowerOps.Core.Drift;

/// <summary>One difference or notice found by <see cref="DriftCompare"/>. <paramref name="Position"/> is the environment's 1-based place in the input list.</summary>
public sealed record DriftItem(int Position, string Environment, string Kind, string Subject, bool Drift, string? Expected, string? Actual);

public static class DriftKind
{
    public const string SolutionMissing = "solution-missing";
    public const string Version = "version";
    public const string Managed = "managed";
    public const string ComponentMissing = "component-missing";
    public const string ComponentExtra = "component-extra";
    public const string AgentMissing = "agent-missing";
    public const string AgentPublished = "agent-published";
    public const string UnmanagedLayer = "unmanaged-layer";
    public const string LayersNotRead = "layers-not-read";
    public const string VariableMissing = "variable-missing";
    public const string VariableValue = "variable-value";
    public const string ReferenceMissing = "reference-missing";
    public const string ReferenceConnector = "reference-connector";
    public const string ReferenceUnbound = "reference-unbound";
    public const string FlowMissing = "flow-missing";
    public const string FlowState = "flow-state";
    public const string AlsoInSolution = "also-in-solution";

    /// <summary>All kinds in report order.</summary>
    public static readonly string[] Order =
    [
        SolutionMissing, Version, Managed, ComponentMissing, ComponentExtra, AgentMissing, AgentPublished, UnmanagedLayer, LayersNotRead,
        VariableMissing, VariableValue, ReferenceMissing, ReferenceConnector, ReferenceUnbound, FlowMissing, FlowState,
        AlsoInSolution,
    ];

    /// <summary>Information kinds are shown but are not drift.</summary>
    public static bool IsDrift(string kind) => kind is not (Managed or AgentPublished or LayersNotRead or AlsoInSolution);
}

public static class DriftCompare
{
    private const int WorkflowType = 29;

    // Types of 10000 and above come from each environment's component definitions, so their numbers can differ between environments.
    private const int DynamicType = 10000;

    /// <summary>Compares every snapshot after the first with the first, plus the checks of each single environment.</summary>
    public static IReadOnlyList<DriftItem> Compare(IReadOnlyList<Snapshot> snapshots)
    {
        var items = new List<DriftItem>();
        for (var i = 0; i < snapshots.Count; i++)
        {
            var s = snapshots[i];
            void Add(string kind, string subject, string? expected = null, string? actual = null) =>
                items.Add(new DriftItem(i + 1, s.Environment, kind, subject, DriftKind.IsDrift(kind), expected, actual));

            if (s.Installed)
            {
                var flowNames = s.Flows.ToDictionary(f => f.Id, f => f.Name);
                foreach (var c in s.Components.Where(c => c.UnmanagedLayer == true && s.Managed == true))
                {
                    Add(DriftKind.UnmanagedLayer, flowNames.TryGetValue(c.Id, out var n) ? n : Component(c.Type, c.Id));
                }

                foreach (var r in s.References.Where(r => !r.Bound))
                {
                    Add(DriftKind.ReferenceUnbound, r.LogicalName);
                }

                if (s.Managed == true && !s.LayersRead)
                {
                    Add(DriftKind.LayersNotRead, s.Solution);
                }

                foreach (var a in s.AlsoIn ?? [])
                {
                    Add(DriftKind.AlsoInSolution, flowNames.TryGetValue(a.Id, out var n) ? n : Component(a.Type, a.Id), null, a.Solution);
                }
            }

            if (i > 0)
            {
                ComparePair(snapshots[0], s, Add);
            }
        }

        return items
            .OrderBy(x => x.Position)
            .ThenBy(x => Array.IndexOf(DriftKind.Order, x.Kind))
            .ThenBy(x => x.Subject, StringComparer.Ordinal)
            .ThenBy(x => x.Actual, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The types <see cref="ComparePair"/> leaves out by id and compares under no kind of their own, with a name for each. 10087 only in a pair compared by number.</summary>
    private static readonly (int Type, string? Name)[] Dropped = [(1, "table"), (62, "site map"), (80, "model-driven app"), (300, "canvas app"), (10087, null)];

    /// <summary>What <see cref="Compare"/> did not compare, one sentence each, in a fixed order (M50 decisions Q7).</summary>
    public static IReadOnlyList<string> NotCompared(IReadOnlyList<Snapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return [];
        }

        var notes = new List<string>();
        // A pair is the first snapshot and another; only a pair with the solution installed in both compares more than that.
        var compared = snapshots[0].Installed ? snapshots.Where(s => s.Installed).ToList() : [];
        if (snapshots.Count == 1)
        {
            notes.Add("only one environment was read, so nothing was compared between environments; only its own checks ran");
        }
        else if (compared.Count < 2)
        {
            notes.Add("no environment has the solution installed together with the first, so only whether it is installed was compared");
        }
        else
        {
            // Positions (0-based) of each pair's other snapshot, by how the pair was compared, and the snapshots in such pairs, the first included.
            var first = snapshots[0];
            var byName = Enumerable.Range(1, snapshots.Count - 1).Where(i => snapshots[i].Installed && ByName(first, snapshots[i])).ToList();
            var byNumber = Enumerable.Range(1, snapshots.Count - 1).Where(i => snapshots[i].Installed && !ByName(first, snapshots[i])).ToList();
            static int[] InPairs(List<int> others) => others.Count == 0 ? [] : [0, .. others];
            var (named, numbered) = (InPairs(byName), InPairs(byNumber));

            notes.AddRange(Dropped.Where(d => (d.Type < DynamicType ? compared : numbered.Select(i => snapshots[i])).Any(s => s.Components.Any(c => c.Type == d.Type)))
                .Select(d => $"component type {d.Type}{(d.Name is null ? "" : $" ({d.Name})")} not compared: its ids change on import, and drift compares components by id"));
            notes.AddRange(named.SelectMany(i => snapshots[i].Components).Select(c => c.Name).Where(CountedByName).Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
                .Select(n => $"component type {n} not compared: its ids can change on import, so it is counted, not compared by id ({string.Join(", ", named.Select(i => $"{i + 1} {snapshots[i].Environment}: {snapshots[i].Components.Count(c => IsName(c.Name, n))}"))})"));
            notes.AddRange(byNumber.Where(i =>
                {
                    var (ka, kb) = Kept(first, snapshots[i]);
                    return ka.Concat(kb).Any(c => c.Type >= DynamicType);
                })
                .Select(i => $"{i + 1} {snapshots[i].Environment}: compared with the first by type number and id, because it or the first has no type names (a snapshot from an older powerops), so agents were not compared"));
            notes.AddRange(named.Select(i => (i, n: snapshots[i].Components.Count(c => (IsName(c.Name, "bot") || IsName(c.Name, "botcomponent")) && c.SchemaName is null))).Where(x => x.n > 0)
                .Select(x => $"{x.i + 1} {snapshots[x.i].Environment}: {x.n} agent(s) or agent component(s) were not returned by the read (the account may not see them), so they were compared by id"));
        }

        // This repeats the layers-not-read item on purpose: items are what differs, this list is what wasn't checked.
        for (var i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i] is { Installed: true, Managed: true, LayersRead: false } s)
            {
                notes.Add($"{i + 1} {s.Environment}: layers not read, so unmanaged layers there are not checked");
            }
        }

        return notes;
    }

    private static void ComparePair(Snapshot a, Snapshot b, Action<string, string, string?, string?> add)
    {
        if (a.Installed != b.Installed)
        {
            add(DriftKind.SolutionMissing, a.Solution, null, null);
            return;
        }

        if (!a.Installed)
        {
            return;
        }

        if (a.Version != b.Version)
        {
            add(DriftKind.Version, a.Solution, a.Version, b.Version);
        }

        if (a.Managed != b.Managed)
        {
            add(DriftKind.Managed, a.Solution, Managed(a.Managed), Managed(b.Managed));
        }

        var (keptA, keptB) = Kept(a, b);
        if (ByName(a, b))
        {
            // Keyed by definition name and schema name, so a type number or id that differs between environments matches.
            static string Key(SnapshotComponent c) => $"{c.Name ?? c.Type.ToString()} {c.SchemaName ?? c.Id.ToString("D")}";
            var (na, nb) = (keptA.Select(Key).ToList(), keptB.Select(Key).ToList());
            foreach (var c in na.Except(nb, StringComparer.OrdinalIgnoreCase))
            {
                add(DriftKind.ComponentMissing, c, null, null);
            }

            foreach (var c in nb.Except(na, StringComparer.OrdinalIgnoreCase))
            {
                add(DriftKind.ComponentExtra, c, null, null);
            }

            Match(a.Agents!, b.Agents!, x => x.SchemaName, StringComparer.OrdinalIgnoreCase, DriftKind.AgentMissing,
                (x, y) => x.PublishedOn != y.PublishedOn ? (DriftKind.AgentPublished, Published(x.PublishedOn), Published(y.PublishedOn)) : null, add);
        }
        else
        {
            var (ka, kb) = (keptA.Select(c => (c.Type, c.Id)).ToHashSet(), keptB.Select(c => (c.Type, c.Id)).ToHashSet());
            foreach (var c in ka.Except(kb))
            {
                add(DriftKind.ComponentMissing, Component(c.Type, c.Id), null, null);
            }

            foreach (var c in kb.Except(ka))
            {
                add(DriftKind.ComponentExtra, Component(c.Type, c.Id), null, null);
            }
        }

        Match(a.Variables, b.Variables, v => v.SchemaName, StringComparer.OrdinalIgnoreCase, DriftKind.VariableMissing,
            (x, y) => x.ValueHash != y.ValueHash ? (DriftKind.VariableValue, null, null) : null, add);
        Match(a.References, b.References, r => r.LogicalName, StringComparer.OrdinalIgnoreCase, DriftKind.ReferenceMissing,
            (x, y) => x.ConnectorId != y.ConnectorId ? (DriftKind.ReferenceConnector, x.ConnectorId, y.ConnectorId) : null, add,
            r => r.LogicalName);
        Match(a.Flows, b.Flows, f => f.Id, EqualityComparer<Guid>.Default, DriftKind.FlowMissing,
            (x, y) => x.State != y.State ? (DriftKind.FlowState, x.State, y.State) : null, add, f => f.Name);
    }

    /// <summary>Whether <paramref name="name"/> is <paramref name="expected"/>, ignoring case.</summary>
    internal static bool IsName(string? name, string expected) => string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

    // A pair is compared by name when both snapshots hold names (a live read or a version-2 file), else by number.
    private static bool ByName(Snapshot a, Snapshot b) => a.Agents is not null && b.Agents is not null;

    // The kinds whose rows can get new ids on import, left out by name and counted: app settings (10087 in the M12 run) and the agent link tables Learn lists on the botcomponent page.
    private static bool CountedByName(string? name) =>
        IsName(name, "appsetting") || IsName(name, "bot_botcomponent") || name?.StartsWith("botcomponent_", StringComparison.OrdinalIgnoreCase) == true;

    // The components a pair compares by key.
    private static (List<SnapshotComponent> A, List<SnapshotComponent> B) Kept(Snapshot a, Snapshot b)
    {
        var byName = ByName(a, b);
        var flowIds = a.Flows.Concat(b.Flows).Select(f => f.Id).ToHashSet();
        // Connection references are compared as references; Learn gives them no fixed componenttype, so they are left out by id.
        var referenceIds = a.References.Concat(b.References).Select(r => r.Id).ToHashSet();
        // Step 1 capture (2026-10-22): after an import, tables (1), canvas apps (300) and environment variables (380, 381) get new ids, so a Dev/Test comparison by id would always differ; they are left out (plan Assumption 2's fallback).
        // M12 manual run (2026-10-24): a model-driven app (80), its sitemap (62) and its app settings (10087) get new ids too, so they are left out the same way; by name, app settings are left out by their name instead.
        // By name, an agent with a schema name is compared as an agent only.
        bool Keep(SnapshotComponent c) =>
            !(c.Type == WorkflowType && flowIds.Contains(c.Id)) && !referenceIds.Contains(c.Id) && c.Type is not (380 or 381)
            && !Dropped.Any(d => d.Type == c.Type && (!byName || d.Type < DynamicType))
            && !(byName && (CountedByName(c.Name) || (IsName(c.Name, "bot") && c.SchemaName is not null)));
        return ([.. a.Components.Where(Keep)], [.. b.Components.Where(Keep)]);
    }

    // Reports a missing item for keys on one side only, and whatever <paramref name="diff"/> says for keys on both.
    private static void Match<T, TKey>(
        IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, TKey> key, IEqualityComparer<TKey> comparer, string missingKind,
        Func<T, T, (string Kind, string? Expected, string? Actual)?> diff, Action<string, string, string?, string?> add, Func<T, string>? name = null)
        where TKey : notnull
    {
        name ??= x => key(x)!.ToString()!;
        var (da, db) = (a.ToDictionary(key, comparer), b.ToDictionary(key, comparer));
        foreach (var x in a)
        {
            if (db.TryGetValue(key(x), out var y))
            {
                if (diff(x, y) is { } d)
                {
                    add(d.Kind, name(x), d.Expected, d.Actual);
                }
            }
            else
            {
                add(missingKind, name(x), null, null);
            }
        }

        foreach (var y in b.Where(y => !da.ContainsKey(key(y))))
        {
            add(missingKind, name(y), null, null);
        }
    }

    private static string Component(int type, Guid id) => $"{type} {id:D}";

    private static string Managed(bool? managed) => managed == true ? "managed" : "unmanaged";

    private static string Published(bool published) => published ? "published" : "not published";
}