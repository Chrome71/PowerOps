using System.Text;

namespace PowerOps.Core.Tests.Readers;

/// <summary>Temp folder for building small solution layouts from inline text.</summary>
internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("powerops-test-").FullName;

    public TempFolder Write(string relativePath, string content, bool bom = false)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, new UTF8Encoding(bom));
        return this;
    }

    /// <summary>True when this file system ignores case in names (Windows, usual macOS), so two names that differ only in case cannot both exist.</summary>
    public bool IgnoresCase
    {
        get
        {
            Write("case-probe.tmp", "");
            return File.Exists(System.IO.Path.Combine(Path, "CASE-PROBE.TMP"));
        }
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal static class SolutionXml
{
    public static string Manifest(
        string uniqueName = "Demo",
        string version = "1.2.3.4",
        string managed = "0",
        string publisher = "demopub",
        string prefix = "demo",
        string inner = "") =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <ImportExportXml><SolutionManifest>
          <UniqueName>{uniqueName}</UniqueName><Version>{version}</Version><Managed>{managed}</Managed>
          <Publisher><UniqueName>{publisher}</UniqueName><CustomizationPrefix>{prefix}</CustomizationPrefix></Publisher>
          {inner}
        </SolutionManifest></ImportExportXml>
        """;

    public static string Customizations(string inner) =>
        $"<ImportExportXml>{inner}</ImportExportXml>";

    /// <summary>A table's `Entity` element, as `Entities/&lt;name&gt;/Entity.xml` and the customizations file hold it.</summary>
    public static string Entity(string name, string set) =>
        $"<Entity><Name>{name}</Name><EntityInfo><entity Name=\"{name}\"><EntitySetName>{set}</EntitySetName></entity></EntityInfo></Entity>";

    /// <summary>
    /// Three `EntityRelationship` elements: a one-to-many with a role 1, a role 0, a role of type 2 and a role with no name;
    /// a one-to-many with no roles (as the system relationships are exported); a many-to-many with roles.
    /// </summary>
    public const string Relationships = """
        <EntityRelationship Name="demo_Expense_CostCentre_demo_CostCentre">
          <EntityRelationshipType>OneToMany</EntityRelationshipType>
          <ReferencingEntityName>demo_Expense</ReferencingEntityName>
          <ReferencedEntityName>demo_CostCentre</ReferencedEntityName>
          <ReferencingAttributeName>demo_CostCentre</ReferencingAttributeName>
          <EntityRelationshipRoles>
            <EntityRelationshipRole><NavigationPropertyName>demo_CostCentre</NavigationPropertyName><RelationshipRoleType>1</RelationshipRoleType></EntityRelationshipRole>
            <EntityRelationshipRole><NavigationPropertyName>demo_Expense_CostCentre_demo_CostCentre</NavigationPropertyName><RelationshipRoleType>0</RelationshipRoleType></EntityRelationshipRole>
            <EntityRelationshipRole><NavigationPropertyName>demo_Other</NavigationPropertyName><RelationshipRoleType>2</RelationshipRoleType></EntityRelationshipRole>
            <EntityRelationshipRole><RelationshipRoleType>1</RelationshipRoleType></EntityRelationshipRole>
          </EntityRelationshipRoles>
        </EntityRelationship>
        <EntityRelationship Name="business_unit_demo_expense">
          <EntityRelationshipType>OneToMany</EntityRelationshipType>
          <ReferencingEntityName>demo_Expense</ReferencingEntityName>
          <ReferencedEntityName>BusinessUnit</ReferencedEntityName>
        </EntityRelationship>
        <EntityRelationship Name="demo_expense_tag">
          <EntityRelationshipType>ManyToMany</EntityRelationshipType>
          <ReferencingEntityName>demo_Expense</ReferencingEntityName>
          <ReferencedEntityName>demo_Tag</ReferencedEntityName>
          <EntityRelationshipRoles>
            <EntityRelationshipRole><NavigationPropertyName>demo_tags</NavigationPropertyName><RelationshipRoleType>1</RelationshipRoleType></EntityRelationshipRole>
            <EntityRelationshipRole><NavigationPropertyName>demo_expenses</NavigationPropertyName><RelationshipRoleType>0</RelationshipRoleType></EntityRelationshipRole>
          </EntityRelationshipRoles>
        </EntityRelationship>
        """;

    /// <summary>The one-to-many without roles of <see cref="Relationships"/> alone.</summary>
    public const string RelationshipWithoutRoles = """
        <EntityRelationship Name="business_unit_demo_expense">
          <EntityRelationshipType>OneToMany</EntityRelationshipType>
          <ReferencingEntityName>demo_Expense</ReferencingEntityName>
          <ReferencedEntityName>BusinessUnit</ReferencedEntityName>
        </EntityRelationship>
        """;
}