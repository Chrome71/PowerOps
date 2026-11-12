using System.Diagnostics;
using System.Text.Json;

using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;
using PowerOps.Core.Tests.Readers;
using PowerOps.Core.Tests.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

public class InventoryReportTests
{
    // Made-up ids, not of the 0000000x-0000-... form, so a leak would be visible.
    private const string G1 = "3f2a9c1e-7b4d-4e8a-9c2f-1a2b3c4d5e6f";
    private const string G2 = "8d7c6b5a-4e3f-4a2b-8c1d-9e8f7a6b5c4d";
    private const string Hex32 = "0a1b2c3d4e5f60718293a4b5c6d7e8f9";
    private const string Apis = "/providers/Microsoft.PowerApps/apis/";

    private static int _flowId;

    private static Workflow Flow(
        string name,
        Dictionary<string, object>? actions = null,
        Dictionary<string, object>? triggers = null,
        (string Key, string? Logical, string Api)[]? refs = null,
        string? id = null)
    {
        var connectionReferences = (refs ?? []).ToDictionary(
            r => r.Key,
            r => (object)new { connection = new { connectionReferenceLogicalName = r.Logical }, api = new { name = r.Api } });
        var json = JsonSerializer.Serialize(new { properties = new { connectionReferences, definition = new { triggers = triggers ?? [], actions = actions ?? [] } } });
        return new Workflow(id ?? $"a1b2c3d4-1111-4222-8333-{Interlocked.Increment(ref _flowId):x12}", name, 5, 1, $"Workflows/flow{_flowId}.json", json);
    }

    private static object Http(string uri, object? headers = null) => new { type = "Http", inputs = new { method = "GET", uri, headers } };

    private static CanvasAppMeta Canvas(string name, int? type = null, Dictionary<string, string>? connections = null, string[]? tables = null, string[]? connectors = null) =>
        new(name, type, connections ?? [], $"CanvasApps/{name}.meta.xml", tables ?? [], connectors ?? [], []);

    private static Inventory Build(Solution s, bool showValues = false, bool yaml = false) =>
        InventoryReport.Build([new LoadedSolution("demo", s, yaml)], showValues);

    private static InventorySolution One(Solution s, bool showValues = false, bool yaml = false) =>
        Assert.Single(Build(s, showValues, yaml).Solutions);

    private static string Text(Inventory i)
    {
        var w = new StringWriter();
        InventoryReport.WriteText(w, i);
        return w.ToString();
    }

    private static string Json(Inventory i)
    {
        var w = new StringWriter();
        InventoryReport.WriteJson(w, i);
        return w.ToString();
    }

    private static string Markdown(Inventory i)
    {
        var w = new StringWriter();
        InventoryReport.WriteMarkdown(w, i);
        return w.ToString();
    }

    private static string Notes(Inventory i)
    {
        var w = new StringWriter();
        InventoryReport.WriteNotes(w, i);
        return w.ToString();
    }

    private static string[] Formats(Inventory i) => [Text(i), Json(i), Markdown(i)];

    private static string[] Ids(InventorySolution s) => [.. s.LicenceDrivers.Select(d => d.Id)];

