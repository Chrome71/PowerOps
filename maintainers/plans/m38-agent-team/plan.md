---
status: done
review: done
signed-off: 2026-11-01
signed-off-hash: 577f4d8ed8943b75250e6b61a7e89b01f4bb929c26ec88e5d9ba9c1f3a73160d
environment: null
depth: standard
supersedes: null
---

# M38: Measure a small agent team before shipping it

## Goal

Find out whether two new agent roles are worth shipping. This milestone ships no role: nothing under `skills/`, `plugins/` or `src/` changes.

The aim behind it is a small team of agent roles for planning, securing, designing and refining Power Platform solutions, alongside Microsoft's skills and MCP servers, designed for clients that read `SKILL.md` files. M38 measures whether the roles help, on Claude Code and Codex only. M39 builds what the person keeps, and tests it on named clients ([m39-outline.md](m39-outline.md)). Neither milestone proves "any client" or "any capable model": a client that wasn't tested stays a stated limit.

| Role | Status | Covers | Can write | Measured here |
|---|---|---|---|---|
| Lead | Exists: `powerops-plan`, `powerops-implement` | Plan, design, build through Microsoft's skills | Plan files, and Dev through Microsoft's skills | No |
| Plan reviewer | Exists: `powerops-review-plan` | Checks the plan and its design | `review.md` | As the baseline |
| Security reviewer | Draft: `powerops-review-security` | Secure | `security-plan.md` or `security-solution.md` | Yes, each pass separately |
| Verifier | Draft: `powerops-verify` | Refine | `verify.md` | Yes |
| The person | Not an agent | Sign-off and promotion, and the keep or drop decision here | The `deploy_apply` form | No |

Done when:

