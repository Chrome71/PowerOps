using PowerOps.Core.Deploy;
using PowerOps.Core.Plans;
using PowerOps.Core.Readers;
using PowerOps.Core.Tests.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Deploy;

public class DeployReviewsTests : IDisposable
{
    private readonly TempFolder _t = new();
    private readonly TempFolder _zips = new();

    public void Dispose()
    {
        _t.Dispose();
        _zips.Dispose();
    }

    // ---- Problem: the rule (row 2) ----

    private static readonly string Zip = new('c', 64);
    private static readonly string Other = new('d', 64);

    private static PlanReportFile R(string file, string? state = "ready", bool? stale = false, string? hash = null, string input = "solutions/seed") =>
        new(file, state, stale, input, hash ?? Zip);

    private static PlanReportFile V(string? state = "ready", bool? stale = false, string? hash = null) => R("verify.md", state, stale, hash);

    private static PlanReportFile S(string? state = "ready", bool? stale = false, string? hash = null) => R("security-solution.md", state, stale, hash);

    private const string NoneRecords = "no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its solution-hash; a report written for an unpacked folder never does";

    [Fact]
    public void Both_reports_of_one_folder_ready_current_and_recording_the_zip_cover_it() =>
        Assert.Null(DeployReviews.Problem([new("a", V(), S())], Zip));

    public static TheoryData<PlanReportFile?, PlanReportFile?, string> SingleFailures => new()
    {
        { V(), null, "security-solution.md is missing" },
        { V("blocked"), S(), "verify.md is blocked" },
        { V(null), S(), "verify.md has no state" },
        { V(), S(hash: Other), "security-solution.md records another solution-hash" },
        { V(stale: true), S(), "verify.md was not written for the current plan" },
        { V(), S(stale: null), "security-solution.md was not written for the current plan" },
    };

    [Theory]
    [MemberData(nameof(SingleFailures))]
    public void Each_failure_of_the_first_recording_folder_is_named(PlanReportFile? verify, PlanReportFile? security, string failure) =>
        Assert.Equal($"docs/plans/a: {failure}", DeployReviews.Problem([new("a", verify, security)], Zip));

    [Fact]
    public void Two_failures_are_joined_and_only_the_first_failure_of_a_report_is_named() =>
        Assert.Equal(
            "docs/plans/a: verify.md is blocked; security-solution.md records another solution-hash",
            DeployReviews.Problem([new("a", V("blocked", stale: null), S("failed", hash: Other))], Zip));

    // A rule that pools the reports of all folders would take a's verify.md and b's security-solution.md as a cover.
    [Fact]
    public void Reports_split_across_two_folders_do_not_cover_the_zip() =>
        Assert.Equal("docs/plans/a: security-solution.md is missing", DeployReviews.Problem([new("a", V(), null), new("b", null, S())], Zip));

    [Fact]
    public void A_later_folder_that_covers_the_zip_wins_over_an_earlier_one_that_does_not() =>
        Assert.Null(DeployReviews.Problem([new("a", V("blocked"), S()), new("b", V(), S())], Zip));

    [Fact]
    public void No_report_recording_the_zip_gives_the_first_sentence()
    {
        Assert.Equal(NoneRecords, DeployReviews.Problem([new("a", V(hash: Other), S(hash: Other))], Zip));
        Assert.Equal(NoneRecords, DeployReviews.Problem([], Zip));
        Assert.False(DeployReviews.Recorded([new("a", V(hash: Other), S(hash: Other))], Zip));
        Assert.True(DeployReviews.Recorded([new("a", null, S("blocked"))], Zip));
    }

    // ---- Evidence: the notes (row 3) ----

    private static readonly string Seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1");

    private const string Tail = "; not compared by diff: security roles, tables, columns, forms, views, app modules and their role maps; this is evidence, not a link";

    private static string Note(string folders, int changes) =>
        $"{folders}: reviewed 'solutions/seed' (digest current); diff against the zip: {changes} change{(changes == 1 ? "" : "s")} besides the managed flag{Tail}";

    private static string Count(int reports) =>
        $"{reports} ready report{(reports == 1 ? "" : "s")} under docs/plans reviewed this solution, each in an input that changed since the review; none was compared with the zip";

