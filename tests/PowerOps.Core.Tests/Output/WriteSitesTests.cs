using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

// M28: every file PowerOps writes goes through SafeWrite, so a new write call elsewhere in src fails here.
public class WriteSitesTests
{
    private static readonly string[] Forms = ["File.WriteAll", "FileMode.Create", "new StreamWriter("];

    private const string SafeWriteFile = "src/PowerOps.Core/Output/SafeWrite.cs";
    private const string McpFile = "src/PowerOps.Cli/McpCommand.cs";

    // The MCP `save` must refuse an existing file or link, which CreateNew does and SafeWrite (it replaces) does not.
    private const string McpSave = "stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);";

    // The lines that write outside SafeWrite, as "path:line".
    private static IEnumerable<string> Violations(string relativePath, IEnumerable<string> lines) =>
        relativePath == SafeWriteFile
            ? []
            : lines.Select((text, i) => (Text: text.Trim(), Line: i + 1))
                .Where(l => Forms.Any(l.Text.Contains) && !(relativePath == McpFile && l.Text == McpSave))
                .Select(l => $"{relativePath}:{l.Line}");

    [Fact]
    public void No_file_in_src_writes_except_through_SafeWrite()
    {
        var src = Path.Combine(Golden.RepoRoot, "src");
        var found = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .SelectMany(f => Violations(Path.GetRelativePath(Golden.RepoRoot, f).Replace('\\', '/'), File.ReadLines(f)))
            .ToList();

        Assert.Empty(found);
    }

    [Fact]
    public void The_scan_finds_each_form_that_writes_and_exempts_only_SafeWrite_and_the_mcp_save()
    {
        string[] planted =
        [
            "File.WriteAllText(path, text);",
            "File.WriteAllBytes(path, bytes);",
            "using var s = new FileStream(path, FileMode.Create);",
            "using var s = new FileStream(path, FileMode.CreateNew);",
            "using var w = new StreamWriter(path);",
        ];

        foreach (var line in planted)
        {
            Assert.Equal(["src/PowerOps.Cli/Other.cs:2"], Violations("src/PowerOps.Cli/Other.cs", ["var a = 1;", line]));
            Assert.Empty(Violations(SafeWriteFile, [line]));
            Assert.Equal([$"{McpFile}:1"], Violations(McpFile, [line]));
        }

        Assert.Empty(Violations(McpFile, ["        " + McpSave]));
    }
}