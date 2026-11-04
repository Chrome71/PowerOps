using System.Security;

using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class CodeAppReaderTests
{
    private const string Cds = """
        {"cdsdependencies":[{"logicalname":" demo_order ","componenttype":1},{"logicalname":"demo_5Fapi","componenttype":372,"componenttypeplatformname":"connector"},{"dependencyid":"00000000-0000-0000-0000-000000000001","componenttype":29,"componenttypeplatformname":"Workflow"},{"logicalname":"DEMO_Order","componenttype":1},{"logicalname":"demo_ORDER","componenttype":1}]}
        """;

    private const string Database = """
        {"default.cds":{"databaseDetails":{"referenceType":"Environmental","environmentName":"default.cds"},"dataSources":{"orders":{"entitySetName":"demo_orders","logicalName":"demo_order"},"env":{"entitySetName":"Environment","logicalName":"environment"}}}}
        """;

    private static string Meta(string name, string? cds, string? database = null, string type = "4") =>
        $"<CanvasApp><Name>{name}</Name><CanvasAppType>{type}</CanvasAppType>"
        + (cds is null ? "" : $"<CdsDependencies>{SecurityElement.Escape(cds)}</CdsDependencies>")
        + (database is null ? "" : $"<DatabaseReferences>{SecurityElement.Escape(database)}</DatabaseReferences>")
        + "</CanvasApp>";

    private static TempFolder Folder() => new TempFolder()
        .Write("Other/Solution.xml", SolutionXml.Manifest())
        .Write("Other/Customizations.xml", SolutionXml.Customizations(""));

    private static Solution LoadFolder(string cds, string? database)
    {
        using var t = Folder().Write("CanvasApps/demo_app.meta.xml", Meta("demo_app", cds, database));
        return SolutionLoader.Load(t.Path);
    }

    private static Solution LoadZip(string cds, string? database)
    {
        using var folder = Folder().Write("Other/Customizations.xml", SolutionXml.Customizations($"<CanvasApps>{Meta("demo_app", cds, database)}</CanvasApps>"));
        using var dir = new TempFolder();
        var zip = System.IO.Path.Combine(dir.Path, "app.zip");
        SolutionZip.Create(folder.Path, zip);
        return SolutionLoader.Load(zip);
    }

    [Fact]
    public void A_folder_and_a_zip_give_the_same_code_app_with_its_own_file()
    {
        var fromFolder = Assert.Single(LoadFolder(Cds, Database).CanvasApps);
        var fromZip = Assert.Single(LoadZip(Cds, Database).CanvasApps);

        foreach (var app in new[] { fromFolder, fromZip })
        {
            Assert.Equal("demo_app", app.Name);
            Assert.Equal(4, app.Type);
            Assert.Equal(["demo_order"], app.Tables);
            Assert.Equal(["demo_5Fapi"], app.Connectors);
        }

        Assert.Equal("CanvasApps/demo_app.meta.xml", fromFolder.File);
        Assert.Equal("customizations.xml", fromZip.File);
    }

    [Fact]
    public void A_database_reference_data_source_is_not_a_table()
    {
        var app = Assert.Single(LoadFolder("""{"cdsdependencies":[{"logicalname":"demo_b","componenttype":1}]}""", Database).CanvasApps);

        Assert.Equal(["demo_b"], app.Tables);
        Assert.Empty(app.Connectors!);
    }

    [Fact]
    public void Entries_of_other_types_are_listed_once_and_do_not_stop_the_table_being_read()
    {
        var app = Assert.Single(LoadFolder("""{"cdsdependencies":[{"componenttype":10001},{"logicalname":"demo_a","componenttype":1},{"componenttype":10000,"logicalname":"x"},{"componenttype":10001}]}""", null).CanvasApps);

        Assert.Equal([10001, 10000], app.OtherTypes);
        Assert.Equal(["demo_a"], app.Tables);
        Assert.Empty(app.Connectors!);
        Assert.Empty(Assert.Single(LoadFolder(Cds, null).CanvasApps).OtherTypes!);
    }

    [Fact]
    public void An_empty_cdsdependencies_array_gives_empty_lists()
    {
        var app = Assert.Single(LoadFolder("""{"cdsdependencies":[]}""", null).CanvasApps);

        Assert.Empty(app.Tables!);
        Assert.Empty(app.Connectors!);
    }

    public static TheoryData<string> Unreadable => new()
    {
        "not json {",
        "[1]",
        "{}",
        """{"other":[]}""",
        """{"cdsdependencies":{}}""",
        """{"cdsdependencies":["demo_order"]}""",
        """{"cdsdependencies":[{"logicalname":"demo_order","componenttype":"1"}]}""",
        """{"cdsdependencies":[{"logicalname":"demo_order"}]}""",
        """{"cdsdependencies":[{"logicalname":"demo_order","componenttype":1.0}]}""",
        """{"cdsdependencies":[{"logicalname":"demo_order","componenttype":1.5}]}""",
        """{"cdsdependencies":[{"logicalname":"demo_order","componenttype":1e10}]}""",
        """{"cdsdependencies":[{"componenttype":1}]}""",
        """{"cdsdependencies":[{"logicalname":"","componenttype":1}]}""",
        """{"cdsdependencies":[{"componenttype":372}]}""",
        """{"cdsdependencies":[{"logicalname":"","componenttype":372}]}""",
        $$"""{"cdsdependencies":[{"componenttype":1,"logicalname":"demo_a","x":{{new string('[', 100)}}{{new string(']', 100)}}}]}""",
        """{"cdsdependencies":[{"logicalname":"\ud800","componenttype":1}]}""",
    };

    [Theory]
    [MemberData(nameof(Unreadable))]
    public void Text_outside_the_known_shape_gives_null_lists_and_the_solution_still_loads(string cds)
    {
        foreach (var s in new[] { LoadFolder(cds, null), LoadZip(cds, null) })
        {
            var app = Assert.Single(s.CanvasApps);

            Assert.Equal(4, app.Type);
            Assert.Null(app.Tables);
            Assert.Null(app.Connectors);
            Assert.Null(app.OtherTypes);
        }
    }

    [Fact]
    public void A_missing_or_empty_cdsdependencies_element_gives_null_lists()
    {
        using var t = Folder()
            .Write("CanvasApps/demo_none.meta.xml", Meta("demo_none", null))
            .Write("CanvasApps/demo_blank.meta.xml", Meta("demo_blank", "  "));

        var apps = SolutionLoader.Load(t.Path).CanvasApps;

        Assert.Equal(2, apps.Count);
        Assert.All(apps, app =>
        {
            Assert.Null(app.Tables);
            Assert.Null(app.Connectors);
            Assert.Null(app.OtherTypes);
        });
    }
}