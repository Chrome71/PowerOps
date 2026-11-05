using System.Text.Json.Nodes;
using System.Xml.Linq;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class BuildPropsTests
{
    [Fact]
    public void NuGet_audit_warnings_stay_warnings_under_TreatWarningsAsErrors()
    {
        var props = XDocument.Load(Path.Combine(Golden.RepoRoot, "Directory.Build.props"));
        var codes = props.Descendants("WarningsNotAsErrors").Single().Value.Split(';');

        Assert.Equal("true", props.Descendants("TreatWarningsAsErrors").Single().Value);
        Assert.All(new[] { "NU1900", "NU1901", "NU1902", "NU1903", "NU1904", "NU1905" }, c => Assert.Contains(c, codes));
    }

    [Fact]
    public void Only_the_shipped_projects_have_a_lock_file_for_the_locked_mode_restore()
    {
        Assert.Empty(XDocument.Load(Path.Combine(Golden.RepoRoot, "Directory.Build.props")).Descendants("RestorePackagesWithLockFile"));

        foreach (var project in new[] { "PowerOps.Cli", "PowerOps.Core" })
        {
            var folder = Path.Combine(Golden.RepoRoot, "src", project);
            Assert.Equal("true", XDocument.Load(Path.Combine(folder, project + ".csproj")).Descendants("RestorePackagesWithLockFile").Single().Value);
            Assert.True(File.Exists(Path.Combine(folder, "packages.lock.json")), project);
        }

        foreach (var project in new[] { "PowerOps.Cli.Tests", "PowerOps.Core.Tests" })
        {
            Assert.False(File.Exists(Path.Combine(Golden.RepoRoot, "tests", project, "packages.lock.json")), project);
        }
    }

    // The version files change only through a release: release-please rewrites the marked line and the manifest together.
    [Fact]
    public void The_props_version_and_the_manifest_are_equal_and_stamped_by_release_please()
    {
        var props = Path.Combine(Golden.RepoRoot, "Directory.Build.props");
        var line = File.ReadAllLines(props).Single(l => l.Contains("<Version>", StringComparison.Ordinal));
        var config = JsonNode.Parse(File.ReadAllText(Path.Combine(Golden.RepoRoot, "release-please-config.json")))!["packages"]!["."]!["extra-files"]!.AsArray();
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(Golden.RepoRoot, ".release-please-manifest.json")))!;

        Assert.Contains("x-release-please-version", line);
        Assert.Contains(config, f => (string?)f!["type"] == "generic" && (string?)f["path"] == "Directory.Build.props");
        Assert.Equal(XDocument.Load(props).Descendants("Version").Single().Value, (string?)manifest["."]);
    }
}