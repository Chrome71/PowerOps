using System.IO.Compression;

namespace PowerOps.Core.Readers;

/// <summary>Finds solutions (unpacked XML folders and exported zips) under a path.</summary>
public static class SolutionDiscovery
{
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", "node_modules" };

    public static IReadOnlyList<string> Find(string path)
    {
        var found = new List<string>();
        if (File.Exists(path))
        {
            // An explicitly named zip is taken as is; the loader reports it if it is not a solution.
            if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                found.Add(path);
            }
        }
        else if (Directory.Exists(path))
        {
            Walk(path, found);
        }
        else if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            found.Add(path);
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static void Walk(string dir, List<string> found)
    {
        if (File.Exists(Path.Combine(dir, "Other", "Solution.xml")))
        {
            found.Add(dir);
            return;
        }

        foreach (var sub in Directory.EnumerateDirectories(dir).Where(d => !Skipped.Contains(Path.GetFileName(d))))
        {
            Walk(sub, found);
        }

        found.AddRange(Directory.EnumerateFiles(dir, "*.zip").Where(HasRootSolutionXml));
    }

    private static bool HasRootSolutionXml(string zipPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            return zip.Entries.Any(e => e.FullName.Equals("solution.xml", StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}