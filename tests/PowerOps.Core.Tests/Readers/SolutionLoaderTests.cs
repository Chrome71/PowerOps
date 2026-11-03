using PowerOps.Core.Readers;
using PowerOps.Core.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class SolutionLoaderTests
{
    private const string MappingFile = "Assets/botcomponent_connectionreferenceset.xml";

    private static TempFolder AgentSolution(string? mapping)
    {
        var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Other/Customizations.xml", SolutionXml.Customizations("""
                <connectionreferences>
                  <connectionreference connectionreferencelogicalname="demo_mapped"><connectorid>/providers/Microsoft.PowerApps/apis/shared_one</connectorid></connectionreference>
                  <connectionreference connectionreferencelogicalname="demo_other"><connectorid>/providers/Microsoft.PowerApps/apis/shared_two</connectorid></connectionreference>
                </connectionreferences>
                """));
        return mapping is null ? t : t.Write(MappingFile, mapping);
    }

    private static List<string?> Unused(string path) =>
        Analyzer.Run(SolutionLoader.Load(path)).Where(f => f.RuleId == "PO005").Select(f => f.Component).Order(StringComparer.Ordinal).ToList();

    [Fact]
    public void An_agent_mapping_counts_as_a_use_in_a_folder_and_in_a_zip()
    {
        using var t = AgentSolution("""
            <botcomponent_connectionreferenceset>
              <botcomponent_connectionreference botcomponentid.schemaname="demo_Agent.topic.one" connectionreferenceid.connectionreferencelogicalname="DEMO_MAPPED">
                <iscustomizable>1</iscustomizable>
              </botcomponent_connectionreference>
              <botcomponent_connectionreference botcomponentid.schemaname="demo_Agent.topic.two" />
            </botcomponent_connectionreferenceset>
            """);
        using var zipDir = new TempFolder();
        var zip = Path.Combine(zipDir.Path, "agent.zip");
        SolutionZip.Create(t.Path, zip);

        Assert.Equal(["demo_other"], Unused(t.Path));
        Assert.Equal(["demo_other"], Unused(zip));
    }

    [Fact]
    public void WithzipDirthe_agent_mapping_both_references_are_unused()
    {
        using var t = AgentSolution(null);

        Assert.Equal(["demo_mapped", "demo_other"], Unused(t.Path));
    }

    [Fact]
    public void A_malformed_agent_mapping_names_its_path()
    {
        using var t = AgentSolution("<botcomponent_connectionreferenceset><oops>");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains($": {MappingFile}: ", ex.Message);
    }

    [Fact]
    public void Folder_errors_name_the_solution_folder()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", "<ImportExportXml><oops>");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains("Other/Solution.xml", ex.Message);
    }

    [Fact]
    public void Zip_errors_name_the_zip()
    {
        using var t = new TempFolder().Write("bad.zip", "this is not a zip");
        var zip = Path.Combine(t.Path, "bad.zip");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(zip));

        Assert.StartsWith(zip, ex.Message);
    }

    [Fact]
    public void Io_errors_surface_as_InvalidDataException_with_the_path()
    {
        using var t = new TempFolder();
        var zip = Path.Combine(t.Path, "missing.zip");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(zip));

        Assert.StartsWith(zip, ex.Message);
    }
}