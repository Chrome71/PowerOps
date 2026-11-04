using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class AppModuleReaderTests
{
    private const string AppFile = "AppModules/demo_app/AppModule.xml";
    private const string SiteMapFile = "AppModuleSiteMaps/demo_app/AppModuleSiteMap.xml";

    private const string AppXml = """
        <AppModule>
          <UniqueName>demo_app</UniqueName>
          <AppModuleComponents>
            <AppModuleComponent type="1" schemaName="demo_item" />
            <AppModuleComponent type="62" schemaName=" demo_map " />
            <AppModuleComponent type="62" schemaName="" />
          </AppModuleComponents>
          <appelements>
            <appelement><canvasappid><name>demo_pageone</name></canvasappid></appelement>
            <appelement><canvasappid><name> demo_pagetwo </name></canvasappid></appelement>
            <appelement><canvasappid><name></name></canvasappid></appelement>
          </appelements>
        </AppModule>
        """;

    private const string SiteMapXml = """
        <AppModuleSiteMap>
          <SiteMapUniqueName>demo_map</SiteMapUniqueName>
          <SiteMap>
            <Area Id="a">
              <Group Id="g">
                <SubArea Id="s1" Page=" demo_pageone " />
                <SubArea Id="s2" GenPageId="{ABCDEF01-0000-0000-0000-00000000000A}" />
                <SubArea Id="s3" Entity="demo_item" />
                <SubArea Id="s4" Entity="demo_item" Page="demo_ignored1" />
                <SubArea Id="s5" GenPageId="00000000-0000-0000-0000-00000000000b" Entity="demo_item" />
                <SubArea Id="s6" Page="" GenPageId="" Entity="" />
                <SubArea Id="s7" Page="demo_pagetwo" Entity="" />
                <Wrapper><SubArea Id="s8" Page="demo_deep" /></Wrapper>
                <SubArea Id="s9" Url="https://example.test" />
              </Group>
            </Area>
          </SiteMap>
        </AppModuleSiteMap>
        """;

    private static string Meta(string name, string? connections, string? type = null) =>
        $"<CanvasApp>{(name.Length == 0 ? "" : $"<Name>{name}</Name>")}{(connections is null ? "" : $"<ConnectionReferences>{System.Security.SecurityElement.Escape(connections)}</ConnectionReferences>")}{(type is null ? "" : $"<CanvasAppType>{type}</CanvasAppType>")}</CanvasApp>";

    private static TempFolder Folder() => new TempFolder()
        .Write("Other/Solution.xml", SolutionXml.Manifest())
        .Write("Other/Customizations.xml", SolutionXml.Customizations(""));

    private static TempFolder Full() => Folder()
        .Write(AppFile, AppXml)
        .Write(SiteMapFile, SiteMapXml)
        .Write("CanvasApps/demo_pageone.meta.xml", Meta("demo_pageone", """
            {"K1":{"id":" /providers/microsoft.powerapps/apis/shared_a "},"k2":{"id":"/providers/microsoft.powerapps/apis/shared_a"}," ":{"id":"x"},"k1":{"id":"dup"},"k3":{"displayName":"no id"},"k4":5," k5 ":{"id":"y"}}
            """, "2"))
        .Write("CanvasApps/demo_pagetwo.meta.xml", Meta("demo_pagetwo", null, "1"))
        .Write("CanvasApps/demo_empty.meta.xml", Meta("demo_empty", "  ", "two"))
        .Write("CanvasApps/demo_braces.meta.xml", Meta("demo_braces", "{}"))
        .Write("CanvasApps/demo_text.meta.xml", Meta("demo_text", "not json {"))
        .Write("CanvasApps/demo_array.meta.xml", Meta("demo_array", "[1]"))
        .Write("CanvasApps/demo_noname.meta.xml", Meta("", "{}"))
        .Write("CanvasApps/demo_bad.meta.xml", "<CanvasApp><Name>demo_bad</Name>");

    private static string Describe(Solution s) => string.Join(
        "\n",
        s.AppModules.Select(a => $"app|{a.UniqueName}|{string.Join(",", a.SiteMaps)}|{string.Join(",", a.Pages)}")
            .Concat(s.SiteMaps.Select(m => $"map|{m.UniqueName}|{string.Join(",", m.Pages)}|{string.Join(",", m.GenPageIds)}"))
            .Concat(s.CanvasApps.Select(c => $"canvas|{c.Name}|{c.Type}|{(c.Connections is null ? "null" : string.Join(",", c.Connections.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}")))}"))
            .Order(StringComparer.Ordinal));

    [Fact]
    public void An_app_module_is_read_with_its_site_map_components_and_pages()
    {
        using var t = Full();

        var app = Assert.Single(SolutionLoader.Load(t.Path).AppModules);

        Assert.Equal("demo_app", app.UniqueName);
        Assert.Equal(["demo_map"], app.SiteMaps);
        Assert.Equal(["demo_pageone", "demo_pagetwo"], app.Pages);
        Assert.Equal(AppFile, app.File);
    }

    [Fact]
    public void A_site_map_gives_page_and_generative_page_targets_by_the_sdk_order()
    {
        using var t = Full();

        var map = Assert.Single(SolutionLoader.Load(t.Path).SiteMaps);

        Assert.Equal("demo_map", map.UniqueName);
        // GenPageId first, then Entity (which hides a Page), then Page; any depth; empty values skipped; ids normalized.
        Assert.Equal(["demo_pageone", "demo_pagetwo", "demo_deep"], map.Pages);
        Assert.Equal(["abcdef01-0000-0000-0000-00000000000a", "00000000-0000-0000-0000-00000000000b"], map.GenPageIds);
        Assert.Equal(SiteMapFile, map.File);
    }

    [Fact]
    public void Canvas_apps_are_read_with_their_connections()
    {
        using var t = Full();

        var apps = SolutionLoader.Load(t.Path).CanvasApps.ToDictionary(c => c.Name);

        Assert.Equal(["demo_array", "demo_braces", "demo_empty", "demo_pageone", "demo_pagetwo", "demo_text"], apps.Keys.Order(StringComparer.Ordinal));
        var one = apps["demo_pageone"];
        Assert.Equal(2, one.Type);
        Assert.Equal(1, apps["demo_pagetwo"].Type);
        Assert.Null(apps["demo_empty"].Type);
        Assert.Null(apps["demo_braces"].Type);
        // Keys are trimmed and compared ignoring case, the first of two equal keys is kept, an empty key is skipped, and an entry without a string id has "".
        Assert.Equal(
            ["K1=/providers/microsoft.powerapps/apis/shared_a", "k2=/providers/microsoft.powerapps/apis/shared_a", "k3=", "k4=", "k5=y"],
            one.Connections!.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}"));
        Assert.True(one.Connections!.ContainsKey("k1"));
        Assert.Empty(apps["demo_pagetwo"].Connections!);
        Assert.Empty(apps["demo_empty"].Connections!);
        Assert.Empty(apps["demo_braces"].Connections!);
        Assert.Null(apps["demo_text"].Connections);
        Assert.Null(apps["demo_array"].Connections);
    }

    [Fact]
    public void Canvas_metadata_with_no_name_or_with_malformed_xml_gives_no_canvas_app()
    {
        using var t = Full();

        var names = SolutionLoader.Load(t.Path).CanvasApps.Select(c => c.Name);

        Assert.DoesNotContain("demo_bad", names);
        Assert.Equal(6, names.Count());
    }

    [Fact]
    public void A_folder_with_both_files_reads_the_unmanaged_one_only_and_a_managed_only_folder_reads_it()
    {
        using var t = Folder()
            .Write("AppModules/demo_both/AppModule.xml", "<AppModule><UniqueName>demo_unmanaged</UniqueName></AppModule>")
            .Write("AppModules/demo_both/AppModule_managed.xml", "<AppModule><UniqueName>demo_managed</UniqueName></AppModule>")
            .Write("AppModules/demo_only/AppModule_managed.xml", "<AppModule><UniqueName>demo_only_managed</UniqueName></AppModule>")
            .Write("AppModuleSiteMaps/demo_both/AppModuleSiteMap.xml", "<AppModuleSiteMap><SiteMapUniqueName>demo_unmanaged</SiteMapUniqueName></AppModuleSiteMap>")
            .Write("AppModuleSiteMaps/demo_both/AppModuleSiteMap_managed.xml", "<AppModuleSiteMap><SiteMapUniqueName>demo_managed</SiteMapUniqueName></AppModuleSiteMap>")
            .Write("AppModuleSiteMaps/demo_only/AppModuleSiteMap_managed.xml", "<AppModuleSiteMap><SiteMapUniqueName>demo_only_managed</SiteMapUniqueName></AppModuleSiteMap>");

        var s = SolutionLoader.Load(t.Path);

        Assert.Equal(["demo_only_managed", "demo_unmanaged"], s.AppModules.Select(a => a.UniqueName).Order(StringComparer.Ordinal));
        Assert.Equal(["demo_only_managed", "demo_unmanaged"], s.SiteMaps.Select(a => a.UniqueName).Order(StringComparer.Ordinal));
        Assert.Contains(s.AppModules, a => a.File == "AppModules/demo_only/AppModule_managed.xml");
    }

    [Fact]
    public void Paths_are_matched_ignoring_case()
    {
        using var t = Folder()
            .Write("appmodules/Other_Folder/APPMODULE.XML", AppXml)
            .Write("APPMODULESITEMAPS/other_folder/appmodulesitemap_MANAGED.xml", SiteMapXml);

        var s = SolutionLoader.Load(t.Path);

        Assert.Equal("demo_app", Assert.Single(s.AppModules).UniqueName);
        Assert.Equal("demo_map", Assert.Single(s.SiteMaps).UniqueName);
    }

    [Fact]
    public void The_same_records_come_from_a_zip_whose_customizations_holds_the_elements()
    {
        using var folder = Full();
        using var zipFolder = Folder();
        using var dir = new TempFolder();
        var inner = $"<AppModules>{AppXml}</AppModules><AppModuleSiteMaps>{SiteMapXml}</AppModuleSiteMaps><CanvasApps>"
            + string.Concat(Directory.EnumerateFiles(System.IO.Path.Combine(folder.Path, "CanvasApps"), "*.meta.xml").Select(f => File.ReadAllText(f)).Where(x => x.Contains("</CanvasApp>")))
            + "</CanvasApps>";
        zipFolder.Write("Other/Customizations.xml", SolutionXml.Customizations(inner));
        var zip = System.IO.Path.Combine(dir.Path, "apps.zip");
        SolutionZip.Create(zipFolder.Path, zip);

        var fromFolder = SolutionLoader.Load(folder.Path);
        var fromZip = SolutionLoader.Load(zip);

        Assert.Equal(Describe(fromFolder), Describe(fromZip));
        Assert.Equal(6, fromZip.CanvasApps.Count);
        Assert.Equal("customizations.xml", Assert.Single(fromZip.AppModules).File);
        Assert.Equal("customizations.xml", Assert.Single(fromZip.SiteMaps).File);
        var one = fromZip.CanvasApps.Single(c => c.Name == "demo_pageone");
        Assert.Equal(2, one.Type);
        Assert.Equal(5, one.Connections!.Count);
        Assert.Equal("/providers/microsoft.powerapps/apis/shared_a", one.Connections["k2"]);
    }

    [Fact]
    public void A_solution_with_none_of_these_has_three_empty_lists()
    {
        using var t = Folder();

        var s = SolutionLoader.Load(t.Path);

        Assert.Empty(s.AppModules);
        Assert.Empty(s.SiteMaps);
        Assert.Empty(s.CanvasApps);
    }

    [Fact]
    public void A_file_under_uxagentprojects_that_is_not_xml_loads()
    {
        using var t = Full()
            .Write("uxagentprojects/demo_gen/genpage.xml", "not xml {")
            .Write("uxagentprojects/demo_gen/page.tsx", "export {}");

        Assert.Single(SolutionLoader.Load(t.Path).AppModules);
    }

    [Theory]
    [InlineData("AppModules/demo_app/AppModule.xml", "<other><UniqueName>x</UniqueName></other>")]
    [InlineData("AppModules/demo_app/AppModule_managed.xml", "<AppModuleSiteMap />")]
    [InlineData("AppModuleSiteMaps/demo_app/AppModuleSiteMap.xml", "<AppModule />")]
    [InlineData("AppModuleSiteMaps/demo_app/AppModuleSiteMap_managed.xml", "<x />")]
    [InlineData("AppModuleSiteMaps/demo_app/AppModuleSiteMap.xml", "<AppModuleSiteMap><SiteMapUniqueName>x</SiteMapUniqueName>")]
    public void A_file_of_an_unknown_shape_is_refused_with_its_path(string path, string xml)
    {
        using var t = Folder().Write(path, xml);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{path}: ", e.Message);
    }

    [Fact]
    public void A_site_map_nested_deeper_than_the_xml_limit_is_refused_with_the_path()
    {
        var depth = SolutionParser.MaxXmlDepth + 1;
        var xml = "<AppModuleSiteMap><SiteMap>" + string.Concat(Enumerable.Repeat("<Area>", depth)) + string.Concat(Enumerable.Repeat("</Area>", depth)) + "</SiteMap></AppModuleSiteMap>";
        using var t = Folder().Write(SiteMapFile, xml);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{SiteMapFile}: ", e.Message);
    }

    [Fact]
    public void Only_the_canvasappid_name_of_an_appelement_is_its_page()
    {
        using var t = Folder().Write(AppFile, """
            <AppModule>
              <UniqueName>demo_app</UniqueName>
              <appelements>
                <appelement uniquename="u"><canvasappid><name>page</name></canvasappid><iscustomizable>1</iscustomizable><name>other</name></appelement>
              </appelements>
            </AppModule>
            """);

        Assert.Equal(["page"], Assert.Single(SolutionLoader.Load(t.Path).AppModules).Pages);
    }

    [Fact]
    public void Attribute_names_of_a_site_map_are_exact()
    {
        using var t = Folder().Write(SiteMapFile, """
            <AppModuleSiteMap>
              <SiteMapUniqueName>demo_map</SiteMapUniqueName>
              <SiteMap><Area><Group>
                <SubArea page="demo_lower" />
                <SubArea genpageid="00000000-0000-0000-0000-00000000000c" />
                <subarea Page="demo_lowerelement" />
              </Group></Area></SiteMap>
            </AppModuleSiteMap>
            """);

        var map = Assert.Single(SolutionLoader.Load(t.Path).SiteMaps);

        Assert.Empty(map.Pages);
        Assert.Empty(map.GenPageIds);
    }

    [Fact]
    public void Connection_entries_that_are_not_objects_are_counted_with_an_empty_id()
    {
        using var t = Folder().Write("CanvasApps/demo_p.meta.xml", Meta("demo_p", """{"a":5,"b":null,"c":[1],"d":"x","e":{"id":null},"f":{"id":7}}"""));

        var app = Assert.Single(SolutionLoader.Load(t.Path).CanvasApps);

        Assert.Equal(["a", "b", "c", "d", "e", "f"], app.Connections!.Keys.Order(StringComparer.Ordinal));
        Assert.All(app.Connections.Values, v => Assert.Equal("", v));
    }

    [Fact]
    public void An_id_that_is_not_valid_text_gives_null_connections_and_does_not_throw()
    {
        using var t = Folder().Write("CanvasApps/demo_p.meta.xml", Meta("demo_p", """{"a":{"id":"\ud800"}}"""));

        Assert.Null(Assert.Single(SolutionLoader.Load(t.Path).CanvasApps).Connections);
    }

    [Fact]
    public void Connection_references_nested_100_levels_deep_give_null_connections()
    {
        var deep = string.Concat(Enumerable.Repeat("{\"a\":", 100)) + "1" + new string('}', 100);
        using var t = Folder().Write("CanvasApps/demo_p.meta.xml", Meta("demo_p", deep));

        Assert.Null(Assert.Single(SolutionLoader.Load(t.Path).CanvasApps).Connections);
    }

    [Theory]
    [InlineData("AppModules/demo_app/AppModule.xml", "<AppModule><UniqueName> </UniqueName></AppModule>", "missing UniqueName")]
    [InlineData("AppModules/demo_app/AppModule_managed.xml", "<AppModule />", "missing UniqueName")]
    [InlineData("AppModuleSiteMaps/demo_app/AppModuleSiteMap.xml", "<AppModuleSiteMap><SiteMapUniqueName /></AppModuleSiteMap>", "missing SiteMapUniqueName")]
    [InlineData("AppModuleSiteMaps/demo_app/AppModuleSiteMap.xml", "<AppModuleSiteMap><SiteMap /></AppModuleSiteMap>", "missing SiteMapUniqueName")]
    public void A_folder_file_without_a_name_is_refused_with_its_path(string path, string xml, string message)
    {
        using var t = Folder().Write(path, xml);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{path}: {message} element.", e.Message);
    }

    [Theory]
    [InlineData("<AppModules><AppModule><AppModuleComponents /></AppModule></AppModules>", "missing UniqueName")]
    [InlineData("<AppModuleSiteMaps><AppModuleSiteMap><SiteMapUniqueName> </SiteMapUniqueName></AppModuleSiteMap></AppModuleSiteMaps>", "missing SiteMapUniqueName")]
    public void A_customizations_element_without_a_name_is_refused_with_the_customizations_path(string inner, string message)
    {
        using var t = Folder().Write("Other/Customizations.xml", SolutionXml.Customizations(inner));
        using var dir = new TempFolder();
        var zip = System.IO.Path.Combine(dir.Path, "apps.zip");
        SolutionZip.Create(t.Path, zip);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(zip));

        Assert.Contains($"customizations.xml: {message} element.", e.Message);
    }
}