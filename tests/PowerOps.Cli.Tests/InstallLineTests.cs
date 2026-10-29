using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The package id PowerOps is not registered on nuget.org, so an install of it without --source could fetch someone else's package.
public partial class InstallLineTests
{
    [GeneratedRegex(@"dotnet\s+tool\s+(install|update)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ToolCommand();

    // A line ending in \ (sh) or ` (PowerShell) continues on the next, so the lines are joined first. Returns the first line's number (1-based).
    internal static IEnumerable<int> InstallsPowerOpsWithoutSource(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var text = lines[i].TrimEnd();
            var last = i;
            while (last + 1 < lines.Count && (text.EndsWith(" \\", StringComparison.Ordinal) || text.EndsWith(" `", StringComparison.Ordinal)))
            {
                text = text[..^1].TrimEnd() + " " + lines[++last].Trim();
            }

            var m = ToolCommand().Match(text);
            if (!m.Success)
            {
                continue;
            }

            // NuGet package ids are case-insensitive, and --add-source does not replace the feed, so only --source counts.
            var words = text[(m.Index + m.Length)..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim('`', '"', '\'', '.', ',', ';', ')', '(')).ToList();
            if (words.Contains("PowerOps", StringComparer.OrdinalIgnoreCase) && !words.Any(w => w == "--source" || w.StartsWith("--source=", StringComparison.Ordinal)))
            {
                yield return i + 1;
            }
        }
    }

    [Theory]
    [InlineData("dotnet tool install --global PowerOps", true)]
    [InlineData("run `dotnet tool install --global PowerOps`, and stop.", true)]
    [InlineData("dotnet tool install --tool-path ./tool --add-source ./artifacts PowerOps --prerelease", true)]
    [InlineData("dotnet tool install -g powerops", true)]
    [InlineData("dotnet tool update -g PowerOps", true)]
    [InlineData("dotnet tool update --global --source artifacts PowerOps", false)]
    [InlineData("dotnet tool install --global \\\n  PowerOps", true)]
    [InlineData("dotnet tool install --global \\\n  --source artifacts PowerOps", false)]
    [InlineData("dotnet tool install --global `\n  PowerOps", true)]
    [InlineData("dotnet tool install --global `\n  --source artifacts PowerOps", false)]
    [InlineData("dotnet tool install --global --source artifacts PowerOps", false)]
    [InlineData("dotnet tool install --global Microsoft.PowerApps.CLI.Tool", false)]
    [InlineData("install PowerOps from a clone", false)]
    public void The_line_check_flags_only_an_install_of_PowerOps_without_source(string text, bool flagged) =>
        Assert.Equal(flagged, InstallsPowerOpsWithoutSource(text.Split('\n')).Any());

    [Fact]
    public void A_continued_install_is_reported_at_its_first_line() =>
        Assert.Equal([2], InstallsPowerOpsWithoutSource(["x", "dotnet tool install -g \\", "  PowerOps", "y"]));

    [Fact]
    public void No_doc_skill_or_workflow_installs_PowerOps_by_name_alone()
    {
        var root = Golden.RepoRoot;
        string[] Find(string folder, string pattern) => Directory.Exists(Path.Combine(root, folder)) ? Directory.GetFiles(Path.Combine(root, folder), pattern, SearchOption.AllDirectories) : [];
        var plans = Path.Combine(root, "docs", "plans") + Path.DirectorySeparatorChar;
        var worktrees = Path.Combine(root, ".claude", "worktrees") + Path.DirectorySeparatorChar; // in the main checkout, whole copies of the repo
        var files = new[] { Path.Combine(root, "README.md"), Path.Combine(root, "CONTRIBUTING.md") }
            .Concat(Find(".github", "*.yml"))
            .Concat(Find("docs", "*.md"))
            .Concat(Find("docs", "*.yml"))
            .Concat(Find(".claude", "*.md"))
            .Concat(Find("skills", "SKILL.md"))
            .Concat(Find("plugins", "SKILL.md"))
            .Where(f => !f.StartsWith(plans, StringComparison.Ordinal) && !f.StartsWith(worktrees, StringComparison.Ordinal)
                && f != Path.Combine(root, "docs", "BUILD-PLAN.md") && File.Exists(f))
            .ToList();

        var bad = files.SelectMany(f => InstallsPowerOpsWithoutSource(File.ReadAllLines(f)).Select(n => $"{Path.GetRelativePath(root, f)}:{n}")).ToList();

        Assert.NotEmpty(files);
        Assert.Empty(bad);
    }
}