using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Core.Readers;
using PowerOps.Core.Settings;

namespace PowerOps.Core.Drift;

/// <summary>Reads and writes the snapshot file. Messages of rejections never quote the file.</summary>
public static partial class SnapshotFile
{
    /// <summary>The snapshot file's own version; it moves only when the file format changes, not with <c>JsonOutput.SchemaVersion</c>.</summary>
    public const int SchemaVersion = 2;

    // Written when no names were read, so the text and every deploy target digest are as before names existed.
    private const int VersionWithoutNames = 1;

    private const long MaxBytes = 1_048_576;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [GeneratedRegex(@"\A[0-9a-f]{64}\z")]
    private static partial Regex HashPattern();

    // Property order here is the order in the file.
    private sealed record Doc(
        int SchemaVersion, string Environment, string Solution, bool Installed, string? Version, bool? Managed, bool LayersRead,
        IReadOnlyList<SnapshotComponent> Components, IReadOnlyList<SnapshotVariable> Variables, IReadOnlyList<SnapshotReference> References, IReadOnlyList<SnapshotFlow> Flows,
        IReadOnlyList<SnapshotAgent> Agents);

    // The version-1 shape: its own component record, so name and schemaName never reach that text.
    private sealed record DocV1(
        int SchemaVersion, string Environment, string Solution, bool Installed, string? Version, bool? Managed, bool LayersRead,
        IReadOnlyList<ComponentV1> Components, IReadOnlyList<SnapshotVariable> Variables, IReadOnlyList<SnapshotReference> References, IReadOnlyList<SnapshotFlow> Flows);

    private sealed record ComponentV1(int Type, Guid Id, bool? UnmanagedLayer);

    /// <summary>The file text: lists sorted by their match key, "\n" line endings, trailing newline. Version 2 when <see cref="Snapshot.Agents"/> is set, else the version-1 text.</summary>
    public static string Write(Snapshot s)
    {
        var sorted = Sort(s);
        object doc = sorted.Agents is { } agents
            ? new Doc(SchemaVersion, sorted.Environment, sorted.Solution, sorted.Installed, sorted.Version, sorted.Managed, sorted.LayersRead,
                sorted.Components, sorted.Variables, sorted.References, sorted.Flows, agents)
            : new DocV1(VersionWithoutNames, sorted.Environment, sorted.Solution, sorted.Installed, sorted.Version, sorted.Managed, sorted.LayersRead,
                [.. sorted.Components.Select(c => new ComponentV1(c.Type, c.Id, c.UnmanagedLayer))], sorted.Variables, sorted.References, sorted.Flows);
        return JsonSerializer.Serialize(doc, Options) + "\n";
    }

    /// <exception cref="InvalidDataException">The file is over 1 MB or <see cref="Read"/> rejects it; the message never names the path.</exception>
    public static Snapshot Load(string path)
    {
        string text;
        try
        {
            text = BoundedRead.ReadAllText(path, MaxBytes);
        }
        catch (InvalidDataException)
        {
            // BoundedRead's message names the path; a snapshot's never does.
            throw new InvalidDataException($"a snapshot file is at most {MaxBytes} bytes");
        }

        return Read(text);
    }

    /// <exception cref="InvalidDataException">The text is not a snapshot file of this schema version.</exception>
    public static Snapshot Read(string json)
    {
        try
        {
            return Check(json);
        }
        catch (InvalidOperationException e)
        {
            // A string with a lone surrogate parses, then GetString throws.
            throw new InvalidDataException($"not a snapshot file: {e.Message}", e);
        }
    }

