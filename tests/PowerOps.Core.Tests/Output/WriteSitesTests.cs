using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

// M28: every file PowerOps writes goes through SafeWrite, so a new call that can write, anywhere in src, fails here until it is routed through SafeWrite or listed below with its reason.
public class WriteSitesTests
{
    // Over the whole text, so a call split over two lines is still seen. \b after Open keeps File.OpenRead and File.OpenText (reads) out, \bFileMode keeps UnixFileMode out, \bFile\. keeps SnapshotFile.Write out.
    private static readonly Regex Writes = new(
        @"\bFile\.(Write\w*|Append\w*|Create\w*|Open|OpenWrite|OpenHandle|Copy|Move|Replace)\b|\bFileMode\.\w+|\bFileAccess\.(Write|ReadWrite)\b|\bnew\s+FileStream\s*\(|\bStreamWriter\b|\bExtractTo\w*|\bXmlWriter\.Create\b|\bDirectory\.CreateDirectory\b",
        RegexOptions.CultureInvariant);

    // "<path>: <match>" -> how many times. A match that is in src and not here, or here and not in src, fails the first test.
    private static readonly Dictionary<string, int> Allowed = new()
    {
        // SafeWrite itself: the typed names, the open of an existing target, the probe and the temp file, and the move (File.WriteAllText is a doc comment).
        ["src/PowerOps.Core/Output/SafeWrite.cs: File.WriteAllText"] = 1,
        ["src/PowerOps.Core/Output/SafeWrite.cs: File.WriteAllBytes"] = 1,
        ["src/PowerOps.Core/Output/SafeWrite.cs: File.OpenHandle"] = 1,
        ["src/PowerOps.Core/Output/SafeWrite.cs: FileMode.Open"] = 1,
        ["src/PowerOps.Core/Output/SafeWrite.cs: FileMode.CreateNew"] = 2,
        ["src/PowerOps.Core/Output/SafeWrite.cs: FileAccess.Write"] = 4,
        ["src/PowerOps.Core/Output/SafeWrite.cs: new FileStream("] = 3,
        ["src/PowerOps.Core/Output/SafeWrite.cs: File.Move"] = 1,

        // The MCP `save` must refuse an existing file or link, which CreateNew does and SafeWrite (it replaces) does not.
        ["src/PowerOps.Cli/McpCommand.cs: new FileStream("] = 1,
        ["src/PowerOps.Cli/McpCommand.cs: FileMode.CreateNew"] = 1,
        ["src/PowerOps.Cli/McpCommand.cs: FileAccess.Write"] = 1,

        // Folders only: `skills install` and `drift --save` make the folder, then write the files in it through SafeWrite.
        ["src/PowerOps.Cli/SkillsCommand.cs: Directory.CreateDirectory"] = 1,
        ["src/PowerOps.Cli/DriftCommand.cs: Directory.CreateDirectory"] = 1,

        // Reads, all FileMode.Open with FileAccess.Read: the hash of a file in `deploy`, and the size check of a zip.
        ["src/PowerOps.Cli/DeployCommand.cs: new FileStream("] = 1,
        ["src/PowerOps.Cli/DeployCommand.cs: FileMode.Open"] = 1,
        ["src/PowerOps.Core/Readers/ZipGuard.cs: new FileStream("] = 1,
        ["src/PowerOps.Core/Readers/ZipGuard.cs: FileMode.Open"] = 1,
    };

    private static string Key(string relativePath, Match match) => $"{relativePath}: {Regex.Replace(match.Value, @"\s+", " ")}";

    [Fact]
    public void No_file_in_src_writes_except_through_SafeWrite_or_a_listed_call()
    {
        var src = Path.Combine(Golden.RepoRoot, "src");
        var found = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .SelectMany(f => Writes.Matches(File.ReadAllText(f)).Select(m => Key(Path.GetRelativePath(Golden.RepoRoot, f).Replace('\\', '/'), m)))
            .GroupBy(k => k)
            .Select(g => $"{g.Key} x{g.Count()}")
            .Order(StringComparer.Ordinal);

        // As text, so the failure shows which line is new or stale.
        Assert.Equal(string.Join('\n', Allowed.Select(a => $"{a.Key} x{a.Value}").Order(StringComparer.Ordinal)), string.Join('\n', found));
    }

    [Fact]
    public void The_scan_finds_each_form_that_writes_and_none_that_only_reads()
    {
        string[] writes =
        [
            "File.WriteAllText(path, text);",
            "File.WriteAllBytes(path, bytes);",
            "File.AppendAllText(path, text);",
            "using var s = File.Create(path);",
            "using var s = File.OpenWrite(path);",
            "using var w = File.CreateText(path);",
            "File.Copy(from, to, true);",
            "File.Move(from, to, true);",
            "File.Replace(from, to, null);",
            "using var h = File.OpenHandle(path, FileMode.Open, FileAccess.Write);",
            "using var s = new FileStream(path, FileMode.Create);",
            "using var s = new FileStream(path, FileMode.CreateNew);",
            "using var s = new FileStream(path, FileMode.OpenOrCreate);",
            "using var s = new FileStream(path, FileMode.Truncate);",
            "using var s = new FileStream(path, FileMode.Open, FileAccess.Write);",
            "using var s = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);",
            "using var s = new FileStream(path,\n    FileMode.Create);",
            "using var s = new FileStream\n(path, options);",
            "using var w = new StreamWriter(path);",
            "ZipFile.ExtractToDirectory(zip, folder);",
            "entry.ExtractToFile(path);",
            "using var w = XmlWriter.Create(path);",
            "Directory.CreateDirectory(folder);",
        ];
        string[] reads =
        [
            "File.ReadAllText(path);",
            "File.OpenRead(path);",
            "File.OpenText(path);",
            "File.Exists(path);",
            "File.Delete(path);",
            "var mode = UnixFileMode.UserRead;",
            "var text = SnapshotFile.Write(snapshot);",
        ];

        Assert.All(writes, text => Assert.True(Writes.IsMatch(text), text));
        Assert.All(reads, text => Assert.False(Writes.IsMatch(text), text));
    }
}