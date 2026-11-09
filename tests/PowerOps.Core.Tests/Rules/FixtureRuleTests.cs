using PowerOps.Core.Readers;
using PowerOps.Core.Rules;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Rules;

public class FixtureRuleTests
{
    private static IReadOnlyList<Finding> Analyze(string relative) =>
        Analyzer.Run(SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", relative)));

    private static List<string?> Components(IReadOnlyList<Finding> f, string rule) =>
        f.Where(x => x.RuleId == rule).Select(x => x.Component).Order(StringComparer.Ordinal).ToList();

    [Fact]
    public void Alm_makers_default_and_shipped_value()
    {
        var f = Analyze("alm-makers");

        Assert.Contains("cat_DependentEnvironmentUrl", Components(f, "PO007"));
        Assert.Contains("cat_DependentEnvironmentUrl", Components(f, "PO008"));
    }

    [Fact]
    public void Alm_makers_flags_two_update_triggers_without_filter_columns()
    {
        Assert.Equal(["When_Project_is_Deleted", "When_a_BuildRequest_in_Cancelled"], Components(Analyze("alm-makers"), "PO012"));
    }

    [Fact]
    public void Seed_v1_is_clean()
    {
        Assert.Empty(Analyze("powerops-seed/v1"));
    }

    [Fact]
    public void Seed_v2_flags_hard_coded_url_and_unused_connection_reference()
    {
        var f = Analyze("powerops-seed/v2");

        Assert.Equal(["PO005", "PO006"], f.Select(x => x.RuleId).Order(StringComparer.Ordinal));
        Assert.Equal(["pops_SpareDataverse"], Components(f, "PO005"));
        Assert.Contains("https://contoso.sharepoint.com/sites/ops", f.Single(x => x.RuleId == "PO006").Message);
    }

    [Fact]
    public void Seed_v2_zip_and_unpacked_findings_come_in_the_same_order()
    {
        var dir = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v2");
        var temp = Directory.CreateTempSubdirectory("powerops-test-").FullName;
        try
        {
            var zipPath = Path.Combine(temp, "v2.zip");
            SolutionZip.Create(dir, zipPath);

            static List<(string, string?, string)> Key(IReadOnlyList<Finding> f) =>
                f.Select(x => (x.RuleId, x.Component, x.Message)).ToList();

            var fromZip = Key(Analyzer.Run(SolutionLoader.Load(zipPath)));

            Assert.NotEmpty(fromZip);
            Assert.Equal(Key(Analyze("powerops-seed/v2")), fromZip);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }
    private static List<(string, string?, string)> Keys(IReadOnlyList<Finding> f) =>
        f.Select(x => (x.RuleId, x.Component, x.Message)).ToList();

    private static IReadOnlyList<Finding> AnalyzeYaml(string version) =>
        Analyze(Path.Combine("powerops-seed-yaml", version, "solutions", "PowerOpsSeed", "solution.yml"));

    [Fact]
    public void Seed_yaml_v1_matches_the_xml_layout_with_no_findings()
    {
        Assert.Empty(Keys(Analyze(Path.Combine("powerops-seed", "v1"))));
        Assert.Empty(Keys(AnalyzeYaml("v1")));
    }

    [Fact]
    public void Seed_yaml_v2_matches_the_xml_layout_plus_the_canvas_formula()
    {
        const string url = "https://contoso.sharepoint.com/sites/ops";
        var xml = Keys(Analyze(Path.Combine("powerops-seed", "v2")));

        Assert.Equal(
            [
                ("PO005", "pops_SpareDataverse", "Connection reference 'pops_SpareDataverse' is not used by any flow or app."),
                ("PO006", url, $"Flow 'Stamp work item status' hard-codes the URL '{url}'."),
            ],
            xml);
        Assert.Equal(
            [.. xml, ("PO014", url, $"Canvas app 'Work Items' hard-codes the URL '{url}' in Button1.OnSelect.")],
            Keys(AnalyzeYaml("v2")));
    }

    [Fact]
    public void Core_portal_loads_its_components_and_is_quiet_on_the_power_pages_rules()
    {
        var s = SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "core-portal"));

        Assert.Equal(122, s.PowerPageComponents.Count);
        Assert.Equal(50, s.PowerPageComponents.Count(c => c.Type == 9));
        Assert.Equal(3, s.PowerPageComponents.Count(c => c.Type == 11));
        Assert.Equal(0, s.PowerPageComponents.Count(c => c.Type == 18));
        Assert.All(s.PowerPageComponents, c => Assert.True(c.Active));
        Assert.DoesNotContain(Analyzer.Run(s), f => f.RuleId is "PO019" or "PO020" or "PO021" or "PO022" or "PO023");
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void Alm_sample_reads_its_app_module_site_map_and_canvas_app_and_is_quiet_on_PO024_and_PO025()
    {
        var s = SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "alm-sample"));

