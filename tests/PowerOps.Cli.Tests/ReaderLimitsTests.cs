using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace PowerOps.Cli.Tests;

/// <summary>
/// M25's hostile-input theory: each audit probe (R-1 to R-4) and an input one past a cap, through the real command.
/// Only the node and zip-entry caps can be passed at their real size here (100,001 files or folders is slow to build);
/// the file and folder caps are proven with small injected caps in PowerOps.Core.Tests.
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
        ["R-1 analyze: flow 240 deep, 100-character keys, 40,000 leaves"] = (t => t.Analyze(t.Folder("flow", ("Workflows/F.data.xml", FlowWorkflow), ("Workflows/F.json", DeepFlow(40_000)))), 0),
        ["R-2 analyze: canvas meta 100,000 deep"] = (t => t.Analyze(t.Folder("meta", ("CanvasApps/app_1.meta.xml", Deep(100_000)))), 3),
        ["R-2 diff: canvas meta 100,000 deep"] = (t => t.Diff(t.Folder("meta", ("CanvasApps/app_1.meta.xml", Deep(100_000)))), 3),
        ["R-2 analyze: canvas meta 202 deep, one past the limit"] = (t => t.Analyze(t.Folder("meta", ("CanvasApps/app_1.meta.xml", Deep(202)))), 3),
        ["R-3 analyze: environment variable default of 80,003 characters"] = (t => t.Analyze(t.Folder("env", ("environmentvariabledefinitions/demo_A/environmentvariabledefinition.xml", EnvVar("a@" + string.Concat(Enumerable.Repeat(".a", 40_000)) + "@")))), 0),
        ["R-4 analyze: 5,000,002 XML nodes"] = (t => t.Analyze(t.Folder("nodes", ("Other/Customizations.xml", Flat(5_000_000)))), 3),
        ["R-4 diff: 5,000,002 XML nodes"] = (t => t.Diff(t.Folder("nodes", ("Other/Customizations.xml", Flat(5_000_000)))), 3),
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

    private const string FlowWorkflow = "<Workflow WorkflowId=\"{AA-11}\" Name=\"F\"><JsonFileName>/Workflows/F.json</JsonFileName><Category>5</Category><StateCode>1</StateCode></Workflow>";

    private static string DeepFlow(int leaves)
    {
        var json = new StringBuilder("{\"properties\":{\"definition\":");
        for (var i = 0; i < 240; i++)
        {
            json.Append("{\"").Append('k', 100).Append("\":");
        }

        json.Append('[').Append(string.Join(',', Enumerable.Repeat("\"a\"", leaves))).Append(']').Append('}', 240).Append("}}");
        return json.ToString();
    }

    private static string Deep(int depth) => string.Concat(Enumerable.Repeat("<a>", depth)) + string.Concat(Enumerable.Repeat("</a>", depth));

    private static string Flat(int elements) => "<ImportExportXml>" + string.Concat(Enumerable.Repeat("<a/>", elements)) + "</ImportExportXml>";

    private static string EnvVar(string defaultValue) =>
        $"<environmentvariabledefinition schemaname=\"demo_A\"><type>100000000</type><defaultvalue>{defaultValue}</defaultvalue></environmentvariabledefinition>";
}