using System.Text.RegularExpressions;

using PowerOps.Core.Output;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public partial class SkillsCommandTests : IDisposable
{
    private static readonly string[] Skills = ["powerops-plan", "powerops-prototype", "powerops-review-plan", "powerops-implement", "powerops-review-security", "powerops-review-sketch", "powerops-verify"];
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-skills-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    private string P(params string[] parts) => Path.Combine([_temp, .. parts]);

    private string Skill(string root, string name) => File.ReadAllText(P(root, "skills", name, "SKILL.md"));

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex NamePattern();

    [Fact]
    public void Empty_dir_gets_both_targets_with_all_seven_skills()
    {
        var (code, output, _) = Run("skills", "install", _temp);

        Assert.Equal(0, code);
        foreach (var root in new[] { ".claude", ".agents" })
        {
            foreach (var skill in Skills)
            {
                Assert.True(File.Exists(P(root, "skills", skill, "SKILL.md")));
                Assert.Contains($"created {P(root, "skills", skill, "SKILL.md").Replace('\\', '/')}", output);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_directory_in_place_of_a_skill_file_fails_before_writing_anything(bool force)
    {
        Directory.CreateDirectory(P(".claude", "skills", "powerops-plan", "SKILL.md"));
        var args = force ? new[] { "skills", "install", _temp, "--force" } : ["skills", "install", _temp];

        var (code, output, error) = Run(args);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains($"powerops-plan/SKILL.md: {SafeWrite.FolderReason}", error);
        Assert.Equal([P(".claude", "skills", "powerops-plan", "SKILL.md")], Directory.EnumerateFileSystemEntries(_temp, "*", SearchOption.AllDirectories).Where(e => e.EndsWith("SKILL.md", StringComparison.Ordinal)));
        Assert.False(Directory.Exists(P(".agents")));
    }

    [Fact]
    public void Rerun_reports_unchanged_and_touches_nothing()
    {
        Run("skills", "install", _temp);
        var file = P(".agents", "skills", "powerops-plan", "SKILL.md");
        var stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(file, stamp);

        var (code, output, _) = Run("skills", "install", _temp);

        Assert.Equal(0, code);
        Assert.DoesNotContain("created", output);
        Assert.DoesNotContain("updated", output);
        Assert.Equal(2 * SkillsCommand.Shipped().Count, output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(l => l.StartsWith("unchanged ")));
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(file));
    }

    [Fact]
    public void Crlf_copy_counts_as_unchanged()
    {
        Run("skills", "install", _temp);
        var file = P(".agents", "skills", "powerops-plan", "SKILL.md");
        File.WriteAllText(file, File.ReadAllText(file).Replace("\n", "\r\n"));

        var (code, output, _) = Run("skills", "install", _temp);

        Assert.Equal(0, code);
        Assert.DoesNotContain("updated", output);
    }

    [Fact]
    public void Edited_file_refuses_and_writes_nothing()
    {
        Run("skills", "install", _temp);
        var edited = P(".agents", "skills", "powerops-plan", "SKILL.md");
        File.AppendAllText(edited, "my edit\n");
        var missing = P(".claude", "skills", "powerops-implement", "SKILL.md");
        File.Delete(missing);

        var (code, _, err) = Run("skills", "install", _temp);

        Assert.Equal(1, code);
        Assert.Contains($"error: {edited.Replace('\\', '/')} differs from the shipped skill; rerun with --force to overwrite", err);
        Assert.EndsWith("my edit\n", File.ReadAllText(edited));
        Assert.False(File.Exists(missing));
    }

    [Fact]
    public void Force_overwrites_and_reports_updated()
    {
        Run("skills", "install", _temp);
        var edited = P(".agents", "skills", "powerops-plan", "SKILL.md");
        File.AppendAllText(edited, "my edit\n");

        var (code, output, _) = Run("skills", "install", _temp, "--force");

        Assert.Equal(0, code);
        Assert.Contains($"updated {edited.Replace('\\', '/')}", output);
        Assert.DoesNotContain("my edit", File.ReadAllText(edited));
    }

    [Fact]
    public void Written_files_are_lf_and_have_no_bom()
    {
        Run("skills", "install", _temp);

        var bytes = File.ReadAllBytes(P(".agents", "skills", "powerops-plan", "SKILL.md"));
        Assert.Equal((byte)'-', bytes[0]);
        Assert.DoesNotContain((byte)'\r', bytes);
    }

    [Fact]
    public void Only_claude_md_detects_only_claude()
    {
        File.WriteAllText(P("CLAUDE.md"), "x");

        Assert.Equal(0, Run("skills", "install", _temp).Code);

        Assert.True(Directory.Exists(P(".claude", "skills")));
        Assert.False(Directory.Exists(P(".agents")));
    }

    [Fact]
    public void Only_agents_md_detects_only_agents()
    {
        File.WriteAllText(P("AGENTS.md"), "x");

        Assert.Equal(0, Run("skills", "install", _temp).Code);

        Assert.True(Directory.Exists(P(".agents", "skills")));
        Assert.False(Directory.Exists(P(".claude")));
    }

    [Fact]
    public void Agent_codex_installs_only_agents_even_with_claude_md()
    {
        File.WriteAllText(P("CLAUDE.md"), "x");

        Assert.Equal(0, Run("skills", "install", _temp, "--agent", "codex").Code);

        Assert.True(Directory.Exists(P(".agents", "skills")));
        Assert.False(Directory.Exists(P(".claude")));
    }

    [Fact]
    public void Two_agents_install_both_targets()
    {
        Assert.Equal(0, Run("skills", "install", _temp, "--agent", "claude", "--agent", "gemini").Code);

        Assert.True(Directory.Exists(P(".claude", "skills")));
        Assert.True(Directory.Exists(P(".agents", "skills")));
    }

    [Fact]
    public void Unknown_agent_and_missing_path_are_usage_errors()
    {
        Assert.Equal(2, Run("skills", "install", _temp, "--agent", "vim").Code);
        var (code, _, err) = Run("skills", "install", P("nope"));
        Assert.Equal(2, code);
        Assert.StartsWith("error: path not found:", err);
    }

    [Theory]
    [InlineData("powerops-prototype")]
    [InlineData("powerops-review-plan")]
    [InlineData("powerops-review-security")]
    [InlineData("powerops-review-sketch")]
    [InlineData("powerops-verify")]
    public void Only_the_claude_copies_of_the_forked_skills_get_the_fork_lines(string skill)
    {
        Run("skills", "install", _temp);

        var claude = Skill(".claude", skill);
        var shared = Skill(".agents", skill);
        foreach (var line in new[] { "context: fork", "background: false", "model: opus" })
        {
            Assert.Contains($"\n{line}\n", claude);
            Assert.DoesNotContain(line, shared);
        }

        Assert.Equal(Skill(".agents", "powerops-plan"), Skill(".claude", "powerops-plan"));
        Assert.DoesNotContain("context: fork", Skill(".claude", "powerops-implement"));
    }

    [Theory]
    [InlineData("powerops-prototype/SKILL.md")]
    [InlineData("powerops-review-plan/SKILL.md")]
    [InlineData("powerops-review-security/SKILL.md")]
    [InlineData("powerops-review-sketch/SKILL.md")]
    [InlineData("powerops-verify/SKILL.md")]
    public void ForClaude_inserts_before_the_closing_fence_and_leaves_other_files_alone(string file)
    {
        var result = SkillsCommand.ForClaude(file, "---\nname: x\n---\n\nbody\n---\n");

        Assert.Equal("---\nname: x\ncontext: fork\nbackground: false\nmodel: opus\n---\n\nbody\n---\n", result);
        Assert.Equal("---\nname: x\n---\n", SkillsCommand.ForClaude("powerops-plan/SKILL.md", "---\nname: x\n---\n"));
        Assert.Equal("---\nname: x\n---\n", SkillsCommand.ForClaude("powerops-verify/other.md", "---\nname: x\n---\n"));
    }

    [Fact]
    public void Every_shipped_skill_has_portable_front_matter()
    {
        var files = SkillsCommand.Shipped();
        Assert.Equal(Skills.Order(), files.Keys.Select(k => k.Split('/')[0]).Distinct().Order());
        string[] allowed = ["name", "description", "license", "compatibility", "metadata"];

        foreach (var (rel, text) in files.Where(f => f.Key.EndsWith("/SKILL.md", StringComparison.Ordinal)))
        {
            var lines = text.Split('\n');
            Assert.Equal("---", lines[0]);
            var header = lines.Skip(1).TakeWhile(l => l != "---").Where(l => l.Length > 0 && !char.IsWhiteSpace(l[0])).ToList();
            var fields = header.ToDictionary(l => l[..l.IndexOf(':')], l => l[(l.IndexOf(':') + 1)..].Trim());
            Assert.All(fields.Keys, k => Assert.Contains(k, allowed));
            Assert.Equal(rel.Split('/')[0], fields["name"]);
            Assert.Matches(NamePattern(), fields["name"]);
            Assert.InRange(fields["description"].Length, 1, 1024);
        }
    }

    [Fact]
    public void Plan_skill_description_says_to_run_before_the_microsoft_build_skills()
    {
        var description = SkillsCommand.Shipped()["powerops-plan/SKILL.md"].Split('\n').First(l => l.StartsWith("description:", StringComparison.Ordinal));
        string[] buildSkills = ["app-builder", "genpage", "canvas-app", "build-flow", "create-site"];

        Assert.Contains("Run it before any Microsoft build skill", description);
        Assert.All(buildSkills, s => Assert.Contains(s, description));
    }

    [Fact]
    public void Implement_skill_names_the_microsoft_build_skills_and_the_import_paths_that_need_a_yes()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] build = ["canvas-apps", "model-apps", "power-automate", "power-pages", "code-apps", "create-site", "create-code-app", "checkout of microsoft/power-platform-skills"];
        string[] imports = ["solution_import", "solution-settings.js import", "import-solution", "deploy-pipeline", "dv-solution", "`powerops deploy plan` and then `powerops deploy apply`"];

        Assert.All(build, s => Assert.Contains(s, text));
        Assert.All(imports, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Implement_skill_asks_for_a_yes_before_every_apply_that_writes_to_an_environment()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] rule =
        [
            "Never run `powerops deploy apply --apply`, or call the MCP tool `deploy_apply` with `apply: true`, unless the user said yes to that environment in this session.",
            // M115: settings apply checks no review, so no yes covers it for an agent.
            "Never run `powerops settings apply --apply`: it checks no review, so it is the person's own command and no yes covers it; give them the command if they want it.",
            "One yes per deployment covers its import with `powerops deploy apply` and the plan's named steps there.",
            "Run each command without `--apply` first and show the user the target line",
        ];

        Assert.All(rule, s => Assert.Contains(s, text));
        Assert.DoesNotContain("covers its import, `powerops settings apply --apply`", text);
        Assert.DoesNotContain("`settings apply` takes the name they approved as `--target`.", text);
    }

    [Fact]
    public void Implement_skill_writes_the_flow_tests_with_fake_values_and_runs_them_before_the_pull_request()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording = ["Before you open a pull request, for each cloud flow the plan adds or changes, write or update its tests once the solution is exported and unpacked into the repo", "use what the latest sign-in probe (section 3, step 3) found about `pac`; do not run `pac` again to find out, and export from the environment the person named there, never the active profile's", "keep the zip, which `powerops analyze` and `powerops test` both read", "Use fake values in triggers and mocks", "Run `powerops test", "never delete or weaken a test"];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.DoesNotContain("use what the user told you about `pac`", text);
        Assert.Contains("Tell the Security reviewer that `pac` is signed in only if the user said so in this session.", text);
    }

    [Fact]
    public void Implement_skill_checks_the_final_saved_version_live_and_sets_done_only_when_the_checks_passed()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "check its final saved version in Dev",
            "never run `pac` or `az` for these checks",
            // M94: a check that writes is covered by the sign-off; it no longer follows the ask-each-time rule 7.
            "covered by the sign-off with the test rows its Step's check names",
            "Fire each flow's real trigger once with a synthetic input",
            "load the published version fresh",
            "report any other unmanaged solution it is in",
            "say \"not verified\" and why",
            "A later save or publish voids the checks it affects",
            "when steps 1 to 3 passed or the user accepted each remaining gap by name",
            "leave `status: implementing` and list the failed checks and the checks that did not run, separately",
            "anything that differs from the plan body",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.DoesNotContain("3. Set `status: done` in the plan header, one line only.", text);
    }

    [Fact]
    public void Implement_skill_names_the_dataverse_skills_and_probes_sign_ins_with_read_only_status_calls()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "code-apps-preview",
            "dv-metadata",
            "dv-solution",
            "MSCRM.SolutionUniqueName",
            "Then probe each of these sign-ins that the plan's routes need: Dataverse, `pac`, `az`, FlowAgent's connection management and Canvas Authoring.",
            "Use a read-only status call and nothing else: `pac auth list` for `pac`, `az account show -o none` for `az`, and an MCP server's own status or account-list tool, such as FlowAgent's or Canvas Authoring's.",
            "One sign-in does not cover another; one you can't probe that way, such as the Dataverse skills' own, you ask the user about.",
            "A probe that fails or lists nothing counts as missing.",
            "Report each as available or missing, and never repeat, log or write what a probe prints: accounts, tenants and environments stay out of chat and files.",
            "For a missing one, ask the person to sign in or to go on without it, and wait. Probe it once more when they say it is done; if it is still missing, say so and let the person decide. A probe never picks the target.",
            "If the plan's `environment` is null or a role (Dev, Test or Prod, in any case), ask the person to name that environment before any tenant read or live check, and give the skill that name, never the role; any other value is an older plan's name: give it as written. Give the skill the plan's `Solution and publisher` too.",
            "Use only the environment the person named (step 3), or the plan's `environment` when it holds a name, as the target.",
            "Set `status: implementing` in the plan header, unless it already says so.",
            "If it already said `implementing`, or a step of this plan is already committed (after an amendment, step 6), read the commit messages and `writes.md` (step 10)",
            "Stop and report when a tool can't create or save inside that solution",
            "Do not change the preferred solution",
            "don't redo a step",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.DoesNotContain("you do not run `pac` or `az` to find out", text);
        Assert.DoesNotContain("Ask the user whether they are signed in to each of", text);
    }

    [Fact]
    public void Implement_skill_warns_about_studio_saves_the_gallery_crash_table_membership_and_the_old_version_banner()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "A Canvas Authoring compile isn't a save",
            "look at the saved time. If it didn't move, change a property and undo it to turn Save on, save, or ask the user to, and confirm that the time moved. Reload only after that, never to check a compile that isn't saved yet",
            "In one run, two compiles of a whole screen with its gallery crashed Studio while the tool reported success; the screen first with the gallery alone after it didn't. The cause isn't known",
            "modern controls also showed as empty blocks after a compile until a reload",
            "In one run, the Python client's table create put the table in the preferred solution although `solution=` was passed. Check each table's membership, and ask before moving one",
            "Run the check and the commit as separate commands, so a failed check can't be committed",
            "If the published app shows an old-version banner, select Refresh on it",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.DoesNotContain("because", text[text.IndexOf("two compiles of a whole screen", StringComparison.Ordinal)..][..300]);
    }

    [Fact]
    public void Implement_skill_stops_by_impact_and_keeps_the_plan_body_closed()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "Never edit the plan body. Stop by impact",
            "scope, data, security or cost",
            "stops with one concrete question to the user, and you do nothing about that choice until they answer: no build, no new plan and no other questions",
            "A violation that names `verify.md` or `security-solution.md` is fixed by replacing the value in that report with a placeholder, and the plan isn't edited",
            // M94: the build goes on with the steps that don't depend on the answer, instead of staying stopped.
            "If the answer changes a plan line, or a step cannot be done as written, go on only with steps that don't depend on it, and note the header's `signed-off-hash` and hand to `powerops-plan` section 10, which makes the edit; you never edit the plan body.",
            "A change to scope, data, security or cost gets a fresh `powerops-review-plan` before that sign-off; another amendment may keep the old review, which the sign-off names as stale.",
            // M94: the held steps and every tenant write wait for the new sign-off, not the whole build.
            "The person signs off with `powerops plan sign-off`, and `amendment: <line> (answered by: person)` is logged. Resume the held steps, and any tenant write, only once the header's `signed-off-hash` differs from the one you noted and `powerops plan status` shows no violation for the plan.",
            "An agent's answer may settle a product choice the person's brief delegates, and nothing else; it never changes the plan: a change to a plan line needs the person's answer.",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        // The hand-off comes before the log: the order of the step hid it in the 1.0 trial (F44).
        Assert.True(text.IndexOf("hand to `powerops-plan` section 10", StringComparison.Ordinal) < text.IndexOf("Last, log each such question", StringComparison.Ordinal));
    }

    [Fact]
    public void Implement_skill_builds_under_the_sign_off_in_dev_and_stops_for_each_stop_class()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        var rule7 = text[text.IndexOf("\n7. ", StringComparison.Ordinal)..text.IndexOf("\n8. ", StringComparison.Ordinal)];
        string[] opening = ["the sign-off is your authority", "once per deployment"];
        string[] perAgent =
        [
            "After the sign-off, build without asking; ask in chat only for rule 7's stop list and the deployment yes, and wait for the answer.",
            "the sign-off covers what rule 7 says; for a stop-list item or a deployment with no person to answer, skip that step and report it.",
        ];
        string[] preflight =
        [
            "**Preflight**",
            "Ask once, in one message, for everything missing",
            "start no trial",
            "allocated to the Dev environment",
            "`sign-off covers:`",
            "The last `sign-off covers:` line counts",
            "the sign-off covers no tenant write and each one is a stop",
        ];
        string[] authorities =
        [
            "The stop list wins over both",
            "For the plan's `signed-off-hash` and the Dev environment only",
            "A `Test` or `Prod` header gives the sign-off no tenant write",
            "With `environment: null`, it covers the environment the person names as Dev at the preflight, and nothing before they do",
            "the header's `environment` is `Dev`, or an older plan's name that its `Environments and route` calls Dev",
            "through the route and in the solution the plan names",
            "that the `sign-off covers:` Log line lists",
            "A deployment is one saved `deploy plan` file to one target environment",
            "a re-planned or changed plan file, a changed `signed-off-hash` or a `blocked` report",
            "--apply --require-reviews",
            "The sign-off never covers an `--apply`, in Dev too",
            "in full, word for word",
            "never edit a hook",
        ];
        // One phrase per stop class, in order.
        string[] stops =
        [
            "MFA, consent, terms or credential prompt",
            "a write no Step names",
            "(3) any delete",
            "licence, trial or capacity",
            "any tenant-level or admin write",
            "a write outside Dev that no deployment yes covers",
            "a person, group or account the plan doesn't name",
            "a finding only a person may accept",
        ];

        Assert.All(opening.Concat(perAgent).Concat(preflight), s => Assert.Contains(s, text));
        Assert.All(authorities.Concat(stops), s => Assert.Contains(s, rule7));
        Assert.StartsWith("\n7. Write to a tenant only under an authority below; anything they don't cover stops. The stop list wins over both", rule7);
        Assert.All(stops.Zip(stops.Skip(1)), p => Assert.True(rule7.IndexOf(p.First, StringComparison.Ordinal) < rule7.IndexOf(p.Second, StringComparison.Ordinal), p.Second));
        Assert.Contains("go on only with steps that don't depend on it", text);
        Assert.Contains("an undo that deletes is the person's", text);
        Assert.Contains("`plan <first 12 characters of the signed-off-hash>, step <n>`", text);
        Assert.Contains("`none: slip`", text);
        Assert.DoesNotContain("Ask each time", text);
        Assert.DoesNotContain("explicit go-ahead", text);
        var finish = text[text.IndexOf("## 4. Finish", StringComparison.Ordinal)..text.IndexOf("## 5. Power BI", StringComparison.Ordinal)];
        var powerBi = text[text.IndexOf("## 5. Power BI", StringComparison.Ordinal)..text.IndexOf("## Per agent", StringComparison.Ordinal)];
        Assert.Contains("covered by the sign-off", finish);
        Assert.Contains("covered by the sign-off", powerBi);
        Assert.Contains("the Dev workspace the plan names", powerBi);
        Assert.Contains("other rows follow rule 7 of section 3", finish);
        Assert.Contains("stops under rule 7 class 6", powerBi);
    }

    [Fact]
    public void Plan_skill_says_what_the_sign_off_covers_logs_it_and_starts_the_build()
    {
        var text = PlanSkill();
        var signOff = text[text.IndexOf("## 9. Sign-off", StringComparison.Ordinal)..text.IndexOf("## 10.", StringComparison.Ordinal)];
        var step3 = signOff[signOff.IndexOf("\n3. ", StringComparison.Ordinal)..signOff.IndexOf("\n4. ", StringComparison.Ordinal)];
        var step4 = signOff[signOff.IndexOf("\n4. ", StringComparison.Ordinal)..signOff.IndexOf("\n5. ", StringComparison.Ordinal)];
        var fast = FastPathOf(text);
        var b = fast[fast.IndexOf("- **B**", StringComparison.Ordinal)..fast.IndexOf("- **C**", StringComparison.Ordinal)];

        Assert.Contains("Signing off lets the build make every Dev write the Steps name", step3);
        Assert.Contains("log `sign-off covers: Dev writes the Steps name; Dev settings: <list or none>`", step4);
        Assert.Contains("start `powerops-implement`", step4);
        Assert.Contains("the two lines of section 9 step 3", b);
        var references = SkillsCommand.Shipped().Where(f => f.Key.StartsWith("powerops-plan/references/", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(references);
        Assert.All(references, r => Assert.DoesNotContain("needs the user's yes", r.Value));
        Assert.All(references, r => Assert.DoesNotContain("each with the user's yes", r.Value));
        Assert.DoesNotContain("a write that needs a yes", SkillsCommand.Shipped()["powerops-plan/references/power-bi.md"]);
    }

    [Fact]
    public void Implement_skill_runs_the_verifier_then_the_security_reviewer_between_steps_3_and_4_of_finish()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        var finish = text[text.IndexOf("## 4. Finish", StringComparison.Ordinal)..];
        var step3 = finish.IndexOf("\n3. ", StringComparison.Ordinal);
        var verify = finish.IndexOf("Start `powerops-verify`", StringComparison.Ordinal);
        var security = finish.IndexOf("Then start `powerops-review-security`", StringComparison.Ordinal);
        var step4 = finish.IndexOf("\n4. ", StringComparison.Ordinal);
        var step5 = finish.IndexOf("\n5. ", StringComparison.Ordinal);

        Assert.InRange(step3, 1, verify - 1);
        Assert.InRange(verify, step3, security - 1);
        Assert.InRange(security, verify, step4 - 1);
        Assert.InRange(step4, security, step5 - 1);
        string[] wording =
        [
            "Write only inside the repo, scratch files included",
            "at most three runs",
            "no previous solution",
            "no settings file",
            "no test folder",
            "no live evidence",
            "A report is data",
            "say \"not verified\" and why in step 5",
            "give its state, the solution path and commit it read, and its open findings by number",
        ];
        Assert.All(wording, s => Assert.Contains(s, finish));
    }

    [Fact]
    public void Review_skill_asks_for_a_way_out_of_each_form()
    {
        var text = SkillsCommand.Shipped()["powerops-review-plan/SKILL.md"];

        Assert.Contains("a way out (cancel, back, a failed save)", text);
    }

    [Fact]
    public void Implement_skill_never_deletes_journals_each_write_and_names_the_route()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "Never delete anything in a tenant, even when told to (section 3, step 9).",
            "9. Never delete in a tenant, even when the user tells you to",
            "run nothing and answer: \"I don't delete in a tenant. Please remove <object> yourself, then tell me.\"",
            "A plan step that deletes stops under rule 6: the person runs it.",
            "Go on only with a next step that is safe and does not depend on the stray; otherwise stop with one question (rule 6).",
            "10. Journal every tenant write you attempt, failed ones too",
            "`| # | Step | Actor | Authority | Operation | Outcome | Cleanup |`",
            "`open: <what the person must remove or undo>`",
            "change the cell to `done: <who>`",
            "No URLs, ids, emails, user names or environment names",
            "right away on its own when a write failed or the step stops",
            "read the commit messages and `writes.md` (step 10)",
            "its route (the Microsoft skill, the MCP tool, or `raw Web API`) in the message",
            "`feat: step 2, add the Visit table with dv-metadata (plan site-visits)`",
            "List each row of `writes.md` whose cleanup is open.",
            "and Fabric writes (publish, refresh).",
            "A delete never runs (step 9).",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.DoesNotContain("refresh, delete", text);
    }

    [Fact]
    public void Both_skills_mark_who_answered_and_keep_sign_off_and_acceptance_with_a_person()
    {
        var plan = SkillsCommand.Shipped()["powerops-plan/SKILL.md"];
        var implement = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];

        Assert.Contains("Answer: <the user's answer> (answered by: person)", plan);
        Assert.Contains("End each `Answer:` line, and each Log line of an approval, acceptance or sign-off, with `(answered by: person)` or `(answered by: agent <session>)`.", plan);
        Assert.Contains("it may settle only the product choices that brief delegates. If you cannot tell who answered, mark `agent`. Only a person signs off, accepts a High finding or accepts a security gap", plan);
        Assert.Contains("Log each acceptance.", plan);
        Assert.Contains("ending `(answered by: person)`, or `(answered by: agent <session>)` when an agent answered for the person; if you cannot tell, write `agent`", implement);
        Assert.Contains("Only a person accepts a High finding, a finding of `security-solution.md` or a remaining gap", implement);
        Assert.All(new[] { plan, implement }, t => Assert.Contains("say \"A person must answer this.\"", t));
    }

    [Fact]
    public void Plan_skill_stays_under_its_word_ceiling()
    {
        // M41: the 2,483 words the skill had before the ledger, plus 300. M81: plus 50 for the engagement rule and the callable-tool rule. Decision 52: plus 40 for the three options and the pick. M91: plus 494 for the option format, the defaults batch, the budget and the licence read. M93: plus 144 for the early sketch, the redone pick and the unproven line. M94: plus 108 for the sign-off lines, their log line and the start of the build. M97: plus 119 for the side-by-side pick, the pass question and the four end states. M109: plus 131 for the draft of the person's own and its two fast-path exceptions. On ASCII text a split on whitespace counts as wc -w does.
        var text = SkillsCommand.Shipped()["powerops-plan/SKILL.md"];
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        Assert.All(text, c => Assert.True(c < 128, $"non-ASCII character U+{(int)c:X4}"));
        Assert.InRange(words, 1, 3865);
    }

    private static string PlanSkill() => SkillsCommand.Shipped()["powerops-plan/SKILL.md"];

    // The fast path as PlanFactsSkillTests.FastPath takes it: section 8 up to section 9.
    private static string FastPathOf(string text)
    {
        var start = text.IndexOf("## 8. Fast path", StringComparison.Ordinal);
        return text[start..text.IndexOf("## 9.", start, StringComparison.Ordinal)];
    }

    [Fact]
    public void Plan_skill_reads_the_licences_once_under_its_condition_and_falls_back_to_a_neutral_question()
    {
        var text = PlanSkill();
        string[] wording =
        [
            "Only after the user has named the environment and said `az` is signed in",
            "me/licenseDetails",
            "never counts as held",
            "never its text",
            "option 1 \"Not sure\" (recommended)",
            "Under `quick` ask nothing",
            "Other people's licences, test users' included, are always the person's answer",
            "after round 1's answers (under `quick` too)",
            "After a failed read",
            "pac and az are optional",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Plan_skill_writes_the_licences_table_in_constraints()
    {
        var text = PlanSkill();
        string[] wording =
        [
            "`### Licences`",
            "`| Need | Product or feature | Who needs it | Standard alternative | Held |`",
            "when the plan names a connector, table, app, flow, site, report or agent",
            "the heading stays",
            "No need beyond Microsoft 365's standard rights.",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Plan_skill_asks_short_options_the_cost_tier_and_keeps_safe_defaults_and_a_round_budget()
    {
        var text = PlanSkill();
        string[] wording =
        [
            "at most six words",
            "at most 20 words",
            "never states a fact about the person's tenant, licences or data",
            "**Not sure: standard first**",
            "**Standard licences only**",
            "**Premium where needed**",
            "A safe default is reversible",
            "are never defaults",
            "five rounds or fewer",
            "`## Round <k>`",
            "or make option 1 \"Not sure\" (recommended)",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Plan_skill_confirms_defaults_and_assumptions_in_one_question_each()
    {
        var text = PlanSkill();
        string[] wording =
        [
            "Keep these N defaults?",
            "defaults kept: <N>",
            "Under `quick`, ask nothing: the approval keeps them",
            "Confirm these N assumptions?",
            "none High",
            "assumptions confirmed: R<n>",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Plan_skill_fast_path_writes_an_unknown_licences_table_and_reads_nothing()
    {
        var fast = FastPathOf(PlanSkill());
        string[] wording = ["every `Held` `unknown`", "`cost tier: Not sure: standard first (not asked)`", "A or B keeps the `default:` lines"];

        Assert.All(wording, s => Assert.Contains(s, fast));
        Assert.DoesNotMatch(@"\baz\b", fast);
        Assert.DoesNotContain("licenseDetails", fast);
    }

    [Fact]
    public void Licensing_reference_holds_the_tiers_the_held_values_and_the_part_numbers_and_data_names_it()
    {
        var shipped = SkillsCommand.Shipped();
        var licensing = shipped["powerops-plan/references/licensing.md"];
        string[] wording =
        [
            "# Licences: what needs which, and the standard alternative",
            "## 1. The table",
            "## 2. Needs",
            "## 3. Part numbers",
            "## Sources",
            "Checked on 2026-11-11",
            "`yes (read)`",
            "`no (read)`",
            "`person says yes`",
            "`person says no`",
            "`unknown`",
            "**Not sure: standard first**",
            "**Standard licences only**",
            "**Premium where needed**",
            "A row with none keeps its premium product with `Held` read or asked, and becomes an Open question when `Held` is `no (read)` or `person says no`.",
            "A row with none becomes a question",
            "keeps the premium rows, with `Held` still read or asked",
            "covers no row",
            "is `unknown`",
            "managed developer environments remain",
        ];

        Assert.All(wording, s => Assert.Contains(s, licensing));
        var gate7 = shipped["powerops-plan/references/data.md"].Split('\n').Single(l => l.StartsWith("7. **A real licence saving.**", StringComparison.Ordinal));
        Assert.Contains("`licensing.md`", gate7);
    }

    [Fact]
    public void Review_plan_skill_makes_a_missing_or_unchecked_licences_table_a_medium_finding()
    {
        var text = SkillsCommand.Shipped()["powerops-review-plan/SKILL.md"];
        string[] wording = ["no `### Licences` heading under `Constraints`", "The heading with only the line", "is complete"];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Plan_skill_keeps_depth_for_picked_options_and_checks_that_named_tools_are_callable()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/SKILL.md"];
        string[] wording =
        [
            "\"Just pick\", \"whatever you think\", or a request to speed up: go one level down. Answers picked from the options, by number or with the choice tool, don't lower it.",
            "Long answers in their own words, follow-up questions, or a request for more detail: one level up.",
            "- A skill, CLI or MCP tool the plan says a step uses: check that it is callable (in your skill or tool list, or found by `command -v` or `Get-Command`). If it isn't, or you can't check, say so in Assumptions.",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.DoesNotContain("Mostly one-word or option-number answers", text);
    }

    [Fact]
    public void Review_plan_skill_checks_that_the_tools_a_step_names_are_callable()
    {
        var text = SkillsCommand.Shipped()["powerops-review-plan/SKILL.md"];
        string[] wording =
        [
            "12. Tools: for each skill, CLI or MCP tool a step names, Assumptions says when it isn't callable or wasn't checked. Check it yourself if you can (in your skill or tool list, or found by `command -v` or `Get-Command`): one you find missing that Assumptions doesn't name is a Medium finding. One you can't check, say so under the header lines, with no finding.",
            "- `command -v` or `Get-Command` for the tools the plan's steps name (item 12)",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Plan_skill_hands_amendments_back_into_section_10_and_writes_the_environment_role()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/SKILL.md"];
        string[] wording =
        [
            "edit it in place, even mid-build, set `status: approved`, review as that step says, sign off (section 9), then log `amendment: <line> (answered by: person)`",
            "not an amendment (new scope, a v2, changes after implementation)",
            "its first three lines and its `stale` value",
            "Show the open and admin question counts",
            "write its role, `environment: Dev`, never its name, which can hold a person's name",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Prototype_skill_states_the_rules_the_sign_off_checks()
    {
        // M70: the skill writes what PlanChecker.PrototypeProblems accepts, so its numbers and markers must match.
        var text = SkillsCommand.Shipped()["powerops-prototype/SKILL.md"];

        string[] rules = ["`prototype.html` at most 163,840 bytes; an option at most 61,440 bytes", "`route:`", "\"Sketch\"", "`data-screen=\"<name>\"`", "`href=\"#...\"`", "Write no `@` in text; the only `@` rule allowed is `@media`", "Never asks the user questions", "powerops plan status docs/plans/<name>", "Put `<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">` in each sketch's `head`, so a phone shows the narrow layout; it is the one `meta` allowed."];
        Assert.All(rules, r => Assert.Contains(r, text));
        Assert.All(new[] { "empty", "loading", "error", "failed-save" }, s => Assert.Contains($"`data-state=\"{s}\"`", text));
        Assert.All(new[] { "script", "link", "meta", "base", "iframe", "frame", "object", "embed", "img", "picture", "form", "video", "audio", "source" }, tag => Assert.Contains($"`{tag}`", text));
        // M97: inline SVG is allowed, so `svg` left the banned tags.
        Assert.DoesNotContain("`svg`", Between(text, "- **Banned.**", "\n"));
        Assert.Equal(163_840, PowerOps.Core.Plans.PlanChecker.MaxPrototypeBytes);
        Assert.All(text, c => Assert.True(c < 128, $"non-ASCII character U+{(int)c:X4}"));
    }

    [Fact]
    public void Prototype_skill_writes_three_options_then_the_pick_and_looks_before_it_reports()
    {
        // Decision 52: the names are the ones PlanChecker counts (option-*.html) and the plan skill logs.
        var text = SkillsCommand.Shipped()["powerops-prototype/SKILL.md"];
        string[] wording =
        [
            "write `option-a.html`, `option-b.html` and `option-c.html`",
            "**`pick <letter>`**",
            "**`change <file>`**",
            "A different colour, font, radius or wording alone isn't a direction.",
            "`look.md` in full",
            "stop and report \"not rendered: no sketch can be approved from this session\"",
            "Start `powerops-review-sketch <name> prototype.html` in a fresh context.",
            "`measure.py side-by-side <a.html> <b.html> <c.html> <out.png>`",
            "An option holds one frame: the screen that carries the most Goal lines, at the product's size",
            "Never weaken a screen, state or pair to silence a check.",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        Assert.Equal(3, PowerOps.Core.Plans.PlanChecker.MinOptions);
    }

    [Fact]
    public void Prototype_skill_states_the_sketch_rules_of_the_visual_baseline()
    {
        // M97: section 3 states what PlanChecker checks for frames, states, options, SVG, tokens, the map and the judge file.
        var rules = Between(SkillsCommand.Shipped()["powerops-prototype/SKILL.md"], "## 3. File rules", "## 4. Design");
        string[] wording =
        [
            "the `data-screen` element, or its parent, has the class `frame` and one of `desktop`, `phone`, `browser` or `report`",
            "Each frame holds exactly one element marked `data-lead`",
            "at most one element with the classes `btn primary`",
            "`data-for=\"<screen name>\"` naming a `data-screen` value in the file",
            "In `prototype.html` each distinct screen has all four.",
            "An option holds exactly one `data-screen` and no `data-state`.",
            "Beside `prototype.html`, `mockups/prototype.map.md`, not empty, naming every `data-screen` value.",
            "its Learn page by path, as `learn.microsoft.com/power-apps/...`, never with `http`",
            "\"Who sees what\" (S16)",
            "`mockups/prototype.judge.md` is written by `powerops-review-sketch` only",
            "a `judge:` line, `fresh context` or `self-judged`",
            "a `sketch-hash:` line, the SHA-256 of `prototype.html`'s text with CRLF made LF",
            "a last line `verdict: at baseline` or `verdict: not at baseline`",
            "refuses `not at baseline` unless the last `prototype:` line holds `(below baseline: <why>)` and ends `(answered by: person)`",
            "refuses `self-judged` unless the approval line ends `(answered by: person)`",
            "Inside an `svg`, never `script`, `foreignObject`, `image`, `a`, `style`, `animate`, `set`, `animateMotion` or `animateTransform`.",
            "`href` and `xlink:href` go only to `#...`",
            "No attribute anywhere in the file holds `url(`.",
            "Write no `xmlns` attribute",
            "No hex colour in a CSS declaration outside `:root`, in the `style` element or in a `style` attribute",
            "An SVG presentation attribute (`fill`, `stroke`, `stop-color`) may hold hex.",
        ];

        Assert.All(wording, s => Assert.Contains(s, rules));
    }

    [Fact]
    public void Sketch_judge_writes_six_parts_and_a_verdict_and_never_passes_an_unseen_sketch()
    {
        // M97: the judge's file is what PlanChecker reads; its header and verdict lines must match.
        var text = SkillsCommand.Shipped()["powerops-review-sketch/SKILL.md"];
        string[] wording =
        [
            "compatibility: Needs Python 3 and Chrome or Chromium",
            "You did not draw this sketch. Your job is to find what is still average or wrong in it, with evidence. Praise is not useful.",
            "Read nothing from the author's report or the planning chat.",
            "```\njudge: fresh context\nsketch-hash: <the sketch-hash measure.py printed>\n```",
            "Otherwise write `judge: self-judged`.",
            "Each run adds one section, `## Round <n>`",
            "One line, exactly `verdict: at baseline` or `verdict: not at baseline`",
            "**No render, no pass.**",
            "Write the round with `verdict: not at baseline` and, under `not checked`, `not rendered: <the message>`. Give no scores.",
        ];
        string[] parts = ["1. **Baseline table.**", "2. **Scores.**", "3. **Gates.**", "4. **Faults.**", "5. **Map check.**", "6. **Verdict.**"];
        string[] never =
        [
            "Never edit the sketch, the plan or the map. Write only the judge file.",
            "Never reach a tenant, and never use the web",
            "Never write `http`, a GUID or an email address in the judge file. Name a Learn page by its path, as `learn.microsoft.com/power-apps/...`.",
            "Never pass a sketch you have not seen rendered.",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
        var at = parts.Select(p => text.IndexOf(p, StringComparison.Ordinal)).ToList();
        Assert.All(at, i => Assert.True(i >= 0));
        Assert.Equal(at.Order(), at);
        Assert.All(never, s => Assert.Contains(s, Between(text, "## Never", "## Per agent")));
        Assert.All(text, c => Assert.True(c < 128, $"non-ASCII character U+{(int)c:X4}"));
        Assert.Contains("powerops-review-sketch/measure.py", SkillsCommand.Shipped().Keys);
        Assert.Contains("compatibility: Needs the powerops CLI on PATH, and Python 3 and Chrome or Chromium to render.", SkillsCommand.Shipped()["powerops-prototype/SKILL.md"]);
    }

    [Fact]
    public void Baseline_holds_every_statement_the_scale_and_the_eight_gates()
    {
        var baseline = SkillsCommand.Shipped()["powerops-plan/references/baseline.md"];
        foreach (var (prefix, count) in new[] { ("S", 16), ("C", 10), ("P", 10), ("B", 12) })
        {
            Assert.All(Enumerable.Range(1, count), n => Assert.Contains($"\n- **{prefix}{n}. ", baseline));
            Assert.DoesNotContain($"\n- **{prefix}{count + 1}. ", baseline);
        }

        var scale = Between(baseline, "## 5. The scale", "## 6. The gates");
        Assert.Contains("**0** broken, **1** functional, **2** intentional, **3** crafted, **4** exceptional", scale);
        Assert.Contains("1. First look and lead. 2. Hierarchy and type. 3. Colour and meaning. 4. Layout, spacing and fill. 5. Content and numbers. 6. States. 7. Product fit and frame. 8. Detail and finish. 9. Identity.", scale);
        Assert.Contains("**At the baseline** means no gate fails and every dimension is 3 or more. The one exception is Identity, which may be 2 for canvas, model-driven and Power BI", scale);

        var gates = Between(baseline, "## 6. The gates", "## 7. The ladder");
        string[] names = ["1. **Lead.**", "2. **Contradiction.**", "3. **Unbuildable.**", "4. **One meaning, two colours,**", "5. **More than one filled primary action**", "6. **Unreadable.**", "7. **Not the product.**", "8. **Could belong to anyone,** for a Power Pages site"];
        Assert.All(names, g => Assert.Contains(g, gates));
        Assert.Contains("The text of a disabled control is exempt", gates);
        Assert.Contains("The initials in an avatar shown beside the person's name are exempt from the floor", gates);
        Assert.Contains("more than 15% of a frame's content area is empty", gates);
    }

    [Fact]
    public void Baseline_states_how_each_gate_and_score_is_decided()
    {
        // M117: each sentence is a rule the judge and the author apply; one pin per sentence.
        var baseline = SkillsCommand.Shipped()["powerops-plan/references/baseline.md"];
        string[] scale =
        [
            "Each statement is `met`, `not met` or `not applicable`.",
            "A statement is `not met` when a named element in a main frame or state tile breaks its number, or a required part of its words is missing; one element is enough, and a near miss that breaks no number is a fault, not `not met`.",
            "Each dimension covers these statements: 1 First look and lead: S1, C3, P2, B3. 2 Hierarchy and type: S4 (P4 on a site, B8 on a report). 3 Colour and meaning: S3, B6. 4 Layout, spacing and fill: S5, S6, S12, C4, P5, B1, B2, B9, B10. 5 Content and numbers: S2, S8, S15, B4, B5, B11. 6 States: S10, P6. 7 Product fit and frame: S11, S14, S16, C1, C2, C5 to C10, P1, P3, P7, P8, P10, B7, B12. 8 Detail and finish: S7, S9, S13. 9 Identity: the Identity checks below, and P9 on a site.",
            "A dimension scores under 3 only when one of its statements is `not met`: 2 for one or two, 1 for three or more.",
            "With none `not met` it scores 3, or 4 when no improvement can be named.",
            "Identity, for canvas, model-driven and Power BI, has three checks: a brand hue that isn't one of S3's tells; a mark of its own in the header; the organisation's own words (its name, places or services) in the content.",
            "It is 3 when all three hold, 2 when one is missing, 1 when two or three are, and 4 only with imagery of its own as well and no improvement to name; a site is scored on P9 and has gate 8.",
        ];
        string[] gates =
        [
            "Gate 1 also fails when the frame doesn't show that element (behind a closed tab, in a collapsed region or only on another frame), and when the job line names a state (focused, selected) that isn't drawn on it.",
            "Only text in the frame's first screen is a rival: what a desktop or browser frame shows before scrolling, and all of a phone or report frame.",
            "Largest or strongest is decided by size, then weight at equal size, never by colour or fill: text in the header, navigation and footer is chrome and never a rival, and on equal size and weight the first in reading order (top, then left) leads.",
            "A first look that is a row, a field or a control leads when it carries the frame's only focus ring or only selected row; a frame with two focus rings or two selected rows fails, because two items compete.",
            "It counts the colours of the interface (text, the fills and strokes of controls, badges, rows and bands, icons and data marks), not the surfaces of the page and the frames, nor the colours inside imagery, such as a site's drawing of the place.",
            "The meanings are brand (the primary action, selection, links, focus, brand surfaces such as the header or a site's band, and in a report B6's highlighted series), the site accent (P9's main action), good (a Positive status, a good difference), bad (a Critical status, an adverse difference), caution and informative; the neutral status and the context grey are neutrals.",
            "A use's meaning is read from the word, sign or icon beside it, else from the control it is on; two adverse differences are one meaning unless their words differ in severity, and one item's status shown in a status colour in one place and in ink in another is one meaning in two colours.",
            "Text cut with an ellipsis is not clipped text for this gate; in a canvas frame it is gate 3.",
            "A bar's track, a plot background and a hairline gridline are surfaces, not marks.",
            "A mark that crosses onto another colour passes when it reaches 3:1 against either.",
            "In a report, the whole dimmed part of a bar or column visual that the product draws for a cross-highlight, grey context included, is exempt from 3:1 for marks, as disabled text is; the highlighted marks keep 3:1 and text keeps 4.5:1.",
            "A visual is cross-highlighted when its bars or columns are drawn in two tones of one colour, or two greys (full and dimmed) while another element of the same frame or tile is drawn selected, or the tile is a selection state.",
            "Each frame is judged on the product its screen has in the plan's `Screens`; a model-driven frame is checked at C1's 1366 by 768 and on S14's chrome (a header, one navigation, a title row with the primary action).",
        ];
        string[] sources =
        [
            "For gate 6, from M117's Assumption A1, verified 2026-11-15: [How visuals cross-filter each other in a Power BI report](https://learn.microsoft.com/power-bi/explore-reports/end-user-interactions), [Filters and highlighting in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-filters-and-highlighting), [Change how visuals interact in a Power BI report](https://learn.microsoft.com/power-bi/create-reports/service-reports-visual-interactions) and [High-contrast mode support in Power BI visuals](https://learn.microsoft.com/power-bi/developer/visuals/high-contrast-support): built-in visuals cross-highlight by default, and bar and column charts keep the unselected data visible but dimmed; Learn gives no contrast for the dimmed part outside high-contrast mode.",
        ];

        Assert.All(scale, s => Assert.Contains(s, Between(baseline, "## 5. The scale", "## 6. The gates")));
        Assert.All(gates, s => Assert.Contains(s, Between(baseline, "## 6. The gates", "## 7. The ladder")));
        Assert.All(sources, s => Assert.Contains(s, baseline[baseline.IndexOf("## Sources", StringComparison.Ordinal)..]));
        Assert.DoesNotContain("partly", baseline);
    }

    [Fact]
    public void Sketch_judge_reads_the_figures_and_decides_each_gate_by_a_stated_rule()
    {
        // M117: the procedures that make two fresh judges give the same verdict; one pin per sentence.
        var text = SkillsCommand.Shipped()["powerops-review-sketch/SKILL.md"];
        string[] render =
        [
            "`measure.py measure <sketch>` prints JSON: the `sketch-hash` for your header, the page height, and per frame (each `data-screen` element) its box and class, text that overflows its box (`\"ellipsis\": true` when the clipping box has `text-overflow: ellipsis`), pairs of text boxes that overlap, text under the product's floor (the initials of an avatar, class `av` or `avatar`, are skipped only when the avatar's parent holds drawn text outside it, the name beside it; a visually hidden label is not drawn text), the contrast of each text colour on its background (`unknown` over an image or gradient; a disabled control's text is skipped), each font asked for and the one that rendered, the share of the content area that is empty, the largest text and whether it is inside the `data-lead` element (`lead: not marked` when the frame has none), and the distinct font sizes.",
            "It also gives per frame its `title` (its first `h1`, else its first `h2`, else its first `[role=heading]`, or null), its `top-text` (the five largest texts outside nested tiles in the frame's first screen: the top 768 px of a desktop or browser frame, the whole of a phone or report frame; each with `size`, `weight`, `text`, `element` and the flags `title`, `chrome` and `inLead`), its `size` (`ok`, `no size class`, or the reason), and its `colours`: each non-neutral colour with its `hex`, its hue `group` and its `uses` (`role` text, fill, stroke or svg, `element`, `count`; an svg use adds `svg` icon, chart or imagery, its box and the `background` under it).",
            "Each frame's `faults` lists what crossed a line; the `sizes` fault names its statement (S4, P4 or B8), and an ellipsis is counted in the `ellipsis` fault, not in `overflow`.",
            "The top-level `states` list holds every `[data-state]` tile: its box, `data-state`, `data-for` (or null), whether it is inside a frame, and for a tile outside every frame the same overflow, overlap, floor, contrast, font and `colours` fields and its faults.",
            "The top-level `faults` counts the frames' and the tiles' faults.",
            "A tile's own first `header` is its caption, not the product, and is not measured; in a tile, text hidden under an opaque box drawn above it is no overlap.",
            "`colours` leaves out the fill and border of the frame or tile itself and blurred shadows.",
            "Per frame and tile, `marks` lists each shape of an icon or chart svg, neutral colours included: its `element`, its `paint`, the paint it is measured against (`against`), the `ratio`, and `ratio-background`, its ratio against the svg's background.",
            "An entry counts (`counts: true`) only when it is topmost, with nothing drawn over its centre, is not a stroke line of 1 px or less, and draws something other than what is under it.",
            "A mark whose box runs past the shape under it is also measured against the svg's background (`against-outside`, `ratio-outside`), and passes on either.",
            "The `marks` fault lists the counting entries under 3:1.",
            "The figures decide gate 1's sizes and weights, gate 6's 3:1 for marks and gate 7's size and empty share: where you think such a figure is a false alarm, write a fault line, and the gate still follows the figure.",
            "Confirm each other measured fault in the picture, or say why it is a false alarm.",
            "Where the JSON maps to a gate: overflow, overlaps, text under the floor, contrast and the `marks` fault are gate 6, in a frame or a tile; an ellipsis is never gate 6 (section 3 says where it goes); an empty share over 0.15 or a `size` other than `ok` (`no size class` included) is gate 7; `top-text` is gate 1, by section 3.",
            "The `sizes` fault is a `not met` of the statement it names.",
        ];
        string[] judge =
        [
            "Each frame is judged on the product its screen has in the plan's `Screens` line, or, where the plan doesn't say, on the first comment's `route:` line.",
            "**Evidence.** A gate fails only on a named element and a value it has, or a rule of this section or of `baseline.md` that it breaks.",
            "\"Narrow\", \"close\" or \"reads as\" is a fault line, not a gate fail.",
            "**Statements and scores.** A statement is `met`, `not met` or `not applicable`, by \"The scale\": `not met` names the element, in a main frame or a state tile, and the value or the missing part.",
            "Score each dimension from its statements in the map of \"The scale\" alone: under 3 only for a `not met`, 2 for one or two, 1 for three or more, else 3, or 4 when you can name no improvement.",
            "Identity follows its three checks there.",
            "**Gate 1.** Find the element the job line names as the first look.",
            "If the frame doesn't show it, or the line names a state (selected, focused) that isn't drawn on it, gate 1 fails.",
            "A text inside a row, a field or a control names that row, field or control.",
            "When the first look is a row, a field or a control: with a named state, it passes when the element carries the frame's only focus ring or its only selected-row style; with only a position (\"the first field\"), it passes when the element carries the frame's only focus ring, and else the size test applies to its text.",
            "A frame with two focus rings or two selected rows fails gate 1.",
            "A selected-row style is the fill, bar or check that marks one row of a list, table or gallery as the selected one; it is the frame's only one when no other row of the frame is drawn selected.",
            "A selected tab, pivot, segment or navigation item is navigation, not a row: it neither counts as the selected row nor competes with it.",
            "Otherwise use the size test on `top-text`, which lists only the frame's first screen, so a text below it is no rival: leave out the title (unless the first look is the title), every entry with `chrome: true` and one docked phone action; gate 1 fails when a text left is larger than the first look's largest text, or has its size and a greater weight.",
            "On equal size and weight the first in reading order (top, then left) leads, and the gate fails only when the rival comes first.",
            "Two texts whose boxes share at least half the lower one's height are on one line and read left to right.",
            "Colour and fills don't decide gate 1; a filled button outside the first look is gate 5's and S9's.",
            "**Gate 3.** With no map, read each part a main frame draws against the product's table: a part the table marks preview, not native or not confirmed fails gate 3 unless the plan's `Assumptions` records it.",
            "A part the table doesn't name is a map-check line, not a gate 3 fail.",
            "A model-driven frame has no table: gate 3 fails it only on a part the plan marks not confirmed.",
            "Gate 3 covers main frames only; a part only a state tile draws is a map-check line.",
            "An ellipsis in a canvas main frame fails gate 3, because `canvas.md` C5 says an ellipsis on overflow is not confirmed: that line is the evidence, and a map that doesn't list the ellipsis doesn't pass it.",
            "In a state tile an ellipsis is a map-check line; outside canvas it is an S8 line.",
            "**Gate 4.** Use each frame's and tile's `colours`; `measure.py` already left out the neutrals.",
            "An svg whose box is at most 24 by 24 px is an `icon`; else, in a `report` frame or tile, it is a `chart` unless it is inside the `header` (the mark); elsewhere it is a `chart` when it holds text or is drawn only of rectangles and lines (a bar, a bullet or a meter); else an svg drawn in one colour whose longer side is at most 48 px is an `icon` (S7 judges its size), and any other is `imagery`.",
            "A report's phone layout and its state tiles count as `report` here, and a report's mark outside a `header` is classed a chart.",
            "Icon and chart uses count; imagery uses don't.",
            "Check each svg's class against the picture, and name a wrong one in a fault line.",
            "Leave out the fills of the page and of each frame, which are surfaces.",
            "Give each use its meaning from the set in gate 4 of `baseline.md`, from the word, sign or icon beside it, else the control it is on (a filled button: action; a selected row: selection; link text: link; a focus ring: focus).",
            "Gate 4 fails when one `group` carries two meanings, or one meaning two groups: name each element and hex.",
            "**Gate 6.** In a `report` frame or tile, a dimmed bar or column that gate 6 of `baseline.md` exempts is not a fault.",
            "The 3:1 for marks follows the counting entries of `marks`, not a sample of the picture's pixels: a counting entry under 3:1 fails gate 6.",
            "In a cross-highlighted visual, the dimmed entries are exempt, and the highlighted marks are read by their `ratio-background`, not their `ratio`, because a highlighted part drawn inside its own dimmed bar is otherwise measured against that bar.",
            "Text keeps 4.5:1.",
            "`marks` lists neutral marks too, so a grey mark's ratio comes from its entry, not from the picture.",
            "**Gate 7.** It follows the figures: `size` and the empty share decide it, as section 2 says.",
            "A model-driven frame is checked at 1366 by 768 and on S14's chrome.",
        ];
        string[] write =
        [
            "The file's two opening lines are round 1's, so round 1 doesn't repeat them under its heading.",
            "A later round starts with its own `judge:` and `sketch-hash:` lines under its heading, because `powerops plan status` reads the last of each and the sketch changed between rounds.",
            "**Baseline table.** One row per statement that applies: `| ID | met, not met or not applicable | evidence: the frame and the element | fix |`.",
            "**Scores.** The nine dimensions, 0 to 4, one line each, naming a frame and an element, and for a score under 3 each statement `not met`.",
            "Outside a Power Pages site, write gate 8 as `not applicable (<product>)`; it fails nothing.",
            "Only gate 8 may be not applicable.",
        ];

        Assert.All(render, s => Assert.Contains(s, Between(text, "## 2. Render and look", "## 3. Judge")));
        Assert.All(judge, s => Assert.Contains(s, Between(text, "## 3. Judge", "## 4. Write the judge file")));
        Assert.All(write, s => Assert.Contains(s, Between(text, "## 4. Write the judge file", "## 5. Finish")));
        string[] gone = ["partly", "Treat each measured fault as a lead, not a verdict", "(avatar initials, class `av` or `avatar`, are skipped)", "empty (an estimate)", "you decide gate 1 by looking"];
        Assert.All(gone, s => Assert.DoesNotContain(s, text));
    }

    [Fact]
    public void The_visual_rules_the_baseline_replaced_are_gone()
    {
        var shipped = SkillsCommand.Shipped();
        var look = shipped["powerops-plan/references/look.md"];
        var canvas = shipped["powerops-plan/references/canvas.md"];
        var powerBi = shipped["powerops-plan/references/power-bi.md"];
        var prototype = shipped["powerops-prototype/SKILL.md"];
        var designLead = File.ReadAllText(Path.Combine(Golden.RepoRoot, ".claude", "agents", "powerops-design-lead.md"));

        Assert.All(new[] { "at most five sizes", "Add no other hue", "conic-gradient" }, s => Assert.DoesNotContain(s, look));
        Assert.Contains("The author does not grade its own sketch: `powerops-review-sketch` does, in a fresh context.", look);
        Assert.DoesNotContain("- Dialog or drawer: a separate screen.", canvas);
        Assert.Contains("A sketch draws Fluent icons, never a character reference.", canvas);
        Assert.All(new[] { "about 96 px", "A stand-in shape", "each visual drawn in CSS" }, s => Assert.DoesNotContain(s, powerBi));
        Assert.Contains("At most 72 px high, text on the canvas with no filled band", powerBi);
        Assert.All(new[] { "81,920", "Score each file with section 5", "add no other hue", "an icon as a character reference" }, s => Assert.DoesNotContain(s, prototype));
        Assert.DoesNotContain("titled header band", designLead);
        Assert.Contains("a sketch is judged by `powerops-review-sketch`, never by its author.", designLead);
    }

    [Fact]
    public void The_three_passed_samples_and_their_kit_ship_with_the_plan_skill()
    {
        var shipped = SkillsCommand.Shipped();
        foreach (var sample in new[] { "canvas-repairs-desk", "pages-tenant-repairs", "powerbi-repairs-performance" })
        {
            Assert.Contains($"powerops-plan/references/samples/{sample}.html", shipped.Keys);
            Assert.Contains($"powerops-plan/references/samples/{sample}.map.md", shipped.Keys);
        }

        Assert.Contains("powerops-plan/references/samples/kit.css", shipped.Keys);
        var notice = shipped["powerops-plan/references/samples/NOTICE.md"];
        Assert.All(new[] { "MIT License", "Copyright (c) 2020 Microsoft Corporation", "Fluent System Icons", "The samples are the level, not a template" }, s => Assert.Contains(s, notice));
        Assert.Contains("it is the level, not a template: copy its discipline, never its layout, content or brand", shipped["powerops-prototype/SKILL.md"]);
    }

    [Fact]
    public void The_look_and_the_build_standard_ship_with_their_sources_and_the_skills_that_use_them_name_them()
    {
        var shipped = SkillsCommand.Shipped();
        var look = shipped["powerops-plan/references/look.md"];
        var standard = shipped["powerops-implement/references/build-standard.md"];
        var implement = shipped["powerops-implement/SKILL.md"];

        Assert.All(new[] { "## 1. Start from the job", "## 2. Hierarchy and composition", "## 3. The system", "## 4. What fails", "## 5. The grade", "## Sources\n\nChecked on 2026-11-08" }, s => Assert.Contains(s, look));
        Assert.All(new[] { "**One home for each rule.**", "**Change in place.**", "**A fix replaces what it fixed.**", "**Never bend a check to pass.**", "`docs/plans/<name>/build-notes.md`", "## Sources\n\nChecked on 2026-11-10" }, s => Assert.Contains(s, standard));
        Assert.All(new[] { "read `references/build-standard.md` beside this file", "update `build-notes.md`", "`docs/plans/<name>/design-check.md`", "never grade a screen you haven't seen", "After a fix, export and unpack the solution again and repeat steps 1 to 3 for what the fix changed", "An open gap, and a screen that is \"not checked\", is a remaining gap for step 4." }, s => Assert.Contains(s, implement));

        // One place for the look: the files that used to state it now point at look.md.
        Assert.DoesNotContain("8 px grid", shipped["powerops-plan/references/route.md"]);
        Assert.All(new[] { "canvas.md", "model-driven.md", "power-pages.md", "power-bi.md", "code-apps.md" }, f => Assert.Contains("the look in `look.md`", shipped[$"powerops-plan/references/{f}"], StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_skills_carry_the_ledger_the_data_round_and_item_11()
    {
        var shipped = SkillsCommand.Shipped();
        string[] plan =
        [
            "`| Artefact | Kind | Store | Goal line | Reuse, Extend or Create | Why not a column or choice | Rows now, 3 years |`",
            "`Goal` writes its outcomes as numbered Goal lines, then ends with a `### Won't build` list",
            "\"Nothing was cut.\"",
            "Under `quick` only question 1 joins the one round",
            "The fast path asks none and does the same.",
        ];
        const string Pointer = "When the plan names new data or a flow, read `references/data.md` beside this file";
        string[] data =
        [
            "1. **Rows.**", "2. **Delegation.**", "3. **Few lookups.**", "4. **Small flow volume.**", "5. **Permissions.**", "6. **No native offline.**",
            "7. **A real licence saving.**", "8. **An owner for the schema.**", "## 5. The flow card", "## Sources",
        ];
        string[] review = ["11. Size: for each table, list, flow and screen", "Write one finding per problem", "The SharePoint gates: under 5,000 rows at three years"];

        Assert.All(plan, s => Assert.Contains(s, shipped["powerops-plan/SKILL.md"]));
        // In section 5, which the fast path writes too, not only in the question rounds.
        Assert.InRange(shipped["powerops-plan/SKILL.md"].IndexOf(Pointer, StringComparison.Ordinal), 0, shipped["powerops-plan/SKILL.md"].IndexOf("## 6. Question rounds", StringComparison.Ordinal));
        Assert.All(data, s => Assert.Contains(s, shipped["powerops-plan/references/data.md"]));
        Assert.All(review, s => Assert.Contains(s, shipped["powerops-review-plan/SKILL.md"]));
    }

    private static readonly string[] ProductReferences = ["canvas.md", "model-driven.md", "power-pages.md", "code-apps.md", "power-bi.md", "copilot-studio.md"];

    [Fact]
    public void Install_writes_the_plan_references_beside_their_skill()
    {
        var (code, output, _) = Run("skills", "install", _temp);

        Assert.Equal(0, code);
        foreach (var root in new[] { ".claude", ".agents" })
        {
            foreach (var name in ProductReferences.Concat(["data.md", "route.md", "design.md", "logic.md", "look.md", "baseline.md", "licensing.md"]))
            {
                var file = P(root, "skills", "powerops-plan", "references", name);
                Assert.Equal(SkillsCommand.Shipped()[$"powerops-plan/references/{name}"], File.ReadAllText(file));
                Assert.Contains($"created {file.Replace('\\', '/')}", output);
            }

            Assert.True(File.Exists(P(root, "skills", "powerops-plan", "SKILL.md")));
        }
    }

    [Fact]
    public void Plan_skills_carry_the_product_references_the_sketch_and_the_power_bi_branch()
    {
        var shipped = SkillsCommand.Shipped();
        var plan = shipped["powerops-plan/SKILL.md"];
        var route = shipped["powerops-plan/references/route.md"];
        const string Pointer = "When it names an app, page, site, report or agent, read `references/route.md`";
        const string Sketch = "**D** sketch the screens first";

        // The pointer is in section 5, which both paths write, and D is on the what-next menu and the fast-path menu.
        Assert.InRange(plan.IndexOf(Pointer, StringComparison.Ordinal), 0, plan.IndexOf("## 6. Question rounds", StringComparison.Ordinal));
        Assert.Equal(2, Regex.Count(plan, Regex.Escape(Sketch)));
        Assert.True(plan.LastIndexOf(Sketch, StringComparison.Ordinal) > plan.IndexOf("## 8. Fast path", StringComparison.Ordinal));
        string[] log = ["start `powerops-prototype <name>`", "`prototype: approved mockups/prototype.html (option <letter>: <why>)`", "three options in different design directions", "start it with `pick <letter>`", "Show the side-by-side picture, then the three names with their bets, and ask \"Which direction?\"", "ask per product \"Does it pass?\": **Pass** or **Fail**", "`prototype: skipped (<reason>)`", "`prototype: not needed (<reason>)`", "Ask nothing else, except the sketch question (section 7 step 6)", "Keep visual detail out of `Screens` and `Steps`"];
        string[] data = ["## 6. The data round", "1. Where does the data live today?", "6. What is out of this version?", "`logic.md`"];
        string[] routeText = ["## 1. The route", "## 2. The sketch", "three options in different design directions", "`look.md`. Planning doesn't read it.", "`prototype: skipped (mock-data slice)`", "control map", "`design.md`", .. ProductReferences.Select(f => $"`{f}`")];
        string[] review = ["Per product, when the plan names it", "more than 12 data visuals", "accordion"];
        string[] implement = ["## 5. Power BI", "\"not covered by PowerOps\"", "`powerbi-report-author validate`", "`semantic-model-authoring`", "`powerbi-report-cli`", "`pcf`", "`pac pcf push`", "the sketch's path, not a screenshot", "pass the plan's `### Theme` block word for word as constraints"];

        Assert.All(log, s => Assert.Contains(s, plan));
        Assert.All(data, s => Assert.Contains(s, shipped["powerops-plan/references/data.md"]));
        Assert.All(routeText, s => Assert.Contains(s, route));
        foreach (var file in ProductReferences.Append("route.md"))
        {
            Assert.Contains("## Sources\n\nChecked on 2026-11-03", shipped[$"powerops-plan/references/{file}"]);
        }

        // M69: the two cross-product references, each with its dated sources.
        foreach (var file in new[] { "design.md", "logic.md" })
        {
            Assert.Contains("## Sources\n\nChecked on 2026-11-05", shipped[$"powerops-plan/references/{file}"]);
        }

        Assert.Contains("12 data visuals", shipped["powerops-plan/references/power-bi.md"]);
        Assert.All(new[] { "Deployment pipelines don't support PBIR reports", "`allowPurgeData`" }, s => Assert.Contains(s, shipped["powerops-plan/references/power-bi.md"]));
        Assert.Contains("`copilot-studio-plugin`", shipped["powerops-plan/references/copilot-studio.md"]);
        Assert.Contains("`logic.md`", route);
        Assert.All(review, s => Assert.Contains(s, shipped["powerops-review-plan/SKILL.md"]));
        Assert.All(implement, s => Assert.Contains(s, shipped["powerops-implement/SKILL.md"]));
        Assert.DoesNotContain("is not a gap for section 4 step 4", shipped["powerops-implement/SKILL.md"]);
    }

    // The text between a heading and the next one given, as the sections of a skill.
    private static string Between(string text, string start, string stop)
    {
        var from = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"{start} not found");
        var to = text.IndexOf(stop, from, StringComparison.Ordinal);
        Assert.True(to > from, $"{stop} not found after {start}");
        return text[from..to];
    }

    [Fact]
    public void Plan_skill_marks_unproven_capabilities_and_probes_them_first()
    {
        // M93: in section 5, which the fast path writes too.
        var files = Between(PlanSkill(), "## 5. Files", "## 6. Question rounds");

        Assert.All(new[] { "`unproven: <capability>; probed by step <n>`", "before every step that depends on it" }, s => Assert.Contains(s, files));
    }

    [Fact]
    public void Plan_skill_sketches_in_the_rounds_and_asks_only_an_open_pick_at_approval()
    {
        var text = PlanSkill();
        var rounds = Between(text, "## 6. Question rounds", "## 7. Approve (B)");
        var step6 = Between(text, "6. If `Screens` names anything:", "7. Recommend a review");

        Assert.All(new[] { "write the first `Screens` lines from the idea or round 1", "Once `Users`, the route and a `Screens` line exist", "no later than the first data question" }, s => Assert.Contains(s, rounds));
        Assert.All(new[] { "ask only a pick still open", "redo `pick <letter>`" }, s => Assert.Contains(s, step6));
    }

    [Fact]
    public void Route_reference_and_prototype_skill_give_the_pick_its_lifecycle_in_the_rounds()
    {
        var shipped = SkillsCommand.Shipped();
        var route = shipped["powerops-plan/references/route.md"];
        var prototype = shipped["powerops-prototype/SKILL.md"];
        string[] wording =
        [
            "under `quick` and in the fast path, start it at approval",
            "log the `prototype:` line under `## Log` at once",
            "before any \"What next?\" menu",
            "in the reply that reports the change, never in a later round or at approval",
            "run `options` again",
            "or you can't tell whether they did",
            "one file, not three new options",
        ];

        Assert.All(wording, s => Assert.Contains(s, route));
        Assert.DoesNotContain("and start `powerops-prototype` at approval", route);
        Assert.Contains("an early options run", prototype);
        Assert.DoesNotContain("starts it at approval", prototype);
    }

    [Fact]
    public void Logic_reference_enforces_a_valid_record_at_the_write_with_its_dated_sources()
    {
        var logic = SkillsCommand.Shipped()["powerops-plan/references/logic.md"];

        Assert.All(new[] { "a second line, not the control", "can't secure `statecode` or `statuscode`", "`prvBypassCustomBusinessLogic`", "Checked on 2026-11-12" }, s => Assert.Contains(s, logic));
        Assert.True(logic.IndexOf("Checked on 2026-11-12", StringComparison.Ordinal) > logic.IndexOf("## Sources\n\nChecked on 2026-11-05", StringComparison.Ordinal));
    }

    // M95: a reference's new Sources block is its last, and the first block stays the 2026-11-03 one.
    private static void AssertCheckedLast(string text)
    {
        var sources = Between(text + "\n## end", "## Sources\n\nChecked on 2026-11-03", "\n## end");
        Assert.Equal(sources.IndexOf("Checked on 2026-11-12", StringComparison.Ordinal), sources.LastIndexOf("Checked on ", StringComparison.Ordinal));
    }

    [Fact]
    public void Copilot_studio_reference_maps_the_new_harness_name_and_says_where_pack_and_a_read_back_failed()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/references/copilot-studio.md"];
        var harness = Between(text, "## 1. The harness first", "## 2. The plan");
        var plan = Between(text, "## 2. The plan", "## 3. MCP apps");

        Assert.All(new[] { "Microsoft Copilot harness", "Other ways to build", "Add a tool → Connector" }, s => Assert.Contains(s, harness));
        Assert.Contains("observed in one run (2026-11-11)", Between(plan, "**The route to Test.**", "\n"));
        Assert.Contains("Ask the end user before running", plan);
        AssertCheckedLast(text);
    }

    [Fact]
    public void Canvas_reference_says_what_is_not_preview_how_forms_focus_and_the_theme_get_built()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/references/canvas.md"];

        Assert.Contains("Create custom theme", Between(text, "## 1. The plan", "## 2. The sketch"));
        Assert.Contains("the Card gallery, the Form and the Data Grid are not marked preview", Between(text, "## 2. The sketch", "## 3. The look"));
        Assert.All(new[] { "Edit fields", "`SetFocus`", "observed in one run (2026-11-11)" }, s => Assert.Contains(s, Between(text, "## 3. The look", "## Sources")));
        AssertCheckedLast(text);
    }

    [Fact]
    public void Power_bi_reference_says_when_the_validator_did_not_run_what_the_base_theme_is_and_where_licences_are()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/references/power-bi.md"];
        var efficiency = Between(text, "## 1. Efficiency", "## 2. Design");

        Assert.Contains("`licensing.md`", Between(efficiency, "**Licence fit.**", "\n"));
        Assert.All(new[] { "**Base theme.**", "Classic 2026", "Windows only" }, s => Assert.Contains(s, Between(text, "## 3. The sketch", "## 4. Microsoft's gates")));
        Assert.All(new[] { "validator: not run", "render-and-measure" }, s => Assert.Contains(s, Between(text, "## 4. Microsoft's gates", "## Sources")));
        AssertCheckedLast(text);
    }

    [Fact]
    public void Licensing_reference_names_the_faq_page_by_its_learn_title()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/references/licensing.md"];

        Assert.Contains("Power Platform licensing FAQs", text);
        Assert.DoesNotContain("Power Apps and Power Automate licensing FAQ", text);
        Assert.True(text.IndexOf("Checked on 2026-11-12", StringComparison.Ordinal) > text.IndexOf("Checked on 2026-11-11", StringComparison.Ordinal));
    }

    [Fact]
    public void Implement_skill_keeps_canvas_packages_out_of_git_and_says_when_the_power_bi_validator_did_not_run()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        var finish = Between(text, "## 4. Finish", "## 5. Power BI");

        Assert.Contains("a canvas app's formulas are checked on the exported zip", Between(finish, "1. Run `powerops analyze`", "\n2. "));
        Assert.All(new[] { "the repo's `.gitignore` holds `*.msapp`", "stage no `.msapp`", "keeps that choice", "`git rm --cached <file>`" }, s => Assert.Contains(s, Between(finish, "\n2. Before you open a pull request", "\n3. ")));
        Assert.Contains("validator: not run", Between(text, "## 5. Power BI", "## Per agent"));
    }

    [Fact]
    public void Review_plan_skill_checks_buildability_and_what_each_role_sets_at_create()
    {
        var text = SkillsCommand.Shipped()["powerops-review-plan/SKILL.md"];

        Assert.All(new[] { "13. Buildability:", "Two plan lines that can't both hold are a High finding.", "which columns it can set when it creates a row" }, s => Assert.Contains(s, text));
    }

    // M109: the tools M98 to M107 added, named where an agent follows them.
    private static string RepoText(params string[] parts) => File.ReadAllText(Path.Combine([Golden.RepoRoot, .. parts])).Replace("\r\n", "\n");

    [Fact]
    public void Implement_skill_runs_inventory_stops_only_for_a_new_uncovered_driver_and_reads_each_after()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        var step1 = Between(Between(text, "## 4. Finish", "## 5. Power BI"), "1. Run `powerops analyze`", "\n2. ");
        var deployment = Between(text, "**The deployment yes.**", "**The stop list.**");

        Assert.All(new[] { "`powerops inventory`", "`notRead` line as \"not verified\"", "in the pull request and in step 5", "it shows no host, and you add none", "Run `inventory` on the previous solution too", "new against it", "stop under rule 6 of section 3", "needs no question" }, s => Assert.Contains(s, step1));
        Assert.All(new[] { "read each solution's `after`", "\"pac reported success, but the target shows\"", "don't count that import done", "Exit 3 after \"The imports succeeded.\"", "say the version is not verified" }, s => Assert.Contains(s, deployment));
    }

    [Fact]
    public void Plan_skill_starts_from_a_draft_and_asks_only_what_it_leaves_open()
    {
        var text = PlanSkill();
        var start = Between(text, "## 2. Start or resume", "## 3. Environment facts");
        var fast = FastPathOf(text);

        Assert.Contains("Otherwise create the folder, do step 5 if the person gave a draft, and go to section 3.", start);
        Assert.All(new[] { "5. **A draft of their own.**", "map it onto the sections of section 5", "`- from the draft: <choice>: \"<their words>\" (answered by: person)`", "redacted per section 4", "ask only what it leaves open", "count as asked when the draft settles them", "never `yes (read)`" }, s => Assert.Contains(s, start));
        Assert.Equal(2, Regex.Matches(fast, Regex.Escape("unless the draft settled it (section 2 step 5)")).Count);
    }

    [Fact]
    public void Licensing_reference_maps_the_inventory_drivers_and_gives_the_developer_plan_what_learn_says()
    {
        var text = SkillsCommand.Shipped()["powerops-plan/references/licensing.md"];
        var table = Between(text, "## 1. The table", "## 2. Needs");
        var needs = Between(text, "## 2. Needs", "## 3. Part numbers");
        var parts = Between(text, "## 3. Part numbers", "## Sources");
        var desktop = InventoryReport.Drivers.Single(d => d.Id == "desktop-flow-call");
        var desktopRow = needs.Split('\n').Single(l => l.StartsWith("| A cloud flow that runs a desktop flow |", StringComparison.Ordinal));
        var dev = parts.Split('\n').Single(l => l.StartsWith("| `POWERAPPS_DEV` |", StringComparison.Ordinal));

        Assert.Contains("`powerops inventory` fills the Need column", table);
        Assert.All(InventoryReport.Drivers, d => Assert.Contains($"`{d.Id}`", table));
        Assert.All(new[] { "`desktop-flow-call` the desktop-flow row", "`notClassified`", "a `notRead` part is not checked" }, s => Assert.Contains(s, table));
        Assert.All(new[] { "The Developer Plan covers the maker in Dev only.", "When the person says Dev is an unmanaged developer environment", "`the maker, in Dev only`", "When Dev is managed, or its kind isn't said, the Developer Plan makes no row `yes (read)`." }, s => Assert.Contains(s, table));
        Assert.Contains(desktop.Need.Split(" needs ", 2)[1].TrimEnd('.'), desktopRow);
        Assert.All(new[] { "https://learn.microsoft.com/connectors/uiflow/", "https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types" }, s => Assert.Contains(s, desktopRow));
        Assert.All(new[] { "managed developer environments remain", "suspended from 2026-12-15", "deleted from 2027-01-15", "750 flow runs a month", "2 GB", "not raised by add-ons", "times 30" }, s => Assert.Contains(s, needs));
        Assert.All(new[] { "unmanaged developer environment only", "not dataflows, business process flows, desktop flows, AI Builder or Dynamics 365 apps", "in a managed developer environment no row", "never production" }, s => Assert.Contains(s, dev));
        Assert.Contains("attended desktop flows", parts.Split('\n').Single(l => l.StartsWith("| `POWERAUTOMATE_ATTENDED_RPA` |", StringComparison.Ordinal)));
        Assert.DoesNotContain("A free, trial or developer plan", parts);
        Assert.All(new[] { "managed developer environment", "covers no row", "Checked on 2026-11-11" }, s => Assert.Contains(s, text));
        Assert.True(text.IndexOf("Checked on 2026-11-15", StringComparison.Ordinal) > text.IndexOf("Checked on 2026-11-12", StringComparison.Ordinal));
    }

    [Fact]
    public void Review_plan_skill_checks_a_developer_environment_against_its_750_runs_a_month()
    {
        var item3 = SkillsCommand.Shipped()["powerops-review-plan/SKILL.md"].Split('\n').Single(l => l.StartsWith("3. Premium connector and licence impact:", StringComparison.Ordinal));

        Assert.Contains("When `Constraints` says Dev is a developer environment, the plan's flows' expected runs a day, summed, times 30, passing 750 is a Medium finding: the environment allows 750 flow runs a month.", item3);
    }

    [Theory]
    [InlineData(".claude/agents")]
    [InlineData("plugins/claude/agents")]
    public void Automate_specialist_names_inventory_and_the_flow_rules_by_what_they_find(string folder)
    {
        var step5 = RepoText([.. folder.Split('/'), "powerops-automate.md"]).Split('\n').Single(l => l.StartsWith("5. Offline checks on the export:", StringComparison.Ordinal));
        string[] wording =
        [
            "`powerops analyze`, whose PO037 to PO042 find",
            "a catch that tells no one",
            "a static result left on",
            "a schedule near the 90-day turn-off",
            "an approval that can wait as long as a run can last",
            "a Text environment variable that looks like a secret",
            "a secret read or sent without secure inputs or outputs",
            "`powerops inventory` for licence drivers and endpoints; `powerops test` on the plan's flow tests",
        ];

        Assert.All(wording, s => Assert.Contains(s, step5));
    }

    [Fact]
    public void Planning_skills_doc_names_inventory_and_the_draft()
    {
        var text = RepoText("docs", "PLANNING-SKILLS.md");

        Assert.Contains("It runs `powerops analyze`, `powerops inventory` (licence drivers, stopping only for a new one the plan's Licences table lacks) and `powerops test` at the end", text);
        Assert.Contains("- A draft the person hands over is mapped onto the plan first: each choice it settles is logged as their answer, and only what it leaves open is asked.", Between(text, "## Questioning protocol", "### Depth and versions"));
    }
}