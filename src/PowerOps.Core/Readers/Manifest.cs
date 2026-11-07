using System.Text;

using PowerOps.Core.Deploy;

namespace PowerOps.Core.Readers;

/// <summary>One file of a folder: its path relative to the folder (with <c>/</c>) and its SHA-256 in lowercase hex.</summary>
public sealed record ManifestEntry(string Path, string Sha256);

/// <summary>The inventory of a solution: <c>folder</c> lists every file; <c>zip</c> has no files and its digest is the SHA-256 of the zip's bytes.</summary>
public sealed record SolutionManifest(string Kind, IReadOnlyList<ManifestEntry> Files, string Digest);

/// <summary>What differs between two folder manifests, as paths in ordinal order, and how many files are the same.</summary>
public sealed record ManifestComparison(IReadOnlyList<string> Added, IReadOnlyList<string> Removed, IReadOnlyList<string> Changed, int Same);

/// <summary>A file-by-file inventory of a solution with a SHA-256 per file, so "unchanged" can mean compared.</summary>
public static class Manifest
{
    /// <param name="path">An unpacked folder or a .zip file.</param>
    /// <param name="skippedLinks">Receives the path of each link inside a folder, which is not followed.</param>
    /// <exception cref="InvalidDataException">The path is a link, does not exist, is a file other than a .zip, or is over a cap; the message starts with the path.</exception>
    public static SolutionManifest Read(string path, ICollection<string>? skippedLinks = null) => Read(path, new ReadBudget(), skippedLinks);

    internal static SolutionManifest Read(string path, ReadBudget budget, ICollection<string>? skippedLinks = null)
    {
        if (new FileInfo(path).LinkTarget is not null)
        {
            throw new InvalidDataException($"{path}: a symbolic link, not read.");
        }

        if (Directory.Exists(path))
        {
            return ReadFolder(path, budget, skippedLinks);
        }

        if (!File.Exists(path))
        {
            throw new InvalidDataException($"{path}: not found.");
        }

        if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"{path}: not a folder or a .zip file.");
        }

        return new SolutionManifest("zip", [], DeployPlan.Sha256Hex(BoundedRead.ReadAllBytes(path, BoundedRead.MaxZipBytes)));
    }

    private static SolutionManifest ReadFolder(string folder, ReadBudget budget, ICollection<string>? skippedLinks)
    {
        var root = Path.GetFullPath(folder);
        var links = new List<string>();
        var files = new List<ManifestEntry>();
        var relative = SolutionDiscovery.ListFiles(root, folder, budget, links);
        foreach (var rel in relative)
        {
            var file = Path.Combine(root, rel);
            var bytes = BoundedRead.ReadAllBytes(file, budget.MaxFileBytes);
            budget.Consume(bytes.Length, file);
            files.Add(new ManifestEntry(rel, DeployPlan.Sha256Hex(Normalize(bytes))));
        }

        files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        links.ForEach(l => skippedLinks?.Add(SolutionDiscovery.ForwardSlashes(Path.Combine(folder, l))));

        var lines = new StringBuilder();
        files.ForEach(f => lines.Append(f.Sha256).Append("  ").Append(f.Path).Append('\n'));
        return new SolutionManifest("folder", files, DeployPlan.Sha256Hex(Encoding.UTF8.GetBytes(lines.ToString())));
    }

    // The usual text test: with no zero byte, each CRLF becomes LF, so one commit hashes the same on a Windows and a Linux checkout. Other files keep their bytes.
    private static byte[] Normalize(byte[] bytes)
    {
        if (Array.IndexOf(bytes, (byte)0) >= 0)
        {
            return bytes;
        }

        var result = new List<byte>(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (!(bytes[i] == '\r' && i + 1 < bytes.Length && bytes[i + 1] == '\n'))
            {
                result.Add(bytes[i]);
            }
        }

        return [.. result];
    }

    /// <summary>Compares two folder manifests by path and hash.</summary>
    public static ManifestComparison Compare(SolutionManifest old, SolutionManifest current)
    {
        var before = old.Files.ToDictionary(f => f.Path, f => f.Sha256, StringComparer.Ordinal);
        var after = current.Files.ToDictionary(f => f.Path, f => f.Sha256, StringComparer.Ordinal);
        var added = after.Keys.Where(p => !before.ContainsKey(p)).Order(StringComparer.Ordinal).ToList();
        var removed = before.Keys.Where(p => !after.ContainsKey(p)).Order(StringComparer.Ordinal).ToList();
        var common = before.Keys.Where(after.ContainsKey).ToList();
        var changed = common.Where(p => before[p] != after[p]).Order(StringComparer.Ordinal).ToList();
        return new ManifestComparison(added, removed, changed, common.Count - changed.Count);
    }
}