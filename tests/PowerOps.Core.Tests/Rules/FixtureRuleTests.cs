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
    public void Coe_auditlogs_declared_dependencies_raise_no_errors()
    {
        var f = Analyze("coe-auditlogs");

        foreach (var id in new[] { "PO001", "PO003", "PO004" })
        {
            Assert.Empty(Components(f, id));
        }
    }

    [Fact]
    public void Coe_auditlogs_warnings()
    {
        var f = Analyze("coe-auditlogs");

        Assert.Equal(
            ["admin_sharedadmin5foffice2036520management20api5f7fc9343cf45734fe_8f08c", "admin_sharedcommondataserviceforapps_8f10b", "new_CoEAuditLogsO365Outlook"],
            Components(f, "PO005"));
        Assert.Equal(["new_CoEAuditLogsDataverse", "new_CoEAuditLogsO365Outlook"], Components(f, "PO009"));
        Assert.Equal(2, Components(f, "PO011").Count);
    }

    [Fact]
    public void Alm_makers_default_and_shipped_value()
    {
        var f = Analyze("alm-makers");

        Assert.Contains("cat_DependentEnvironmentUrl", Components(f, "PO007"));
        Assert.Contains("cat_DependentEnvironmentUrl", Components(f, "PO008"));
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
    public void Contoso_zip_uses_default_publisher()
    {
        Assert.Single(Components(Analyze("zips/Contoso_sample_1_0_0_1_managed.zip"), "PO010"));
    }
}