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
    IReadOnlyList<string>? AgentConnectionReferences = null)
{
    /// <summary>The formulas of the canvas apps' `.pa.yaml` source; empty unless the YAML layout provides them.</summary>
    public IReadOnlyList<CanvasFormula> CanvasFormulas { get; init; } = CanvasFormulas ?? [];

    /// <summary>The connection references that an agent component maps in `Assets/botcomponent_connectionreferenceset.xml`; empty when the solution has none.</summary>
    public IReadOnlyList<string> AgentConnectionReferences { get; init; } = AgentConnectionReferences ?? [];
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