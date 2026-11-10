using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// deploy plan's review lines (M75 row 4): a warning for each zip the plan imports that no plan folder's two reports cover, and the evidence notes.
public class DeployReviewWarningTests : DeployTestBase
{
    private const string NoneRecords = "no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its deploy-hash or its solution-hash";

    private const string Tail = "; not compared by diff: security roles, tables, columns, forms, views, app modules and their role maps; this is evidence, not a link";

    private Outcome Plan(string folder, FakeTarget? target = null, FakePac? pac = null) => Run(target ?? new FakeTarget(), pac ?? new FakePac(), "deploy", "plan", folder, "--target", TargetValue);

    // A deploy folder at <repo>/<deploy> with Solo.zip in it.
    private (string Repo, string Folder, string Zip) Solo(string repoName, string deploy = "deploy")
    {
        var repo = Dir(repoName);
        var folder = Directory.CreateDirectory(Path.Combine(repo, deploy)).FullName;
        return (repo, folder, SmallZip(folder, "Solo"));
    }

    [Theory]
    [InlineData("deploy")]
    [InlineData("a/b/deploy")]
    public void Both_reports_of_one_plan_folder_covering_the_zip_give_no_line(string deploy)
    {
        var (repo, folder, zip) = Solo("repo", deploy);
        Reviewed(zip, repo);

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Theory]
    [InlineData("only-verify", "docs/plans/m: security-solution.md is missing")]
    [InlineData("security-blocked", "docs/plans/m: security-solution.md is blocked")]
    [InlineData("stale", "docs/plans/m: verify.md was not written for the current plan; security-solution.md was not written for the current plan")]
    [InlineData("another-hash", NoneRecords)]
    public void A_report_that_does_not_cover_the_zip_gives_one_warning_and_the_same_plan(string kind, string sentence)
    {
        var (coveredRepo, coveredFolder, coveredZip) = Solo("covered");
        Reviewed(coveredZip, coveredRepo);
        var covered = Plan(coveredFolder);
        var (repo, folder, zip) = Solo(kind);
        var plan = Reviewed(zip, repo);
        switch (kind)
        {
            case "only-verify":
                File.Delete(Path.Combine(plan, "security-solution.md"));
                break;
            case "security-blocked":
                Edit(Path.Combine(plan, "security-solution.md"), "state: ready", "state: blocked");
                break;
            case "stale":
                Edit(Path.Combine(plan, "plan.md"), "# Plan", "# Plan, edited");
                break;
            default:
                // Not recorded: the reviewed input (the zip itself) no longer has the recorded digest, so the count line follows.
                var hash = Core.Readers.Manifest.Read(zip).Digest;
                Edit(Path.Combine(plan, "verify.md"), hash, new string('a', 64));
                Edit(Path.Combine(plan, "security-solution.md"), hash, new string('a', 64));
                break;
        }

        var r = Plan(folder);

        var count = kind == "another-hash" ? "note: Solo.zip: 2 ready reports under docs/plans reviewed this solution, each in an input that changed since the review; none was compared with the zip\n" : "";
        Assert.Equal($"warning: Solo.zip: {sentence}\n{count}", r.Err);
        Assert.Equal(covered.Out.Replace(coveredFolder, "F"), r.Out.Replace(folder, "F"));
        Assert.Equal(covered.Code, r.Code);
        Assert.Equal(0, r.Code);
    }

