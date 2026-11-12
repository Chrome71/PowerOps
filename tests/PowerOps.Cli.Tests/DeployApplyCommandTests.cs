using System.Runtime.Versioning;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// One serial collection: the bare plan file name test changes the current folder.
[CollectionDefinition("deploy apply", DisableParallelization = true)]
public sealed class DeployApplyCollection;

[Collection("deploy apply")]
public class DeployApplyCommandTests : DeployTestBase
{
    private Outcome Apply(FakeTarget fake, FakePac pac, params string[] extra) => Run(fake, pac, ["deploy", "apply", PlanFile, "--target", TargetValue, .. extra]);

    private static JsonObject Json(Outcome r) => JsonNode.Parse(r.Out)!.AsObject();

    private static string[] Statuses(JsonObject doc) => [.. doc["solutions"]!.AsArray().Select(s => $"{s!["solution"]}:{s["import"]}")];

    private void EditPlanFile(Action<JsonObject> edit)
    {
        var doc = JsonNode.Parse(File.ReadAllText(PlanFile))!.AsObject();
        edit(doc);
        File.WriteAllText(PlanFile, doc.ToJsonString());
    }

    private static Snapshot Installed(string name, string version, bool managed = true) => new("Contoso Test", name, true, version, managed, true, [], [], [], []);

    // The seed's second read after its import: the seed zip's version (1.0.0.1) and every flow in the given state.
    private static Dictionary<string, Snapshot> SeedAfter(string state)
    {
        var snap = SnapshotFile.Load(TestSnapshot);
        return new(StringComparer.OrdinalIgnoreCase) { ["PowerOpsSeed"] = snap with { Version = "1.0.0.1", Flows = [.. snap.Flows.Select(f => f with { State = state })] } };
    }

    private string SeedImportPath(string name) => Path.Combine(Path.GetFullPath(Folder), name);

    // ---- exit 2 ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("no-target")]
    [InlineData("missing-plan-file")]
    [InlineData("plan-file-is-a-folder")]
    [InlineData("out-is-a-folder")]
    [InlineData("out-directory-missing")]
    [InlineData("no-plan-file-argument")]
    [InlineData("empty-plan-file")]
    public void A_usage_error_exits_2_before_anything_is_read_or_run(string problem)
    {
        SeedZip(Folder);
        MakePlan();
        string[] args = ["deploy", "apply", PlanFile, "--target", TargetValue, "--apply"];
        switch (problem)
        {
            case "no-target":
                args = ["deploy", "apply", PlanFile, "--apply"];
                break;
            case "missing-plan-file":
                args = ["deploy", "apply", Path.Combine(_root, "nope.json"), "--target", TargetValue];
                break;
            case "plan-file-is-a-folder":
                args = ["deploy", "apply", Out, "--target", TargetValue];
                break;
            case "empty-plan-file":
                args = ["deploy", "apply", "", "--target", TargetValue];
                break;
            case "out-is-a-folder":
                args = [.. args, "--out", Out];
                break;
            case "out-directory-missing":
                args = [.. args, "--out", Path.Combine(_root, "no-such-folder", "apply.txt")];
                break;
            default:
                args = ["deploy", "apply", "--target", TargetValue];
                break;
        }

        var (fake, pac) = (SeedTarget(), new FakePac());
        var r = Run(fake, pac, args);

        Assert.Equal(2, r.Code);
        Assert.StartsWith("error: ", r.Err);
        Assert.DoesNotContain("Exception", r.Err);
        Assert.Equal("", r.Out);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
        Assert.DoesNotContain(_root, r.Err);
    }

