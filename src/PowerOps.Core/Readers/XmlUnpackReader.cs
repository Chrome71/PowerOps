using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Reads a solution unpacked by `pac solution unpack` (XML layout, `Other/Solution.xml`).</summary>
public static class XmlUnpackReader
{
    public static Solution Read(string folder) => Read(folder, new ReadBudget());

    internal static Solution Read(string folder, ReadBudget budget)
    {
        var root = Path.GetFullPath(folder);
        if (!Directory.Exists(root))
        {
            throw new InvalidDataException($"{folder}: folder not found.");
        }

        // Symlinks are never followed, and .git, node_modules, bin and obj are neither listed nor counted.
        var paths = budget.LimitFiles(SolutionDiscovery.EnumerateFiles(root), folder)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .ToList();

        return SolutionParser.Parse(new SolutionFiles(
            paths,
            p => SolutionParser.ReadCapped(File.OpenRead(Path.Combine(root, p)), p, budget),
            "Other/Solution.xml",
            "Other/Customizations.xml",
            budget));
    }
}