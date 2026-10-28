using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Output;

namespace PowerOps.Cli;

public static class McpCommand
{
    private const string PathsText = "Absolute paths of solution folders or exported zips; folders are searched recursively.";
    private const string SolutionText = "Absolute path of an unpacked solution folder or an exported zip.";

    private enum Kind { Path, Paths, Text, Texts }

    private sealed record Field(string Name, Kind Kind, bool Required, string Description, int MinItems = 0)
    {
        public bool IsList => Kind is Kind.Paths or Kind.Texts;

        public bool IsPath => Kind is Kind.Path or Kind.Paths;
    }

    private sealed class Values(Dictionary<string, object> map)
    {
        public string Text(string name) => (string)map[name];

        public string? Optional(string name) => map.TryGetValue(name, out var value) ? (string)value : null;

        public string[] List(string name) => (string[])map[name];
    }

    private sealed record ToolDef(string Name, string Description, Field[] Fields, Func<Values, TextWriter, TextWriter, int> Run);

    private static readonly ToolAnnotations ReadOnly = new() { ReadOnlyHint = true, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = false };

    public static Command Create(TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot> driftLive,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> deployTarget)
    {
        var command = new Command("mcp", "Run an MCP server over stdio for agents: its tools run the commands and return their JSON.");
        command.SetAction(async (ParseResult parse, CancellationToken cancellationToken) =>
        {
            try
            {
                await using var server = McpServer.Create(new StdioServerTransport("powerops"), Options(pac, driftLive, deployTarget));
                await server.RunAsync(cancellationToken);
                return 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }
            catch (Exception ex)
            {
                error.WriteLine(Environment.GetEnvironmentVariable("POWEROPS_DEBUG") == "1" ? ex.ToString() : $"error: {TextReport.Clean(ex.Message)}");
                return 3;
            }
        });
        return command;
    }