    // M28: the report is written after the imports, so a refused --out is found before the first one.
    [Theory]
    [InlineData("link")]
    [InlineData("plan-file")]
    public void An_out_that_is_a_link_or_the_plan_file_exits_2_before_any_import_and_leaves_both_files(string kind)
    {
        SeedZip(Folder);
        MakePlan();
        var victim = Path.Combine(_root, "victim.txt");
        File.WriteAllText(victim, "victim");
        var link = Path.Combine(Out, "apply.txt");
        string outFile;
        if (kind == "link")
        {
            Links.Create(link, victim);
            outFile = link;
        }
        else
        {
            outFile = PlanFile;
        }

        var files = Directory.GetFiles(Folder).ToDictionary(f => f, File.ReadAllBytes);
        var (fake, pac) = (SeedTarget(), new FakePac());

        var r = Apply(fake, pac, "--apply", "--out", outFile);

        Assert.Equal(2, r.Code);
        Assert.Equal(kind == "link" ? "error: --out is a symbolic link; symbolic links are not written.\n" : "error: --out is an input of this command; it is not overwritten.\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.All(files, f => Assert.Equal(f.Value, File.ReadAllBytes(f.Key)));
    }

    // M33 U-2: the zips and settings files beside the plan file are inputs too.
    [Theory]
    [InlineData("PowerOpsSeed.zip")]
    [InlineData("PowerOpsSeed.settings.json")]
    public void An_out_over_a_zip_or_settings_file_of_the_plan_folder_exits_2_before_any_import_and_leaves_the_file(string name)
    {
        SeedZip(Folder);
        MakePlan();
        var input = Path.Combine(Folder, name);
        var before = File.ReadAllBytes(input);
        var (fake, pac) = (SeedTarget(), new FakePac());

        var r = Apply(fake, pac, "--apply", "--out", input);

        Assert.Equal(2, r.Code);
        Assert.Equal("error: --out is an input of this command; it is not overwritten.\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
        Assert.Equal(before, File.ReadAllBytes(input));
    }

    [Fact]
    public void An_out_into_the_plan_folder_is_still_written()
    {
        SeedZip(Folder);
        MakePlan();
        var outFile = Path.Combine(Folder, "apply.txt");

        var r = Apply(SeedTarget(), new FakePac(), "--out", outFile);

        Assert.Equal(0, r.Code);
        Assert.True(File.Exists(outFile));
    }

    // The report file exists and is writable, but replacing it needs a temp file in its folder.
    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void An_out_in_a_folder_where_no_file_can_be_created_exits_2_before_any_import()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix modes.");
        SeedZip(Folder);
        MakePlan();
        var folder = Dir("readonly");
        var outFile = Path.Combine(folder, "apply.txt");
        File.WriteAllText(outFile, "old");
        File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Assert.SkipWhen(CanCreateIn(folder), "This user can create files there anyway.");
            var (fake, pac) = (SeedTarget(), new FakePac());

            var r = Apply(fake, pac, "--apply", "--out", outFile);

            Assert.Equal(2, r.Code);
            Assert.Equal("error: --out cannot be written: it is read-only, or no file can be created in its folder\n", r.Err);
            Assert.Equal("", r.Out);
            Assert.Equal(0, fake.Calls);
            Assert.Empty(pac.Calls);
            Assert.Equal("old", File.ReadAllText(outFile));
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    // The folder takes new files, but the report itself is read-only: WriteText would throw after the imports.
    [Fact]
    public void A_read_only_out_exits_2_before_any_import()
    {
        SeedZip(Folder);
        MakePlan();
        var outFile = Path.Combine(Dir("reports"), "apply.txt");
        File.WriteAllText(outFile, "old");
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(outFile, FileAttributes.ReadOnly);
        }
        else
        {
            File.SetUnixFileMode(outFile, UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }

        try
        {
            Assert.SkipWhen(CanWrite(outFile), "This user can write the file anyway.");
            var (fake, pac) = (SeedTarget(), new FakePac());

            var r = Apply(fake, pac, "--apply", "--out", outFile);

            Assert.Equal(2, r.Code);
            Assert.Equal("error: --out cannot be written: it is read-only, or no file can be created in its folder\n", r.Err);
            Assert.Equal("", r.Out);
            Assert.Equal(0, fake.Calls);
            Assert.Empty(pac.Calls);
            Assert.Equal("old", File.ReadAllText(outFile));
        }
        finally
        {
            File.SetAttributes(outFile, FileAttributes.Normal);
        }
    }

    private static bool CanWrite(string file)
    {
        try
        {
            File.OpenHandle(file, FileMode.Open, FileAccess.Write).Dispose();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    // A device is written in place, so no file needs to be created in /dev.
    [Fact]
    public void An_out_of_dev_null_passes_the_folder_check()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix devices.");
        SeedZip(Folder);
        MakePlan();
        var (fake, pac) = (SeedTarget(), new FakePac());

        var r = Apply(fake, pac, "--out", "/dev/null");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void An_empty_out_exits_2_before_any_import()
    {
        SeedZip(Folder);
        MakePlan();
        var (fake, pac) = (SeedTarget(), new FakePac());

        var r = Apply(fake, pac, "--apply", "--out", "");

        Assert.Equal(2, r.Code);
        Assert.Equal("error: --out is empty; name a file.\n", r.Err);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
    }

    private static bool CanCreateIn(string folder)
    {
        try
        {
            var probe = Path.Combine(folder, ".probe");
            new FileStream(probe, FileMode.CreateNew).Dispose();
            File.Delete(probe);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    [Fact]
    public void A_target_whose_friendly_name_differs_from_the_plans_exits_2_and_imports_nothing()
    {
        SeedZip(Folder);
        MakePlan();
        var (fake, pac) = (new FakeTarget { Name = "Contoso Prod", Snaps = { ["PowerOpsSeed"] = SnapshotFile.Load(TestSnapshot) with { Environment = "Contoso Prod" } } }, new FakePac());

        var r = Apply(fake, pac, "--apply");

        Assert.Equal(2, r.Code);
        Assert.Equal("error: the plan was made for Contoso Test, --target is Contoso Prod; nothing was imported\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(pac.Calls);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void A_failed_re_plan_exits_as_deploy_plan_does_and_imports_nothing(int exitCode)
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac();

        var r = Apply(new FakeTarget { Fail = new DriftReadException("the read failed", "tool line", exitCode) }, pac, "--apply");

        Assert.Equal(exitCode, r.Code);
        Assert.Equal("error: --target: the read failed\n  | tool line\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(pac.Calls);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void A_failed_re_plan_on_the_plans_display_name_adds_a_hint_and_keeps_the_exit_code(int exitCode)
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac();
        var fake = new FakeTarget { Fail = new DriftReadException("the read failed", "tool line", exitCode) };

        var r = Run(fake, pac, ["deploy", "apply", PlanFile, "--target", "contoso TEST", "--apply"]);

        Assert.Equal(exitCode, r.Code);
        Assert.Equal(
            "error: --target: the read failed\n  | tool line\nerror: --target takes the environment's id or URL, as pac --environment does; 'Contoso Test' is the plan's display name for it\n",
            r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void A_folder_without_a_zip_is_a_usage_error_from_the_re_plan()
    {
        SeedZip(Folder);
        MakePlan();
        File.Delete(Path.Combine(Folder, "PowerOpsSeed.zip"));
        File.Delete(Path.Combine(Folder, "PowerOpsSeed.settings.json"));

        var fake = SeedTarget();
        var r = Apply(fake, new FakePac(), "--apply");

        Assert.Equal(2, r.Code);
        Assert.Contains("the folder holds no zip file", r.Err);
        Assert.Equal(0, fake.Calls);
    }

    // ---- exit 3: the plan file ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("bad-hash")]
    [InlineData("over-1-mb")]
    [InlineData("symbolic-link")]
    public void An_unreadable_plan_file_exits_3_naming_the_problem_before_the_target_is_read(string problem)
    {
        SeedZip(Folder);
        MakePlan();
        switch (problem)
        {
            case "bad-hash":
                EditPlanFile(d => d["hash"] = "SECRET-VALUE");
                break;
            case "over-1-mb":
                File.WriteAllText(PlanFile, "{}" + new string(' ', 1024 * 1024));
                break;
            default:
                var real = Path.Combine(Stage, "plan.json");
                File.Move(PlanFile, real);
                try
                {
                    File.CreateSymbolicLink(PlanFile, real);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Assert.Skip("Symbolic links are not available here.");
                }

                break;
        }

        var (fake, pac) = (SeedTarget(), new FakePac());
        var r = Apply(fake, pac, "--apply");

        Assert.Equal(3, r.Code);
        Assert.StartsWith("error: ", r.Err);
        Assert.Equal(1, r.Err.Count(c => c == '\n'));
        Assert.Equal("", r.Out);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
        Assert.DoesNotContain("SECRET-VALUE", r.Err);
        Assert.DoesNotContain(_root, r.Err);
    }

    // ---- exit 1: refusals --------------------------------------------------------------------------------------

    [Fact]
    public void A_blocked_plan_file_is_refused_before_the_target_is_read()
    {
        SeedZip(Folder, settings: false);
        MakePlan();
        Assert.True((bool)JsonNode.Parse(File.ReadAllText(PlanFile))!["blocked"]!);

        var (fake, pac) = (SeedTarget(), new FakePac());
        var r = Apply(fake, pac, "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("refused", (string?)doc["status"]);
        Assert.Equal("a blocked plan is never applied", (string?)doc["message"]);
        Assert.Empty(doc["solutions"]!.AsArray());
        Assert.Empty(doc["flows"]!.AsArray());
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void A_plan_made_by_another_tool_is_refused_before_the_target_is_read()
    {
        SeedZip(Folder);
        MakePlan();
        EditPlanFile(d => d["tool"] = "9.9.9\n::error::x");

        var (fake, pac) = (SeedTarget(), new FakePac());
        var r = Apply(fake, pac, "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("refused", (string?)doc["status"]);
        Assert.StartsWith("the plan was made by powerops ", (string?)doc["message"]);
        Assert.EndsWith($"; this is {AnalyzeCommand.ToolVersion()}; plan again with this version", (string?)doc["message"]);
        Assert.Empty(doc["solutions"]!.AsArray());
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
    }

    [Theory]
    [InlineData("zip-changed", "the zip Solo.zip changed since the plan")]
    [InlineData("zip-added", "the zip Added.zip is not in the reviewed plan")]
    [InlineData("zip-removed", "the zip Extra.zip of the reviewed plan is gone")]
    [InlineData("settings-added", "the settings file of Solo.zip was added since the plan")]
    [InlineData("settings-removed", "the settings file of Extra.zip was removed since the plan")]
    [InlineData("settings-changed", "the settings file of Extra.zip changed since the plan")]
    [InlineData("target-changed", "the target's state of the solution in Solo.zip changed since the plan")]
    [InlineData("dependency-changed", "the installed version of the dependency lib changed since the plan")]
    [InlineData("hash-edited", DeployApply.InputsEqualMessage)]
    public void A_changed_input_is_refused_naming_it_and_nothing_is_imported(string change, string expected)
    {
        var (planFake, applyFake) = (new FakeTarget(), new FakeTarget());
        SmallZip(Folder, "Solo", requires: ["Lib (1.0)"]);
        SmallZip(Folder, "Extra");
        File.WriteAllText(Path.Combine(Folder, "Extra.settings.json"), "{}");
        planFake.Deps["Lib"] = "1.0.0.0";
        applyFake.Deps["Lib"] = "1.0.0.0";
        MakePlan(planFake);
        switch (change)
        {
            case "zip-changed":
                File.Delete(Path.Combine(Folder, "Solo.zip"));
                SmallZip(Folder, "Solo", requires: ["Lib (1.0)"], files: ("Other/Marker.txt", "changed"));
                break;
            case "zip-added":
                SmallZip(Folder, "Added");
                break;
            case "zip-removed":
                File.Delete(Path.Combine(Folder, "Extra.zip"));
                File.Delete(Path.Combine(Folder, "Extra.settings.json"));
                break;
            case "settings-added":
                File.WriteAllText(Path.Combine(Folder, "Solo.settings.json"), "{}");
                break;
            case "settings-removed":
                File.Delete(Path.Combine(Folder, "Extra.settings.json"));
                break;
            case "settings-changed":
                File.WriteAllText(Path.Combine(Folder, "Extra.settings.json"), "{ }");
                break;
            case "target-changed":
                applyFake.Snaps["Solo"] = Installed("Solo", "0.9.0.0");
                break;
            case "dependency-changed":
                applyFake.Deps["Lib"] = "2.0.0.0";
                break;
            default:
                EditPlanFile(d => d["hash"] = new string('a', 64));
                break;
        }

        var pac = new FakePac();
        var r = Apply(applyFake, pac, "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("refused", (string?)doc["status"]);
        Assert.Equal(expected + "; nothing was imported. Plan again and review the new plan.", (string?)doc["message"]);
        Assert.All(doc["solutions"]!.AsArray(), s => Assert.Equal("not-run", (string?)s!["import"]));
        Assert.Empty(doc["flows"]!.AsArray());
        Assert.Empty(pac.Calls);
    }

    // The in-process entry point takes the hash the user approved; no argument list can.
    private Outcome ApplyApproved(FakeTarget fake, FakePac pac, string approvedHash, bool mcp = false)
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        var code = Program.Run(["deploy", "apply", PlanFile, "--target", TargetValue, "--format", "json", "--apply"], o, e, pac.Run, null, fake.Read, null, approvedHash, mcp, new FakeAgents().Read);
        return new Outcome(code, o.ToString().ReplaceLineEndings("\n"), e.ToString().ReplaceLineEndings("\n"));
    }

    [Fact]
    public void A_plan_whose_hash_is_not_the_approved_one_is_refused_naming_both_hashes_and_nothing_is_imported()
    {
        SeedZip(Folder);
        MakePlan();
        var planHash = (string)JsonNode.Parse(File.ReadAllText(PlanFile))!["hash"]!;
        var approved = new string('a', 64);

        var pac = new FakePac();
        var r = ApplyApproved(SeedTarget(), pac, approved);

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("refused", (string?)doc["status"]);
        Assert.Equal($"the plan is not the one that was approved (approved {approved}, found {planHash}); nothing was imported. Plan again and review the new plan.", (string?)doc["message"]);
        Assert.Equal(planHash, (string?)doc["hash"]);
        Assert.Equal(["PowerOpsSeed:not-run"], Statuses(doc));
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void A_plan_with_the_approved_hash_imports_as_before()
    {
        SeedZip(Folder);
        MakePlan();
        var planHash = (string)JsonNode.Parse(File.ReadAllText(PlanFile))!["hash"]!;

        var pac = new FakePac();
        var r = ApplyApproved(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, pac, planHash);

        Assert.Equal(0, r.Code);
        Assert.Single(pac.Calls);
    }

    // ---- executor, approval and time (M52) ---------------------------------------------------------------------

    [Fact]
    public void A_dry_run_is_cli_with_no_approval_and_no_time()
    {
        SeedZip(Folder);
        MakePlan();

        var json = Json(Apply(SeedTarget(), new FakePac(), "--format", "json"));
        var text = Apply(SeedTarget(), new FakePac()).Out;

        Assert.Equal("cli", (string?)json["executor"]);
        Assert.Null(json["approval"]);
        Assert.Null(json["appliedAt"]);
        Assert.Equal(["status", "message", "appliedAt", "executor", "approval", "solutions"], json.Select(p => p.Key).Skip(5).Take(6));
        Assert.Contains("Plan hash: ", text);
        Assert.Contains("\nExecutor: cli\n", text);
        Assert.DoesNotContain("Approval:", text);
        Assert.DoesNotContain("Applied at:", text);
    }

    [Fact]
    public void An_apply_is_cli_cli_with_the_utc_time_it_started_importing()
    {
        SeedZip(Folder);
        MakePlan();
        static DateTime Second(DateTime t) => new(t.Ticks - (t.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        var before = Second(DateTime.UtcNow);

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), "--apply", "--format", "json");

        var after = DateTime.UtcNow;
        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal("cli", (string?)doc["executor"]);
        Assert.Equal("cli", (string?)doc["approval"]!["mode"]);
        Assert.Null(doc["approval"]!["receipt"]);
        var stamp = (string)doc["appliedAt"]!;
        Assert.Matches("^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$", stamp);
        var at = DateTime.ParseExact(stamp, "yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        Assert.InRange(at, before, after);
        var text = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), "--apply").Out;
        Assert.Matches(@"\nExecutor: cli\nApproval: cli\nApplied at: [0-9T:Z-]{20}\n", text);
    }

    [Fact]
    public void A_failed_import_has_a_time_too()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, false, 1, "boom") : null };

        var doc = Json(Apply(new FakeTarget(), pac, "--apply", "--format", "json"));

        Assert.Equal("failed", (string?)doc["status"]);
        Assert.NotNull(doc["appliedAt"]);
        Assert.Equal("cli", (string?)doc["approval"]!["mode"]);
    }

    [Fact]
    public void Github_run_makes_the_executor_action_and_with_apply_the_approval_its_receipt()
    {
        SeedZip(Folder);
        MakePlan();

        var dry = Json(Apply(SeedTarget(), new FakePac(), "--github-run", "run 7 attempt 2", "--format", "json"));
        var applied = Json(Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), "--github-run", "run 7 attempt 2, environment test", "--apply", "--format", "json"));
        var text = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), "--github-run", "run 7 attempt 2, environment test", "--apply").Out;

        Assert.Equal("action", (string?)dry["executor"]);
        Assert.Null(dry["approval"]);
        Assert.Equal("action", (string?)applied["executor"]);
        Assert.Equal("action", (string?)applied["approval"]!["mode"]);
        Assert.Equal("run 7 attempt 2, environment test", (string?)applied["approval"]!["receipt"]);
        Assert.Contains("\nExecutor: action\nApproval: action, receipt run 7 attempt 2, environment test\nApplied at: ", text);
    }

    [Fact]
    public void The_approved_hash_entry_gives_the_form_approval_and_mcp_when_the_caller_says_so()
    {
        SeedZip(Folder);
        MakePlan();
        var planHash = (string)JsonNode.Parse(File.ReadAllText(PlanFile))!["hash"]!;

        var viaCli = Json(ApplyApproved(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), planHash));
        var viaMcp = Json(ApplyApproved(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), planHash, mcp: true));

        Assert.Equal("cli", (string?)viaCli["executor"]);
        Assert.Equal("form", (string?)viaCli["approval"]!["mode"]);
        Assert.Equal("mcp", (string?)viaMcp["executor"]);
        Assert.Equal("form", (string?)viaMcp["approval"]!["mode"]);
        Assert.Null(viaMcp["approval"]!["receipt"]);
        Assert.NotNull(viaMcp["appliedAt"]);
    }

    [Fact]
    public void The_approved_hash_wins_the_approval_over_github_run_and_github_run_still_makes_the_executor_action()
    {
        SeedZip(Folder);
        MakePlan();
        var planHash = (string)JsonNode.Parse(File.ReadAllText(PlanFile))!["hash"]!;
        var (o, e) = (new StringWriter(), new StringWriter());
        var pac = new FakePac();

        var code = Program.Run(["deploy", "apply", PlanFile, "--target", TargetValue, "--format", "json", "--apply", "--github-run", "contoso/widgets run 7 attempt 1"], o, e, pac.Run, null, new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }.Read, null, planHash, deployAgents: new FakeAgents().Read);

        Assert.Equal(0, code);
        var doc = JsonNode.Parse(o.ToString())!;
        Assert.Equal("action", (string?)doc["executor"]);
        Assert.Equal("form", (string?)doc["approval"]!["mode"]);
        Assert.Null(doc["approval"]!["receipt"]);
    }

    [Fact]
    public void A_refusal_with_apply_keeps_its_approval_and_has_no_time()
    {
        SeedZip(Folder);
        MakePlan();
        var settings = Path.Combine(Folder, "PowerOpsSeed.settings.json");
        File.WriteAllBytes(settings, [.. File.ReadAllBytes(settings), (byte)'\n']);

        var pac = new FakePac();
        var doc = Json(Apply(SeedTarget(), pac, "--apply", "--github-run", "run 7 attempt 1", "--format", "json"));

        Assert.Equal("refused", (string?)doc["status"]);
        Assert.Equal("action", (string?)doc["executor"]);
        Assert.Equal("run 7 attempt 1", (string?)doc["approval"]!["receipt"]);
        Assert.Null(doc["appliedAt"]);
        Assert.Empty(pac.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("501")]
    [InlineData("format")]
    [InlineData("separator")]
    [InlineData("private-use")]
    [InlineData("line-break")]
    [InlineData("control")]
    public void A_bad_github_run_exits_2_before_anything_is_read_or_run(string kind)
    {
        SeedZip(Folder);
        MakePlan();
        var value = kind switch
        {
            "501" => new string('a', 501),
            "format" => "run\u200b7",
            "separator" => "run\u20287",
            "private-use" => "run\ue0007",
            "line-break" => "run 7\n::error::x",
            "control" => "run\u0007 7",
            _ => "",
        };

        var (fake, pac) = (SeedTarget(), new FakePac());
        var r = Apply(fake, pac, "--apply", "--github-run", value);

        Assert.Equal(2, r.Code);
        Assert.StartsWith("error: --github-run", r.Err);
        Assert.Equal("", r.Out);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
        Assert.All(r.Err.Split('\n'), l => Assert.DoesNotMatch("^::", l));
    }

    [Fact]
    public void A_github_run_of_500_characters_is_accepted()
    {
        SeedZip(Folder);
        MakePlan();

        var r = Apply(SeedTarget(), new FakePac(), "--github-run", new string('a', 500), "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal("action", (string?)Json(r)["executor"]);
    }

    [Fact]
    public void A_plan_file_whose_plannedBy_was_edited_still_applies()
    {
        SeedZip(Folder);
        MakePlan();
        EditPlanFile(d => d["plannedBy"] = "agent");

        var pac = new FakePac();
        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, pac, "--apply");

        Assert.Equal(0, r.Code);
        Assert.Single(pac.Calls);
    }

    [Fact]
    public void A_blocked_re_plan_with_equal_inputs_is_refused_and_nothing_is_imported()
    {
        SeedZip(Folder, settings: false);
        MakePlan();
        EditPlanFile(d => d["blocked"] = false);

        var pac = new FakePac();
        var r = Apply(SeedTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("refused", (string?)doc["status"]);
        Assert.Equal("the plan is blocked now; nothing was imported. Plan again.", (string?)doc["message"]);
        Assert.Equal(["PowerOpsSeed:not-run"], Statuses(doc));
        Assert.Empty(doc["flows"]!.AsArray());
        Assert.Empty(pac.Calls);
    }

    // ---- the dry run -------------------------------------------------------------------------------------------

    [Fact]
    public void The_dry_run_imports_nothing_exits_0_and_shows_the_arguments_with_file_names_only()
    {
        SeedZip(Folder);
        MakePlan();
        var (fake, pac) = (SeedTarget(), new FakePac());

        var r = Apply(fake, pac);

        Assert.Equal(0, r.Code);
        Assert.Empty(pac.Calls);
        Assert.Equal(1, fake.Calls);
        Assert.Equal("", r.Err);
        Assert.StartsWith("would run: pac solution import --environment <environment id> --path PowerOpsSeed.zip --settings-file PowerOpsSeed.settings.json --stage-and-upgrade\n", r.Out);
        Assert.EndsWith("1 PowerOpsSeed (PowerOpsSeed.zip): upgrade, planned\nflow PowerOpsSeed/Stamp work item status: expected on, now not read\ndry run: would import 1 solution\n", r.Out);
    }

    [Fact]
    public void The_json_dry_run_has_planned_solutions_and_flows_without_a_state_and_writes_to_out()
    {
        SeedZip(Folder);
        MakePlan();
        var file = Path.Combine(Out, "apply.json");

        var r = Apply(SeedTarget(), new FakePac(), "--format", "json", "--out", file);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Out);
        var doc = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        Assert.False((bool)doc["applied"]!);
        Assert.Equal("planned", (string?)doc["status"]);
        Assert.Equal(["PowerOpsSeed:planned"], Statuses(doc));
        Assert.Null(doc["flows"]![0]!["stateAfter"]);
        Assert.Contains("would run: pac solution import --environment <environment id> --path PowerOpsSeed.zip", r.Err);
    }

    // ---- the imports -------------------------------------------------------------------------------------------

    [Fact]
    public void A_managed_upgrade_with_a_settings_file_stages_and_upgrades_and_the_imports_get_absolute_paths_the_environment_id_and_a_60_minute_timeout()
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac();

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, pac, "--apply");

        Assert.Equal(0, r.Code);
        var call = Assert.Single(pac.Calls);
        Assert.Equal(
            ["solution", "import", "--environment", TargetIdText, "--path", SeedImportPath("PowerOpsSeed.zip"), "--settings-file", SeedImportPath("PowerOpsSeed.settings.json"), "--stage-and-upgrade"],
            call);
        Assert.Equal([TimeSpan.FromMinutes(60)], pac.Timeouts);
    }

    [Fact]
    public void The_imports_follow_the_plan_order_a_skip_imports_nothing_and_only_a_managed_upgrade_stages()
    {
        Chain();
        SmallZip(Folder, "Kept", "1.0.0.0");
        SmallZip(Folder, "Plain", "2.0.0.0", managed: false);
        SmallZip(Folder, "-Dash", unique: "Dash");
        File.WriteAllText(Path.Combine(Folder, "B.settings.json"), "{}");
        var target = new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0"), ["Plain"] = Installed("Plain", "1.0.0.0", managed: false) } };
        MakePlan(target);
        var plan = JsonNode.Parse(File.ReadAllText(PlanFile))!;
        var expected = plan["solutions"]!.AsArray().Where(s => (string?)s!["action"] != "skip").Select(s => (string)s!["zip"]!).ToList();
        Assert.Equal(["A.zip", "B.zip"], expected.Where(z => z is "A.zip" or "B.zip" or "C.zip").Take(2));
        var pac = new FakePac();

        var r = Apply(new FakeTarget { Snaps = target.Snaps, After = AfterOf(Installed("A", "1.0.0.0"), Installed("B", "1.0.0.0"), Installed("C", "1.0.0.0"), Installed("Plain", "2.0.0.0", managed: false), Installed("Dash", "1.0.0.0")) }, pac, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal(
            [.. expected.Select(z => SeedImportPath(z))],
            pac.Calls.Select(c => c[5]));
        Assert.All(pac.Calls, c =>
        {
            Assert.True(Path.IsPathRooted(c[5]));
            Assert.Equal(TargetIdText, c[3]);
        });
        Assert.Equal(
            ["solution", "import", "--environment", TargetIdText, "--path", SeedImportPath("B.zip"), "--settings-file", SeedImportPath("B.settings.json")],
            pac.Calls.Single(c => c[5].EndsWith("B.zip", StringComparison.Ordinal)));
        Assert.Equal(["solution", "import", "--environment", TargetIdText, "--path", SeedImportPath("Plain.zip")], pac.Calls.Single(c => c[5].EndsWith("Plain.zip", StringComparison.Ordinal)));
        Assert.Equal(["solution", "import", "--environment", TargetIdText, "--path", SeedImportPath("-Dash.zip")], pac.Calls.Single(c => c[5].EndsWith("-Dash.zip", StringComparison.Ordinal)));
        Assert.DoesNotContain(pac.Calls, c => c[5].EndsWith("Kept.zip", StringComparison.Ordinal));
        Assert.DoesNotContain(pac.Calls, c => c.Contains("--stage-and-upgrade"));
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Equal("skipped", (string?)doc["solutions"]!.AsArray().Single(s => (string?)s!["solution"] == "Kept")!["import"]);
        Assert.Equal(expected.Count, doc["solutions"]!.AsArray().Count(s => (string?)s!["import"] == "ok"));
    }

    [Fact]
    public void A_plan_of_only_skips_imports_nothing_reads_nothing_again_and_exits_0()
    {
        SmallZip(Folder, "Kept");
        var target = new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") } };
        MakePlan(target);
        var pac = new FakePac();

        var r = Apply(target, pac, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Equal("Nothing was imported: every solution was skipped.", (string?)doc["message"]);
        Assert.Equal(["Kept:skipped"], Statuses(doc));
        Assert.Empty(doc["flows"]!.AsArray());
        Assert.Empty(pac.Calls);
        Assert.Equal(2, target.Calls);
        Assert.Null(doc["solutions"]![0]!["after"]);

        var text = Apply(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") } }, new FakePac(), "--apply");

        Assert.Equal(0, text.Code);
        Assert.Contains("applied: Nothing was imported: every solution was skipped.", text.Out);
    }

    [Fact]
    public void An_apply_that_imports_one_solution_with_no_flow_has_no_message()
    {
        SmallZip(Folder, "Solo");
        var target = new FakeTarget();
        MakePlan(target);
        var pac = new FakePac();

        var r = Apply(new FakeTarget { After = AfterOf(Installed("Solo", "1.0.0.0")) }, pac, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Null((string?)doc["message"]);
        Assert.Equal(["Solo:ok"], Statuses(doc));
        Assert.Single(pac.Calls);
    }

    [Fact]
    public void A_saved_plan_with_a_same_version_item_still_applies_as_a_skip_in_a_dry_run()
    {
        SmallZip(Folder, "Kept");
        var target = new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") with { Variables = [new("demo_Gone", null)] } } };
        MakePlan(target);
        Assert.Contains("same-version-differs", File.ReadAllText(PlanFile));
        var pac = new FakePac();

        var r = Apply(target, pac, "--format", "json");

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal("planned", (string?)doc["status"]);
        Assert.Equal(["Kept:skipped"], Statuses(doc));
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void An_apply_prints_the_target_line_and_one_line_before_and_after_each_import_then_the_report()
    {
        Chain();
        MakePlan(new FakeTarget());

        var r = Apply(new FakeTarget { After = ChainAfter() }, new FakePac(), "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.StartsWith("environment: Contoso Test\nimporting 1 A (A.zip)\nimported A\nimporting 2 B (B.zip)\nimported B\nimporting 3 C (C.zip)\nimported C\nTarget: Contoso Test\n", r.Out);
        Assert.EndsWith("3 C (C.zip): install, ok\n  after: 1.0.0.0 (managed)\napplied\n", r.Out);
    }

    // ---- failures ----------------------------------------------------------------------------------------------

    [Fact]
    public void The_first_failed_import_stops_the_run_later_solutions_are_not_run_and_pacs_output_goes_to_stderr_with_every_line_prefixed()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, false, 5, "out line\nerror: boom\n", "err line\n::error::x\r\n") : null };

        var r = Apply(new FakeTarget(), pac, "--apply");

        Assert.Equal(3, r.Code);
        Assert.Equal(2, pac.Calls.Count);
        Assert.Equal("  | out line\n  | error: boom\n  | err line\n  | ::error::x\n", r.Err);
        Assert.DoesNotContain("boom", r.Out);
        Assert.Contains("2 B (B.zip): install, failed\n3 C (C.zip): install, not-run\n", r.Out);
        Assert.EndsWith("failed: The import failed (pac exit code 5); pac's output is on stderr. There is no rollback. Imported: A.\n", r.Out);
    }

    [Fact]
    public void A_failed_import_in_json_has_the_pac_exit_code_and_no_pac_text()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 0 ? new PacResult(true, false, 7, "secret pac text") : null };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal("failed", (string?)doc["status"]);
        Assert.Equal(["A:failed", "B:not-run", "C:not-run"], Statuses(doc));
        Assert.Equal(7, (int?)doc["solutions"]![0]!["pacExitCode"]);
        Assert.Null(doc["solutions"]![1]!["pacExitCode"]);
        Assert.Contains("Nothing was imported.", (string?)doc["message"]);
        Assert.DoesNotContain("secret pac text", r.Out);
        Assert.Contains("  | secret pac text\n", r.Err);
    }

    private const string TimeoutText = "The import timed out after 60 minutes. It may still be running in the environment: check the solution's import history there before running again.";

    [Fact]
    public void A_timed_out_import_exits_3_with_the_timeout_text_and_forwards_nothing()
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac { Answer = _ => new PacResult(true, true, 0, "") };

        var r = Apply(SeedTarget(), pac, "--apply");

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Err);
        Assert.Contains($"failed: {TimeoutText} There is no rollback. No import before it finished.\n", r.Out);
        Assert.Contains("PowerOpsSeed (PowerOpsSeed.zip): upgrade, unknown\n", r.Out);
    }

    [Fact]
    public void A_timeout_on_the_first_zip_leaves_its_row_unknown_the_rest_not_run_and_says_no_import_finished()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 0 ? new PacResult(true, true, 0, "") : null };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Assert.Single(pac.Calls);
        var doc = Json(r);
        Assert.Equal("failed", (string?)doc["status"]);
        Assert.Equal(["A:unknown", "B:not-run", "C:not-run"], Statuses(doc));
        Assert.Equal($"{TimeoutText} There is no rollback. No import before it finished.", (string?)doc["message"]);
        Assert.DoesNotContain("Nothing was imported", (string?)doc["message"]);
        Assert.Null(doc["solutions"]![0]!["pacExitCode"]);
    }

    [Fact]
    public void A_timeout_on_the_second_zip_names_the_imports_before_it()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, true, 0, "") : null };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal(["A:ok", "B:unknown", "C:not-run"], Statuses(doc));
        Assert.Equal($"{TimeoutText} There is no rollback. Imported before it: A.", (string?)doc["message"]);

        var text = Apply(new FakeTarget(), new FakePac { Answer = n => n == 1 ? new PacResult(true, true, 0, "") : null }, "--apply");
        Assert.Contains("1 A (A.zip): install, ok\n2 B (B.zip): install, unknown\n3 C (C.zip): install, not-run\n", text.Out);
    }

    [Fact]
    public void A_missing_pac_exits_3_and_imports_nothing()
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac { Answer = _ => new PacResult(false, false, 0, "") };

        var r = Apply(SeedTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal("failed", (string?)doc["status"]);
        Assert.StartsWith("pac was not found on PATH.", (string?)doc["message"]);
        Assert.Equal(["PowerOpsSeed:failed"], Statuses(doc));
        Assert.Null(doc["solutions"]![0]!["pacExitCode"]);
    }

    [Theory]
    [InlineData("zip", "B.zip changed after the re-plan read it")]
    [InlineData("settings", "B.settings.json changed after the re-plan read it")]
    public void A_file_changed_between_the_re_plan_and_its_import_stops_the_run_with_exit_3(string file, string message)
    {
        Chain();
        File.WriteAllText(Path.Combine(Folder, "B.settings.json"), "{}");
        MakePlan(new FakeTarget());
        var path = Path.Combine(Folder, file == "zip" ? "B.zip" : "B.settings.json");
        var pac = new FakePac { During = n => File.WriteAllBytes(path, [.. File.ReadAllBytes(path), (byte)' ']) };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Assert.Single(pac.Calls);
        var doc = Json(r);
        Assert.Equal("failed", (string?)doc["status"]);
        Assert.Equal(["A:ok", "B:failed", "C:not-run"], Statuses(doc));
        Assert.StartsWith(message, (string?)doc["message"]);
        Assert.Contains("Imported: A.", (string?)doc["message"]);
        Assert.Null(doc["solutions"]![1]!["pacExitCode"]);
        Assert.Equal("", r.Err.Replace("environment: Contoso Test\n", "").Replace("importing 1 A (A.zip)\nimported A\n", ""));
    }

    // M26: the re-read before an import is capped like the first read, so a file swapped for an endless one is "changed", not an out-of-memory crash.
    [Theory]
    [InlineData("zip", "B.zip changed after the re-plan read it")]
    [InlineData("settings", "B.settings.json changed after the re-plan read it")]
    public void A_file_swapped_for_a_link_to_dev_zero_between_the_re_plan_and_its_import_counts_as_changed(string file, string message)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "No /dev/zero; creating a link needs privileges on Windows.");
        Chain();
        File.WriteAllText(Path.Combine(Folder, "B.settings.json"), "{}");
        MakePlan(new FakeTarget());
        var path = Path.Combine(Folder, file == "zip" ? "B.zip" : "B.settings.json");
        var pac = new FakePac
        {
            During = n =>
            {
                File.Delete(path);
                File.CreateSymbolicLink(path, "/dev/zero");
            },
        };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Assert.StartsWith(message, (string?)Json(r)["message"]);
    }

    // ---- the flows after the imports ---------------------------------------------------------------------------

    [Fact]
    public void The_flows_are_read_by_id_from_the_second_read_and_a_flow_as_expected_exits_0()
    {
        SeedZip(Folder);
        MakePlan();
        var fake = SeedTarget();

        var r = Apply(new FakeTarget { Snaps = fake.Snaps, After = SeedAfter("on") }, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        var flow = doc["flows"]![0]!;
        Assert.Equal(("Stamp work item status", "on", "on"), ((string?)flow["name"], (string?)flow["expectedState"], (string?)flow["stateAfter"]));
    }

    [Theory]
    [InlineData("off", "off")]
    [InlineData("gone", "missing")]
    public void A_flow_not_in_its_expected_state_exits_1_after_a_good_import(string state, string shown)
    {
        SeedZip(Folder);
        MakePlan();
        var after = state == "gone" ? new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase) { ["PowerOpsSeed"] = SeedAfter("on")["PowerOpsSeed"] with { Flows = [] } } : SeedAfter(state);

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = after }, new FakePac(), "--apply");

        Assert.Equal(1, r.Code);
        Assert.Contains($"flow PowerOpsSeed/Stamp work item status: expected on, now {shown}\n", r.Out);
        Assert.EndsWith("applied, but 1 flow is not in its expected state\n", r.Out);
    }

    [Fact]
    public void A_flow_that_is_meant_to_stay_off_and_is_off_exits_0()
    {
        var flowId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        SmallZip(Folder, "Offline", files: ("Workflows/F.json.data.xml", $"<Workflow WorkflowId=\"{{{flowId}}}\" Name=\"F\"><Category>5</Category><StateCode>0</StateCode></Workflow>"));
        MakePlan(new FakeTarget());
        var after = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase) { ["Offline"] = Installed("Offline", "1.0.0.0") with { Flows = [new SnapshotFlow(flowId, "Renamed in the target", "off")] } };

        var r = Apply(new FakeTarget { After = after }, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal(("off", "off"), ((string?)Json(r)["flows"]![0]!["expectedState"], (string?)Json(r)["flows"]![0]!["stateAfter"]));
    }

    [Fact]
    public void A_failed_second_read_after_good_imports_exits_3_with_status_imported_and_no_state_after()
    {
        SeedZip(Folder);
        MakePlan();

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, FailAfter = new DriftReadException("the read failed", "tool line") }, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.StartsWith("The imports succeeded. ", (string?)doc["message"]);
        Assert.Contains("the read failed", (string?)doc["message"]);
        Assert.Equal(["PowerOpsSeed:ok"], Statuses(doc));
        Assert.Null(doc["flows"]![0]!["stateAfter"]);
        Assert.Contains("  | tool line\n", r.Err);
    }

    [Fact]
    public void Any_exception_from_the_second_read_after_good_imports_is_the_same_exit_3_row_and_keeps_the_report()
    {
        SeedZip(Folder);
        MakePlan();
        var file = Path.Combine(Out, "apply.json");

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, FailAfter = new HttpRequestException("SENTINEL") }, new FakePac(), "--apply", "--format", "json", "--out", file);

        Assert.Equal(3, r.Code);
        var doc = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.StartsWith("The imports succeeded. ", (string?)doc["message"]);
        Assert.DoesNotContain("SENTINEL", (string?)doc["message"]);
        Assert.DoesNotContain("SENTINEL", r.Err);
        Assert.DoesNotContain("rollback", (string?)doc["message"]);
        Assert.Equal(["PowerOpsSeed:ok"], Statuses(doc));
        Assert.Null(doc["flows"]![0]!["stateAfter"]);
    }

    [Fact]
    public void The_second_read_asks_for_the_same_solutions_as_the_first()
    {
        SeedZip(Folder);
        MakePlan();
        var fake = new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") };

        Apply(fake, new FakePac(), "--apply");

        Assert.Equal(2, fake.Calls);
        Assert.Equal([TargetValue, "PowerOpsSeed"], fake.Solutions[0]);
        Assert.Equal(fake.Solutions[0], fake.Solutions[1]);
    }

    // ---- the solutions after the imports (M98) -------------------------------------------------------------------

    private static Dictionary<string, Snapshot> AfterOf(params Snapshot[] snaps) => snaps.ToDictionary(x => x.Solution, StringComparer.OrdinalIgnoreCase);

    // The second read after the imports of Chain(), or of AgentZip(), at each zip's version.
    private static Dictionary<string, Snapshot> ChainAfter() => AfterOf(Installed("A", "1.0.0.0"), Installed("B", "1.0.0.0"), Installed("C", "1.0.0.0"));

    private static Dictionary<string, Snapshot> AgentToolsAfter() => AfterOf(Installed("PowerOpsAgentTools", "1.0.0.0", managed: false));

    private static string Mismatch(string solution, string what) => $"pac reported success, but the target shows {solution} {what}; check the solution's import history.";

    private static JsonNode? AfterOf(JsonObject doc, int row) => doc["solutions"]![row]!["after"];

    [Fact]
    public void A_flowless_import_reads_the_target_again_and_at_the_zips_version_exits_0_with_after_set()
    {
        SmallZip(Folder, "Solo");
        MakePlan(new FakeTarget());
        var fake = new FakeTarget { After = AfterOf(Installed("Solo", "1.0.0.0")) };

        var r = Apply(fake, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Null((string?)doc["message"]);
        Assert.Equal("""{"installed":true,"version":"1.0.0.0","managed":true}""", AfterOf(doc, 0)?.ToJsonString());
        Assert.Equal(2, fake.Calls);
        Assert.Equal(fake.Solutions[0], fake.Solutions[1]);
    }

    [Fact]
    public void A_flowless_import_the_target_does_not_show_exits_1_with_status_imported_and_the_not_installed_message()
    {
        SmallZip(Folder, "Solo");
        MakePlan(new FakeTarget());

        var r = Apply(new FakeTarget(), new FakePac(), "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Equal(Mismatch("Solo", "not installed"), (string?)doc["message"]);
        Assert.Equal("""{"installed":false,"version":null,"managed":null}""", AfterOf(doc, 0)?.ToJsonString());

        var text = Apply(new FakeTarget(), new FakePac(), "--apply");

        Assert.Equal(1, text.Code);
        Assert.Contains("1 Solo (Solo.zip): install, ok\n  after: not installed\n", text.Out);
        Assert.EndsWith($"applied: {Mismatch("Solo", "not installed")}\n", text.Out);
    }

    [Fact]
    public void A_flowless_upgrade_the_target_shows_at_a_lower_version_exits_1_with_the_at_version_message()
    {
        SmallZip(Folder, "Solo", version: "1.0.0.1");
        MakePlan(new FakeTarget { Snaps = { ["Solo"] = Installed("Solo", "1.0.0.0") } });

        var r = Apply(new FakeTarget { Snaps = { ["Solo"] = Installed("Solo", "1.0.0.0") }, After = AfterOf(Installed("Solo", "1.0.0.0")) }, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Equal(Mismatch("Solo", "at version 1.0.0.0"), (string?)doc["message"]);
        Assert.Equal("1.0.0.0", (string?)AfterOf(doc, 0)?["version"]);
    }

    [Fact]
    public void A_failed_read_after_a_flowless_import_exits_3_with_every_after_null_and_the_agents_still_read_after_the_reads_stderr()
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => [("demo_Helper", true)] };

        var r = ApplyAgents(new FakeTarget { FailAfter = new DriftReadException("the read failed", "tool line") }, new FakePac(), agents.Read, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.StartsWith("The imports succeeded. Reading the target again failed: the read failed", (string?)doc["message"]);
        Assert.Equal(["PowerOpsAgentTools:ok"], Statuses(doc));
        Assert.All(doc["solutions"]!.AsArray(), s => Assert.Null(s!["after"]));
        Assert.Empty(doc["flows"]!.AsArray());
        Assert.Equal(("found", true), AgentOf(doc));
        Assert.EndsWith("\n  | tool line\n", r.Err);

        var both = ApplyAgents(new FakeTarget { FailAfter = new DriftReadException("the read failed", "tool line") }, new FakePac(), (_, _, _) => throw new DriftReadException("agents failed"), "--apply", "--format", "json");

        Assert.Equal(3, both.Code);
        Assert.EndsWith("\n  | tool line\nwarning: the agent read-back failed: agents failed\n", both.Err);
    }

    [Fact]
    public void A_staged_upgrade_the_target_shows_at_the_zips_version_exits_0_and_at_the_old_version_exits_1()
    {
        SmallZip(Folder, "Solo", version: "1.0.0.1");
        var before = Installed("Solo", "1.0.0.0");
        MakePlan(new FakeTarget { Snaps = { ["Solo"] = before } });
        var pac = new FakePac();

        var good = Apply(new FakeTarget { Snaps = { ["Solo"] = before }, After = AfterOf(Installed("Solo", "1.0.0.1")) }, pac, "--apply", "--format", "json");

        Assert.Contains("--stage-and-upgrade", Assert.Single(pac.Calls));
        Assert.Equal(0, good.Code);
        Assert.Null((string?)Json(good)["message"]);
        Assert.Equal("""{"installed":true,"version":"1.0.0.1","managed":true}""", AfterOf(Json(good), 0)?.ToJsonString());

        var old = Apply(new FakeTarget { Snaps = { ["Solo"] = before }, After = AfterOf(before) }, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(1, old.Code);
        Assert.Equal(Mismatch("Solo", "at version 1.0.0.0"), (string?)Json(old)["message"]);
    }

    [Fact]
    public void A_dry_run_has_every_after_null_and_reads_the_target_once()
    {
        SeedZip(Folder);
        MakePlan();
        var fake = new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") };

        var r = Apply(fake, new FakePac(), "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.All(Json(r)["solutions"]!.AsArray(), s => Assert.Null(s!["after"]));
        Assert.True(Json(r)["solutions"]![0]!.AsObject().ContainsKey("after"));
        Assert.Equal(1, fake.Calls);
        Assert.DoesNotContain("after:", Apply(SeedTarget(), new FakePac()).Out);
    }

    [Fact]
    public void Flows_as_expected_with_a_version_that_differs_exit_1_with_the_version_message_and_the_flows_reported()
    {
        SeedZip(Folder);
        MakePlan();
        var after = AfterOf(SeedAfter("on")["PowerOpsSeed"] with { Version = "1.0.0.0" });

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = after }, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Equal(Mismatch("PowerOpsSeed", "at version 1.0.0.0"), (string?)doc["message"]);
        Assert.Equal(("on", "on"), ((string?)doc["flows"]![0]!["expectedState"], (string?)doc["flows"]![0]!["stateAfter"]));
    }

    [Fact]
    public void Flows_at_the_zips_version_with_a_flow_off_exit_1_with_no_message_and_the_flow_sentence()
    {
        SeedZip(Folder);
        MakePlan();

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("off") }, new FakePac(), "--apply", "--format", "json");
        var text = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("off") }, new FakePac(), "--apply");

        Assert.Equal(1, r.Code);
        Assert.Null((string?)Json(r)["message"]);
        Assert.Equal("1.0.0.1", (string?)AfterOf(Json(r), 0)?["version"]);
        Assert.Contains("PowerOpsSeed (PowerOpsSeed.zip): upgrade, ok\n  after: 1.0.0.1 (managed)\n", text.Out);
        Assert.EndsWith("applied, but 1 flow is not in its expected state\n", text.Out);
    }

    [Fact]
    public void A_flow_off_and_a_version_mismatch_together_exit_1_with_the_version_message_and_the_flow_rows()
    {
        SeedZip(Folder);
        MakePlan();
        var after = AfterOf(SeedAfter("off")["PowerOpsSeed"] with { Version = "1.0.0.0" });

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = after }, new FakePac(), "--apply", "--format", "json");
        var text = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = after }, new FakePac(), "--apply");

        Assert.Equal(1, r.Code);
        Assert.Equal(Mismatch("PowerOpsSeed", "at version 1.0.0.0"), (string?)Json(r)["message"]);
        Assert.Equal(("on", "off"), ((string?)Json(r)["flows"]![0]!["expectedState"], (string?)Json(r)["flows"]![0]!["stateAfter"]));
        Assert.Contains("flow PowerOpsSeed/Stamp work item status: expected on, now off\n", text.Out);
        Assert.EndsWith($"applied: {Mismatch("PowerOpsSeed", "at version 1.0.0.0")}\n", text.Out);
    }

    [Fact]
    public void Two_mismatched_solutions_give_one_sentence_each_in_plan_order()
    {
        SmallZip(Folder, "A");
        SmallZip(Folder, "B", version: "1.0.0.1");
        var target = new FakeTarget { Snaps = { ["B"] = Installed("B", "1.0.0.0") } };
        MakePlan(target);

        var r = Apply(target, new FakePac(), "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        var doc = Json(r);
        Assert.Equal(["A:ok", "B:ok"], Statuses(doc));
        Assert.Equal($"{Mismatch("A", "not installed")} {Mismatch("B", "at version 1.0.0.0")}", (string?)doc["message"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_failed_or_timed_out_later_import_makes_no_read_and_leaves_the_earlier_ok_row_after_null(bool timedOut)
    {
        Chain();
        MakePlan(new FakeTarget());
        var fake = new FakeTarget { After = AfterOf(Installed("A", "1.0.0.0"), Installed("B", "1.0.0.0"), Installed("C", "1.0.0.0")) };
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, timedOut, timedOut ? 0 : 1, "boom") : null };

        var r = Apply(fake, pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal(["A:ok", timedOut ? "B:unknown" : "B:failed", "C:not-run"], Statuses(doc));
        Assert.All(doc["solutions"]!.AsArray(), s => Assert.Null(s!["after"]));
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public void A_version_from_the_read_that_is_not_four_numeric_parts_is_null_in_after_and_unknown_in_the_message()
    {
        SmallZip(Folder, "Solo");
        MakePlan(new FakeTarget());
        var after = AfterOf(Installed("Solo", "1.0.0.0\n::error::HOSTILE"));

        var r = Apply(new FakeTarget { After = after }, new FakePac(), "--apply", "--format", "json");
        var text = Apply(new FakeTarget { After = after }, new FakePac(), "--apply");

        Assert.Equal(1, r.Code);
        Assert.Equal(true, (bool?)AfterOf(Json(r), 0)?["installed"]);
        Assert.Null(AfterOf(Json(r), 0)!["version"]);
        Assert.Equal(Mismatch("Solo", "at version unknown"), (string?)Json(r)["message"]);
        Assert.DoesNotContain("HOSTILE", r.Out);
        Assert.Equal(1, text.Code);
        Assert.Contains("1 Solo (Solo.zip): install, ok\n  after: version not read (managed)\n", text.Out);
        Assert.EndsWith($"applied: {Mismatch("Solo", "at version unknown")}\n", text.Out);
        Assert.DoesNotContain("HOSTILE", text.Out + text.Err);
    }

    // ---- the agents after the imports --------------------------------------------------------------------------

    private const string HelperPublished = """{"value":[{"schemaname":"demo_Helper","publishedon":"2026-01-01T00:00:00Z"}]}""";

    private Outcome ApplyAgents(FakeTarget target, FakePac pac, Func<string, Guid, IReadOnlyList<string>, IReadOnlyList<(string SchemaName, bool PublishedOn)>> agents, params string[] extra) =>
        RunWith(target, pac, agents, ["deploy", "apply", PlanFile, "--target", TargetValue, .. extra]);

    private static (string State, bool? PublishedOn) AgentOf(JsonObject doc) => ((string?)doc["agents"]![0]!["state"] ?? "", (bool?)doc["agents"]![0]!["publishedOn"]);

    [Fact]
    public void The_dry_run_lists_each_agent_of_a_zip_it_would_import_with_no_state_and_reads_nothing()
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents();

        var json = ApplyAgents(new FakeTarget(), new FakePac(), agents.Read, "--format", "json");
        var text = ApplyAgents(new FakeTarget(), new FakePac(), agents.Read);

        Assert.Equal(0, json.Code);
        var agent = Json(json)["agents"]!.AsArray().Single()!;
        Assert.Equal(("PowerOpsAgentTools", "demo_Helper", null, null), ((string?)agent["solution"], (string?)agent["name"], (string?)agent["state"], (bool?)agent["publishedOn"]));
        Assert.Contains("agent PowerOpsAgentTools/demo_Helper: in the import; the import may leave it published\n", text.Out);
        Assert.Empty(agents.Calls);
    }

    [Fact]
    public void An_agent_only_solution_with_no_flow_is_read_back_by_name_against_the_environment_imported_into()
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => [("DEMO_helper", true)] };
        var pac = new FakePac();

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, pac, agents.Read, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        var call = Assert.Single(agents.Calls);
        Assert.Equal((TargetValue, TargetId), (call.Target, call.EnvironmentId));
        Assert.Equal(["demo_Helper"], call.Names);
        Assert.Equal(("found", true), AgentOf(Json(r)));
        Assert.Equal("imported", (string?)Json(r)["status"]);
        Assert.Null((string?)Json(r)["message"]);
        Assert.Single(pac.Calls);
    }

    [Theory]
    [InlineData("""{"value":[{"schemaname":"demo_Helper","publishedon":"2026-01-01T00:00:00Z"}]}""", "found", true)]
    [InlineData("""{"value":[{"schemaname":"demo_Helper","publishedon":null}]}""", "found", false)]
    [InlineData("""{"value":[]}""", "missing", null)]
    public void A_bot_row_gives_found_with_publishedon_and_no_row_gives_missing_through_the_real_reader(string body, string state, bool? published)
    {
        AgentZip(Folder);
        MakePlan();
        var live = new LiveAgents { Web = _ => new WebResult(200, body, false) };

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), live.Read, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal((state, published), AgentOf(Json(r)));
        Assert.Equal("bots", Assert.Single(live.WebCalls).AbsolutePath.Split('/')[^1]);
        Assert.DoesNotContain("warning", r.Err);
    }

    [Theory]
    [InlineData("403")]
    [InlineData("timeout")]
    [InlineData("malformed")]
    public void A_failed_agent_read_gives_not_read_and_a_warning_and_changes_no_status_row_or_exit_code(string failure)
    {
        AgentZip(Folder);
        MakePlan();
        var live = new LiveAgents
        {
            Web = _ => failure switch
            {
                "403" => new WebResult(403, """{"error":{"code":"1","message":"No access"}}""", false),
                "timeout" => new WebResult(0, "", true),
                _ => new WebResult(200, """{"value":[{"publishedon":"x"}]}""", false),
            },
        };

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), live.Read, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        Assert.Equal(("not-read", null), AgentOf(doc));
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.Null((string?)doc["message"]);
        Assert.Equal(["PowerOpsAgentTools:ok"], Statuses(doc));
        Assert.Contains("\nwarning: the agent read-back failed: ", r.Err);
        Assert.Contains("`bot`", r.Err);
        Assert.DoesNotContain("crm.dynamics.com", r.Err);
        Assert.DoesNotContain("fake-token", r.Err);
    }

    [Fact]
    public void Any_other_exception_from_the_agent_read_gives_not_read_and_a_warning_without_its_text()
    {
        AgentZip(Folder);
        MakePlan();

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), (_, _, _) => throw new HttpRequestException("SENTINEL"), "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal(("not-read", null), AgentOf(Json(r)));
        Assert.EndsWith("\nwarning: the agent read-back failed: failed.\n", r.Err);
        Assert.DoesNotContain("SENTINEL", r.Err);
    }

    [Fact]
    public void A_failed_agent_read_forwards_the_tools_text_like_a_failed_flow_read_does()
    {
        AgentZip(Folder);
        MakePlan();

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), (_, _, _) => throw new DriftReadException("the read failed", "tool line"), "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal("  | tool line\nwarning: the agent read-back failed: the read failed\n", r.Err);
        Assert.Contains("agent PowerOpsAgentTools/demo_Helper: not read\n", r.Out);
    }

    [Fact]
    public void A_partial_failure_still_reads_the_agents_of_the_solution_that_went_through_and_keeps_failed_and_exit_3()
    {
        AgentZip(Folder);
        SmallZip(Folder, "Second", requires: ["PowerOpsAgentTools (1.0)"]);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => [("demo_Helper", false)] };
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, false, 1, "", "boom\n") : null };

        var r = ApplyAgents(new FakeTarget(), pac, agents.Read, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal("failed", (string?)doc["status"]);
        Assert.Equal(["PowerOpsAgentTools:ok", "Second:failed"], Statuses(doc));
        Assert.Equal(("found", false), AgentOf(doc));
        Assert.Equal(["demo_Helper"], Assert.Single(agents.Calls).Names);
    }

    [Fact]
    public void A_failed_first_import_reads_no_agents()
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents();

        var r = ApplyAgents(new FakeTarget(), new FakePac { Answer = _ => new PacResult(true, false, 1, "", "boom\n") }, agents.Read, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Assert.Empty(Json(r)["agents"]!.AsArray());
        Assert.Empty(agents.Calls);
    }

    private void FlowAndAgentPlan()
    {
        AgentZip(Folder);
        SmallZip(Folder, "Flows", requires: ["PowerOpsAgentTools (1.0)"], files: ("Workflows/F.json.data.xml", "<Workflow WorkflowId=\"{11111111-2222-3333-4444-555555555555}\" Name=\"F\"><Category>5</Category><StateCode>1</StateCode></Workflow>"));
        MakePlan();
    }

    [Fact]
    public void A_failed_flow_read_keeps_its_message_and_exit_3_and_the_agents_are_still_read()
    {
        FlowAndAgentPlan();
        var agents = new FakeAgents { Answer = _ => [("demo_Helper", true)] };

        var r = ApplyAgents(new FakeTarget { FailAfter = new DriftReadException("the read failed", "tool line") }, new FakePac(), agents.Read, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        var doc = Json(r);
        Assert.Equal("imported", (string?)doc["status"]);
        Assert.StartsWith("The imports succeeded. Reading the target again failed: the read failed", (string?)doc["message"]);
        Assert.Null(doc["flows"]![0]!["stateAfter"]);
        Assert.Equal(("found", true), AgentOf(doc));
        Assert.EndsWith("\n  | tool line\n", r.Err);
        Assert.DoesNotContain("warning", r.Err);
    }

    [Fact]
    public void Both_reads_failing_write_the_flow_reads_stderr_then_the_agent_warning_and_exit_3()
    {
        FlowAndAgentPlan();

        var r = ApplyAgents(new FakeTarget { FailAfter = new DriftReadException("the read failed", "tool line") }, new FakePac(), (_, _, _) => throw new DriftReadException("agents failed"), "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Assert.EndsWith("\n  | tool line\nwarning: the agent read-back failed: agents failed\n", r.Err);
        Assert.Equal(("not-read", null), AgentOf(Json(r)));
    }

    [Fact]
    public void A_good_flow_read_and_an_agent_read_together_exit_0()
    {
        FlowAndAgentPlan();
        var after = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["Flows"] = Installed("Flows", "1.0.0.0") with { Flows = [new SnapshotFlow(Guid.Parse("11111111-2222-3333-4444-555555555555"), "F", "on")] },
            ["PowerOpsAgentTools"] = Installed("PowerOpsAgentTools", "1.0.0.0", managed: false),
        };

        var r = ApplyAgents(new FakeTarget { After = after }, new FakePac(), new FakeAgents { Answer = _ => [("demo_Helper", true)] }.Read, "--apply", "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal("on", (string?)Json(r)["flows"]![0]!["stateAfter"]);
        Assert.Equal(("found", true), AgentOf(Json(r)));
    }

    [Fact]
    public void A_skip_only_plan_and_a_plan_with_no_agent_make_no_agent_read()
    {
        AgentZip(Folder);
        var skipping = new FakeTarget { Snaps = { ["PowerOpsAgentTools"] = Installed("PowerOpsAgentTools", "1.0.0.0", managed: false) } };
        MakePlan(skipping);
        var agents = new FakeAgents();

        var skipped = ApplyAgents(skipping, new FakePac(), agents.Read, "--apply", "--format", "json");

        Assert.Equal(0, skipped.Code);
        Assert.Empty(Json(skipped)["agents"]!.AsArray());

        File.Delete(Path.Combine(Folder, "PowerOpsAgentTools.zip"));
        File.Delete(Path.Combine(Folder, "PowerOpsAgentTools.settings.json"));
        SmallZip(Folder, "Solo");
        MakePlan();
        var plain = ApplyAgents(new FakeTarget { After = AfterOf(Installed("Solo", "1.0.0.0")) }, new FakePac(), agents.Read, "--apply", "--format", "json");

        Assert.Equal(0, plain.Code);
        Assert.Empty(Json(plain)["agents"]!.AsArray());
        Assert.Empty(agents.Calls);
    }

    [Fact]
    public void A_refused_report_has_no_agents()
    {
        AgentZip(Folder);
        MakePlan();
        File.AppendAllText(Path.Combine(Folder, "PowerOpsAgentTools.settings.json"), " ");
        var agents = new FakeAgents();

        var r = ApplyAgents(new FakeTarget(), new FakePac(), agents.Read, "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        Assert.Empty(Json(r)["agents"]!.AsArray());
        Assert.Empty(agents.Calls);
    }

    // ---- agentsReadAt (M89) -------------------------------------------------------------------------------------

    private const string Stamp = @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z";

    private static readonly Regex NoteLine = new($@"^note: agents read at ({Stamp}); an agent can still become published after this read \(40 seconds after the import in one run\): check in Copilot Studio, or with drift\.$", RegexOptions.Multiline);

    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private static void AssertNoReadTime(Outcome json, Outcome text)
    {
        var doc = Json(json);
        Assert.True(doc.ContainsKey("agentsReadAt"));
        Assert.Null(doc["agentsReadAt"]);
        Assert.DoesNotContain("note: agents read", text.Out);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_read_agent_found_or_missing_has_the_time_it_was_read_between_the_apply_time_and_the_end_of_the_run(bool found)
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => found ? [("demo_Helper", true)] : [] };

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), agents.Read, "--apply", "--format", "json");
        var after = Now();

        Assert.Equal(0, r.Code);
        var doc = Json(r);
        var readAt = (string?)doc["agentsReadAt"];
        Assert.Matches($"^{Stamp}$", readAt);
        Assert.True(string.CompareOrdinal((string?)doc["appliedAt"], readAt) <= 0);
        Assert.True(string.CompareOrdinal(readAt, after) <= 0);
        Assert.Equal(found ? "found" : "missing", AgentOf(doc).State);
        Assert.Equal("agentsReadAt", doc.Last().Key);
    }

    [Fact]
    public void The_text_report_has_the_note_with_the_read_time_after_the_agent_lines_and_before_the_summary()
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => [("demo_Helper", true)] };

        var r = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), agents.Read, "--apply");
        var after = Now();

        Assert.Equal(0, r.Code);
        var lines = r.Out.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var note = Assert.Single(lines, l => l.StartsWith("note: agents read", StringComparison.Ordinal));
        var match = NoteLine.Match(note);
        Assert.True(match.Success, note);
        var applied = Regex.Match(r.Out, $"^Applied at: ({Stamp})$", RegexOptions.Multiline).Groups[1].Value;
        Assert.True(string.CompareOrdinal(applied, match.Groups[1].Value) <= 0);
        Assert.True(string.CompareOrdinal(match.Groups[1].Value, after) <= 0);
        var at = Array.IndexOf(lines, note);
        Assert.StartsWith("agent PowerOpsAgentTools/demo_Helper: found", lines[at - 1], StringComparison.Ordinal);
        Assert.Equal(["applied"], lines[(at + 1)..]);
    }

    [Fact]
    public void The_dry_run_and_a_refusal_have_no_read_time_and_no_note()
    {
        AgentZip(Folder);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => [("demo_Helper", true)] };

        AssertNoReadTime(
            ApplyAgents(new FakeTarget(), new FakePac(), agents.Read, "--format", "json"),
            ApplyAgents(new FakeTarget(), new FakePac(), agents.Read));

        File.AppendAllText(Path.Combine(Folder, "PowerOpsAgentTools.settings.json"), " ");
        AssertNoReadTime(
            ApplyAgents(new FakeTarget(), new FakePac(), agents.Read, "--apply", "--format", "json"),
            ApplyAgents(new FakeTarget(), new FakePac(), agents.Read, "--apply"));
        Assert.Empty(agents.Calls);
    }

    [Fact]
    public void An_agent_read_that_throws_has_no_read_time_and_no_note_and_changes_nothing_else()
    {
        AgentZip(Folder);
        MakePlan();

        var json = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), (_, _, _) => throw new HttpRequestException("SENTINEL"), "--apply", "--format", "json");
        var text = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), (_, _, _) => throw new HttpRequestException("SENTINEL"), "--apply");

        AssertNoReadTime(json, text);
        Assert.Equal((0, 0), (json.Code, text.Code));
        Assert.Equal("imported", (string?)Json(json)["status"]);
        Assert.Null((string?)Json(json)["message"]);
        Assert.EndsWith("\nwarning: the agent read-back failed: failed.\n", json.Err);
        Assert.Contains("agent PowerOpsAgentTools/demo_Helper: not read\n", text.Out);
        Assert.EndsWith("\napplied\n", text.Out);
    }

    [Fact]
    public void A_partial_failure_has_the_read_time_of_the_solution_that_went_through_and_a_failed_first_import_has_none()
    {
        AgentZip(Folder);
        SmallZip(Folder, "Second", requires: ["PowerOpsAgentTools (1.0)"]);
        MakePlan();
        var agents = new FakeAgents { Answer = _ => [("demo_Helper", false)] };

        var partial = ApplyAgents(new FakeTarget(), new FakePac { Answer = n => n == 1 ? new PacResult(true, false, 1, "", "boom\n") : null }, agents.Read, "--apply", "--format", "json");
        var first = ApplyAgents(new FakeTarget(), new FakePac { Answer = _ => new PacResult(true, false, 1, "", "boom\n") }, agents.Read, "--apply", "--format", "json");

        Assert.Equal(3, partial.Code);
        Assert.Equal("failed", (string?)Json(partial)["status"]);
        Assert.Matches($"^{Stamp}$", (string?)Json(partial)["agentsReadAt"]);
        Assert.Equal(3, first.Code);
        Assert.Null(Json(first)["agentsReadAt"]);
        Assert.True(Json(first).ContainsKey("agentsReadAt"));
        Assert.Single(agents.Calls);
    }

    [Fact]
    public void An_agent_whose_name_powerops_does_not_send_is_not_read_and_has_no_read_time_and_no_web_call()
    {
        // A copy of the fixture with a schema name the reader refuses; the tracked fixture is never edited.
        var source = Path.Combine(Scratch("agent"), "unpacked");
        var fixture = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "agents", "dataverse-tools");
        foreach (var file in Directory.EnumerateFiles(fixture, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(source, Path.GetRelativePath(fixture, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }

        var bot = Path.Combine(source, "bots", "demo_Helper", "bot.xml");
        File.WriteAllText(bot, File.ReadAllText(bot).Replace("schemaname=\"demo_Helper\"", "schemaname=\"demo Helper\""));
        SolutionZip.Create(source, Path.Combine(Folder, "PowerOpsAgentTools.zip"));
        File.WriteAllText(
            Path.Combine(Folder, "PowerOpsAgentTools.settings.json"),
            """{"ConnectionReferences":[{"LogicalName":"demo_Helper.shared_commondataserviceforapps.shared-commondataser-0000001","ConnectionId":"fake-connection-id","ConnectorId":"/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"}]}""");
        MakePlan();
        var live = new LiveAgents();

        var json = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), live.Read, "--apply", "--format", "json");
        var text = ApplyAgents(new FakeTarget { After = AgentToolsAfter() }, new FakePac(), live.Read, "--apply");

        Assert.Equal((0, 0), (json.Code, text.Code));
        Assert.Equal(("not-read", null), AgentOf(Json(json)));
        AssertNoReadTime(json, text);
        Assert.Empty(live.WebCalls);
    }

    // ---- --require-reviews (M75) -------------------------------------------------------------------------------

    private string SeedZipPath => Path.Combine(Folder, "PowerOpsSeed.zip");

    private const string NoneRecords = "no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its deploy-hash or its solution-hash";

    private static FakeTarget SeedAfterOn() => new() { Snaps = SeedTarget().Snaps, After = SeedAfter("on") };

    [Theory]
    [InlineData("text", "deploy-apply-dry-run.txt", false)]
    [InlineData("json", "deploy-apply-dry-run.json", false)]
    [InlineData("text", "deploy-apply.txt", true)]
    [InlineData("json", "deploy-apply.json", true)]
    public void With_require_reviews_a_covered_zip_plans_and_imports_and_the_reports_are_todays_goldens(string format, string golden, bool apply)
    {
        SeedZip(Folder);
        MakePlan();
        Reviewed(SeedZipPath);
        var pac = new FakePac();

        var r = Apply(SeedAfterOn(), pac, ["--require-reviews", "--format", format, .. apply ? new[] { "--apply" } : []]);

        Assert.Equal(0, r.Code);
        Assert.Equal(apply ? 1 : 0, pac.Calls.Count);
        Assert.DoesNotContain("warning:", r.Err);
        Golden.Verify(Scrub(r.Out), golden);
    }

    // Stages a plan whose imports some review doesn't cover: the refusal's message, the stderr of the same run (with or without the option), and the target.
    private (string Message, string Warnings, Func<FakeTarget> Target) Uncovered(string kind)
    {
        if (kind == "two-zips")
        {
            Chain();
            MakePlan(new FakeTarget());
            Reviewed(Path.Combine(Folder, "A.zip"), plan: "a");
            return (
                $"no review covers B.zip: {NoneRecords} (2 zips in all; the warning lines name each); nothing was imported.",
                $"warning: B.zip: {NoneRecords}\nwarning: C.zip: {NoneRecords}\n",
                () => new FakeTarget { After = ChainAfter() });
        }

        SeedZip(Folder);
        MakePlan();
        switch (kind)
        {
            case "security-blocked":
                Edit(Path.Combine(Reviewed(SeedZipPath), "security-solution.md"), "state: ready", "state: blocked");
                return ("no review covers PowerOpsSeed.zip: docs/plans/m: security-solution.md is blocked; nothing was imported.", "warning: PowerOpsSeed.zip: docs/plans/m: security-solution.md is blocked\n", SeedAfterOn);
            case "stale":
                Edit(Path.Combine(Reviewed(SeedZipPath), "plan.md"), "# Plan", "# Plan, edited");
                const string Stale = "docs/plans/m: verify.md was not written for the current plan; security-solution.md was not written for the current plan";
                return ($"no review covers PowerOpsSeed.zip: {Stale}; nothing was imported.", $"warning: PowerOpsSeed.zip: {Stale}\n", SeedAfterOn);
            case "no-docs-plans":
                // The .git folder ends the walk, so a docs/plans above the temp folder can't change this test.
                Directory.CreateDirectory(Path.Combine(_root, ".git"));
                return ("no docs/plans folder at or above the plan file's folder, so no review covers the zips; nothing was imported.", "", SeedAfterOn);
            default:
                Assert.SkipWhen(OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
                Directory.CreateDirectory(Path.Combine(Dir("elsewhere"), "plans", "x"));
                File.WriteAllText(Path.Combine(Dir("elsewhere"), "plans", "x", "plan.md"), ReviewedPlanText);
                Directory.CreateSymbolicLink(Path.Combine(_root, "docs"), Dir("elsewhere"));
                return ("docs/plans could not be read, so the zips were not matched with review reports; nothing was imported.", "warning: docs/plans could not be read, so the zips were not matched with review reports\n", SeedAfterOn);
        }
    }

    [Theory]
    [InlineData("security-blocked")]
    [InlineData("stale")]
    [InlineData("no-docs-plans")]
    [InlineData("linked-docs")]
    [InlineData("two-zips")]
    public void With_require_reviews_an_import_no_review_covers_is_refused_with_no_pac_call(string kind)
    {
        var (message, warnings, target) = Uncovered(kind);

        foreach (var apply in new[] { false, true })
        {
            var pac = new FakePac();

            var r = Apply(target(), pac, ["--require-reviews", "--format", "json", .. apply ? new[] { "--apply" } : []]);

            Assert.Equal(1, r.Code);
            var doc = Json(r);
            Assert.Equal("refused", (string?)doc["status"]);
            Assert.Equal(message, (string?)doc["message"]);
            Assert.All(Statuses(doc), s => Assert.EndsWith(":not-run", s));
            Assert.Equal(warnings, r.Err);
            Assert.Empty(pac.Calls);
            Assert.DoesNotContain(_root, r.Out + r.Err);
        }
    }

    [Theory]
    [InlineData("security-blocked")]
    [InlineData("stale")]
    [InlineData("no-docs-plans")]
    [InlineData("linked-docs")]
    [InlineData("two-zips")]
    public void Without_require_reviews_the_same_run_prints_its_warnings_and_imports(string kind)
    {
        var (_, warnings, target) = Uncovered(kind);
        var pac = new FakePac();

        var r = Apply(target(), pac, "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal(warnings, r.Err);
        Assert.NotEmpty(pac.Calls);
    }

    [Fact]
    public void With_require_reviews_a_covered_import_beside_an_uncovered_skipped_zip_imports()
    {
        SmallZip(Folder, "Kept");
        var solo = SmallZip(Folder, "Solo");
        MakePlan(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") } });
        Reviewed(solo);
        var pac = new FakePac();

        var r = Apply(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") }, After = AfterOf(Installed("Solo", "1.0.0.0")) }, pac, "--require-reviews", "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.EndsWith("Solo.zip", Assert.Single(pac.Calls)[5]);
    }

    [Fact]
    public void With_require_reviews_a_plan_that_skips_every_zip_passes_with_no_docs_plans()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        SmallZip(Folder, "Kept");
        MakePlan(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") } });
        var pac = new FakePac();

        var r = Apply(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept", "1.0.0.0") } }, pac, "--require-reviews", "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Contains("applied: Nothing was imported: every solution was skipped.", r.Out);
        Assert.Empty(pac.Calls);
    }

    // ---- --require-reviews and the deploy-hash link (M83) ------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void With_require_reviews_a_zip_linked_by_deploy_hash_plans_and_imports(bool apply)
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac { UnpackFrom = ReviewedUnpack(SeedZipPath) };

        var r = Apply(SeedAfterOn(), pac, ["--require-reviews", .. apply ? new[] { "--apply" } : []]);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Equal(apply ? 1 : 0, pac.Calls.Count);
        Assert.Equal("--packagetype Managed", string.Join(' ', Assert.Single(pac.Unpacks)[^2..]));
        UnpacksGone(pac, r);
    }

    [Fact]
    public void With_require_reviews_a_verify_md_linked_by_deploy_hash_and_a_security_solution_md_linked_directly_cover()
    {
        SeedZip(Folder);
        MakePlan();
        var review = ReviewedUnpack(SeedZipPath);
        File.WriteAllText(
            Path.Combine(_root, "docs", "plans", "m", "security-solution.md"),
            $"state: ready\nplan-hash: {Core.Plans.PlanDocument.Parse(ReviewedPlanText).BodyHash}\ninput: folder/PowerOpsSeed.zip\nsolution-hash: {Core.Readers.Manifest.Read(SeedZipPath).Digest}\n\n# Report\n");
        var pac = new FakePac { UnpackFrom = review };

        var r = Apply(SeedAfterOn(), pac, "--require-reviews", "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Single(pac.Calls);
    }

    [Fact]
    public void Unpacks_do_not_shift_the_import_numbers()
    {
        Chain();
        MakePlan(new FakeTarget());
        var reviews = new[] { "A", "B", "C" }.ToDictionary(n => n, n => ReviewedUnpack(Path.Combine(Folder, n + ".zip"), plan: n));
        var seen = new List<int>();
        var pac = new FakePac { UnpackFrom = reviews["A"], Answer = n => { seen.Add(n); return null; } };

        var r = Apply(new FakeTarget { After = ChainAfter() }, pac, "--apply");

        // A's unpack is copied for every zip, so B and C warn; the imports still run as 0, 1, 2.
        Assert.Equal(0, r.Code);
        Assert.Equal([0, 1, 2], seen);
        Assert.Equal(3, pac.Unpacks.Count);
        Assert.Equal(["A.zip", "B.zip", "C.zip"], pac.Unpacks.Select(u => Path.GetFileName(u[3])));
        UnpacksGone(pac, r);
    }

    public static TheoryData<string, string> RefusedUnpacks => new()
    {
        { "another-folder", "verify.md's solution-hash is not the digest of this zip's unpack, so it reviewed another folder; security-solution.md's solution-hash is not the digest of this zip's unpack, so it reviewed another folder" },
        { "not-found", "verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac was not found; security-solution.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac was not found" },
        { "exit", "verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac solution unpack exited 1; security-solution.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac solution unpack exited 1" },
        { "timeout", "verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac solution unpack timed out; security-solution.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac solution unpack timed out" },
        { "empty", "verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: the unpacked folder could not be read; security-solution.md records this zip as its deploy-hash, but the zip's unpack could not be checked: the unpacked folder could not be read" },
    };

    [Theory]
    [MemberData(nameof(RefusedUnpacks))]
    public void With_require_reviews_an_unpack_that_does_not_link_the_zip_refuses_with_no_import(string kind, string why)
    {
        SeedZip(Folder);
        MakePlan();
        var review = ReviewedUnpack(SeedZipPath);
        var other = Scratch("other");
        File.WriteAllText(Path.Combine(other, "Solution.xml"), "<other />");

        foreach (var apply in new[] { false, true })
        {
            var pac = new FakePac
            {
                UnpackFrom = kind switch { "another-folder" => other, "empty" => null, _ => review },
                UnpackAnswer = _ => kind switch
                {
                    "not-found" => new PacResult(false, false, 0, ""),
                    "exit" => new PacResult(true, false, 1, FakePac.UnpackOutput, FakePac.UnpackOutput),
                    "timeout" => new PacResult(true, true, 0, ""),
                    _ => null,
                },
            };

            var r = Apply(SeedAfterOn(), pac, ["--require-reviews", "--format", "json", .. apply ? new[] { "--apply" } : []]);

            Assert.Equal(1, r.Code);
            var doc = Json(r);
            Assert.Equal("refused", (string?)doc["status"]);
            Assert.Equal($"no review covers PowerOpsSeed.zip: docs/plans/m: {why}; nothing was imported.", (string?)doc["message"]);
            Assert.StartsWith($"warning: PowerOpsSeed.zip: docs/plans/m: {why}\n", r.Err);
            Assert.Empty(pac.Calls);
            Assert.Single(pac.Unpacks);
            UnpacksGone(pac, r);
        }
    }

    // The plan file's actions are not covered by its hash: the zips checked are those the fresh plan imports.
    [Fact]
    public void With_require_reviews_a_plan_file_edited_to_skip_the_uncovered_zip_is_still_refused()
    {
        var a = SmallZip(Folder, "A");
        SmallZip(Folder, "B");
        MakePlan(new FakeTarget());
        Reviewed(a);
        EditPlanFile(doc =>
        {
            foreach (var s in doc["solutions"]!.AsArray().Where(s => (string?)s!["zip"] == "B.zip"))
            {
                s!["action"] = "skip";
            }
        });
        var pac = new FakePac();

        var r = Apply(new FakeTarget(), pac, "--require-reviews", "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        Assert.Equal("refused", (string?)Json(r)["status"]);
        Assert.Equal($"no review covers B.zip: {NoneRecords}; nothing was imported.", (string?)Json(r)["message"]);
        Assert.Equal($"warning: B.zip: {NoneRecords}\n", r.Err);
        Assert.Empty(pac.Calls);
    }

    // ---- the plan file's folder, the tool, what is never printed -----------------------------------------------

    [Fact]
    public void A_plan_file_given_as_a_bare_file_name_reads_the_zips_from_the_current_folder()
    {
        SeedZip(Folder);
        MakePlan();
        var old = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Folder;
        try
        {
            var (o, e) = (new StringWriter(), new StringWriter());
            var code = Program.Run(["deploy", "apply", "powerops-plan.json", "--target", TargetValue], o, e, new FakePac().Run, null, SeedTarget().Read, deployAgents: new FakeAgents().Read);

            Assert.Equal(0, code);
            Assert.EndsWith("dry run: would import 1 solution\n", o.ToString().ReplaceLineEndings("\n"));
        }
        finally
        {
            Environment.CurrentDirectory = old;
        }
    }

    [Fact]
    public void The_report_tool_is_the_version_powerops_prints()
    {
        SeedZip(Folder);
        MakePlan();

        Assert.Equal(AnalyzeCommand.ToolVersion(), (string?)Json(Apply(SeedTarget(), new FakePac(), "--format", "json"))["tool"]);
    }

    [Fact]
    public void The_environment_id_the_target_value_the_folder_and_settings_values_appear_in_no_output_and_no_file()
    {
        var settings = File.ReadAllText(ValidSettings).Replace("fake-connection-id", "SENTINEL-CONN-7f3a").Replace("\"Value\": \"Open\"", "\"Value\": \"SENTINEL-VALUE-9b2c\"");
        Assert.Contains("SENTINEL-VALUE-9b2c", settings);
        SeedZip(Folder, settingsText: settings);
        MakePlan();
        var all = new StringBuilder();
        foreach (var format in new[] { "text", "json" })
        {
            foreach (var apply in new[] { false, true })
            {
                var outFile = Path.Combine(Out, $"{format}-{apply}.out");
                string[] extra = apply ? ["--apply"] : [];
                var a = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), [.. extra, "--format", format]);
                var b = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), [.. extra, "--format", format, "--out", outFile]);
                all.Append(a.Out).Append(a.Err).Append(b.Out).Append(b.Err).Append(File.ReadAllText(outFile));
            }
        }

        var failing = new FakePac { Answer = _ => new PacResult(true, false, 1, "") };
        var failed = Apply(SeedTarget(), failing, "--apply");
        var mismatch = Apply(new FakeTarget { Name = "Other" }, new FakePac(), "--apply");
        var read = Apply(new FakeTarget { Fail = new DriftReadException("the read failed") }, new FakePac(), "--apply");
        var usage = Run(SeedTarget(), new FakePac(), "deploy", "apply", PlanFile);
        foreach (var r in new[] { failed, mismatch, read, usage })
        {
            all.Append(r.Out).Append(r.Err);
        }

        foreach (var secret in new[] { TargetIdText, "secret-host-7f3a", _root, "SENTINEL" })
        {
            Assert.DoesNotContain(secret, all.ToString());
        }
    }

    // ---- goldens -----------------------------------------------------------------------------------------------

    private static string Scrub(string text) =>
        Regex.Replace(
            Regex.Replace(Regex.Replace(text, "[0-9a-f]{64}", new string('0', 64)), "\"tool\": \"[^\"]*\"", "\"tool\": \"<tool>\""),
            "(\"appliedAt\": \"|Applied at: )[^\"\n]*", "${1}2000-01-01T00:00:00Z");

    [Theory]
    [InlineData("text", "deploy-apply-dry-run.txt")]
    [InlineData("json", "deploy-apply-dry-run.json")]
    public void The_seed_dry_run_matches_the_golden(string format, string golden)
    {
        SeedZip(Folder);
        MakePlan();

        var r = Apply(SeedTarget(), new FakePac(), "--format", format);

        Assert.Equal(0, r.Code);
        Golden.Verify(Scrub(r.Out), golden);
    }

    [Theory]
    [InlineData("text", "deploy-apply.txt")]
    [InlineData("json", "deploy-apply.json")]
    public void The_seed_apply_matches_the_golden(string format, string golden)
    {
        SeedZip(Folder);
        MakePlan();

        var r = Apply(new FakeTarget { Snaps = SeedTarget().Snaps, After = SeedAfter("on") }, new FakePac(), "--apply", "--format", format);

        Assert.Equal(0, r.Code);
        Golden.Verify(Scrub(r.Out), golden);
    }

    [Fact]
    public void A_hash_mismatch_matches_the_golden()
    {
        SeedZip(Folder);
        MakePlan();
        var settings = Path.Combine(Folder, "PowerOpsSeed.settings.json");
        File.WriteAllBytes(settings, [.. File.ReadAllBytes(settings), (byte)'\n']);

        var r = Apply(SeedTarget(), new FakePac(), "--apply", "--format", "json");

        Assert.Equal(1, r.Code);
        Golden.Verify(Scrub(r.Out), "deploy-apply-refused.json");
    }

    [Fact]
    public void A_failed_import_matches_the_golden()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, false, 1, "boom") : null };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Golden.Verify(Scrub(r.Out), "deploy-apply-failed.json");
    }

    [Fact]
    public void A_timed_out_import_matches_the_golden()
    {
        Chain();
        MakePlan(new FakeTarget());
        var pac = new FakePac { Answer = n => n == 1 ? new PacResult(true, true, 0, "") : null };

        var r = Apply(new FakeTarget(), pac, "--apply", "--format", "json");

        Assert.Equal(3, r.Code);
        Golden.Verify(Scrub(r.Out), "deploy-apply-timeout.json");
    }
}