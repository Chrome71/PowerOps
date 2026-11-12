using System.Text.Json;
using System.Text.RegularExpressions;

namespace PowerOps.Core.Flows;

/// <summary>A connection reference entry of a cloud flow. <paramref name="LogicalName"/> is null for embedded/direct connections.</summary>
public sealed record FlowConnectionReference(string Key, string? LogicalName, string? ApiName);

/// <summary>A string value of the flow's definition. <paramref name="InParameters"/> is true under the definition's top-level <c>parameters</c> key; a path string per literal made a small deep file cost gigabytes.</summary>
public sealed record StringLiteral(bool InParameters, string Value);

/// <summary>A trigger of a cloud flow. <paramref name="Message"/> is the Dataverse subscription message (1 create, 2 delete, 3 update, ...), null when absent or not a number.</summary>
public sealed record FlowTrigger(string Name, string? OperationId, int? Message, string? Entity, string? FilteringAttributes)
{
    /// <summary>The trigger's <c>type</c>.</summary>
    public string? Type { get; init; }

    /// <summary><c>recurrence.frequency</c> when <see cref="Type"/> is <c>Recurrence</c> (ignoring case), else null.</summary>
    public string? Frequency { get; init; }

    /// <summary><c>recurrence.interval</c> when it is a number that fits an <c>int</c> or a string that reads as one, else null.</summary>
    public int? Interval { get; init; }
}

/// <summary>A loop (<c>Foreach</c> or <c>Until</c>) that runs inside another loop, by action name; <paramref name="Outer"/> is the nearest enclosing loop.</summary>
public sealed record NestedLoop(string Outer, string Inner);

/// <summary>
/// An action of the Microsoft Dataverse connector (M79). <paramref name="EntitySet"/> is <c>parameters.entityName</c> and <paramref name="Expand"/> is <c>parameters.$expand</c>, each only when it is a string with no <c>@</c> (an expression), else null.
/// <paramref name="Binds"/> are the <c>&lt;name&gt;</c> of each <c>item/&lt;name&gt;@odata.bind</c> parameter key, in key order.
/// </summary>
public sealed record DataverseAction(string Name, string? EntitySet, string? Expand, IReadOnlyList<string> Binds);

/// <summary>One entry of an action's <c>runAfter</c> object, in key order; <paramref name="Statuses"/> are the strings of its array as written (empty when the value is not an array).</summary>
public sealed record FlowRunAfter(string Action, IReadOnlyList<string> Statuses);

/// <summary>
/// An action of a cloud flow, at any depth. <paramref name="ApiId"/> and <paramref name="OperationId"/> are <c>inputs.host</c>'s; <paramref name="RunStatus"/> is <c>inputs.runStatus</c> of a <c>Terminate</c> only;
/// <paramref name="StaticResult"/> is true when <c>runtimeConfiguration.staticResult.staticResultOptions</c> is <c>Enabled</c>; <paramref name="Timeout"/> is the string <c>limit.timeout</c>.
/// <paramref name="Depth"/> counts the containers around the action; the next <paramref name="Descendants"/> entries of <see cref="FlowDefinition.Actions"/> are the actions inside it.
/// A field is null, empty, false or 0 when its key is missing or of another JSON kind.
/// </summary>
public sealed record FlowAction(string Name, string? Type, string? ApiId, string? OperationId, IReadOnlyList<FlowRunAfter> RunAfter, string? RunStatus, bool StaticResult, string? Timeout, int Depth, int Descendants);

