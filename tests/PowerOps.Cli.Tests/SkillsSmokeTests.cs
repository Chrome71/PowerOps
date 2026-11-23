using System.Diagnostics;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public sealed class SkillsSmokeTests
{
    [Fact]
    public async Task Smoke_script_parses_with_bash()
    {
        var (code, output) = await Run("-n");
        Assert.True(code == 0, output);
    }

    [Theory]
    [InlineData("other", "standard", "", "unknown agent")]
    [InlineData("claude", "missing", "", "unknown scenario in SMOKE_ONLY")]
    [InlineData("codex", "standard", "1", "SMOKE_MAX_USD works with claude only")]
    [InlineData("claude", "standard", "abc", "SMOKE_MAX_USD must be a number")]
    public async Task Invalid_arguments_exit_with_usage_error(string agent, string only, string cap, string error)
    {
        var (code, output) = await Run(agent, only, cap);
        Assert.Equal(2, code);
        Assert.Contains(error, output);
    }

    [Theory]
    [InlineData("claude", "quick standard")]
    [InlineData("codex", "standard")]
    public async Task Sketch_conversations_finish_with_receipts(string agent, string only)
    {
        var (code, output) = await Run(agent, only);
        Assert.True(code == 0, output);
        Assert.Contains("all checks passed", output);
        Assert.Matches(@"(?m)^total time: [0-9]+\.[0-9] minutes$", output);
        Assert.Contains("time [standard]", output);
        if (agent == "claude")
        {
            Assert.Contains("time [quick]", output);
            Assert.Contains("total cost: USD 0.0000", output);
        }
    }

    [Fact]
    public async Task Standard_rejects_a_judge_file_after_a_skip()
    {
        var (code, output) = await Run("claude", mutant: "judge");
        Assert.Equal(1, code);
        Assert.Contains("FAIL [standard]: mockups/prototype.judge.md", output);
    }

    [Fact]
    public async Task Capped_run_fails_closed_when_the_agent_omits_cost()
    {
        var (code, output) = await Run("claude", cap: "1", mutant: "missing-cost");
        Assert.Equal(1, code);
        Assert.Contains("cost guard: a call reported no numeric total_cost_usd", output);
    }

    private static async Task<(int Code, string Output)> Run(string agent, string only = "standard", string cap = "", string mutant = "")
    {
        Tools.Require("bash", "jq", "git", "dotnet");
        var temp = Directory.CreateTempSubdirectory("powerops-skills-smoke-");
        try
        {
            foreach (var name in new[] { "claude", "codex" })
            {
                var path = Path.Combine(temp.FullName, name);
                File.WriteAllText(path, AgentStub.ReplaceLineEndings("\n") + "\n");
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
                }
            }

            var start = new ProcessStartInfo(Tools.Bash)
            {
                WorkingDirectory = Golden.RepoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            if (agent == "-n")
            {
                start.ArgumentList.Add("-n");
            }

            start.ArgumentList.Add(Path.Combine(Golden.RepoRoot, "tests", "smoke", "skills-smoke.sh"));
            if (agent != "-n")
            {
                start.ArgumentList.Add(agent);
            }

            start.Environment.Remove("ANTHROPIC_API_KEY");
            start.Environment.Remove("OPENAI_API_KEY");
            start.Environment["PATH"] = temp.FullName + Path.PathSeparator + start.Environment["PATH"];
            start.Environment["TMPDIR"] = temp.FullName;
            start.Environment["GIT_CONFIG_GLOBAL"] = "/dev/null";
            start.Environment["SMOKE_ONLY"] = only;
            start.Environment["SMOKE_MAX_USD"] = cap;
            start.Environment["SMOKE_MAX_TURNS"] = "3";
            start.Environment["STUB_MUTANT"] = mutant;
            // Use the packages restored by the test build and keep this nested build's intermediates private.
            start.Environment["RestoreSources"] = temp.FullName;
            start.Environment["ArtifactsPath"] = Path.Combine(temp.FullName, "artifacts");
            start.Environment["NuGetAudit"] = "false";
            start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            start.Environment["DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE"] = "true";
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("skills-smoke.sh did not finish.");
            }

            return (process.ExitCode, await stdout + await stderr);
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    // Both CLI protocols drive the same conversation. Only the agent stub writes files in the scenario repo.
    private const string AgentStub = """
        #!/usr/bin/env bash
        set -eu
        message="${!#}"
        case "$message" in
          "You are role-playing"*)
            case "${message##*The agent said:}" in
              *"Which direction?") [[ "$message" == *skip* ]]; reply="Skip because the options settled the layout." ;;
              *"Does it pass?") [[ "$message" == *Pass* ]]; reply=Pass ;;
              *"How many rows?") reply="One hundred." ;;
              *"Approve the draft?") reply=Approve ;;
              *) exit 2 ;;
            esac
            mode=user ;;
          *)
            depth="${PWD##*/}"
            plan=docs/plans/loans
            mockups="$plan/mockups"
            log="$plan/decisions.md"
            mode=resume
            case "$message" in
              "Use the powerops-plan skill"*)
                mode=start
                mkdir -p "$mockups"
                cat > "$plan/plan.md" <<PLAN
        ---
        status: draft
        review: skipped
        signed-off: null
        signed-off-hash: null
        environment: null
        depth: $depth
        supersedes: null
        ---
        # Loans
        ## Goal
        1. Track equipment loans.
        ## Screens
        - Home
        ## Data
        ### Licences
        | Licence | Held |
        |---|---|
        | Standard | Unknown |
        PLAN
                cat > "$log" <<'ROUND'
        ## Round 1
        ### Q1. Which licences and environments?
        1. Standard in Dev (recommended)
        Answer: Standard in Dev (answered by: person)
        ROUND
                sketch() {
                  echo '<!-- route: canvas -->'
                  echo "<p>Sketch $1</p>"
                  echo '<section class="frame desktop" data-screen="Home"><h1 data-lead>Loans</h1></section>'
                }
                if [ "$depth" = standard ]; then
                  for letter in a b c; do sketch "$letter" > "$mockups/option-$letter.html"; done
                  reply="Which direction?"
                else
                  [ "$depth" = quick ]
                  sketch main > "$mockups/prototype.html"
                  for state in empty loading error failed-save; do
                    echo "<div data-for=\"Home\" data-state=\"$state\">No loans</div>" >> "$mockups/prototype.html"
                  done
                  echo '- Home: loan list' > "$mockups/prototype.map.md"
                  reply="Does it pass?"
                fi ;;
              "Skip because the options settled the layout.")
                cat >> "$log" <<'SKIP'
        ## Round 2
        ### Q2. Which direction?
        1. Option A (recommended)
        Answer: Skip the full prototype (answered by: person)
        SKIP
                if [ "$STUB_MUTANT" = judge ]; then touch "$mockups/prototype.judge.md"; fi
                reply="How many rows?" ;;
              "One hundred.")
                cat >> "$log" <<'DATA'
        ## Round 3
        ### Q3. How many rows?
        1. One hundred (recommended)
        Answer: One hundred (answered by: person)
        ## Log
        - prototype: skipped (the options settled the layout (including the loan list)) (answered by: person)
        DATA
                reply="Approve the draft?" ;;
              Pass)
                printf '%s\n' '## Log' '- prototype: approved mockups/prototype.html (main screen: loan list) (answered by: person)' >> "$log"
                sed -i.bak 's/^status: draft$/status: approved/' "$plan/plan.md"
                rm -f "$plan/plan.md.bak"
                reply="Pass recorded." ;;
              Approve)
                sed -i.bak 's/^status: draft$/status: approved/' "$plan/plan.md"
                rm -f "$plan/plan.md.bak"
                reply="Approved." ;;
              *) exit 2 ;;
            esac ;;
        esac

        if [ "${0##*/}" = claude ]; then
          jq -nc --arg reply "$reply" --arg mutant "$STUB_MUTANT" \
            '{session_id:"stub-session", result:$reply} + (if $mutant == "missing-cost" then {} else {total_cost_usd:0} end)'
        else
          [ "$1" = exec ]
          case "$mode" in
            start) [[ " $* " == *" --json "* ]]; echo '{"type":"thread.started","thread_id":"stub-thread"}' ;;
            resume) [ "$2" = resume ]; [ "${@: -2:1}" = stub-thread ] ;;
            user) [ "$2" = --ephemeral ] ;;
          esac
          while [ "$1" != -o ]; do shift; done
          printf '%s\n' "$reply" > "$2"
        fi
        """;
}