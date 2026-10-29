using System.CommandLine;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Cli;

public static class DeployCommand
{
    private const int UsageError = 2;
    private const int InternalError = 3;
    private const long MaxSettingsBytes = 1024 * 1024;
    private const string SettingsSuffix = ".settings.json";

    /// <summary>Half of both clients' default hook timeout of 600 seconds.</summary>
    internal static readonly TimeSpan GateDeadline = TimeSpan.FromSeconds(300);

    private const string ReadingGuidance = "The call was blocked. If it is an import, write it as one plain pac solution import command with literal absolute paths and the environment's id or https URL. If it only mentions an import, reword it.";
    private const string ImportGuidance = "The import was blocked. To import a reviewed plan, run powerops deploy apply <plan file> --target <environment id or URL> --apply after the user's yes to that environment. Do not retry this import another way.";

    /// <param name="pac">Runs pac; <c>deploy apply</c> uses it for the imports only.</param>
    /// <param name="target">Reads the target: the <c>--target</c> value, the solution names of the folder and the dependency names not in it. Throws <see cref="DriftReadException"/>.</param>
    /// <param name="input">Opens the hook input of <c>deploy gate</c> (stdin); null when there is none. Only the gate calls it.</param>
    /// <param name="deadline">How long <c>deploy gate</c> may check before it blocks; a test seam, <see cref="GateDeadline"/> in production.</param>
    public static Command Create(
        TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> target,
        Func<Stream?>? input = null, TimeSpan? deadline = null, string? approvedHash = null)
    {
        var folder = new Argument<string>("folder") { Description = "A folder of exported solution zips, each with an optional <zip name>.settings.json beside it." };
        var targetOption = new Option<string?>("--target") { Description = "The environment to plan against (an id or URL, as pac --environment). Never printed." };
        var save = new Option<string?>("--save") { Description = "Also write the JSON report (the plan file) here, whatever --format is." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json", "markdown");
        var outFile = new Option<string?>("--out") { Description = "Write the report here instead of stdout." };
        var plan = new Command("plan", "Say what importing the solution zips in a folder into the target would do. Reads the target only.")
        {
            folder, targetOption, save, format, outFile,
        };
        plan.SetAction(parse => SettingsCommand.Guard(error, () =>
            Run(parse.GetValue(folder)!, parse.GetValue(targetOption), parse.GetValue(save), parse.GetValue(format)!, parse.GetValue(outFile), output, error, target)));
        return new Command("deploy", "Plan a deployment of solution zips, and import a reviewed plan.") { plan, Apply(output, error, pac, target, approvedHash), Gate(error, input ?? (() => null), deadline ?? GateDeadline, target) };
    }

    private static Command Apply(
        TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> target, string? approvedHash)
    {
        var planFile = new Argument<string>("plan-file") { Description = "A plan file written by deploy plan --save. The zips and settings files are read from its folder." };
        var targetOption = new Option<string?>("--target") { Description = "The environment the plan was made for (an id or URL, as pac --environment). Never printed." };
        var apply = new Option<bool>("--apply") { Description = "Import the solutions. Without it the command is a dry run that writes nothing." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json");
        var outFile = new Option<string?>("--out") { Description = "Write the report here instead of stdout." };
        var command = new Command("apply", "Plan again against the target, refuse when anything changed since the reviewed plan, then import the zips in plan order through pac (only with --apply).")
        {
            planFile, targetOption, apply, format, outFile,
        };
        command.SetAction(parse => SettingsCommand.Guard(error, () =>
            Apply(parse.GetValue(planFile)!, parse.GetValue(targetOption), parse.GetValue(apply), parse.GetValue(format)!, parse.GetValue(outFile), output, error, pac, target, approvedHash)));
        return command;
    }

    private static string Clean(string? text) => TextReport.Clean(text);

    private sealed record Prepared(DeployPlanResult Plan, IReadOnlyList<DeployZip> Zips, DeployTarget Target);

    private static int Run(
        string folder, string? target, string? save, string format, string? outFile, TextWriter output, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        // The --target value and the folder's full path are never printed.
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(target))
        {
            problems.Add("--target is required");
        }

        // The zips and settings files of the folder, listed before the refusals and before the target read; a report written into the folder is not one of them.
        var inputs = FolderInputs(folder);
        foreach (var (name, value) in new[] { ("--save", save), ("--out", outFile) })
        {
            if (value is not null && Directory.Exists(value))
            {
                problems.Add($"{name} names an existing folder, not a file");
            }
            else if (value is not null && SafeWrite.Refusal(value, inputs) is { } refusal)
            {
                problems.Add($"{name} {refusal.Reason}");
            }
        }

        if (Prepare(folder, target, problems, error, read, out var code) is not { } prepared)
        {
            return code;
        }

        var result = prepared.Plan;

        var report = new StringWriter();
        switch (format)
        {
            case "json":
                DeployPlanReport.WriteJson(result, AnalyzeCommand.ToolVersion(), report);
                break;
            case "markdown":
                DeployPlanReport.WriteMarkdown(result, report);
                break;
            default:
                DeployPlanReport.WriteText(result, report);
                break;
        }

        if (save is not null)
        {
            var json = new StringWriter();
            DeployPlanReport.WriteJson(result, AnalyzeCommand.ToolVersion(), json);
            SafeWrite.WriteText(save, json.ToString(), inputs);
        }

        if (outFile is not null)
        {
            SafeWrite.WriteText(outFile, report.ToString(), inputs);
        }
        else
        {
            output.Write(report.ToString());
        }

        return result.Blocked ? 1 : 0;
    }

    // The plan file read of `deploy apply` and of the MCP tool deploy_apply, under the same rules; null after an error line.
    internal static PlanFileRead? ReadPlanFile(string planFile, string tool, TextWriter error)
    {
        try
        {
            if (new FileInfo(planFile).LinkTarget is not null)
            {
                error.WriteLine("error: the plan file is a symbolic link; symbolic links are not read");
                return null;
            }

            string text;
            try
            {
                text = BoundedRead.ReadAllText(planFile, MaxSettingsBytes);
            }
            catch (InvalidDataException)
            {
                error.WriteLine("error: the plan file is larger than 1 MB");
                return null;
            }

            return DeployPlanFile.Read(text, tool);
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine("error: the plan file could not be read");
            return null;
        }
    }

    private static void PlanFileProblems(string planFile, string? target, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            problems.Add("--target is required");
        }

        if (!File.Exists(planFile))
        {
            problems.Add("the plan file does not exist");
        }
    }

