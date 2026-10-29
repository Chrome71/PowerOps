using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace PowerOps.Cli.Tests;

/// <summary>
/// M26's hostile-input theory for plan files, settings, baseline, snapshot, deploy and simulator inputs (R-7, R-8, R-10, C-2, C-10a), through the real command.
/// A limit is proven with a file one byte over it. The 100 MB zip is a sparse file; the 200,000-item Foreach is a 400 KB test file.
/// </summary>
public sealed class PlanSettingsLimitsTests : IDisposable
{
    private const string Manifest = "<ImportExportXml><SolutionManifest><UniqueName>Demo</UniqueName><Version>1.0.0.0</Version><Managed>0</Managed>"
        + "<Publisher><UniqueName>demopub</UniqueName><CustomizationPrefix>demo</CustomizationPrefix></Publisher></SolutionManifest></ImportExportXml>";

    private const string FlowWorkflow = "<Workflow WorkflowId=\"{AA-11}\" Name=\"F\"><JsonFileName>/Workflows/F.json</JsonFileName><Category>5</Category><StateCode>1</StateCode></Workflow>";

    private const long MB = 1024 * 1024;

    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-limits26-").FullName;

    // A case builds its input under a temp folder (the helpers return the test, so a case reads as a chain) and ends in the command to run.
    // Stream is where the expected line is ("err" or "out"); some line of it must start with Prefix, in which {P} is the plan folder as a path
    // (as an error prints it) and {p} the same with forward slashes (as plan status prints a path in a warning); {T} is the temp folder with a trailing separator.
    private sealed record Case(string[] Args, int Exit, string Stream, string Prefix);

    private delegate Case Build(PlanSettingsLimitsTests t);

    private const string PlanOk = "1 plan, 0 violations, 0 warnings";

    private const string SymbolicLink = "is a symbolic link; symbolic links are not read.";

