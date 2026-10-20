using PowerOps.Core.Model;
using PowerOps.Core.Rules;

namespace PowerOps.Core.Tests.Rules;

public class ErrorRuleTests
{
    private static IReadOnlyList<Finding> Run(IRule rule, Solution s) => rule.Evaluate(s).ToList();

    [Fact]
    public void PO001_fires_for_undefined_reference()
    {
        var s = B.Empty() with { Workflows = [B.Flow(refs: ["demo_missing"])] };

        var f = Assert.Single(Run(new PO001UndefinedConnectionReference(), s));

        Assert.Equal("demo_missing", f.Component);
        Assert.Equal(Severity.Error, f.Severity);
        Assert.Equal("Workflows/Flow-11111111-1111-1111-1111-111111111111.json", f.File);
    }

    [Fact]
    public void PO001_quiet_when_defined()
    {
        var s = B.Empty() with { Workflows = [B.Flow(refs: ["demo_a"])], ConnectionReferences = [B.Ref("DEMO_A")] };

        Assert.Empty(Run(new PO001UndefinedConnectionReference(), s));
    }

    [Fact]
    public void PO001_quiet_when_declared_dependency()
    {
        var s = B.Empty() with { Workflows = [B.Flow(refs: ["demo_a"])], MissingDependencies = [B.Dep("connectionreference", "Demo_A")] };

        Assert.Empty(Run(new PO001UndefinedConnectionReference(), s));
    }

    [Fact]
    public void PO001_ignores_non_cloud_flows()
    {
        var s = B.Empty() with { Workflows = [B.Flow(refs: ["demo_a"], category: 0)] };

        Assert.Empty(Run(new PO001UndefinedConnectionReference(), s));
    }

    [Fact]
    public void PO002_fires_for_required_without_values()
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_a", required: true)] };

        var f = Assert.Single(Run(new PO002RequiredEnvironmentVariableWithoutValue(), s));

