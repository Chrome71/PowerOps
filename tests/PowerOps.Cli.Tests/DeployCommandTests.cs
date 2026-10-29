using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class DeployCommandTests : IDisposable
{
    private const string TargetValue = "https://secret-host-7f3a.crm.dynamics.com/";
    private static readonly Guid TargetId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static readonly string Seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1");
    private static readonly string ValidSettings = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "settings", "powerops-seed-v1.valid.json");
    private static readonly string TestSnapshot = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-test.json");

    private readonly string _root = Directory.CreateTempSubdirectory("powerops-deploy-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    // The deploy folder, a staging folder for files that are copied in, and a folder for --save and --out files.
    private string Folder => Dir("folder");

    private string Stage => Dir("stage");

    private string Out => Dir("out");

    private string Scratch(string prefix) => Dir($"{prefix}-{Guid.NewGuid():N}");

    private string Dir(string name) => Directory.CreateDirectory(Path.Combine(_root, name)).FullName;

    private sealed record Call(string Target, string[] Solutions, string[] Dependencies);

    private sealed class FakeTarget
    {
        public Dictionary<string, Snapshot> Snaps { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string?> Deps { get; init; } = [];

        public Exception? Fail { get; init; }

        public Guid Id { get; init; } = TargetId;

        public List<Call> Calls { get; } = [];

        public DeployTarget Read(string target, IReadOnlyList<string> solutions, IReadOnlyList<string> dependencies)
        {
            Calls.Add(new Call(target, [.. solutions], [.. dependencies]));
            return Fail is not null
                ? throw Fail
                : new DeployTarget(
                    "Contoso Test", Id,
                    [.. solutions.Select(s => Snaps.TryGetValue(s, out var snap) ? snap : new Snapshot("Contoso Test", s, false, null, null, true, [], [], [], []))],
                    dependencies.ToDictionary(d => d, d => Deps.GetValueOrDefault(d)));
        }
    }

    private sealed record Outcome(int Code, string Out, string Err);

    private static Outcome Run(FakeTarget fake, params string[] args)
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        var code = Program.Run(args, o, e, (_, _) => throw new Xunit.Sdk.XunitException("pac must not be called"), null, fake.Read);
        return new Outcome(code, o.ToString().ReplaceLineEndings("\n"), e.ToString().ReplaceLineEndings("\n"));
    }

    private Outcome Plan(FakeTarget fake, params string[] extra) => Run(fake, ["deploy", "plan", Folder, "--target", TargetValue, .. extra]);

    private static JsonObject Json(Outcome r) => JsonNode.Parse(r.Out)!.AsObject();

    private static string HashOf(Outcome r) => (string)Json(r)["hash"]!;

    // ---- inputs ------------------------------------------------------------------------------------------------

    private static string Manifest(string name, string version, bool managed, params string[] requires) =>
        $"""
        <ImportExportXml><SolutionManifest>
          <UniqueName>{name}</UniqueName><Version>{version}</Version><Managed>{(managed ? 1 : 0)}</Managed>
          <Publisher><UniqueName>pub</UniqueName><CustomizationPrefix>pub</CustomizationPrefix></Publisher>
          <MissingDependencies>{string.Concat(requires.Select(r => $"<MissingDependency><Required type=\"1\" schemaName=\"x\" solution=\"{r}\" /><Dependent type=\"29\" id=\"{{AA}}\" /></MissingDependency>"))}</MissingDependencies>
        </SolutionManifest></ImportExportXml>
        """;

    /// <summary>Writes a small solution zip. <paramref name="name"/> is also its unique name and the zip's file name.</summary>
    private string SmallZip(string folder, string name, string version = "1.0.0.0", bool managed = true, string[]? requires = null, string? unique = null, params (string Path, string Text)[] files)
    {
        var dir = Scratch("small");
        Directory.CreateDirectory(Path.Combine(dir, "Other"));
        File.WriteAllText(Path.Combine(dir, "Other", "Solution.xml"), Manifest(unique ?? name, version, managed, requires ?? []));
        foreach (var (path, text) in files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dir, path))!);
            File.WriteAllText(Path.Combine(dir, path), text);
        }

        var zip = Path.Combine(folder, name + ".zip");
        SolutionZip.Create(dir, zip);
        return zip;
    }

    // The seed as a managed zip one version higher (1.0.0.1), with or without its valid settings file.
    private void SeedZip(string folder, bool settings = true, string name = "PowerOpsSeed")
    {
        var dir = Path.Combine(Scratch("seed"), "v1");
        foreach (var f in Directory.EnumerateFiles(Seed, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(dir, Path.GetRelativePath(Seed, f));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(f, to);
        }

        var xml = Path.Combine(dir, "Other", "Solution.xml");
        var text = File.ReadAllText(xml);
        var patched = text.Replace("<Managed>0</Managed>", "<Managed>1</Managed>").Replace("<Version>1.0.0.0</Version>", "<Version>1.0.0.1</Version>");
        Assert.NotEqual(text, patched);
        File.WriteAllText(xml, patched);
        SolutionZip.Create(dir, Path.Combine(folder, name + ".zip"));
        if (settings)
        {
            File.Copy(ValidSettings, Path.Combine(folder, name + ".settings.json"));
        }
    }

    private static FakeTarget SeedTarget() => new() { Snaps = { ["PowerOpsSeed"] = SnapshotFile.Load(TestSnapshot) } };

    // ---- exit 0 and 1 ------------------------------------------------------------------------------------------

    [Fact]
    public void A_clean_plan_exits_0_and_says_plan_ok()
    {
        SmallZip(Folder, "Solo");

        var r = Plan(new FakeTarget());

        Assert.Equal(0, r.Code);
        Assert.EndsWith("1 Solo (Solo.zip): install not installed -> 1.0.0.0, managed\nplan ok\n", r.Out);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void A_blocking_item_exits_1_and_still_writes_the_report_and_the_plan_file()
    {
        const string kind = "cycle";
        var fake = new FakeTarget();
        SmallZip(Folder, "A", requires: ["B (1.0)"]);
        SmallZip(Folder, "B", requires: ["A (1.0)"]);

        var save = Path.Combine(Out, "plan.json");
        var r = Plan(fake, "--format", "json", "--save", save);

        Assert.Equal(1, r.Code);
        Assert.Contains(Json(r)["items"]!.AsArray(), i => (string?)i!["kind"] == kind && (string?)i["severity"] == "block");
        Assert.True((bool)Json(r)["blocked"]!);
        Assert.Equal(r.Out, File.ReadAllText(save));
    }

    [Fact]
    public void An_upper_case_ZIP_extension_is_read_and_its_settings_file_is_matched_by_the_name_without_the_extension()
    {
        SeedZip(Folder);
        File.Move(Path.Combine(Folder, "PowerOpsSeed.zip"), Path.Combine(Folder, "PowerOpsSeed.ZIP"));

        var r = Plan(SeedTarget(), "--format", "json");

        Assert.Equal(0, r.Code);
        var s = Json(r)["solutions"]![0]!;
        Assert.Equal(("PowerOpsSeed.ZIP", "PowerOpsSeed.settings.json"), (s["zip"]!.ToString(), s["settings"]!.ToString()));
    }

    // ---- exit 2 ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("missing-folder", "the folder argument is not an existing folder")]
    [InlineData("no-zip", "the folder holds no zip file")]
    [InlineData("space-in-name", "a zip or settings file name may hold only")]
    [InlineData("name-with-a-line-break", "a zip or settings file name may hold only")]
    [InlineData("settings-without-zip", "no zip file has this settings file name")]
    [InlineData("settings-differs-in-case", "differs only in case from Case.settings.json, the settings file name of Case.zip")]
    [InlineData("same-unique-name", "hold the same solution")]
    [InlineData("no-target", "--target is required")]
    [InlineData("save-is-a-folder", "--save names an existing folder")]
    [InlineData("out-is-a-folder", "--out names an existing folder")]
    [InlineData("symbolic-link", "symbolic links are not read")]
    public void A_usage_error_exits_2_before_the_target_is_read(string problem, string message)
    {
        string[] args = ["deploy", "plan", Folder, "--target", TargetValue];
        switch (problem)
        {
            case "missing-folder":
                args = ["deploy", "plan", Path.Combine(_root, "nope"), "--target", TargetValue];
                break;
            case "no-zip":
                File.WriteAllText(Path.Combine(Folder, "readme.txt"), "x");
                break;
            case "space-in-name":
                SmallZip(Folder, "ok");
                File.WriteAllText(Path.Combine(Folder, "my solution.zip"), "x");
                break;
            case "name-with-a-line-break":
                try
                {
                    File.WriteAllText(Path.Combine(Folder, "bad\n::error::x.zip"), "x");
                }
                catch (Exception e) when (e is IOException or ArgumentException)
                {
                    Assert.Skip("This file system has no file names with a line break.");
                }

                break;
            case "settings-without-zip":
                SmallZip(Folder, "ok");
                File.WriteAllText(Path.Combine(Folder, "typo.settings.json"), "{}");
                break;
            case "settings-differs-in-case":
                SmallZip(Folder, "Case");
                File.WriteAllText(Path.Combine(Folder, "Case.SETTINGS.json"), "{}");
                break;
            case "same-unique-name":
                SmallZip(Folder, "One", unique: "Same");
                SmallZip(Folder, "Two", unique: "same");
                break;
            case "no-target":
                SmallZip(Folder, "ok");
                args = ["deploy", "plan", Folder];
                break;
            case "save-is-a-folder":
                SmallZip(Folder, "ok");
                args = [.. args, "--save", Out];
                break;
            case "out-is-a-folder":
                SmallZip(Folder, "ok");
                args = [.. args, "--out", Out];
                break;
            default:
                var real = SmallZip(Stage, "Real");
                try
                {
                    File.CreateSymbolicLink(Path.Combine(Folder, "Link.zip"), real);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Assert.Skip("Symbolic links are not available here.");
                }

                break;
        }

        var fake = new FakeTarget();
        var r = Run(fake, args);

        Assert.Equal(2, r.Code);
        Assert.Contains(message, r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(fake.Calls);
        Assert.DoesNotContain(_root, r.Err);
        Assert.All(r.Err.Split('\n'), l => Assert.DoesNotMatch("^::", l));
    }

    // M28: --save and --out are not written through a link; the line names the option and no path.
    [Theory]
    [InlineData("--save")]
    [InlineData("--out")]
    public void A_link_as_save_or_out_exits_2_and_the_file_behind_it_is_not_touched(string option)
    {
        SeedZip(Folder);
        var victim = Path.Combine(_root, "victim.txt");
        File.WriteAllText(victim, "victim");
        var link = Path.Combine(Out, "plan.json");
        Links.Create(link, victim);
        var zips = Directory.GetFiles(Folder).ToDictionary(f => f, File.ReadAllBytes);

        var fake = new FakeTarget();
        var r = Plan(fake, option, link);

        Assert.Equal(2, r.Code);
        Assert.Equal($"error: {option} is a symbolic link; symbolic links are not written.\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(fake.Calls);
        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.Equal(victim, new FileInfo(link).LinkTarget);
        Assert.All(zips, z => Assert.Equal(z.Value, File.ReadAllBytes(z.Key)));
    }

    // M33 U-2: the zips and settings files of the folder are inputs; the refusal comes before the target is read.
    [Theory]
    [InlineData("--save", "PowerOpsSeed.zip")]
    [InlineData("--save", "PowerOpsSeed.settings.json")]
    [InlineData("--out", "PowerOpsSeed.zip")]
    [InlineData("--out", "PowerOpsSeed.settings.json")]
    public void Save_or_out_over_a_zip_or_settings_file_of_the_folder_exits_2_and_leaves_the_file(string option, string name)
    {
        SeedZip(Folder);
        var input = Path.Combine(Folder, name);
        var before = File.ReadAllBytes(input);

        var fake = new FakeTarget();
        var r = Plan(fake, option, input);

        Assert.Equal(2, r.Code);
        Assert.Equal($"error: {option} is an input of this command; it is not overwritten.\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(fake.Calls);
        Assert.Equal(before, File.ReadAllBytes(input));
    }

    [Fact]
    public void Save_and_out_into_the_folder_are_still_written()
    {
        SeedZip(Folder);
        var (save, outFile) = (Path.Combine(Folder, "powerops-plan.json"), Path.Combine(Folder, "report.txt"));

        var r = Plan(SeedTarget(), "--format", "json", "--save", save, "--out", outFile);

        Assert.Equal(0, r.Code);
        Assert.True(File.Exists(save));
        Assert.True(File.Exists(outFile));
    }

    [Fact]
    public void Save_run_again_replaces_its_own_file()
    {
        SeedZip(Folder);
        var save = Path.Combine(Folder, "plan.json");

        var first = Plan(SeedTarget(), "--format", "json", "--save", save);
        var second = Plan(SeedTarget(), "--format", "json", "--save", save);

        Assert.Equal(0, first.Code);
        Assert.Equal(0, second.Code);
        Assert.Equal(second.Out, File.ReadAllText(save));
        Assert.Empty(Directory.GetFiles(Folder, ".powerops-*.tmp"));
    }

    // ---- exit 3 ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("not-a-zip", "Bad.zip")]
    [InlineData("settings-not-json", "Sol.settings.json")]
    [InlineData("settings-over-1-mb", "Sol.settings.json")]
    [InlineData("bad-version", "Sol.zip")]
    public void An_unreadable_input_exits_3_naming_the_file_with_no_report_and_before_the_target_is_read(string problem, string file)
    {
        switch (problem)
        {
            case "not-a-zip":
                File.WriteAllText(Path.Combine(Folder, "Bad.zip"), "this is not a zip");
                break;
            case "bad-version":
                SmallZip(Folder, "Sol", "1.0");
                break;
            default:
                SmallZip(Folder, "Sol");
                File.WriteAllText(
                    Path.Combine(Folder, "Sol.settings.json"),
                    problem == "settings-not-json" ? "{ nope" : "{}" + new string(' ', 1024 * 1024));
                break;
        }

        var fake = new FakeTarget();
        var save = Path.Combine(Out, "plan.json");
        var r = Plan(fake, "--save", save);

        Assert.Equal(3, r.Code);
        Assert.StartsWith($"error: {file}", r.Err);
        Assert.Equal("", r.Out);
        Assert.Empty(fake.Calls);
        Assert.False(File.Exists(save));
        Assert.DoesNotContain(_root, r.Err);
        Assert.DoesNotContain(TargetValue, r.Err);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void A_failed_target_read_prefixes_the_message_forwards_the_tool_text_to_stderr_and_writes_nothing(int exitCode)
    {
        SmallZip(Folder, "Sol");
        var fake = new FakeTarget { Fail = new DriftReadException("the read failed", "tool line 1\ntool line 2", exitCode) };
        var (save, outFile) = (Path.Combine(Out, "plan.json"), Path.Combine(Out, "report.md"));

        var r = Plan(fake, "--save", save, "--out", outFile);

        Assert.Equal(exitCode, r.Code);
        Assert.Equal("error: --target: the read failed\n  | tool line 1\n  | tool line 2\n", r.Err);
        Assert.Equal("", r.Out);
        Assert.False(File.Exists(save));
        Assert.False(File.Exists(outFile));
    }

    // ---- what the target read gets and what is written ---------------------------------------------------------

    [Fact]
    public void The_target_read_gets_the_target_unchanged_the_folders_solutions_and_the_dependencies_not_in_the_folder()
    {
        SmallZip(Folder, "B");
        SmallZip(Folder, "A", requires: ["Lib (1.0)", "B (1.0)", "Lib (2.0)"]);
        var fake = new FakeTarget { Deps = { ["Lib"] = "3.0.0.0" } };

        var r = Plan(fake, "--format", "json");

        Assert.Equal(0, r.Code);
        var call = Assert.Single(fake.Calls);
        Assert.Equal(TargetValue, call.Target);
        Assert.Equal(["A", "B"], call.Solutions);
        Assert.Equal(["Lib"], call.Dependencies);
        Assert.Equal("lib", (string?)Json(r)["inputs"]!["dependencies"]![0]!["name"]);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    public void Save_writes_the_json_report_whatever_the_format_is(string format)
    {
        SeedZip(Folder);
        var save = Path.Combine(Out, "plan.json");

        var r = Plan(SeedTarget(), "--format", format, "--save", save);
        var asJson = Plan(SeedTarget(), "--format", "json");

        Assert.Equal(0, r.Code);
        Assert.Equal(asJson.Out, File.ReadAllText(save));
        Assert.Equal(format == "json", r.Out == asJson.Out);
    }

    [Fact]
    public void Out_writes_the_report_there_and_leaves_stdout_empty()
    {
        SeedZip(Folder);
        var file = Path.Combine(Out, "report.md");

        var r = Plan(SeedTarget(), "--format", "markdown", "--out", file);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Out);
        Assert.StartsWith("## PowerOps deploy plan", File.ReadAllText(file));
    }

    [Fact]
    public void The_tool_is_the_version_powerops_prints()
    {
        SmallZip(Folder, "Solo");

        Assert.Equal(AnalyzeCommand.ToolVersion(), (string?)Json(Plan(new FakeTarget(), "--format", "json"))["tool"]);
    }

    [Fact]
    public void The_target_value_and_the_folder_path_appear_in_no_output_and_no_file()
    {
        SeedZip(Folder);
        var all = new StringBuilder();
        foreach (var format in new[] { "text", "json", "markdown" })
        {
            var (save, outFile) = (Path.Combine(Out, $"{format}.json"), Path.Combine(Out, $"{format}.out"));
            var r = Plan(SeedTarget(), "--format", format, "--save", save);
            var file = Plan(SeedTarget(), "--format", format, "--out", outFile);
            all.Append(r.Out).Append(r.Err).Append(file.Out).Append(file.Err).Append(File.ReadAllText(save)).Append(File.ReadAllText(outFile));
        }

        var failed = Plan(new FakeTarget { Fail = new DriftReadException("the read failed") });
        var usage = Run(new FakeTarget(), "deploy", "plan", Folder);
        all.Append(failed.Out).Append(failed.Err).Append(usage.Out).Append(usage.Err);

        Assert.DoesNotContain("secret-host-7f3a", all.ToString());
        Assert.DoesNotContain(_root, all.ToString());
    }

    [Fact]
    public void A_connection_id_and_a_variable_value_from_the_settings_file_appear_in_no_output_and_no_file()
    {
        SeedZip(Folder);
        var settings = File.ReadAllText(ValidSettings).Replace("fake-connection-id", "SENTINEL-CONN-7f3a").Replace("\"Value\": \"Open\"", "\"Value\": \"SENTINEL-VALUE-9b2c\"");
        Assert.Contains("SENTINEL-VALUE-9b2c", settings);
        File.WriteAllText(Path.Combine(Folder, "PowerOpsSeed.settings.json"), settings);
        var all = new StringBuilder();
        foreach (var format in new[] { "text", "json", "markdown" })
        {
            var save = Path.Combine(Out, $"{format}.json");
            var r = Plan(SeedTarget(), "--format", format, "--save", save);
            Assert.Equal(0, r.Code);
            all.Append(r.Out).Append(r.Err).Append(File.ReadAllText(save));
        }

        Assert.DoesNotContain("SENTINEL", all.ToString());
    }

    // ---- the hash ----------------------------------------------------------------------------------------------

    [Fact]
    public void Two_runs_over_the_same_files_give_the_same_hash()
    {
        SeedZip(Folder);

        Assert.Equal(HashOf(Plan(SeedTarget(), "--format", "json")), HashOf(Plan(SeedTarget(), "--format", "json")));
    }

    [Fact]
    public void Files_created_in_the_other_order_give_the_same_hash()
    {
        SmallZip(Stage, "A");
        SmallZip(Stage, "B");
        var (one, two) = (Dir("one"), Dir("two"));
        foreach (var n in new[] { "A", "B" })
        {
            File.Copy(Path.Combine(Stage, n + ".zip"), Path.Combine(one, n + ".zip"));
        }

        foreach (var n in new[] { "B", "A" })
        {
            File.Copy(Path.Combine(Stage, n + ".zip"), Path.Combine(two, n + ".zip"));
        }

        static string Hash(string folder) => HashOf(Run(new FakeTarget(), "deploy", "plan", folder, "--target", TargetValue, "--format", "json"));

        Assert.Equal(Hash(one), Hash(two));
    }

    [Fact]
    public void A_changed_settings_byte_changes_the_hash_and_inputs_names_the_new_hash_of_that_file()
    {
        SeedZip(Folder);
        var before = Json(Plan(SeedTarget(), "--format", "json"));
        var path = Path.Combine(Folder, "PowerOpsSeed.settings.json");
        File.WriteAllBytes(path, [.. File.ReadAllBytes(path), (byte)'\n']);

        var after = Json(Plan(SeedTarget(), "--format", "json"));

        Assert.NotEqual((string?)before["hash"], (string?)after["hash"]);
        var (b, a) = (before["inputs"]!["zips"]![0]!, after["inputs"]!["zips"]![0]!);
        Assert.NotEqual((string?)b["settingsSha256"], (string?)a["settingsSha256"]);
        Assert.Equal(DeployPlan.Sha256Hex(File.ReadAllBytes(path)), (string?)a["settingsSha256"]);
        Assert.Equal((string?)b["zipSha256"], (string?)a["zipSha256"]);
        Assert.Equal((string?)b["snapshotSha256"], (string?)a["snapshotSha256"]);
    }

    [Fact]
    public void A_different_target_id_changes_the_hash()
    {
        SmallZip(Folder, "Solo");

        Assert.NotEqual(
            HashOf(Plan(new FakeTarget(), "--format", "json")),
            HashOf(Plan(new FakeTarget { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002") }, "--format", "json")));
    }

    // ---- goldens -----------------------------------------------------------------------------------------------

    // The hashes depend on zip bytes built at test time and the tool on the build: both are replaced (the hash by 64 zeros, which the schema's pattern still accepts).
    private static string Scrub(string text) =>
        Regex.Replace(Regex.Replace(text, "[0-9a-f]{64}", new string('0', 64)), "\"tool\": \"[^\"]*\"", "\"tool\": \"<tool>\"");

    [Theory]
    [InlineData("text", "deploy-plan-seed.txt")]
    [InlineData("json", "deploy-plan-seed.json")]
    [InlineData("markdown", "deploy-plan-seed.md")]
    public void The_seed_upgrade_matches_the_golden(string format, string golden)
    {
        SeedZip(Folder);

        var r = Plan(SeedTarget(), "--format", format);

        Assert.Equal(0, r.Code);
        Golden.Verify(Scrub(r.Out), golden);
    }

    [Fact]
    public void A_blocked_plan_matches_the_golden()
    {
        SmallZip(Folder, "CycleA", requires: ["CycleB (1.0)"]);
        SmallZip(Folder, "CycleB", requires: ["CycleA (1.0)"]);
        SmallZip(Folder, "NeedsDep", requires: ["Absent (1.0)"]);
        SmallZip(Folder, "Conflict", "2.0.0.0", managed: true);
        SeedZip(Folder, settings: false);
        var fake = new FakeTarget { Snaps = { ["Conflict"] = new Snapshot("Contoso Test", "Conflict", true, "1.0.0.0", false, true, [], [], [], []) } };

        var r = Plan(fake, "--format", "json");

        Assert.Equal(1, r.Code);
        Golden.Verify(Scrub(r.Out), "deploy-plan-blocked.json");
    }
}