    private static readonly Dictionary<string, Build> Cases = new()
    {
        // R-7: plan status and sign-off
        ["R-7 plan status: link to /dev/zero is skipped"] = t => t.Link("zero.txt", "/dev/zero").Status(0, "out", PlanOk),
        ["R-7 plan status: link loops are skipped"] = t => t.Loop().Status(0, "out", PlanOk),
        ["R-7 plan status: link to a file outside the folder is skipped"] = t => t.Link("outside.txt", t.Outside("secret.txt", "see https://contoso.example/x")).Status(0, "out", PlanOk),
        ["R-7 plan status: link to a folder outside is skipped"] = t => t.DirLink("dir", Path.GetDirectoryName(t.Outside("a.txt", "see https://contoso.example/x"))!).Status(0, "out", PlanOk),
        ["R-7 plan status: file of 1 MB and a byte is skipped with a warning"] = t => t.Pad("big.txt", MB + 1).Status(0, "out", "  warning: {p}/big.txt: larger than 1 MB, not read"),
        ["R-7 plan status: file of exactly 1 MB is scanned"] = t => t.Pad("exact.txt", MB, "http://a ").Status(1, "out", "  error: {p}/exact.txt:1: looks like a URL"),
        ["R-7 plan status: binary file under 1 MB is skipped with a warning"] = t => t.Binary("small.bin", 1000).Status(0, "out", "  warning: {p}/small.bin: a binary file, not read"),
        ["R-7 plan status: a 2 MB image passes"] = t => t.Binary("mockup.png", 2 * MB).Status(0, "out", "  warning: {p}/mockup.png: larger than 1 MB, not read"),
        ["R-7 plan status: plan.md of exactly 1 MB"] = t => t.PlanOfLength(MB).Status(0, "out", PlanOk),
        ["R-7 plan status: plan.md of 1 MB and a byte"] = t => t.PlanOfLength(MB + 1).Status(3, "err", "error: {P}" + Path.DirectorySeparatorChar + "plan.md: larger than 1 MB."),
        ["R-7 plan status: plan.md is a link to /dev/zero"] = t => t.PlanLink("/dev/zero").Status(3, "err", "error: {P}" + Path.DirectorySeparatorChar + "plan.md: " + SymbolicLink),
        ["R-7 plan status: plan.md is a link to a file outside the folder"] = t => t.PlanLink(t.Outside("plan.md", "---\nstatus: draft\nreview: pending\n---\n")).Status(3, "err", "error: {P}" + Path.DirectorySeparatorChar + "plan.md: " + SymbolicLink),
        ["R-7 plan status: review.md is a link to /dev/zero"] = t => t.Link("review.md", "/dev/zero").Status(0, "out", "  warning: {p}/review.md: a symbolic link, not read"),
        ["R-7 plan sign-off: file of 1 MB and a byte is skipped with a warning"] = t => t.Pad("big.txt", MB + 1).SignOff(0, "err", "warning: {p}/big.txt: larger than 1 MB, not read"),
        ["R-7 plan sign-off: link to /dev/zero is skipped"] = t => t.Link("zero.txt", "/dev/zero").SignOff(0, "out", "signed off "),
        ["R-7 plan sign-off: plan.md is a link to /dev/zero"] = t => t.PlanLink("/dev/zero").SignOff(3, "err", "error: {P}" + Path.DirectorySeparatorChar + "plan.md: " + SymbolicLink),
        ["R-7 plan sign-off: plan.md of 1 MB and a byte"] = t => t.PlanOfLength(MB + 1).SignOff(3, "err", "error: {P}" + Path.DirectorySeparatorChar + "plan.md: larger than 1 MB."),

        // R-10: settings, baseline, snapshot and deploy files
        ["R-10 settings validate: /dev/zero as the settings file"] = t => t.Validate("/dev/zero", 3, "err", "error: /dev/zero: larger than 1 MB."),
        ["R-10 settings validate: settings file of 1 MB and a byte"] = t => t.Validate(t.TempFile("s.json", MB + 1), 3, "err", "error: {T}s.json: larger than 1 MB."),
        ["R-10 settings validate: settings file of exactly 1 MB"] = t => t.Validate(t.TempFile("s.json", MB, "{}"), 0, "out", "no problems"),
        ["R-10 settings apply: /dev/zero as the settings file"] = t => t.Apply("/dev/zero", 3, "error: /dev/zero: larger than 1 MB."),
        ["R-10 settings apply: settings file of 1 MB and a byte"] = t => t.Apply(t.TempFile("s.json", MB + 1), 3, "error: {T}s.json: larger than 1 MB."),
        ["R-10 analyze: /dev/zero as the baseline"] = t => t.Baseline("/dev/zero", 2, "error: /dev/zero: larger than 50 MB."),
        ["R-10 analyze: baseline of 50 MB and a byte"] = t => t.Baseline(t.TempFile("b.json", 50 * MB + 1), 2, "error: {T}b.json: larger than 50 MB."),
        ["R-10 drift: /dev/zero as a snapshot"] = t => t.Drift("/dev/zero", 3, "error: --env 1: a snapshot file is at most 1048576 bytes"),
        ["R-10 drift: snapshot of 1 MB and a byte"] = t => t.Drift(t.TempFile("a.json", MB + 1), 3, "error: --env 1: a snapshot file is at most 1048576 bytes"),
        ["R-10 deploy plan: settings file of 1 MB and a byte"] = t => t.Deploy(settings: MB + 1, zip: 0, 3, "error: Solo.settings.json: the settings file is larger than 1 MB"),
        ["R-10 deploy plan: zip of 100 MB and a byte"] = t => t.Deploy(settings: 0, zip: 100 * MB + 1, 3, "error: Solo.zip: larger than 100 MB."),

        // R-8
        ["R-8 settings validate: 20,000 pairs of duplicate names"] = t => t.Validate(t.Text("s.json", "{\"EnvironmentVariables\":[" + string.Join(',', Enumerable.Range(0, 20_000).Select(i => $"{{\"SchemaName\":\"a{i}\"}},{{\"SchemaName\":\"A{i}\"}}")) + "]}"), 1, "out", "duplicate environmentVariable a19999: listed more than once"),

        // C-2 and C-10a: powerops test
        ["C-2 test: Foreach over 200,000 trigger items that read triggerBody() each time"] = t => t.Simulate(Compose("@length(triggerBody()?['items'])"), 200_000, 1, "out", "  The run made more than 100000 action calls; the simulator stops there."),
        ["C-2 test: 20,000 appends of 1,000 characters"] = t => t.Simulate(Append(new string('x', 1000)), 20_000, 1, "out", "  Action 'Step': the run's recorded and copied values pass 256 MB."),
        ["C-10a test: a 1,000,000 character expression that does not parse"] = t => t.Simulate(Compose("@(" + new string('1', 1_000_000)), 1, 1, "out", "  Action 'Step': Cannot read expression '(1111"),
    };

