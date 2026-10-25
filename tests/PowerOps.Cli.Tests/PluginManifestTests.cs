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

    private static void AssertManifest(JsonObject manifest, string dir)
    {
        Assert.Equal("powerops", (string?)manifest["name"]);
        var description = (string?)manifest["description"] ?? "";
        Assert.Contains("`powerops` CLI", description);
        Assert.Contains("PATH", description);
        Assert.Equal(Props("RepositoryUrl"), (string?)manifest["homepage"]);
        Assert.Equal(Props("RepositoryUrl"), (string?)manifest["repository"]);
        Assert.Null(manifest["mcpServers"]);
        Assert.False(File.Exists(Path.Combine(dir, ".mcp.json")));
        Assert.False(File.Exists(Path.Combine(dir, "mcp.json")));
    }

    [Fact]
    public void Claude_marketplace_lists_the_powerops_plugin()
    {
        var marketplace = Json(Repo(".claude-plugin", "marketplace.json"));
        var dir = PluginDir(marketplace, (string)marketplace["plugins"]![0]!["source"]!);

        AssertManifest(Json(Path.Combine(dir, ".claude-plugin", "plugin.json")), dir);
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
        AssertManifest(manifest, dir);
        Assert.Equal("./skills/", (string?)manifest["skills"]);
        Assert.False(string.IsNullOrEmpty((string?)manifest["interface"]?["displayName"]));
        Assert.Equal(Props("RepositoryUrl"), (string?)manifest["interface"]?["websiteURL"]);
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
        Assert.Equal(agent == "claude", actual["powerops-review-plan/SKILL.md"].Contains("\ncontext: fork\n"));
    }

    // "<skill>/<path>" to text with LF line endings, as SkillsCommand.Shipped() keys them.
    private static Dictionary<string, string> Files(string root) => Directory.Exists(root)
        ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToDictionary(f => Path.GetRelativePath(root, f).Replace('\\', '/'), f => File.ReadAllText(f).Replace("\r\n", "\n"))
        : [];

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