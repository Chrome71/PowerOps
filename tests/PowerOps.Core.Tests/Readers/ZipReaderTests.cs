using System.IO.Compression;
using System.Text;

using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class ZipReaderTests
{
    private static string Fixture(string name) => Path.Combine(Golden.RepoRoot, "tests", "fixtures", "zips", name);

    private static TempFolder Zip(out string zipPath, params (string Name, string Content)[] entries)
    {
        var t = new TempFolder();
        zipPath = System.IO.Path.Combine(t.Path, "test.zip");
        using var fs = File.Create(zipPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var w = zip.CreateEntry(name).Open();
            w.Write(Encoding.UTF8.GetBytes(content));
        }

        return t;
    }

    [Fact]
    public void Fixture_unmanaged_column_level_security()
    {
        var s = ZipReader.Read(Fixture("ColumnLevelSecuritySampleSolution.zip"));

        Assert.Equal("ColumnLevelSecuritySampleSolution", s.UniqueName);
        Assert.Equal("1.0.0.0", s.Version);
        Assert.False(s.Managed);
        Assert.Equal(new Publisher("ColumnLevelSecuritySamplePublisher", "sample"), s.Publisher);
        Assert.Equal("solution.xml", s.ManifestFile);
        Assert.Equal(3, s.RootComponents.Count);
    }

    [Fact]
    public void Fixture_managed_column_level_security()
    {
        var s = ZipReader.Read(Fixture("ColumnLevelSecuritySampleSolution_managed.zip"));

        Assert.True(s.Managed);
        Assert.Equal("sample", s.Publisher.Prefix);
    }

    [Fact]
    public void Fixture_contoso_has_default_publisher_connection_reference_and_env_var_value()
    {
        var s = ZipReader.Read(Fixture("Contoso_sample_1_0_0_1_managed.zip"));

        Assert.Equal("Contoso_sample", s.UniqueName);
        Assert.True(s.Managed);
        Assert.Equal("DefaultPublisherorg8a403877", s.Publisher.UniqueName);
        Assert.Equal("new", s.Publisher.Prefix);
        Assert.Equal("new_D365connection", Assert.Single(s.ConnectionReferences).LogicalName);
        var v = Assert.Single(s.EnvironmentVariables);
        Assert.Equal("new_ContosoURL", v.SchemaName);
        Assert.Equal("https://contoso.com", v.CurrentValue);
        Assert.Null(v.DefaultValue);
    }

    [Fact]
    public void Fixture_polymorphic_lookup()
    {
        var s = ZipReader.Read(Fixture("polymorphiclookupexamplesolution.zip"));

        Assert.Equal("polymorphiclookupexamplesolution", s.UniqueName);
        Assert.Equal("examplepublisher", s.Publisher.UniqueName);
    }

    [Fact]
    public void Reads_workflows_from_customizations_with_backslash_entry_names()
    {
        using var t = Zip(out var zip,
            ("solution.xml", SolutionXml.Manifest()),
            ("customizations.xml", SolutionXml.Customizations("""
                <Workflows>
                  <Workflow WorkflowId="{AA-11}" Name="Flow">
                    <JsonFileName>/Workflows/Flow-AA.json</JsonFileName><Category>5</Category><StateCode>1</StateCode>
                  </Workflow>
                </Workflows>
                """)),
            ("Workflows\\Flow-AA.json", "{}"));

        var w = Assert.Single(ZipReader.Read(zip).Workflows);

        Assert.Equal("aa-11", w.Id);
        Assert.Equal(1, w.StateCode);
        Assert.Equal("Workflows/Flow-AA.json", w.File);
        Assert.Equal("{}", w.ClientData);
    }

    [Fact]
    public void Ignores_entries_with_parent_segments_or_rooted_paths()
    {
        const string envDef = "<environmentvariabledefinition schemaname=\"evil_A\" />";
        using var t = Zip(out var zip,
            ("solution.xml", SolutionXml.Manifest(prefix: "good")),
            ("../evil/environmentvariabledefinitions/evil_A/environmentvariabledefinition.xml", envDef),
            ("a/../../environmentvariabledefinitions/evil_B/environmentvariabledefinition.xml", envDef),
            ("/environmentvariabledefinitions/evil_C/environmentvariabledefinition.xml", envDef),
            ("C:/environmentvariabledefinitions/evil_D/environmentvariabledefinition.xml", envDef));

        var s = ZipReader.Read(zip);

        Assert.Equal("good", s.Publisher.Prefix);
        Assert.Empty(s.EnvironmentVariables);
    }

    [Fact]
    public void Evil_solution_xml_alone_is_not_a_solution()
    {
        using var t = Zip(out var zip, ("../evil/solution.xml", SolutionXml.Manifest()));

        Assert.Throws<InvalidDataException>(() => ZipReader.Read(zip));
    }

    [Fact]
    public void Tolerates_bom_and_crlf_in_solution_xml()
    {
        var xml = SolutionXml.Manifest(prefix: "bom").Replace("\n", "\r\n");
        using var t = new TempFolder();
        var zipPath = System.IO.Path.Combine(t.Path, "bom.zip");
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        using (var w = zip.CreateEntry("solution.xml").Open())
        {
            w.Write(new UTF8Encoding(true).GetPreamble());
            w.Write(Encoding.UTF8.GetBytes(xml));
        }

        var s = ZipReader.Read(zipPath);

        Assert.Equal("bom", s.Publisher.Prefix);
        Assert.Equal("1.2.3.4", s.Version);
    }

    [Fact]
    public void Malformed_solution_xml_throws_InvalidDataException_naming_the_file()
    {
        using var t = Zip(out var zip, ("solution.xml", "<ImportExportXml><oops>"));

        var ex = Assert.Throws<InvalidDataException>(() => ZipReader.Read(zip));

        Assert.Contains("solution.xml", ex.Message);
    }

    [Fact]
    public void Not_a_zip_throws_InvalidDataException()
    {
        using var t = new TempFolder().Write("bad.zip", "this is not a zip");

        Assert.Throws<InvalidDataException>(() => ZipReader.Read(System.IO.Path.Combine(t.Path, "bad.zip")));
    }

    [Fact]
    public void Entry_over_the_size_cap_throws_InvalidDataException()
    {
        using var t = Zip(out var zip, ("solution.xml", "<ImportExportXml>" + new string(' ', 51 * 1024 * 1024) + "</ImportExportXml>"));

        Assert.Throws<InvalidDataException>(() => ZipReader.Read(zip));
    }

    [Fact]
    public void Loader_and_discovery_handle_zips()
    {
        using var t = Zip(out var zip, ("solution.xml", SolutionXml.Manifest()));

        Assert.Equal([zip], SolutionDiscovery.Find(t.Path));
        Assert.Equal("Demo", SolutionLoader.Load(zip).UniqueName);
    }
}