    // Tests build a server from these options over in-process pipes.
    internal static McpServerOptions Options(Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot> driftLive,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> deployTarget)
    {
        var tools = Tools(pac, driftLive, deployTarget);
        var listed = tools.Select(Describe).ToList();
        return new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "powerops", Version = AnalyzeCommand.ToolVersion() },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = listed }),
                CallToolHandler = (context, _) => ValueTask.FromResult(Call(tools, context.Params)),
            },
        };
    }

    // Exit code 0 and 1 are reports; 2, 3 and anything else are errors, and so is a 1 that printed nothing
    // (System.CommandLine's own exception handler returns 1). Only powerops' own error lines are passed on:
    // forwarded pac, az and Dataverse text can hold an environment URL or an account name.
    internal static CallToolResult Result(int code, string stdout, string stderr)
    {
        var failed = code is not (0 or 1) || (code == 1 && stdout.Length == 0);
        var errors = stderr.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.StartsWith("error:", StringComparison.Ordinal)).ToList();
        if (failed && errors.Count == 0)
        {
            errors.Add($"error: the command failed (exit code {code})");
        }

        List<ContentBlock> content = [];
        if (stdout.Length > 0)
        {
            content.Add(new TextContentBlock { Text = stdout });
        }

        if (errors.Count > 0)
        {
            content.Add(new TextContentBlock { Text = string.Join("\n", errors) });
        }

        return new CallToolResult { IsError = failed, Content = content };
    }

    private static ToolDef[] Tools(Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot> driftLive,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> deployTarget)
    {
        int Cli(string[] args, TextWriter output, TextWriter error) => Program.Run(args, output, error, pac, driftLive, deployTarget);
        static string[] Option(string name, string? value) => value is null ? [] : [name, value];
        const string Json = "Read-only; returns the JSON of `powerops ";

        return
        [
            new("analyze", "Check solution folders or exported zips for common problems, offline. " + Json + "analyze --format json` (docs/schemas/analyze.schema.json).",
                [
                    new("paths", Kind.Paths, true, PathsText, 1),
                    new("baseline", Kind.Path, false, "Optional: the absolute path of a baseline file from the baseline tool; the findings it lists are left out."),
                ],
                (v, o, e) => Cli(["analyze", "--format", "json", .. Option("--baseline", v.Optional("baseline")), "--", .. v.List("paths")], o, e)),
            new("rules", "List every analysis rule with its id, severity, title, fix and docs link. " + Json + "rules --json` (docs/schemas/rules.schema.json).",
                [],
                (_, o, e) => Cli(["rules", "--json"], o, e)),
            new("baseline", "Record the current analyze findings of solution folders or zips as a baseline for analyze's `baseline` input. Read-only: returns the baseline file's text (a file format with its own `version`, no schema) and writes nothing.",
                [new("paths", Kind.Paths, true, PathsText, 1)],
                (v, o, e) => AnalyzeCommand.Run(v.List("paths"), null, e, r =>
                {
                    Baseline.Write(r, o);
                    return 0;
                })),
            new("diff", "Compare two versions of a solution (exported zips or unpacked folders) component by component, offline. " + Json + "diff --format json` (docs/schemas/diff.schema.json).",
                [
                    new("old", Kind.Path, true, "Absolute path of the older solution: an exported zip or an unpacked folder."),
                    new("new", Kind.Path, true, "Absolute path of the newer solution: an exported zip or an unpacked folder."),
                ],
                (v, o, e) => Cli(["diff", "--format", "json", "--", v.Text("old"), v.Text("new")], o, e)),
            new("test", "Run the offline flow tests (*.flowtest.json) against the cloud flows of the solutions under a path, with mocked connector actions. " + Json + "test --format json` (docs/schemas/test.schema.json).",
                [
                    new("path", Kind.Path, true, "Absolute path of the solution folder or zip to test against; folders are searched recursively."),
                    new("tests", Kind.Path, false, "Optional: the absolute path of the folder to search for *.flowtest.json files (default: path; needed when path is a zip)."),
                ],
                (v, o, e) => Cli(["test", "--format", "json", .. Option("--tests", v.Optional("tests")), "--", v.Text("path")], o, e)),
            new("settings_generate", "Make a pac deployment settings file with empty values for a solution's connection references and environment variables. Read-only: returns the file's text (pac's settings format, no schema) and writes nothing.",
                [new("solution", Kind.Path, true, SolutionText)],
                (v, o, e) => Cli(["settings", "generate", "--", v.Text("solution")], o, e)),
            new("settings_validate", "Check a pac deployment settings file against a solution: missing or extra entries, empty connection ids and values. " + Json + "settings validate --format json` (docs/schemas/settings-validate.schema.json).",
                [
                    new("solution", Kind.Path, true, SolutionText),
                    new("settings", Kind.Path, true, "Absolute path of the deployment settings file to check."),
                ],
                (v, o, e) => Cli(["settings", "validate", "--settings", v.Text("settings"), "--format", "json", "--", v.Text("solution")], o, e)),
            new("plan_status", "Check the planning documents (docs/plans/*/plan.md) under a path: header, sign-off hash, review and tenant data. " + Json + "plan status --json` (docs/schemas/plan-status.schema.json).",
                [new("path", Kind.Path, true, "Absolute path of a repo folder, a plan folder or a plan.md.")],
                (v, o, e) => Cli(["plan", "status", "--json", "--", v.Text("path")], o, e)),
        ];
    }

    private static Tool Describe(ToolDef tool)
    {
        var properties = new JsonObject();
        foreach (var f in tool.Fields)
        {
            var property = new JsonObject { ["type"] = f.IsList ? "array" : "string" };
            if (f.IsList)
            {
                property["items"] = new JsonObject { ["type"] = "string" };
                property["minItems"] = f.MinItems;
            }

            property["description"] = f.Description;
            properties[f.Name] = property;
        }

        var schema = new JsonObject { ["type"] = "object", ["properties"] = properties };
        var required = tool.Fields.Where(f => f.Required).Select(f => (JsonNode)f.Name).ToArray();
        if (required.Length > 0)
        {
            schema["required"] = new JsonArray(required);
        }

        schema["additionalProperties"] = false;
        return new Tool { Name = tool.Name, Description = tool.Description, InputSchema = JsonSerializer.SerializeToElement(schema), Annotations = ReadOnly };
    }

    private static CallToolResult Call(ToolDef[] tools, CallToolRequestParams? request)
    {
        if (request is null)
        {
            throw new McpProtocolException("Missing params.", McpErrorCode.InvalidParams);
        }

        var tool = tools.FirstOrDefault(t => t.Name == request.Name)
            ?? throw new McpProtocolException($"Unknown tool: '{request.Name}'", McpErrorCode.InvalidParams);
        var problems = new List<string>();
        var values = Check(tool, request.Arguments, problems);
        if (values is null)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = string.Join("\n", problems) }] };
        }

        var output = new StringWriter();
        var error = new StringWriter();
        var code = tool.Run(values, output, error);
        return Result(code, output.ToString(), error.ToString());
    }

    // The plan's input checks 1 to 4, all problems at once; messages hold field names only, never a value.
    private static Values? Check(ToolDef tool, IDictionary<string, JsonElement>? arguments, List<string> problems)
    {
        arguments ??= new Dictionary<string, JsonElement>();
        foreach (var name in arguments.Keys.Where(k => tool.Fields.All(f => f.Name != k)))
        {
            problems.Add($"error: {TextReport.Clean(name)}: unknown field");
        }

        var values = new Dictionary<string, object>();
        foreach (var f in tool.Fields)
        {
            if (!arguments.TryGetValue(f.Name, out var given) || given.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (f.Required)
                {
                    problems.Add($"error: {f.Name}: missing");
                }
            }
            else if (!f.IsList)
            {
                if (given.ValueKind == JsonValueKind.String)
                {
                    values[f.Name] = given.GetString()!;
                }
                else
                {
                    problems.Add($"error: {f.Name}: must be a string");
                }
            }
            else if (given.ValueKind != JsonValueKind.Array || given.EnumerateArray().Any(i => i.ValueKind != JsonValueKind.String))
            {
                problems.Add($"error: {f.Name}: must be a list of strings");
            }
            else if (given.GetArrayLength() < f.MinItems)
            {
                problems.Add($"error: {f.Name}: give at least {f.MinItems}");
            }
            else
            {
                values[f.Name] = given.EnumerateArray().Select(i => i.GetString()!).ToArray();
            }
        }

        var entries = tool.Fields.Where(f => values.ContainsKey(f.Name)).SelectMany(f => f.IsList
            ? ((string[])values[f.Name]).Select((s, i) => (Field: f, Label: $"{f.Name} {i + 1}", Value: s))
            : [(Field: f, Label: f.Name, Value: (string)values[f.Name])]).ToList();
        var refused = new HashSet<string>();
        foreach (var (_, label, value) in entries)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                problems.Add($"error: {label}: must not be empty");
            }
            else if (value[0] is '-' or '@')
            {
                problems.Add($"error: {label}: a value that starts with - or @ is refused");
            }
            else
            {
                continue;
            }

            refused.Add(label);
        }

        foreach (var (field, label, value) in entries)
        {
            if (field.IsPath && !refused.Contains(label) && !Path.IsPathFullyQualified(value))
            {
                problems.Add($"error: {label}: must be an absolute path");
            }
        }

        return problems.Count > 0 ? null : new Values(values);
    }
}