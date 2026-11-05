using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class RoleReaderTests
{
    private const string RoleXml = """
        <Role id="{ABCDEF01-0000-0000-0000-00000000000A}" name=" Front desk ">
          <RolePrivileges>
            <RolePrivilege name="prvReadpops_Loan" level="Global" />
            <RolePrivilege name=" prvWritepops_Loan " level="Global" />
            <RolePrivilege name="" level="Global" />
            <RolePrivilege level="Global" />
          </RolePrivileges>
        </Role>
        """;

    private const string EmptyRoleXml = """<Role id="00000000-0000-0000-0000-00000000000b" name="Empty"><RolePrivileges /></Role>""";

    private const string NoPrivilegesXml = """<Role id="00000000-0000-0000-0000-00000000000c" name="None" />""";

    private const string AppXml = """
        <AppModule>
          <UniqueName>demo_app</UniqueName>
          <AppModuleRoleMaps>
            <Role id="{627090FF-40A3-4053-8790-584EDC5BE201}" />
            <Role id="" />
            <Role id="00000000-0000-0000-0000-00000000000b" />
          </AppModuleRoleMaps>
        </AppModule>
        """;

    private static TempFolder Folder() => new TempFolder()
        .Write("Other/Solution.xml", SolutionXml.Manifest())
        .Write("Other/Customizations.xml", SolutionXml.Customizations(""));

    private static string Describe(Solution s) => string.Join(
        "\n",
        s.Roles.Select(r => $"role|{r.Id}|{r.Name}|{(r.Privileges is null ? "null" : string.Join(",", r.Privileges))}")
            .Concat(s.AppModules.Select(a => $"app|{a.UniqueName}|{string.Join(",", a.Roles)}"))
            .Order(StringComparer.Ordinal));

    [Fact]
    public void A_role_is_read_with_a_normalized_id_a_trimmed_name_and_its_privileges_in_file_order()
    {
        using var t = Folder().Write("Roles/Front desk.xml", RoleXml);

        var role = Assert.Single(SolutionLoader.Load(t.Path).Roles);

        Assert.Equal("abcdef01-0000-0000-0000-00000000000a", role.Id);
        Assert.Equal("Front desk", role.Name);
        Assert.Equal(["prvReadpops_Loan", "prvWritepops_Loan"], role.Privileges);
        Assert.Equal("Roles/Front desk.xml", role.File);
    }

    [Fact]
    public void An_empty_RolePrivileges_gives_an_empty_list_and_a_missing_one_gives_null()
    {
        using var t = Folder().Write("Roles/Empty.xml", EmptyRoleXml).Write("Roles/None.xml", NoPrivilegesXml);

        var roles = SolutionLoader.Load(t.Path).Roles.ToDictionary(r => r.Name);

        Assert.Empty(roles["Empty"].Privileges!);
        Assert.Null(roles["None"].Privileges);
    }

    [Fact]
    public void Roles_are_read_in_path_order_from_one_level_and_the_folder_name_is_matched_ignoring_case()
    {
        using var t = Folder()
            .Write("roles/B.xml", NoPrivilegesXml)
            .Write("Roles/A.XML", EmptyRoleXml)
            .Write("Roles/nested/c.xml", "not xml {");

        var roles = SolutionLoader.Load(t.Path).Roles;

        Assert.Equal(["Roles/A.XML", "roles/B.xml"], roles.Select(r => r.File));
    }

    [Fact]
    public void An_app_module_reads_the_normalized_ids_of_its_role_map_and_skips_an_empty_one()
    {
        using var t = Folder().Write("AppModules/demo_app/AppModule.xml", AppXml)
            .Write("AppModules/demo_none/AppModule.xml", "<AppModule><UniqueName>demo_none</UniqueName></AppModule>");

        var apps = SolutionLoader.Load(t.Path).AppModules.ToDictionary(a => a.UniqueName);

        Assert.Equal(["627090ff-40a3-4053-8790-584edc5be201", "00000000-0000-0000-0000-00000000000b"], apps["demo_app"].Roles);
        Assert.Empty(apps["demo_none"].Roles);
    }

    [Fact]
    public void The_same_records_come_from_a_zip_whose_customizations_holds_the_elements()
    {
        using var folder = Folder().Write("Roles/Front desk.xml", RoleXml).Write("Roles/Empty.xml", EmptyRoleXml).Write("Roles/None.xml", NoPrivilegesXml)
            .Write("AppModules/demo_app/AppModule.xml", AppXml);
        using var zipFolder = Folder().Write("Other/Customizations.xml", SolutionXml.Customizations($"<Roles>{RoleXml}{EmptyRoleXml}{NoPrivilegesXml}</Roles><AppModules>{AppXml}</AppModules>"));
        using var dir = new TempFolder();
        var zip = System.IO.Path.Combine(dir.Path, "roles.zip");
        SolutionZip.Create(zipFolder.Path, zip);

        var fromFolder = SolutionLoader.Load(folder.Path);
        var fromZip = SolutionLoader.Load(zip);

        Assert.Equal(Describe(fromFolder), Describe(fromZip));
        Assert.Equal(3, fromZip.Roles.Count);
        Assert.All(fromZip.Roles, r => Assert.Equal("customizations.xml", r.File));
    }

    [Fact]
    public void A_role_in_a_managed_file_and_in_customizations_is_read_once_from_the_first_file()
    {
        using var t = Folder()
            .Write("Roles/A_managed.xml", EmptyRoleXml)
            .Write("Roles/A.xml", EmptyRoleXml)
            .Write("Other/Customizations.xml", SolutionXml.Customizations($"<Roles>{EmptyRoleXml}{NoPrivilegesXml}</Roles>"));

        var roles = SolutionLoader.Load(t.Path).Roles;

        Assert.Equal(["Roles/A.xml", "Other/Customizations.xml"], roles.Select(r => r.File));
        Assert.Equal(["Empty", "None"], roles.Select(r => r.Name));
    }

    [Fact]
    public void A_solution_with_none_has_an_empty_Roles()
    {
        using var t = Folder();

        Assert.Empty(SolutionLoader.Load(t.Path).Roles);
    }

    [Theory]
    [InlineData("Roles/x.xml", "<AppModule />", "the root element is 'AppModule', not 'Role'")]
    [InlineData("Roles/x.xml", """<Role name="x" />""", "missing id attribute")]
    [InlineData("Roles/x.xml", """<Role id=" {} " name="x" />""", "missing id attribute")]
    [InlineData("Roles/x.xml", """<Role id="00000000-0000-0000-0000-00000000000b" />""", "missing name attribute")]
    [InlineData("Roles/x.xml", """<Role id="00000000-0000-0000-0000-00000000000b" name=" " />""", "missing name attribute")]
    public void A_role_file_of_an_unknown_shape_is_refused_with_its_path(string path, string xml, string message)
    {
        using var t = Folder().Write(path, xml);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.Contains($"{path}: {message}", e.Message);
    }

    [Theory]
    [InlineData("""<Role name="x" />""")]
    [InlineData("""<Role id="00000000-0000-0000-0000-00000000000b" />""")]
    public void A_customizations_role_without_an_id_or_name_is_refused_with_the_customizations_path(string role)
    {
        using var t = Folder().Write("Other/Customizations.xml", SolutionXml.Customizations($"<Roles>{role}</Roles>"));
        using var dir = new TempFolder();
        var zip = System.IO.Path.Combine(dir.Path, "roles.zip");
        SolutionZip.Create(t.Path, zip);

        var e = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(zip));

        Assert.Contains("customizations.xml: missing ", e.Message);
    }
}