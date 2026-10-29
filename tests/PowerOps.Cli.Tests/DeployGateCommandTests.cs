using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;

namespace PowerOps.Cli.Tests;

[Collection("deploy apply")]
public class DeployGateCommandTests : DeployTestBase
{
    private const string DevId = "bbbbbbbb-0000-0000-0000-000000000002";
    private const string TwinId = "cccccccc-0000-0000-0000-000000000003";
    private const string DevUrl = "https://dev-host-1c2d.crm.dynamics.com";
    private const string TwinUrl = "https://twin-host-9e8f.crm.dynamics.com";
    private const string Prefix = "error: deploy gate: ";
    private const string ReadingGuidance = "The call was blocked. If it is an import, write it as one plain pac solution import command with literal absolute paths and the environment's id or https URL. If it only mentions an import, reword it.";
    private const string ImportGuidance = "The import was blocked. To import a reviewed plan, run powerops deploy apply <plan file> --target <environment id or URL> --apply after the user's yes to that environment. Do not retry this import another way.";

    private string SeedZipPath => Path.Combine(Folder, "PowerOpsSeed.zip");

    private string SeedSettingsPath => Path.Combine(Folder, "PowerOpsSeed.settings.json");

    // ---- environments, calls and runs --------------------------------------------------------------------------

    // Three environments, answered by the value the read gets: Test (the plan's name and id), Dev (another name and id)
    // and a twin (Test's name, another id), each by id and by URL. Every fake records the values it was asked for.
    private static Dictionary<string, FakeTarget> Environments(FakeTarget test)
    {
        var dev = new FakeTarget { Name = "Contoso Dev", Id = Guid.Parse(DevId), Snaps = { ["PowerOpsSeed"] = SnapshotFile.Load(TestSnapshot) with { Environment = "Contoso Dev" } } };
        var twin = new FakeTarget { Id = Guid.Parse(TwinId), Snaps = { ["PowerOpsSeed"] = SnapshotFile.Load(TestSnapshot) } };
        return new()
        {
            [TargetIdText] = test,
            [TargetValue] = test,
            [DevId] = dev,
            [DevUrl] = dev,
            [TwinId] = twin,
            [TwinUrl] = twin,
        };
    }

    private static Dictionary<string, FakeTarget> Seeded() => Environments(SeedTarget());

    private static byte[] Hook(string tool, JsonObject input) => Encoding.UTF8.GetBytes(new JsonObject { ["tool_name"] = tool, ["tool_input"] = input }.ToJsonString());

    // A plain pac solution import, as an agent writes it.
    private static string PacCommand(string env, string zip, string? settings = null, bool stage = false) =>
        $"pac solution import --path '{zip}' --environment {env}{(settings is null ? "" : $" --settings-file '{settings}'")}{(stage ? " --stage-and-upgrade" : "")}";

    // Claude Code's Bash input holds the command and a description.
    private static byte[] Cli(string env, string zip, string? settings = null, bool stage = false, string tool = "Bash") =>
        Hook(tool, new JsonObject { ["command"] = PacCommand(env, zip, settings, stage), ["description"] = "Import the solution" });

    // Codex's PreToolUse input for its Bash tool (0.163.0), with the common fields it sends.
    private byte[] CodexBash(string command) =>
        Encoding.UTF8.GetBytes(new JsonObject
        {
            ["session_id"] = "s",
            ["turn_id"] = "t",
            ["transcript_path"] = null,
            ["cwd"] = Folder,
            ["hook_event_name"] = "PreToolUse",
            ["model"] = "m",
            ["permission_mode"] = "default",
            ["tool_name"] = "Bash",
            ["tool_input"] = new JsonObject { ["command"] = command },
            ["tool_use_id"] = "u",
        }.ToJsonString());

    private static byte[] Mcp(string env, string zip, string? settings = null, bool stage = false, string tool = "mcp__pac-cli__solution_import")
    {
        var input = new JsonObject { ["--path"] = zip, ["--environment"] = env };
        if (settings is not null)
        {
            input["--settings-file"] = settings;
        }

        if (stage)
        {
            input["--stage-and-upgrade"] = "true";
        }

        return Hook(tool, input);
    }

