using System.Text.Json;
using System.Text.RegularExpressions;

namespace PowerOps.Core.Flows;

/// <summary>A connection reference entry of a cloud flow. <paramref name="LogicalName"/> is null for embedded/direct connections.</summary>
public sealed record FlowConnectionReference(string Key, string? LogicalName, string? ApiName);

/// <summary>A string value of the flow's definition. <paramref name="InParameters"/> is true under the definition's top-level <c>parameters</c> key; a path string per literal made a small deep file cost gigabytes.</summary>
public sealed record StringLiteral(bool InParameters, string Value);

/// <summary>A trigger of a cloud flow. <paramref name="Message"/> is the Dataverse subscription message (1 create, 2 delete, 3 update, ...), null when absent or not a number.</summary>
public sealed record FlowTrigger(string Name, string? OperationId, int? Message, string? Entity, string? FilteringAttributes);

/// <summary>A loop (<c>Foreach</c> or <c>Until</c>) that runs inside another loop, by action name; <paramref name="Outer"/> is the nearest enclosing loop.</summary>
public sealed record NestedLoop(string Outer, string Inner);

/// <summary>
/// An action of the Microsoft Dataverse connector (M79). <paramref name="EntitySet"/> is <c>parameters.entityName</c> and <paramref name="Expand"/> is <c>parameters.$expand</c>, each only when it is a string with no <c>@</c> (an expression), else null.
/// <paramref name="Binds"/> are the <c>&lt;name&gt;</c> of each <c>item/&lt;name&gt;@odata.bind</c> parameter key, in key order.
/// </summary>
public sealed record DataverseAction(string Name, string? EntitySet, string? Expand, IReadOnlyList<string> Binds);

