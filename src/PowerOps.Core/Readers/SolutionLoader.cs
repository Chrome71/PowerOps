using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Loads a solution from a path returned by <see cref="SolutionDiscovery"/>.</summary>
public static class SolutionLoader
{
    public static Solution Load(string path) =>
        Directory.Exists(path) ? XmlUnpackReader.Read(path)
        : path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ZipReader.Read(path)
        : throw new InvalidDataException($"{path}: not a solution folder or zip.");
}