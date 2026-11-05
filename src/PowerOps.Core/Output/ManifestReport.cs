using PowerOps.Core.Readers;

namespace PowerOps.Core.Output;

/// <summary>The text of <c>powerops manifest</c>, with "\n" line endings on every OS.</summary>
public static class ManifestReport
{
    public static void WriteText(TextWriter writer, string display, SolutionManifest manifest)
    {
        writer.Write(First(display, manifest) + "\n");
        foreach (var f in manifest.Files)
        {
            writer.Write($"{f.Sha256}  {TextReport.Clean(f.Path)}\n");
        }
    }

    public static void WriteComparison(TextWriter writer, string oldDisplay, SolutionManifest old, string newDisplay, SolutionManifest current, ManifestComparison comparison)
    {
        writer.Write(First(oldDisplay, old) + "\n");
        writer.Write(First(newDisplay, current) + "\n");
        foreach (var (kind, paths) in new[] { ("added", comparison.Added), ("removed", comparison.Removed), ("changed", comparison.Changed) })
        {
            foreach (var p in paths)
            {
                writer.Write($"{kind} {TextReport.Clean(p)}\n");
            }
        }

        writer.Write($"{Files(comparison.Same)} the same\n");
    }

    private static string First(string display, SolutionManifest m) =>
        m.Kind == "zip"
            ? $"{TextReport.Clean(display)}: zip, digest {m.Digest}"
            : $"{TextReport.Clean(display)}: folder, {Files(m.Files.Count)}, digest {m.Digest}";

    private static string Files(int n) => $"{n} file{(n == 1 ? "" : "s")}";
}