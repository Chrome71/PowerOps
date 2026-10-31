using System.Text.RegularExpressions;

using PowerOps.Core.Output;

namespace PowerOps.Cli.Tests;

public partial class SkillsCommandTests : IDisposable
{
    private static readonly string[] Skills = ["powerops-plan", "powerops-review-plan", "powerops-implement"];
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
    public void Empty_dir_gets_both_targets_with_all_three_skills()
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
        Assert.Equal(6, output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(l => l.StartsWith("unchanged ")));
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

    [Fact]
    public void Only_the_claude_review_copy_gets_the_fork_lines()
    {
        Run("skills", "install", _temp);

        var claude = Skill(".claude", "powerops-review-plan");
        var shared = Skill(".agents", "powerops-review-plan");
        foreach (var line in new[] { "context: fork", "background: false", "model: opus" })
        {
            Assert.Contains($"\n{line}\n", claude);
            Assert.DoesNotContain(line, shared);
        }

        Assert.Equal(Skill(".agents", "powerops-plan"), Skill(".claude", "powerops-plan"));
    }

    [Fact]
    public void ForClaude_inserts_before_the_closing_fence_and_leaves_other_files_alone()
    {
        var result = SkillsCommand.ForClaude("powerops-review-plan/SKILL.md", "---\nname: x\n---\n\nbody\n---\n");

        Assert.Equal("---\nname: x\ncontext: fork\nbackground: false\nmodel: opus\n---\n\nbody\n---\n", result);
        Assert.Equal("---\nname: x\n---\n", SkillsCommand.ForClaude("powerops-plan/SKILL.md", "---\nname: x\n---\n"));
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
    public void Review_skill_asks_for_a_way_out_of_each_form()
    {
        var text = SkillsCommand.Shipped()["powerops-review-plan/SKILL.md"];

        Assert.Contains("a way out (cancel, back, a failed save)", text);
    }
}