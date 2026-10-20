using PowerOps.Core.Model;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class YamlReaderTests
{
    private static TempFolder Layout(string managed = "0", bool publisher = true, bool components = true, string listed = "/publishers/demopub")
    {
        var t = new TempFolder().Write("solutions/Demo/solution.yml", $"""
            ImportExportXml:
              SolutionManifest:
                UniqueName: Demo
                Version: 1.2.3.4
                Managed: {managed}
                Publisher:
                  UniqueName: demopub
            """);
        if (publisher)
        {
            t.Write("publishers/demopub/publisher.yml", """
                Publisher:
                  UniqueName: demopub
                  CustomizationPrefix: demo
                """);
        }

        if (components)
        {
            t.Write("solutions/Demo/solutioncomponents.yml", $"""
                SolutionComponents:
                  Component:
                  - '@path': {listed}
                """);
        }

        return t;
    }

    private static string SolutionYml(TempFolder t) => Path.Combine(t.Path, "solutions", "Demo", "solution.yml");

    [Fact]
    public void Inline_managed_one_is_managed()
    {
        using var t = Layout(managed: "1");

        var s = SolutionLoader.Load(SolutionYml(t));

        Assert.True(s.Managed);
        Assert.Equal("Demo", s.UniqueName);
        Assert.Equal("1.2.3.4", s.Version);
        Assert.Equal(new Publisher("demopub", "demo"), s.Publisher);
        Assert.Equal("solutions/Demo/solution.yml", s.ManifestFile);
    }

    [Fact]
    public void One_segment_entry_gives_the_component_folders_below_it()
    {
        using var t = Layout(listed: "/entities").Write("entities/demo_thing/entity.yml", "Entity:\n");

        Assert.Equal(["entities/demo_thing"], SolutionLoader.Load(SolutionYml(t)).SourceFolders);
    }

    [Fact]
    public void Reserved_xml_name_is_an_error_starting_with_the_path()
    {
        using var t = Layout().Write("publishers/demopub/publisher.yml", """
            Publisher:
              '@xmlns': http://www.w3.org/2000/xmlns/
              UniqueName: demopub
            """);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(SolutionYml(t)));
        Assert.StartsWith(SolutionYml(t), e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cloud_flow_definition_is_found_and_parsed()
    {
        using var t = Layout(listed: "/modernflows/f1")
            .Write("modernflows/f1/workflow.yml", """
                Workflow:
                  '@WorkflowId': '{00000000-0000-0000-0000-000000000001}'
                  '@Name': Demo flow
                  JsonFileName: /modernflows/f1/flow.json
                  Category: 5
                  StateCode: 1
                """)
            .Write("modernflows/f1/flow.json", """{"properties":{"definition":{}}}""");

        var w = Assert.Single(SolutionLoader.Load(SolutionYml(t)).Workflows);

        Assert.NotNull(w.ClientData);
        Assert.Equal("modernflows/f1/flow.json", w.File);
        Assert.Equal("00000000-0000-0000-0000-000000000001", w.Id);
        Assert.Equal(1, w.StateCode);
    }

    [Fact]
    public void Missing_publisher_names_the_solution_yml()
    {
        using var t = Layout(publisher: false);

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(SolutionYml(t)));

        Assert.StartsWith(SolutionYml(t), ex.Message);
    }

    [Fact]
    public void Missing_solutioncomponents_names_the_solution_yml()
    {
        using var t = Layout(components: false);

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(SolutionYml(t)));

        Assert.StartsWith(SolutionYml(t), ex.Message);
    }

    [Fact]
    public void Anchors_and_aliases_are_rejected()
    {
        using var t = Layout().Write("solutions/Demo/rootcomponents.yml", """
            RootComponents:
              RootComponent:
              - &first
                '@type': 1
              - *first
            """);

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(SolutionYml(t)));

        Assert.StartsWith(SolutionYml(t), ex.Message);
        Assert.Contains("anchors", ex.Message);
    }

    [Fact]
    public void Nesting_deeper_than_the_xml_cap_is_rejected()
    {
        var deep = string.Concat(Enumerable.Range(0, 300).Select(i => new string(' ', i) + "a:\n")) + new string(' ', 300) + "b: 1\n";
        using var t = Layout().Write("solutions/Demo/rootcomponents.yml", deep);

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(SolutionYml(t)));

        Assert.StartsWith(SolutionYml(t), ex.Message);
        Assert.Contains("deeper", ex.Message);
    }

    private static string Manifest(string name, string publisher) => $"""
        ImportExportXml:
          SolutionManifest:
            UniqueName: {name}
            Version: 1.0.0.0
            Managed: 0
            Publisher:
              UniqueName: {publisher}
        """;

    // Two solutions of one YAML root: a shared connection reference, and a table only B lists.
    private static TempFolder TwoSolutionRoot() => new TempFolder()
        .Write("solutions/A/solution.yml", Manifest("A", "pubA"))
        .Write("solutions/A/solutioncomponents.yml", "SolutionComponents:\n  Component:\n  - '@path': /publishers/pubA\n  - '@path': /connectionreferences/shared\n")
        .Write("solutions/B/solution.yml", Manifest("B", "pubB"))
        .Write("solutions/B/solutioncomponents.yml", "SolutionComponents:\n  Component:\n  - '@path': /publishers/pubB\n  - '@path': /connectionreferences/shared\n  - '@path': /entities/thing\n")
        .Write("publishers/pubA/publisher.yml", "Publisher:\n  UniqueName: pubA\n  CustomizationPrefix: aa\n")
        .Write("publishers/pubB/publisher.yml", "Publisher:\n  UniqueName: pubB\n  CustomizationPrefix: bb\n")
        .Write("connectionreferences/shared/connectionreference.yml", "connectionreference:\n  '@connectionreferencelogicalname': shared\n  connectorid: /providers/Microsoft.PowerApps/apis/shared_x\n")
        .Write("entities/thing/entity.yml", "Entity:\n  Name: thing\n");

    private static string Yml(TempFolder t, string name) => Path.Combine(t.Path, "solutions", name, "solution.yml");

    [Fact]
    public void Solutions_of_one_root_keep_their_own_publisher_and_components()
    {
        using var t = TwoSolutionRoot();
        var context = new LoadContext();

        var a = SolutionLoader.Load(Yml(t, "A"), context);
        var b = SolutionLoader.Load(Yml(t, "B"), context);

        Assert.Equal("aa", a.Publisher.Prefix);
        Assert.Equal("bb", b.Publisher.Prefix);
        Assert.DoesNotContain("entities/thing", a.SourceFolders!);
        Assert.Contains("entities/thing", b.SourceFolders!);
        Assert.Single(a.ConnectionReferences);
        Assert.Single(b.ConnectionReferences);
    }

    [Fact]
    public void A_file_shared_by_solutions_of_one_root_counts_against_the_budget_once()
    {
        using var t = TwoSolutionRoot();
        string[] read =
        [
            "solutions/A/solution.yml", "solutions/A/solutioncomponents.yml", "solutions/B/solution.yml", "solutions/B/solutioncomponents.yml",
            "publishers/pubA/publisher.yml", "publishers/pubB/publisher.yml", "connectionreferences/shared/connectionreference.yml",
        ];
        var once = read.Sum(r => new FileInfo(Path.Combine(t.Path, Path.Combine(r.Split('/')))).Length);

        var enough = new LoadContext(() => new ReadBudget(once));
        SolutionLoader.Load(Yml(t, "A"), enough);
        SolutionLoader.Load(Yml(t, "B"), enough);

        var tooSmall = new LoadContext(() => new ReadBudget(once - 1));
        SolutionLoader.Load(Yml(t, "A"), tooSmall);
        Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(Yml(t, "B"), tooSmall));
    }

    [Fact]
    public void A_flow_definition_outside_the_listed_entries_is_not_read()
    {
        using var t = Layout(listed: "/modernflows/f1/workflow.yml")
            .Write("modernflows/f1/workflow.yml", """
                Workflow:
                  '@Name': Demo flow
                  JsonFileName: /modernflows/f1/flow.json
                  Category: 5
                """)
            .Write("modernflows/f1/flow.json", """{"properties":{"definition":{}}}""");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(SolutionYml(t)));

        Assert.StartsWith(SolutionYml(t), ex.Message);
        Assert.Contains("which is missing", ex.Message);
    }

    [Fact]
    public void Run_reports_the_yaml_root_and_names_the_solution_yml_in_errors()
    {
        using var t = TwoSolutionRoot();

        var result = AnalysisResult.Run([t.Path], t.Path);

        Assert.Equal([".", "."], result.Solutions.Select(s => s.Path));
        Assert.Equal(["A", "B"], result.Solutions.Select(s => s.Solution.UniqueName));

        File.Delete(Path.Combine(t.Path, "publishers", "pubB", "publisher.yml"));
        var ex = Assert.Throws<InvalidDataException>(() => AnalysisResult.Run([t.Path], t.Path));
        Assert.StartsWith(Yml(t, "B"), ex.Message);
    }

    [Fact]
    public void A_solution_yml_outside_a_solutions_folder_is_rejected()
    {
        using var t = new TempFolder().Write("other/Demo/solution.yml", "ImportExportXml:\n");
        var path = Path.Combine(t.Path, "other", "Demo", "solution.yml");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(path));

        Assert.StartsWith(path, ex.Message);
    }

    [Fact]
    public void Reads_the_committed_fixture()
    {
        var path = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed-yaml", "v2", "solutions", "PowerOpsSeed", "solution.yml");

        var s = SolutionLoader.Load(path);

        Assert.Equal("PowerOpsSeed", s.UniqueName);
        Assert.Equal(new Publisher("powerops", "pops"), s.Publisher);
        Assert.Equal("solutions/PowerOpsSeed/solution.yml", s.ManifestFile);
        Assert.Collection(
            s.RootComponents,
            c => Assert.Equal(new RootComponent("1", "pops_workitem", null), c),
            c => Assert.Equal(new RootComponent("29", null, "4c8dd007-dfcb-f111-a05c-0022480697a8"), c),
            c => Assert.Equal(new RootComponent("300", "pops_workitems_2af60", null), c));
        Assert.Equal(2, s.ConnectionReferences.Count);
        Assert.NotNull(Assert.Single(s.Workflows).ClientData);
        var v = Assert.Single(s.EnvironmentVariables);
        Assert.Equal("pops_DefaultStatus", v.SchemaName);
        Assert.Equal("New", v.DefaultValue);
        Assert.StartsWith("<CanvasApp><Name>pops_workitems_2af60</Name>", Assert.Single(s.CanvasAppTexts));
        Assert.Contains("entities/pops_workitem", s.SourceFolders!);
        Assert.Contains("modernflows/4c8dd007-dfcb-f111-a05c-0022480697a8", s.SourceFolders!);
    }

    private const string SrcDir = "canvasapps/app1/app1_DocumentUri/Src/";

    private static TempFolder CanvasLayout(string appYml = "CanvasApp:\n  Name: app1\n  DisplayName: Contoso Tracker\n") =>
        Layout(listed: "/canvasapps/app1").Write("canvasapps/app1/canvasapp.yml", appYml);

    private static IReadOnlyList<CanvasFormula> Formulas(TempFolder t) => SolutionLoader.Load(SolutionYml(t)).CanvasFormulas;

    [Fact]
    public void Block_scalar_formula_is_read()
    {
        using var t = CanvasLayout().Write(SrcDir + "Screen1.pa.yaml", """
            Screens:
              Screen1:
                Children:
                  - Gallery1:
                      Control: Gallery@2.15.0
                      Properties:
                        Items: |
                          ='Work Items'
            """);

        var f = Assert.Single(Formulas(t));

        Assert.Equal(new CanvasFormula("Contoso Tracker", "Gallery1", "Items", "='Work Items'", SrcDir + "Screen1.pa.yaml"), f);
    }

    [Fact]
    public void Nested_control_property_gives_control_and_property()
    {
        using var t = CanvasLayout().Write(SrcDir + "Screen1.pa.yaml", """
            Screens:
              Screen1:
                Properties:
                  OnVisible: =Set(a, 1)
                Children:
                  - Gallery1:
                      Properties:
                        Items: =Table1
                      Children:
                        - Button1:
                            Control: Button@2.2.0
                            Properties:
                              OnSelect: =Launch("https://contoso.sharepoint.com")
            """);

        Assert.Equal(
            [("Screen1", "OnVisible"), ("Gallery1", "Items"), ("Button1", "OnSelect")],
            Formulas(t).Select(f => (f.Control, f.Property)));
    }

    [Fact]
    public void App_and_component_definitions_are_read()
    {
        using var t = CanvasLayout().Write(SrcDir + "App.pa.yaml", """
            App:
              Properties:
                OnStart: =Set(a, 1)
            ComponentDefinitions:
              Header:
                Properties:
                  Text: =Left("x", 1)
                Children:
                  - Label1:
                      Properties:
                        Text: =Parent.Text
            """);

        Assert.Equal(
            [("App", "OnStart"), ("Header", "Text"), ("Label1", "Text")],
            Formulas(t).Select(f => (f.Control, f.Property)));
    }

    [Fact]
    public void A_value_without_a_leading_equals_is_skipped()
    {
        using var t = CanvasLayout().Write(SrcDir + "Screen1.pa.yaml", """
            Screens:
              Screen1:
                Properties:
                  Fill: red
                  Visible: =true
            """);

        Assert.Equal("Visible", Assert.Single(Formulas(t)).Property);
    }

    [Fact]
    public void A_file_without_controls_gives_no_formulas()
    {
        using var t = CanvasLayout().Write(SrcDir + "_EditorState.pa.yaml", "EditorState:\n  ScreensOrder:\n    - Screen1\n");

        Assert.Empty(Formulas(t));
    }

    [Fact]
    public void App_name_falls_back_to_name_then_folder()
    {
        const string yml = "Screens:\n  S:\n    Properties:\n      Fill: =red\n";
        using var named = CanvasLayout("CanvasApp:\n  Name: schema_name\n").Write(SrcDir + "S.pa.yaml", yml);
        using var bare = CanvasLayout("CanvasApp:\n  Status: Ready\n").Write(SrcDir + "S.pa.yaml", yml);

        Assert.Equal("schema_name", Assert.Single(Formulas(named)).App);
        Assert.Equal("app1", Assert.Single(Formulas(bare)).App);
    }

    [Fact]
    public void Formulas_are_ordered_by_file_path_then_document_order()
    {
        using var t = CanvasLayout()
            .Write(SrcDir + "b.pa.yaml", "Screens:\n  B:\n    Properties:\n      Z: =1\n      A: =2\n")
            .Write(SrcDir + "Components/a.pa.yaml", "Screens:\n  A:\n    Properties:\n      P: =3\n");

        Assert.Equal(
            [(SrcDir + "Components/a.pa.yaml", "P"), (SrcDir + "b.pa.yaml", "Z"), (SrcDir + "b.pa.yaml", "A")],
            Formulas(t).Select(f => (f.File, f.Property)));
    }

    [Fact]
    public void A_pa_yaml_outside_the_src_folder_is_ignored()
    {
        const string yml = "Screens:\n  S:\n    Properties:\n      Fill: =red\n";
        using var t = CanvasLayout()
            .Write("canvasapps/app1/app1_DocumentUri/Other/S.pa.yaml", yml)
            .Write("canvasapps/app1/S.pa.yaml", yml);

        Assert.Empty(Formulas(t));
    }

    [Fact]
    public void A_pa_yaml_with_an_anchor_names_the_file()
    {
        using var t = CanvasLayout().Write(SrcDir + "S.pa.yaml", "Screens:\n  S:\n    Properties: &p\n      Fill: =red\n");

        var ex = Assert.Throws<InvalidDataException>(() => Formulas(t));

        Assert.StartsWith(SolutionYml(t), ex.Message);
        Assert.Contains(SrcDir + "S.pa.yaml", ex.Message);
    }

    [Fact]
    public void The_xml_layout_has_no_canvas_formulas()
    {
        var path = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v2");

        Assert.Empty(SolutionLoader.Load(path).CanvasFormulas);
    }

    [Fact]
    public void The_fixture_has_the_launch_formula()
    {
        var path = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed-yaml", "v2", "solutions", "PowerOpsSeed", "solution.yml");

        var s = SolutionLoader.Load(path);

        var f = Assert.Single(s.CanvasFormulas, f => f.Property == "OnSelect" && f.Formula.StartsWith("=Launch", StringComparison.Ordinal));
        Assert.Equal("Work Items", f.App);
        Assert.Equal("Button1", f.Control);
        Assert.Equal("=Launch(\"https://contoso.sharepoint.com/sites/ops\")", f.Formula);
        Assert.StartsWith("canvasapps/pops_workitems_2af60/pops_workitems_2af60_DocumentUri/Src/", f.File);
    }
}