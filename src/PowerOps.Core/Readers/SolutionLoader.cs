using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>Loads a solution from a path returned by <see cref="SolutionDiscovery"/>.</summary>
public static class SolutionLoader
{
    /// <exception cref="InvalidDataException">The solution is unreadable or malformed; the message starts with <paramref name="path"/>.</exception>
    public static Solution Load(string path, LoadContext? context = null)
    {
        try
        {
            context ??= new LoadContext();
            return Directory.Exists(path) ? XmlUnpackReader.Read(path, new ReadBudget(), context.SkippedLinkSink)
                : YamlReader.IsSolutionYml(path) ? YamlReader.Read(path, context.RootFor(YamlReader.RootFolder(path)), context.SkippedLinkSink)
                : path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ZipReader.Read(path)
                : throw new InvalidDataException($"{path}: not a solution folder or zip.");
        }
        catch (InvalidDataException e) when (e.Message.StartsWith(path, StringComparison.Ordinal))
        {
            throw;
        }
        // InvalidOperationException and ArgumentException: a lone surrogate or a control character in the text of a file.
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }
}