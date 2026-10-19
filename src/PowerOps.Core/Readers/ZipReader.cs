using System.IO.Compression;

using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Reads an exported solution zip (root `solution.xml`) by streaming entries; nothing is extracted to disk.</summary>
public static class ZipReader
{
    private const long MaxEntryBytes = 50L * 1024 * 1024;

    public static Solution Read(string zipPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
            foreach (var entry in zip.Entries)
            {
                var name = entry.FullName.Replace('\\', '/');
                if (name.EndsWith('/') || !IsSafe(name))
                {
                    continue; // directories, rooted and `..` entries (zip-slip) are never read
                }

                entries.TryAdd(name, entry);
            }

            return SolutionParser.Parse(new SolutionFiles(
                entries.Keys,
                p => ReadEntry(entries[p], p),
                "solution.xml",
                "customizations.xml"));
        }
        catch (InvalidDataException e) when (!e.Message.StartsWith(zipPath, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{zipPath}: {e.Message}", e);
        }
    }

    private static bool IsSafe(string name) =>
        !name.StartsWith('/')
        && !(name.Length > 1 && name[1] == ':')
        && !name.Split('/').Contains("..");

    private static string ReadEntry(ZipArchiveEntry entry, string name)
    {
        // Declared sizes can lie, so the cap is enforced on the bytes actually read.
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int n;
        while ((n = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + n > MaxEntryBytes)
            {
                throw new InvalidDataException($"{name}: larger than {MaxEntryBytes / (1024 * 1024)} MB.");
            }

            buffer.Write(chunk, 0, n);
        }

        buffer.Position = 0;
        return SolutionParser.ReadAllText(buffer);
    }
}