    private static void Copy(string from, string to)
    {
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    // The seed unpacked at <repo>/solutions/seed and a managed zip of the same files (one more flow when asked) in <repo>/deploy.
    private (string Repo, string Folder, string Unpacked) SeedPair(bool extraFlow = false)
    {
        var repo = Dir("seed-repo");
        var unpacked = Path.Combine(repo, "solutions", "seed");
        Copy(Seed, unpacked);
        var dir = Scratch("managed");
        Copy(Seed, dir);
        Edit(Path.Combine(dir, "Other", "Solution.xml"), "<Managed>0</Managed>", "<Managed>1</Managed>");
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

        var folder = Directory.CreateDirectory(Path.Combine(repo, "deploy")).FullName;
        SolutionZip.Create(dir, Path.Combine(folder, "PowerOpsSeed.zip"));
        File.Copy(ValidSettings, Path.Combine(folder, "PowerOpsSeed.settings.json"));
        return (repo, folder, unpacked);
    }

    [Theory]
    [InlineData(false, "0 changes")]
    [InlineData(true, "1 change")]
    public void A_ready_review_of_the_unpacked_folder_gives_the_warning_and_a_note_with_what_diff_finds(bool extraFlow, string changes)
    {
        var (repo, folder, unpacked) = SeedPair(extraFlow);
        Reviewed(unpacked, repo);

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal(
            $"warning: PowerOpsSeed.zip: {NoneRecords}\nnote: PowerOpsSeed.zip: docs/plans/m: reviewed 'solutions/seed' (digest current); diff against the zip: {changes} besides the managed flag{Tail}\n",
            r.Err);
        Assert.DoesNotContain(_root, r.Err);
    }

    [Fact]
    public void A_folder_edited_since_its_review_gives_the_warning_and_the_count_line()
    {
        var (repo, folder, unpacked) = SeedPair();
        Reviewed(unpacked, repo);
        File.AppendAllText(Path.Combine(unpacked, "Other", "Relationships.xml"), "\n");

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal(
            $"warning: PowerOpsSeed.zip: {NoneRecords}\nnote: PowerOpsSeed.zip: 2 ready reports under docs/plans reviewed this solution, each in an input that changed since the review; none was compared with the zip\n",
            r.Err);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("link")]
    [InlineData("outside")]
    public void An_input_that_is_missing_linked_or_outside_the_repository_gives_the_warning_only(string kind)
    {
        Assert.SkipWhen(kind == "link" && OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        var (repo, folder, unpacked) = SeedPair();
        var plan = Reviewed(unpacked, repo);
        var input = kind switch
        {
            "missing" => "solutions/gone",
            "link" => "solutions/link",
            _ => "../seed-repo/solutions/seed",
        };
        if (kind == "link")
        {
            Directory.CreateSymbolicLink(Path.Combine(repo, "solutions", "link"), unpacked);
        }

        Edit(Path.Combine(plan, "verify.md"), "input: solutions/seed", "input: " + input);
        Edit(Path.Combine(plan, "security-solution.md"), "input: solutions/seed", "input: " + input);

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal($"warning: PowerOpsSeed.zip: {NoneRecords}\n", r.Err);
    }

    [Fact]
    public void A_zip_the_plan_skips_gives_no_line()
    {
        var repo = Dir("skips");
        var folder = Directory.CreateDirectory(Path.Combine(repo, "deploy")).FullName;
        SmallZip(folder, "Kept");
        var solo = SmallZip(folder, "Solo");
        Reviewed(solo, repo);
        var target = new FakeTarget { Snaps = { ["Kept"] = new("Contoso Test", "Kept", true, "1.0.0.0", true, true, [], [], [], []) } };

        var r = Plan(folder, target);

        Assert.Equal(0, r.Code);
        Assert.Contains("Kept", r.Out);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void No_docs_plans_at_or_above_the_deploy_folder_prints_nothing()
    {
        // The .git folder ends the walk, so a docs/plans elsewhere above the temp folder can't change this test.
        Directory.CreateDirectory(Path.Combine(Dir("none"), ".git"));
        var (_, folder, _) = Solo("none", "a/deploy");

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void A_docs_plans_above_the_repositorys_git_folder_is_not_read()
    {
        // outer/docs/plans would warn (it covers no zip); outer/repo/.git stops the walk before it.
        var outer = Dir("outer");
        Directory.CreateDirectory(Path.Combine(outer, "docs", "plans", "x"));
        File.WriteAllText(Path.Combine(outer, "docs", "plans", "x", "plan.md"), ReviewedPlanText);
        Directory.CreateDirectory(Path.Combine(outer, "repo", ".git"));
        var (_, folder, _) = Solo("outer", "repo/deploy");

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void A_docs_folder_that_is_a_link_gives_one_warning_and_the_plan()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        var elsewhere = Directory.CreateDirectory(Path.Combine(Dir("elsewhere"), "plans", "x")).FullName;
        File.WriteAllText(Path.Combine(elsewhere, "plan.md"), ReviewedPlanText);
        Directory.CreateSymbolicLink(Path.Combine(Dir("linked"), "docs"), Dir("elsewhere"));
        var (_, folder, _) = Solo("linked");

        var r = Plan(folder);

        Assert.Equal(0, r.Code);
        Assert.Equal("warning: docs/plans could not be read, so the zips were not matched with review reports\n", r.Err);
    }

    // ---- the deploy-hash link (M83 row 3) ----

    private const string Linked = "docs/plans/m: both reports record this zip's SHA-256 as their deploy-hash, and the digest of its pac solution unpack is their solution-hash";

    private const string OtherFolder = "'s solution-hash is not the digest of this zip's unpack, so it reviewed another folder";

    private static string Unchecked(string why) =>
        $"docs/plans/m: verify.md records this zip as its deploy-hash, but the zip's unpack could not be checked: {why}; security-solution.md records this zip as its deploy-hash, but the zip's unpack could not be checked: {why}";

    [Theory]
    [InlineData(true, "Managed")]
    [InlineData(false, "Unmanaged")]
    public void A_review_of_the_zips_unpack_links_it_with_a_note_and_no_warning(bool managed, string type)
    {
        var repo = Dir("repo");
        var folder = Directory.CreateDirectory(Path.Combine(repo, "deploy")).FullName;
        var zip = SmallZip(folder, "Solo", managed: managed);
        var pac = new FakePac { UnpackFrom = ReviewedUnpack(zip, repo) };

        var r = Plan(folder, pac: pac);

        Assert.Equal(0, r.Code);
        Assert.Equal($"note: Solo.zip: {Linked}\n", r.Err);
        var args = Assert.Single(pac.Unpacks);
        var temp = Assert.Single(pac.UnpackTemps);
        Assert.Equal(["solution", "unpack", "--zipfile", zip, "--folder", Path.Combine(temp, "solution"), "--packagetype", type], args);
        Assert.Equal(TimeSpan.FromSeconds(120), Assert.Single(pac.UnpackTimeouts));
        Assert.Empty(pac.Calls);
        UnpacksGone(pac, r);
    }

    [Fact]
    public void A_verify_md_linked_by_deploy_hash_beside_a_security_solution_md_linked_directly_covers_with_no_note()
    {
        var (repo, folder, zip) = Solo("mixed");
        var review = ReviewedUnpack(zip, repo);
        // security-solution.md reviewed the zip itself, so it links directly.
        File.WriteAllText(
            Path.Combine(repo, "docs", "plans", "m", "security-solution.md"),
            $"state: ready\nplan-hash: {Core.Plans.PlanDocument.Parse(ReviewedPlanText).BodyHash}\ninput: deploy/Solo.zip\nsolution-hash: {Core.Readers.Manifest.Read(zip).Digest}\n\n# Report\n");
        var pac = new FakePac { UnpackFrom = review };

        var r = Plan(folder, pac: pac);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
        Assert.Single(pac.Unpacks);
        UnpacksGone(pac, r);
    }

    [Fact]
    public void A_report_with_no_deploy_hash_runs_no_unpack()
    {
        var (repo, folder, zip) = Solo("direct");
        Reviewed(zip, repo);
        var pac = new FakePac();

        var r = Plan(folder, pac: pac);

        Assert.Equal(("", 0), (r.Err, r.Code));
        Assert.Empty(pac.Unpacks);
    }

    [Fact]
    public void A_changed_zip_whose_sha_256_no_report_records_warns_and_runs_no_unpack()
    {
        var (repo, folder, zip) = Solo("changed-zip");
        var pac = new FakePac { UnpackFrom = ReviewedUnpack(zip, repo) };
        File.Delete(zip);
        SmallZip(folder, "Solo", version: "1.0.0.1");

        var r = Plan(folder, pac: pac);

        Assert.Equal(0, r.Code);
        Assert.StartsWith($"warning: Solo.zip: {NoneRecords}\n", r.Err);
        Assert.Empty(pac.Unpacks);
    }

    [Theory]
    [InlineData("folder")]
    [InlineData("roles")]
    public void An_unpack_with_another_digest_warns(string kind)
    {
        var (repo, folder, zip) = Solo(kind);
        var review = ReviewedUnpack(zip, repo);
        var unpacked = Scratch("unpacked");
        Copy(review, unpacked);
        if (kind == "folder")
        {
            // The reviewed folder differs from the zip's unpack in a file both have.
            var xml = Directory.EnumerateFiles(unpacked, "*.xml", SearchOption.AllDirectories).Order(StringComparer.Ordinal).First();
            var text = File.ReadAllText(xml);
            File.Delete(xml);
            File.WriteAllText(xml, text + "\n<!-- edited -->");
        }
        else
        {
            // A security role that the unpack has and the reviewed folder doesn't, which diff doesn't compare.
            Directory.CreateDirectory(Path.Combine(unpacked, "Roles"));
            File.WriteAllText(Path.Combine(unpacked, "Roles", "Admin.xml"), "<Role name=\"Admin\" />");
        }

        var pac = new FakePac { UnpackFrom = unpacked };

        var r = Plan(folder, pac: pac);

        Assert.Equal(0, r.Code);
        Assert.StartsWith($"warning: Solo.zip: docs/plans/m: verify.md{OtherFolder}; security-solution.md{OtherFolder}\n", r.Err);
        Assert.DoesNotContain(Linked, r.Err);
        UnpacksGone(pac, r);
    }

    public static TheoryData<string, string> UnpackFailures => new()
    {
        { "not-found", "pac was not found" },
        { "timeout", "pac solution unpack timed out" },
        { "exit", "pac solution unpack exited 3" },
        { "empty", "the unpacked folder could not be read" },
    };

    [Theory]
    [MemberData(nameof(UnpackFailures))]
    public void An_unpack_that_fails_warns_with_its_sentence(string kind, string why)
    {
        var (repo, folder, zip) = Solo(kind);
        var review = ReviewedUnpack(zip, repo);
        var pac = new FakePac
        {
            UnpackFrom = kind == "empty" ? null : review,
            UnpackAnswer = _ => kind switch
            {
                "not-found" => new PacResult(false, false, 0, ""),
                "timeout" => new PacResult(true, true, 0, ""),
                "exit" => new PacResult(true, false, 3, FakePac.UnpackOutput, FakePac.UnpackOutput),
                _ => null,
            },
        };

        var r = Plan(folder, pac: pac);

        Assert.Equal(0, r.Code);
        Assert.Equal($"warning: Solo.zip: {Unchecked(why)}\n", r.Err);
        Assert.Single(pac.Unpacks);
        UnpacksGone(pac, r);
    }

    // Runs the real pac on a managed sample zip: the reports hash the folder of pac's own unpack, so the check's unpack must give the same digest.
    [Fact]
    public void A_review_of_a_real_pac_unpack_links_the_zip()
    {
        Assert.SkipWhen(Tools.Missing(["pac"], Environment.GetEnvironmentVariable("PATH")) is not null, "pac is not on PATH; this test runs where pac is installed.");
        var repo = Dir("real");
        var folder = Directory.CreateDirectory(Path.Combine(repo, "deploy")).FullName;
        var zip = Path.Combine(folder, "ColumnLevelSecuritySampleSolution_managed.zip");
        File.Copy(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "zips", "ColumnLevelSecuritySampleSolution_managed.zip"), zip);
        var review = Path.Combine(repo, "review", "ColumnLevelSecuritySampleSolution_managed");
        var unpack = new PacRunner(timeout: TimeSpan.FromSeconds(120)).Run(["solution", "unpack", "--zipfile", zip, "--folder", review, "--packagetype", "Managed"]);
        Assert.True(unpack is { Started: true, TimedOut: false, ExitCode: 0 }, unpack.Stdout + unpack.Stderr);
        Reviewed(review, repo, "m", zip);
        var target = new FakeTarget();

        var (o, e) = (new StringWriter(), new StringWriter());
        var code = Program.Run(["deploy", "plan", folder, "--target", TargetValue], o, e, (args, timeout) => new PacRunner(timeout: timeout).Run(args), null, target.Read, deployAgents: new FakeAgents().Read);

        Assert.True(code is 0 or 1, e.ToString());
        Assert.Equal($"note: ColumnLevelSecuritySampleSolution_managed.zip: {Linked}\n", e.ToString().ReplaceLineEndings("\n"));
    }
}