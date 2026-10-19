using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using PowerOps.Core.Model;

namespace PowerOps.Core.Readers;

/// <summary>The file-access seam: relative forward-slash paths plus a way to read one as text.</summary>
internal sealed record SolutionFiles(
    IReadOnlyCollection<string> Paths,
    Func<string, string> ReadText,
    string ManifestFile,
    string CustomizationsFile);

/// <summary>Parses the files of an unpacked or exported solution; folder and zip differ only in <see cref="SolutionFiles"/>.</summary>
internal static partial class SolutionParser
{
    public static string ReadAllText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public static Solution Parse(SolutionFiles files)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in files.Paths.OrderBy(p => p, StringComparer.Ordinal))
        {
            paths.TryAdd(p, p);
        }

        if (!paths.TryGetValue(files.ManifestFile, out var manifestPath))
        {
            throw new InvalidDataException($"{files.ManifestFile} not found.");
        }

        var manifest = LoadXml(files, manifestPath).Element("SolutionManifest")
            ?? throw new InvalidDataException($"{manifestPath}: missing SolutionManifest element.");
        var publisher = manifest.Element("Publisher");

        var customizations = paths.TryGetValue(files.CustomizationsFile, out var customizationsPath)
            ? LoadXml(files, customizationsPath)
            : null;

        var workflows = new List<Workflow>();
        foreach (var p in paths.Values.Where(p => p.StartsWith("Workflows/", StringComparison.OrdinalIgnoreCase)
            && p.EndsWith(".data.xml", StringComparison.OrdinalIgnoreCase)))
        {
            workflows.Add(ReadWorkflow(files, paths, LoadXml(files, p), p));
        }

        foreach (var w in customizations?.Element("Workflows")?.Elements("Workflow") ?? [])
        {
            workflows.Add(ReadWorkflow(files, paths, w, customizationsPath!));
        }

        var canvasApps = paths.Values
            .Where(p => CanvasMetaPath().IsMatch(p))
            .Select(files.ReadText)
            .Concat((customizations?.Element("CanvasApps")?.Elements("CanvasApp") ?? []).Select(e => e.ToString()))
            .ToList();

        return new Solution(
            UniqueName: Text(manifest, "UniqueName") ?? "",
            Version: Text(manifest, "Version") ?? "",
            Managed: Text(manifest, "Managed") == "1",
            Publisher: new Publisher(
                publisher is null ? "" : Text(publisher, "UniqueName") ?? "",
                publisher is null ? "" : Text(publisher, "CustomizationPrefix") ?? ""),
            RootComponents: (manifest.Element("RootComponents")?.Elements("RootComponent") ?? [])
                .Select(e => new RootComponent((string?)e.Attribute("type") ?? "", Opt((string?)e.Attribute("schemaName")), NormalizeId((string?)e.Attribute("id"))))
                .ToList(),
            MissingDependencies: (manifest.Element("MissingDependencies")?.Elements("MissingDependency") ?? [])
                .Select(e => new MissingDependency(
                    ReadNode(e.Element("Required") ?? new XElement("Required")),
                    ReadNode(e.Element("Dependent") ?? new XElement("Dependent"))))
                .ToList(),
            ConnectionReferences: (customizations?.Element("connectionreferences")?.Elements("connectionreference") ?? [])
                .Select(e => new ConnectionReference(
                    (string?)e.Attribute("connectionreferencelogicalname") ?? "",
                    Text(e, "connectorid"),
                    customizationsPath!))
                .ToList(),
            EnvironmentVariables: paths.Values
                .Where(p => EnvVarDefinitionPath().IsMatch(p))
                .Select(p => ReadEnvironmentVariable(files, paths, p))
                .ToList(),
            Workflows: workflows,
            CanvasAppTexts: canvasApps,
            ManifestFile: manifestPath);
    }

    private static Workflow ReadWorkflow(SolutionFiles files, Dictionary<string, string> paths, XElement e, string definedIn)
    {
        var jsonName = Opt(Text(e, "JsonFileName")?.TrimStart('/'));
        var jsonPath = jsonName is not null && paths.TryGetValue(jsonName, out var actual) ? actual : null;
        return new Workflow(
            NormalizeId((string?)e.Attribute("WorkflowId")) ?? "",
            (string?)e.Attribute("Name") ?? "",
            int.TryParse(Text(e, "Category"), out var category) ? category : 0,
            int.TryParse(Text(e, "StateCode"), out var state) ? state : null,
            jsonName ?? definedIn,
            jsonPath is null ? null : files.ReadText(jsonPath));
    }

    private static EnvironmentVariable ReadEnvironmentVariable(SolutionFiles files, Dictionary<string, string> paths, string path)
    {
        var def = LoadXml(files, path);
        var valuesName = path[..path.LastIndexOf('/')] + "/environmentvariablevalues.json";
        string? current = null;
        if (paths.TryGetValue(valuesName, out var valuesPath))
        {
            current = ReadCurrentValue(files.ReadText(valuesPath), valuesPath);
        }

        return new EnvironmentVariable(
            (string?)def.Attribute("schemaname") ?? "",
            Text(def, "type"),
            Text(def, "isrequired") is "1" or "true",
            Text(def, "defaultvalue"),
            current,
            path,
            valuesPath);
    }

    private static string? ReadCurrentValue(string json, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("environmentvariablevalues", out var values)
                && values.ValueKind == JsonValueKind.Object
                && values.TryGetProperty("environmentvariablevalue", out var value))
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    value = value.EnumerateArray().FirstOrDefault();
                }

                if (value.ValueKind == JsonValueKind.Object
                    && value.TryGetProperty("value", out var v)
                    && v.ValueKind == JsonValueKind.String)
                {
                    return v.GetString();
                }
            }

            return null;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    private static DependencyNode ReadNode(XElement e) => new(
        (string?)e.Attribute("type") ?? "",
        // Some nodes are keyed by `id.schemaname` / `id.connectionreferencelogicalname` instead of schemaName.
        Opt((string?)e.Attribute("schemaName"))
            ?? Opt(e.Attributes().FirstOrDefault(a => a.Name.LocalName.StartsWith("id.", StringComparison.Ordinal))?.Value),
        NormalizeId((string?)e.Attribute("id")),
        Opt((string?)e.Attribute("displayName")),
        Opt((string?)e.Attribute("solution")));

    private static XElement LoadXml(SolutionFiles files, string path)
    {
        try
        {
            // DTDs are prohibited: solutions are untrusted input.
            using var reader = XmlReader.Create(new StringReader(files.ReadText(path)), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            return XDocument.Load(reader).Root!;
        }
        catch (XmlException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    private static string? Text(XElement e, string child) => Opt(e.Element(child)?.Value.Trim());

    private static string? Opt(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static string? NormalizeId(string? id) => Opt(id?.Trim().Trim('{', '}').ToLowerInvariant());

    [GeneratedRegex(@"^CanvasApps/[^/]+\.meta\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex CanvasMetaPath();

    [GeneratedRegex(@"^environmentvariabledefinitions/[^/]+/environmentvariabledefinition\.xml$", RegexOptions.IgnoreCase)]
    private static partial Regex EnvVarDefinitionPath();
}