    private static Snapshot Check(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"not JSON (line {e.LineNumber}, position {e.BytePositionInLine})", e);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw Bad("the file is not a JSON object");
            }

            if (!root.TryGetProperty("schemaVersion", out var v) || v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out var version) || version is not (VersionWithoutNames or SchemaVersion))
            {
                throw Bad($"schemaVersion must be {VersionWithoutNames} or {SchemaVersion}");
            }

            var named = version == SchemaVersion;
            // Read first, so a version-1 text marked 2 is rejected for its missing agents.
            var agents = named ? Items(root, "agents", c => new SnapshotAgent(NonEmpty(c, "schemaName"), Flag(c, "publishedOn"))) : null;

            var snapshot = new Snapshot(
                NonEmpty(root, "environment"),
                NonEmpty(root, "solution"),
                Flag(root, "installed"),
                Text(root, "version", true),
                OptionalFlag(root, "managed"),
                Flag(root, "layersRead"),
                Items(root, "components", c => named ? Named(c) : new SnapshotComponent(Int(c, "type"), Id(c), OptionalFlag(c, "unmanagedLayer"))),
                Items(root, "variables", c => new SnapshotVariable(Text(c, "schemaName", false)!, Hash(c))),
                Items(root, "references", c => new SnapshotReference(Id(c), Text(c, "logicalName", false)!, Text(c, "connectorId", false)!, Flag(c, "bound"))),
                Items(root, "flows", c => new SnapshotFlow(Id(c), Text(c, "name", false)!, State(c))),
                null,
                agents);

            Unique(snapshot.Components.Select(c => $"{c.Type}/{c.Id}"), StringComparer.Ordinal, "components");
            if (named)
            {
                // Row 6's comparison key of a by-name pair.
                Unique(snapshot.Components.Select(c => $"{c.Name ?? c.Type.ToString()} {c.SchemaName ?? c.Id.ToString("D")}"), StringComparer.OrdinalIgnoreCase, "components");
                Unique(agents!.Select(a => a.SchemaName), StringComparer.OrdinalIgnoreCase, "agents");
            }

            Unique(snapshot.Variables.Select(c => c.SchemaName), StringComparer.OrdinalIgnoreCase, "variables");
            Unique(snapshot.References.Select(c => c.LogicalName), StringComparer.OrdinalIgnoreCase, "references");
            Unique(snapshot.Flows.Select(c => c.Id.ToString()), StringComparer.Ordinal, "flows");
            return Sort(snapshot);
        }
    }

    /// <summary>The snapshot with every list sorted by its match key (ordinal).</summary>
    public static Snapshot Sort(Snapshot s) => s with
    {
        Components = s.Components.OrderBy(c => c.Type).ThenBy(c => c.Id.ToString("D"), StringComparer.Ordinal).ToList(),
        Variables = s.Variables.OrderBy(c => c.SchemaName, StringComparer.Ordinal).ToList(),
        References = s.References.OrderBy(c => c.LogicalName, StringComparer.Ordinal).ToList(),
        Flows = s.Flows.OrderBy(c => c.Id.ToString("D"), StringComparer.Ordinal).ToList(),
        Agents = s.Agents?.OrderBy(a => a.SchemaName, StringComparer.Ordinal).ToList(),
    };

    // A version-2 component: a name only on a type of 10000 and above, a schema name only on a bot or botcomponent.
    private static SnapshotComponent Named(JsonElement c)
    {
        var (type, name, schemaName) = (Int(c, "type"), Text(c, "name", true), Text(c, "schemaName", true));
        if (name is not null && (name.Length == 0 || type < 10000))
        {
            throw Bad("a component name is empty or on a type below 10000");
        }

        if (schemaName is not null && (schemaName.Length == 0 || !(DriftCompare.IsName(name, "bot") || DriftCompare.IsName(name, "botcomponent"))))
        {
            throw Bad("a component schemaName is empty or not on a bot or botcomponent");
        }

        return new SnapshotComponent(type, Id(c), OptionalFlag(c, "unmanagedLayer"), name, schemaName);
    }

    private static InvalidDataException Bad(string why) => new($"not a snapshot file: {why}");

    private static void Unique(IEnumerable<string> keys, StringComparer comparer, string list)
    {
        if (!keys.All(new HashSet<string>(comparer).Add))
        {
            throw Bad($"{list} has a duplicate entry");
        }
    }

    private static JsonElement Prop(JsonElement o, string name, JsonValueKind kind) =>
        o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var p) && p.ValueKind == kind ? p : throw Bad($"{name} is missing or has the wrong type");

    private static string? Text(JsonElement o, string name, bool nullable)
    {
        if (nullable && o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return Prop(o, name, JsonValueKind.String).GetString();
    }

    private static string NonEmpty(JsonElement o, string name) =>
        Text(o, name, false) is { Length: > 0 } s ? s : throw Bad($"{name} is empty");

    private static bool Flag(JsonElement o, string name) =>
        o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? p.GetBoolean()
            : throw Bad($"{name} is missing or has the wrong type");

    private static bool? OptionalFlag(JsonElement o, string name) =>
        o.ValueKind == JsonValueKind.Object && o.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Null ? null : Flag(o, name);

    private static int Int(JsonElement o, string name) =>
        Prop(o, name, JsonValueKind.Number).TryGetInt32(out var n) ? n : throw Bad($"{name} is not an integer");

    private static Guid Id(JsonElement o) =>
        Guid.TryParse(Prop(o, "id", JsonValueKind.String).GetString(), out var id) ? id : throw Bad("an id is not a GUID");

    private static string? Hash(JsonElement o)
    {
        var h = Text(o, "valueHash", true);
        return h is null || HashPattern().IsMatch(h) ? h : throw Bad("a valueHash is not 64 lowercase hex characters");
    }

    private static string State(JsonElement o) =>
        Text(o, "state", false) is { } s && s is FlowState.On or FlowState.Off or FlowState.Suspended ? s : throw Bad("a flow state is not on, off or suspended");

    private static List<T> Items<T>(JsonElement o, string name, Func<JsonElement, T> read) =>
        Prop(o, name, JsonValueKind.Array).EnumerateArray().Select(read).ToList();
}