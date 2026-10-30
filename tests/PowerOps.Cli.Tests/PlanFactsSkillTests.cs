using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// The product's rule (docs/COMMANDS.md): `plan facts` runs only for an environment the user named. The skills
// must say so before they tell an agent to run it.
public partial class PlanFactsSkillTests
{
    // A line that runs the command carries its own condition, on that same line.
    [GeneratedRegex(@"\bonly (after|if|when) (the user|your request)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Condition();

    [GeneratedRegex(@"\b(do not|never) run `powerops plan facts`", RegexOptions.IgnoreCase)]
    private static partial Regex Prohibition();

    // A phrase that turns the condition around ("before asking", "before the user names it") or drops it ("now").
    [GeneratedRegex(@"\bbefore (ask|the user)|\bnow\b", RegexOptions.IgnoreCase)]
    private static partial Regex Reversal();

    [GeneratedRegex(@"\bread no environment facts\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReadsNoFacts();

    [GeneratedRegex(@"\breads? (the )?environment facts\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReadsFacts();

    private static string Skill(string name) =>
        File.ReadAllText(Path.Combine(Golden.RepoRoot, "skills", name, "SKILL.md"));

    // Every line that names the command is a prohibition of it, or says on that line that the user named or confirmed the environment first, and reverses neither.
    private static List<string> Unconditional(string text) =>
        text.Split('\n')
            .Where(line => line.Contains("`powerops plan facts`") && (Reversal().IsMatch(line) || !(Prohibition().IsMatch(line) || Condition().IsMatch(line))))
            .ToList();

    private static string FastPath(string text)
    {
        var start = text.IndexOf("## 8. Fast path", StringComparison.Ordinal);
        var end = text.IndexOf("## 9.", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return text[start..end];
    }

    private static bool ReadsNoEnvironmentFacts(string section) =>
        !section.Contains("plan facts") && ReadsNoFacts().IsMatch(section) && !ReadsFacts().IsMatch(section);

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
        Assert.True(ReadsNoEnvironmentFacts(FastPath(Skill("powerops-plan"))));
    }

    [Fact]
    public void The_fast_path_check_fails_on_a_path_that_reads_facts()
    {
        var text = FastPath(Skill("powerops-plan"));

        Assert.False(ReadsNoEnvironmentFacts(text.Replace("read no environment facts", "read environment facts")));
        Assert.False(ReadsNoEnvironmentFacts(text.Replace("read no environment facts", "read the environment facts")));
        Assert.False(ReadsNoEnvironmentFacts(text.Replace("read no environment facts", "run `powerops plan facts`")));
        Assert.False(ReadsNoEnvironmentFacts(text.Replace("read no environment facts", "carry on")));
    }

    [Fact]
    public void The_check_fails_on_the_old_wording()
    {
        Assert.NotEmpty(Unconditional("## 3. Environment facts\n\nRun `powerops plan facts` once.\n"));
        Assert.Empty(Unconditional("## 3. Environment facts\n\nAsk which one.\nRun `powerops plan facts` once, and only after the user named it.\n"));
        Assert.Empty(Unconditional("## 3. Environment facts\n\nWithout that, do not run `powerops plan facts`.\n"));
    }

    [Theory]
    [InlineData("Run `powerops plan facts` now, before asking any environment question.")]
    [InlineData("Run `powerops plan facts` only before the user names the environment.")]
    [InlineData("Run `powerops plan facts` once.")]
    [InlineData("Run `powerops plan facts` only after the user named it, now.")]
    public void The_check_fails_on_a_removed_or_reversed_condition_even_after_an_earlier_condition(string line)
    {
        Assert.NotEmpty(Unconditional($"## 3. Environment facts\n\nAsk which one. Only after the user has named it, go on.\n{line}\n"));
    }
}
