namespace PowerOps.Core.Output;

/// <summary>The version every JSON output but <c>diff</c> carries as <c>schemaVersion</c>; see docs/schemas/README.md for when it changes.</summary>
public static class JsonOutput
{
    public const int SchemaVersion = 1;

    /// <summary>Only <c>diff</c> is at 3: it gained closed-enum values (BUILD-PLAN decision 48 for 2, M53's <c>same-version</c> risk for 3).</summary>
    public const int DiffSchemaVersion = 3;
}