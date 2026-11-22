using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The plugin folders hold copies of skills/: POWEROPS_UPDATE_GOLDEN=1 writes them, a normal run compares.
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
        Assert.DoesNotContain("specialist", (string?)manifest["description"]);
        // Servers only in .mcp.json, so they are defined once.
        Assert.Null(manifest["mcpServers"]);
        AssertServer(Path.Combine(dir, ".mcp.json"));
    }

    // M121: the six specialists stay in .claude/agents/ for this repo; the M119 eval kept none for the plugin.
    [Fact]
    public void Claude_plugin_ships_no_agents()
    {
        Assert.False(Directory.Exists(Repo("plugins", "claude", "agents")), "plugins/claude/agents/ is back; M121 ships no agents in the plugin");
        Assert.Null(Json(Repo("plugins", "claude", ".claude-plugin", "plugin.json"))["agents"]);
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
        foreach (var skill in new[] { "powerops-prototype", "powerops-review-plan", "powerops-review-security", "powerops-review-sketch", "powerops-verify" })
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

    private static string Lf(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

    // The "Power Platform specialist team" section of CLAUDE.md, from its heading to "Saved workflows in".
    private static string SpecialistSection()
    {
        var lines = Lf(Repo("CLAUDE.md")).Split('\n');
        var start = Array.IndexOf(lines, "### Power Platform specialist team");
        Assert.True(start >= 0, "CLAUDE.md: \"### Power Platform specialist team\" not found");
        var stop = Array.FindIndex(lines, start, l => l.StartsWith("Saved workflows in", StringComparison.Ordinal));
        Assert.True(stop > start, "CLAUDE.md: \"Saved workflows in\" not found after the specialist section");
        return string.Join('\n', lines[(start + 1)..stop]);
    }

    // M93: these read the sources in .claude/agents/ and CLAUDE.md.
    [Fact]
    public void Specialist_rules_mark_a_proposed_line_with_no_source_as_unproven()
    {
        Assert.Contains("`(unproven: <probe>)`", SpecialistSection());
    }

    [Fact]
    public void Specialist_rules_run_tenant_writes_under_the_signed_off_plan_or_a_yes_for_a_stop_or_a_deployment()
    {
        Assert.Contains("under the signed-off plan, or after the person's yes for a stop-list item or a deployment", SpecialistSection());
    }

    [Fact]
    public void Workflow_logic_enforces_a_valid_record_at_the_write_not_in_a_flow_after_it()
    {
        Assert.Contains("a second line, not the control", Lf(Repo(".claude", "agents", "powerops-workflow-logic.md")));
    }

    // M95: the step that says what to read names the build standard, not only checklist item 16.
    [Fact]
    public void Workflow_logic_reads_the_build_standard_in_its_step_2()
    {
        var step = Lf(Repo(".claude", "agents", "powerops-workflow-logic.md")).Split('\n').Single(l => l.StartsWith("2. Read the plan's", StringComparison.Ordinal));

        Assert.Contains("build-standard.md", step);
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