    // The seed plan upgrades a managed zip, so its first import has a settings file and stages.
    private byte[] SeedCall(string env = TargetIdText, bool mcp = false) => mcp ? Mcp(env, SeedZipPath, SeedSettingsPath, stage: true) : Cli(env, SeedZipPath, SeedSettingsPath, stage: true);

    // `during` runs inside the target read, as a slow live read would; `deadline` ends a check that hangs (a test seam, else the 300 seconds).
    private Outcome Gate(Dictionary<string, FakeTarget> environments, byte[]? stdin, FakePac? pac = null, Action? during = null, TimeSpan? deadline = null)
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        DeployTarget Read(string target, IReadOnlyList<string> solutions, IReadOnlyList<string> dependencies)
        {
            during?.Invoke();
            return environments[target].Read(target, solutions, dependencies);
        }

        Func<Stream?> input = () => stdin is null ? null : new MemoryStream(stdin);
        int code;
        if (deadline is null)
        {
            code = Program.Run(["deploy", "gate", PlanFile], o, e, (pac ?? new FakePac()).Run, null, Read, input);
        }
        else
        {
            var root = new RootCommand { DeployCommand.Create(o, e, (pac ?? new FakePac()).Run, Read, input, deadline) };
            code = root.Parse(["deploy", "gate", PlanFile]).Invoke(new InvocationConfiguration { Output = o, Error = e, EnableDefaultExceptionHandler = false });
        }