    private static List<AppliedSolution> Rows(DeployPlanResult plan, Func<PlannedSolution, string> import) =>
        [.. plan.Solutions.Select(s => new AppliedSolution(s.Order, s.Solution, s.Zip, s.Action, import(s), null))];

    /// <summary>What the dry run of <c>deploy apply</c> found when nothing stops the import: the plan against the target now, and the imports and flows it holds.</summary>
    private sealed record Ready(string Folder, ReviewedPlan Reviewed, Prepared Now, IReadOnlyList<ImportStep> Imports, IReadOnlyList<FlowRef> FlowRefs);

    /// <summary>How the dry run stopped: the exit code and, when <paramref name="Name"/> is not null, the report that <c>deploy apply</c> writes for it. With a null name only error lines were written.</summary>
    private sealed record Stopped(int Code, string? Name = null, string Hash = "", string Status = "", string? Message = null, IReadOnlyList<AppliedSolution>? Solutions = null);

    // The dry run of `deploy apply`, from the plan file read to the imports: one copy for `deploy apply` and `deploy gate`.
    // `problems` holds the usage problems found so far; error lines go to `error`.
    private static (Ready? Ready, Stopped? Stop) DryRun(
        string planFile, string? target, List<string> problems, string tool, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, string? approvedHash = null)
    {
        if (problems.Count > 0)
        {
            problems.ForEach(p => error.WriteLine($"error: {p}"));
            return (null, new Stopped(UsageError));
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(planFile))!;
        if (ReadPlanFile(planFile, tool, error) is not { } reviewedRead)
        {
            return (null, new Stopped(InternalError));
        }

        // A file of another version has no fields to trust, so its report has no target and no hash.
        if (reviewedRead.Plan is not { } reviewed)
        {
            return (null, new Stopped(1, "", "", ApplyStatus.Refused, reviewedRead.Refusal, []));
        }

        if (reviewed.Blocked)
        {
            return (null, new Stopped(1, reviewed.Target, reviewed.Hash, ApplyStatus.Refused, "a blocked plan is never applied", []));
        }

        if (Prepare(folder, target, [], error, read, out var code) is not { } now)
        {
            return (null, new Stopped(code));
        }

        var plan = now.Plan;
        if (!string.Equals(plan.Target, reviewed.Target, StringComparison.Ordinal))
        {
            error.WriteLine($"error: the plan was made for {Clean(reviewed.Target)}, --target is {Clean(plan.Target)}; nothing was imported");
            return (null, new Stopped(UsageError));
        }

        if (plan.Hash != reviewed.Hash)
        {
            var why = DeployApply.ChangedInput(reviewed.Inputs, plan.Inputs) ?? DeployApply.InputsEqualMessage;
            return (null, new Stopped(1, plan.Target, reviewed.Hash, ApplyStatus.Refused, $"{why}; nothing was imported. Plan again and review the new plan.", Rows(plan, _ => ImportStatus.NotRun)));
        }

        // The MCP tool passes the hash its form showed; a file and zips swapped as a consistent pair after the yes agree with each other, not with that hash.
        if (approvedHash is not null && plan.Hash != approvedHash)
        {
            return (null, new Stopped(1, plan.Target, reviewed.Hash, ApplyStatus.Refused, $"the plan is not the one that was approved (approved {approvedHash}, found {plan.Hash}); nothing was imported. Plan again and review the new plan.", Rows(plan, _ => ImportStatus.NotRun)));
        }

        if (plan.Blocked)
        {
            return (null, new Stopped(1, plan.Target, reviewed.Hash, ApplyStatus.Refused, "the plan is blocked now; nothing was imported. Plan again.", Rows(plan, _ => ImportStatus.NotRun)));
        }

        try
        {
            return (new Ready(folder, reviewed, now, DeployApply.Imports(plan, now.Zips), DeployApply.FlowIds(plan, now.Zips)), null);
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            return (null, new Stopped(InternalError));
        }
    }