    [Fact]
    public void Connectors_gather_references_flows_apps_and_agent_tools_sorted()
    {
        var s = B.Empty() with
        {
            ConnectionReferences =
            [
                B.Ref("demo_RefB", Apis + "shared_office365"),
                B.Ref("demo_refA", Apis + "shared_Office365"),
                B.Ref("demo_Mcp", Apis + "shared_demo"),
                new ConnectionReference("demo_NoConnector", null, "Other/Customizations.xml"),
            ],
            Workflows =
            [
                Flow("Zeta flow", refs: [("shared_office365", "demo_RefB", "shared_office365"), ("shared_sharepointonline", null, "shared_sharepointonline")]),
                Flow("Alpha flow", refs: [("shared_office365", "demo_refa", "shared_office365")]),
            ],
            CanvasApps =
            [
                Canvas("Orders", connections: new() { ["a"] = Apis + "shared_sharepointonline", ["b"] = "" }),
                Canvas("Page", 2, new() { ["a"] = Apis + "shared_office365" }),
                Canvas("Code", 4, new() { ["a"] = "shared_office365" }),
            ],
            Agents = [B.Agent(tools: [new AgentTool("demo_agent.tool.Search", null, true, "Invoker", "botcomponents/x/data", ConnectionReference: "demo_mcp", Kind: "McpTool")])],
        };

        var c = One(s).Connectors;

        Assert.Equal(["shared_demo", "shared_office365", "shared_sharepointonline"], c.Select(x => x.Id));
        Assert.Equal(["demo_Mcp"], c[0].References);
        Assert.Equal([new InventoryUser("agentTool", "demo_agent.tool.Search")], c[0].Users);
        Assert.Equal(["demo_refA", "demo_RefB"], c[1].References);
        Assert.Equal(
            [new InventoryUser("codeApp", "Code"), new InventoryUser("customPage", "Page"), new InventoryUser("flow", "Alpha flow"), new InventoryUser("flow", "Zeta flow")],
            c[1].Users);
        Assert.Empty(c[2].References);
        Assert.Equal([new InventoryUser("canvasApp", "Orders"), new InventoryUser("flow", "Zeta flow")], c[2].Users);
        Assert.Equal(["shared_demo", "shared_office365", "shared_sharepointonline"], One(s).NotClassified.Select(n => n.Id));
    }

    [Fact]
    public void Custom_connectors_come_from_root_components_missing_dependencies_and_canvas_apps()
    {
        var s = B.Empty() with
        {
            RootComponents = [new RootComponent("372", "demo_Zed", null), new RootComponent("372", null, "x"), new RootComponent("1", "demo_table", null)],
            MissingDependencies = [B.Dep("372", "demo_Other"), B.Dep("372", "DEMO_ZED")],
            CanvasApps = [Canvas("App", connectors: ["demo_alpha", "demo_zed"])],
        };

        var r = One(s);

        Assert.Equal(
            [
                new InventoryCustomConnector("(no schema name)", true),
                new InventoryCustomConnector("demo_alpha", false),
                new InventoryCustomConnector("demo_Other", false),
                new InventoryCustomConnector("demo_Zed", true),
            ],
            r.CustomConnectors);
        var driver = Assert.Single(r.LicenceDrivers);
        Assert.Equal("custom-connector", driver.Id);
        Assert.Equal(["(no schema name)", "demo_alpha", "demo_Other", "demo_Zed"], driver.Where);
    }

    [Fact]
    public void A_missing_dependency_of_type_372_alone_fires_custom_connector()
    {
        var r = One(B.Empty() with { MissingDependencies = [B.Dep("372", "demo_Other")] });

        Assert.Equal([new InventoryCustomConnector("demo_Other", false)], r.CustomConnectors);
        Assert.Equal(["custom-connector"], Ids(r));
    }

    [Fact]
    public void Http_actions_in_flow_then_walk_order_with_host_only_when_shown()
    {
        var b = Flow("B flow", new()
        {
            ["Get"] = Http("https://api.contoso.com/v1/x"),
            ["Cond"] = new
            {
                type = "If",
                actions = new { },
                @else = new { actions = new { Inner = new { type = "HttpWebhook", inputs = new { subscribe = new { method = "POST", uri = "@parameters('demo_Base (demo_Base)')" } } } } },
            },
            ["Compose"] = new { type = "Compose", inputs = "x" },
        });
        var a = Flow("A flow", new() { ["Call"] = Http("@{variables('u')}") });
        var s = B.Empty() with { Workflows = [b, a] };

        Assert.Equal(
            [
                new InventoryHttpAction("B flow", "Get", "Http", "literal", null),
                new InventoryHttpAction("B flow", "Inner", "HttpWebhook", "variable", null),
                new InventoryHttpAction("A flow", "Call", "Http", "expression", null),
            ],
            One(s).HttpActions);
        Assert.Equal("api.contoso.com", One(s, showValues: true).HttpActions[0].Host);
        var driver = Assert.Single(One(s).LicenceDrivers);
        Assert.Equal("http-action", driver.Id);
        Assert.Equal(["flow 'B flow' action 'Get'", "flow 'B flow' action 'Inner'", "flow 'A flow' action 'Call'"], driver.Where);
    }