    private static void Copy(string from, string to)
    {
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    // The seed copied to solutions/seed under the root, and its digest.
    private string SeedFolder()
    {
        var folder = Path.Combine(_t.Path, "solutions", "seed");
        if (!Directory.Exists(folder))
        {
            Copy(Seed, folder);
        }

        return Manifest.Read(folder).Digest;
    }

    // A managed zip of the seed's files, with one more flow when asked.
    private DeployZip SeedZip(string name = "Seed", bool extraFlow = false)
    {
        var dir = Path.Combine(_zips.Path, name);
        Copy(Seed, dir);
        var xml = Path.Combine(dir, "Other", "Solution.xml");
        File.WriteAllText(xml, File.ReadAllText(xml).Replace("<Managed>0</Managed>", "<Managed>1</Managed>", StringComparison.Ordinal));
        if (extraFlow)
        {
            const string Flow = "Stampworkitemstatus-4C8DD007-DFCB-F111-A05C-0022480697A8";
            const string Second = "Second-11111111-2222-3333-4444-555555555555";
            var flows = Path.Combine(dir, "Workflows");
            File.Copy(Path.Combine(flows, Flow + ".json"), Path.Combine(flows, Second + ".json"));
            File.WriteAllText(
                Path.Combine(flows, Second + ".json.data.xml"),
                File.ReadAllText(Path.Combine(flows, Flow + ".json.data.xml"))
                    .Replace(Flow, Second, StringComparison.Ordinal)
                    .Replace("4c8dd007-dfcb-f111-a05c-0022480697a8", "11111111-2222-3333-4444-555555555555", StringComparison.Ordinal));
        }

        var zip = Path.Combine(_zips.Path, name + ".zip");
        SolutionZip.Create(dir, zip);
        return new DeployZip(name + ".zip", DeployPlan.Sha256Hex(File.ReadAllBytes(zip)), SolutionLoader.Load(zip), null, null, null);
    }

    private static PlanReviews Both(string plan, string hash, string input = "solutions/seed", string state = "ready") =>
        new(plan, R("verify.md", state, hash: hash, input: input), R("security-solution.md", state, hash: hash, input: input));

    private IReadOnlyList<string> Notes(IReadOnlyList<PlanReviews> plans, DeployZip zip) =>
        DeployReviews.Evidence(_t.Path, plans, [zip]).GetValueOrDefault(zip.File) ?? [];

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void A_current_review_of_the_seed_folder_gives_one_note_with_the_changes_besides_the_managed_flag(bool extraFlow, int changes)
    {
        var digest = SeedFolder();
        var notes = Notes([Both("a", digest)], SeedZip(extraFlow: extraFlow));

        Assert.Equal([Note("docs/plans/a", changes)], notes);
        Assert.DoesNotContain(notes, n => n.Contains(_t.Path, StringComparison.Ordinal) || n.Contains(_zips.Path, StringComparison.Ordinal));
    }

    [Fact]
    public void Current_reviews_of_one_input_in_several_folders_share_one_note()
    {
        var digest = SeedFolder();
        var zip = SeedZip();

        Assert.Equal([Note("docs/plans/a, docs/plans/b", 0)], Notes([Both("a", digest), Both("b", digest)], zip));
        Assert.Equal(
            [Note("docs/plans/a, docs/plans/b, docs/plans/c and 2 more", 0)],
            Notes([.. new[] { "e", "d", "c", "b", "a" }.Select(p => Both(p, digest))], zip));
    }

    [Fact]
    public void An_input_edited_since_the_review_gives_one_count_line()
    {
        var digest = SeedFolder();
        var zip = SeedZip();
        File.AppendAllText(Path.Combine(_t.Path, "solutions", "seed", "Other", "Relationships.xml"), "\n");

        Assert.Equal([Count(1)], Notes([new("a", R("verify.md", hash: digest), null)], zip));
        Assert.Equal([Count(3)], Notes([new("a", R("verify.md", hash: digest), null), new("b", null, R("security-solution.md", hash: digest)), new("c", R("verify.md", hash: digest), R("security-solution.md", "blocked", hash: digest))], zip));
    }

    [Fact]
    public void A_current_review_beside_stale_ones_gives_the_note_and_no_count_line()
    {
        var digest = SeedFolder();

        Assert.Equal([Note("docs/plans/b", 0)], Notes([Both("a", Other), Both("b", digest), Both("c", Other)], SeedZip()));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("link")]
    [InlineData("unloadable")]
    [InlineData("other solution")]
    [InlineData("blocked")]
    public void An_input_that_cannot_give_a_note_gives_nothing_and_throws_nothing(string kind)
    {
        Assert.SkipWhen(kind == "link" && OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        var digest = SeedFolder();
        var input = "solutions/seed";
        var state = "ready";
        switch (kind)
        {
            case "missing":
                input = "solutions/missing";
                break;
            case "link":
                Directory.CreateSymbolicLink(Path.Combine(_t.Path, "solutions", "link"), Path.Combine(_t.Path, "solutions", "seed"));
                input = "solutions/link";
                break;
            case "unloadable":
                _t.Write("solutions/bad/Other/Solution.xml", "not xml");
                (input, digest) = ("solutions/bad", Manifest.Read(Path.Combine(_t.Path, "solutions", "bad")).Digest);
                break;
            case "other solution":
                _t.Write("solutions/other/Other/Solution.xml", SolutionXml.Manifest(uniqueName: "Other"));
                (input, digest) = ("solutions/other", Manifest.Read(Path.Combine(_t.Path, "solutions", "other")).Digest);
                break;
            default:
                state = "blocked";
                break;
        }

        Assert.Empty(Notes([Both("a", digest, input, state)], SeedZip()));
    }

    [Fact]
    public void A_zip_that_a_report_records_gives_nothing_and_loads_nothing()
    {
        SeedFolder();
        var zip = SeedZip();
        var loads = 0;

        var evidence = DeployReviews.Evidence(_t.Path, [Both("a", zip.Sha256)], [zip], p => { loads++; return SolutionLoader.Load(p); });

        Assert.Empty(evidence.GetValueOrDefault(zip.File) ?? []);
        Assert.Equal(0, loads);
    }

    [Fact]
    public void Each_distinct_input_is_loaded_once_per_run()
    {
        var digest = SeedFolder();
        var (one, two) = (SeedZip("One"), SeedZip("Two", extraFlow: true));
        var loads = 0;

        var evidence = DeployReviews.Evidence(_t.Path, [Both("a", digest), Both("b", digest), Both("c", digest)], [one, two], p => { loads++; return SolutionLoader.Load(p); });

        Assert.Equal(1, loads);
        Assert.Equal([Note("docs/plans/a, docs/plans/b, docs/plans/c", 0)], evidence[one.File]);
        Assert.Equal([Note("docs/plans/a, docs/plans/b, docs/plans/c", 1)], evidence[two.File]);
    }
}