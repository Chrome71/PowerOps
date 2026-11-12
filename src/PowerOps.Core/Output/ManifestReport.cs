using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using PowerOps.Core.Readers;

namespace PowerOps.Core.Output;

/// <summary>The text or JSON of <c>powerops manifest</c>, with "\n" line endings on every OS.</summary>
public static class ManifestReport
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Writes one or two manifests (the old one first) and, for two, the comparison; paths are as given (JSON escapes them, so nothing is cleaned).</summary>
    public static void WriteJson(TextWriter writer, IReadOnlyList<(string Display, SolutionManifest Manifest)> manifests, ManifestComparison? comparison) =>
        writer.Write(JsonSerializer.Serialize(
            new Doc(JsonOutput.SchemaVersion, manifests.Select(m => new Item(m.Display, m.Manifest.Kind, m.Manifest.Digest, m.Manifest.Files)).ToList(), comparison),
            Options) + "\n");

    private sealed record Doc(int SchemaVersion, IReadOnlyList<Item> Manifests, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ManifestComparison? Comparison);

    private sealed record Item(string Path, string Kind, string Digest, IReadOnlyList<ManifestEntry> Files);

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