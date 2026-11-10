using System.Text.Json.Nodes;

using PowerOps.Core.Deploy;
using PowerOps.Core.Drift;
using PowerOps.Core.Plans;
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
        /// <summary>The imports only; a <c>solution unpack</c> goes to <see cref="Unpacks"/>, so it never shifts an import's number.</summary>
        public List<string[]> Calls { get; } = [];

        public List<TimeSpan> Timeouts { get; } = [];

        public List<string[]> Unpacks { get; } = [];

        public List<TimeSpan> UnpackTimeouts { get; } = [];

        /// <summary>The folder a <c>solution unpack</c> copies into its <c>--folder</c>; null writes no file.</summary>
        public string? UnpackFrom { get; init; }

        /// <summary>Answers an unpack, given its timeout, instead of the copy; null means the copy and exit 0.</summary>
        public Func<TimeSpan, PacResult?>? UnpackAnswer { get; init; }

        /// <summary>Stdout and stderr of every unpack answer, which must never be printed.</summary>
        public const string UnpackOutput = "PAC-UNPACK-OUTPUT";

        /// <summary>The temp folders the unpacks were given (the parent of each <c>--folder</c>).</summary>
        public IEnumerable<string> UnpackTemps => Unpacks.Select(a => Path.GetDirectoryName(a[Array.IndexOf(a, "--folder") + 1])!);

        /// <summary>Answers import number n (from 0); null means success.</summary>
        public Func<int, PacResult?>? Answer { get; init; }

        /// <summary>Runs while import number n is in flight, before pac answers.</summary>
        public Action<int>? During { get; init; }

        public PacResult Run(IReadOnlyList<string> args, TimeSpan timeout)
        {
            if (args.Take(2).SequenceEqual(["solution", "unpack"]))
            {
                Unpacks.Add([.. args]);
                UnpackTimeouts.Add(timeout);
                var folder = args[args.ToList().IndexOf("--folder") + 1];
                if (UnpackAnswer?.Invoke(timeout) is { } answer)
                {
                    return answer;
                }

                foreach (var file in UnpackFrom is null ? [] : Directory.EnumerateFiles(UnpackFrom, "*", SearchOption.AllDirectories))
                {
                    var to = Path.Combine(folder, Path.GetRelativePath(UnpackFrom!, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                    File.Copy(file, to);
                }

                return new PacResult(true, false, 0, $"{UnpackOutput} {folder}\n", UnpackOutput);
            }

            Assert.Equal(["solution", "import"], args.Take(2));
            Calls.Add([.. args]);
            Timeouts.Add(timeout);
            var n = Calls.Count - 1;
            During?.Invoke(n);
            return Answer?.Invoke(n) ?? new PacResult(true, false, 0, "imported\n");
        }
    }

    /// <summary>The agent read-back: records each call and answers what <see cref="Answer"/> gives (no rows by default). Every run passes one, so no test reaches the real reader.</summary>
    protected sealed class FakeAgents
    {
        public List<(string Target, Guid EnvironmentId, string[] Names)> Calls { get; } = [];

        public Func<IReadOnlyList<string>, IReadOnlyList<(string SchemaName, bool PublishedOn)>> Answer { get; init; } = _ => [];

        public IReadOnlyList<(string SchemaName, bool PublishedOn)> Read(string target, Guid environmentId, IReadOnlyList<string> names)
        {
            Calls.Add((target, environmentId, [.. names]));
            return Answer(names);
        }
    }

    /// <summary>The agent read-back through a real <see cref="DriftLiveReader"/> over its own fake pac (it answers only <c>org who</c>, with <see cref="TargetIdText"/>), az and web. <see cref="Web"/> answers each bot read; no row by default.</summary>
    internal sealed class LiveAgents
    {
        public List<Uri> WebCalls { get; } = [];

        public Func<Uri, WebResult> Web { get; init; } = _ => new WebResult(200, """{"value":[]}""", false);

        public IReadOnlyList<(string SchemaName, bool PublishedOn)> Read(string target, Guid environmentId, IReadOnlyList<string> names) =>
            new DriftLiveReader(
                (args, _) =>
                {
                    Assert.Equal(["org", "who"], args.Take(2));
                    return new PacResult(true, false, 0, File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "org-who.txt")).Replace("00000000-0000-0000-0000-00000000000c", TargetIdText));
                },
                (_, _) => new PacResult(true, false, 0, "fake-token\n"),
                (url, _, _) =>
                {
                    WebCalls.Add(url);
                    return Web(url);
                }).ReadAgents(target, environmentId, names);
    }

    protected sealed record Outcome(int Code, string Out, string Err);

    protected static Outcome Run(FakeTarget fake, FakePac pac, params string[] args) => RunWith(fake, pac, new FakeAgents().Read, args);

    protected static Outcome RunWith(FakeTarget fake, FakePac pac, Func<string, Guid, IReadOnlyList<string>, IReadOnlyList<(string SchemaName, bool PublishedOn)>> agents, params string[] args)
    {
        var (o, e) = (new StringWriter(), new StringWriter());
        var code = Program.Run(args, o, e, pac.Run, null, fake.Read, deployAgents: agents);
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

    // The made agent fixture (solution PowerOpsAgentTools, agent demo_Helper) and the settings file its plan needs.
    protected static void AgentZip(string folder)
    {
        SolutionZip.Create(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "agents", "dataverse-tools"), Path.Combine(folder, "PowerOpsAgentTools.zip"));
        File.WriteAllText(
            Path.Combine(folder, "PowerOpsAgentTools.settings.json"),
            """{"ConnectionReferences":[{"LogicalName":"demo_Helper.shared_commondataserviceforapps.shared-commondataser-0000001","ConnectionId":"fake-connection-id","ConnectorId":"/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"}]}""");
    }

    protected static FakeTarget SeedTarget() => new() { Snaps = { ["PowerOpsSeed"] = SnapshotFile.Load(TestSnapshot) } };

    // ---- review reports ----------------------------------------------------------------------------------------

    protected const string ReviewedPlanText = "---\nstatus: signed-off\nreview: done\nsigned-off: 2026-01-01\nsigned-off-hash: null\nenvironment: null\n---\n# Plan\n";

    /// <summary>
    /// Writes <c>docs/plans/&lt;plan&gt;/plan.md</c> under <paramref name="repo"/> (the test's root by default, which the walk up from <see cref="Folder"/> reaches)
    /// and both review reports for <paramref name="input"/>, a zip or an unpacked folder under the repo: <c>state: ready</c>, the plan's hash,
    /// the input's path relative to the repo, and its digest (a zip's SHA-256) as the solution-hash. Returns the plan folder.
    /// </summary>
    protected string Reviewed(string input, string? repo = null, string plan = "m", string? deployZip = null)
    {
        repo ??= _root;
        var folder = Directory.CreateDirectory(Path.Combine(repo, "docs", "plans", plan)).FullName;
        File.WriteAllText(Path.Combine(folder, "plan.md"), ReviewedPlanText);
        var deploy = deployZip is null ? "" : $"deploy-hash: {Core.Readers.Manifest.Read(deployZip).Digest}\n";
        var head = $"state: ready\nplan-hash: {PlanDocument.Parse(ReviewedPlanText).BodyHash}\ninput: {Path.GetRelativePath(repo, input).Replace('\\', '/')}\nsolution-hash: {Core.Readers.Manifest.Read(input).Digest}\n{deploy}\n# Report\n";
        File.WriteAllText(Path.Combine(folder, "verify.md"), head);
        File.WriteAllText(Path.Combine(folder, "security-solution.md"), head);
        return folder;
    }

    /// <summary>
    /// The zip's files at <c>review/&lt;zip name&gt;</c> under <paramref name="repo"/>, as a pac unpack of it leaves them, and both reports of that folder
    /// with the zip's SHA-256 as their deploy-hash (M83). Returns the folder, which a <see cref="FakePac"/> with it as <see cref="FakePac.UnpackFrom"/> unpacks again.
    /// </summary>
    protected string ReviewedUnpack(string zip, string? repo = null, string plan = "m")
    {
        repo ??= _root;
        var review = Path.Combine(repo, "review", Path.GetFileNameWithoutExtension(zip));
        System.IO.Compression.ZipFile.ExtractToDirectory(zip, review);
        Reviewed(review, repo, plan, zip);
        return review;
    }

    /// <summary>Every unpack's temp folder is gone, and no line of the run names it or holds pac's output.</summary>
    protected static void UnpacksGone(FakePac pac, Outcome r)
    {
        Assert.All(pac.UnpackTemps, t =>
        {
            Assert.StartsWith("powerops-unpack-", Path.GetFileName(t));
            Assert.False(Directory.Exists(t), t);
            Assert.DoesNotContain(t, r.Out + r.Err);
        });
        Assert.DoesNotContain(FakePac.UnpackOutput, r.Out + r.Err);
    }

    /// <summary>Replaces <paramref name="from"/> with <paramref name="to"/> in a file the test wrote.</summary>
    protected static void Edit(string file, string from, string to) => File.WriteAllText(file, File.ReadAllText(file).Replace(from, to, StringComparison.Ordinal));

    // A chain A <- B <- C of managed zips (imported in that order), no settings files.
    protected void Chain()
    {
        SmallZip(Folder, "A");
        SmallZip(Folder, "B", requires: ["A (1.0)"]);
        SmallZip(Folder, "C", requires: ["B (1.0)"]);
    }
}