using System.Text.Json.Nodes;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The fakes and the folder helpers of the `deploy apply` and `deploy gate` tests: a fake target read, a fake pac, and plans made with `deploy plan --save`.
// The types are public because the test classes that derive from it are.
public abstract class DeployTestBase : IDisposable
{
    protected const string TargetValue = "https://secret-host-7f3a.crm.dynamics.com/";
    protected const string TargetIdText = "aaaaaaaa-0000-0000-0000-000000000001";
    protected static readonly Guid TargetId = Guid.Parse(TargetIdText);

    protected static readonly string Seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1");
    protected static readonly string ValidSettings = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "settings", "powerops-seed-v1.valid.json");
    protected static readonly string TestSnapshot = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "drift", "seed-test.json");

    protected readonly string _root = Directory.CreateTempSubdirectory("powerops-apply-").FullName;

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    protected string Folder => Dir("folder");

    protected string Stage => Dir("stage");

    protected string Out => Dir("out");

    protected string PlanFile => Path.Combine(Folder, "powerops-plan.json");

    protected string Scratch(string prefix) => Dir($"{prefix}-{Guid.NewGuid():N}");

    protected string Dir(string name) => Directory.CreateDirectory(Path.Combine(_root, name)).FullName;

    // ---- fakes -------------------------------------------------------------------------------------------------

    protected sealed class FakeTarget
    {
        public Dictionary<string, Snapshot> Snaps { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>What the second and later reads return; null for the same as the first.</summary>
        public Dictionary<string, Snapshot>? After { get; init; }

        public Dictionary<string, string?> Deps { get; init; } = [];

        public Exception? Fail { get; init; }

        public Exception? FailAfter { get; init; }

        public string Name { get; init; } = "Contoso Test";

        public Guid Id { get; init; } = TargetId;

        public int Calls { get; private set; }

        public List<string[]> Solutions { get; } = [];

        public DeployTarget Read(string target, IReadOnlyList<string> solutions, IReadOnlyList<string> dependencies)
        {
            Calls++;
            Solutions.Add([target, .. solutions]);
            if (Fail is not null || (Calls > 1 && FailAfter is not null))
            {
                throw (Calls > 1 ? FailAfter : null) ?? Fail!;
            }

            var snaps = Calls > 1 && After is not null ? After : Snaps;
            return new DeployTarget(
                Name, Id,
                [.. solutions.Select(s => snaps.TryGetValue(s, out var snap) ? snap : new Snapshot(Name, s, false, null, null, true, [], [], [], []))],
                dependencies.ToDictionary(d => d, d => Deps.GetValueOrDefault(d)));
        }
    }

    protected sealed class FakePac
    {
        public List<string[]> Calls { get; } = [];

        public List<TimeSpan> Timeouts { get; } = [];

        /// <summary>Answers import number n (from 0); null means success.</summary>
        public Func<int, PacResult?>? Answer { get; init; }

        /// <summary>Runs while import number n is in flight, before pac answers.</summary>
        public Action<int>? During { get; init; }

        public PacResult Run(IReadOnlyList<string> args, TimeSpan timeout)
        {
            Assert.Equal(["solution", "import"], args.Take(2));
            Calls.Add([.. args]);
            Timeouts.Add(timeout);
            var n = Calls.Count - 1;
            During?.Invoke(n);
            return Answer?.Invoke(n) ?? new PacResult(true, false, 0, "imported\n");
        }
    }

    protected sealed record Outcome(int Code, string Out, string Err);

    protected static Outcome Run(FakeTarget fake, FakePac pac, params string[] args)
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        var code = Program.Run(args, o, e, pac.Run, null, fake.Read);
        return new Outcome(code, o.ToString().ReplaceLineEndings("\n"), e.ToString().ReplaceLineEndings("\n"));
    }

    // Writes the plan file with `deploy plan --save`, as the action does, beside the zips.
    protected string MakePlan(FakeTarget? fake = null)
    {
        var r = Run(fake ?? SeedTarget(), new FakePac(), "deploy", "plan", Folder, "--target", TargetValue, "--save", PlanFile);
        Assert.True(r.Code is 0 or 1, r.Err);
        return PlanFile;
    }

    // ---- inputs ------------------------------------------------------------------------------------------------

    protected static string Manifest(string name, string version, bool managed, params string[] requires) =>
        $"""
        <ImportExportXml><SolutionManifest>
          <UniqueName>{name}</UniqueName><Version>{version}</Version><Managed>{(managed ? 1 : 0)}</Managed>
          <Publisher><UniqueName>pub</UniqueName><CustomizationPrefix>pub</CustomizationPrefix></Publisher>
          <MissingDependencies>{string.Concat(requires.Select(r => $"<MissingDependency><Required type=\"1\" schemaName=\"x\" solution=\"{r}\" /><Dependent type=\"29\" id=\"{{AA}}\" /></MissingDependency>"))}</MissingDependencies>
        </SolutionManifest></ImportExportXml>
        """;

    protected string SmallZip(string folder, string name, string version = "1.0.0.0", bool managed = true, string[]? requires = null, string? unique = null, params (string Path, string Text)[] files)
    {
        var dir = Scratch("small");
        Directory.CreateDirectory(Path.Combine(dir, "Other"));
        File.WriteAllText(Path.Combine(dir, "Other", "Solution.xml"), Manifest(unique ?? name, version, managed, requires ?? []));
        foreach (var (path, text) in files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dir, path))!);
            File.WriteAllText(Path.Combine(dir, path), text);
        }

        var zip = Path.Combine(folder, name + ".zip");
        SolutionZip.Create(dir, zip);
        return zip;
    }

    protected void SeedZip(string folder, bool settings = true, string? settingsText = null)
    {
        var dir = Path.Combine(Scratch("seed"), "v1");
        foreach (var f in Directory.EnumerateFiles(Seed, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(dir, Path.GetRelativePath(Seed, f));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(f, to);
        }

        var xml = Path.Combine(dir, "Other", "Solution.xml");
        File.WriteAllText(xml, File.ReadAllText(xml).Replace("<Managed>0</Managed>", "<Managed>1</Managed>").Replace("<Version>1.0.0.0</Version>", "<Version>1.0.0.1</Version>"));
        SolutionZip.Create(dir, Path.Combine(folder, "PowerOpsSeed.zip"));
        if (settings)
        {
            File.WriteAllText(Path.Combine(folder, "PowerOpsSeed.settings.json"), settingsText ?? File.ReadAllText(ValidSettings));
        }
    }

    protected static FakeTarget SeedTarget() => new() { Snaps = { ["PowerOpsSeed"] = SnapshotFile.Load(TestSnapshot) } };

    // A chain A <- B <- C of managed zips (imported in that order), no settings files.
    protected void Chain()
    {
        SmallZip(Folder, "A");
        SmallZip(Folder, "B", requires: ["A (1.0)"]);
        SmallZip(Folder, "C", requires: ["B (1.0)"]);
    }
}