- `tests/eval/roles/` holds the cases, the pilot, the frozen key and baseline, the two draft skills and the run script, and the tests of row 7 pass.
- `docs/plans/m38-agent-team/report.md` records the evaluated commit, the client versions, each client's model and effort, the results by pass and by family with every count and denominator, which of row 1's points had a keyed flaw, every replaced or excluded run with its reason, every wrong `state` and stray write, cost and time where the client reports them, and the person's keep or drop decision. It quotes no planted value.
- A draft M39 plan exists that names only the kept roles.
- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .` are clean, and the work is committed on its own branch.

## Users

- A person deciding which roles PowerOps should ship.
- A contributor who reruns the set after changing a role.

## Data

Line numbers are those of `fb7e0cc`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Draft skill `powerops-review-security`. It works alone and never asks. The request names the plan and the pass, `plan` or `solution`. The solution pass also needs the current solution and the settings file, and takes the previous solution if there is one. A missing required input or a wrong status gives `state: failed`, and the report names it. The plan pass needs status `approved`. The solution pass needs `signed-off`, `implementing` or `done`, so it doesn't copy `powerops-review-plan` section 2. The plan pass reads the plan files. The solution pass also runs `powerops analyze`, `settings validate` and `diff` of the solution against the previous version named in the request; with none, it skips `diff` and says so. For each item of the Power Platform Well-Architected Security checklist, SE:01 to SE:10, and each of these points, it records evidence, a finding, not applicable with a reason, or not verified: data policies, security roles, app users, sharing, connection references, secrets in environment variables, column security, Power Pages table permissions and web roles. It never infers tenant configuration from a clean export. It reads `plan facts` only under the rule `powerops-review-plan` uses. It lists the commands it may run; that list is instruction, not enforcement | `tests/eval/roles/skills/powerops-review-security/SKILL.md` | row 7 |
| 2 | Draft skill `powerops-verify`. It works alone and never asks. The request gives the plan, the current and previous solution, the settings file, the test folder and the Lead's evidence for live checks. It runs `powerops analyze`, `test`, `diff` and `settings validate` with those paths. It needs status `signed-off`, `implementing` or `done`, and a wrong status gives `state: failed`, and the report names it. A missing required input is a failed check, an input that doesn't apply is recorded as not applicable, and it never invents one. It compares the result with the plan's `Steps`, `Data`, `Automation` and `Screens`; anything built outside the plan is a finding. Live evidence is optional: with none, the live checks are recorded as not verified, and the state isn't `failed` for that. It never fires a flow or writes to a tenant. If `<zip>.findings.json`, the output of `powercat-overflow` or `powercat-overpage` from microsoft/power-cat-skills, lies beside the exported zip, it cites that as evidence. It runs nothing from power-cat-skills, because those skills ask questions and upload to a hosted viewer. It lists the commands it may run; that list is instruction, not enforcement | `tests/eval/roles/skills/powerops-verify/SKILL.md` | row 7 |
| 3 | The report format of both drafts. Line 1 `plan-hash: <bodyHash>`, line 2 `model:`, line 3 `fresh-context:`, as `powerops-review-plan` section 4 has them, and line 4 `state:`, from the table under Automation. A solution-pass report and `verify.md` also carry a body line `input: <solution path or zip file name>`, which nothing parses. Findings use the fields and severities of `powerops-review-plan` section 4, numbered S1, S2 for security and V1, V2 for the Verifier. Each draft writes only its own report and never opens `plan.md` for writing | rows 1 and 2 | row 7 |
| 4 | The cases. 12 plan cases, 9 with three flaws each and 3 clean: each is a folder holding `docs/plans/<name>/plan.md`, status `approved` and review `pending`, and `decisions.md`. 6 solution cases, 4 with three flaws each and 2 clean: each holds a plan, the previous and current solution, a settings file and flow tests. Each of these plans starts `approved` with `review: skipped` and no planted tenant value, is signed with `powerops plan sign-off`, and then has only its `status` header line changed to `implementing`, which keeps the hash. Its flaws sit in the solution, the settings and the tests. That makes 39 flaws: 27 in the plan cases, 6 for the Security solution pass and 6 for the Verifier. Every solution case starts from one clean base: `tests/fixtures/powerops-seed/v1` as the previous solution, a copy of it with a raised version and the plan's change as the current one, `tests/fixtures/settings/powerops-seed-v1.valid.json` and `tests/fixtures/flowtests/seed/stamp-work-item-status-active.flowtest.json`. At `fb7e0cc`, `analyze` finds nothing in `v1`, `settings validate` reports no problem with that file, and that flow test passes. `powerops-seed/v2` isn't a base, because it carries two warnings and a settings gap, and the other seed flow test fails on `v1`. A flaw taken from them is planted and keyed like any other. Microsoft's samples aren't used, because AGENTS.md keeps them unmodified. Planted values are fake: `contoso.example`, `@example.invalid`, all-zero GUIDs, listed in `tests/eval/roles/NOTICE.md`. The seed's own component ids stay | `tests/eval/roles/cases/`, which is outside `tests/fixtures`, so the `analyze-fixtures.*` goldens don't change | row 7 |
| 5 | The key and the baseline. `key.json` has one entry per flaw: flaw id, case, pass (`plan`, `security-solution` or `verify`), point (one of row 1's points, an SE number, or `plan checklist`), expected severity, one sentence and, for a solution flaw, `baseline: shown` or `not-shown`. A `plan` flaw is scored for both the `review-plan` and the `security-plan` pass. Shown means that a finding, a failed test or a problem line of the baseline names the flaw's component or value. A `diff` line that only lists a change doesn't show a flaw. At least 4 of each pass's 6 solution flaws are `not-shown`. `baseline/<case>.txt` holds the output of `powerops analyze`, `test`, `diff` and `settings validate` on that solution case, run from the case folder with relative paths and no model, and isn't part of the case | `tests/eval/roles/key.json`, `tests/eval/roles/baseline/` | row 7 |
| 6 | The run script and the pilot. `run.sh <claude\|codex> <pass> <case folder>`, where `<pass>` is `review-plan`, `security-plan`, `security-solution` or `verify`, takes `EVAL_MODEL` and `EVAL_EFFORT` from the environment. It stops if either is missing or if `git status --porcelain` shows a change under `tests/eval/`, `src/` or `skills/`, and it records `git rev-parse HEAD` and the client's `--version`. It copies only that case to a new temp folder outside the repo, copies the source `skills/powerops-review-plan/SKILL.md` or the draft that pass needs, as it stands, into `.claude/skills/<name>/` or `.agents/skills/<name>/`, puts a build of this commit first on `PATH`, and starts one new, unresumed session with the pass's fixed prompt, which claims no `pac` sign-in, under a 30-minute limit. That new session is what earns `fresh-context: yes`, because a copied skill has no fork line. Claude runs `claude -p --setting-sources project --strict-mcp-config --tools Bash,Read,Write,Edit,Glob,Grep,Skill --permission-mode acceptEdits --output-format stream-json --verbose` with `--model`, `--effort` and `--allowedTools` set to `Bash(powerops analyze *)`, `Bash(powerops diff *)`, `Bash(powerops test *)`, `Bash(powerops settings validate *)` and `Bash(powerops plan status *)`. Codex runs `codex exec --ephemeral --skip-git-repo-check -s workspace-write --json` with `-m` and `-c model_reasoning_effort=`, and with `HOME` and `CODEX_HOME` pointing to a throwaway folder that holds only a link to the person's Codex sign-in file. If a run replaced that link with a new file, because the client refreshed its token, the script moves the new file over the person's sign-in file, so the refreshed token isn't lost. Then it removes the folder. `--ignore-user-config` isn't enough, because it still loads the user's `AGENTS.md` and skills. The script saves, outside the repo, the event stream, the exit code, the start and end time, the report and a `diff -rq` of the case against the temp folder. Right after each run it searches the event stream for the repo path and the key's and baseline's folder names, and flags a hit. `run.sh --list <case folder>` prints what it would copy and starts nothing. The script stays bash 3.2 compatible, as `tests/smoke/skills-smoke.sh` does. `pilot/` holds one plan case and one solution case with one flaw each, and its own `key.json`. They aren't among the 18 and aren't in any result | `tests/eval/roles/run.sh`, `tests/eval/roles/pilot/` | row 7 |
| 7 | Two tests. The first runs on every OS: every key entry names a real case and pass and has a point, every case and pass has its expected flaw count, every solution flaw has its `baseline` field, every solution case's baseline file matches live output, the plan cases parse with `PlanDocument`, `PlanChecker.Check` finds no violation in the solution-case plans, `analyze`, `test` and `settings validate` report nothing on the two clean solution cases, both drafts carry the four header lines and portable front matter, `DocsDriftTests.UnknownCommands` and `UnknownFlags` find nothing in either draft, and the commands the drafts list, `plan facts` aside, are in `run.sh`'s allowlist. The second needs bash and is skipped on Windows, as `ActionScriptTests` does with `Tools.Require("bash")`: `run.sh --list` prints no key or baseline file | `tests/PowerOps.Cli.Tests/RoleEvalTests.cs` | itself |
| 8 | `AGENTS.md` Layout gains a `tests/eval/` line. `docs/BUILD-PLAN.md` gains an M38 section after M37, with a milestone row (no release) and build-order items from 66 | as listed | `dotnet test` |

## Automation

None of the product's automation changes. The runs are, for each of Claude Code and Codex:

| Pass | Cases | Runs |
|---|---|---|
| Plan reviewer | 12 plan cases | 12 |
| Security reviewer, plan pass | 12 plan cases | 12 |
| Security reviewer, solution pass | 6 solution cases | 6 |
| Verifier | 6 solution cases | 6 |

That is 36 runs per client, 72 in all, after the 8 pilot runs of step 4. The baseline for the solution cases is the stored output of row 5.

A report's state:

| State | Meaning |
|---|---|
| `ready` | The role finished and no High finding is open |
| `blocked` | A High finding is open, or a question only a person can answer |
| `failed` | A required check could not run, and the report says which |
| `dropped` | Nothing for this role to do, and the report says why |

Choose `blocked` before `failed` before `ready`. An optional check that can't run isn't a failure.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant, and no step runs `pac`, `az`, `plan facts` or any `--apply`. The runs hold to this by their setup: no MCP server is loaded, Claude's allowlist has only offline commands, and Codex's throwaway `HOME` has no `pac` profile.

## Solution and publisher

None.

## Constraints

- AGENTS.md: new logic comes with tests, `dotnet format --verify-no-changes` stays clean, Conventional Commits, no version or changelog edits, no new dependency.
- Nothing ships. The drafts stay under `tests/eval/roles/skills/`, which the `skills/**` embed in `src/PowerOps.Cli/PowerOps.Cli.csproj:33` doesn't reach, so `SkillsCommandTests` and `PluginManifestTests` don't change.
- No model id in a committed skill (`docs/PLANNING-SKILLS.md`). `report.md` records what each client reported.
- All runs of a client use one model and effort: for Claude the tier the shipped review runs on, `opus`, and for Codex its strongest model, both at high effort.
- The cases, the key, the baseline, the skills, `run.sh` and its prompts are frozen by commit before the first of the 72 runs, and `report.md` names that commit. A defect found after the freeze is fixed in a new freeze commit, and every pass it touches reruns on both clients. `report.md` records it.
- The runs cost model quota. They run by hand, never per build, and the 72 start only after the person has seen the pilot's estimate and said go.
- Keep rules, frozen before the first run. The numbers are judgement calls, not results from a study:
  - A flaw counts once per case and run. Results pool both families, and each family is also shown.
  - Security plan pass: finds at least 15% of the plan flaws that the same-family Plan reviewer missed on the same case.
  - Security solution pass and Verifier: each finds at least 15% of its own flaws marked `baseline: not-shown`.
  - False alarms: at most one false High per role across its clean-case runs, pooled. A false High is a High on a clean case that the scorers can't support from the case files. One they can support is recorded as an unkeyed finding, not a flaw.
  - Second-family advice: pair the Plan reviewer with the Security plan pass. For families A and B, compare the assignments AA, BB, AB and BA on the cases with valid results for all four. Union recall is the share of all keyed plan flaws in those cases that either role in a pair found. The mean of AB and BA must beat the better of AA and BB by at least 10 percentage points.
  - A pooled denominator below 5 is inconclusive, and inconclusive doesn't justify keeping.
  - Below a floor, that pass is dropped, and a role with no pass left is dropped. At or above a floor, the person decides: passing permits keeping and never requires it.
  - One valid run per case, pass and family, including results near a floor. `report.md` calls its conclusions provisional.
  - An infrastructure failure is a client, sign-in, quota, rate-limit or network error that ended a run before it wrote a report, or a stop at the 30-minute limit with retries or a rate-limit message in the event stream. Any other run is valid, including one with no report, a malformed report or another stop at the limit, and it scores what its report holds, which may be nothing.
  - A quota or rate-limit failure pauses the runs, and the same run starts again after the limit resets. That isn't a replacement.
  - Any other infrastructure failure, or an event stream that shows a read outside the temp folder, gets one replacement: a fresh run of the same case, pass and family. If the replacement fails the same way, the case leaves that role's denominator. Excluded transcripts and reasons are kept. A valid poor result is never replaced.

## Assumptions

- Microsoft's per-product planners and architects cover product design, so there is no Designer role.
- A second writer is the setup the evidence warns about, so there is no Builder role, and the Lead builds.
- A report is written once, at the end of a run, so there is no `progress` state.
- A temp folder is a mitigation, not proof that a reviewer was blind. `run.sh` searches each event stream for the repo path and the key's folder.
- One run per case and client gives a rough signal, not a measurement that would hold at scale.
- The Claude runs use the source skill in a new session, not the installed fork, so the Plan reviewer's baseline differs from the shipped setup in how the session starts. Neither has chat history, and the model is pinned.
- Plan flaws are written from row 1's list and the Plan reviewer's checklist, so the plan-pass floor is a low bar, and `report.md` says so.
- The seed has no security role, field security profile or Power Pages site, and data policies, app users and sharing never show in an export. Most of row 1's points will have no keyed flaw, and the keep decision says nothing about them.
- A family here is a client with its own harness and model, and Codex's shell has no allowlist. The second-family advice can't separate the three, and `report.md` says so.
- No case holds a zip, so the `<zip>.findings.json` branch of row 2 isn't exercised, and `report.md` says so.
- `--read-only` for the MCP server was in an earlier draft and is cut: no role would use it, because the skills call the CLI through a shell and a subagent inherits its session's servers.

## Open questions

## Questions for your admin

## Steps

1. This plan, a fresh review, fixes, sign-off.
2. On `test/m38-role-eval`: rows 1, 2 and 3, the two draft skills, from this plan only.
3. Rows 4, 5, 6 and 7, by an agent that neither changes nor reads the draft skills. Its brief is row 1's point list, the Well-Architected Security checklist and `powerops-review-plan` section 3, so the flaws aren't planted in the drafts' own words. Row 7's key and case checks come first, and the baseline runs before the key's `baseline` fields are written. Commit by path.
4. The pilot, before the freeze. Run each of the four passes on the `pilot/` cases on both clients, 8 runs, and score them as step 8 does. Fix what breaks, commit by path, and after any change to `run.sh`, a prompt or a draft, rerun the pilot runs it touches. An agent that didn't write the cases then checks that each keyed flaw is in its case and that each clean case has none. The coordinating session runs its leak scan. The last commit of this step is the freeze.
5. Stop. Scale the pilot's tokens and time to the 72 runs, and show the person. The runs start only on their go.
6. Write the freeze commit's id into `report.md` as the evaluated revision.
7. The 72 runs of the Automation table. A failed or flagged run is settled here, under Constraints.
8. The coordinating session writes a neutral copy of every report, with only the finding text, new ids, no header lines and no `input:` line, and it keeps the mapping. Two agents that didn't run the reviews score the copies against the key, one from each family, each on its own. They also record any claim about tenant configuration drawn from an export. From the original reports and the saved files, the coordinating session records a wrong `state` and any file a run changed besides its own report and the Plan reviewer's `review:` line. It settles every difference, applies the keep rules and writes `report.md`.
9. Stop. The person keeps or drops each pass and the second-family advice, and `report.md` records it.
10. With `powerops-plan`, draft the M39 plan from `report.md` and `m39-outline.md`. It names only the kept roles and has no conditional rows.
11. Row 8.
12. The coordinating session reads the diff, and re-runs `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` and `powerops plan status .`. It also runs its leak scan. The scan's patterns are private, so it isn't in this repo, and a building agent skips it.
13. Commit by path on `test/m38-role-eval`. No push or PR unless the user asks.
