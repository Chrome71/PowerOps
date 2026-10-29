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

    public YamlRoot(string folder, ReadBudget? budget = null)
    {
        Folder = Path.GetFullPath(folder);
        Budget = budget ?? new ReadBudget();
        if (!Directory.Exists(Folder))
        {
            throw new InvalidDataException($"{folder}: folder not found.");
        }

        // Symlinks are never followed, and .git, node_modules, bin and obj are neither listed nor counted.
        foreach (var p in Budget.LimitFiles(SolutionDiscovery.EnumerateFiles(Folder), folder)
            .Select(f => Path.GetRelativePath(Folder, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal))
        {
            Paths.TryAdd(p, p);
        }
    }

    public ReadBudget Budget { get; }

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