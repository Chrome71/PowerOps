using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The package id PowerOps is not registered on nuget.org, so an install of it without --source could fetch someone else's package.
public class InstallLineTests
{
    internal static bool InstallsPowerOpsWithoutSource(string line)
    {
        var at = line.IndexOf("dotnet tool install", StringComparison.Ordinal);
        if (at < 0)
        {
            return false;
        }

        var words = line[(at + "dotnet tool install".Length)..].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w.Trim('`', '"', '\'', '.', ',', ';', ')', '(')).ToList();
        return words.Contains("PowerOps") && !words.Any(w => w == "--source" || w.StartsWith("--source=", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("dotnet tool install --global PowerOps", true)]
    [InlineData("run `dotnet tool install --global PowerOps`, and stop.", true)]
    [InlineData("dotnet tool install --tool-path ./tool --add-source ./artifacts PowerOps --prerelease", true)]
    [InlineData("dotnet tool install --global --source artifacts PowerOps", false)]
    [InlineData("dotnet tool install --global Microsoft.PowerApps.CLI.Tool", false)]
    [InlineData("install PowerOps from a clone", false)]
    public void The_line_check_flags_only_an_install_of_PowerOps_without_source(string line, bool flagged) =>
        Assert.Equal(flagged, InstallsPowerOpsWithoutSource(line));

    [Fact]
    public void No_doc_or_skill_installs_PowerOps_by_name_alone()
    {
        var root = Golden.RepoRoot;
        var files = new[] { Path.Combine(root, "README.md"), Path.Combine(root, "CONTRIBUTING.md") }
            .Concat(Directory.GetFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Path.Combine(root, "skills"), "SKILL.md", SearchOption.AllDirectories))
            .Where(f => !f.StartsWith(Path.Combine(root, "docs", "plans") + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && f != Path.Combine(root, "docs", "BUILD-PLAN.md") && File.Exists(f));

        var bad = files.SelectMany(f => File.ReadLines(f).Select((l, i) => (f, n: i + 1, l)))
            .Where(x => InstallsPowerOpsWithoutSource(x.l))
            .Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.n}")
            .ToList();

        Assert.Empty(bad);
    }
}