using System.IO.Compression;

namespace PowerOps.Core.Readers;

/// <summary>Finds solutions (unpacked XML folders, YAML layout `solution.yml` files and exported zips) under a path.</summary>
public static class SolutionDiscovery
{
    internal static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", "node_modules" };

    // Symlinks are not followed (loops, escapes); unreadable folders are skipped; `X.ZIP` matches too.
    internal static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        MatchCasing = MatchCasing.CaseInsensitive,
    };

    // Not const so tests can inject a small cap; only they change it.
    internal static int MaxFolders = 100_000;

    private static readonly ReadBudget DefaultBudget = new();

    public static IReadOnlyList<string> Find(string path) => Find(path, MaxFolders);

    internal static IReadOnlyList<string> Find(string path, int maxFolders)
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
            var folders = 0;
            Walk(path, found, path, maxFolders, ref folders);
        }
        else if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            found.Add(path);
        }

        // Sorted by the forward-slash form so output is identical on Windows.
        found.Sort((a, b) => string.CompareOrdinal(a.Replace('\\', '/'), b.Replace('\\', '/')));
        return found;
    }

    private static void Walk(string dir, List<string> found, string root, int maxFolders, ref int folders)
    {
        if (++folders > maxFolders)
        {
            throw new InvalidDataException($"{root}: more than {maxFolders} folders to search; narrow the path.");
        }

        if (File.Exists(Path.Combine(dir, "Other", "Solution.xml")))
        {
            found.Add(dir);
            return;
        }

        // A YAML root: solutions/<name>/solution.yml. Only roots at or below the given path are found.
        var yamlSolutions = Directory.EnumerateDirectories(dir, "solutions", Options)
            .SelectMany(s => Directory.EnumerateDirectories(s, "*", Options))
            .SelectMany(s => Directory.EnumerateFiles(s, "solution.yml", Options))
            .ToList();
        if (yamlSolutions.Count > 0)
        {
            found.AddRange(yamlSolutions);
            return;
        }

        foreach (var sub in Directory.EnumerateDirectories(dir, "*", Options).Where(d => !Skipped.Contains(Path.GetFileName(d))))
        {
            Walk(sub, found, root, maxFolders, ref folders);
        }

        found.AddRange(Directory.EnumerateFiles(dir, "*.zip", Options).Where(HasRootSolutionXml));
    }

    // Like Options, but lists the links too, so they can be reported.
    private static readonly EnumerationOptions WithLinks = new() { IgnoreInaccessible = true, AttributesToSkip = 0, MatchCasing = MatchCasing.CaseInsensitive };

    /// <summary>The files under <paramref name="folder"/>, without descending into <see cref="Skipped"/> folders or following symlinks. A link inside (file or folder) is added to <paramref name="links"/>, so the caller can say it was not read.</summary>
    internal static IEnumerable<string> EnumerateFiles(string folder, ICollection<string>? links = null)
    {
        var pending = new Stack<string>();
        pending.Push(folder);
        while (pending.TryPop(out var dir))
        {
            foreach (var entry in new DirectoryInfo(dir).EnumerateFileSystemInfos("*", WithLinks))
            {
                if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    if (!Skipped.Contains(entry.Name))
                    {
                        links?.Add(entry.FullName);
                    }
                }
                else if (entry is DirectoryInfo)
                {
                    if (!Skipped.Contains(entry.Name))
                    {
                        pending.Push(entry.FullName);
                    }
                }
                else
                {
                    yield return entry.FullName;
                }
            }
        }
    }

    private static bool HasRootSolutionXml(string zipPath)
    {
        // Over-cap zips are refused, not skipped, like a zip named on the command line. So is a file that cannot be opened or read;
        // only one that vanished during the scan is skipped. A file that is readable but not a zip is skipped below.
        try
        {
            ZipGuard.Check(zipPath, DefaultBudget);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"{zipPath}: cannot be read: {e.Message}", e);
        }

        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            return zip.Entries.Any(e => e.FullName.Equals("solution.xml", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            // Only a file that starts with the zip signature is a zip that failed; a Git LFS pointer or an empty placeholder named *.zip is skipped.
            if (StartsWithZipSignature(zipPath))
            {
                throw new InvalidDataException($"{zipPath}: cannot be opened as a zip: {e.Message}", e);
            }

            return false;
        }
    }

    private static bool StartsWithZipSignature(string path)
    {
        try
        {
            using var file = File.OpenRead(path);
            Span<byte> head = stackalloc byte[2];
            return file.ReadAtLeast(head, 2, throwOnEndOfStream: false) == 2 && head[0] == (byte)'P' && head[1] == (byte)'K';
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}