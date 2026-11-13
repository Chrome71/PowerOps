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

    private const string NoneRecords = "no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its deploy-hash or its solution-hash";

    [Fact]
    public void Both_reports_of_one_folder_ready_current_and_recording_the_zip_cover_it() =>
        Assert.Null(DeployReviews.Problem([new("a", V(), S(), null)], Zip));

    public static TheoryData<PlanReportFile?, PlanReportFile?, string> SingleFailures => new()
    {
        { V(), null, "security-solution.md is missing" },
        { V("blocked"), S(), "verify.md is blocked" },
        { V(null), S(), "verify.md has no state" },
        { V(), S(hash: Other), "security-solution.md records another deploy-hash and solution-hash" },
        { V(stale: true), S(), "verify.md was not written for the current plan" },
        { V(), S(stale: null), "security-solution.md was not written for the current plan" },
    };

    [Theory]
    [MemberData(nameof(SingleFailures))]
    public void Each_failure_of_the_first_recording_folder_is_named(PlanReportFile? verify, PlanReportFile? security, string failure) =>
        Assert.Equal($"docs/plans/a: {failure}", DeployReviews.Problem([new("a", verify, security, null)], Zip));

    [Fact]
    public void Two_failures_are_joined_and_only_the_first_failure_of_a_report_is_named() =>
        Assert.Equal(
            "docs/plans/a: verify.md is blocked; security-solution.md records another deploy-hash and solution-hash",
            DeployReviews.Problem([new("a", V("blocked", stale: null), S("failed", hash: Other), null)], Zip));

    // A rule that pools the reports of all folders would take a's verify.md and b's security-solution.md as a cover.
    [Fact]
    public void Reports_split_across_two_folders_do_not_cover_the_zip() =>
        Assert.Equal("docs/plans/a: security-solution.md is missing", DeployReviews.Problem([new("a", V(), null, null), new("b", null, S(), null)], Zip));

    [Fact]
    public void A_later_folder_that_covers_the_zip_wins_over_an_earlier_one_that_does_not() =>
        Assert.Null(DeployReviews.Problem([new("a", V("blocked"), S(), null), new("b", V(), S(), null)], Zip));

    [Fact]
    public void No_report_recording_the_zip_gives_the_first_sentence()
    {
        Assert.Equal(NoneRecords, DeployReviews.Problem([new("a", V(hash: Other), S(hash: Other), null)], Zip));
        Assert.Equal(NoneRecords, DeployReviews.Problem([], Zip));
        Assert.False(DeployReviews.Recorded([new("a", V(hash: Other), S(hash: Other), null)], Zip));
        Assert.True(DeployReviews.Recorded([new("a", null, S("blocked"), null)], Zip));
    }

    // ---- Problem and Cover: the deploy-hash link (M83 row 2) ----

    private static readonly string Unpacked = new('e', 64);

    private static readonly ZipUnpack Same = new(Unpacked, null);

    // A report of the zip's unpacked folder: the zip's SHA-256 as its deploy-hash, the folder's digest as its solution-hash.
    private static PlanReportFile D(string file, string? state = "ready", bool? stale = false, string? folder = null, string? deploy = null) =>
        new(file, state, stale, "review/seed", folder ?? Unpacked, DeployHash: deploy ?? Zip);

    private static PlanReportFile DV(string? state = "ready", bool? stale = false, string? folder = null, string? deploy = null) => D("verify.md", state, stale, folder, deploy);

    private static PlanReportFile DS(string? state = "ready", bool? stale = false, string? folder = null, string? deploy = null) => D("security-solution.md", state, stale, folder, deploy);

    [Fact]
    public void Both_reports_linked_by_deploy_hash_with_the_unpacks_digest_cover_the_zip()
    {
        PlanReviews[] plans = [new("a", DV(), DS(), null)];

        Assert.Null(DeployReviews.Problem(plans, Zip, Same));
        Assert.Equal("a", DeployReviews.Cover(plans, Zip, Same)?.Plan);
    }

    public static TheoryData<PlanReportFile?, PlanReportFile?, ZipUnpack?, string> DeployHashFailures => new()
    {
        { DV(), null, Same, "security-solution.md is missing" },
        { DV(deploy: Other), DS(), Same, "verify.md records another deploy-hash and solution-hash" },
        { DV(), DS(), null, "verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: no unpack was given; security-solution.md records this zip as its deploy-hash, but the zip's unpack could not be checked: no unpack was given" },
        { DV(), S(), new(null, "pac was not found"), "verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: pac was not found" },
        { DV(), S(), new(Other, null), "verify.md's solution-hash is not the digest of this zip's unpack, so it reviewed another folder" },
        { DV("blocked", folder: Other), S(), Same, "verify.md's solution-hash is not the digest of this zip's unpack, so it reviewed another folder" },
        { DV("blocked"), S(), Same, "verify.md is blocked" },
        { DV(null, stale: true), S(), Same, "verify.md has no state" },
        { DV(), DS(stale: true), Same, "security-solution.md was not written for the current plan" },
    };

    [Theory]
    [MemberData(nameof(DeployHashFailures))]
    public void Each_failure_of_a_deploy_hash_link_is_named_in_order(PlanReportFile? verify, PlanReportFile? security, ZipUnpack? unpack, string failure)
    {
        PlanReviews[] plans = [new("a", verify, security, null)];

        Assert.Equal($"docs/plans/a: {failure}", DeployReviews.Problem(plans, Zip, unpack));
        Assert.Null(DeployReviews.Cover(plans, Zip, unpack));
    }

    [Fact]
    public void M75s_direct_link_covers_with_no_unpack_and_with_a_failed_one()
    {
        Assert.Null(DeployReviews.Problem([new("a", V(), S(), null)], Zip));
        Assert.Null(DeployReviews.Problem([new("a", V() with { DeployHash = Zip }, S(), null)], Zip, new(null, "pac was not found")));
        Assert.Equal("a", DeployReviews.Cover([new("a", V(), S(), null)], Zip, null)?.Plan);
    }

    [Fact]
    public void A_verify_md_linked_by_deploy_hash_and_a_security_solution_md_linked_directly_in_one_folder_cover() =>
        Assert.Null(DeployReviews.Problem([new("a", DV(), S(), null)], Zip, Same));

    [Fact]
    public void Reports_of_two_folders_linked_either_way_do_not_cover_the_zip() =>
        Assert.Equal("docs/plans/a: security-solution.md is missing", DeployReviews.Problem([new("a", DV(), null, null), new("b", null, S(), null)], Zip, Same));

    [Fact]
    public void The_first_folder_named_counts_a_deploy_hash_and_the_first_covering_folder_is_ordinal()
    {
        var different = new ZipUnpack(Other, null);

        Assert.Equal(
            "docs/plans/b: verify.md's solution-hash is not the digest of this zip's unpack, so it reviewed another folder; security-solution.md's solution-hash is not the digest of this zip's unpack, so it reviewed another folder",
            DeployReviews.Problem([new("c", V(hash: Other), S(hash: Other), null), new("b", DV(), DS(), null)], Zip, different));
        Assert.Equal("a", DeployReviews.Cover([new("b", V(), S(), null), new("a", DV(), DS(), null)], Zip, Same)?.Plan);
    }

    [Fact]
    public void Needs_unpack_only_when_a_solution_report_records_the_zip_as_its_deploy_hash()
    {
        Assert.True(DeployReviews.NeedsUnpack([new("a", null, DS("blocked"), null)], Zip));
        Assert.False(DeployReviews.NeedsUnpack([new("a", V(), S(), null)], Zip));
        Assert.False(DeployReviews.NeedsUnpack([new("a", DV(deploy: Other), DS(deploy: Other), null)], Zip));
        Assert.False(DeployReviews.NeedsUnpack([], Zip));
    }

    [Fact]
    public void Recorded_still_counts_only_a_solution_hash() =>
        Assert.False(DeployReviews.Recorded([new("a", DV(), DS(), null)], Zip));

    // ---- Problem and Cover: the plan's sign-off and open High findings (M114 rows 3 and 5) ----

    public static TheoryData<string> NoSignOffReasons => new() { "could not be read", "malformed header", "status draft", "status approved", "signed without the tool", "changed since sign-off" };

    [Theory]
    [MemberData(nameof(NoSignOffReasons))]
    public void A_plan_with_no_valid_sign_off_is_named_first_alone_and_before_a_report_fault(string reason)
    {
        Assert.Equal($"docs/plans/a: plan.md has no valid sign-off ({reason})", DeployReviews.Problem([new("a", V(), S(), reason)], Zip));
        Assert.Equal($"docs/plans/a: plan.md has no valid sign-off ({reason}); verify.md is blocked", DeployReviews.Problem([new("a", V("blocked"), S(), reason)], Zip));
        Assert.Null(DeployReviews.Cover([new("a", V(), S(), reason)], Zip, null));
    }

    [Fact]
    public void A_ready_report_that_lists_an_open_high_is_no_cover()
    {
        Assert.Equal("docs/plans/a: verify.md is ready but lists 1 open High finding", DeployReviews.Problem([new("a", V() with { OpenHigh = 1 }, S(), null)], Zip));
        Assert.Equal(
            "docs/plans/a: plan.md has no valid sign-off (status draft); verify.md is ready but lists 1 open High finding; security-solution.md is ready but lists 2 open High findings",
            DeployReviews.Problem([new("a", V() with { OpenHigh = 1 }, S() with { OpenHigh = 2 }, "status draft")], Zip));
        Assert.Null(DeployReviews.Cover([new("a", V(), S() with { OpenHigh = 1 }, null)], Zip, null));
    }

    [Fact]
    public void A_stale_report_with_an_open_high_gives_the_stale_sentence() =>
        Assert.Equal("docs/plans/a: verify.md was not written for the current plan", DeployReviews.Problem([new("a", V(stale: true) with { OpenHigh = 1 }, S(), null)], Zip));

    [Fact]
    public void A_later_valid_folder_covers_when_the_first_has_no_valid_sign_off()
    {
        PlanReviews[] plans = [new("a", V(), S(), "status draft"), new("b", V(), S(), null)];

        Assert.Null(DeployReviews.Problem(plans, Zip));
        Assert.Equal("b", DeployReviews.Cover(plans, Zip, null)?.Plan);
    }

    [Fact]
    public void A_deploy_hash_link_under_an_unsigned_plan_with_a_matching_unpack_is_refused_with_the_plans_sentence()
    {
        PlanReviews[] plans = [new("a", DV(), DS(), "status approved")];

        Assert.True(DeployReviews.NeedsUnpack(plans, Zip));
        Assert.Equal("docs/plans/a: plan.md has no valid sign-off (status approved)", DeployReviews.Problem(plans, Zip, Same));
        Assert.Null(DeployReviews.Cover(plans, Zip, Same));
    }

    // Row 5: an edited body is changed since sign-off and leaves both reports stale; a re-sign of the edited body leaves only the stale reports.
    [Fact]
    public void An_edited_body_gives_all_three_clauses_and_a_re_signed_one_gives_the_stale_reports_alone()
    {
        static string PlanText(string hash) => $"---\nstatus: signed-off\nreview: skipped\nsigned-off: 2026-01-01\nsigned-off-hash: {hash}\nenvironment: null\n---\n# Plan, edited\n";
        var old = PlanDocument.Parse("---\nstatus: draft\nreview: skipped\n---\n# Plan\n").BodyHash!;
        var edited = PlanDocument.Parse(PlanText(old)).BodyHash!;
        var report = $"state: ready\nplan-hash: {old}\ninput: solutions/seed\nsolution-hash: {Zip}\n\n# Report\n";
        _t.Write("docs/plans/m/plan.md", PlanText(old)).Write("docs/plans/m/verify.md", report).Write("docs/plans/m/security-solution.md", report);
        const string Stale = "verify.md was not written for the current plan; security-solution.md was not written for the current plan";

        Assert.Equal($"docs/plans/m: plan.md has no valid sign-off (changed since sign-off); {Stale}", DeployReviews.Problem(PlanChecker.Reviews(_t.Path)!, Zip));

        _t.Write("docs/plans/m/plan.md", PlanText(edited));
        Assert.Equal($"docs/plans/m: {Stale}", DeployReviews.Problem(PlanChecker.Reviews(_t.Path)!, Zip));
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
        new(plan, R("verify.md", state, hash: hash, input: input), R("security-solution.md", state, hash: hash, input: input), null);

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

        Assert.Equal([Count(1)], Notes([new("a", R("verify.md", hash: digest), null, null)], zip));
        Assert.Equal([Count(3)], Notes([new("a", R("verify.md", hash: digest), null, null), new("b", null, R("security-solution.md", hash: digest), null), new("c", R("verify.md", hash: digest), R("security-solution.md", "blocked", hash: digest), null)], zip));
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

    // M83: a deploy-hash link that failed is no record, so the person still gets the notes.
    [Fact]
    public void A_zip_whose_only_record_is_a_deploy_hash_still_gets_the_notes()
    {
        var digest = SeedFolder();
        var zip = SeedZip();
        var plans = new[] { new PlanReviews("a", R("verify.md", hash: digest) with { DeployHash = zip.Sha256 }, R("security-solution.md", hash: digest) with { DeployHash = zip.Sha256 }, null) };

        Assert.Equal([Note("docs/plans/a", 0)], Notes(plans, zip));
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