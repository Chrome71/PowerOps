using System.Text;

namespace PowerOps.Core.Tests.Readers;

/// <summary>Temp folder for building small solution layouts from inline text.</summary>
internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("powerops-test-").FullName;

    public TempFolder Write(string relativePath, string content, bool bom = false)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, new UTF8Encoding(bom));
        return this;
    }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal static class SolutionXml
{
    public static string Manifest(
        string uniqueName = "Demo",
        string version = "1.2.3.4",
        string managed = "0",
        string publisher = "demopub",
        string prefix = "demo",
        string inner = "") =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <ImportExportXml><SolutionManifest>
          <UniqueName>{uniqueName}</UniqueName><Version>{version}</Version><Managed>{managed}</Managed>
          <Publisher><UniqueName>{publisher}</UniqueName><CustomizationPrefix>{prefix}</CustomizationPrefix></Publisher>
          {inner}
        </SolutionManifest></ImportExportXml>
        """;

    public static string Customizations(string inner) =>
        $"<ImportExportXml>{inner}</ImportExportXml>";
}