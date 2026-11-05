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
        SolutionMissing, Version, Managed, ComponentMissing, ComponentExtra, UnmanagedLayer, LayersNotRead,
        VariableMissing, VariableValue, ReferenceMissing, ReferenceConnector, ReferenceUnbound, FlowMissing, FlowState,
        AlsoInSolution,
    ];

    /// <summary>Information kinds are shown but are not drift.</summary>
    public static bool IsDrift(string kind) => kind is not (Managed or LayersNotRead or AlsoInSolution);
}

public static class DriftCompare
{
    private const int WorkflowType = 29;

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

    /// <summary>The types <see cref="ComparePair"/> leaves out by id and compares under no kind of their own, with a name for each.</summary>
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
            notes.AddRange(Dropped.Where(d => compared.Any(s => s.Components.Any(c => c.Type == d.Type)))
                .Select(d => $"component type {d.Type}{(d.Name is null ? "" : $" ({d.Name})")} not compared: its ids change on import, and drift compares components by id"));
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

        var flowIds = a.Flows.Concat(b.Flows).Select(f => f.Id).ToHashSet();
        // Connection references are compared as references; Learn gives them no fixed componenttype, so they are left out by id.
        var referenceIds = a.References.Concat(b.References).Select(r => r.Id).ToHashSet();
        // Step 1 capture (2026-10-22): after an import, tables (1), canvas apps (300) and environment variables (380, 381) get new ids, so a Dev/Test comparison by id would always differ; they are left out (plan Assumption 2's fallback).
        // M12 manual run (2026-10-24): a model-driven app (80), its sitemap (62) and its app settings (10087) get new ids too, so they are left out the same way.
        // shortcut: 10087 was AppSetting in both of that run's environments, but codes above 10000 come from solutioncomponentdefinitions and may differ per org; match on its primaryentityname "appsetting" if one does.
        IEnumerable<(int Type, Guid Id)> Keys(Snapshot s) => s.Components
            .Where(c => !(c.Type == WorkflowType && flowIds.Contains(c.Id)) && !referenceIds.Contains(c.Id) && c.Type is not (380 or 381) && !Dropped.Any(d => d.Type == c.Type))
            .Select(c => (c.Type, c.Id));
        var (ka, kb) = (Keys(a).ToHashSet(), Keys(b).ToHashSet());
        foreach (var c in ka.Except(kb))
        {
            add(DriftKind.ComponentMissing, Component(c.Type, c.Id), null, null);
        }

        foreach (var c in kb.Except(ka))
        {
            add(DriftKind.ComponentExtra, Component(c.Type, c.Id), null, null);
        }

        Match(a.Variables, b.Variables, v => v.SchemaName, StringComparer.OrdinalIgnoreCase, DriftKind.VariableMissing,
            (x, y) => x.ValueHash != y.ValueHash ? (DriftKind.VariableValue, null, null) : null, add);
        Match(a.References, b.References, r => r.LogicalName, StringComparer.OrdinalIgnoreCase, DriftKind.ReferenceMissing,
            (x, y) => x.ConnectorId != y.ConnectorId ? (DriftKind.ReferenceConnector, x.ConnectorId, y.ConnectorId) : null, add,
            r => r.LogicalName);
        Match(a.Flows, b.Flows, f => f.Id, EqualityComparer<Guid>.Default, DriftKind.FlowMissing,
            (x, y) => x.State != y.State ? (DriftKind.FlowState, x.State, y.State) : null, add, f => f.Name);
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
}