/// <summary>The string <c>defaultValue</c> of a flow parameter bound to the environment variable <paramref name="SchemaName"/>, as written in the flow.</summary>
public sealed record EnvironmentVariableDefault(string SchemaName, string Value);

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

    /// <summary>Every action under <c>definition.actions</c>, at any depth, in walk order: an action, then its own <c>actions</c>, its <c>else</c>, its <c>default</c> and each of its <c>cases</c>.</summary>
    public IReadOnlyList<FlowAction> Actions { get; init; } = [];

    /// <summary>The string default of each parameter under <c>definition.parameters</c> bound to an environment variable, in key order.</summary>
    public IReadOnlyList<EnvironmentVariableDefault> EnvironmentVariableDefaults { get; init; } = [];

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
            var envDefaults = new List<EnvironmentVariableDefault>();
            var all = new List<FlowAction>();

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
                            if (Str(Get(p.Value, "defaultValue")) is { } value)
                            {
                                envDefaults.Add(new EnvironmentVariableDefault(schema, value));
                            }
                        }
                    }
                }

                if (Get(definition, "triggers") is { ValueKind: JsonValueKind.Object } triggerObjects)
                {
                    foreach (var t in triggerObjects.EnumerateObject())
                    {
                        var inputs = Get(t.Value, "inputs");
                        var triggerParams = Get(inputs, "parameters");
                        var type = Str(Get(t.Value, "type"));
                        var recurrence = Get(t.Value, "recurrence");
                        triggers.Add(new FlowTrigger(
                            t.Name,
                            Str(Get(Get(inputs, "host"), "operationId")),
                            Int(Get(triggerParams, "subscriptionRequest/message")),
                            Str(Get(triggerParams, "subscriptionRequest/entityname")),
                            Str(Get(triggerParams, "subscriptionRequest/filteringattributes")))
                        {
                            Type = type,
                            Frequency = string.Equals(type, "Recurrence", StringComparison.OrdinalIgnoreCase) ? Str(Get(recurrence, "frequency")) : null,
                            Interval = Int(Get(recurrence, "interval")),
                        });
                    }
                }

                WalkActions(definition, 0, null, ref actionCount, ref maxDepth, nestedLoops, dataverse, all);
                Walk(definition, 0, false, false, children, literals);
            }

            return new FlowDefinition(connections, envVars, children.Distinct().ToList(), literals, triggers, actionCount, maxDepth, nestedLoops) { DataverseActions = dataverse, EnvironmentVariableDefaults = envDefaults, Actions = all };
        }
        // InvalidOperationException: GetString on a string with a lone surrogate.
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new InvalidDataException("Flow definition is not valid JSON: " + ex.Message, ex);
        }
    }

    /// <summary>Counts and records the actions under <paramref name="holder"/>'s <c>actions</c> object, at <paramref name="depth"/>, and everything nested in them. A holder or <c>actions</c> value that is not an object is skipped.</summary>
    private static void WalkActions(JsonElement? holder, int depth, string? enclosingLoop, ref int count, ref int maxDepth, List<NestedLoop> nested, List<DataverseAction> dataverse, List<FlowAction> all)
    {
        if (Get(holder, "actions") is not { ValueKind: JsonValueKind.Object } actions)
        {
            return;
        }

        foreach (var a in actions.EnumerateObject())
        {
            count++;
            maxDepth = Math.Max(maxDepth, depth);
            var actionType = Str(Get(a.Value, "type"));
            var isLoop = actionType is { } type
                && (type.Equals("Foreach", StringComparison.OrdinalIgnoreCase) || type.Equals("Until", StringComparison.OrdinalIgnoreCase));
            if (isLoop && enclosingLoop is not null)
            {
                nested.Add(new NestedLoop(enclosingLoop, a.Name));
            }

            var inputs = Get(a.Value, "inputs");
            var host = Get(inputs, "host");
            var index = all.Count;
            all.Add(new FlowAction(
                a.Name,
                actionType,
                Str(Get(host, "apiId")),
                Str(Get(host, "operationId")),
                RunAfter(Get(a.Value, "runAfter")),
                string.Equals(actionType, "Terminate", StringComparison.OrdinalIgnoreCase) ? Str(Get(inputs, "runStatus")) : null,
                Str(Get(Get(Get(a.Value, "runtimeConfiguration"), "staticResult"), "staticResultOptions")) == "Enabled",
                Str(Get(Get(a.Value, "limit"), "timeout")),
                depth,
                0));

            if (all[index].ApiId is { } api && api.EndsWith("/shared_commondataserviceforapps", StringComparison.OrdinalIgnoreCase))
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
            WalkActions(a.Value, depth + 1, loop, ref count, ref maxDepth, nested, dataverse, all);
            WalkActions(Get(a.Value, "else"), depth + 1, loop, ref count, ref maxDepth, nested, dataverse, all);
            WalkActions(Get(a.Value, "default"), depth + 1, loop, ref count, ref maxDepth, nested, dataverse, all);
            if (Get(a.Value, "cases") is { ValueKind: JsonValueKind.Object } cases)
            {
                foreach (var c in cases.EnumerateObject())
                {
                    WalkActions(c.Value, depth + 1, loop, ref count, ref maxDepth, nested, dataverse, all);
                }
            }

            if (all.Count - index - 1 is > 0 and var descendants)
            {
                all[index] = all[index] with { Descendants = descendants };
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

    private static int? Int(JsonElement? e) => e switch
    {
        { ValueKind: JsonValueKind.Number } n when n.TryGetInt32(out var i) => i,
        { ValueKind: JsonValueKind.String } s when int.TryParse(s.GetString(), out var i) => i,
        _ => null,
    };

    private static IReadOnlyList<FlowRunAfter> RunAfter(JsonElement? e) =>
        e is { ValueKind: JsonValueKind.Object } o
            ? [.. o.EnumerateObject().Select(p => new FlowRunAfter(
                p.Name,
                p.Value.ValueKind == JsonValueKind.Array ? [.. p.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!)] : []))]
            : [];

    private static string? Str(JsonElement? e) => e is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;

    // An `@` starts an expression or an interpolation, whose value isn't known offline.
    private static string? Literal(JsonElement? e) => Str(e) is { } v && !v.Contains('@') ? v : null;
}