/// <summary>The parts of a cloud flow's clientdata JSON that the analyzer rules need. <c>ActionCount</c> counts every action under <c>definition.actions</c>, containers and the actions in all their branches included, not the trigger. <c>Depth</c> is the number of containers around the deepest action (0 for a top-level action).</summary>
public sealed partial record FlowDefinition(
    IReadOnlyList<FlowConnectionReference> ConnectionReferences,
    IReadOnlyList<string> EnvironmentVariables,
    IReadOnlyList<string> ChildFlowIds,
    IReadOnlyList<StringLiteral> StringLiterals,
    IReadOnlyList<FlowTrigger> Triggers,
    int ActionCount,
    int Depth,
    IReadOnlyList<NestedLoop> NestedLoops)
{
    /// <summary>Every action of the Microsoft Dataverse connector (<c>inputs.host.apiId</c> ending in <c>/shared_commondataserviceforapps</c>, ignoring case), at any depth, in walk order.</summary>
    public IReadOnlyList<DataverseAction> DataverseActions { get; init; } = [];

    private static readonly string[] SkippedParameters = ["$connections", "$authentication"];
    private static readonly string[] SkippedProperties = ["metadata", "$schema", "operationMetadataId"];

    [GeneratedRegex(@"\(([^()\s]+)\)\s*$")]
    private static partial Regex LabelSuffix();

    [GeneratedRegex(@"^item/(.+)@odata\.bind$")]
    private static partial Regex BindKey();

    /// <summary>The environment variable schema name a flow parameter stands for: <c>metadata.schemaName</c>, else the <c>(schema)</c> suffix of its label. Null for <c>$connections</c>, <c>$authentication</c> and parameters that are no environment variable.</summary>
    internal static string? EnvironmentVariableSchema(string parameterName, string? metadataSchemaName)
    {
        if (SkippedParameters.Contains(parameterName))
        {
            return null;
        }

        var schema = metadataSchemaName;
        if (string.IsNullOrEmpty(schema) && LabelSuffix().Match(parameterName) is { Success: true } m)
        {
            schema = m.Groups[1].Value;
        }

        return string.IsNullOrEmpty(schema) ? null : schema;
    }

    /// <exception cref="InvalidDataException">The text is not a JSON object.</exception>
    public static FlowDefinition Parse(string clientDataJson)
    {
        try
        {
            // Parse takes raw strings, so a BOM that a stream reader would have removed can still be present.
            using var doc = JsonDocument.Parse(clientDataJson.TrimStart('\uFEFF'), new JsonDocumentOptions { MaxDepth = 256, AllowDuplicateProperties = false });
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("Flow definition JSON is not an object.");
            }

            var connections = new List<FlowConnectionReference>();
            var envVars = new List<string>();
            var children = new List<string>();
            var literals = new List<StringLiteral>();
            var triggers = new List<FlowTrigger>();
            var actionCount = 0;
            var maxDepth = 0;
            var nestedLoops = new List<NestedLoop>();
            var dataverse = new List<DataverseAction>();

            if (Get(doc.RootElement, "properties") is not { } props)
            {
                return new FlowDefinition(connections, envVars, children, literals, [], 0, 0, []);
            }

            if (Get(props, "connectionReferences") is { ValueKind: JsonValueKind.Object } refs)
            {
                foreach (var r in refs.EnumerateObject())
                {
                    connections.Add(new FlowConnectionReference(
                        r.Name,
                        Str(Get(Get(r.Value, "connection"), "connectionReferenceLogicalName")),
                        Str(Get(Get(r.Value, "api"), "name"))));
                }
            }

            if (Get(props, "definition") is { } definition)
            {
                if (Get(definition, "parameters") is { ValueKind: JsonValueKind.Object } parameters)
                {
                    foreach (var p in parameters.EnumerateObject())
                    {
                        if (EnvironmentVariableSchema(p.Name, Str(Get(Get(p.Value, "metadata"), "schemaName"))) is { } schema)
                        {
                            envVars.Add(schema);
                        }
                    }
                }

                if (Get(definition, "triggers") is { ValueKind: JsonValueKind.Object } triggerObjects)
                {
                    foreach (var t in triggerObjects.EnumerateObject())
                    {
                        var inputs = Get(t.Value, "inputs");
                        var triggerParams = Get(inputs, "parameters");
                        var message = Get(triggerParams, "subscriptionRequest/message");
                        triggers.Add(new FlowTrigger(
                            t.Name,
                            Str(Get(Get(inputs, "host"), "operationId")),
                            message switch
                            {
                                { ValueKind: JsonValueKind.Number } n when n.TryGetInt32(out var i) => i,
                                { ValueKind: JsonValueKind.String } sv when int.TryParse(sv.GetString(), out var i) => i,
                                _ => null,
                            },
                            Str(Get(triggerParams, "subscriptionRequest/entityname")),
                            Str(Get(triggerParams, "subscriptionRequest/filteringattributes"))));
                    }
                }

                WalkActions(definition, 0, null, ref actionCount, ref maxDepth, nestedLoops, dataverse);
                Walk(definition, 0, false, false, children, literals);
            }

            return new FlowDefinition(connections, envVars, children.Distinct().ToList(), literals, triggers, actionCount, maxDepth, nestedLoops) { DataverseActions = dataverse };
        }
        // InvalidOperationException: GetString on a string with a lone surrogate.
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new InvalidDataException("Flow definition is not valid JSON: " + ex.Message, ex);
        }
    }

    /// <summary>Counts the actions under <paramref name="holder"/>'s <c>actions</c> object, at <paramref name="depth"/>, and everything nested in them. A holder or <c>actions</c> value that is not an object is skipped.</summary>
    private static void WalkActions(JsonElement? holder, int depth, string? enclosingLoop, ref int count, ref int maxDepth, List<NestedLoop> nested, List<DataverseAction> dataverse)
    {
        if (Get(holder, "actions") is not { ValueKind: JsonValueKind.Object } actions)
        {
            return;
        }

        foreach (var a in actions.EnumerateObject())
        {
            count++;
            maxDepth = Math.Max(maxDepth, depth);
            var isLoop = Str(Get(a.Value, "type")) is { } type
                && (type.Equals("Foreach", StringComparison.OrdinalIgnoreCase) || type.Equals("Until", StringComparison.OrdinalIgnoreCase));
            if (isLoop && enclosingLoop is not null)
            {
                nested.Add(new NestedLoop(enclosingLoop, a.Name));
            }

            var inputs = Get(a.Value, "inputs");
            if (Str(Get(Get(inputs, "host"), "apiId")) is { } api && api.EndsWith("/shared_commondataserviceforapps", StringComparison.OrdinalIgnoreCase))
            {
                var parameters = Get(inputs, "parameters");
                dataverse.Add(new DataverseAction(
                    a.Name,
                    Literal(Get(parameters, "entityName")),
                    Literal(Get(parameters, "$expand")),
                    parameters is { ValueKind: JsonValueKind.Object } p
                        ? [.. p.EnumerateObject().Select(x => BindKey().Match(x.Name)).Where(m => m.Success).Select(m => m.Groups[1].Value)]
                        : []));
            }

            // Whatever the action's type, these hold the next level: its own actions, a Condition's else, a Switch's default and cases.
            var loop = isLoop ? a.Name : enclosingLoop;
            WalkActions(a.Value, depth + 1, loop, ref count, ref maxDepth, nested, dataverse);
            WalkActions(Get(a.Value, "else"), depth + 1, loop, ref count, ref maxDepth, nested, dataverse);
            WalkActions(Get(a.Value, "default"), depth + 1, loop, ref count, ref maxDepth, nested, dataverse);
            if (Get(a.Value, "cases") is { ValueKind: JsonValueKind.Object } cases)
            {
                foreach (var c in cases.EnumerateObject())
                {
                    WalkActions(c.Value, depth + 1, loop, ref count, ref maxDepth, nested, dataverse);
                }
            }
        }
    }

    private static void Walk(JsonElement e, int depth, bool inParameters, bool skipped, List<string> children, List<StringLiteral> literals)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in e.EnumerateObject())
                {
                    if (p.Name == "workflowReferenceName" && p.Value.ValueKind == JsonValueKind.String)
                    {
                        children.Add(p.Value.GetString()!.ToLowerInvariant());
                    }

                    // Skipped subtrees still contribute child flow ids, but no literals.
                    Walk(p.Value, depth + 1, inParameters || (depth == 0 && p.Name == "parameters"), skipped || SkippedProperties.Contains(p.Name), children, literals);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in e.EnumerateArray())
                {
                    Walk(item, depth + 1, inParameters, skipped, children, literals);
                }

                break;
            case JsonValueKind.String when !skipped:
                literals.Add(new StringLiteral(inParameters, e.GetString()!));
                break;
        }
    }

    private static JsonElement? Get(JsonElement? e, string name) =>
        e is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty(name, out var v) ? v : null;

    private static string? Str(JsonElement? e) => e is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;

    // An `@` starts an expression or an interpolation, whose value isn't known offline.
    private static string? Literal(JsonElement? e) => Str(e) is { } v && !v.Contains('@') ? v : null;
}