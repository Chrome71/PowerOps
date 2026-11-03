using System.IO.Compression;

using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class PowerPagesReaderTests
{
    private const string SiteId = "00000000-0000-0000-0000-0000000000f1";
    private const string SettingId = "00000000-0000-0000-0000-0000000000b1";
    private const string RoleId = "00000000-0000-0000-0000-0000000000b2";
    private const string PermissionId = "00000000-0000-0000-0000-0000000000b3";

    private static string Path(string id) => $"powerpagecomponents/{id}/powerpagecomponent.xml";

    private static string Xml(string id, int type, string name, string? content, string? state = "0", string root = "powerpagecomponent") =>
        $"""
        <{root} powerpagecomponentid="{id}">
          {(content is null ? "" : $"<content>{System.Security.SecurityElement.Escape(content)}</content>")}
          <name>{name}</name>
          <powerpagecomponenttype>{type}</powerpagecomponenttype>
          <powerpagesiteid><powerpagesiteid>{SiteId}</powerpagesiteid></powerpagesiteid>
          {(state is null ? "" : $"<statecode>{state}</statecode>")}
        </{root}>
        """;

    private static TempFolder Folder() => new TempFolder()
        .Write("Other/Solution.xml", SolutionXml.Manifest())
        .Write("Other/Customizations.xml", SolutionXml.Customizations(""));

    private static TempFolder Full() => Folder()
        .Write(Path(SettingId), Xml(SettingId.ToUpperInvariant(), 9, "Webapi/demo_item/fields", """{"value":"*","description":"d"}"""))
        .Write(Path(RoleId), Xml(RoleId, 11, "Anonymous Users", """{"anonymoususersrole":true,"authenticatedusersrole":false}"""))
        .Write(Path(PermissionId), Xml(PermissionId, 18, "Item access", """
            {"entitylogicalname":"demo_item","adx_entitypermission_webrole":["00000000-0000-0000-0000-0000000000b2"],"create":true,"write":false,"delete":false,"parententitypermission":null,"scope":756150000}
            """));

    private static List<string> Describe(string path) =>
        SolutionLoader.Load(path).PowerPageComponents
            .Select(c => $"{c.Id}|{c.Type}|{c.Name}|{c.SiteId}|{c.Active}|{c.Content.GetRawText()}|{c.File}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void A_site_setting_a_web_role_and_a_table_permission_are_read_with_every_field()
    {
        using var t = Full();

        var all = SolutionLoader.Load(t.Path).PowerPageComponents.ToDictionary(c => c.Type);

        Assert.Equal(3, all.Count);
        var setting = all[9];
        Assert.Equal(SettingId, setting.Id);
        Assert.Equal("Webapi/demo_item/fields", setting.Name);
        Assert.Equal(SiteId, setting.SiteId);
        Assert.True(setting.Active);
        Assert.Equal("*", setting.Content.GetProperty("value").GetString());
        Assert.Equal(Path(SettingId), setting.File);
        Assert.True(all[11].Content.GetProperty("anonymoususersrole").GetBoolean());
        var permission = all[18];
        Assert.Equal("demo_item", permission.Content.GetProperty("entitylogicalname").GetString());
        Assert.Equal(RoleId, permission.Content.GetProperty("adx_entitypermission_webrole")[0].GetString());
        Assert.True(permission.Content.GetProperty("create").GetBoolean());
    }

    [Fact]
    public void The_content_outlives_the_read()
    {
        using var t = Full();
        var solution = SolutionLoader.Load(t.Path);
        GC.Collect();

        Assert.All(solution.PowerPageComponents, c => Assert.Equal(System.Text.Json.JsonValueKind.Object, c.Content.ValueKind));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void No_content_gives_an_empty_object(string? content)
    {
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, 9, "Footer/OutputCache/Enabled", content));

        var c = Assert.Single(SolutionLoader.Load(t.Path).PowerPageComponents);

        Assert.Equal("{}", c.Content.GetRawText());
    }

    [Theory]
    [InlineData("1", false)]
    [InlineData("0", true)]
    [InlineData(null, true)]
    public void A_component_is_active_unless_its_statecode_is_other_than_zero(string? state, bool active)
    {
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, 9, "Footer/OutputCache/Enabled", "{}", state));

        Assert.Equal(active, Assert.Single(SolutionLoader.Load(t.Path).PowerPageComponents).Active);
    }

    [Fact]
    public void Other_keys_of_the_three_types_are_kept_and_other_types_get_an_empty_object()
    {
        using var t = Folder()
            .Write(Path(SettingId), Xml(SettingId, 9, "Footer/OutputCache/Duration", """{"value":42}"""))
            .Write(Path(RoleId), Xml(RoleId, 2, "Home", """{"anonymoususersrole":"yes","x":[1]}"""));

        var all = SolutionLoader.Load(t.Path).PowerPageComponents.ToDictionary(c => c.Type);

        Assert.Equal("""{"value":42}""", all[9].Content.GetRawText());
        Assert.Equal("{}", all[2].Content.GetRawText());
    }

    [Fact]
    public void A_component_of_another_type_loads_even_when_its_content_is_not_json()
    {
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, 34, "Snippet", "<div>not json {"));

        var c = Assert.Single(SolutionLoader.Load(t.Path).PowerPageComponents);

        Assert.Equal(34, c.Type);
        Assert.Equal("{}", c.Content.GetRawText());
    }

    [Fact]
    public void A_duplicate_key_in_content_is_refused_with_the_path()
    {
        using var t = Folder().Write(Path(PermissionId), Xml(PermissionId, 18, "Perm", """{"create":false,"create":true}"""));

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{Path(PermissionId)}: ", e.Message);
    }

    [Fact]
    public void Content_nested_65_deep_is_refused_with_the_path_as_an_InvalidDataException()
    {
        var deep = string.Concat(Enumerable.Repeat("{\"a\":", 65)) + "1" + new string('}', 65);
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, 9, "Footer/OutputCache/Enabled", deep));

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{Path(SettingId)}: ", e.Message);
    }

    [Fact]
    public void A_component_file_nested_deeper_than_the_xml_limit_is_refused_with_the_path()
    {
        var depth = SolutionParser.MaxXmlDepth + 1;
        var xml = $"<powerpagecomponent powerpagecomponentid=\"{SettingId}\">" + string.Concat(Enumerable.Repeat("<x>", depth)) + string.Concat(Enumerable.Repeat("</x>", depth)) + "</powerpagecomponent>";
        using var t = Folder().Write(Path(SettingId), xml);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{Path(SettingId)}: ", e.Message);
    }

    private static string ZipOf(TempFolder dir, IEnumerable<(string Name, byte[] Bytes)> entries)
    {
        var path = System.IO.Path.Combine(dir.Path, "hand.zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, bytes) in entries)
        {
            using var s = zip.CreateEntry(name).Open();
            s.Write(bytes);
        }

        return path;
    }

    private static byte[] Utf8(string text) => System.Text.Encoding.UTF8.GetBytes(text);

    [Fact]
    public void An_oversized_component_entry_in_a_zip_is_refused_with_its_path()
    {
        using var dir = new TempFolder();
        var zip = ZipOf(dir,
        [
            ("solution.xml", Utf8(SolutionXml.Manifest())),
            (Path(SettingId), Utf8(Xml(SettingId, 9, "Footer/OutputCache/Enabled", "{\"value\":\"" + new string('x', 5000) + "\"}"))),
        ]);

        ZipReader.Read(zip, new ReadBudget());
        var e = Assert.Throws<InvalidDataException>(() => ZipReader.Read(zip, new ReadBudget(maxFileBytes: 1024)));

        Assert.StartsWith($"{Path(SettingId)}: ", e.Message);
    }

    [Fact]
    public void A_binary_filecontent_entry_beside_a_component_is_not_opened_in_a_zip()
    {
        using var dir = new TempFolder();
        var zip = ZipOf(dir,
        [
            ("solution.xml", Utf8(SolutionXml.Manifest())),
            (Path(SettingId), Utf8(Xml(SettingId, 9, "Footer/OutputCache/Enabled", "{}"))),
            ($"powerpagecomponents/{SettingId}/filecontent/x.png", [0x89, 0x50, 0x4E, 0x47, 0xFF, 0xFE, 0x00, 0x3C]),
        ]);

        Assert.Single(ZipReader.Read(zip).PowerPageComponents);
        Assert.Single(ZipReader.Read(zip, new ReadBudget(maxFileBytes: 4096)).PowerPageComponents);
    }

    [Fact]
    public void A_zip_with_the_entry_names_pac_packs_reads_its_components()
    {
        using var dir = new TempFolder();
        var zip = ZipOf(dir,
        [
            ("solution.xml", Utf8(SolutionXml.Manifest())),
            ("customizations.xml", Utf8(SolutionXml.Customizations(""))),
            ("[Content_Types].xml", Utf8("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types" />""")),
            ("Assets/powerpagesites.xml", Utf8("<powerpagesites />")),
            ("Assets/powerpagesitelanguages.xml", Utf8("<powerpagesitelanguages />")),
            (Path(SettingId), Utf8(Xml(SettingId, 9, "Webapi/demo_item/enabled", """{"value":"true"}"""))),
            (Path(RoleId), Utf8(Xml(RoleId, 11, "Anonymous Users", """{"anonymoususersrole":true}"""))),
            ($"powerpagecomponents/{SettingId}/filecontent/x.png", [0x89, 0x50, 0x4E, 0x47]),
        ]);

        var components = ZipReader.Read(zip).PowerPageComponents;

        Assert.Equal([Path(SettingId), Path(RoleId)], components.Select(c => c.File).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_solution_without_components_has_an_empty_list()
    {
        using var t = Folder();

        Assert.Empty(SolutionLoader.Load(t.Path).PowerPageComponents);
    }

    [Fact]
    public void Webapi_names_are_matched_ignoring_case_for_the_value_check()
    {
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, 9, "WEBAPI/demo_item/fields", """{"value":true}"""));

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{Path(SettingId)}: ", e.Message);
    }

    [Theory]
    [InlineData("<other powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><powerpagecomponenttype>9</powerpagecomponenttype></other>")]
    [InlineData("<powerpagecomponent><powerpagecomponenttype>9</powerpagecomponenttype></powerpagecomponent>")]
    [InlineData("<powerpagecomponent powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><name>x</name></powerpagecomponent>")]
    [InlineData("<powerpagecomponent powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><powerpagecomponenttype>nine</powerpagecomponenttype></powerpagecomponent>")]
    [InlineData("<powerpagecomponent powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><content>{\"value\":</content><powerpagecomponenttype>9</powerpagecomponenttype></powerpagecomponent>")]
    [InlineData("<powerpagecomponent powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><content>[1]</content><powerpagecomponenttype>9</powerpagecomponenttype></powerpagecomponent>")]
    [InlineData("<powerpagecomponent powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><content>\"x\"</content><powerpagecomponenttype>9</powerpagecomponenttype></powerpagecomponent>")]
    [InlineData("<powerpagecomponent powerpagecomponentid=\"00000000-0000-0000-0000-0000000000b1\"><powerpagecomponenttype>9</powerpagecomponenttype>")]
    public void A_file_of_an_unknown_shape_is_refused_with_its_path(string xml)
    {
        using var t = Folder().Write(Path(SettingId), xml);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{Path(SettingId)}: ", e.Message);
    }

    [Theory]
    [InlineData(9, "Webapi/demo_item/fields", """{"value":1}""")]
    [InlineData(9, "Webapi/demo_item/fields", """{"value":["*"]}""")]
    [InlineData(11, "Role", """{"anonymoususersrole":"true"}""")]
    [InlineData(11, "Role", """{"anonymoususersrole":1}""")]
    [InlineData(18, "Perm", """{"adx_entitypermission_webrole":"x"}""")]
    [InlineData(18, "Perm", """{"adx_entitypermission_webrole":[1]}""")]
    [InlineData(18, "Perm", """{"create":"true"}""")]
    [InlineData(18, "Perm", """{"write":1}""")]
    [InlineData(18, "Perm", """{"delete":[]}""")]
    public void A_key_the_rules_read_with_another_kind_is_refused_with_the_path(int type, string name, string content)
    {
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, type, name, content));

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{Path(SettingId)}: ", e.Message);
    }

    [Theory]
    [InlineData(9, "Webapi/demo_item/fields", """{"value":null}""")]
    [InlineData(9, "Webapi/demo_item/fields", """{}""")]
    [InlineData(11, "Role", """{"anonymoususersrole":null}""")]
    [InlineData(18, "Perm", """{"adx_entitypermission_webrole":null,"create":null,"write":null,"delete":null,"parententitypermission":null}""")]
    [InlineData(18, "Perm", """{"adx_entitypermission_webrole":[],"parententitypermission":"00000000-0000-0000-0000-0000000000b9"}""")]
    [InlineData(18, "Perm", """{"parententitypermission":{"id":5}}""")]
    public void A_key_the_rules_read_may_be_absent_or_null(int type, string name, string content)
    {
        using var t = Folder().Write(Path(SettingId), Xml(SettingId, type, name, content));

        Assert.Single(SolutionLoader.Load(t.Path).PowerPageComponents);
    }

    [Fact]
    public void The_same_files_zipped_give_the_same_components_with_the_same_file()
    {
        using var t = Full();
        using var zipDir = new TempFolder();
        var zip = System.IO.Path.Combine(zipDir.Path, "site.zip");
        SolutionZip.Create(t.Path, zip);

        var fromFolder = Describe(t.Path);

        Assert.Equal(3, fromFolder.Count);
        Assert.Equal(fromFolder, Describe(zip));
    }

    [Fact]
    public void Only_the_component_file_is_read_not_the_filecontent_next_to_it()
    {
        using var t = Folder()
            .Write(Path(SettingId), Xml(SettingId, 9, "Footer/OutputCache/Enabled", "{}"))
            .Write($"powerpagecomponents/{SettingId}/filecontent/site.css", "not xml {");

        Assert.Single(SolutionLoader.Load(t.Path).PowerPageComponents);
    }
}