using System.IO.Compression;
using System.Text;

namespace PowerOps.Tests.Shared;

public static class SolutionZip
{
    // One stamp for every entry, so the same content always gives the same bytes and SHA-256 (the fixture zips use this date too).
    private static readonly DateTimeOffset Stamp = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);

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
            var zipEntry = zip.CreateEntry(entry);
            zipEntry.LastWriteTime = Stamp;
            using var target = zipEntry.Open();
            using var source = File.OpenRead(file);
            source.CopyTo(target);
        }
    }

    /// <summary>Writes a canvas app package (an `.msapp` is a zip) with exactly these entry names, so a test can use the backslashes `pac` writes.</summary>
    public static void Msapp(string path, params (string Name, string Text)[] entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, text) in entries)
        {
            var entry = zip.CreateEntry(name);
            entry.LastWriteTime = Stamp;
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(text);
        }
    }
}