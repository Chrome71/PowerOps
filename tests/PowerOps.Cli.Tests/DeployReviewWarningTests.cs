using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// deploy plan's review lines (M75 row 4): a warning for each zip the plan imports that no plan folder's two reports cover, and the evidence notes.
public class DeployReviewWarningTests : DeployTestBase
{
    private const string NoneRecords = "no verify.md or security-solution.md under docs/plans records this zip's SHA-256 as its solution-hash; a report written for an unpacked folder never does";

    private const string Tail = "; not compared by diff: security roles, tables, columns, forms, views, app modules and their role maps; this is evidence, not a link";

    private Outcome Plan(string folder, FakeTarget? target = null) => Run(target ?? new FakeTarget(), new FakePac(), "deploy", "plan", folder, "--target", TargetValue);

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
}