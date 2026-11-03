using System.Text.Json;

namespace PowerOps.Core.Model;

/// <summary>A solution (unpacked folder or exported zip) reduced to what the rules need. Paths in `File` are relative to the solution root, forward slashes.</summary>
public sealed record Solution(
    string UniqueName,
    string Version,
    bool Managed,
    Publisher Publisher,
    IReadOnlyList<RootComponent> RootComponents,
    IReadOnlyList<MissingDependency> MissingDependencies,
    IReadOnlyList<ConnectionReference> ConnectionReferences,
    IReadOnlyList<EnvironmentVariable> EnvironmentVariables,
    IReadOnlyList<Workflow> Workflows,
    IReadOnlyList<string> CanvasAppTexts,
    string ManifestFile = "Other/Solution.xml",
    IReadOnlyList<string>? SourceFolders = null,
    IReadOnlyList<CanvasFormula>? CanvasFormulas = null,
    IReadOnlyList<string>? AgentConnectionReferences = null,
    IReadOnlyList<PowerPageComponent>? PowerPageComponents = null,
    IReadOnlyList<string>? CanvasAppsWithoutSource = null)
{
    /// <summary>The formulas of the canvas apps' `.pa.yaml` source: the YAML layout's `Src` folders, and the `Src/*.pa.yaml` inside each `.msapp` of the XML layouts.</summary>
    public IReadOnlyList<CanvasFormula> CanvasFormulas { get; init; } = CanvasFormulas ?? [];

    /// <summary>The connection references that an agent component maps in `Assets/botcomponent_connectionreferenceset.xml`; empty when the solution has none.</summary>
    public IReadOnlyList<string> AgentConnectionReferences { get; init; } = AgentConnectionReferences ?? [];

    /// <summary>The Power Pages components (enhanced data model) of `powerpagecomponents/`; empty when the solution has none.</summary>
    public IReadOnlyList<PowerPageComponent> PowerPageComponents { get; init; } = PowerPageComponents ?? [];

    /// <summary>The paths of the `.msapp` files of the XML layouts that hold no `Src/*.pa.yaml`; empty when every `.msapp` has source.</summary>
    public IReadOnlyList<string> CanvasAppsWithoutSource { get; init; } = CanvasAppsWithoutSource ?? [];
}

public sealed record Publisher(string UniqueName, string Prefix);

public sealed record RootComponent(string Type, string? SchemaName, string? Id);

public sealed record DependencyNode(string Type, string? SchemaName, string? Id, string? DisplayName, string? Solution);

public sealed record MissingDependency(DependencyNode Required, DependencyNode Dependent);

public sealed record ConnectionReference(string LogicalName, string? ConnectorId, string File);

public sealed record EnvironmentVariable(
    string SchemaName,
    string? Type,
    bool IsRequired,
    string? DefaultValue,
    string? CurrentValue,
    string File,
    string? ValuesFile = null);

/// <summary>One Power Fx formula (a property value starting with `=`) of a canvas app; `Control` is the app, screen, component or control that holds it.</summary>
public sealed record CanvasFormula(string App, string Control, string Property, string Formula, string File);

public sealed record Workflow(string Id, string Name, int Category, int? StateCode, string File, string? ClientData);

/// <summary>One Power Pages component: `Id` is normalized, `Active` is true unless `statecode` is other than 0, and `Content` is the component's `content` JSON object (empty when it has none).</summary>
public sealed record PowerPageComponent(string Id, int Type, string Name, string? SiteId, bool Active, JsonElement Content, string File);