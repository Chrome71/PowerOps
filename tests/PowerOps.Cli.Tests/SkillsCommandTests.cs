using System.Text.RegularExpressions;

using PowerOps.Core.Output;

namespace PowerOps.Cli.Tests;

public partial class SkillsCommandTests : IDisposable
{
    private static readonly string[] Skills = ["powerops-plan", "powerops-prototype", "powerops-review-plan", "powerops-implement", "powerops-review-security", "powerops-verify"];
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
    public void Empty_dir_gets_both_targets_with_all_six_skills()
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
        string[] rule = ["Never run `powerops settings apply --apply` or `powerops deploy apply --apply`, or call the MCP tool `deploy_apply` with `apply: true`, unless the user said yes to that environment in this session", "Run each command without `--apply` first and show the user the target line"];

        Assert.All(rule, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Implement_skill_writes_the_flow_tests_with_fake_values_and_runs_them_before_the_pull_request()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording = ["Before you open a pull request, for each cloud flow the plan adds or changes, write or update its tests once the solution is exported and unpacked into the repo", "use what the user told you about `pac`; do not run `pac` to find out", "keep the zip, which `powerops analyze` and `powerops test` both read", "Use fake values in triggers and mocks", "Run `powerops test", "never delete or weaken a test"];

        Assert.All(wording, s => Assert.Contains(s, text));
    }

    [Fact]
    public void Implement_skill_checks_the_final_saved_version_live_and_sets_done_only_when_the_checks_passed()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "check its final saved version in Dev",
            "never run `pac` or `az` for these checks",
            "follows step 7 of section 3",
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
    public void Implement_skill_names_the_dataverse_skills_and_asks_about_sign_ins_without_running_pac_or_az()
    {
        var text = SkillsCommand.Shipped()["powerops-implement/SKILL.md"];
        string[] wording =
        [
            "code-apps-preview",
            "dv-metadata",
            "dv-solution",
            "MSCRM.SolutionUniqueName",
            "you do not run `pac` or `az` to find out",
            "Give the skill the plan's `environment` and its `Solution and publisher`",
            "Stop and report when a tool can't create or save inside that solution",
            "Do not change the preferred solution",
            "ask the user to name the Dev environment",
            "don't redo a step",
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
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
        ];

        Assert.All(wording, s => Assert.Contains(s, text));
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
        // M41: the 2,483 words the skill had before the ledger, plus 300. On ASCII text a split on whitespace counts as wc -w does.
        var text = SkillsCommand.Shipped()["powerops-plan/SKILL.md"];
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        Assert.All(text, c => Assert.True(c < 128, $"non-ASCII character U+{(int)c:X4}"));
        Assert.InRange(words, 1, 2783);
    }

    [Fact]
    public void Prototype_skill_states_the_rules_the_sign_off_checks()
    {
        // M70: the skill writes what PlanChecker.PrototypeProblems accepts, so its numbers and markers must match.
        var text = SkillsCommand.Shipped()["powerops-prototype/SKILL.md"];

        string[] rules = ["At most 40,960 bytes", "`route:`", "\"Sketch\"", "`data-screen=\"<name>\"`", "`href=\"#...\"`", "Write no `@` anywhere", "Never asks the user questions", "powerops plan status docs/plans/<name>"];
        Assert.All(rules, r => Assert.Contains(r, text));
        Assert.All(new[] { "empty", "loading", "error", "failed-save" }, s => Assert.Contains($"`data-state=\"{s}\"`", text));
        Assert.All(new[] { "script", "link", "meta", "base", "iframe", "frame", "object", "embed", "img", "picture", "svg", "form", "video", "audio", "source" }, tag => Assert.Contains($"`{tag}`", text));
        Assert.All(text, c => Assert.True(c < 128, $"non-ASCII character U+{(int)c:X4}"));
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
            foreach (var name in ProductReferences.Concat(["data.md", "route.md", "design.md", "logic.md"]))
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
        string[] log = ["start `powerops-prototype <name>`", "`prototype: approved mockups/prototype.html`", "`prototype: skipped (<reason>)`", "`prototype: not needed (<reason>)`", "Ask nothing else, except the sketch question (section 7 step 6)", "Keep visual detail out of `Screens` and `Steps`"];
        string[] data = ["## 6. The data round", "1. Where does the data live today?", "6. What is out of this version?", "`logic.md`"];
        string[] routeText = ["## 1. The route", "## 2. The sketch", "at most 8 screens and 40 KB (40,960 bytes)", "`prototype: skipped (mock-data slice)`", "control map", "`design.md`", .. ProductReferences.Select(f => $"`{f}`")];
        string[] review = ["Per product, when the plan names it", "more than 12 data visuals", "accordion"];
        string[] implement = ["## 5. Power BI", "\"not applicable\"", "`powerbi-report-author validate`", "`semantic-model-authoring`", "`powerbi-report-cli`", "`pcf`", "`pac pcf push`", "the sketch's path, not a screenshot", "pass the plan's `### Theme` block word for word as constraints"];

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
    }
}