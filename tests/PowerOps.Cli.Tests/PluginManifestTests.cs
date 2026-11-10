using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The plugin folders hold copies of skills/, and the Claude plugin generated copies of the six specialists: POWEROPS_UPDATE_GOLDEN=1 writes them, a normal run compares.
public class PluginManifestTests
{
    private static string Repo(params string[] parts) => Path.Combine([Golden.RepoRoot, .. parts]);

    private static JsonObject Json(string path) => JsonNode.Parse(File.ReadAllText(path))!.AsObject();

    private static string Props(string element) => XDocument.Load(Repo("Directory.Build.props")).Descendants(element).Single().Value;

    // The plugin folder of the one entry in a marketplace file, after the checks both agents share.
    private static string PluginDir(JsonObject marketplace, string sourcePath)
    {
        Assert.False(string.IsNullOrEmpty((string?)marketplace["name"]));
        Assert.False(string.IsNullOrEmpty((string?)marketplace["owner"]?["name"]));
        var entry = Assert.Single(marketplace["plugins"]!.AsArray())!.AsObject();
        Assert.Equal("powerops", (string?)entry["name"]);
        Assert.StartsWith("./", sourcePath);
        Assert.DoesNotContain("..", sourcePath);
        var dir = Repo(sourcePath[2..].Split('/'));
        Assert.True(Directory.Exists(dir), $"{sourcePath} not found");
        return dir;
    }

    private static void AssertManifest(JsonObject manifest)
    {
        Assert.Equal("powerops", (string?)manifest["name"]);
        var description = (string?)manifest["description"] ?? "";
        Assert.Contains("`powerops` CLI", description);
        Assert.Contains("PATH", description);
        Assert.Contains("`powerops mcp`", description);
        Assert.Equal(Props("RepositoryUrl"), (string?)manifest["homepage"]);
        Assert.Equal(Props("RepositoryUrl"), (string?)manifest["repository"]);
    }

    // The one server both plugins start: the installed CLI from PATH, so the server is the CLI's version.
    private static void AssertServer(string mcpJson, params string[] extraKeys)
    {
        var server = Assert.Single(Json(mcpJson)["mcpServers"]!.AsObject());
        Assert.Equal("powerops", server.Key);
        var entry = server.Value!.AsObject();
        Assert.Equal(["args", "command", .. extraKeys], entry.Select(p => p.Key).Order(StringComparer.Ordinal));
        Assert.Equal("powerops", (string?)entry["command"]);
        Assert.Equal(["mcp"], entry["args"]!.AsArray().Select(a => (string?)a));
    }

    [Fact]
    public void Claude_marketplace_lists_the_powerops_plugin()
    {
        var marketplace = Json(Repo(".claude-plugin", "marketplace.json"));
        var dir = PluginDir(marketplace, (string)marketplace["plugins"]![0]!["source"]!);

        var manifest = Json(Path.Combine(dir, ".claude-plugin", "plugin.json"));
        AssertManifest(manifest);
        // Servers only in .mcp.json, so they are defined once.
        Assert.Null(manifest["mcpServers"]);
        AssertServer(Path.Combine(dir, ".mcp.json"));
    }

