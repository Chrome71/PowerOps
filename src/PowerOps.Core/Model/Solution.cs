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
    IReadOnlyList<string>? CanvasAppsWithoutSource = null,
    IReadOnlyList<AppModule>? AppModules = null,
    IReadOnlyList<SiteMap>? SiteMaps = null,
    IReadOnlyList<CanvasAppMeta>? CanvasApps = null,
    IReadOnlyList<Agent>? Agents = null)
{
    /// <summary>The formulas of the canvas apps' `.pa.yaml` source: the YAML layout's `Src` folders, and the `Src/*.pa.yaml` inside each `.msapp` of the XML layouts.</summary>
    public IReadOnlyList<CanvasFormula> CanvasFormulas { get; init; } = CanvasFormulas ?? [];

    /// <summary>The connection references that an agent component maps in `Assets/botcomponent_connectionreferenceset.xml`; empty when the solution has none.</summary>
    public IReadOnlyList<string> AgentConnectionReferences { get; init; } = AgentConnectionReferences ?? [];

    /// <summary>The Power Pages components (enhanced data model) of `powerpagecomponents/`; empty when the solution has none.</summary>
    public IReadOnlyList<PowerPageComponent> PowerPageComponents { get; init; } = PowerPageComponents ?? [];

    /// <summary>The paths of the `.msapp` files of the XML layouts that hold no `Src/*.pa.yaml`; empty when every `.msapp` has source.</summary>
    public IReadOnlyList<string> CanvasAppsWithoutSource { get; init; } = CanvasAppsWithoutSource ?? [];

    /// <summary>The model-driven app modules of `AppModules/` or the `customizations.xml` `AppModules` element; empty when the solution has none.</summary>
    public IReadOnlyList<AppModule> AppModules { get; init; } = AppModules ?? [];

    /// <summary>The site maps of `AppModuleSiteMaps/` or the `customizations.xml` `AppModuleSiteMaps` element; empty when the solution has none.</summary>
    public IReadOnlyList<SiteMap> SiteMaps { get; init; } = SiteMaps ?? [];

    /// <summary>The canvas apps whose metadata names them (`CanvasApps/*.meta.xml` or the `customizations.xml` `CanvasApp` elements); empty when the solution has none.</summary>
    public IReadOnlyList<CanvasAppMeta> CanvasApps { get; init; } = CanvasApps ?? [];

    /// <summary>The Copilot Studio agents of `bots/&lt;folder&gt;/bot.xml`, in path order; empty when the solution has none.</summary>
    public IReadOnlyList<Agent> Agents { get; init; } = Agents ?? [];
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

/// <summary>A model-driven app: `SiteMaps` are the `schemaName`s of its type-62 components, `Pages` the names of its `appelements` custom pages.</summary>
public sealed record AppModule(string UniqueName, IReadOnlyList<string> SiteMaps, IReadOnlyList<string> Pages, string File);

/// <summary>A site map: `Pages` are the `Page` targets of its `SubArea`s, `GenPageIds` the normalized `GenPageId` targets (generative pages).</summary>
public sealed record SiteMap(string UniqueName, IReadOnlyList<string> Pages, IReadOnlyList<string> GenPageIds, string File);

/// <summary>
/// A canvas app named by its metadata (`File` is that metadata's path): `Type` is `CanvasAppType` (null when absent or not an integer; 2 is a custom page, 4 a code app),
/// `Connections` maps each `ConnectionReferences` key to its connector id ("" when it has none) and is null when that text is not a JSON object.
/// `Tables` and `Connectors` are the logical names that the type-1 and type-372 entries of `CdsDependencies` name, distinct ignoring case; `OtherTypes` are the distinct component types of its other entries (not 1, 372 or 29), in the order read. All three are null when that text is missing or not in the shape the reader knows.
/// </summary>
public sealed record CanvasAppMeta(string Name, int? Type, IReadOnlyDictionary<string, string>? Connections, string File, IReadOnlyList<string>? Tables, IReadOnlyList<string>? Connectors, IReadOnlyList<int>? OtherTypes);

/// <summary>
/// A Copilot Studio agent. `File` is its `bot.xml`. `AuthenticationMode` is the integer in `authenticationmode` (1 is None), null when missing or not an integer.
/// `Configuration` is null when `configuration.json` is missing or not a JSON object. `Instructions` is the text of the `instructions` key of the agent's one Custom GPT component ("" when the key is missing),
/// null when the agent has several of them or the value is not text; `InstructionsFile` is that component's `data`. An agent with no Custom GPT component takes the text of `configuration.json`'s `agentSettings.instructions.segments` instead (M51), with `InstructionsFile` that file and `InstructionsFromConfiguration` true.
/// `Tools` are its `TaskDialog` and `McpTool` components, in path order.
/// `UnreadFiles` are the `data` paths of its type-9 components that may be tools but whose YAML keys can't be mapped, in ordinal order.
/// `OtherComponents` are its type-9 components of a kind the reader doesn't read, in ordinal order of their file.
/// </summary>
public sealed record Agent(string SchemaName, string File, int? AuthenticationMode, AgentConfiguration? Configuration, string? Instructions, string? InstructionsFile, IReadOnlyList<AgentTool> Tools, IReadOnlyList<string>? UnreadFiles = null)
{
    /// <summary>The `data` paths of type-9 components that mention `TaskDialog` but whose YAML keys can't be mapped, in ordinal order, so the tool they may hold isn't read.</summary>
    public IReadOnlyList<string> UnreadFiles { get; init; } = UnreadFiles ?? [];

    /// <summary>The type-9 components whose top-level kind is not `TaskDialog`, `AdaptiveDialog` (a topic) or `AgentDialog` (a child agent), such as the new authoring UI's `McpTool`, in ordinal order of their file, so a tool they may hold isn't read (M50).</summary>
    public IReadOnlyList<AgentComponent> OtherComponents { get; init; } = [];

    /// <summary>The Work IQ workloads (type-9 `IQCapability` components) in ordinal order of their file (M51).</summary>
    public IReadOnlyList<AgentCapability> Capabilities { get; init; } = [];

    /// <summary>True when `Instructions` come from `configuration.json`; only the rules read it, because they read a Custom GPT component's instructions (M51).</summary>
    public bool InstructionsFromConfiguration { get; init; }

    /// <summary>True when the instructions come from a Custom GPT component and `configuration.json` has instructions that can't be read or that differ from them, ignoring `\r\n` and trailing line breaks (M51).</summary>
    public bool InstructionsConflict { get; init; }
}

/// <summary>A Work IQ workload: a type-9 `IQCapability` component with its `capability` and `access` (M51). `File` is its `data`.</summary>
public sealed record AgentCapability(string SchemaName, string Capability, string Access, string File);

/// <summary>A type-9 agent component the reader doesn't read: `Kind` is its top-level `kind` (null when it has none), `File` its `data`.</summary>
public sealed record AgentComponent(string? Kind, string File);

/// <summary>
/// What an agent's `configuration.json` says: the `$kind` of its `recognizer` (null when there is no `recognizer`, "" when it names no string kind), and `settings.GenerativeActionsEnabled` (null when absent or not a boolean).
/// `Instructions` is the joined `value`s of `agentSettings.instructions.segments` when all are `StaticSegment` items with text; `InstructionsUnread` is true when `agentSettings` has an `instructions` key in any other shape (M51).
/// </summary>
public sealed record AgentConfiguration(string? Recognizer, bool? GenerativeOrchestration, string? Instructions = null, bool InstructionsUnread = false);

/// <summary>
/// An agent tool (a type-9 component whose `data` has `kind: TaskDialog` or `kind: McpTool`; `File` is that `data`): `Action` is `action.kind`, `Mcp` is true for `ModelContextProtocolMetadata` operation details, and `Mode` is `action.connectionProperties.mode`.
/// An `McpTool` (`Kind`) has no `action`: `Mcp` is true, `Mode` is its `authMode`, and `ConnectionReference` and `OperationId` are its top-level keys of those names (M51); a `TaskDialog` leaves them null.
/// </summary>
public sealed record AgentTool(string SchemaName, string? Action, bool Mcp, string? Mode, string File, string? ConnectionReference = null, string? OperationId = null, string Kind = "TaskDialog");