using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>The formulas of a canvas app's `Src/*.pa.yaml` files, from a `Src` folder (YAML layout) or from inside an `.msapp` (XML layouts).</summary>
internal static class CanvasSource
{
    /// <summary>The formulas of one `.pa.yaml` file converted by <see cref="YamlXml"/>: the property values starting with `=` of the app, each screen, each component and their nested controls.</summary>
    public static IEnumerable<CanvasFormula> Formulas(XElement top, string app, string file)
    {
        var holders = new[] { top.Element("App") }
            .Concat(top.Element("Screens")?.Elements() ?? [])
            .Concat(top.Element("ComponentDefinitions")?.Elements() ?? []);
        foreach (var holder in holders.OfType<XElement>().SelectMany(Controls))
        {
            foreach (var property in holder.Element("Properties")?.Elements() ?? [])
            {
                if (!property.HasElements && property.Value.StartsWith('='))
                {
                    yield return new CanvasFormula(app, XmlConvert.DecodeName(holder.Name.LocalName), XmlConvert.DecodeName(property.Name.LocalName), property.Value.TrimEnd(), file);
                }
            }
        }
    }

    /// <summary>The element itself, then its nested controls (each `Children` item holds one control element), in document order.</summary>
    private static IEnumerable<XElement> Controls(XElement control) =>
        control.Elements("Children").SelectMany(c => c.Elements()).SelectMany(Controls).Prepend(control);

    /// <summary>
    /// The formulas of the `Src/*.pa.yaml` entries of an `.msapp` held in memory, or null when it has none or is not a zip (no `PK` signature: an empty file, a Git LFS pointer). Only those entries are opened, in memory, in ordinal name order;
    /// `Controls/` and the other JSON are not source and are never read. Each formula's file is <paramref name="path"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The package is not a zip, is over a cap, or an entry is malformed; the message starts with <paramref name="path"/> (and the entry name, for one entry).</exception>
    public static List<CanvasFormula>? ReadMsapp(byte[] bytes, string path, string app, ReadBudget budget)
    {
        if (bytes.Length < 2 || bytes[0] != (byte)'P' || bytes[1] != (byte)'K')
        {
            return null;
        }

        try
        {
            using var memory = new MemoryStream(bytes);
            ZipGuard.Check(memory, path, budget);
            using var zip = new ZipArchive(memory, ZipArchiveMode.Read);

            // Everything but the source files is never read.
            var sources = ZipReader.Entries(zip.Entries, path, budget, name => name.StartsWith("Src/", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".pa.yaml", StringComparison.OrdinalIgnoreCase));
            if (sources.Count == 0)
            {
                return null;
            }

            var formulas = new List<CanvasFormula>();
            foreach (var (name, entry) in sources.OrderBy(e => e.Key, StringComparer.Ordinal))
            {
                formulas.AddRange(ReadEntry(entry, $"{path}: {name}", app, path, budget));
            }

            return formulas;
        }
        // The library's own errors for a package that is not a zip, is truncated or uses a method it cannot read.
        catch (Exception e) when (e is InvalidDataException or IOException or NotSupportedException && !e.Message.StartsWith(path + ": ", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    private static List<CanvasFormula> ReadEntry(ZipArchiveEntry entry, string name, string app, string file, ReadBudget budget)
    {
        try
        {
            if (entry.Length > budget.MaxFileBytes)
            {
                throw new InvalidDataException($"{name}: larger than {budget.MaxFileBytes / (1024 * 1024)} MB.");
            }

            return [.. Formulas(YamlXml.Convert(SolutionParser.ReadCapped(entry.Open(), name, budget), n => budget.CountNode(name, n)), app, file)];
        }
        catch (Exception e) when (e is InvalidDataException or IOException or NotSupportedException && !e.Message.StartsWith(name + ": ", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{name}: {e.Message}", e);
        }
    }
}