        return new Outcome(code, o.ToString().ReplaceLineEndings("\n"), e.ToString().ReplaceLineEndings("\n"));
    }

    private static Snapshot Installed(string name, string version = "1.0.0.0") => new("Contoso Test", name, true, version, true, true, [], [], [], []);

    // ---- 4. the environment decides ----------------------------------------------------------------------------

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void One_plan_and_zip_pass_for_the_plans_environment_and_block_for_dev_and_the_twin(bool mcp, bool url)
    {
        SeedZip(Folder);
        MakePlan();
        var (test, dev, twin) = (url ? TargetValue : TargetIdText, url ? DevUrl : DevId, url ? TwinUrl : TwinId);

        foreach (var (env, line) in new[]
        {
            (test, ""),
            (dev, "error: the plan was made for Contoso Test, --target is Contoso Dev; nothing was imported\n"),
            (twin, $"{Prefix}the dry run did not pass: the target environment, or the plan file's hash, differs"),
        })
        {
            var environments = Seeded();
            var r = Gate(environments, SeedCall(env, mcp));

            Assert.Equal(line == "" ? 0 : 2, r.Code);
            Assert.Equal("", r.Out);
            Assert.Contains(line, r.Err);
            Assert.Equal(env, Assert.Single(environments[env].Solutions)[0]);
            Assert.Equal(1, environments.Values.Distinct().Sum(f => f.Calls));
            if (line != "")
            {
                Assert.DoesNotContain(env, r.Err);
                Assert.EndsWith(ImportGuidance + "\n", r.Err);
            }
        }
    }

    // ---- 5. allowed --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("cli")]
    [InlineData("claude-mcp")]
    [InlineData("codex-mcp")]
    [InlineData("codex-bash")]
    public void The_first_import_of_the_plan_into_its_environment_is_allowed_and_nothing_is_written(string form)
    {
        SeedZip(Folder);
        MakePlan();
        var test = SeedTarget();
        var pac = new FakePac();
        var stdin = form switch
        {
            "claude-mcp" => SeedCall(mcp: true),
            "codex-mcp" => Mcp(TargetIdText, SeedZipPath, SeedSettingsPath, stage: true, tool: "mcp__pac_cli__solution_import"),
            "codex-bash" => CodexBash(PacCommand(TargetIdText, SeedZipPath, SeedSettingsPath, stage: true)),
            _ => SeedCall(),
        };

        var r = Gate(Environments(test), stdin, pac);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Out);
        Assert.Equal("", r.Err);
        Assert.Equal(1, test.Calls);
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void An_install_step_of_the_chain_has_no_stage_flag_and_the_chains_first_zip_passes()
    {
        Chain();
        var test = new FakeTarget();
        MakePlan(test);
        var pac = new FakePac();

        var r = Gate(Environments(new FakeTarget()), Cli(TargetIdText, Path.Combine(Folder, "A.zip")), pac);

        Assert.Equal((0, "", ""), (r.Code, r.Out, r.Err));
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void A_call_that_is_not_an_import_is_allowed_without_reading_anything()
    {
        var test = SeedTarget();

        var r = Gate(Environments(test), Hook("Bash", new JsonObject { ["command"] = "git status" }));

        Assert.Equal((0, "", ""), (r.Code, r.Out, r.Err));
        Assert.Equal(0, test.Calls);
    }

    // ---- 6. blocked --------------------------------------------------------------------------------------------

    /// <summary>One call to the gate: the environments, the stdin, and the lines the block must hold (none when the call must pass).</summary>
    private sealed record Case(Dictionary<string, FakeTarget> Environments, byte[]? Stdin, string[] Lines, bool Reading = false, string Env = TargetIdText);

    private static string[] Gated(params string[] lines) => [.. lines.Select(l => Prefix + l)];

    private Case Setup(string name)
    {
        switch (name)
        {
            case "zip-of-no-plan":
                SeedZip(Folder);
                MakePlan();
                return new(Seeded(), Cli(TargetIdText, SmallZip(Stage, "Other"), SeedSettingsPath, stage: true), Gated("Other.zip is not a zip of the plan"));
            case "second-zip-of-the-chain":
                Chain();
                MakePlan(new FakeTarget());
                return new(Environments(new FakeTarget()), Cli(TargetIdText, Path.Combine(Folder, "B.zip")), Gated("the plan imports A.zip first"));
            case "skipped-zip":
            case "plan-of-skips":
                SmallZip(Folder, "Kept");
                if (name == "skipped-zip")
                {
                    SmallZip(Folder, "Fresh");
                }

                MakePlan(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept") } });
                return new(
                    Environments(new FakeTarget { Snaps = { ["Kept"] = Installed("Kept") } }),
                    Cli(TargetIdText, Path.Combine(Folder, "Kept.zip")),
                    Gated(name == "skipped-zip" ? "the plan skips Kept.zip" : "the plan imports nothing"));
            case "zip-changed-after-the-plan":
                SmallZip(Folder, "Solo");
                MakePlan(new FakeTarget());
                File.Delete(Path.Combine(Folder, "Solo.zip"));
                var changed = SmallZip(Folder, "Solo", files: ("Other/Marker.txt", "changed"));
                return new(Environments(new FakeTarget()), Cli(TargetIdText, changed), Gated("the dry run did not pass: the zip Solo.zip changed since the plan; nothing was imported. Plan again and review the new plan."));
            case "settings-file-missing":
                SeedZip(Folder);
                MakePlan();
                return new(Seeded(), Cli(TargetIdText, SeedZipPath, stage: true), Gated("the plan imports PowerOpsSeed.zip with PowerOpsSeed.settings.json"));
            case "settings-file-different":
                SeedZip(Folder);
                MakePlan();
                var other = Path.Combine(Stage, "PowerOpsSeed.settings.json");
                File.WriteAllText(other, "{}");
                return new(Seeded(), Cli(TargetIdText, SeedZipPath, other, stage: true), Gated("the plan imports PowerOpsSeed.zip with a different settings file"));
            case "settings-file-extra":
                Chain();
                MakePlan(new FakeTarget());
                var extra = Path.Combine(Stage, "x.settings.json");
                File.WriteAllText(extra, "{}");
                return new(Environments(new FakeTarget()), Cli(TargetIdText, Path.Combine(Folder, "A.zip"), extra), Gated("the plan imports A.zip with no settings file"));
            case "upgrade-without-stage-and-upgrade":
                SeedZip(Folder);
                MakePlan();
                return new(Seeded(), Cli(TargetIdText, SeedZipPath, SeedSettingsPath), Gated("the plan stages and upgrades PowerOpsSeed.zip"));
            case "install-with-stage-and-upgrade":
                Chain();
                MakePlan(new FakeTarget());
                return new(Environments(new FakeTarget()), Cli(TargetIdText, Path.Combine(Folder, "A.zip"), stage: true), Gated("the plan doesn't stage and upgrade A.zip"));
            case "target-changed":
                SeedZip(Folder);
                MakePlan();
                return new(
                    Environments(new FakeTarget { Snaps = { ["PowerOpsSeed"] = Installed("PowerOpsSeed", "0.9.0.0") } }),
                    SeedCall(),
                    Gated("the dry run did not pass: the target's state of the solution in PowerOpsSeed.zip changed since the plan; nothing was imported. Plan again and review the new plan."));
            case "blocked-plan":
                SeedZip(Folder, settings: false);
                MakePlan();
                return new(Seeded(), Cli(TargetIdText, SeedZipPath, stage: true), Gated("the dry run did not pass: a blocked plan is never applied"));
            case "plan-file-missing":
                SeedZip(Folder);
                return new(Seeded(), SeedCall(), ["error: the plan file does not exist", .. Gated("the dry run did not pass")]);
            case "plan-file-of-another-version":
                SeedZip(Folder);
                MakePlan();
                var doc = JsonNode.Parse(File.ReadAllText(PlanFile))!.AsObject();
                doc["tool"] = "9.9.9";
                File.WriteAllText(PlanFile, doc.ToJsonString());
                return new(Seeded(), SeedCall(), Gated("the dry run did not pass: the plan was made by powerops 9.9.9; this is "));
            case "plan-file-symbolic-link":
                SeedZip(Folder);
                MakePlan();
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

                return new(Seeded(), SeedCall(), ["error: the plan file is a symbolic link; symbolic links are not read", .. Gated("the dry run did not pass")]);
            case "call-zip-missing":
                SeedZip(Folder);
                MakePlan();
                return new(Seeded(), Cli(TargetIdText, Path.Combine(Folder, "Gone.zip"), SeedSettingsPath, stage: true), Gated("Gone.zip does not exist, is a symbolic link, is not a file, or can't be read"));
            case "call-settings-file-missing":
                SeedZip(Folder);
                MakePlan();
                return new(Seeded(), Cli(TargetIdText, SeedZipPath, Path.Combine(Folder, "Gone.settings.json"), stage: true), Gated("Gone.settings.json does not exist, is a symbolic link, is not a file, or can't be read"));
            case "call-zip-symbolic-link":
                SeedZip(Folder);
                MakePlan();
                var link = Path.Combine(Stage, "Link.zip");
                try
                {
                    File.CreateSymbolicLink(link, SeedZipPath);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Assert.Skip("Symbolic links are not available here.");
                }

                return new(Seeded(), Cli(TargetIdText, link, SeedSettingsPath, stage: true), Gated("Link.zip does not exist, is a symbolic link, is not a file, or can't be read"));
            case "call-zip-unreadable":
                Assert.SkipWhen(OperatingSystem.IsWindows(), "File permissions are not Unix modes here.");
                SeedZip(Folder);
                MakePlan();
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(SeedZipPath, UnixFileMode.None);
                }

                Assert.SkipWhen(CanRead(SeedZipPath), "The file is readable anyway, as for root.");
                return new(Seeded(), SeedCall(), Gated("PowerOpsSeed.zip does not exist, is a symbolic link, is not a file, or can't be read"));
            case "stdin-over-1-mb":
                return new(Seeded(), new byte[1_048_577], Gated("the hook input is larger than 1 MB"), Reading: true);
            case "no-stdin":
                return new(Seeded(), null, Gated("the hook input is read from stdin"), Reading: true);
            case "target-read-throws":
                SeedZip(Folder);
                MakePlan();
                return new(Environments(new FakeTarget { Fail = new InvalidOperationException("secret detail") }), SeedCall(), Gated("internal error (InvalidOperationException)"));
            case "target-read-fails":
                SeedZip(Folder);
                MakePlan();
                return new(Environments(new FakeTarget { Fail = new DriftReadException("the read failed", "tool line", 3) }), SeedCall(), ["error: --target: the read failed", "  | tool line", .. Gated("the dry run did not pass")]);
            default:
                throw new ArgumentException(name);
        }
    }

    private static bool CanRead(string path)
    {
        try
        {
            File.ReadAllBytes(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static TheoryData<string> BlockNames() =>
    [
        "zip-of-no-plan", "second-zip-of-the-chain", "skipped-zip", "plan-of-skips", "zip-changed-after-the-plan", "settings-file-missing",
        "settings-file-different", "settings-file-extra", "upgrade-without-stage-and-upgrade", "install-with-stage-and-upgrade", "target-changed",
        "blocked-plan", "plan-file-missing", "plan-file-of-another-version", "plan-file-symbolic-link", "call-zip-missing", "call-settings-file-missing",
        "call-zip-symbolic-link", "call-zip-unreadable", "stdin-over-1-mb", "no-stdin", "target-read-throws", "target-read-fails",
    ];

    [Theory]
    [MemberData(nameof(BlockNames))]
    public void A_call_that_does_not_match_the_plan_is_blocked_with_its_reason(string name)
    {
        var c = Setup(name);
        var pac = new FakePac();

        var r = Gate(c.Environments, c.Stdin, pac);

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        foreach (var line in c.Lines)
        {
            Assert.Contains(line, r.Err);
        }

        Assert.EndsWith((c.Reading ? ReadingGuidance : ImportGuidance) + "\n", r.Err);
        Assert.DoesNotContain(c.Env, r.Err);
        Assert.DoesNotContain(_root, r.Err);
        Assert.DoesNotContain("secret detail", r.Err);
        Assert.DoesNotContain("--path", r.Err);
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void A_check_past_the_deadline_blocks_and_its_late_output_is_dropped()
    {
        SeedZip(Folder);
        MakePlan();
        var test = SeedTarget();
        using var release = new ManualResetEventSlim();
        DeployTarget Read(string target, IReadOnlyList<string> solutions, IReadOnlyList<string> dependencies)
        {
            release.Wait(TimeSpan.FromSeconds(30));
            return test.Read(target, solutions, dependencies);
        }

        var (o, e) = (new StringWriter(), new StringWriter());
        var root = new RootCommand { DeployCommand.Create(o, e, new FakePac().Run, Read, () => new MemoryStream(SeedCall()), TimeSpan.FromSeconds(1)) };

        var code = root.Parse(["deploy", "gate", PlanFile]).Invoke(new InvocationConfiguration { Output = o, Error = e, EnableDefaultExceptionHandler = false });
        release.Set();
        Thread.Sleep(300);

        Assert.Equal(2, code);
        Assert.Equal("", o.ToString());
        Assert.Equal($"{Prefix}the check took longer than 1 seconds\n{ImportGuidance}\n", e.ToString().ReplaceLineEndings("\n"));
    }

    // ---- 6b. the files the call names ---------------------------------------------------------------------------

    private static void MakeFifo(string path)
    {
        using var mkfifo = Process.Start(new ProcessStartInfo("mkfifo") { ArgumentList = { path }, RedirectStandardError = true })!;
        mkfifo.WaitForExit();
        Assert.True(mkfifo.ExitCode == 0, "mkfifo failed");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_fifo_as_the_zip_or_the_settings_file_is_blocked_and_does_not_hang(bool settings)
    {
        // Opening a FIFO read-write without blocking is Linux behaviour; POSIX leaves it undefined, so a hang elsewhere is not risked.
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux only.");
        SeedZip(Folder);
        MakePlan();
        var fifo = Path.Combine(Stage, settings ? "Fifo.settings.json" : "Fifo.zip");
        MakeFifo(fifo);
        // Held open for reading and writing, so the gate's open does not wait for a writer; with no data a read of it would wait for ever.
        using var holder = new FileStream(fifo, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        var r = Gate(Seeded(), settings ? Cli(TargetIdText, SeedZipPath, fifo, stage: true) : Cli(TargetIdText, fifo, SeedSettingsPath, stage: true), deadline: TimeSpan.FromSeconds(20));

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.Contains($"{Prefix}{Path.GetFileName(fifo)} does not exist, is a symbolic link, is not a file, or can't be read\n", r.Err);
        Assert.EndsWith(ImportGuidance + "\n", r.Err);
    }

    [Fact]
    public void A_device_that_reads_for_ever_as_the_zip_is_blocked()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "/dev/zero is a Linux device.");
        SeedZip(Folder);
        MakePlan();

        var r = Gate(Seeded(), Cli(TargetIdText, "/dev/zero", SeedSettingsPath, stage: true), deadline: TimeSpan.FromSeconds(20));

        Assert.Equal(2, r.Code);
        Assert.Contains($"{Prefix}zero does not exist, is a symbolic link, is not a file, or can't be read\n", r.Err);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_zip_or_settings_file_rewritten_during_the_dry_run_blocks(bool settings)
    {
        SeedZip(Folder);
        MakePlan();
        var (path, name) = settings ? (SeedSettingsPath, "PowerOpsSeed.settings.json") : (SeedZipPath, "PowerOpsSeed.zip");
        var test = SeedTarget();
        var pac = new FakePac();

        var r = Gate(Environments(test), SeedCall(), pac, during: () => File.WriteAllBytes(path, [.. File.ReadAllBytes(path), (byte)' ']));

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.Contains($"{Prefix}{name} changed while the gate checked it\n", r.Err);
        Assert.EndsWith(ImportGuidance + "\n", r.Err);
        Assert.Equal(1, test.Calls);
        Assert.Empty(pac.Calls);
    }

    [Fact]
    public void Apply_refuses_a_zip_that_became_a_link_after_the_re_plan_read_it()
    {
        SeedZip(Folder);
        MakePlan();
        var copy = Path.Combine(Stage, "Copy.zip");
        File.Copy(SeedZipPath, copy);
        var (test, pac) = (SeedTarget(), new FakePac());
        var (o, e) = (new StringWriter(), new StringWriter());

        var code = Program.Run(["deploy", "apply", PlanFile, "--target", TargetValue, "--apply"], o, e, pac.Run, null, (t, s, d) =>
        {
            File.Delete(SeedZipPath);
            try
            {
                File.CreateSymbolicLink(SeedZipPath, copy);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Assert.Skip("Symbolic links are not available here.");
            }

            return test.Read(t, s, d);
        });

        Assert.Equal(3, code);
        Assert.Empty(pac.Calls);
        Assert.Contains("PowerOpsSeed.zip changed after the re-plan read it, so it was not imported.", o.ToString());
    }

    [Fact]
    public void The_first_import_comes_from_the_fresh_plan_not_the_plan_files_solutions_array()
    {
        Chain();
        MakePlan(new FakeTarget());
        var doc = JsonNode.Parse(File.ReadAllText(PlanFile))!.AsObject();
        var edited = new JsonArray([.. doc["solutions"]!.AsArray().Reverse().Select(n => n!.DeepClone())]);
        Assert.Equal("C.zip", (string?)edited[0]!["zip"]);
        doc["solutions"] = edited;
        File.WriteAllText(PlanFile, doc.ToJsonString());

        var first = Gate(Environments(new FakeTarget()), Cli(TargetIdText, Path.Combine(Folder, "A.zip")));
        var other = Gate(Environments(new FakeTarget()), Cli(TargetIdText, Path.Combine(Folder, "C.zip")));

        Assert.Equal((0, "", ""), (first.Code, first.Out, first.Err));
        Assert.Equal(2, other.Code);
        Assert.Contains($"{Prefix}the plan imports A.zip first\n", other.Err);
    }

    // ---- 6c. the arguments --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("-x")]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("-?")]
    [InlineData("/?")]
    [InlineData("/h")]
    public void A_plan_file_argument_that_starts_with_a_dash_or_asks_for_help_blocks_everything(string argument)
    {
        var (o, e) = (new StringWriter(), new StringWriter());

        var code = Program.Run(["deploy", "gate", argument], o, e, new FakePac().Run, null, (t, s, d) => throw new InvalidOperationException(), () => new MemoryStream(Hook("Bash", new JsonObject { ["command"] = "git status" })));

        Assert.Equal(2, code);
        Assert.Equal("", o.ToString());
        Assert.Contains(argument is "-x" ? $"{Prefix}the plan file must not start with '-'\n{ImportGuidance}\n" : "error: ", e.ToString().ReplaceLineEndings("\n"));
    }

    // ---- 7. exit codes -----------------------------------------------------------------------------------------

    // A stderr that fails on every write.
    private sealed class BrokenWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value) => throw new IOException("broken");

        public override void Write(string? value) => throw new IOException("broken");
    }

    [Fact]
    public void A_stderr_that_fails_still_exits_2()
    {
        SeedZip(Folder);
        MakePlan();
        var envs = Seeded();

        var code = Program.Run(["deploy", "gate", PlanFile], new StringWriter(), new BrokenWriter(), new FakePac().Run, null, (t, s, d) => envs[t].Read(t, s, d), () => new MemoryStream(Cli(TargetIdText, Path.Combine(Folder, "Gone.zip"))));

        Assert.Equal(2, code);
    }

    [Fact]
    public void No_plan_file_argument_is_a_usage_error()
    {
        var r = Run(SeedTarget(), new FakePac(), "deploy", "gate");

        Assert.Equal(2, r.Code);
        Assert.Equal("", r.Out);
        Assert.StartsWith("error: ", r.Err);
    }
}