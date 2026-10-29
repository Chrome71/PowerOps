using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

/// <summary>
/// M27's hostile-input theory (R-5, R-6, R-11, R-13, C-3, C-4, C-6), through the real command.
/// A case builds its input under a temp folder and names the exit code and a line that must start with a path.
/// </summary>
public sealed class ErrorsExitCodesTests : IDisposable
{
    private const string Manifest = "<ImportExportXml><SolutionManifest><UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>"
        + "<Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher></SolutionManifest></ImportExportXml>";

    private const string FlowWorkflow = "<Workflow WorkflowId=\"{AA-11}\" Name=\"F\"><JsonFileName>/Workflows/F.json</JsonFileName><Category>5</Category><StateCode>1</StateCode></Workflow>";

    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-errors27-").FullName;

    // Stream is where the expected line is ("err" or "out"); some line of it must start with Prefix, in which {T} is the temp folder with a trailing separator.
    private sealed record Case(string[] Args, int Exit, string Stream, string Prefix);

    private delegate Case Build(ErrorsExitCodesTests t);

    private static readonly Dictionary<string, Build> Cases = new()
    {
        // R-5
        ["R-5 analyze: baseline with a lone surrogate"] = t => new(["analyze", t.Folder("sln"), "--baseline", t.Text("b.json", "{\"version\":1,\"findings\":[{\"solution\":\"\\ud800\",\"ruleId\":\"PO001\",\"message\":\"m\"}]}")], 2, "err", "error: {T}b.json"),

        // R-6
        ["R-6 analyze: lone surrogate in a flow literal"] = t => t.Analyze(t.Flow("flow", "{\"properties\":{\"definition\":{\"actions\":{\"A\":{\"type\":\"Compose\",\"inputs\":\"\\ud800\"}}}}}"), 3),
        ["R-6 analyze: lone surrogate in an environment variable value"] = t => t.Analyze(t.Folder("env",
            ("environmentvariabledefinitions/demo_A/environmentvariabledefinition.xml", "<environmentvariabledefinition schemaname=\"demo_A\"><type>100000000</type></environmentvariabledefinition>"),
            ("environmentvariabledefinitions/demo_A/environmentvariablevalues.json", "{\"environmentvariablevalues\":{\"environmentvariablevalue\":{\"value\":\"\\ud800\"}}}")), 3),
        ["R-6 analyze: control character in a canvas app's canvasapp.yml"] = t => t.Analyze(t.Yaml("canvasapp.yml", "DisplayName: Work Items", "DisplayName: \"Work\\x01Items\""), 3),
        ["R-6 test: lone surrogate in a flow test trigger"] = t => t.Test("@1", "{\"flow\":\"F\",\"trigger\":{\"body\":{\"a\":\"\\ud800\"}},\"assertions\":[]}", 3, "err", "error: {T}compose" + Path.DirectorySeparatorChar + "t.flowtest.json"),
        ["R-6 test: json() of a lone surrogate fails the action, not the run"] = t => t.Test("@json('\"\\ud800\"')", "{\"flow\":\"F\",\"assertions\":[{\"kind\":\"runStatus\",\"equals\":\"Failed\"}]}", 0, "out", "PASS "),
        ["R-6 settings validate: lone surrogate in the settings"] = t => new(["settings", "validate", t.Folder("sln"), "--settings", t.Text("s.json", "{\"EnvironmentVariables\":[{\"SchemaName\":\"\\ud800\"}]}")], 3, "err", "error: {T}s.json"),
        ["R-6 drift: lone surrogate in a snapshot"] = t => t.Drift(),

        // R-11
        ["R-11 analyze: zip with Other/Solution.xml and Other/solution.xml"] = t => t.Analyze(t.Zip("x.zip", "solution.xml", "Other/Solution.xml", "Other/solution.xml"), 3),
        ["R-11 analyze: zip with the same entry twice"] = t => t.Analyze(t.Zip("x.zip", "solution.xml", "a.txt", "a.txt"), 3),

        // R-13
        ["R-13 analyze: flow with two definition keys"] = t => t.Analyze(t.Flow("flow", "{\"properties\":{\"definition\":{\"a\":\"https://org1.crm.dynamics.com/\"},\"definition\":{}}}"), 3),

        // C-4
        ["C-4 analyze: folder with a good solution and a truncated zip"] = t => t.Analyze(t.WithZip(Truncated()), 3, "corrupt.zip"),
        ["C-4 diff: folder with a good solution and a truncated zip"] = t => t.Diff(t.WithZip(Truncated()), 3, "corrupt.zip"),
        ["C-4 test: folder with a good solution and a truncated zip"] = t => t.SeedTest(Truncated(), 3, "err", "error: {T}repo" + Path.DirectorySeparatorChar + "corrupt.zip"),
        ["C-4 analyze: a Git LFS pointer named .zip is skipped"] = t => t.Analyze(t.WithZip(Lfs()), 0),
        ["C-4 diff: a Git LFS pointer named .zip is skipped"] = t => t.Diff(t.WithZip(Lfs()), 0),
        ["C-4 test: a Git LFS pointer named .zip is skipped"] = t => t.SeedTest(Lfs(), 0, "out", "PASS "),
        ["C-4 analyze: an empty placeholder named .zip is skipped"] = t => t.Analyze(t.WithZip([]), 0),
    };

