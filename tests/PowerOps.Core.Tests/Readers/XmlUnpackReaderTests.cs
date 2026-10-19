using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class XmlUnpackReaderTests
{
    private static Solution Read(TempFolder t) => XmlUnpackReader.Read(t.Path);

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("2", false)]
    public void Reads_publisher_version_and_managed(string managed, bool expected)
    {
        using var t = new TempFolder().Write("Other/Solution.xml", SolutionXml.Manifest(managed: managed));

        var s = Read(t);

        Assert.Equal("Demo", s.UniqueName);
        Assert.Equal("1.2.3.4", s.Version);
        Assert.Equal(expected, s.Managed);
        Assert.Equal(new Publisher("demopub", "demo"), s.Publisher);
        Assert.Equal("Other/Solution.xml", s.ManifestFile);
    }

    [Fact]
    public void Reads_root_components_with_normalized_ids()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", SolutionXml.Manifest(inner: """
            <RootComponents>
              <RootComponent type="1" schemaName="demo_thing" behavior="0" />
              <RootComponent type="29" id="{ABC-DEF}" behavior="0" />
            </RootComponents>
            """));

        var s = Read(t);

        Assert.Equal(
            [new RootComponent("1", "demo_thing", null), new RootComponent("29", null, "abc-def")],
            s.RootComponents);
    }

    [Fact]
    public void Reads_missing_dependencies_with_and_without_solution()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", SolutionXml.Manifest(inner: """
            <MissingDependencies>
              <MissingDependency>
                <Required type="1" schemaName="core_app" displayName="App" solution="Core (1.0)" />
                <Dependent type="29" displayName="Flow" id="{AA-BB}" />
              </MissingDependency>
              <MissingDependency>
                <Required type="connectionreference" displayName="core_conn" id.connectionreferencelogicalname="core_conn" />
                <Dependent type="connectionreference" displayName="x" id.connectionreferencelogicalname="x" />
              </MissingDependency>
            </MissingDependencies>
            """));

        var deps = Read(t).MissingDependencies;

        Assert.Equal(2, deps.Count);
        Assert.Equal(new DependencyNode("1", "core_app", null, "App", "Core (1.0)"), deps[0].Required);
        Assert.Equal(new DependencyNode("29", null, "aa-bb", "Flow", null), deps[0].Dependent);
        Assert.Equal(new DependencyNode("connectionreference", "core_conn", null, "core_conn", null), deps[1].Required);
        Assert.Null(deps[1].Required.Solution);
    }

    [Fact]
    public void Reads_connection_references()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Other/Customizations.xml", SolutionXml.Customizations("""
                <connectionreferences>
                  <connectionreference connectionreferencelogicalname="demo_conn">
                    <connectorid>/providers/Microsoft.PowerApps/apis/shared_office365</connectorid>
                  </connectionreference>
                  <connectionreference connectionreferencelogicalname="demo_bare" />
                </connectionreferences>
                """));

        var refs = Read(t).ConnectionReferences;

        Assert.Equal(
            [
                new ConnectionReference("demo_conn", "/providers/Microsoft.PowerApps/apis/shared_office365", "Other/Customizations.xml"),
                new ConnectionReference("demo_bare", null, "Other/Customizations.xml"),
            ],
            refs);
    }

    [Fact]
    public void Reads_environment_variable_with_default_required_flag_and_no_value()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("environmentvariabledefinitions/demo_A/environmentvariabledefinition.xml", """
                <environmentvariabledefinition schemaname="demo_A">
                  <defaultvalue>hello</defaultvalue><isrequired>1</isrequired><type>100000000</type>
                </environmentvariabledefinition>
                """)
            .Write("environmentvariabledefinitions/demo_B/environmentvariabledefinition.xml", """
                <environmentvariabledefinition schemaname="demo_B"><isrequired>0</isrequired></environmentvariabledefinition>
                """);

        var vars = Read(t).EnvironmentVariables;

        Assert.Equal(2, vars.Count);
        var a = Assert.Single(vars, v => v.SchemaName == "demo_A");
        Assert.True(a.IsRequired);
        Assert.Equal("hello", a.DefaultValue);
        Assert.Equal("100000000", a.Type);
        Assert.Null(a.CurrentValue);
        Assert.Null(a.ValuesFile);
        Assert.Equal("environmentvariabledefinitions/demo_A/environmentvariabledefinition.xml", a.File);
        var b = Assert.Single(vars, v => v.SchemaName == "demo_B");
        Assert.False(b.IsRequired);
        Assert.Null(b.DefaultValue);
    }

    [Fact]
    public void Reads_environment_variable_value_from_json_with_bom_and_crlf()
    {
        var json = "{\r\n  \"environmentvariablevalues\": {\r\n    \"environmentvariablevalue\": {\r\n      \"value\": \"current\"\r\n    }\r\n  }\r\n}";
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("environmentvariabledefinitions/demo_A/environmentvariabledefinition.xml",
                "<environmentvariabledefinition schemaname=\"demo_A\">\r\n  <defaultvalue>d</defaultvalue>\r\n</environmentvariabledefinition>", bom: true)
            .Write("environmentvariabledefinitions/demo_A/environmentvariablevalues.json", json, bom: true);

        var v = Assert.Single(Read(t).EnvironmentVariables);

        Assert.Equal("d", v.DefaultValue);
        Assert.Equal("current", v.CurrentValue);
        Assert.Equal("environmentvariabledefinitions/demo_A/environmentvariablevalues.json", v.ValuesFile);
    }

    [Fact]
    public void Reads_workflows_with_state_category_and_client_data()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Workflows/Flow-AA.json.data.xml", """
                <Workflow WorkflowId="{AA-11}" Name="My flow">
                  <JsonFileName>/Workflows/Flow-AA.json</JsonFileName>
                  <Category>5</Category><StateCode>0</StateCode>
                </Workflow>
                """, bom: true)
            .Write("Workflows/Flow-AA.json", "{\"properties\":{}}", bom: true)
            .Write("Workflows/Classic.xaml.data.xml", """
                <Workflow WorkflowId="{BB-22}" Name="Classic"><Category>0</Category></Workflow>
                """);

        var wfs = Read(t).Workflows;

        var flow = Assert.Single(wfs, w => w.Id == "aa-11");
        Assert.Equal("My flow", flow.Name);
        Assert.Equal(5, flow.Category);
        Assert.Equal(0, flow.StateCode);
        Assert.Equal("Workflows/Flow-AA.json", flow.File);
        Assert.Equal("{\"properties\":{}}", flow.ClientData);
        var classic = Assert.Single(wfs, w => w.Id == "bb-22");
        Assert.Null(classic.StateCode);
        Assert.Null(classic.ClientData);
    }

    [Fact]
    public void Reads_canvas_app_metadata_text()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("CanvasApps/app_1.meta.xml", "<CanvasApp><Name>app_1</Name></CanvasApp>");

        Assert.Contains("app_1", Assert.Single(Read(t).CanvasAppTexts));
    }

    [Fact]
    public void Missing_optional_files_give_empty_lists()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", SolutionXml.Manifest());

        var s = Read(t);

        Assert.Empty(s.RootComponents);
        Assert.Empty(s.MissingDependencies);
        Assert.Empty(s.ConnectionReferences);
        Assert.Empty(s.EnvironmentVariables);
        Assert.Empty(s.Workflows);
        Assert.Empty(s.CanvasAppTexts);
    }

    [Fact]
    public void Malformed_manifest_throws_InvalidDataException_naming_the_file()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", "<ImportExportXml><oops>");

        var ex = Assert.Throws<InvalidDataException>(() => Read(t));

        Assert.Contains("Other/Solution.xml", ex.Message);
    }

    [Fact]
    public void Folder_without_manifest_throws_InvalidDataException()
    {
        using var t = new TempFolder();

        Assert.Throws<InvalidDataException>(() => Read(t));
    }

    [Fact]
    public void Malformed_environment_values_json_throws_InvalidDataException()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("environmentvariabledefinitions/demo_A/environmentvariabledefinition.xml", "<environmentvariabledefinition schemaname=\"demo_A\" />")
            .Write("environmentvariabledefinitions/demo_A/environmentvariablevalues.json", "{ nope");

        var ex = Assert.Throws<InvalidDataException>(() => Read(t));

        Assert.Contains("environmentvariablevalues.json", ex.Message);
    }

    [Fact]
    public void Doctype_in_solution_xml_is_rejected()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", ZipReaderTests.Xxe);

        Assert.Throws<InvalidDataException>(() => Read(t));
    }

    [Fact]
    public void File_over_the_size_cap_throws_InvalidDataException()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", "<ImportExportXml>" + new string(' ', 51 * 1024 * 1024) + "</ImportExportXml>");

        Assert.Throws<InvalidDataException>(() => Read(t));
    }

    [Fact]
    public void Solution_over_the_total_budget_throws_InvalidDataException()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Other/Customizations.xml", SolutionXml.Customizations(new string(' ', 2000)));

        var ex = Assert.Throws<InvalidDataException>(() => XmlUnpackReader.Read(t.Path, new ReadBudget(maxTotalBytes: 1500)));

        Assert.Contains("Customizations.xml", ex.Message);
    }

    [Fact]
    public void Symlinked_files_are_not_read()
    {
        using var outside = new TempFolder().Write("def.xml", "<environmentvariabledefinition schemaname=\"evil_A\" />");
        using var t = new TempFolder().Write("Other/Solution.xml", SolutionXml.Manifest());
        Directory.CreateDirectory(Path.Combine(t.Path, "environmentvariabledefinitions", "evil_A"));
        if (!TrySymlink(Path.Combine(t.Path, "environmentvariabledefinitions", "evil_A", "environmentvariabledefinition.xml"), Path.Combine(outside.Path, "def.xml")))
        {
            Assert.Skip("Symbolic links are not available here.");
        }

        Assert.Empty(Read(t).EnvironmentVariables);
    }

    internal static bool TrySymlink(string link, string target)
    {
        try
        {
            if (Directory.Exists(target))
            {
                Directory.CreateSymbolicLink(link, target);
            }
            else
            {
                File.CreateSymbolicLink(link, target);
            }

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    [Fact]
    public void Workflow_json_name_is_matched_with_backslashes_and_case_and_reports_the_actual_file()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Workflows/Flow-AA.json.data.xml", """
                <Workflow WorkflowId="{AA-11}" Name="F"><JsonFileName>\workflows\flow-aa.json</JsonFileName><Category>5</Category></Workflow>
                """)
            .Write("Workflows/Flow-AA.json", "{}");

        var w = Assert.Single(Read(t).Workflows);

        Assert.Equal("Workflows/Flow-AA.json", w.File);
        Assert.Equal("{}", w.ClientData);
    }

    [Fact]
    public void Cloud_flow_with_missing_json_file_throws_InvalidDataException_naming_it()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Workflows/Flow-AA.json.data.xml", """
                <Workflow WorkflowId="{AA-11}" Name="F"><JsonFileName>/Workflows/Flow-AA.json</JsonFileName><Category>5</Category></Workflow>
                """);

        var ex = Assert.Throws<InvalidDataException>(() => Read(t));

        Assert.Contains("Workflows/Flow-AA.json", ex.Message);
    }

    [Fact]
    public void Deeply_nested_xml_throws_InvalidDataException_naming_the_file()
    {
        var nested = string.Concat(Enumerable.Repeat("<a>", 1000)) + string.Concat(Enumerable.Repeat("</a>", 1000));
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Other/Customizations.xml", $"<ImportExportXml>{nested}</ImportExportXml>");

        var ex = Assert.Throws<InvalidDataException>(() => Read(t));

        Assert.StartsWith("Other/Customizations.xml: ", ex.Message);
        Assert.Contains("nested", ex.Message);
    }

    private static string Fixture(string name) => Path.Combine(Golden.RepoRoot, "tests", "fixtures", name);

    [Fact]
    public void Fixture_coe_auditlogs()
    {
        var s = XmlUnpackReader.Read(Fixture("coe-auditlogs"));

        Assert.Equal("CenterofExcellenceAuditLogs", s.UniqueName);
        Assert.Equal("3.27.2", s.Version);
        Assert.False(s.Managed);
        Assert.Equal("admin", s.Publisher.Prefix);
        Assert.Equal(2, s.RootComponents.Count);
        Assert.Equal(5, s.ConnectionReferences.Count);
        Assert.Equal(5, s.EnvironmentVariables.Count);
        Assert.Equal(23, s.MissingDependencies.Count);
        Assert.All(s.MissingDependencies, d => Assert.Equal("CenterofExcellenceCoreComponents (4.4.1)", d.Required.Solution));
        Assert.Contains(s.MissingDependencies, d =>
            d.Required.Type == "29" && d.Required.Id == "5625768c-bd3d-ec11-8c63-00224829720b");
        Assert.Contains(s.MissingDependencies, d =>
            d.Required.Type == "connectionreference" && d.Required.SchemaName == "admin_sharedcommondataserviceforapps_98924");
        Assert.Equal(2, s.Workflows.Count);
        Assert.All(s.Workflows, w =>
        {
            Assert.Equal(0, w.StateCode);
            Assert.Equal(5, w.Category);
            Assert.NotNull(w.ClientData);
            Assert.StartsWith("Workflows/", w.File);
        });
    }

    [Fact]
    public void Fixture_alm_makers_env_var_default_and_current_value()
    {
        var s = XmlUnpackReader.Read(Fixture("alm-makers"));

        var v = Assert.Single(s.EnvironmentVariables, e => e.SchemaName == "cat_DependentEnvironmentUrl");
        Assert.Equal("https://githubdevops.crm.dynamics.com/", v.DefaultValue);
        Assert.Equal("https://githubdevops.crm.dynamics.com/", v.CurrentValue);
        Assert.Equal("environmentvariabledefinitions/cat_DependentEnvironmentUrl/environmentvariablevalues.json", v.ValuesFile);
        Assert.Equal("cat", s.Publisher.Prefix);
    }

    [Fact]
    public void Fixture_alm_sample_has_canvas_app_text_and_one_missing_dependency()
    {
        var s = XmlUnpackReader.Read(Fixture("alm-sample"));

        Assert.All(s.CanvasAppTexts, text => Assert.False(string.IsNullOrWhiteSpace(text)));
        Assert.NotEmpty(s.CanvasAppTexts);
        Assert.Equal(3, s.ConnectionReferences.Count);
        Assert.Equal(7, s.EnvironmentVariables.Count);
        Assert.Equal(6, s.Workflows.Count);
        var d = Assert.Single(s.MissingDependencies);
        Assert.Equal("AppModuleWebResources (2.5)", d.Required.Solution);
    }
}