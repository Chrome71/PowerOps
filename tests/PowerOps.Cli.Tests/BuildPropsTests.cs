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
        Assert.All(new[] { "NU1901", "NU1902", "NU1903", "NU1904" }, c => Assert.Contains(c, codes));
    }
}