    [Fact]
    public void Codex_marketplace_lists_the_powerops_plugin()
    {
        var marketplace = Json(Repo(".agents", "plugins", "marketplace.json"));
        var entry = marketplace["plugins"]![0]!;
        Assert.Equal("local", (string?)entry["source"]?["source"]);
        Assert.False(string.IsNullOrEmpty((string?)entry["policy"]?["installation"]));
        Assert.False(string.IsNullOrEmpty((string?)entry["policy"]?["authentication"]));
        Assert.False(string.IsNullOrEmpty((string?)entry["category"]));
        var dir = PluginDir(marketplace, (string)entry["source"]!["path"]!);

        var manifest = Json(Path.Combine(dir, ".codex-plugin", "plugin.json"));
        AssertManifest(manifest);
        Assert.Equal("./skills/", (string?)manifest["skills"]);
        Assert.False(string.IsNullOrEmpty((string?)manifest["interface"]?["displayName"]));
        Assert.Equal(Props("RepositoryUrl"), (string?)manifest["interface"]?["websiteURL"]);
        Assert.Equal(manifest["description"]!.GetValue<string>(), (string?)manifest["interface"]?["longDescription"]);

        var servers = (string?)manifest["mcpServers"] ?? "";
        Assert.Equal("./.mcp.json", servers);
        var mcpJson = Path.Combine([dir, .. servers[2..].Split('/')]);
        Assert.True(File.Exists(mcpJson), $"{servers} not found");
        AssertServer(mcpJson, "env_vars", "tool_timeout_sec");
        var server = Json(mcpJson)["mcpServers"]!["powerops"]!;
        // deploy apply waits up to 60 minutes for one import; Codex's default is 300 s in its code.
        Assert.Equal(3600, (int?)server["tool_timeout_sec"]);
        // Codex passes a server only a few variables; on Linux pac needs one of these to reach its saved sign-in.
        Assert.Equal(["DBUS_SESSION_BUS_ADDRESS", "XDG_RUNTIME_DIR"], server["env_vars"]!.AsArray().Select(v => (string?)v));
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("codex")]
    public void Plugin_skills_are_the_shipped_skills(string agent)
    {
        var root = Repo("plugins", agent, "skills");
        var expected = SkillsCommand.Shipped().ToDictionary(f => f.Key, f => agent == "claude" ? SkillsCommand.ForClaude(f.Key, f.Value) : f.Value);
        if (Environment.GetEnvironmentVariable("POWEROPS_UPDATE_GOLDEN") == "1")
        {
            // Copies of renamed or removed skills.
            foreach (var stale in Files(root).Keys.Where(k => !expected.ContainsKey(k)))
            {
                File.Delete(Path.Combine([root, .. stale.Split('/')]));
            }

            foreach (var (rel, text) in expected)
            {
                var file = Path.Combine([root, .. rel.Split('/')]);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, text, new UTF8Encoding(false));
            }
        }

        var actual = Files(root);
        Assert.Equal(expected.Keys.Order(), actual.Keys.Order());
        foreach (var (rel, text) in expected)
        {
            Assert.True(text == actual[rel], $"plugins/{agent}/skills/{rel} differs from the shipped skill; rerun the tests with POWEROPS_UPDATE_GOLDEN=1");
            if (rel.EndsWith("/SKILL.md", StringComparison.Ordinal))
            {
                Assert.Contains($"\nname: {rel.Split('/')[0]}\n", text);
            }
        }

        // ForClaude compared with itself proves nothing on its own.
        foreach (var skill in new[] { "powerops-prototype", "powerops-review-plan", "powerops-review-security", "powerops-verify" })
        {
            Assert.Equal(agent == "claude", actual[$"{skill}/SKILL.md"].Contains("\ncontext: fork\n"));
        }

