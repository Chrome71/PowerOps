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
        object? triggers = null,
        IDictionary<string, object>? actions = null)
    {
        actions = new Dictionary<string, object>(actions ?? new Dictionary<string, object>());
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

    public static CanvasFormula Formula(string formula, string control = "Button1", string property = "OnSelect", string app = "Demo") =>
        new(app, control, property, formula, "CanvasApps/demo_DocumentUri.msapp");

    public static ConnectionReference Ref(string name, string connector = "/providers/x") => new(name, connector, "Other/Customizations.xml");

    public static EnvironmentVariable Env(string name, bool required = false, string? def = null, string? cur = null) =>
        new(name, "100000000", required, def, cur, $"environmentvariabledefinitions/{name}/environmentvariabledefinition.xml",
            cur is null ? null : $"environmentvariabledefinitions/{name}/environmentvariablevalues.json");

    public static MissingDependency Dep(string type, string? schema, string? id = null, string? owner = "Other (1.0)") =>
        new(new DependencyNode(type, schema, id, null, owner), new DependencyNode("29", null, "x", null, null));

    public static AppModule App(string name = "demo_app", string[]? siteMaps = null, string[]? pages = null, string[]? roles = null) =>
        new(name, siteMaps ?? [], pages ?? [], $"AppModules/{name}/AppModule.xml") { Roles = roles ?? [] };

    /// <summary>A security role with these privileges; <see cref="SecurityRoleUnread"/> is one whose file had no `RolePrivileges` element. (<see cref="Role"/> is a Power Pages web role.)</summary>
    public static Role SecurityRole(string name, string id, params string[] privileges) => new(id, name, privileges, $"Roles/{name}.xml");

    public static Role SecurityRoleUnread(string name, string id) => new(id, name, null, $"Roles/{name}.xml");

    public static SiteMap SiteMap(string name = "demo_app", string[]? pages = null, string[]? genPageIds = null) =>
        new(name, pages ?? [], genPageIds ?? [], $"AppModuleSiteMaps/{name}/AppModuleSiteMap.xml");

    /// <summary>A canvas app whose metadata lists these (key, connector id) connections; <see cref="CanvasUnreadable"/> is one whose `ConnectionReferences` was not a JSON object.</summary>
    public static CanvasAppMeta Canvas(string name, params (string Key, string Id)[] connections) =>
        new(name, 2, connections.ToDictionary(c => c.Key, c => c.Id, StringComparer.OrdinalIgnoreCase), $"CanvasApps/{name}.meta.xml", [], [], []);

    public static CanvasAppMeta CanvasUnreadable(string name) => new(name, 2, null, $"CanvasApps/{name}.meta.xml", [], [], []);

    /// <summary>A canvas app (a code app by default) whose `CdsDependencies` names these tables and connectors; `with { Tables = null, Connectors = null, OtherTypes = null }` is one whose text was unreadable.</summary>
    public static CanvasAppMeta CodeApp(string name, IEnumerable<string>? tables = null, IEnumerable<string>? connectors = null, int? type = 4, IEnumerable<int>? otherTypes = null) =>
        new(name, type, new Dictionary<string, string>(), $"CanvasApps/{name}.meta.xml", (tables ?? []).ToList(), (connectors ?? []).ToList(), (otherTypes ?? []).ToList());

    /// <summary>An agent on the standard harness that no rule fires on; `with { Configuration = null }` is one whose configuration.json was missing. A tool is MCP when <paramref name="mcp"/> is true.</summary>
    public static Agent Agent(
        string name = "demo_agent",
        int? auth = 2,
        string? instructions = "Answer questions about orders.",
        bool? generative = true,
        string? recognizer = "GenerativeAIRecognizer",
        IEnumerable<AgentTool>? tools = null,
        IEnumerable<string>? unread = null) =>
        new(name, $"bots/{name}/bot.xml", auth, new AgentConfiguration(recognizer, generative), instructions, $"botcomponents/{name}.gpt.default/data", [.. tools ?? []], [.. unread ?? []]);

    public static AgentTool Tool(string name = "demo_agent.topic.Orders", bool mcp = false, string? action = null, string? mode = "Invoker") =>
        new(name, action ?? (mcp ? "InvokeExternalAgentTaskAction" : "InvokeConnectorTaskAction"), mcp, mode, $"botcomponents/{name}/data");

    private static int _nextId = 0x1000;

    /// <summary>A fake GUID that ends in <paramref name="n"/> (hex).</summary>
    public static string Guid(int n) => $"00000000-0000-0000-0000-{n:x12}";

    private static PowerPageComponent Component(string id, int type, string name, bool active, string site, object content) =>
        new(id, type, name, site, active, JsonSerializer.SerializeToElement(content), $"powerpagecomponents/{id}/powerpagecomponent.xml");

    /// <summary>A site setting; a null <paramref name="value"/> gives no `value` key, as nine of the core-portal settings have none.</summary>
    public static PowerPageComponent Setting(string name, string? value, bool active = true, string site = "00000000-0000-0000-0000-0000000000f1") =>
        Component(Guid(Interlocked.Increment(ref _nextId)), 9, name, active, site, value is null ? new { } : new { value });

    public static PowerPageComponent Role(string id, string name, bool anonymous = false, bool authenticated = false, bool active = true) =>
        Component(id, 11, name, active, "00000000-0000-0000-0000-0000000000f1", new { anonymoususersrole = anonymous, authenticatedusersrole = authenticated });

    public static PowerPageComponent Permission(
        string id,
        string name,
        string[]? roles = null,
        string table = "demo_item",
        object? parent = null,
        bool read = false,
        bool create = false,
        bool write = false,
        bool delete = false,
        bool append = false,
        bool appendTo = false,
        bool active = true) =>
        Component(id, 18, name, active, "00000000-0000-0000-0000-0000000000f1", new Dictionary<string, object?>
        {
            ["entitylogicalname"] = table,
            ["adx_entitypermission_webrole"] = roles ?? [],
            ["parententitypermission"] = parent,
            ["read"] = read,
            ["create"] = create,
            ["write"] = write,
            ["delete"] = delete,
            ["append"] = append,
            ["appendto"] = appendTo,
        });
}