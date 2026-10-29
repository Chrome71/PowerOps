using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Output;

namespace PowerOps.Cli;

public static partial class McpCommand
{
    private const string PathsText = "Absolute paths of solution folders or exported zips; folders are searched recursively.";
    private const string SolutionText = "Absolute path of an unpacked solution folder or an exported zip.";

    // Limits on what a caller can send (UTF-16 units; entries per list). They are not in the tools' input schemas: an agent that goes over reads the limit in the error.
    private const int MaxTextLength = 4096;
    private const int MaxPaths = 50;
    private const int MaxEnvs = 10;

    private enum Kind { Path, Paths, Text, Texts, Flag }

    // Local: a text field whose entries can also be file paths (drift's envs).
    private sealed record Field(string Name, Kind Kind, bool Required, string Description, int MinItems = 0, bool Local = false)
    {
        public bool IsList => Kind is Kind.Paths or Kind.Texts;

        public bool IsPath => Kind is Kind.Path or Kind.Paths;

        public int MaxItems => Kind == Kind.Paths ? MaxPaths : MaxEnvs;
    }

    private sealed class Values(Dictionary<string, object> map)
    {
        public string Text(string name) => (string)map[name];

        public bool Flag(string name) => map.TryGetValue(name, out var value) && (bool)value;

        public string? Optional(string name) => map.TryGetValue(name, out var value) ? (string)value : null;

        public string[] List(string name) => (string[])map[name];

        public string[] OptionalList(string name) => map.TryGetValue(name, out var value) ? (string[])value : [];
    }

    // Extra holds the checks of one tool beyond the generic ones: the labels that already failed are passed in so a value is refused once.
    // RunAsync (given instead of Run) also gets the request's own server, to ask the user.
    private sealed record ToolDef(string Name, string Description, Field[] Fields, Func<Values, TextWriter, TextWriter, CancellationToken, int>? Run, ToolAnnotations? Annotations = null,
        Action<Values, ISet<string>, List<string>>? Extra = null, Func<Values, McpServer, CancellationToken, ValueTask<CallToolResult>>? RunAsync = null);

    private static readonly ToolAnnotations ReadOnly = new() { ReadOnlyHint = true, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = false };
    private static readonly ToolAnnotations ReadTenant = new() { ReadOnlyHint = true, DestructiveHint = false, IdempotentHint = true, OpenWorldHint = true };
    private static readonly ToolAnnotations WritePlan = new() { ReadOnlyHint = false, DestructiveHint = false, IdempotentHint = false, OpenWorldHint = true };
    private static readonly ToolAnnotations Import = new() { ReadOnlyHint = false, DestructiveHint = true, IdempotentHint = false, OpenWorldHint = true };

    [GeneratedRegex(@"^[A-Za-z0-9._-]+\.json\z")]
    private static partial Regex SaveName();

    // FileStream throws NotSupportedException for these on Windows; refused on every OS.
    private static readonly string[] DeviceNames = ["CON", "PRN", "AUX", "NUL", .. Enumerable.Range(0, 10).SelectMany(i => new[] { "COM" + i, "LPT" + i })];

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

