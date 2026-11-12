namespace PowerOps.Core.Output;

/// <summary>The version every JSON output but <c>diff</c>, <c>deploy plan</c> and <c>drift</c> carries as <c>schemaVersion</c>; see docs/schemas/README.md for when it changes.</summary>
public static class JsonOutput
{
    public const int SchemaVersion = 1;

    /// <summary><c>diff</c> is at 3: it gained closed-enum values (BUILD-PLAN decision 48 for 2, M53's <c>same-version</c> risk for 3).</summary>
    public const int DiffSchemaVersion = 3;

    /// <summary><c>deploy plan</c> is at 3: its <c>kind</c> enum gained <c>agent</c> (M73, 2) and <c>stays</c> (M106, 3), and <c>deploy apply</c> reads only plan files of this version.</summary>
    public const int DeployPlanSchemaVersion = 3;

    /// <summary><c>drift</c> is at 2: its <c>kind</c> enum gained <c>agent-missing</c> and <c>agent-published</c> (M76).</summary>
    public const int DriftSchemaVersion = 2;
}