    public static TheoryData<string> Names => [.. Cases.Keys];

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    [Theory]
    [MemberData(nameof(Names))]
    public void Hostile_input_gives_its_exit_code_in_time_with_the_expected_path_prefixed_line(string name)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows() && (name.Contains("link", StringComparison.Ordinal) || name.Contains("/dev/zero", StringComparison.Ordinal)), "Links and /dev/zero are Unix; creating a link needs privileges on Windows.");
        var c = Cases[name](this);
        var o = new StringWriter();
        var e = new StringWriter();

        var sw = Stopwatch.StartNew();
        var code = Program.Run(c.Args, o, e, (_, _) => throw new Xunit.Sdk.XunitException("pac must not be called"), null, (_, _, _) => throw new Xunit.Sdk.XunitException("the target must not be read"));
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
        var lines = (c.Stream == "err" ? e : o).ToString().Replace("\r", "").Split('\n');
        Assert.Equal(c.Exit, code);
        var prefix = c.Prefix.Replace("{P}", PlanDir, StringComparison.Ordinal).Replace("{p}", PlanDir.Replace('\\', '/'), StringComparison.Ordinal).Replace("{T}", _temp + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        Assert.Contains(lines, l => l.StartsWith(prefix, StringComparison.Ordinal));
        Assert.All(lines, l => Assert.True(l.Length < 600, $"a line of {l.Length} characters"));
    }

    // ---- the command each case ends in -------------------------------------------------------------------------

    private Case Status(int exit, string stream, string prefix) => new(["plan", "status", _temp], exit, stream, prefix);

    private Case SignOff(int exit, string stream, string prefix) => new(["plan", "sign-off", PlanDir], exit, stream, prefix);

    private Case Validate(string settings, int exit, string stream, string prefix) => new(["settings", "validate", Solution(), "--settings", settings], exit, stream, prefix);

    private Case Apply(string settings, int exit, string prefix) => new(["settings", "apply", Zip(), "--settings", settings], exit, "err", prefix);

    private Case Baseline(string baseline, int exit, string prefix) => new(["analyze", Solution(), "--baseline", baseline], exit, "err", prefix);

    private Case Drift(string snapshot, int exit, string prefix) => new(["drift", "--solution", "Demo", "--env", snapshot, "--env", snapshot], exit, "err", prefix);

    private Case Deploy(long settings, long zip, int exit, string prefix)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_temp, "deploy")).FullName;
        var zipPath = Path.Combine(folder, "Solo.zip");
        if (zip > 0)
        {
            using var f = System.IO.File.Create(zipPath);
            f.SetLength(zip); // sparse
        }
        else
        {
            WriteZip(zipPath);
        }

        if (settings > 0)
        {
            using var f = System.IO.File.Create(Path.Combine(folder, "Solo.settings.json"));
            f.SetLength(settings);
        }

        return new(["deploy", "plan", folder, "--target", "dev"], exit, "err", prefix);
    }

    // A flow that loops over the test's trigger items and runs one action each time.
    private Case Simulate(string stepJson, int items, int exit, string stream, string prefix)
    {
        var dir = Path.Combine(_temp, "flow");
        Directory.CreateDirectory(Path.Combine(dir, "Other"));
        Directory.CreateDirectory(Path.Combine(dir, "Workflows"));
        System.IO.File.WriteAllText(Path.Combine(dir, "Other", "Solution.xml"), Manifest);
        System.IO.File.WriteAllText(Path.Combine(dir, "Workflows", "F.data.xml"), FlowWorkflow);
        System.IO.File.WriteAllText(
            Path.Combine(dir, "Workflows", "F.json"),
            "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\",\"kind\":\"Button\"}},"
            + "\"actions\":{\"Init\":{\"type\":\"InitializeVariable\",\"inputs\":{\"variables\":[{\"name\":\"s\",\"type\":\"string\",\"value\":\"\"}]},\"runAfter\":{}},"
            + "\"Loop\":{\"type\":\"Foreach\",\"foreach\":\"@triggerBody()?['items']\",\"runAfter\":{\"Init\":[\"Succeeded\"]},\"actions\":{\"Step\":" + stepJson + "}}}}}}");
        System.IO.File.WriteAllText(
            Path.Combine(dir, "t.flowtest.json"),
            "{\"flow\":\"F\",\"trigger\":{\"body\":{\"items\":[" + string.Join(',', Enumerable.Repeat("1", items)) + "]}},\"assertions\":[{\"kind\":\"runStatus\",\"equals\":\"Succeeded\"}]}");
        return new(["test", dir], exit, stream, prefix);
    }

    private static string Compose(string expression) => "{\"type\":\"Compose\",\"inputs\":" + System.Text.Json.JsonSerializer.Serialize(expression) + ",\"runAfter\":{}}";

    private static string Append(string text) => "{\"type\":\"AppendToStringVariable\",\"inputs\":{\"name\":\"s\",\"value\":\"" + text + "\"},\"runAfter\":{}}";

    // ---- the files they build ----------------------------------------------------------------------------------

    private string PlanDir => Path.Combine(_temp, "docs", "plans", "x");

    private static void WriteZip(string path)
    {
        using var zip = new ZipArchive(System.IO.File.Create(path), ZipArchiveMode.Create);
        using var w = new StreamWriter(zip.CreateEntry("solution.xml").Open());
        w.Write(Manifest);
    }

    private string Zip()
    {
        var path = Path.Combine(_temp, "sln.zip");
        WriteZip(path);
        return path;
    }

    private string Solution()
    {
        var dir = Path.Combine(_temp, "sln");
        Directory.CreateDirectory(Path.Combine(dir, "Other"));
        System.IO.File.WriteAllText(Path.Combine(dir, "Other", "Solution.xml"), Manifest);
        return dir;
    }

    // The plan folder with an approved plan whose review is skipped (ready for sign-off), unless a case wrote its own plan.md.
    private PlanSettingsLimitsTests Plan()
    {
        Directory.CreateDirectory(PlanDir);
        var plan = Path.Combine(PlanDir, "plan.md");
        if (!System.IO.File.Exists(plan) && new FileInfo(plan).LinkTarget is null)
        {
            System.IO.File.WriteAllText(plan, "---\nstatus: approved\nreview: skipped\n---\n# Plan\n");
        }

        return this;
    }

    private PlanSettingsLimitsTests PlanOfLength(long length)
    {
        Directory.CreateDirectory(PlanDir);
        const string Text = "---\nstatus: approved\nreview: skipped\n---\n# Plan\n";
        System.IO.File.WriteAllText(Path.Combine(PlanDir, "plan.md"), Text + new string(' ', (int)(length - Text.Length)));
        return this;
    }

    private PlanSettingsLimitsTests PlanLink(string target)
    {
        Directory.CreateDirectory(PlanDir);
        File.CreateSymbolicLink(Path.Combine(PlanDir, "plan.md"), target);
        return this;
    }

    private PlanSettingsLimitsTests Link(string name, string target)
    {
        Plan();
        File.CreateSymbolicLink(Path.Combine(PlanDir, name), target);
        return this;
    }

    private PlanSettingsLimitsTests DirLink(string name, string target)
    {
        Plan();
        Directory.CreateSymbolicLink(Path.Combine(PlanDir, name), target);
        return this;
    }

    // Two folder links that point at each other, and a file link to itself.
    private PlanSettingsLimitsTests Loop()
    {
        Plan();
        Directory.CreateSymbolicLink(Path.Combine(PlanDir, "a"), Path.Combine(PlanDir, "b"));
        Directory.CreateSymbolicLink(Path.Combine(PlanDir, "b"), Path.Combine(PlanDir, "a"));
        File.CreateSymbolicLink(Path.Combine(PlanDir, "self.txt"), Path.Combine(PlanDir, "self.txt"));
        return this;
    }

    // A file outside the plan folder; returns its path.
    private string Outside(string name, string content)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_temp, "outside", Guid.NewGuid().ToString("N"))).FullName;
        System.IO.File.WriteAllText(Path.Combine(dir, name), content);
        return Path.Combine(dir, name);
    }

    private string Text(string name, string content)
    {
        var path = Path.Combine(_temp, name);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    // A text file of exactly `length` bytes: `first`, then spaces. Pad puts it in the plan folder; TempFile in the temp folder and returns its path.
    private PlanSettingsLimitsTests Pad(string name, long length, string first = "")
    {
        Plan();
        System.IO.File.WriteAllText(Path.Combine(PlanDir, name), first + new string(' ', (int)(length - first.Length)), new UTF8Encoding(false));
        return this;
    }

    private string TempFile(string name, long length, string first = "")
    {
        var path = Path.Combine(_temp, name);
        System.IO.File.WriteAllText(path, first + new string(' ', (int)(length - first.Length)), new UTF8Encoding(false));
        return path;
    }

    // A file of `length` zero bytes in the plan folder, as an image has.
    private PlanSettingsLimitsTests Binary(string name, long length)
    {
        System.IO.File.WriteAllBytes(Path.Combine(Plan().PlanDir, name), new byte[length]);
        return this;
    }
}