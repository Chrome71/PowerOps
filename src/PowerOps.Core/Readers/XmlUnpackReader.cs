using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Reads a solution unpacked by `pac solution unpack` (XML layout, `Other/Solution.xml`).</summary>
public static class XmlUnpackReader
{
    public static Solution Read(string folder)
    {
        var root = Path.GetFullPath(folder);
        if (!Directory.Exists(root))
        {
            throw new InvalidDataException($"{folder}: folder not found.");
        }

        var paths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .ToList();

        return SolutionParser.Parse(new SolutionFiles(
            paths,
            p => SolutionParser.ReadAllText(File.OpenRead(Path.Combine(root, p))),
            "Other/Solution.xml",
            "Other/Customizations.xml"));
    }
}