    private static int Apply(
        string planFile, string? target, bool apply, string format, string? outFile, TextWriter output, TextWriter error,
        Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, string? approvedHash)
    {
        // The --target value, the environment id and every folder path are never printed.
        var problems = new List<string>();
        // The plan file and the zips and settings files beside it, which DryRun reads.
        string[] inputs = [planFile, .. FolderInputs(Path.GetDirectoryName(Path.GetFullPath(planFile)) ?? "")];
        PlanFileProblems(planFile, target, problems);
        if (outFile is not null && Directory.Exists(outFile))
        {
            problems.Add("--out names an existing folder, not a file");
        }
        else if (outFile is not null && SafeWrite.Refusal(outFile, inputs) is { } refusal)
        {
            // Before any import: the report is written after them, and a refusal then would lose the report of an import that ran.
            problems.Add($"--out {refusal.Reason}");
        }
        else if (outFile is not null && !Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(outFile))))
        {
            problems.Add("the folder of --out does not exist");
        }
        else if (outFile is not null && !SafeWrite.CanCreateBeside(outFile))
        {
            problems.Add("a file cannot be created in the folder of --out");
        }

        var tool = AnalyzeCommand.ToolVersion();
        var progress = format == "json" ? error : output;

        int Finish(int code, string name, string hash, string status, string? message, IReadOnlyList<AppliedSolution> solutions, IReadOnlyList<AppliedFlow> flows)
        {
            var report = new StringWriter();
            var result = new DeployApplyResult(tool, apply, name, hash, status, message, solutions, flows);
            if (format == "json")
            {
                DeployApplyReport.WriteJson(result, report);
            }
            else
            {
                DeployApplyReport.WriteText(result, report);
            }

            if (outFile is not null)
            {
                SafeWrite.WriteText(outFile, report.ToString(), inputs);
            }
            else
            {
                output.Write(report.ToString());
            }

            return code;
        }

        var (ready, stop) = DryRun(planFile, target, problems, tool, error, read, approvedHash);
        if (stop is not null)
        {
            return stop.Name is null ? stop.Code : Finish(stop.Code, stop.Name, stop.Hash, stop.Status, stop.Message, stop.Solutions!, []);
        }

        var (folder, reviewed, now, imports, flowRefs) = ready!;
        var plan = now.Plan;

        var planned = flowRefs.Select(f => new AppliedFlow(f.Solution, f.Flow.Name, f.Flow.ExpectedState, null)).ToList();
        if (!apply)
        {
            foreach (var step in imports)
            {
                progress.WriteLine($"would run: pac {string.Join(' ', SettingsCommand.ImportArguments(SettingsCommand.EnvironmentPlaceholder, step.Zip, step.Settings, step.StageAndUpgrade).Select(Clean))}");
            }

            return Finish(0, plan.Target, reviewed.Hash, ApplyStatus.Planned, null, Rows(plan, s => s.Action == PlanAction.Skip ? ImportStatus.Skipped : ImportStatus.Planned), planned);
        }

        progress.Write(SettingsApplyReport.Header(Clean(plan.Target)));
        var state = new Dictionary<int, string>();
        int? pacExit = null;
        (string Text, bool TimedOut)? failure = null;
        foreach (var step in imports)
        {
            failure = Import(step);
            state[step.Order] = failure is null ? ImportStatus.Ok : failure.Value.TimedOut ? ImportStatus.Unknown : ImportStatus.Failed;
            if (failure is not null)
            {
                break;
            }
        }

        List<AppliedSolution> Done() =>
            [.. plan.Solutions.Select(s => new AppliedSolution(
                s.Order, s.Solution, s.Zip, s.Action,
                s.Action == PlanAction.Skip ? ImportStatus.Skipped : state.GetValueOrDefault(s.Order, ImportStatus.NotRun),
                state.GetValueOrDefault(s.Order) == ImportStatus.Failed ? pacExit : null))];

        if (failure is not null)
        {
            var did = imports.Where(i => state.GetValueOrDefault(i.Order) == ImportStatus.Ok).Select(i => i.Solution).ToList();
            var after = failure.Value.TimedOut
                ? did.Count == 0 ? "No import before it finished." : $"Imported before it: {string.Join(", ", did)}."
                : did.Count == 0 ? "Nothing was imported." : $"Imported: {string.Join(", ", did)}.";
            return Finish(InternalError, plan.Target, reviewed.Hash, ApplyStatus.Failed, $"{failure.Value.Text} There is no rollback. {after}", Done(), []);
        }

        if (flowRefs.Count == 0)
        {
            return Finish(0, plan.Target, reviewed.Hash, ApplyStatus.Imported, null, Done(), []);
        }

        DeployTarget second;
        try
        {
            second = read(target!, [.. now.Zips.Select(z => z.Solution.UniqueName)], DeployPlan.TargetDependencies(now.Zips.Select(z => z.Solution)));
        }
        catch (Exception ex)
        {
            // Whatever the read throws, the imports stand and the report must still be written; only powerops' own read message is quoted.
            if (ex is DriftReadException drift)
            {
                Forward(error, drift.Forwarded);
            }

            var why = ex is DriftReadException d ? $": {d.Message}" : ".";
            return Finish(InternalError, plan.Target, reviewed.Hash, ApplyStatus.Imported, $"The imports succeeded. Reading the target again failed{why}", Done(), planned);
        }

        var flows = DeployApply.FlowsAfter(flowRefs, second.Solutions);
        return Finish(flows.AsExpected ? 0 : 1, plan.Target, reviewed.Hash, ApplyStatus.Imported, null, Done(), flows.Flows);

        // The failure message of one import (and whether it timed out), or null when it went through; hashes both files again first.
        (string Text, bool TimedOut)? Import(ImportStep step)
        {
            var input = plan.Inputs.Zips.First(z => z.Zip == step.Zip);
            var zip = Path.Combine(folder, step.Zip);
            var settings = step.Settings is null ? null : Path.Combine(folder, step.Settings);
            var changed = Changed(zip, input.ZipSha256, BoundedRead.MaxZipBytes) ? step.Zip : settings is not null && Changed(settings, input.SettingsSha256, MaxSettingsBytes) ? step.Settings : null;
            if (changed is not null)
            {
                return ($"{changed} changed after the re-plan read it, so it was not imported.", false);
            }

            progress.WriteLine($"importing {step.Order} {Clean(step.Solution)} ({Clean(step.Zip)})");
            progress.Flush();
            var result = pac(SettingsCommand.ImportArguments(now.Target.Id.ToString("D"), zip, settings, step.StageAndUpgrade), SettingsCommand.ImportTimeout);
            if (!result.Started)
            {
                return ("pac was not found on PATH.", false);
            }

            if (result.TimedOut)
            {
                return (SettingsCommand.ImportTimedOut, true);
            }

            if (result.ExitCode != 0)
            {
                Forward(error, result.Stdout);
                Forward(error, result.Stderr);
                pacExit = result.ExitCode;
                return ($"The import failed (pac exit code {result.ExitCode}); pac's output is on stderr.", false);
            }

            progress.WriteLine($"imported {Clean(step.Solution)}");
            return null;
        }
    }

    private static Command Gate(TextWriter error, Func<Stream?> input, TimeSpan deadline, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        var planFile = new Argument<string>("plan-file") { Description = "The plan file written by deploy plan --save that the hook trusts." };
        var command = new Command("gate", "A PreToolUse hook for Microsoft's import tools: reads the hook's JSON on stdin and exits 0 for a call that is not an import or is the plan's first import, 2 for any other call (the reason is on stderr).") { planFile };
        // Not SettingsCommand.Guard: the gate exits only 0 or 2, whatever happens, because a hook blocks only on exit 2.
        // Shadows the recursive help option: help would print to stdout and exit 0, which lets every call through.
        var help = new Option<bool>("-h", "/h", "--help", "-?", "/?") { Hidden = true };
        command.Options.Add(help);
        command.SetAction(parse => RunGate(parse.GetValue(help) ? "-h" : parse.GetValue(planFile)!, error, input, deadline, read));
        return command;
    }

    private static int RunGate(
        string planFile, TextWriter error, Func<Stream?> input, TimeSpan deadline, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        try
        {
            if (planFile.StartsWith('-'))
            {
                // Without this a mistyped option would be taken as the plan file.
                GateBlocked(error, ["the plan file must not start with '-'"], false);
                return UsageError;
            }

            // The check runs on its own task and writes to its own buffer, so past the deadline its late output is dropped.
            var report = new StringWriter();
            var check = Task.Run(() => GateCheck(planFile, input, report, read));
            if (!check.Wait(deadline))
            {
                error.WriteLine($"error: deploy gate: the check took longer than {(int)deadline.TotalSeconds} seconds");
                error.WriteLine(ImportGuidance);
                return UsageError;
            }

            error.Write(report.ToString());
            return check.Result;
        }
        catch (Exception ex)
        {
            try
            {
                GateBlocked(error, [$"internal error ({ex.GetType().Name})"], false);
            }
            catch (Exception)
            {
                // Nothing is left to report to: a stderr that fails still blocks.
            }

            return UsageError;
        }
    }

    private static int GateCheck(
        string planFile, Func<Stream?> input, TextWriter report, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        try
        {
            return GateDecide(planFile, input, report, read);
        }
        catch (Exception ex)
        {
            return GateBlocked(report, [$"internal error ({ex.GetType().Name})"], false);
        }
    }

    // Every block says why on stderr and ends with one guidance line; Codex lets the call through on exit 2 with no stderr.
    private static int GateBlocked(TextWriter report, IEnumerable<string> lines, bool unreadable)
    {
        foreach (var line in lines)
        {
            report.WriteLine($"error: deploy gate: {line}");
        }

        report.WriteLine(unreadable ? ReadingGuidance : ImportGuidance);
        return UsageError;
    }

    private static int GateDecide(
        string planFile, Func<Stream?> input, TextWriter report, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        using var stdin = input();
        if (stdin is null)
        {
            return GateBlocked(report, ["the hook input is read from stdin"], true);
        }

        var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int got;
        while (buffer.Length <= ImportCall.MaxInputBytes && (got = stdin.Read(chunk, 0, (int)Math.Min(chunk.Length, ImportCall.MaxInputBytes + 1 - buffer.Length))) > 0)
        {
            buffer.Write(chunk, 0, got);
        }

        switch (ImportCall.Read(buffer.ToArray()))
        {
            case ImportRead.NotImport:
                return 0;
            case ImportRead.Blocked blocked:
                return GateBlocked(report, blocked.Lines, blocked.Unreadable);
            case ImportRead.Check { Call: var call }:
                return GateCheckCall(call, planFile, report, read);
            default:
                throw new InvalidOperationException("unknown import reading");
        }
    }

    private static int GateCheckCall(ImportCall call, string planFile, TextWriter report, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read)
    {
        // The call's files are opened only to hash them.
        var zipHash = HashRegularFile(call.Zip, BoundedRead.MaxZipBytes);
        var settingsHash = call.Settings is null ? null : HashRegularFile(call.Settings, MaxSettingsBytes);
        var missing = new[] { (call.Zip, zipHash), (call.Settings!, settingsHash) }
            .Where(f => f.Item1 is not null && f.Item2 is null)
            .Select(f => $"{ImportCall.Printable(Path.GetFileName(f.Item1))} does not exist, is a symbolic link, is not a file, or can't be read")
            .ToList();
        if (missing.Count > 0)
        {
            return GateBlocked(report, missing, false);
        }

        var problems = new List<string>();
        PlanFileProblems(planFile, call.Environment, problems);
        var (ready, stop) = DryRun(planFile, call.Environment, problems, AnalyzeCommand.ToolVersion(), report, read);
        if (stop is not null)
        {
            return GateBlocked(report, [$"the dry run did not pass{(stop.Message is null ? "" : ": " + Clean(stop.Message))}"], false);
        }

        var plan = ready!.Now.Plan;
        if (ready.Imports.Count == 0)
        {
            return GateBlocked(report, ["the plan imports nothing"], false);
        }

        // The plan file's solutions are not covered by its hash, so the first step comes from the fresh plan.
        var first = ready.Imports[0];
        var firstInput = plan.Inputs.Zips.First(z => z.Zip == first.Zip);
        if (zipHash != firstInput.ZipSha256)
        {
            var other = plan.Inputs.Zips.FirstOrDefault(z => z.ZipSha256 == zipHash)?.Zip;
            return GateBlocked(report, [other is null
                ? $"{ImportCall.Printable(Path.GetFileName(call.Zip))} is not a zip of the plan"
                : plan.Solutions.Any(s => s.Zip == other && s.Action == PlanAction.Skip) ? $"the plan skips {Clean(other)}" : $"the plan imports {Clean(first.Zip)} first"], false);
        }

        var lines = new List<string>();
        if (first.Settings is not null && call.Settings is null)
        {
            lines.Add($"the plan imports {Clean(first.Zip)} with {Clean(first.Settings)}");
        }
        else if (first.Settings is not null && settingsHash != firstInput.SettingsSha256)
        {
            lines.Add($"the plan imports {Clean(first.Zip)} with a different settings file");
        }
        else if (first.Settings is null && call.Settings is not null)
        {
            lines.Add($"the plan imports {Clean(first.Zip)} with no settings file");
        }

        if (call.StageAndUpgrade != first.StageAndUpgrade)
        {
            lines.Add($"the plan {(first.StageAndUpgrade ? "stages and upgrades" : "doesn't stage and upgrade")} {Clean(first.Zip)}");
        }

        if (lines.Count > 0)
        {
            return GateBlocked(report, lines, false);
        }

        // The dry run can take minutes of live reads: the files the call names must still be the bytes that were checked.
        var swapped = new[] { (call.Zip, zipHash, BoundedRead.MaxZipBytes), (call.Settings, settingsHash, MaxSettingsBytes) }
            .Where(f => f.Item1 is not null && HashRegularFile(f.Item1, f.Item3) != f.Item2)
            .Select(f => $"{ImportCall.Printable(Path.GetFileName(f.Item1!))} changed while the gate checked it")
            .ToList();
        return swapped.Count == 0 ? 0 : GateBlocked(report, swapped, false);
    }

    // The SHA-256 of a regular file, or null when it does not exist, is a symbolic link, is not a seekable file (a FIFO), is over the cap or cannot be read.
    // The bytes read must equal the length it reports and be followed by the end of the file, so a device such as /dev/zero (seekable, length 0) is refused too.
    private static string? HashRegularFile(string path, long maxBytes)
    {
        try
        {
            if (new FileInfo(path).LinkTarget is not null)
            {
                return null;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!stream.CanSeek || stream.Length > maxBytes)
            {
                return null;
            }

            var bytes = new byte[stream.Length];
            var got = 0;
            int read;
            while (got < bytes.Length && (read = stream.Read(bytes, got, bytes.Length - got)) > 0)
            {
                got += read;
            }

            return got == bytes.Length && stream.ReadByte() < 0 ? DeployPlan.Sha256Hex(bytes) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // True when the file's SHA-256 is not `expected`, or it cannot be read, or it is now over the cap the first read had.
    private static bool Changed(string path, string? expected, long maxBytes) => HashRegularFile(path, maxBytes) is not { } hash || hash != expected;

    // Another tool's text goes to stderr with every line prefixed, so no line can start with "error:" or "::".
    internal static void Forward(TextWriter error, string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').ToList().ForEach(l => error.WriteLine("  | " + Clean(l)));
        }
    }

    // The folder's usage checks (their problems join `problems`), then the zips and settings files loaded, the target read and the plan built.
    // Returns null with the exit code (2 usage, 3 unreadable input or a failed read) when any of them fails; the caller prints nothing more.
    private static Prepared? Prepare(
        string folder, string? target, List<string> problems, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, out int code)
    {
        code = 0;
        var zips = new List<(string Name, string Path)>();
        if (!Directory.Exists(folder))
        {
            problems.Add("the folder argument is not an existing folder");
        }
        else
        {
            zips = ListZips(folder, problems);
        }

        if (problems.Count > 0)
        {
            problems.ForEach(p => error.WriteLine($"error: {p}"));
            code = UsageError;
            return null;
        }

        var loaded = new List<DeployZip>();
        foreach (var (name, path) in zips)
        {
            if (Load(name, path, folder, error) is not { } zip)
            {
                code = InternalError;
                return null;
            }

            loaded.Add(zip);
        }

        foreach (var g in loaded.GroupBy(z => z.Solution.UniqueName, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            error.WriteLine($"error: {string.Join(" and ", g.Select(z => Clean(z.File)))} hold the same solution ({Clean(g.Key)})");
            code = UsageError;
            return null;
        }

        DeployTarget got;
        try
        {
            // Build checks every zip (unique name, version, flow ids, settings file) before any read, against a target that holds none of them.
            DeployPlan.Build(loaded, new DeployTarget("", Guid.Empty, [.. loaded.Select(z => new Snapshot("", z.Solution.UniqueName, false, null, null, true, [], [], [], []))], new Dictionary<string, string?>()));
            got = read(target!, [.. loaded.Select(z => z.Solution.UniqueName)], DeployPlan.TargetDependencies(loaded.Select(z => z.Solution)));
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            code = InternalError;
            return null;
        }
        catch (DriftReadException ex)
        {
            error.WriteLine($"error: --target: {Clean(ex.Message)}");
            Forward(error, ex.Forwarded);
            code = ex.ExitCode;
            return null;
        }

        DeployPlanResult result;
        try
        {
            result = DeployPlan.Build(loaded, got);
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            code = InternalError;
            return null;
        }

        return new Prepared(result, loaded, got);
    }

    // The zips and settings files directly in the folder, as ListZips picks them; none when it is not a folder.
    private static string[] FolderInputs(string folder) => !Directory.Exists(folder)
        ? []
        : [.. Directory.EnumerateFiles(folder).Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || f.EndsWith(SettingsSuffix, StringComparison.OrdinalIgnoreCase))];

    // The folder's zips in ordinal order; problems are added for every file name, case or link that breaks the rules.
    private static List<(string Name, string Path)> ListZips(string folder, List<string> problems)
    {
        var files = Directory.EnumerateFiles(folder)
            .Select(f => (Name: System.IO.Path.GetFileName(f), Path: f))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();
        var zips = files.Where(f => f.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
        var settings = files.Where(f => f.Name.EndsWith(SettingsSuffix, StringComparison.OrdinalIgnoreCase)).ToList();
        if (zips.Count == 0)
        {
            problems.Add("the folder holds no zip file");
        }

        var bad = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in zips.Concat(settings))
        {
            if (!DeployPlanFile.IsSafeName(f.Name))
            {
                problems.Add($"{Clean(f.Name)}: a zip or settings file name may hold only letters, digits, '.', '_' and '-'");
                bad.Add(f.Name);
            }
            else if (new FileInfo(f.Path).LinkTarget is not null)
            {
                problems.Add($"{Clean(f.Name)}: symbolic links are not read");
                bad.Add(f.Name);
            }
        }

        var expected = zips.Where(z => !bad.Contains(z.Name)).Select(z => (Zip: z.Name, Settings: System.IO.Path.GetFileNameWithoutExtension(z.Name) + SettingsSuffix)).ToList();
        foreach (var s in settings.Where(s => !bad.Contains(s.Name) && !expected.Any(e => e.Settings == s.Name)))
        {
            var near = expected.FirstOrDefault(e => string.Equals(e.Settings, s.Name, StringComparison.OrdinalIgnoreCase));
            problems.Add(near.Zip is null
                ? $"{Clean(s.Name)}: no zip file has this settings file name"
                : $"{Clean(s.Name)}: differs only in case from {Clean(near.Settings)}, the settings file name of {Clean(near.Zip)}");
        }

        return [.. zips.Select(z => (z.Name, z.Path))];
    }

    // Returns null (after printing the reason) when the zip or its settings file cannot be read.
    private static DeployZip? Load(string name, string path, string folder, TextWriter error)
    {
        try
        {
            var bytes = BoundedRead.ReadAllBytes(path, BoundedRead.MaxZipBytes);
            var solution = SolutionLoader.Load(path);
            var settingsName = System.IO.Path.GetFileNameWithoutExtension(name) + SettingsSuffix;
            var settingsPath = System.IO.Path.Combine(folder, settingsName);
            if (!File.Exists(settingsPath))
            {
                return new DeployZip(name, DeployPlan.Sha256Hex(bytes), solution, null, null, null);
            }

            byte[] settings;
            try
            {
                settings = BoundedRead.ReadAllBytes(settingsPath, MaxSettingsBytes);
            }
            catch (InvalidDataException)
            {
                error.WriteLine($"error: {Clean(settingsName)}: the settings file is larger than 1 MB");
                return null;
            }

            using var reader = new StreamReader(new MemoryStream(settings));
            return new DeployZip(name, DeployPlan.Sha256Hex(bytes), solution, settingsName, DeployPlan.Sha256Hex(settings), reader.ReadToEnd());
        }
        catch (InvalidDataException ex)
        {
            // The loader's message starts with the path as given; only the file name is printed.
            error.WriteLine($"error: {Clean(ex.Message.Replace(path, name, StringComparison.Ordinal))}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"error: {Clean(name)}: the file could not be read");
        }

        return null;
    }
}