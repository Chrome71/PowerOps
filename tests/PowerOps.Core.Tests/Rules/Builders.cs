using System.Text.Json;

using PowerOps.Core.Model;

namespace PowerOps.Core.Tests.Rules;

internal static class B
{
    public static Solution Empty(string prefix = "demo", string publisher = "demopub") => new(
        "Demo", "1.0.0.0", false, new Publisher(publisher, prefix),
        [], [], [], [], [], [], "Other/Solution.xml");

    public static Workflow Flow(
        string id = "11111111-1111-1111-1111-111111111111",
        string name = "Flow",
        int? state = 1,
        int category = 5,
        string[]? refs = null,
        string[]? children = null,
        string[]? literals = null,
        object? triggers = null)
    {
        var actions = new Dictionary<string, object>();
        var i = 0;
        foreach (var c in children ?? [])
        {
            actions["child" + i++] = new { type = "Workflow", inputs = new { host = new { workflowReferenceName = c } } };
        }

        foreach (var l in literals ?? [])
        {
            actions["lit" + i++] = new { type = "Compose", inputs = l };
        }

        var connectionReferences = (refs ?? []).ToDictionary(
            r => "shared_" + r,
            r => (object)new { connection = new { connectionReferenceLogicalName = r }, api = new { name = "shared_x" } });
        var json = JsonSerializer.Serialize(new { properties = new { connectionReferences, definition = new { actions, triggers = triggers ?? new { } } } });
        return new Workflow(id, name, category, state, $"Workflows/{name}-{id}.json", json);
    }

    /// <summary>A Dataverse "When a row is added, modified or deleted" trigger set.</summary>
    public static object RowTrigger(object message, string? filteringAttributes = null)
    {
        var parameters = new Dictionary<string, object> { ["subscriptionRequest/message"] = message, ["subscriptionRequest/entityname"] = "demo_item" };
        if (filteringAttributes is not null)
        {
            parameters["subscriptionRequest/filteringattributes"] = filteringAttributes;
        }

        return new Dictionary<string, object>
        {
            ["When_a_row"] = new { type = "OpenApiConnectionWebhook", inputs = new { host = new { operationId = "SubscribeWebhookTrigger" }, parameters } },
        };
    }

    public static ConnectionReference Ref(string name, string connector = "/providers/x") => new(name, connector, "Other/Customizations.xml");

    public static EnvironmentVariable Env(string name, bool required = false, string? def = null, string? cur = null) =>
        new(name, "100000000", required, def, cur, $"environmentvariabledefinitions/{name}/environmentvariabledefinition.xml",
            cur is null ? null : $"environmentvariabledefinitions/{name}/environmentvariablevalues.json");

    public static MissingDependency Dep(string type, string? schema, string? id = null, string? owner = "Other (1.0)") =>
        new(new DependencyNode(type, schema, id, null, owner), new DependencyNode("29", null, "x", null, null));
}