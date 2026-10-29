using System.Text.Json;
using System.Xml.Linq;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Every package a project restores must carry a plain MIT or Apache-2.0 licence expression,
// read offline from project.assets.json and the nuspecs in the NuGet packages folder. Why: a
// new version of a dependency can change licence (JsonSchema.Net 9.x moved to a non-OSI EULA,
// so it stays at 8.0.5); a compound expression, a licence file or a licence URL needs a person to look.
public class LicenceTests
{
    private static readonly string[] Allowed = ["MIT", "Apache-2.0"];

    private static readonly string[] Projects =
    [
        Path.Combine("src", "PowerOps.Cli"),
        Path.Combine("src", "PowerOps.Core"),
        Path.Combine("tests", "PowerOps.Cli.Tests"),
        Path.Combine("tests", "PowerOps.Core.Tests"),
    ];

    [Fact]
    public void Every_restored_package_is_MIT_or_Apache_2_0()
    {
        var packages = Projects
            .SelectMany(p => Licences(Path.Combine(Golden.RepoRoot, p, "obj", "project.assets.json")))
            .Distinct()
            .ToList();

        Assert.NotEmpty(packages);
        Assert.Empty(Problems(packages));
    }

    [Fact]
    public void A_BSD_3_Clause_package_is_reported()
    {
        var problems = FakeClosure("BSD-3-Clause");

        Assert.Equal(["Fake.Bsd 1.0.0: BSD-3-Clause"], problems);
    }

    [Fact]
    public void An_MIT_package_passes()
    {
        Assert.Empty(FakeClosure("MIT"));
    }

    private static List<string> FakeClosure(string licence)
    {
        var dir = Directory.CreateTempSubdirectory("powerops-licence-");
        try
        {
            var packages = Path.Combine(dir.FullName, "packages");
            Directory.CreateDirectory(Path.Combine(packages, "fake.bsd", "1.0.0"));
            File.WriteAllText(
                Path.Combine(packages, "fake.bsd", "1.0.0", "fake.bsd.nuspec"),
                $"""<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>Fake.Bsd</id><license type="expression">{licence}</license></metadata></package>""");
            var assets = Path.Combine(dir.FullName, "project.assets.json");
            File.WriteAllText(assets, JsonSerializer.Serialize(new
            {
                packageFolders = new Dictionary<string, object> { [packages] = new { } },
                libraries = new Dictionary<string, object>
                {
                    ["Fake.Bsd/1.0.0"] = new { type = "package", path = "fake.bsd/1.0.0" },
                },
            }));
            return Problems(Licences(assets));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    internal static List<string> Problems(IEnumerable<(string Package, string Licence)> packages) =>
        [.. packages.Where(p => !Allowed.Contains(p.Licence)).Select(p => $"{p.Package}: {p.Licence}")];

    // Package is "Id Version"; Licence is the expression, or a short description of what was found instead.
    internal static IEnumerable<(string Package, string Licence)> Licences(string assetsPath)
    {
        Assert.True(File.Exists(assetsPath), $"{assetsPath} is missing: build the project before running this test.");
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var folders = assets.RootElement.GetProperty("packageFolders").EnumerateObject().Select(f => f.Name).ToList();

        foreach (var lib in assets.RootElement.GetProperty("libraries").EnumerateObject())
        {
            if (lib.Value.GetProperty("type").GetString() != "package")
            {
                continue;
            }

            var (id, version) = lib.Name.Split('/') is [var i, var v] ? (i, v) : throw new InvalidDataException($"{assetsPath}: bad library name {lib.Name}");
            var path = lib.Value.GetProperty("path").GetString()!;
            var nuspec = folders
                .Select(f => Path.Combine(f, path, id.ToLowerInvariant() + ".nuspec"))
                .FirstOrDefault(File.Exists);

            yield return ($"{id} {version}", nuspec is null ? "nuspec not found" : LicenceOf(nuspec));
        }
    }

    private static string LicenceOf(string nuspec)
    {
        var licence = XDocument.Load(nuspec).Descendants().FirstOrDefault(e => e.Name.LocalName == "license");
        return licence?.Attribute("type")?.Value == "expression" ? licence.Value.Trim() : "no licence expression";
    }
}