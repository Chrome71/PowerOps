using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Plans;
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

    private static readonly TimeSpan UnpackTimeout = TimeSpan.FromSeconds(120);

    // What the gate keeps of its deadline after an unpack's timeout, so no pac outlives the gate.
    private static readonly TimeSpan UnpackMargin = TimeSpan.FromSeconds(10);

    private const string ReadingGuidance = "The call was blocked. If it is an import, write it as one plain pac solution import command with literal absolute paths and the environment's id or https URL. If it only mentions an import, reword it.";
    private const string ImportGuidance = "The import was blocked. To import a reviewed plan, ask the user for a yes to that environment, then call the MCP tool deploy_apply with apply: true, where the user types the environment's name in a form, or leave the import to the Action's approved job or to the user, who runs powerops deploy apply <plan file> --target <environment id or URL> --apply. Do not retry this import another way.";

    /// <param name="pac">Runs pac: the imports of <c>deploy apply</c>, and the unpack of a zip that a review names as its deploy-hash.</param>
    /// <param name="target">Reads the target: the <c>--target</c> value, the solution names of the folder and the dependency names not in it. Throws <see cref="DriftReadException"/>.</param>
    /// <param name="input">Opens the hook input of <c>deploy gate</c> (stdin); null when there is none. Only the gate calls it.</param>
    /// <param name="deadline">How long <c>deploy gate</c> may check before it blocks; a test seam, <see cref="GateDeadline"/> in production.</param>
    public static Command Create(
        TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> target,
        Func<Stream?>? input = null, TimeSpan? deadline = null, string? approvedHash = null, bool mcp = false,
        Func<string, Guid, IReadOnlyList<string>, IReadOnlyList<(string SchemaName, bool PublishedOn)>>? deployAgents = null)
    {
        var folder = new Argument<string>("folder") { Description = "A folder of exported solution zips, each with an optional <zip name>.settings.json beside it." };
        var targetOption = new Option<string?>("--target") { Description = "The environment to plan against (an id or URL, as pac --environment). Never printed." };
        var save = new Option<string?>("--save") { Description = "Also write the JSON report (the plan file) here, whatever --format is." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json", "markdown");
        var outFile = new Option<string?>("--out") { Description = "Write the report here instead of stdout." };
        var plannedBy = new Option<string?>("--planned-by") { Description = "Who made the plan, as you report it (not checked, not part of the hash): agent, person or action. Written to the plan file as plannedBy." };
        plannedBy.AcceptOnlyFromAmong("agent", "person", "action");
        var plan = new Command("plan", "Say what importing the solution zips in a folder into the target would do. Reads the target only; a zip that a review names as its deploy-hash is unpacked through pac into a temp folder, which is then deleted.")
        {
            folder, targetOption, save, format, outFile, plannedBy,
        };
        plan.SetAction(parse => SettingsCommand.Guard(error, () =>
            Run(parse.GetValue(folder)!, parse.GetValue(targetOption), parse.GetValue(save), parse.GetValue(format)!, parse.GetValue(outFile), parse.GetValue(plannedBy), output, error, target, pac)));
        return new Command("deploy", "Plan a deployment of solution zips, and import a reviewed plan.") { plan, Apply(output, error, pac, target, approvedHash, mcp, deployAgents), Gate(error, input ?? (() => null), deadline ?? GateDeadline, target, pac) };
    }

    private static Command Apply(
        TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> target, string? approvedHash, bool mcp,
        Func<string, Guid, IReadOnlyList<string>, IReadOnlyList<(string SchemaName, bool PublishedOn)>>? deployAgents)
    {
        var planFile = new Argument<string>("plan-file") { Description = "A plan file written by deploy plan --save. The zips and settings files are read from its folder." };
        var targetOption = new Option<string?>("--target") { Description = "The environment the plan was made for (an id or URL, as pac --environment). The plan file's target is the environment's display name, which this doesn't take. Never printed." };
        var apply = new Option<bool>("--apply") { Description = "Import the solutions. Without it the command is a dry run that writes nothing but a temp folder for a zip's pac solution unpack, which is then deleted." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json");
        var outFile = new Option<string?>("--out") { Description = "Write the report here instead of stdout." };
        var githubRun = new Option<string?>("--github-run") { Description = "For the GitHub Action: its run, attempt and environment (1 to 500 characters, no control or format character). Recorded as the report's executor action and, with --apply, as the approval's receipt; not checked." };
        var requireReviews = new Option<bool>("--require-reviews") { Description = "Refuse, before any import, unless for each zip the plan imports one plan folder under docs/plans holds a verify.md and a security-solution.md that are both ready, were written for the plan's current hash and record that zip's SHA-256 as their solution-hash, or as their deploy-hash with a solution-hash that equals the digest of the zip's pac solution unpack. Without it the same check only warns." };
        var command = new Command("apply", "Plan again against the target, refuse when an input of the plan changed since the review (a zip, a settings file, the target environment, its solution state or a dependency's version), then import the zips in plan order through pac (only with --apply).")
        {
            planFile, targetOption, apply, format, outFile, githubRun, requireReviews,
        };
        command.SetAction(parse => SettingsCommand.Guard(error, () =>
            Apply(parse.GetValue(planFile)!, parse.GetValue(targetOption), parse.GetValue(apply), parse.GetValue(format)!, parse.GetValue(outFile), parse.GetValue(githubRun), parse.GetValue(requireReviews), output, error, pac, target, approvedHash, mcp, deployAgents)));
        return command;
    }

    private static string Clean(string? text) => TextReport.Clean(text);

    // The nearest folder at or above `folder` that holds docs/plans, or null. The walk stops at the repository's top folder (the one holding .git),
    // so a docs/plans above the repository is not taken for its own.
    private static string? PlansRoot(string folder)
    {
        string? root = Path.GetFullPath(folder);
        while (root is not null && !Directory.Exists(Path.Combine(root, "docs", "plans")))
        {
            root = Path.Exists(Path.Combine(root, ".git")) ? null : Path.GetDirectoryName(root);
        }

        return root;
    }

    // Writes `warning: <zip>: <sentence>` for each imported zip, in plan order, that no plan folder's two reports cover (and with `evidence`, its `note:` lines,
    // and a `note:` for a zip whose covering folder links both reports through their deploy-hash).
    // Returns null when every one is covered, else the refusal message of --require-reviews. With no docs/plans nothing is written.
    // `left` is the time the gate has left, null outside it.
    private static string? WarnUnreviewed(
        string folder, IReadOnlyList<DeployZip> zips, IReadOnlyList<ImportStep> imports, TextWriter error, bool evidence, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<TimeSpan>? left)
    {
        const string NoPlans = "no docs/plans folder at or above the plan file's folder, so no review covers the zips; nothing was imported.";
        if (PlansRoot(folder) is not { } root)
        {
            return NoPlans;
        }

        IReadOnlyList<PlanReviews>? plans;
        try
        {
            plans = PlanChecker.Reviews(root);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine("warning: docs/plans could not be read, so the zips were not matched with review reports");
            return "docs/plans could not be read, so the zips were not matched with review reports; nothing was imported.";
        }

        // Gone between the walk and the read.
        if (plans is null)
        {
            return NoPlans;
        }

        // A zip is unpacked only when a report names it as its deploy-hash, so plans without such a report call no pac.
        var checks = imports.Select(i => zips.First(z => z.File == i.Zip))
            .Select(z => (Zip: z, Unpack: DeployReviews.NeedsUnpack(plans, z.Sha256) ? Unpack(Path.Combine(folder, z.File), z.Solution.Managed, pac, left) : null))
            .Select(c => (c.Zip, c.Unpack, Problem: DeployReviews.Problem(plans, c.Zip.Sha256, c.Unpack)))
            .ToList();
        var open = checks.Where(c => c.Problem is not null).Select(c => (c.Zip, c.Problem)).ToList();
        var notes = evidence && open.Count > 0 ? DeployReviews.Evidence(root, plans, [.. open.Select(o => o.Zip)]) : null;
        foreach (var (zip, unpack, problem) in checks)
        {
            if (problem is null)
            {
                if (evidence && DeployReviews.Cover(plans, zip.Sha256, unpack) is { Verify: { } verify, Security: { } security } cover
                    && verify.SolutionHash != zip.Sha256 && security.SolutionHash != zip.Sha256)
                {
                    error.WriteLine($"note: {Clean(zip.File)}: docs/plans/{Clean(cover.Plan)}: both reports record this zip's SHA-256 as their deploy-hash, and the digest of its pac solution unpack is their solution-hash");
                }

                continue;
            }

            error.WriteLine($"warning: {Clean(zip.File)}: {Clean(problem)}");
            foreach (var note in notes?.GetValueOrDefault(zip.File) ?? [])
            {
                error.WriteLine($"note: {Clean(zip.File)}: {Clean(note)}");
            }
        }

        return open.Count == 0 ? null
            : $"no review covers {Clean(open[0].Zip.File)}: {Clean(open[0].Problem)}{(open.Count > 1 ? $" ({open.Count} zips in all; the warning lines name each)" : "")}; nothing was imported.";
    }

    // The digest of a fresh pac solution unpack of the zip, or why there is none. pac's output is never printed: it holds full paths.
    private static ZipUnpack Unpack(string zip, bool managed, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<TimeSpan>? left)
    {
        var timeout = UnpackTimeout;
        if (left is not null)
        {
            var rest = left() - UnpackMargin;
            if (rest <= TimeSpan.Zero)
            {
                return new ZipUnpack(null, "no time was left to unpack it");
            }

            timeout = rest < timeout ? rest : timeout;
        }

        string? temp = null;
        try
        {
            temp = Directory.CreateTempSubdirectory("powerops-unpack-").FullName;
            var unpacked = Path.Combine(temp, "solution");
            // The zip's own type: pac refuses a managed zip unpacked as Unmanaged.
            var result = pac(["solution", "unpack", "--zipfile", Path.GetFullPath(zip), "--folder", unpacked, "--packagetype", managed ? "Managed" : "Unmanaged"], timeout);
            if (!result.Started)
            {
                return new ZipUnpack(null, "pac was not found");
            }

            if (result.TimedOut)
            {
                return new ZipUnpack(null, "pac solution unpack timed out");
            }

            if (result.ExitCode != 0)
            {
                return new ZipUnpack(null, $"pac solution unpack exited {result.ExitCode}");
            }

            // An unpack with no file would hash as empty input, which a report could claim.
            var manifest = Manifest.Read(unpacked);
            return manifest.Files.Count == 0 ? new ZipUnpack(null, Unreadable) : new ZipUnpack(manifest.Digest, null);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return new ZipUnpack(null, Unreadable);
        }
        finally
        {
            if (temp is not null)
            {
                try
                {
                    Directory.Delete(temp, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // shortcut: a temp folder whose delete fails (a file pac still holds open) is left in the system temp folder; retry or report it if that is ever seen.
                }
            }
        }
    }

    private const string Unreadable = "the unpacked folder could not be read";

    private sealed record Prepared(DeployPlanResult Plan, IReadOnlyList<DeployZip> Zips, DeployTarget Target);

    private static int Run(
        string folder, string? target, string? save, string format, string? outFile, string? plannedBy, TextWriter output, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac)
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

        // The report written last would replace the plan file.
        // shortcut: two spellings through a linked folder are not seen as one file; resolve both as SafeWrite does if that is ever needed.
        if (!string.IsNullOrEmpty(save) && !string.IsNullOrEmpty(outFile)
            && string.Equals(Path.GetFullPath(save), Path.GetFullPath(outFile), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            problems.Add("--save and --out name the same file");
        }

        if (Prepare(folder, target, problems, error, read, null, out var code) is not { } prepared)
        {
            return code;
        }

        var result = prepared.Plan;
        // Advisory here: changes neither the plan nor the exit code.
        WarnUnreviewed(folder, prepared.Zips, DeployApply.Imports(result, prepared.Zips), error, evidence: true, pac, left: null);

        // deploy apply reads the zips from the plan file's own folder (as at ReadyPlan); the line names no path, and the file is still written.
        // shortcut: two spellings through a linked folder are compared as text; resolve both as SafeWrite does if that is ever needed.
        if (!string.IsNullOrEmpty(save)
            && !string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(save)) ?? ""),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            error.WriteLine("warning: the plan file is not saved in the deploy folder; deploy apply reads the zips and settings files from the plan file's own folder");
        }

        var report = new StringWriter();
        switch (format)
        {
            case "json":
                DeployPlanReport.WriteJson(result, AnalyzeCommand.ToolVersion(), report, plannedBy);
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
            DeployPlanReport.WriteJson(result, AnalyzeCommand.ToolVersion(), json, plannedBy);
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
    // `problems` holds the usage problems found so far; error lines go to `error`. `left` is the time the gate has left, null outside it.
    private static (Ready? Ready, Stopped? Stop) DryRun(
        string planFile, string? target, List<string> problems, string tool, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, bool requireReviews, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac,
        string? approvedHash = null, bool hintPlanTarget = false, Func<TimeSpan>? left = null)
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

        if (Prepare(folder, target, [], error, read, hintPlanTarget ? reviewed.Target : null, out var code) is not { } now)
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

        IReadOnlyList<ImportStep> imports;
        IReadOnlyList<FlowRef> flowRefs;
        try
        {
            (imports, flowRefs) = (DeployApply.Imports(plan, now.Zips), DeployApply.FlowIds(plan, now.Zips));
        }
        catch (InvalidDataException ex)
        {
            error.WriteLine($"error: {Clean(ex.Message)}");
            return (null, new Stopped(InternalError));
        }

        // After the re-plan: only the fresh plan says which zips are imported, since the plan file's actions are not covered by its hash. With no import nothing is looked for.
        if (imports.Count > 0 && WarnUnreviewed(folder, now.Zips, imports, error, evidence: false, pac, left) is { } unreviewed && requireReviews)
        {
            return (null, new Stopped(1, plan.Target, reviewed.Hash, ApplyStatus.Refused, unreviewed, Rows(plan, _ => ImportStatus.NotRun)));
        }

        return (new Ready(folder, reviewed, now, imports, flowRefs), null);
    }

    private static int Apply(
        string planFile, string? target, bool apply, string format, string? outFile, string? githubRun, bool requireReviews, TextWriter output, TextWriter error,
        Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, string? approvedHash, bool mcp,
        Func<string, Guid, IReadOnlyList<string>, IReadOnlyList<(string SchemaName, bool PublishedOn)>>? deployAgents)
    {
        // The --target value, the environment id and every folder path are never printed.
        var problems = new List<string>();
        // The plan file and the zips and settings files beside it, which DryRun reads.
        // A plan file that does not exist is a usage problem below; its folder is not read (GetFullPath throws on an empty path).
        string[] inputs = File.Exists(planFile) ? [planFile, .. FolderInputs(Path.GetDirectoryName(Path.GetFullPath(planFile)) ?? "")] : [];
        PlanFileProblems(planFile, target, problems);
        // The value is not echoed: it lands in a report.
        if (githubRun is not null && (githubRun.Length is 0 or > 500 || githubRun.EnumerateRunes().Any(r => Rune.GetUnicodeCategory(r) is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.PrivateUse)))
        {
            problems.Add("--github-run must be 1 to 500 characters with no control or format character");
        }

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
            problems.Add("--out cannot be written: it is read-only, or no file can be created in its folder");
        }

        var tool = AnalyzeCommand.ToolVersion();
        var progress = format == "json" ? error : output;

        // As the caller reports it. `form` is true only for the in-process entry that passes the approved hash.
        var executor = githubRun is not null ? ApplyExecutor.Action : mcp ? ApplyExecutor.Mcp : ApplyExecutor.Cli;
        var approval = !apply ? null
            : approvedHash is not null ? new Approval(ApprovalMode.Form, null)
            : githubRun is not null ? new Approval(ApprovalMode.Action, githubRun)
            : new Approval(ApprovalMode.Cli, null);
        string? appliedAt = null;

        int Finish(int code, string name, string hash, string status, string? message, IReadOnlyList<AppliedSolution> solutions, IReadOnlyList<AppliedFlow> flows, IReadOnlyList<AppliedAgent>? agents = null)
        {
            var report = new StringWriter();
            var result = new DeployApplyResult(tool, apply, name, hash, status, message, appliedAt, executor, approval, solutions, flows, agents ?? []);
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

        var (ready, stop) = DryRun(planFile, target, problems, tool, error, read, requireReviews, pac, approvedHash, hintPlanTarget: true);
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

            var listed = DeployApply.ExpectedAgents(plan, now.Zips, s => s.Action != PlanAction.Skip).Select(a => new AppliedAgent(a.Solution, a.Name, null, null)).ToList();
            return Finish(0, plan.Target, reviewed.Hash, ApplyStatus.Planned, null, Rows(plan, s => s.Action == PlanAction.Skip ? ImportStatus.Skipped : ImportStatus.Planned), planned, listed);
        }

        appliedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
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
            return Finish(InternalError, plan.Target, reviewed.Hash, ApplyStatus.Failed, $"{failure.Value.Text} There is no rollback. {after}", Done(), [], ReadAgents());
        }

        if (flowRefs.Count == 0)
        {
            return Finish(0, plan.Target, reviewed.Hash, ApplyStatus.Imported, imports.Count == 0 ? "Nothing was imported: every solution was skipped." : null, Done(), [], ReadAgents());
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
            return Finish(InternalError, plan.Target, reviewed.Hash, ApplyStatus.Imported, $"The imports succeeded. Reading the target again failed{why}", Done(), planned, ReadAgents());
        }

        var flows = DeployApply.FlowsAfter(flowRefs, second.Solutions);
        return Finish(flows.AsExpected ? 0 : 1, plan.Target, reviewed.Hash, ApplyStatus.Imported, null, Done(), flows.Flows, ReadAgents());

        // The agents of every solution whose import went through, read once; a failed read never changes the status or the exit code.
        List<AppliedAgent> ReadAgents()
        {
            var expected = DeployApply.ExpectedAgents(plan, now.Zips, s => state.GetValueOrDefault(s.Order) == ImportStatus.Ok);
            if (expected.Count == 0)
            {
                return [];
            }

            try
            {
                return [.. DeployApply.AgentsAfter(expected, deployAgents!(target!, now.Target.Id, [.. expected.Select(e => e.Name)]))];
            }
            catch (Exception ex)
            {
                if (ex is DriftReadException drift)
                {
                    Forward(error, drift.Forwarded);
                }

                error.WriteLine($"warning: the agent read-back failed: {(ex is DriftReadException d ? Clean(d.Message) : "failed.")}");
                return [.. DeployApply.AgentsAfter(expected, null)];
            }
        }

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

    private static Command Gate(
        TextWriter error, Func<Stream?> input, TimeSpan deadline, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac)
    {
        var planFile = new Argument<string>("plan-file") { Description = "The plan file written by deploy plan --save that the hook trusts." };
        var command = new Command("gate", "A PreToolUse hook for Microsoft's import tools: reads the hook's JSON on stdin and exits 0 for a call that is not an import or is the plan's first import, 2 for any other call (the reason is on stderr).") { planFile };
        // Not SettingsCommand.Guard: the gate exits only 0 or 2, whatever happens, because a hook blocks only on exit 2.
        // Shadows the recursive help option: help would print to stdout and exit 0, which lets every call through.
        var help = new Option<bool>(GateHelpAliases[0], GateHelpAliases[1..]) { Hidden = true };
        command.Options.Add(help);
        command.SetAction(parse => RunGate(parse.GetValue(help) ? "-h" : parse.GetValue(planFile)!, error, input, deadline, read, pac));
        return command;
    }

    internal static readonly string[] GateHelpAliases = ["-h", "/h", "--help", "-?", "/?"];

    // The gate has no help: help would exit 0 and let the call through. Exits 2 even if stderr fails.
    internal static int GateNoHelp(TextWriter error)
    {
        try
        {
            error.WriteLine("error: deploy gate has no help: it is a hook, and help would exit 0 and let the call through; see docs/COMMANDS.md#deploy-gate");
        }
        catch (Exception)
        {
            // Nothing is left to report to: a stderr that fails still blocks.
        }

        return UsageError;
    }

    private static int RunGate(
        string planFile, TextWriter error, Func<Stream?> input, TimeSpan deadline, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read,
        Func<IReadOnlyList<string>, TimeSpan, PacResult> pac)
    {
        // The deadline's clock: an unpack in the check gets only the time left on it.
        var clock = Stopwatch.StartNew();
        try
        {
            if (planFile == GateHelpAliases[0])
            {
                return GateNoHelp(error);
            }

            if (planFile.StartsWith('-'))
            {
                // Without this a mistyped option would be taken as the plan file.
                GateBlocked(error, ["the plan file must not start with '-'"], false);
                return UsageError;
            }

            // The check runs on its own task and writes to its own buffer, so past the deadline its late output is dropped.
            var report = new StringWriter();
            var check = Task.Run(() => GateCheck(planFile, input, report, read, pac, () => deadline - clock.Elapsed));
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
        string planFile, Func<Stream?> input, TextWriter report, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read,
        Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<TimeSpan> left)
    {
        try
        {
            return GateDecide(planFile, input, report, read, pac, left);
        }
        catch (Exception ex)
        {
            return GateBlocked(report, [$"internal error ({ex.GetType().Name})"], false);
        }
    }

    // Every block says why on stderr and ends with one guidance line; Codex lets the call through on exit 2 with no stderr.
    // `notes` are warning lines between the reasons and the guidance.
    private static int GateBlocked(TextWriter report, IEnumerable<string> lines, bool unreadable, IEnumerable<string>? notes = null)
    {
        foreach (var line in lines)
        {
            report.WriteLine($"error: deploy gate: {line}");
        }

        foreach (var note in notes ?? [])
        {
            report.WriteLine($"warning: deploy gate: {note}");
        }

        report.WriteLine(unreadable ? ReadingGuidance : ImportGuidance);
        return UsageError;
    }

    private static int GateDecide(
        string planFile, Func<Stream?> input, TextWriter report, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read,
        Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<TimeSpan> left)
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
                return GateCheckCall(call, planFile, report, read, pac, left);
            default:
                throw new InvalidOperationException("unknown import reading");
        }
    }

    private static int GateCheckCall(
        ImportCall call, string planFile, TextWriter report, Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read,
        Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, Func<TimeSpan> left)
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
        var (ready, stop) = DryRun(planFile, call.Environment, problems, AnalyzeCommand.ToolVersion(), report, read, requireReviews: true, pac, left: left);
        if (stop is not null)
        {
            return GateBlocked(report, [$"the dry run did not pass{(stop.Message is null ? "" : ": " + Clean(stop.Message))}"], false);
        }

        var plan = ready!.Now.Plan;
        if (ready.Imports.Count == 0)
        {
            return GateBlocked(report, ["the plan imports nothing"], false);
        }

        // A block after the dry run passed names each agent of the fresh plan, so the person who reads it also sees what the import may do.
        var notes = plan.Items.Where(i => i.Kind == PlanKind.Agent).Select(i => $"{Clean(i.Solution)}: {Clean(DeployPlan.AgentMessage(DeployApply.ShownAgentName(i.Subject)))}").ToList();

        // The plan file's solutions are not covered by its hash, so the first step comes from the fresh plan.
        var first = ready.Imports[0];
        var firstInput = plan.Inputs.Zips.First(z => z.Zip == first.Zip);
        if (zipHash != firstInput.ZipSha256)
        {
            var other = plan.Inputs.Zips.FirstOrDefault(z => z.ZipSha256 == zipHash)?.Zip;
            return GateBlocked(report, [other is null
                ? $"{ImportCall.Printable(Path.GetFileName(call.Zip))} is not a zip of the plan"
                : plan.Solutions.Any(s => s.Zip == other && s.Action == PlanAction.Skip) ? $"the plan skips {Clean(other)}" : $"the plan imports {Clean(first.Zip)} first"], false, notes);
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
            return GateBlocked(report, lines, false, notes);
        }

        // The dry run can take minutes of live reads: the files the call names must still be the bytes that were checked.
        var swapped = new[] { (call.Zip, zipHash, BoundedRead.MaxZipBytes), (call.Settings, settingsHash, MaxSettingsBytes) }
            .Where(f => f.Item1 is not null && HashRegularFile(f.Item1, f.Item3) != f.Item2)
            .Select(f => $"{ImportCall.Printable(Path.GetFileName(f.Item1!))} changed while the gate checked it")
            .ToList();
        return swapped.Count == 0 ? 0 : GateBlocked(report, swapped, false, notes);
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
    // `planTarget` is the reviewed plan's target name, or null: a failed read of exactly that name gets a hint, since the plan's name is not an id or URL.
    private static Prepared? Prepare(
        string folder, string? target, List<string> problems, TextWriter error,
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, DeployTarget> read, string? planTarget, out int code)
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
            if (planTarget is not null && string.Equals(target, planTarget, StringComparison.OrdinalIgnoreCase))
            {
                error.WriteLine($"error: --target takes the environment's id or URL, as pac --environment does; '{Clean(planTarget)}' is the plan's display name for it");
            }

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

        // The plan file names each zip once ignoring case, as the file systems of Windows do, so deploy apply would refuse the plan.
        foreach (var clash in zips.Where(z => !bad.Contains(z.Name)).GroupBy(z => z.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            problems.Add($"{string.Join(" and ", clash.Select(z => Clean(z.Name)))}: zip file names may not differ only in case");
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