        Assert.Equal("demo_a", f.Component);
    }

    [Theory]
    [InlineData(true, "x", null)]
    [InlineData(true, null, "x")]
    [InlineData(false, null, null)]
    public void PO002_quiet_when_not_required_or_has_value(bool required, string? def, string? cur)
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_a", required, def, cur)] };

        Assert.Empty(Run(new PO002RequiredEnvironmentVariableWithoutValue(), s));
    }

    [Fact]
    public void PO003_fires_without_owning_solution()
    {
        var s = B.Empty() with { MissingDependencies = [B.Dep("29", null, "abc", owner: null)] };

        var f = Assert.Single(Run(new PO003DependencyWithoutOwningSolution(), s));

        Assert.Equal("Other/Solution.xml", f.File);
        Assert.Equal("abc", f.Component);
    }

    [Fact]
    public void PO003_reports_one_finding_per_required_component_naming_dependents()
    {
        var required = new DependencyNode("29", "admin_AdminMail", null, null, null);
        var s = B.Empty() with
        {
            MissingDependencies =
            [
                new(required, new DependencyNode("29", null, "b", "Flow B", null)),
                new(required, new DependencyNode("29", null, "a", "Flow A", null)),
                new(required with { SchemaName = "ADMIN_adminmail" }, new DependencyNode("29", null, "a", "Flow A", null)),
            ],
        };

        var f = Assert.Single(Run(new PO003DependencyWithoutOwningSolution(), s));

        Assert.Equal("Missing dependency 'admin_AdminMail' (needed by 'Flow A', 'Flow B') has no owning solution; the import will fail.", f.Message);
    }

    [Fact]
    public void PO003_quiet_with_owning_solution()
    {
        var s = B.Empty() with { MissingDependencies = [B.Dep("29", null, "abc")] };

        Assert.Empty(Run(new PO003DependencyWithoutOwningSolution(), s));
    }

    [Fact]
    public void PO004_fires_for_unknown_child()
    {
        var s = B.Empty() with { Workflows = [B.Flow(children: ["AAAA"])] };

        var f = Assert.Single(Run(new PO004UndefinedChildFlow(), s));

        Assert.Equal("aaaa", f.Component);
    }

    [Fact]
    public void PO004_quiet_when_child_in_solution()
    {
        var child = B.Flow(id: "aaaa", name: "Child");
        var s = B.Empty() with { Workflows = [B.Flow(children: ["AAAA"]), child] };

        Assert.Empty(Run(new PO004UndefinedChildFlow(), s));
    }

    [Fact]
    public void PO004_quiet_when_declared_dependency()
    {
        var s = B.Empty() with { Workflows = [B.Flow(children: ["AAAA"])], MissingDependencies = [B.Dep("29", null, "aaaa")] };

        Assert.Empty(Run(new PO004UndefinedChildFlow(), s));
    }

    private const string Manifest = "solutions/Demo/solution.yml";

    private static Solution Yaml(string[] folders, params RootComponent[] roots) =>
        B.Empty() with { RootComponents = roots, SourceFolders = folders, ManifestFile = Manifest };

    [Fact]
    public void PO013_fires_for_root_component_without_source_folder()
    {
        var s = Yaml(["entities/demo_other"], new RootComponent("1", "demo_item", null));

        var f = Assert.Single(Run(new PO013RootComponentWithoutSourceFolder(), s));

        Assert.Equal("Root component 'demo_item' (type 1) has no source folder 'entities/demo_item'.", f.Message);
        Assert.Equal("demo_item", f.Component);
        Assert.Equal("solutions/Demo/rootcomponents.yml", f.File);
        Assert.Equal(Severity.Error, f.Severity);
    }

    [Fact]
    public void PO013_fires_with_empty_source_folders()
    {
        var s = Yaml([], new RootComponent("300", "demo_app", null));

        var f = Assert.Single(Run(new PO013RootComponentWithoutSourceFolder(), s));

        Assert.Contains("'canvasapps/demo_app'", f.Message);
    }

    [Fact]
    public void PO013_names_cloud_flow_by_normalized_id()
    {
        var s = Yaml([], new RootComponent("29", null, "{ABCD-1}"));

        var f = Assert.Single(Run(new PO013RootComponentWithoutSourceFolder(), s));

        Assert.Equal("abcd-1", f.Component);
        Assert.Contains("'modernflows/abcd-1'", f.Message);
    }

    [Fact]
    public void PO013_ignores_case_of_folder()
    {
        var s = Yaml(["entities/DEMO_Item", "modernflows/abcd-1", "environmentvariabledefinitions/Demo_Var"],
            new RootComponent("1", "demo_item", null),
            new RootComponent("29", null, "ABCD-1"),
            new RootComponent("380", "demo_var", null));

        Assert.Empty(Run(new PO013RootComponentWithoutSourceFolder(), s));
    }

    [Fact]
    public void PO013_quiet_for_xml_layout()
    {
        var s = B.Empty() with { RootComponents = [new RootComponent("1", "demo_item", null)] };

        Assert.Empty(Run(new PO013RootComponentWithoutSourceFolder(), s));
    }

    [Fact]
    public void PO013_skips_unchecked_types_and_missing_names()
    {
        var s = Yaml([], new RootComponent("61", "demo_web", null), new RootComponent("29", null, null), new RootComponent("1", null, null));

        Assert.Empty(Run(new PO013RootComponentWithoutSourceFolder(), s));
    }

    [Fact]
    public void Analyzer_sorts_findings_ordinally_by_rule_then_component_message_file()
    {
        var s = B.Empty() with
        {
            Workflows = [B.Flow(refs: ["demo_b"], children: ["bbbb"])],
            EnvironmentVariables = [B.Env("demo_a", required: true)],
        };

        var ids = Analyzer.Run(s).Select(f => f.RuleId + "|" + f.Component + "|" + f.Message + "|" + f.File).ToList();

        Assert.Equal(ids.Order(StringComparer.Ordinal).ToList(), ids);
        Assert.Equal(3, ids.Count);
    }
}