using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace PowerOps.Cli.Tests;

/// <summary>
/// M25's hostile-input theory through the real command: the canvas-metadata depth probe (R-2) and an input one past the node and zip-entry caps.
/// Those can be passed at their real size here (100,001 files or folders is slow to build); the file and folder caps are proven with
/// small injected caps, the folder cap at command level in <see cref="FolderCapCommandTests"/> and the rest in PowerOps.Core.Tests.
/// R-1 and R-3 pass without their fixes at this level; PowerOps.Core.Tests proves them.
/// </summary>
public sealed class ReaderLimitsTests : IDisposable
{
    private const string Manifest = "<ImportExportXml><SolutionManifest><UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>"
        + "<Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher></SolutionManifest></ImportExportXml>";

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-limits-").FullName;

    // A case builds its input under a temp folder and returns the arguments plus the path the error line must start with.
    private delegate (string[] Args, string ErrorPath) Build(ReaderLimitsTests t);

    private static readonly Dictionary<string, (Build Build, int Exit)> Cases = new()
    {
        ["R-2 analyze: canvas meta 100,000 deep"] = (t => t.Analyze(t.Folder("meta", ("CanvasApps/app_1.meta.xml", Deep(100_000)))), 3),
        ["R-2 diff: canvas meta 100,000 deep"] = (t => t.Diff(t.Folder("meta", ("CanvasApps/app_1.meta.xml", Deep(100_000)))), 3),
        ["R-2 analyze: canvas meta 202 deep, one past the limit"] = (t => t.Analyze(t.Folder("meta", ("CanvasApps/app_1.meta.xml", Deep(202)))), 3),
        ["R-4 analyze: 15,000,002 XML nodes"] = (t => t.Analyze(t.Folder("nodes", ("Other/Customizations.xml", Dense(5_000_000)))), 3),
        ["R-4 diff: 15,000,002 XML nodes"] = (t => t.Diff(t.Folder("nodes", ("Other/Customizations.xml", Dense(5_000_000)))), 3),
        ["R-4 analyze: zip with 100,001 entries"] = (t => t.Analyze(t.ZipWith(100_001)), 3),
        ["R-4 diff: zip with 100,001 entries"] = (t => t.Diff(t.ZipWith(100_001)), 3),
    };

    public static TheoryData<string> Names => [.. Cases.Keys];

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    [Theory]
    [MemberData(nameof(Names))]
    public void Hostile_input_gives_its_exit_code_in_time_with_an_error_line_that_starts_with_the_path(string name)
    {
        var (build, exit) = Cases[name];
        var (args, errorPath) = build(this);
        var o = new StringWriter();
        var e = new StringWriter();

        var sw = Stopwatch.StartNew();
        var code = Program.Run(args, o, e);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
        Assert.Equal(exit, code);
        var err = e.ToString().Replace("\r", "");
        if (exit == 0)
        {
            Assert.Equal("", err);
        }
        else
        {
            Assert.StartsWith($"error: {errorPath}", err.Split('\n')[0]);
        }
    }

    private (string[] Args, string ErrorPath) Analyze((string Path, string ErrorPath) input) => (["analyze", input.Path], input.ErrorPath);

    // diff compares the hostile input with itself, so either side fails the same way and the error names the first.
    private (string[] Args, string ErrorPath) Diff((string Path, string ErrorPath) input) => (["diff", input.Path, input.Path], input.ErrorPath);

    private (string Path, string ErrorPath) Folder(string name, params (string Path, string Content)[] files)
    {
        var dir = Path.Combine(_temp, name);
        foreach (var (path, content) in files.Prepend(("Other/Solution.xml", Manifest)))
        {
            var full = Path.Combine(dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content, new UTF8Encoding(false));
        }

        return (dir, dir);
    }

    private (string Path, string ErrorPath) ZipWith(int entries)
    {
        var path = Path.Combine(_temp, "x.zip");
        using var zip = new ZipArchive(File.Create(path), ZipArchiveMode.Create);
        using (var w = new StreamWriter(zip.CreateEntry("solution.xml").Open()))
        {
            w.Write(Manifest);
        }

        for (var i = 1; i < entries; i++)
        {
            zip.CreateEntry($"f/{i}.txt");
        }

        return (path, path);
    }

    private static string Deep(int depth) => string.Concat(Enumerable.Repeat("<a>", depth)) + string.Concat(Enumerable.Repeat("</a>", depth));

    // Three nodes in eight bytes, so 15 million nodes fit under the 50 MB file cap.
    private static string Dense(int elements) => "<ImportExportXml>" + string.Concat(Enumerable.Repeat("<a>x</a>", elements)) + "</ImportExportXml>";
}