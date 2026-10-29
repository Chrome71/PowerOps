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
    public void Every_project_has_a_lock_file_for_the_locked_mode_restore()
    {
        var props = XDocument.Load(Path.Combine(Golden.RepoRoot, "Directory.Build.props"));
        Assert.Equal("true", props.Descendants("RestorePackagesWithLockFile").Single().Value);

        foreach (var (folder, project) in new[] { ("src", "PowerOps.Cli"), ("src", "PowerOps.Core"), ("tests", "PowerOps.Cli.Tests"), ("tests", "PowerOps.Core.Tests") })
        {
            Assert.True(File.Exists(Path.Combine(Golden.RepoRoot, folder, project, "packages.lock.json")), project);
        }
    }
}