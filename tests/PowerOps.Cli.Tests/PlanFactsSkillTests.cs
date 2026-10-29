using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The product's rule (docs/COMMANDS.md): `plan facts` runs only for an environment the user named. The skills
// must say so before they tell an agent to run it.
public partial class PlanFactsSkillTests
{
    [GeneratedRegex(@"user (has )?(names|named|confirms|confirmed)")]
    private static partial Regex Condition();

    private static string Skill(string name) =>
        File.ReadAllText(Path.Combine(Golden.RepoRoot, "skills", name, "SKILL.md"));

    // Every line that names the command has the user naming or confirming the environment in its section, at or before it.
    private static List<string> Unconditional(string text)
    {
        var bad = new List<string>();
        var section = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith("## "))
            {
                section.Clear();
            }

            section.Add(line);
            if (line.Contains("`powerops plan facts`") && !Condition().IsMatch(string.Join('\n', section)))
            {
                bad.Add(line);
            }
        }

        return bad;
    }

    [Theory]
    [InlineData("powerops-plan")]
    [InlineData("powerops-review-plan")]
    public void Plan_facts_is_run_only_for_an_environment_the_user_named(string skill)
    {
        Assert.Empty(Unconditional(Skill(skill)));
    }

    [Fact]
    public void The_fast_path_reads_no_environment_facts()
    {
        var text = Skill("powerops-plan");
        var start = text.IndexOf("## 8. Fast path", StringComparison.Ordinal);
        var end = text.IndexOf("## 9.", start, StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start);
        Assert.DoesNotContain("plan facts", text[start..end]);
    }

    [Fact]
    public void The_check_fails_on_the_old_wording()
    {
        Assert.NotEmpty(Unconditional("## 3. Environment facts\n\nRun `powerops plan facts` once.\n"));
        Assert.Empty(Unconditional("## 3. Environment facts\n\nAsk which one.\nRun `powerops plan facts` once, after the user named it.\n"));
    }
}