        var app = Assert.Single(s.AppModules);
        Assert.Equal("cat_AlmAcceleratorSampleMda", app.UniqueName);
        Assert.Equal(["cat_AlmAcceleratorSampleMda"], app.SiteMaps);
        Assert.Empty(app.Pages);
        var map = Assert.Single(s.SiteMaps);
        Assert.Equal("cat_AlmAcceleratorSampleMda", map.UniqueName);
        Assert.Empty(map.Pages);
        Assert.Empty(map.GenPageIds);
        var canvas = Assert.Single(s.CanvasApps);
        Assert.Equal("cat_devopskitsamplecanvasapp_c7ec5", canvas.Name);
        Assert.Equal(8, canvas.Connections!.Count);
        Assert.Equal(5, canvas.Connections.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(Analyzer.Run(s), f => f.RuleId is "PO024" or "PO025");
        Assert.DoesNotContain(Analyzer.NotChecked(s), n => n.StartsWith("PO024", StringComparison.Ordinal) || n.StartsWith("PO025", StringComparison.Ordinal));
    }

    [Fact]
    public void Equipment_checkout_gives_PO032_once_PO033_twice_and_PO034_twice_and_nothing_else()
    {
        var s = SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "equipment-checkout"));

        Assert.Equal(["Front desk", "IT staff"], s.Roles.Select(r => r.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["627090ff-40a3-4053-8790-584edc5be201", "119f245c-3cc8-4b62-b31c-d1a046ced15d"], Assert.Single(s.AppModules).Roles);
        Assert.Equal(
            [
                ("PO032", "equipment_checkout_a1a05935"),
                ("PO033", "Front desk"),
                ("PO033", "IT staff"),
                ("PO034", "Front desk"),
                ("PO034", "IT staff"),
            ],
            Analyzer.Run(s).Select(f => (f.RuleId, f.Component)));
        Assert.All(Analyzer.Run(s).Where(f => f.RuleId == "PO034"), f => Assert.Contains("has Create and Write on 'SharePointData'", f.Message));
        Assert.Empty(Analyzer.NotChecked(s));
    }

    [Fact]
    public void Alm_sample_reads_the_dependencies_of_its_canvas_app_and_is_quiet_on_PO026()
    {
        var s = SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "alm-sample"));

        var canvas = Assert.Single(s.CanvasApps);
        Assert.Equal(0, canvas.Type);
        Assert.Equal("CanvasApps/cat_devopskitsamplecanvasapp_c7ec5.meta.xml", canvas.File);
        Assert.Empty(canvas.Tables!);
        Assert.Equal(["cat_5Fenvironmentvariableconnector"], canvas.Connectors);
        Assert.DoesNotContain(Analyzer.Run(s), f => f.RuleId == "PO026");
        Assert.DoesNotContain(Analyzer.NotChecked(s), n => n.StartsWith("PO026", StringComparison.Ordinal));
    }

    [Fact]
    public void Dataverse_tools_fixture_reads_every_tool_with_its_table_and_gives_only_PO030_and_PO034()
    {
        var s = SolutionLoader.Load(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "agents", "dataverse-tools"));

        var agent = Assert.Single(s.Agents);
        Assert.Equal(
            [
                ("demo_Helper.action.MicrosoftDataverse-AddNewRow", "demo_Ticket"),
                ("demo_Helper.action.MicrosoftDataverse-ListRowsQueues", "demo_Queue"),
                ("demo_Helper.action.MicrosoftDataverse-ListRowsTickets", "demo_Ticket"),
                ("demo_Helper.action.MicrosoftDataverse-UpdateRow", "demo_Ticket"),
            ],
            agent.Tools.Select(t => (t.SchemaName, t.Table!.Name!)));
        Assert.Empty(agent.UnreadFiles);
        Assert.Empty(agent.OtherComponents);
        Assert.Empty(agent.ActionDialogs);
        Assert.Equal(2, Assert.Single(s.Workflows).Category);
        Assert.Equal(
            [("PO030", "demo_Helper.action.MicrosoftDataverse-UpdateRow"), ("PO034", "Service desk")],
            Analyzer.Run(s).Select(f => (f.RuleId, f.Component)));
        Assert.Contains("has Create and Write on 'SharePointData'", Analyzer.Run(s).Single(f => f.RuleId == "PO034").Message);
        Assert.Empty(Analyzer.NotChecked(s));
    }
}