using System.Xml.Linq;

namespace PowerOps.Core.Readers;

/// <summary>
/// The files of one Dataverse Git-integration YAML root (the folder holding `solutions/`). Several solutions can share it,
/// so each file is listed and read once against one <see cref="ReadBudget"/>.
/// </summary>
internal sealed class YamlRoot
{
    private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, XElement> _parsed = new(StringComparer.Ordinal);
    private readonly List<string> _links;
    private readonly HashSet<string> _reported = new(StringComparer.Ordinal);

    public YamlRoot(string folder, ReadBudget? budget = null)
    {
        Folder = Path.GetFullPath(folder);
        Budget = budget ?? new ReadBudget();
        if (!Directory.Exists(Folder))
        {
            throw new InvalidDataException($"{folder}: folder not found.");
        }

        // Symlinks are never followed, and .git, node_modules, bin and obj are neither listed nor counted.
        var links = new List<string>();
        foreach (var p in Budget.LimitFiles(SolutionDiscovery.EnumerateFiles(Folder, links), folder)
            .Select(f => Path.GetRelativePath(Folder, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal))
        {
            Paths.TryAdd(p, p);
        }

        _links = links.Select(l => System.IO.Path.GetRelativePath(Folder, l).Replace('\\', '/')).ToList();
    }

    public ReadBudget Budget { get; }

    /// <summary>Adds the full path (forward slashes) of each link found under the root that <paramref name="belongs"/> accepts (given as a root-relative path) to <paramref name="sink"/>, once per link even when several solutions of the root claim it.</summary>
    public void ReportLinks(Func<string, bool> belongs, ICollection<string> sink)
    {
        foreach (var link in _links.Where(belongs).Where(_reported.Add))
        {
            sink.Add(System.IO.Path.Combine(Folder, link).Replace('\\', '/'));
        }
    }

    public string Folder { get; }

    /// <summary>All files as relative forward-slash paths, keyed case-insensitively, in ordinal order.</summary>
    public Dictionary<string, string> Paths { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string ReadText(string path) =>
        _cache.TryGetValue(path, out var text)
            ? text
            : _cache[path] = SolutionParser.ReadCapped(File.OpenRead(System.IO.Path.Combine(Folder, path)), path, Budget);

    /// <exception cref="InvalidDataException">The message starts with <paramref name="path"/>.</exception>
    /// <remarks>Parsed once per path and shared by every solution of the root, so callers must not change it.</remarks>
    public XElement LoadYaml(string path)
    {
        if (_parsed.TryGetValue(path, out var parsed))
        {
            return parsed;
        }

        var text = ReadText(path);
        try
        {
            return _parsed[path] = YamlXml.Convert(text);
        }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }
}