    // Tests build a server from these options over in-process pipes; ran sees every argument list a tool passes to Program.Run.
    internal static McpServerOptions Options(Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot> driftLive,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> deployTarget, Action<string[]>? ran = null)
    {
        var tools = Tools(pac, driftLive, deployTarget, ran);
        var listed = tools.Select(Describe).ToList();
        return new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "powerops", Version = AnalyzeCommand.ToolVersion() },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = listed }),
                CallToolHandler = (context, cancellationToken) => Call(tools, context, cancellationToken),
            },
            Filters = new McpServerFilters { Message = { IncomingFilters = { RefuseUnpairedSurrogate } } },
        };
    }

    private const string SurrogateLine = "error: arguments: a string holds an unpaired surrogate";

    // The SDK cannot turn a request that holds a lone surrogate back into JSON, so it answers -32603 before any handler runs. This filter runs first and answers with a tool error instead.
    private static McpMessageHandler RefuseUnpairedSurrogate(McpMessageHandler next) => async (context, cancellationToken) =>
    {
        if (context.JsonRpcMessage is JsonRpcRequest { Method: "tools/call", Params: { } parameters } request)
        {
            try
            {
                _ = parameters.ToJsonString();
            }
            catch (InvalidOperationException)
            {
                var refusal = Refuse(SurrogateLine);

                // The 2026-07-28 schema requires resultType on results; the SDK stamps it on handler results only, so a result sent from here carries it itself.
                var revision = request.Context?.ProtocolVersion ?? context.Server.NegotiatedProtocolVersion;
                if (revision is not null && string.CompareOrdinal(revision, "2026-07-28") >= 0)
                {
                    refusal.ResultType = "complete";
                }

                await context.Server.SendMessageAsync(new JsonRpcResponse { Id = request.Id, Result = JsonSerializer.SerializeToNode(refusal, McpJsonUtilities.DefaultOptions) }, cancellationToken);
                return;
            }
        }

        await next(context, cancellationToken);
    };

    // Exit code 0 and 1 are reports; 2, 3 and anything else are errors, and so is a 1 that printed nothing
    // (System.CommandLine's own exception handler returns 1). Only powerops' own stderr lines are passed on: those that start
    // with "error:", then those that start with "note:" (a link that was not read) or "warning:", so an agent knows what was skipped.
    // Forwarded pac, az and Dataverse text can hold an environment URL or an account name; DeployCommand.Forward always writes it
    // as "  | <line>", which none of these prefixes match.
    internal static CallToolResult Result(int code, string stdout, string stderr)
    {
        var failed = code is not (0 or 1) || (code == 1 && stdout.Length == 0);
        var lines = stderr.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var errors = lines.Where(l => l.StartsWith("error:", StringComparison.Ordinal)).ToList();
        var notes = lines.Where(l => l.StartsWith("note:", StringComparison.Ordinal) || l.StartsWith("warning:", StringComparison.Ordinal)).ToList();
        if (failed && errors.Count == 0)
        {
            errors.Add($"error: the command failed (exit code {code})");
        }

        List<ContentBlock> content = [];
        if (stdout.Length > 0)
        {
            content.Add(new TextContentBlock { Text = stdout });
        }

        errors.AddRange(notes);
        if (errors.Count > 0)
        {
            content.Add(new TextContentBlock { Text = string.Join("\n", errors) });
        }

        return new CallToolResult { IsError = failed, Content = content };
    }

    private static ToolDef[] Tools(Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, string, Snapshot> driftLive,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> deployTarget, Action<string[]>? ran)
    {
        // Only deploy_apply passes a hash: the one the user approved in the form.
        int Approved(string[] args, TextWriter output, TextWriter error, string? approvedHash)
        {
            ran?.Invoke(args);
            return Program.Run(args, output, error, pac, driftLive, deployTarget, approvedHash: approvedHash);
        }

        int Cli(string[] args, TextWriter output, TextWriter error) => Approved(args, output, error, null);

        // One import at a time in this server; it covers the import only, never the form (see DeployApply).
        var guard = new SemaphoreSlim(1, 1);
        static string[] Option(string name, string? value) => value is null ? [] : [name, value];
        const string Json = "Read-only; returns the JSON of `powerops ";

        return
        [
            new("analyze", "Check solution folders or exported zips for common problems, offline. " + Json + "analyze --format json` (docs/schemas/analyze.schema.json).",
                [
                    new("paths", Kind.Paths, true, PathsText, 1),
                    new("baseline", Kind.Path, false, "Optional: the absolute path of a baseline file from the baseline tool; the findings it lists are left out."),
                ],
                (v, o, e, _) => Cli(["analyze", "--format", "json", .. Option("--baseline", v.Optional("baseline")), "--", .. v.List("paths")], o, e)),
            new("rules", "List every analysis rule with its id, severity, title, fix and docs link. " + Json + "rules --json` (docs/schemas/rules.schema.json).",
                [],
                (_, o, e, _) => Cli(["rules", "--json"], o, e)),
            new("baseline", "Record the current analyze findings of solution folders or zips as a baseline for analyze's `baseline` input. Read-only: returns the baseline file's text (a file format with its own `version`, no schema) and writes nothing.",
                [new("paths", Kind.Paths, true, PathsText, 1)],
                (v, o, e, _) => AnalyzeCommand.Run(v.List("paths"), null, null, e, (r, _) =>
                {
                    Baseline.Write(r, o);
                    return 0;
                })),
            new("diff", "Compare two versions of a solution (exported zips or unpacked folders) component by component, offline. " + Json + "diff --format json` (docs/schemas/diff.schema.json).",
                [
                    new("old", Kind.Path, true, "Absolute path of the older solution: an exported zip or an unpacked folder."),
                    new("new", Kind.Path, true, "Absolute path of the newer solution: an exported zip or an unpacked folder."),
                ],
                (v, o, e, _) => Cli(["diff", "--format", "json", "--", v.Text("old"), v.Text("new")], o, e)),
            new("test", "Run the offline flow tests (*.flowtest.json) against the cloud flows of the solutions under a path, with mocked connector actions. " + Json + "test --format json` (docs/schemas/test.schema.json).",
                [
                    new("path", Kind.Path, true, "Absolute path of the solution folder or zip to test against; folders are searched recursively."),
                    new("tests", Kind.Path, false, "Optional: the absolute path of the folder to search for *.flowtest.json files (default: path; needed when path is a zip)."),
                ],
                (v, o, e, _) => Cli(["test", "--format", "json", .. Option("--tests", v.Optional("tests")), "--", v.Text("path")], o, e)),
            new("settings_generate", "Make a pac deployment settings file with empty values for a solution's connection references and environment variables. Read-only: returns the file's text (pac's settings format, no schema) and writes nothing.",
                [new("solution", Kind.Path, true, SolutionText)],
                (v, o, e, _) => Cli(["settings", "generate", "--", v.Text("solution")], o, e)),
            new("settings_validate", "Check a pac deployment settings file against a solution: missing or extra entries, empty connection ids and values. " + Json + "settings validate --format json` (docs/schemas/settings-validate.schema.json).",
                [
                    new("solution", Kind.Path, true, SolutionText),
                    new("settings", Kind.Path, true, "Absolute path of the deployment settings file to check."),
                ],
                (v, o, e, _) => Cli(["settings", "validate", "--settings", v.Text("settings"), "--format", "json", "--", v.Text("solution")], o, e)),
            new("plan_status", "Check the planning documents (docs/plans/*/plan.md) under a path: header, sign-off hash, review and tenant data. " + Json + "plan status --json` (docs/schemas/plan-status.schema.json).",
                [new("path", Kind.Path, true, "Absolute path of a repo folder, a plan folder or a plan.md.")],
                (v, o, e, _) => Cli(["plan", "status", "--json", "--", v.Text("path")], o, e)),
            new("plan_facts", "Read the name of pac's current environment and its solutions (unique name, version, managed) through pac. " + Json + "plan facts` (docs/schemas/plan-facts.schema.json). It reads the environment pac is signed in to; use it only when that is an environment the user named in the session.",
                [],
                (_, o, e, _) => Cli(["plan", "facts"], o, e),
                ReadTenant),
            new("drift", "Compare one solution across environments, read through pac, az and the Dataverse Web API, or across saved snapshot files: version, components, layers, variables, references and flows. " + Json + "drift --format json` (docs/schemas/drift.schema.json). Use it only on an environment the user named in the session.",
                [
                    new("solution", Kind.Text, true, "The solution's unique name."),
                    new("envs", Kind.Texts, true, "At least two: an entry ending in .json is the absolute path of a snapshot file; any other is an environment id, URL or name (as pac --environment).", 2, true),
                ],
                (v, o, e, _) => Cli(["drift", "--solution", v.Text("solution"), .. v.List("envs").SelectMany(env => new[] { "--env", env }), "--format", "json"], o, e),
                ReadTenant,
                CheckEnvs),
            new("deploy_plan", "Say what importing the solution zips in a folder into a target environment would do, reading the target through pac, az and the Dataverse Web API; returns the JSON of `powerops deploy plan --format json` (docs/schemas/deploy-plan.schema.json). It writes only when `save` names a new file, which gets that JSON in the folder; promote a reviewed plan with deploy_apply, not with an import tool. Use it only on an environment the user named in the session.",
                [
                    new("folder", Kind.Path, true, "Absolute path of a folder of exported solution zips, each with an optional <zip name>.settings.json beside it."),
                    new("target", Kind.Text, true, "The environment to plan against: an id or URL (as pac --environment)."),
                    new("save", Kind.Text, false, "Optional: a new file name (letters, digits, '.', '_' and '-', ending in .json but not .settings.json) for the plan file in folder; an existing file is never replaced."),
                ],
                (v, o, e, ct) => DeployPlan(v, o, e, Cli, ct),
                WritePlan,
                CheckSave),
            new("deploy_apply", "Promote a reviewed plan file from deploy_plan's `save` with this tool, not with an import tool: it plans again against the target and returns the JSON of `powerops deploy apply --format json` (docs/schemas/deploy-apply.schema.json), as a dry run that imports nothing unless `apply` is true. With `apply: true` it imports the zips in plan order through pac only after the user types the target's name into the server's confirmation form, and refuses when anything changed since the plan. Use it only on an environment the user named in the session.",
                [
                    new("planFile", Kind.Path, true, "Absolute path of a plan file saved by deploy_plan's save; the zips and settings files are read from its folder."),
                    new("target", Kind.Text, true, "The environment the plan was made for: an id or URL (as pac --environment)."),
                    new("apply", Kind.Flag, false, "Optional, default false: true imports after the user types the target's name into the confirmation form; false is a dry run."),
                ],
                null,
                Import,
                RunAsync: (v, server, ct) => DeployApply(v, server, guard, Approved, ct)),
        ];
    }

    private const string GuardLine = "error: apply: an import is already running in this server; nothing was imported";
    private const string NoFormLine = "error: apply: this client cannot show the confirmation form; nothing was imported";
    private const string FormHint = "error: apply: to import without the form, the user runs: powerops deploy apply <plan file> --target <environment id or URL> --apply";

    private static CallToolResult Refuse(params string[] lines) => new() { IsError = true, Content = [new TextContentBlock { Text = string.Join("\n", lines) }] };

    // A command's result that the tool refuses anyway: isError, with the line after the command's own error lines.
    private static CallToolResult RefuseWith(string stdout, string stderr, string line) => Result(3, stdout, stderr + "\n" + line);

    private sealed record DryRun(string? Status, string? Target, string? Hash);

    // The dry run's report says planned for this plan's target and hash; a stdout that is not such a JSON object does not.
    private static bool Passed(int code, string stdout, ReviewedPlan plan)
    {
        try
        {
            var report = JsonSerializer.Deserialize<DryRun>(stdout, JsonSerializerOptions.Web);
            return code == 0 && report is { Status: ApplyStatus.Planned } && report.Target == plan.Target && report.Hash == plan.Hash;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // Without apply: a dry run. With it: the user types the plan's target name into the client's form, and only an ordinal accept with that exact name passes.
    private static async ValueTask<CallToolResult> DeployApply(Values v, McpServer server, SemaphoreSlim guard, Func<string[], TextWriter, TextWriter, string?, int> cli, CancellationToken cancellationToken)
    {
        var (planFile, target) = (v.Text("planFile"), v.Text("target"));
        string[] Args(bool apply) => ["deploy", "apply", "--target", target, "--format", "json", .. (apply ? new[] { "--apply" } : []), "--", planFile];
        (int Code, string Out, string Err) Exec(string[] args, string? approvedHash = null)
        {
            var (o, e) = (new StringWriter(), new StringWriter());
            return (cli(args, o, e, approvedHash), o.ToString(), e.ToString());
        }

        if (!v.Flag("apply"))
        {
            var (code, o, e) = Exec(Args(false));
            return Result(code, o, e);
        }

        if (guard.CurrentCount == 0)
        {
            return Refuse(GuardLine);
        }

        if (server.ClientCapabilities?.Elicitation?.Form is null)
        {
            return Refuse(NoFormLine, FormHint);
        }

        var tool = AnalyzeCommand.ToolVersion();
        PlanFileRead? Read() => File.Exists(planFile) ? DeployCommand.ReadPlanFile(planFile, tool, TextWriter.Null) : null;
        if (Read()?.Plan is not { Blocked: false } plan)
        {
            var (_, o, e) = Exec(Args(false));
            return RefuseWith(o, e, "error: apply: the plan file cannot be applied; nothing was imported");
        }

        if (plan.Target.Length > 200 || plan.Target.EnumerateRunes().Any(r => Rune.GetUnicodeCategory(r) is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.PrivateUse)
            || char.IsWhiteSpace(plan.Target[0]) || char.IsWhiteSpace(plan.Target[^1]))
        {
            return Refuse("error: planFile: the plan's target name cannot be shown in the form (too long, characters that can't be typed, or white space at either end); nothing was imported", FormHint);
        }

        // The report's target is the live friendly name of the environment the target input names, so the name typed below is that environment's.
        var (dryCode, dryOut, dryErr) = Exec(Args(false));
        if (!Passed(dryCode, dryOut, plan))
        {
            return RefuseWith(dryOut, dryErr, "error: apply: the dry run did not pass, so no form was sent; nothing was imported");
        }

        var zips = string.Join(", ", plan.Inputs.Zips.Select(z => z.Zip));
        var form = new ElicitRequestParams
        {
            Message = $"powerops deploy_apply wants to import a reviewed plan into an environment. There is no rollback.\n\nTarget environment: {TextReport.Clean(plan.Target)}\nPlan hash: {plan.Hash}\nSolution zips: {zips}\n\nTo import, type the target environment's name exactly as shown above. Decline or cancel to import nothing.",
            RequestedSchema = new()
            {
                // No default: the SDK fills a missing accepted field from it, so an empty accept would pass.
                Properties = { ["environment"] = new ElicitRequestParams.StringSchema { Title = "Environment name", Description = "Type the target environment's name exactly as shown above." } },
                Required = ["environment"],
            },
        };
        ElicitResult answer;
        try
        {
            answer = await server.ElicitAsync(form, cancellationToken);
        }
        catch (Exception)
        {
            // The client's error, a cancel, a retry with no answer or a closed connection: whatever the cause, nothing is imported.
            return Refuse("error: apply: the confirmation form failed; nothing was imported", FormHint);
        }

        if (!string.Equals(answer.Action, "accept", StringComparison.Ordinal))
        {
            return Refuse($"error: apply: the confirmation form was {(answer.Action == "decline" ? "declined" : "cancelled")}; nothing was imported", FormHint);
        }

        if (answer.Content is null || !answer.Content.TryGetValue("environment", out var typed) || typed.ValueKind != JsonValueKind.String || !string.Equals(typed.GetString(), plan.Target, StringComparison.Ordinal))
        {
            return Refuse("error: apply: the name typed in the form is not the plan's target; nothing was imported", FormHint);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Refuse("error: apply: the call was cancelled; nothing was imported");
        }

        // The file may have changed while the form was open.
        if (Read()?.Plan is not { } again || again.Hash != plan.Hash || !string.Equals(again.Target, plan.Target, StringComparison.Ordinal))
        {
            return Refuse("error: planFile: the plan file changed while the form was open; nothing was imported");
        }

        if (!guard.Wait(0))
        {
            return Refuse(GuardLine);
        }

        try
        {
            var (code, o, e) = Exec(Args(true), plan.Hash);
            return Result(code, o, e);
        }
        finally
        {
            guard.Release();
        }
    }

    // An entry ending in .json is a snapshot path and must be absolute; any other entry that names a file would be read by drift as a snapshot relative to the server's folder.
    private static void CheckEnvs(Values values, ISet<string> refused, List<string> problems)
    {
        foreach (var (entry, i) in values.OptionalList("envs").Select((s, i) => (s, i)).Where(t => !refused.Contains($"envs {t.i + 1}")))
        {
            if (entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                if (!Path.IsPathFullyQualified(entry))
                {
                    problems.Add($"error: envs {i + 1}: must be an absolute path");
                }
            }
            else if (File.Exists(entry))
            {
                problems.Add($"error: envs {i + 1}: an entry that names a file must end in .json");
            }
        }
    }

    private static void CheckSave(Values values, ISet<string> refused, List<string> problems)
    {
        if (values.Optional("save") is not { } save || refused.Contains("save"))
        {
            return;
        }

        if (!SaveName().IsMatch(save))
        {
            problems.Add("error: save: must be a file name of letters, digits, '.', '_' and '-' that ends in .json");
        }
        else if (save.EndsWith(".settings.json", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add("error: save: a name that ends in .settings.json is refused");
        }
        else if (DeviceNames.Contains(save[..save.IndexOf('.')], StringComparer.OrdinalIgnoreCase))
        {
            problems.Add("error: save: a Windows device name is refused");
        }
        else if (!refused.Contains("folder") && values.Optional("folder") is { } folder && (File.Exists(Path.Combine(folder, save)) || Directory.Exists(Path.Combine(folder, save))))
        {
            problems.Add("error: save: the file already exists in folder");
        }
    }

    // The plan runs without --save; the plan file is written here, as a new file only, so one that appeared since the check is not replaced.
    private static int DeployPlan(Values v, TextWriter output, TextWriter error, Func<string[], TextWriter, TextWriter, int> cli, CancellationToken cancellationToken)
    {
        var folder = v.Text("folder");
        string[] args = ["deploy", "plan", "--target", v.Text("target"), "--format", "json", "--", folder];
        if (v.Optional("save") is not { } save)
        {
            return cli(args, output, error);
        }

        var plan = new StringWriter();
        var code = cli(args, plan, error);
        var text = plan.ToString();
        output.Write(text);
        if (code is 0 or 1 && text.Length > 0 && cancellationToken.IsCancellationRequested)
        {
            // The client gave up (it may retry), so a file written now would make its retry fail.
            error.WriteLine("error: save: the call was cancelled; the plan file was not saved");
            return 3;
        }

        if (code is 0 or 1 && text.Length > 0 && !TrySave(Path.Combine(folder, save), text))
        {
            error.WriteLine("error: save: the plan file was not saved (it exists, or the folder is not writable)");
            return 3;
        }

        return code;
    }

    // Not SafeWrite: `save` must refuse a file or link that exists, which CreateNew does and SafeWrite (it replaces a file) does not.
    private static bool TrySave(string path, string text)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        try
        {
            using (stream)
            {
                stream.Write(new UTF8Encoding(false).GetBytes(text));
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // This call created the file, so it removes it (and only it).
            try
            {
                File.Delete(path);
            }
            catch (Exception deleteEx) when (deleteEx is IOException or UnauthorizedAccessException)
            {
                // Best effort: the caller reports the failed save either way.
            }

            return false;
        }
    }

    private static Tool Describe(ToolDef tool)
    {
        var properties = new JsonObject();
        foreach (var f in tool.Fields)
        {
            var property = new JsonObject { ["type"] = f.IsList ? "array" : f.Kind == Kind.Flag ? "boolean" : "string" };
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
        return new Tool { Name = tool.Name, Description = tool.Description, InputSchema = JsonSerializer.SerializeToElement(schema), Annotations = tool.Annotations ?? ReadOnly };
    }

    private static async ValueTask<CallToolResult> Call(ToolDef[] tools, RequestContext<CallToolRequestParams> context, CancellationToken cancellationToken)
    {
        var request = context.Params;
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

        if (tool.RunAsync is { } runAsync)
        {
            return await runAsync(values, context.Server, cancellationToken);
        }

        var output = new StringWriter();
        var error = new StringWriter();
        var code = tool.Run!(values, output, error, cancellationToken);
        return Result(code, output.ToString(), error.ToString());
    }

    // The plan's input checks 1 to 5, all problems at once; messages hold field names only, never a value.
    private static Values? Check(ToolDef tool, IDictionary<string, JsonElement>? arguments, List<string> problems)
    {
        arguments ??= new Dictionary<string, JsonElement>();
        foreach (var name in arguments.Keys.Where(k => tool.Fields.All(f => f.Name != k)))
        {
            // A key of any length is cut to 64 characters, not inside a surrogate pair, so the reply stays within the caps.
            var cut = name.Length <= 64 ? name.Length : char.IsHighSurrogate(name[63]) ? 63 : 64;
            problems.Add($"error: {TextReport.Clean(name[..cut])}: unknown field");
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
            else if (f.Kind == Kind.Flag)
            {
                if (given.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    values[f.Name] = given.GetBoolean();
                }
                else
                {
                    problems.Add($"error: {f.Name}: must be true or false");
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
            else if (given.GetArrayLength() > f.MaxItems)
            {
                problems.Add($"error: {f.Name}: give at most {f.MaxItems}");
            }
            else
            {
                values[f.Name] = given.EnumerateArray().Select(i => i.GetString()!).ToArray();
            }
        }

        var entries = tool.Fields.Where(f => f.Kind != Kind.Flag && values.ContainsKey(f.Name)).SelectMany(f => f.IsList
            ? ((string[])values[f.Name]).Select((s, i) => (Field: f, Label: $"{f.Name} {i + 1}", Value: s))
            : [(Field: f, Label: f.Name, Value: (string)values[f.Name])]).ToList();
        var refused = new HashSet<string>();
        foreach (var (field, label, value) in entries)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                problems.Add($"error: {label}: must not be empty");
            }
            else if (value.Any(char.IsControl))
            {
                problems.Add($"error: {label}: must not hold a control character");
            }
            else if (value.Length > MaxTextLength)
            {
                problems.Add($"error: {label}: longer than {MaxTextLength} characters");
            }
            else if (value[0] is '-' or '@')
            {
                problems.Add($"error: {label}: a value that starts with - or @ is refused");
            }
            else if ((field.IsPath || field.Local) && ImportCall.IsNetworkPath(value))
            {
                problems.Add($"error: {label}: a network or device path (starting with two \\ or / characters) is refused");
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
                refused.Add(label);
            }
        }

        var checkedValues = new Values(values);
        tool.Extra?.Invoke(checkedValues, refused, problems);
        return problems.Count > 0 ? null : checkedValues;
    }
}