using System.IO.Compression;
using System.Xml.Linq;

using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

public class FixtureTests
{
    [Theory]
    [InlineData("coe-auditlogs")]
    [InlineData("alm-sample")]
    [InlineData("alm-makers")]
    public void Unpacked_fixture_solution_xml_has_ImportExportXml_root(string name)
    {
        var path = Path.Combine(Golden.RepoRoot, "tests", "fixtures", name, "Other", "Solution.xml");

        var doc = XDocument.Load(path);

        Assert.Equal("ImportExportXml", doc.Root?.Name.LocalName);
    }

    [Theory]
    [InlineData("ColumnLevelSecuritySampleSolution.zip")]
    [InlineData("ColumnLevelSecuritySampleSolution_managed.zip")]
    [InlineData("Contoso_sample_1_0_0_1_managed.zip")]
    [InlineData("polymorphiclookupexamplesolution.zip")]
    public void Zip_fixture_contains_solution_xml(string fileName)
    {
        var path = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "zips", fileName);

        using var zip = ZipFile.OpenRead(path);

        Assert.Contains(zip.Entries, entry => entry.FullName == "solution.xml");
    }
}