        foreach (var skill in new[] { "powerops-plan", "powerops-implement" })
        {
            Assert.DoesNotContain("\ncontext: fork\n", actual[$"{skill}/SKILL.md"]);
        }
    }

    // "<skill>/<path>" to text with LF line endings, as SkillsCommand.Shipped() keys them.
    private static Dictionary<string, string> Files(string root) => Directory.Exists(root)
        ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToDictionary(f => Path.GetRelativePath(root, f).Replace('\\', '/'), f => File.ReadAllText(f).Replace("\r\n", "\n"))
        : [];

    private static string AgentsDir => Repo("plugins", "claude", "agents");

    private static string Lf(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

    // "<name>.md" to the text the generator makes from .claude/agents/<name>.md and CLAUDE.md.
    private static Dictionary<string, string> ExpectedAgents()
    {
        var claudeMd = Lf(Repo("CLAUDE.md"));
        return PluginAgents.Specialists.ToDictionary(n => n + ".md", n => PluginAgents.ForPlugin(n, Lf(Repo(".claude", "agents", n + ".md")), claudeMd));
    }

    // Each file in dir that is missing, extra or different from the generator's text, in ordinal order.
    private static List<string> StaleAgentCopies(string dir)
    {
        var expected = ExpectedAgents();
        var actual = Files(dir);
        return [.. expected.Keys.Union(actual.Keys)
            .Where(k => !expected.TryGetValue(k, out var e) || !actual.TryGetValue(k, out var a) || e != a)
            .Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void Plugin_agents_are_the_generated_specialists()
    {
        var expected = ExpectedAgents();
        if (Environment.GetEnvironmentVariable("POWEROPS_UPDATE_GOLDEN") == "1")
        {
            foreach (var stale in Files(AgentsDir).Keys.Where(k => !expected.ContainsKey(k)))
            {
                File.Delete(Path.Combine([AgentsDir, .. stale.Split('/')]));
            }

            Directory.CreateDirectory(AgentsDir);
            foreach (var (file, text) in expected)
            {
                File.WriteAllText(Path.Combine(AgentsDir, file), text, new UTF8Encoding(false));
            }
        }

        var differing = StaleAgentCopies(AgentsDir);
        Assert.True(differing.Count == 0, $"plugins/claude/agents/ differs from the generated specialists ({string.Join(", ", differing)}); rerun the tests with POWEROPS_UPDATE_GOLDEN=1");
    }

    [Fact]
    public void A_changed_missing_or_extra_agent_copy_is_stale()
    {
        var dir = Directory.CreateTempSubdirectory("powerops-agents-").FullName;
        try
        {
            foreach (var (file, text) in ExpectedAgents())
            {
                File.WriteAllText(Path.Combine(dir, file), text);
            }

            Assert.Empty(StaleAgentCopies(dir));

            var changed = Path.Combine(dir, "powerops-copilot-studio.md");
            File.WriteAllText(changed, "X" + File.ReadAllText(changed)[1..]);
            File.Delete(Path.Combine(dir, "powerops-design-lead.md"));
            File.WriteAllText(Path.Combine(dir, "a-extra.md"), "extra");

            Assert.Equal(["a-extra.md", "powerops-copilot-studio.md", "powerops-design-lead.md"], StaleAgentCopies(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Plugin_agents_ship_no_repo_only_reference()
    {
        foreach (var name in PluginAgents.Specialists)
        {
            var path = Path.Combine(AgentsDir, name + ".md");
            Assert.True(File.Exists(path), $"plugins/claude/agents/{name}.md not found");
            var text = Lf(path);
            var hits = RepoOnlyReferences(text);
            Assert.True(hits.Count == 0, $"plugins/claude/agents/{name}.md: {string.Join(", ", hits)}");

            var (front, body) = SplitFrontmatter(text);
            var (sourceFront, _) = SplitFrontmatter(Lf(Repo(".claude", "agents", name + ".md")));
            Assert.Contains($"name: {name}", front);
            Assert.Equal(sourceFront.Select(l => l.StartsWith("description: ", StringComparison.Ordinal) ? $"{l} {PluginAgents.Sentence}" : l), front);

            Assert.Single(Regex.Matches(body, "^## Shared rules$", RegexOptions.Multiline));
            Assert.Contains("Record-only until measured", body);
            Assert.Contains("No evaluation has measured", body);
            Assert.Contains("`subagent_type: \"powerops:<name>\"`", body);
            Assert.Contains("\n- Never run `powerops deploy apply --apply` or `powerops settings apply --apply`, or call the MCP tool `deploy_apply` with `apply: true`", body);
        }
    }

    private static (string[] Front, string Body) SplitFrontmatter(string text)
    {
        Assert.StartsWith("---\n", text);
        var end = text.IndexOf("\n---\n", 3, StringComparison.Ordinal);
        Assert.True(end > 0, "no closing --- line");
        return (text[4..end].Split('\n'), text[(end + 5)..]);
    }

    // Each reference in a plugin agent to something only this repo has.
    private static List<string> RepoOnlyReferences(string text)
    {
        var workflows = Directory.EnumerateFiles(Repo(".claude", "workflows"), "*.js").Select(f => "/" + Path.GetFileNameWithoutExtension(f));
        var hits = new[] { "docs/research", ".claude/", "CLAUDE.md", "AGENTS.md", "this repo", "M38" }.Concat(workflows)
            .Where(t => text.Contains(t, StringComparison.Ordinal)).ToList();

        var repoAgents = Directory.EnumerateFiles(Repo(".claude", "agents"), "*.md").Select(f => Path.GetFileNameWithoutExtension(f)).Except(PluginAgents.Specialists);
        hits.AddRange(repoAgents.Where(a => Regex.IsMatch(text, $@"(?<![\w-]){Regex.Escape(a)}(?![\w-])")));

        var rest = text.Replace(Props("RepositoryUrl") + "/blob/main/docs/", "", StringComparison.Ordinal);
        hits.AddRange(Regex.Matches(rest, @"docs/(?!plans/)[^\s`)]*").Select(m => m.Value));

        const string pluginPath = @"\$\{CLAUDE_PLUGIN_ROOT\}/([^\s`)]*)";
        foreach (Match m in Regex.Matches(rest, pluginPath))
        {
            var target = Path.Combine([Repo("plugins", "claude"), .. m.Groups[1].Value.Split('/', StringSplitOptions.RemoveEmptyEntries)]);
            if (m.Groups[1].Value.Contains("..", StringComparison.Ordinal) || !(File.Exists(target) || Directory.Exists(target)))
            {
                hits.Add(m.Value);
            }
        }

        hits.AddRange(Regex.Matches(Regex.Replace(rest, pluginPath, ""), @"[^\s`(]*(?:skills|references)/[^\s`)]*").Select(m => m.Value));
        return hits;
    }

    [Fact]
    public void Repo_only_references_are_found()
    {
        Assert.Equal(["docs/HANDOFF.md"], RepoOnlyReferences("step 11 of `docs/HANDOFF.md` again"));
        Assert.Equal(["references/data.md"], RepoOnlyReferences("whose `references/data.md` holds"));
        Assert.Equal(["powerops-researcher"], RepoOnlyReferences("- `powerops-researcher`, through the main session"));
        Assert.Equal(["${CLAUDE_PLUGIN_ROOT}/skills/nope.md"], RepoOnlyReferences("read `${CLAUDE_PLUGIN_ROOT}/skills/nope.md` first"));
        Assert.Empty(RepoOnlyReferences($"the guide ({Props("RepositoryUrl")}/blob/main/docs/HANDOFF.md) and `docs/plans/x`"));
        Assert.Empty(RepoOnlyReferences("`${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/references/data.md` and `${CLAUDE_PLUGIN_ROOT}/skills/powerops-plan/`"));
    }

    [Fact]
    public void The_generator_throws_when_a_source_no_longer_matches()
    {
        var source = Lf(Repo(".claude", "agents", "powerops-apps.md"));
        var claudeMd = Lf(Repo("CLAUDE.md"));

        Assert.Throws<InvalidOperationException>(() => PluginAgents.ForPlugin("powerops-apps", source[(source.IndexOf("\n---\n", 3, StringComparison.Ordinal) + 5)..], claudeMd));

        var reworded = source.Replace("which hold the rules every specialist shares", "which hold the shared rules", StringComparison.Ordinal);
        Assert.NotEqual(source, reworded);
        var ex = Assert.Throws<InvalidOperationException>(() => PluginAgents.ForPlugin("powerops-apps", reworded, claudeMd));
        Assert.Contains("powerops-apps.md", ex.Message);
        Assert.Contains("which hold the rules every specialist shares", ex.Message);

        Assert.Throws<InvalidOperationException>(() => PluginAgents.SharedRules(claudeMd.Replace("### Power Platform specialist team", "### Specialists", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("plugins/claude/.claude-plugin/plugin.json")]
    [InlineData("plugins/codex/.codex-plugin/plugin.json")]
    public void Plugin_version_is_the_release_version(string path)
    {
        Assert.Equal(Props("Version"), (string?)Json(Repo(path.Split('/')))["version"]);

        var extraFiles = Json(Repo("release-please-config.json"))["packages"]!["."]!["extra-files"]!.AsArray();
        Assert.Contains(extraFiles, f => (string?)f!["type"] == "json" && (string?)f["path"] == path && (string?)f["jsonpath"] == "$.version");
    }
}