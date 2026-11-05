using System.Security.Cryptography;

namespace PowerOps.Cli.Tests;

public class DeployReviewWarningTests : DeployTestBase
{
    private const string PlanText = "---\nstatus: draft\nreview: pending\nsigned-off: null\nsigned-off-hash: null\nenvironment: null\n---\n# Plan\n";

    private const string Warning = "warning: Solo.zip: no ready verify.md or security-solution.md under docs/plans has this zip's SHA-256 as its solution-hash\n";

    // Plans a deploy folder at <repo>/<deploy> (the repo holds one report, when report is not null) with a zip of the given content.
    private Outcome PlanIn(string repoName, string deploy, string? report, string state = "ready", string? hash = null)
    {
        var repo = Dir(repoName);
        var folder = Directory.CreateDirectory(Path.Combine(repo, deploy)).FullName;
        var zip = SmallZip(folder, "Solo");
        if (report is not null)
        {
            Directory.CreateDirectory(Path.Combine(repo, "docs", "plans", "x"));
            File.WriteAllText(Path.Combine(repo, "docs", "plans", "x", "plan.md"), PlanText);
            File.WriteAllText(Path.Combine(repo, "docs", "plans", "x", report), $"state: {state}\nsolution-hash: {hash ?? Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(zip)))}\n");
        }

        return Run(new FakeTarget(), new FakePac(), "deploy", "plan", folder, "--target", TargetValue);
    }

    [Theory]
    [InlineData("verify.md", "deploy")]
    [InlineData("security-solution.md", "deploy")]
    [InlineData("verify.md", "a/b/deploy")]
    public void A_ready_report_with_the_zips_hash_gives_no_warning(string report, string deploy)
    {
        var r = PlanIn("repo", deploy, report);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void A_blocked_report_or_another_hash_gives_one_warning_and_the_same_plan()
    {
        var matched = PlanIn("matched", "deploy", "verify.md");
        var blocked = PlanIn("blocked", "deploy", "verify.md", state: "blocked");
        var other = PlanIn("other", "a/b/deploy", "security-solution.md", hash: new string('a', 64));

        Assert.Equal("", matched.Err);
        Assert.Equal(Warning, blocked.Err);
        Assert.Equal(Warning, other.Err);
        Assert.Equal(matched.Out.Replace(Dir("matched"), "R"), blocked.Out.Replace(Dir("blocked"), "R"));
        Assert.Equal(matched.Code, blocked.Code);
        Assert.Equal(matched.Code, other.Code);
    }

    [Fact]
    public void No_docs_plans_at_or_above_the_deploy_folder_prints_nothing()
    {
        // The .git folder ends the walk, so a docs/plans elsewhere above the temp folder can't change this test.
        Directory.CreateDirectory(Path.Combine(Dir("none"), ".git"));
        var r = PlanIn("none", "a/deploy", null);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void A_docs_plans_above_the_repositorys_git_folder_is_not_read()
    {
        // outer/docs/plans would warn (its report names another hash); outer/repo/.git stops the walk before it.
        PlanIn("outer", "repo/deploy", "verify.md", hash: new string('a', 64));
        Directory.CreateDirectory(Path.Combine(Dir("outer"), "repo", ".git"));

        var r = Run(new FakeTarget(), new FakePac(), "deploy", "plan", Path.Combine(Dir("outer"), "repo", "deploy"), "--target", TargetValue);

        Assert.Equal(0, r.Code);
        Assert.Equal("", r.Err);
    }

    [Fact]
    public void A_docs_folder_that_is_a_link_gives_one_warning_and_the_plan()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "symbolic links need a privilege on Windows");
        var elsewhere = Directory.CreateDirectory(Path.Combine(Dir("elsewhere"), "plans", "x")).FullName;
        File.WriteAllText(Path.Combine(elsewhere, "plan.md"), PlanText);
        Directory.CreateSymbolicLink(Path.Combine(Dir("linked"), "docs"), Dir("elsewhere"));

        var r = PlanIn("linked", "deploy", null);

        Assert.Equal(0, r.Code);
        Assert.Equal("warning: docs/plans could not be read, so the zips were not matched with review reports\n", r.Err);
    }
}