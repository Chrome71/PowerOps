using System.IO.Compression;

namespace PowerOps.Tests.Shared;

public static class SolutionZip
{
    /// <summary>Zips an unpacked solution folder the way an export looks: Other/Solution.xml and Other/Customizations.xml move to the zip root, every other file keeps its relative path.</summary>
    public static void Create(string unpackedDir, string zipPath)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var file in Directory.EnumerateFiles(unpackedDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(unpackedDir, file).Replace('\\', '/');
            var entry = rel switch
            {
                "Other/Solution.xml" => "solution.xml",
                "Other/Customizations.xml" => "customizations.xml",
                _ => rel,
            };
            zip.CreateEntryFromFile(file, entry);
        }
    }
}