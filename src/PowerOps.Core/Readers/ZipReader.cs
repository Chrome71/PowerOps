using System.IO.Compression;

using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Reads an exported solution zip (root `solution.xml`) by streaming entries; nothing is extracted to disk.</summary>
public static class ZipReader
{
    public static Solution Read(string zipPath) => Read(zipPath, new ReadBudget());

    internal static Solution Read(string zipPath, ReadBudget budget)
    {
        ZipGuard.Check(zipPath, budget);
        using var zip = ZipFile.OpenRead(zipPath);
        var entries = Entries(zip.Entries, zipPath, budget);

        return SolutionParser.Parse(new SolutionFiles(
            entries.Keys,
            p => SolutionParser.ReadCapped(OpenEntry(entries[p], p, budget.MaxFileBytes), p, budget),
            "solution.xml",
            "customizations.xml",
            budget,
            // An .msapp is a zip, so it may be as long as one.
            p => OpenEntry(entries[p], p, budget.MaxZipBytes)));
    }

    /// <summary>The file entries of a zip that <paramref name="keep"/> accepts (all when null), by name with `\` read as `/`.</summary>
    /// <exception cref="InvalidDataException">More entries than the budget allows, or two kept names that are equal without case; the message starts with <paramref name="zipPath"/>.</exception>
    internal static Dictionary<string, ZipArchiveEntry> Entries(IReadOnlyCollection<ZipArchiveEntry> all, string zipPath, ReadBudget budget, Func<string, bool>? keep = null)
    {
        // ZipGuard reads the Zip64 count only when the classic count, size or offset is at its limit; .NET also reads it when the disk number is, so it can find more entries than the guard saw.
        if (all.Count > budget.MaxEntries)
        {
            throw new InvalidDataException($"{zipPath}: more than {budget.MaxEntries} entries.");
        }

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in all)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/') || !IsSafe(name) || keep?.Invoke(name) == false)
            {
                continue; // directories, rooted and `..` entries (zip-slip), and names the caller skips, are never read
            }

            // Two entries with one name, or names that differ only in case, may be read differently by the import than here.
            if (!seen.Add(name))
            {
                throw new InvalidDataException($"{zipPath}: {name} is listed more than once (names are compared without case).");
            }

            entries.Add(name, entry);
        }

        return entries;
    }

    internal static bool IsSafe(string name) =>
        !name.StartsWith('/')
        && !(name.Length > 1 && name[1] == ':')
        && !name.Split('/').Contains("..");

    private static Stream OpenEntry(ZipArchiveEntry entry, string name, long cap)
    {
        if (entry.Length > cap)
        {
            throw new InvalidDataException($"{name}: larger than {cap / (1024 * 1024)} MB.");
        }

        return entry.Open();
    }
}