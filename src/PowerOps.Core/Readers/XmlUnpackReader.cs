using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Reads a solution unpacked by `pac solution unpack` (XML layout, `Other/Solution.xml`).</summary>
public static class XmlUnpackReader
{
    public static Solution Read(string folder) => Read(folder, new ReadBudget());

    /// <param name="skippedLinks">Receives the path of each link inside the folder, which is not followed.</param>
    internal static Solution Read(string folder, ReadBudget budget, ICollection<string>? skippedLinks = null)
    {
        var root = Path.GetFullPath(folder);
        if (!Directory.Exists(root))
        {
            throw new InvalidDataException($"{folder}: folder not found.");
        }

        // Symlinks are never followed, and .git, node_modules, bin and obj are neither listed nor counted.
        var links = new List<string>();
        var paths = budget.LimitFiles(SolutionDiscovery.EnumerateFiles(root, links), folder)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .ToList();
        links.ForEach(l => skippedLinks?.Add(Path.Combine(folder, Path.GetRelativePath(root, l)).Replace('\\', '/')));

        return SolutionParser.Parse(new SolutionFiles(
            paths,
            p => SolutionParser.ReadCapped(File.OpenRead(Path.Combine(root, p)), p, budget),
            "Other/Solution.xml",
            "Other/Customizations.xml",
            budget));
    }
}