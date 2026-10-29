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

    private static Dictionary<string, Snapshot> SeedAfter(string state)
    {
        var snap = SnapshotFile.Load(TestSnapshot);
        return new(StringComparer.OrdinalIgnoreCase) { ["PowerOpsSeed"] = snap with { Flows = [.. snap.Flows.Select(f => f with { State = state })] } };
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
        Assert.Equal("", r.Out);
        Assert.Equal(0, fake.Calls);
        Assert.Empty(pac.Calls);
        Assert.DoesNotContain(_root, r.Err);
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
    [InlineData("not-json")]
    [InlineData("not-an-object")]
    [InlineData("no-hash")]
    [InlineData("bad-hash")]
    [InlineData("empty-target")]
    [InlineData("no-inputs")]
    [InlineData("unsafe-zip-name")]
    [InlineData("over-1-mb")]
    [InlineData("symbolic-link")]
    public void An_unreadable_plan_file_exits_3_naming_the_problem_before_the_target_is_read(string problem)
    {
        SeedZip(Folder);
        MakePlan();
        switch (problem)
        {
            case "not-json":
                File.WriteAllText(PlanFile, "{ nope");
                break;
            case "not-an-object":
                File.WriteAllText(PlanFile, "[]");
                break;
            case "no-hash":
                EditPlanFile(d => d.Remove("hash"));
                break;
            case "bad-hash":
                EditPlanFile(d => d["hash"] = "SECRET-VALUE");
                break;
            case "empty-target":
                EditPlanFile(d => d["target"] = "");
                break;
            case "no-inputs":
                EditPlanFile(d => d.Remove("inputs"));
                break;
            case "unsafe-zip-name":
                EditPlanFile(d => d["inputs"]!["zips"]![0]!["zip"] = "x\n::error::y.zip");
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

    [Theory]
    [InlineData("tool")]
    [InlineData("schemaVersion")]
    public void A_plan_made_by_another_tool_or_schema_version_is_refused_before_the_target_is_read(string field)
    {
        SeedZip(Folder);
        MakePlan();
        EditPlanFile(d => d[field] = field == "tool" ? "9.9.9\n::error::x" : 99);

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

        var r = Apply(SeedTarget(), pac, "--apply");

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

        var r = Apply(target, pac, "--apply", "--format", "json");

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
        Assert.Equal(["Kept:skipped"], Statuses(doc));
        Assert.Empty(doc["flows"]!.AsArray());
        Assert.Empty(pac.Calls);
        Assert.Equal(2, target.Calls);
    }

    [Fact]
    public void An_apply_prints_the_target_line_and_one_line_before_and_after_each_import_then_the_report()
    {
        Chain();
        MakePlan(new FakeTarget());

        var r = Apply(new FakeTarget(), new FakePac(), "--apply");

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.StartsWith("environment: Contoso Test\nimporting 1 A (A.zip)\nimported A\nimporting 2 B (B.zip)\nimported B\nimporting 3 C (C.zip)\nimported C\nTarget: Contoso Test\n", r.Out);
        Assert.EndsWith("3 C (C.zip): install, ok\napplied\n", r.Out);
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

    [Fact]
    public void A_timed_out_import_exits_3_with_the_timeout_text_and_forwards_nothing()
    {
        SeedZip(Folder);
        MakePlan();
        var pac = new FakePac { Answer = _ => new PacResult(true, true, 0, "") };

        var r = Apply(SeedTarget(), pac, "--apply");

        Assert.Equal(3, r.Code);
        Assert.Equal("", r.Err);
        Assert.Contains("failed: The import timed out after 60 minutes. It may still be running in the environment: check the solution's import history there before running again.", r.Out);
        Assert.Contains("PowerOpsSeed (PowerOpsSeed.zip): upgrade, failed\n", r.Out);
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
            var code = Program.Run(["deploy", "apply", "powerops-plan.json", "--target", TargetValue], o, e, new FakePac().Run, null, SeedTarget().Read);

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
        Regex.Replace(Regex.Replace(text, "[0-9a-f]{64}", new string('0', 64)), "\"tool\": \"[^\"]*\"", "\"tool\": \"<tool>\"");

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
}