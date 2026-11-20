using System.Text;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class PortableSpecialistTests
{
    private static string Repo(params string[] parts) => Path.Combine([Golden.RepoRoot, .. parts]);

    [Fact]
    public void Portable_specialists_preserve_the_source_procedures()
    {
        var shared = File.ReadAllText(Repo("CLAUDE.md")).Replace("\r\n", "\n");
        foreach (var name in PluginAgents.Specialists)
        {
            var source = File.ReadAllText(Repo(".claude", "agents", name + ".md")).Replace("\r\n", "\n");
            var expected = PluginAgents.ForSkill(name, source, shared);
            var path = Repo("skills", "powerops-specialists", "references", name + ".md");
            if (Environment.GetEnvironmentVariable("POWEROPS_UPDATE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, expected, new UTF8Encoding(false));
            }

            Assert.True(File.Exists(path), $"{path} is missing; run POWEROPS_UPDATE_GOLDEN=1 dotnet test");
            Assert.Equal(expected, File.ReadAllText(path).Replace("\r\n", "\n"));
            Assert.DoesNotContain("${CLAUDE_PLUGIN_ROOT}", expected);
            Assert.DoesNotContain("docs/research/", expected);
            Assert.DoesNotContain("CLAUDE.md", expected);
            Assert.Contains("## Never", expected);

            // Independently retain every domain check and prohibition, not just generator equality.
            foreach (var heading in new[] { "Checklist", "Never" })
            {
                var section = Regex.Match(source, $@"(?s)## {heading}\n(.*?)(?=\n## |\z)").Groups[1].Value;
                Assert.NotEmpty(section);
                var portable = section.Replace("`skills/", "`../../", StringComparison.Ordinal)
                    .Replace("`references/", "`../../powerops-plan/references/", StringComparison.Ordinal)
                    .Replace(" (AGENTS.md rule)", "", StringComparison.Ordinal)
                    .Replace("Bash stays", "The shell stays", StringComparison.Ordinal)
                    .Replace("Bash", "the shell", StringComparison.Ordinal);
                Assert.Contains(portable, expected);
            }

            // The procedure after adaptation must still resolve every local skill reference.
            foreach (Match match in Regex.Matches(expected, @"`(\.\./[^`]+)`"))
            {
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, match.Groups[1].Value));
                Assert.True(File.Exists(target) || Directory.Exists(target), $"{path}: broken reference {match.Value}");
            }
        }
    }

    [Theory]
    [InlineData("claude", ".claude")]
    [InlineData("codex", ".agents")]
    [InlineData("antigravity", ".agents")]
    public void Installer_carries_each_specialist_and_its_dependencies(string agent, string directory)
    {
        var root = Directory.CreateTempSubdirectory("powerops-specialists-").FullName;
        try
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(0, Program.Run(["skills", "install", root, "--agent", agent], output, error));
            var skill = Path.Combine(root, directory, "skills", "powerops-specialists");
            var instructions = File.ReadAllText(Path.Combine(skill, "SKILL.md"));
            foreach (var name in PluginAgents.Specialists)
            {
                Assert.Contains($"references/{name}.md", instructions);
                var reference = Path.Combine(skill, "references", name + ".md");
                var text = File.ReadAllText(reference);
                foreach (Match match in Regex.Matches(text, @"`(\.\./[^`]+)`"))
                {
                    var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(reference)!, match.Groups[1].Value));
                    Assert.True(File.Exists(target) || Directory.Exists(target), $"Installed reference missing: {target}");
                }
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}