    [Fact]
    public void Request_triggers_in_key_order_with_kind_or_null()
    {
        var s = B.Empty() with
        {
            Workflows =
            [
                Flow("Triggers", triggers: new()
                {
                    ["manual"] = new { type = "Request", kind = "Http" },
                    ["button"] = new { type = "request", kind = "Button" },
                    ["nokind"] = new { type = "Request" },
                    ["rec"] = new { type = "Recurrence", recurrence = new { frequency = "Day", interval = 1 } },
                }),
            ],
        };

        var r = One(s);

        Assert.Equal(
            [new InventoryRequestTrigger("Triggers", "manual", "Http"), new InventoryRequestTrigger("Triggers", "button", "Button"), new InventoryRequestTrigger("Triggers", "nokind", null)],
            r.RequestTriggers);
        Assert.Empty(r.LicenceDrivers);
    }

    [Fact]
    public void Child_flows_give_the_name_in_the_solution_and_no_id_outside_it()
    {
        const string inside = "c1c1c1c1-2222-4333-8444-955566677788";
        const string outside = "d4d4d4d4-5555-4666-8777-988899900011";
        var child = new Workflow(inside, "Child flow", 5, 1, "Workflows/child.json", null);
        var parent = Flow("Parent", new()
        {
            ["CallIn"] = new { type = "Workflow", inputs = new { host = new { workflowReferenceName = "{" + inside.ToUpperInvariant() + "}" } } },
            ["CallOut"] = new { type = "Workflow", inputs = new { host = new { workflowReferenceName = outside } } },
        });
        var s = B.Empty() with { Workflows = [parent, child] };

        Assert.Equal([new InventoryChildFlow("Parent", "Child flow"), new InventoryChildFlow("Parent", null)], One(s).ChildFlows);
        foreach (var output in Formats(Build(s, showValues: true)))
        {
            Assert.DoesNotContain(outside, output, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("a flow not in this solution", Text(Build(s)), StringComparison.Ordinal);
        Assert.Contains("a flow not in this solution", Markdown(Build(s)), StringComparison.Ordinal);
    }

    [Fact]
    public void Dataverse_in_canvas_app_fires_on_a_table_but_not_for_code_apps_or_custom_pages()
    {
        var s = B.Empty() with
        {
            CanvasApps =
            [
                Canvas("Canvas Tables", tables: ["demo_item"]),
                Canvas("Code", 4, tables: ["demo_item"]),
                Canvas("Page", 2, new() { ["d"] = Apis + "shared_commondataserviceforapps" }, tables: ["demo_item"]),
            ],
        };

        var r = One(s);

        var driver = Assert.Single(r.LicenceDrivers);
        Assert.Equal("dataverse-in-canvas-app", driver.Id);
        Assert.Equal(["Canvas Tables"], driver.Where);
        Assert.Equal([new InventoryUser("customPage", "Page")], Assert.Single(r.Connectors).Users);
    }

    [Fact]
    public void Dataverse_in_canvas_app_fires_on_a_Dataverse_connection()
    {
        var r = One(B.Empty() with { CanvasApps = [Canvas("Orders", 0, new() { ["cds"] = Apis + "shared_commondataserviceforapps" })] });

        Assert.Equal(["dataverse-in-canvas-app"], Ids(r));
        Assert.Equal(["shared_commondataserviceforapps"], r.NotClassified.Select(n => n.Id));
    }

    [Fact]
    public void An_app_module_fires_model_driven_app()
    {
        var driver = Assert.Single(One(B.Empty() with { AppModules = [B.App("demo_orders")] }).LicenceDrivers);

        Assert.Equal("model-driven-app", driver.Id);
        Assert.Equal(["demo_orders"], driver.Where);
    }

    [Fact]
    public void The_observed_custom_connector_id_forms_resolve_from_an_app_and_a_flow()
    {
        var s = B.Empty() with
        {
            RootComponents = [new RootComponent("372", "cat_5Fenvironmentvariableconnector", null)],
            MissingDependencies = [B.Dep("372", "admin_5Foffice-20365-20management-20api")],
            CanvasApps = [Canvas("Env", connections: new() { ["c"] = Apis + "shared_cat-5fenvironmentvariableconnector-5f4c1d2e3f4a5b6c7d" })],
            Workflows = [Flow("Audit", refs: [("shared_admin", "demo_Audit", "shared_admin-5foffice-20365-20management-20api-5f8a9b0c1d2e3f4a5b")])],
        };

        var r = One(s);

        Assert.Equal(["admin_5foffice-20365-20management-20api", "cat_5fenvironmentvariableconnector"], r.Connectors.Select(c => c.Id));
        Assert.Equal([new InventoryUser("flow", "Audit")], r.Connectors[0].Users);
        Assert.Equal(["demo_Audit"], r.Connectors[0].References);
        Assert.Equal([new InventoryUser("canvasApp", "Env")], r.Connectors[1].Users);
        Assert.Empty(r.NotClassified);
        Assert.Equal(["custom-connector"], Ids(r));
    }

    [Fact]
    public void A_connector_id_equal_to_a_custom_connector_name_resolves()
    {
        var r = One(B.Empty() with
        {
            RootComponents = [new RootComponent("372", "demo_Custom", null)],
            ConnectionReferences = [B.Ref("demo_ref", Apis + "DEMO_custom")],
        });

        Assert.Equal(["demo_custom"], r.Connectors.Select(c => c.Id));
        Assert.Empty(r.NotClassified);
    }

    [Fact]
    public void When_two_custom_connector_names_prefix_an_id_the_longest_wins()
    {
        var r = One(B.Empty() with
        {
            RootComponents = [new RootComponent("372", "demo_a", null), new RootComponent("372", "demo_a_5Fb", null)],
            Workflows = [Flow("F", refs: [("k", "demo_ref", "shared_demo-a-5fb-5f0123456789abcdef")])],
        });

        Assert.Equal(["demo_a_5fb"], r.Connectors.Select(c => c.Id));
        Assert.Empty(r.NotClassified);
    }

    [Fact]
    public void A_desktop_flow_call_fires_desktop_flow_call_and_shared_uiflow_is_classified()
    {
        static object Ui(string op) => new { type = "OpenApiConnection", inputs = new { host = new { apiId = Apis + "shared_uiflow", operationId = op, connectionName = "x" } } };
        var s = B.Empty() with
        {
            Workflows =
            [
                Flow("Desk", new() { ["Run"] = Ui("RunUIFlow_V2"), ["Old"] = Ui("executedesktopuiflow"), ["Other"] = Ui("GetSomething") }, refs: [("shared_uiflow", "demo_ui", "shared_uiflow")]),
            ],
        };

        var r = One(s);

        var driver = Assert.Single(r.LicenceDrivers);
        Assert.Equal("desktop-flow-call", driver.Id);
        Assert.Equal(["flow 'Desk' action 'Run'", "flow 'Desk' action 'Old'"], driver.Where);
        Assert.Equal(["shared_uiflow"], r.Connectors.Select(c => c.Id));
        Assert.Empty(r.NotClassified);
    }

    [Fact]
    public void A_solution_with_none_of_these_has_no_driver_and_an_unlisted_connector_is_not_classified()
    {
        var r = One(B.Empty() with { Workflows = [Flow("Plain", new() { ["Compose"] = new { type = "Compose", inputs = "x" } }, refs: [("k", "demo_ref", "shared_demo")])] });

        Assert.Empty(r.LicenceDrivers);
        Assert.Equal([new InventoryNotClassified("shared_demo", "tier not classified: check the connector's reference page")], r.NotClassified);
    }

    [Fact]
    public void A_yaml_solution_gives_its_not_read_sentence_in_every_format_and_on_stderr()
    {
        var i = Build(B.Empty(), yaml: true);
        const string sentence = "canvas apps, model-driven apps and agents are not read in the YAML layout, so dataverse-in-canvas-app, model-driven-app and their users are not reported";

        Assert.Equal([sentence], Assert.Single(i.Solutions).NotRead);
        Assert.All(Formats(i), o => Assert.Contains(sentence, o, StringComparison.Ordinal));
        Assert.Equal($"note: demo: {sentence}\n", Notes(i).ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void A_canvas_app_with_unreadable_connections_or_tables_reports_nothing_of_it_but_its_sentence(bool connectionsNull, bool tablesNull)
    {
        var app = new CanvasAppMeta(
            "Orders",
            null,
            connectionsNull ? null : new Dictionary<string, string> { ["cds"] = Apis + "shared_commondataserviceforapps" },
            "CanvasApps/Orders.meta.xml",
            tablesNull ? null : ["demo_item"],
            tablesNull ? null : ["demo_cc"],
            null);
        var i = Build(B.Empty() with { CanvasApps = [app] });
        var r = Assert.Single(i.Solutions);
        const string sentence = "canvas app 'Orders': its connections or tables can't be read, so its connectors and drivers are not reported";

        Assert.Empty(r.Connectors);
        Assert.Empty(r.CustomConnectors);
        Assert.Empty(r.LicenceDrivers);
        Assert.Equal([sentence], r.NotRead);
        Assert.All(Formats(i), o => Assert.Contains(sentence, o, StringComparison.Ordinal));
        Assert.Equal($"note: demo: {sentence}\n", Notes(i).ReplaceLineEndings("\n"));
    }

    [Fact]
    public void The_driver_set_is_closed_and_its_text_never_says_standard()
    {
        Assert.Equal(["http-action", "custom-connector", "dataverse-in-canvas-app", "model-driven-app", "desktop-flow-call"], InventoryReport.Drivers.Select(d => d.Id));
        Assert.Equal(
            [
                "A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow.",
                "Custom connectors are premium: an app's users need Power Apps Premium or per app, and a flow needs Power Automate Premium or a Process licence.",
                "A canvas app that uses Dataverse needs Power Apps Premium or per app for its users.",
                "A model-driven app is premium: its users need Power Apps Premium or per app.",
                "Running a desktop flow from a cloud flow needs Power Automate Premium for the connection's user; an unattended run also needs a Process licence or the unattended RPA add-on.",
            ],
            InventoryReport.Drivers.Select(d => d.Need));
        const string designation = "https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation";
        const string types = "https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types";
        const string appsFaq = "https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq";
        Assert.Equal(
            [
                [designation, "https://learn.microsoft.com/training/modules/http-connectors/2-webhook", types],
                [designation, types],
                [appsFaq],
                [appsFaq],
                ["https://learn.microsoft.com/connectors/uiflow/", "https://learn.microsoft.com/troubleshoot/power-platform/power-automate/desktop-flows/desktop-flow-run-time-license-check", types],
            ],
            InventoryReport.Drivers.Select(d => d.Sources));
        Assert.All(InventoryReport.Drivers, d => Assert.Equal("2026-11-12", d.Verified));

        // Every section, the footer and both notRead sentences in all three formats.
        var i = InventoryReport.Build(
            [
                new LoadedSolution("demo", Everything(), false),
                new LoadedSolution("yaml", B.Empty(), true),
            ],
            showValues: false);
        Assert.All(Formats(i), o => Assert.DoesNotContain("standard", o, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(InventoryReport.Footer, Text(i), StringComparison.Ordinal);
        Assert.Contains(InventoryReport.Footer, Markdown(i), StringComparison.Ordinal);
        Assert.Equal(InventoryReport.Drivers.Select(d => d.Id), Ids(i.Solutions[0]));
    }

    /// <summary>A solution that fills every section and fires every driver.</summary>
    private static Solution Everything() => B.Empty() with
    {
        RootComponents = [new RootComponent("372", "demo_Custom", null)],
        ConnectionReferences = [B.Ref("demo_ref", Apis + "shared_demo")],
        Workflows =
        [
            Flow(
                "Main",
                new()
                {
                    ["Get"] = Http("https://api.contoso.com/v1/x"),
                    ["Run"] = new { type = "OpenApiConnection", inputs = new { host = new { apiId = Apis + "shared_uiflow", operationId = "RunUIFlow_V2" } } },
                    ["Call"] = new { type = "Workflow", inputs = new { host = new { workflowReferenceName = "d4d4d4d4-5555-4666-8777-988899900011" } } },
                },
                new() { ["manual"] = new { type = "Request", kind = "Http" } },
                [("k", "demo_ref", "shared_demo")]),
        ],
        CanvasApps = [Canvas("Orders", tables: ["demo_item"]), new CanvasAppMeta("Broken", null, null, "CanvasApps/Broken.meta.xml", null, null, null)],
        AppModules = [B.App("demo_orders")],
    };

    [Fact]
    public void Json_has_schema_version_1_every_array_and_the_nullable_fields()
    {
        using var doc = JsonDocument.Parse(Json(Build(Everything())));
        var root = doc.RootElement;

        Assert.Equal(["schemaVersion", "solutions"], root.EnumerateObject().Select(p => p.Name));
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        var s = Assert.Single(root.GetProperty("solutions").EnumerateArray().ToList());
        Assert.Equal(
            ["path", "uniqueName", "licenceDrivers", "notClassified", "connectors", "customConnectors", "httpActions", "requestTriggers", "childFlows", "notRead"],
            s.EnumerateObject().Select(p => p.Name));
        Assert.Equal(["id", "need", "sources", "verified", "where"], s.GetProperty("licenceDrivers")[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(["id", "references", "users"], s.GetProperty("connectors")[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(["kind", "name"], s.GetProperty("connectors")[0].GetProperty("users")[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(["name", "inSolution"], s.GetProperty("customConnectors")[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(JsonValueKind.Null, s.GetProperty("httpActions")[0].GetProperty("host").ValueKind);
        Assert.Equal(JsonValueKind.Null, s.GetProperty("childFlows")[0].GetProperty("child").ValueKind);

        using var empty = JsonDocument.Parse(Json(Build(B.Empty() with { Workflows = [Flow("T", triggers: new() { ["t"] = new { type = "Request" } })] })));
        var e = empty.RootElement.GetProperty("solutions")[0];
        Assert.Equal(JsonValueKind.Null, e.GetProperty("requestTriggers")[0].GetProperty("kind").ValueKind);
        Assert.All(e.EnumerateObject().Where(p => p.Name is not ("path" or "uniqueName" or "requestTriggers")), p => Assert.Equal(0, p.Value.GetArrayLength()));
        Assert.DoesNotContain('\r', Json(Build(Everything())));
    }

    [Fact]
    public void Text_lists_each_section_with_none_when_empty_and_the_footer()
    {
        var s = B.Empty() with { Workflows = [Flow("F", new() { ["Get"] = Http("https://api.contoso.com/v1/x") })] };
        var text = Text(Build(s));

        Assert.Equal(
            "demo (Demo)\n" +
            "  licenceDrivers (1):\n" +
            "    http-action: A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow. " +
            "where: flow 'F' action 'Get' sources: https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation " +
            "https://learn.microsoft.com/training/modules/http-connectors/2-webhook https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types\n" +
            "  notClassified (0):\n    none\n" +
            "  connectors (0):\n    none\n" +
            "  customConnectors (0):\n    none\n" +
            "  httpActions (1):\n    flow 'F' action 'Get' Http literal host not shown (--show-values)\n" +
            "  requestTriggers (0):\n    none\n" +
            "  childFlows (0):\n    none\n" +
            "  notRead (0):\n    none\n" +
            "Drivers come from a closed set with sources; a solution with no driver listed may still need a premium licence.\n",
            text);
        Assert.Contains("    flow 'F' action 'Get' Http literal host api.contoso.com\n", Text(Build(s, showValues: true)), StringComparison.Ordinal);
    }

    [Fact]
    public void Text_prints_connectors_custom_connectors_triggers_and_child_flows()
    {
        var text = Text(Build(Everything()));

        Assert.Contains("    shared_demo: references demo_ref; users flow 'Main'\n", text, StringComparison.Ordinal);
        Assert.Contains("    shared_demo: tier not classified: check the connector's reference page\n", text, StringComparison.Ordinal);
        Assert.Contains("    demo_Custom (in solution)\n", text, StringComparison.Ordinal);
        Assert.Contains("    flow 'Main' trigger 'manual' kind Http\n", text, StringComparison.Ordinal);
        Assert.Contains("    flow 'Main' calls a flow not in this solution\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_has_a_table_per_section_none_when_empty_and_escaped_cells()
    {
        var s = B.Empty() with { Workflows = [Flow("F|*x*", new() { ["Get"] = Http("https://api.contoso.com/v1/x") })] };
        var md = Markdown(Build(s));

        Assert.StartsWith("## demo (Demo)\n\n### licenceDrivers\n\n| Driver | Needs | Where | Sources |\n|---|---|---|---|\n| http-action | ", md, StringComparison.Ordinal);
        Assert.Contains("\n### httpActions\n\n| Flow | Action | Type | Endpoint | Host |\n|---|---|---|---|---|\n| F\\|\\*x\\* | Get | Http | literal | not shown |\n", md, StringComparison.Ordinal);
        Assert.Contains("\n### notClassified\n\nNone.\n", md, StringComparison.Ordinal);
        Assert.EndsWith("\n\nDrivers come from a closed set with sources; a solution with no driver listed may still need a premium licence.\n", md, StringComparison.Ordinal);
        Assert.Contains("| F\\|\\*x\\* | Get | Http | literal | api.contoso.com |\n", Markdown(Build(s, showValues: true)), StringComparison.Ordinal);

        var expression = Markdown(Build(B.Empty() with { Workflows = [Flow("F", new() { ["Get"] = Http("@{variables('u')}") })] }));
        Assert.Contains("| F | Get | Http | expression |  |\n", expression, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_output_holds_no_host_path_id_address_or_header_value_and_show_values_adds_the_host_only()
    {
        // An '@' would make the URL an expression, so the address is percent-encoded as a query holds it.
        var s = B.Empty() with
        {
            Workflows =
            [
                Flow("Fetch", new() { ["Get"] = Http($"https://api.contoso.com/v1/orders?id={G1}&to=pat%40contoso.com", new Dictionary<string, string> { ["X-Api-Key"] = "hdr-value-71" }) }),
            ],
        };
        string[] never = ["/v1/orders", G1, "pat%40", "pat@", "hdr-value-71"];

        foreach (var output in Formats(Build(s)))
        {
            Assert.DoesNotContain("contoso", output, StringComparison.OrdinalIgnoreCase);
            Assert.All(never, v => Assert.DoesNotContain(v, output, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var output in Formats(Build(s, showValues: true)))
        {
            Assert.Contains("api.contoso.com", output, StringComparison.Ordinal);
            Assert.All(never, v => Assert.DoesNotContain(v, output, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Names_holding_a_url_an_id_or_an_address_are_replaced_unless_shown()
    {
        var flow = $"Sync https://contoso.sharepoint.com/sites/x {G1} pat@contoso.com";
        var action = $"Post {{{G2}}} {Hex32} ops@contoso.com";
        var s = B.Empty() with { Workflows = [Flow(flow, new() { [action] = Http("@{variables('u')}") })] };

        var r = One(s);
        Assert.Equal(new InventoryHttpAction("Sync [redacted URL] [id] [redacted address]", "Post [id] [id] [redacted address]", "Http", "expression", null), Assert.Single(r.HttpActions));
        foreach (var output in Formats(Build(s)))
        {
            Assert.All(["sharepoint", G1, G2, Hex32, "pat@", "ops@", "contoso"], v => Assert.DoesNotContain(v, output, StringComparison.OrdinalIgnoreCase));
            Assert.Contains("redacted URL", output, StringComparison.Ordinal);
            Assert.Contains("redacted address", output, StringComparison.Ordinal);
        }

        var shown = Build(s, showValues: true);
        Assert.Equal(new InventoryHttpAction(flow, action, "Http", "expression", null), Assert.Single(shown.Solutions[0].HttpActions));
        Assert.Contains(flow, Text(shown), StringComparison.Ordinal);
        Assert.Contains(action, Text(shown), StringComparison.Ordinal);
        Assert.Contains(G1, Markdown(shown), StringComparison.Ordinal);
        Assert.Contains(Hex32, Markdown(shown), StringComparison.Ordinal);
        Assert.Contains(G2, Json(shown), StringComparison.Ordinal);
    }

    [Fact]
    public void A_name_with_the_word_Standard_prints_as_written()
    {
        var i = Build(B.Empty() with { Workflows = [Flow("Standard approvals", triggers: new() { ["manual"] = new { type = "Request", kind = "Button" } })] });

        Assert.All(Formats(i), o => Assert.Contains("Standard approvals", o, StringComparison.Ordinal));
    }

    [Fact]
    public void Names_are_made_distinct_before_redaction_so_two_may_print_alike()
    {
        var r = One(B.Empty() with { ConnectionReferences = [B.Ref($"demo_ref_{G1}", Apis + "shared_demo"), B.Ref($"demo_ref_{G2}", Apis + "shared_demo")] });

        Assert.Equal(["demo_ref_[id]", "demo_ref_[id]"], Assert.Single(r.Connectors).References);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a.")]
    [InlineData("a@")]
    [InlineData("0-")]
    [InlineData("f")]
    [InlineData("x://")]
    public void Redaction_of_a_very_long_name_runs_in_linear_time(string unit)
    {
        var name = string.Concat(Enumerable.Repeat(unit, 200_000 / unit.Length));
        var watch = Stopwatch.StartNew();

        var result = InventoryReport.Redact(name);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), watch.Elapsed.ToString());
        Assert.NotNull(result);
        Assert.Equal("[id]", InventoryReport.Redact(new string('f', 200_000)));
    }

    [Fact]
    public void Solutions_keep_the_order_given()
    {
        var i = InventoryReport.Build([new LoadedSolution("z", B.Empty(), false), new LoadedSolution("a", B.Empty() with { UniqueName = "Other" }, false)]);

        Assert.Equal(["z", "a"], i.Solutions.Select(s => s.Path));
        Assert.Equal(["Demo", "Other"], i.Solutions.Select(s => s.UniqueName));
    }

    [Fact]
    public void LoadFound_shows_a_yaml_solution_as_its_root_and_reads_each_path_once()
    {
        var root = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed-yaml", "v1");
        var found = SolutionDiscovery.Find(root);

        var loaded = AnalysisResult.LoadFound([.. found, .. found], Golden.RepoRoot);

        var s = Assert.Single(loaded);
        Assert.True(s.FromYaml);
        Assert.Equal("tests/fixtures/powerops-seed-yaml/v1", s.Path);
        Assert.Equal(AnalysisResult.RunFound(found, Golden.RepoRoot).Solutions[0].Path, s.Path);
    }

    [Fact]
    public void LoadFound_names_the_solution_then_the_flow_file_for_an_unreadable_flow_as_analyze_does()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Workflows/Flow-AA.json.data.xml", """
                <Workflow WorkflowId="{AA-11}" Name="My flow">
                  <JsonFileName>/Workflows/Flow-AA.json</JsonFileName>
                  <Category>5</Category><StateCode>0</StateCode>
                </Workflow>
                """)
            .Write("Workflows/Flow-AA.json", "[1]");

        var inventory = Assert.Throws<InvalidDataException>(() => AnalysisResult.LoadFound([t.Path], t.Path));
        var analyze = Assert.Throws<InvalidDataException>(() => AnalysisResult.RunFound([t.Path], t.Path));

        Assert.StartsWith($"{t.Path}: Workflows/Flow-AA.json: ", inventory.Message, StringComparison.Ordinal);
        Assert.Equal(analyze.Message, inventory.Message);
    }
}