    public static TheoryData<string> Names => [.. Cases.Keys];

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    [Theory]
    [MemberData(nameof(Names))]
    public void Hostile_input_gives_its_exit_code_with_a_line_that_starts_with_the_path(string name)
    {
        var c = Cases[name](this);
        var o = new StringWriter();
        var e = new StringWriter();

        var sw = Stopwatch.StartNew();
        var code = Program.Run(c.Args, o, e);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
        var lines = (c.Stream == "err" ? e : o).ToString().Replace("\r", "").Split('\n');
        Assert.True(c.Exit == code, $"exit {code}, expected {c.Exit}; err: {e}");
        var prefix = c.Prefix.Replace("{T}", _temp + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        if (c.Exit == 0 && c.Prefix.Length == 0)
        {
            Assert.Equal("", e.ToString());
        }
        else
        {
            Assert.True(lines.Any(l => l.StartsWith(prefix, StringComparison.Ordinal)), $"no line starts with \"{prefix}\" in {c.Stream}:\n{string.Join("\n", lines)}");
        }
    }

    [Fact]
    public void An_exception_that_escapes_a_command_exits_three_with_one_error_line()
    {
        var o = new ThrowingWriter();
        var e = new StringWriter();

        var code = Program.Run(["rules"], o, e);

        Assert.Equal(3, code);
        Assert.Equal(["error: the output is gone"], e.ToString().Replace("\r", "").TrimEnd('\n').Split('\n'));
    }

    [Theory]
    [InlineData("analyze")]
    [InlineData("diff")]
    public void A_zip_that_cannot_be_read_while_a_folder_is_scanned_exits_three_and_names_it(string command)
    {
        // Another handle on the file with FileShare.None: .NET refuses a second open on Windows, and on Unix through its own advisory lock.
        var dir = WithZip(Lfs());
        var zip = Path.Combine(dir, "corrupt.zip");
        using var hold = new FileStream(zip, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.SkipUnless(CannotBeOpened(zip), "A second open of a file held with FileShare.None succeeds here.");
        var o = new StringWriter();
        var e = new StringWriter();

        var code = Program.Run(command == "analyze" ? ["analyze", dir] : ["diff", dir, dir], o, e);

        Assert.True(code == 3, $"exit {code}; out: {o}; err: {e}");
        Assert.Contains(e.ToString().Replace("\r", "").Split('\n'), l => l.StartsWith("error: " + zip, StringComparison.Ordinal));
    }

    private static bool CannotBeOpened(string path)
    {
        try
        {
            using var f = File.OpenRead(path);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    [Fact]
    public void A_Swedish_culture_does_not_reach_the_messages()
    {
        Assert.SkipUnless(CultureAvailable("sv-SE"), "The sv-SE culture is not available here.");
        // DefaultThreadCurrentCulture is process-wide while xunit runs other classes in parallel; the window is short and Program.Run sets the invariant culture on its own thread.
        var old = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);
        try
        {
            var sv = new CultureInfo("sv-SE");
            Assert.Contains('\u2212', (-1).ToString(sv)); // the premise: this culture writes a real minus sign
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = sv;
            // A trigger condition that cannot be evaluated stops the run, and the text report prints its message.
            var dir = Flow("culture", "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\",\"kind\":\"Button\",\"conditions\":[{\"expression\":\"@equals(substring('abc',-1,1),'')\"}]}},\"actions\":{}}}}");
            File.WriteAllText(Path.Combine(dir, "t.flowtest.json"), "{\"flow\":\"F\",\"assertions\":[{\"kind\":\"runStatus\",\"equals\":\"Succeeded\"}]}");
            var o = new StringWriter();

            var code = Program.Run(["test", dir], o, new StringWriter());

            var text = o.ToString();
            Assert.Equal(1, code);
            Assert.True(text.Contains("index -1 of", StringComparison.Ordinal), text);
            Assert.DoesNotContain('\u2212', text);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture) = old;
        }
    }

    private static bool CultureAvailable(string name)
    {
        try
        {
            return !CultureInfo.GetCultureInfo(name).Equals(CultureInfo.InvariantCulture) && (-1).ToString(new CultureInfo(name)).Contains('\u2212');
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private sealed class ThrowingWriter : StringWriter
    {
        public override void Write(string? value) => throw new IOException("the output is gone");

        public override void WriteLine(string? value) => throw new IOException("the output is gone");
    }

    // ---- the command each case ends in -------------------------------------------------------------------------

    private Case Analyze(string folder, int exit, string? at = null) => new(["analyze", folder], exit, "err", exit == 0 ? "" : "error: " + (at is null ? folder : Path.Combine(folder, at)));

    private Case Diff(string folder, int exit, string? at = null) => new(["diff", folder, folder], exit, "err", exit == 0 ? "" : "error: " + (at is null ? folder : Path.Combine(folder, at)));

    // powerops test over a copy of the seed solution and its tests, plus one extra file (the zip case) in the solution folder.
    private Case SeedTest(byte[] zip, int exit, string stream, string prefix)
    {
        var repo = Path.GetDirectoryName(Copy(Path.Combine(Fixtures, "powerops-seed", "v2"), "repo/seed"))!;
        File.WriteAllBytes(Path.Combine(repo, "corrupt.zip"), zip);
        return new(["test", repo, "--tests", Path.Combine(Fixtures, "flowtests", "seed")], exit, stream, prefix);
    }

    // powerops test over a flow with one Compose action 'Step' of `expression`; the test is `testJson`, or one that expects Succeeded.
    private Case Test(string expression, string? testJson, int exit, string stream, string prefix)
    {
        var dir = Compose(expression);
        if (testJson is not null)
        {
            File.WriteAllText(Path.Combine(dir, "t.flowtest.json"), testJson);
        }

        return new(["test", dir], exit, stream, prefix);
    }

    private Case Drift()
    {
        var snapshot = File.ReadAllText(Path.Combine(Fixtures, "drift", "seed-dev.json")).Replace("pops_DefaultStatus", "\\ud800", StringComparison.Ordinal);
        var path = Text("snap.json", snapshot);
        return new(["drift", "--solution", "PowerOpsSeed", "--env", path, "--env", path], 3, "err", "error: --env 1:");
    }

    // ---- the inputs they build ---------------------------------------------------------------------------------

    private string Folder(string name, params (string Path, string Content)[] files)
    {
        var dir = Path.Combine(_temp, name);
        foreach (var (path, content) in files.Prepend(("Other/Solution.xml", Manifest)))
        {
            var full = Path.Combine(dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content, new UTF8Encoding(false));
        }

        return dir;
    }

    private string Flow(string name, string json) => Folder(name, ("Workflows/F.data.xml", FlowWorkflow), ("Workflows/F.json", json));

    // A flow with one Compose action 'Step' and a passing-shaped test; the caller replaces the test file when it needs another.
    private string Compose(string expression)
    {
        var dir = Flow("compose", "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\",\"kind\":\"Button\"}},\"actions\":{\"Step\":{\"type\":\"Compose\",\"inputs\":"
            + System.Text.Json.JsonSerializer.Serialize(expression) + ",\"runAfter\":{}}}}}}");
        File.WriteAllText(Path.Combine(dir, "t.flowtest.json"), "{\"flow\":\"F\",\"assertions\":[{\"kind\":\"runStatus\",\"equals\":\"Succeeded\"}]}");
        return dir;
    }

    private string Text(string name, string content)
    {
        var path = Path.Combine(_temp, name);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    // A zip whose first entry is named by `first` (the manifest) and whose others are empty; the same name twice is allowed when creating.
    private string Zip(string name, params string[] entries)
    {
        var path = Path.Combine(_temp, name);
        using (var zip = new ZipArchive(File.Create(path), ZipArchiveMode.Create))
        {
            foreach (var entry in entries)
            {
                using var w = new StreamWriter(zip.CreateEntry(entry).Open());
                w.Write(entry.EndsWith("olution.xml", StringComparison.OrdinalIgnoreCase) ? Manifest : "x");
            }
        }

        return path;
    }

    private string WithZip(byte[] zip)
    {
        // The solution is one level down: a folder that is itself a solution is not searched for zips.
        var dir = Path.GetDirectoryName(Folder("scan/sub"))!;
        File.WriteAllBytes(Path.Combine(dir, "corrupt.zip"), zip);
        return dir;
    }

    // A copy of the YAML seed in which `find` is replaced by `replace` in the one file named `file`.
    private string Yaml(string file, string find, string replace)
    {
        var root = Copy(Path.Combine(Fixtures, "powerops-seed-yaml", "v1"), "yaml");
        var path = Directory.GetFiles(root, file, SearchOption.AllDirectories).Single();
        var text = File.ReadAllText(path);
        Assert.Contains(find, text);
        File.WriteAllText(path, text.Replace(find, replace, StringComparison.Ordinal));
        return root;
    }

    private string Copy(string source, string name)
    {
        var target = Path.Combine(_temp, name);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }

        return target;
    }

    // The first 3,000 bytes of a fixture zip: the signature is there, the central directory is not.
    private static byte[] Truncated() => File.ReadAllBytes(Path.Combine(Fixtures, "zips", "polymorphiclookupexamplesolution.zip"))[..3000];

    private static byte[] Lfs() => Encoding.UTF8.GetBytes("version https://git-lfs.github.com/spec/v1\noid sha256:" + new string('a', 64) + "\nsize 12345\n");
}