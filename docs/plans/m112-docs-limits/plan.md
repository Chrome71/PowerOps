---
status: done
review: done
environment: null
depth: quick
supersedes: null
signed-off: 2026-11-13
signed-off-hash: 8aa961de16e900fb2b3838662a369f5ce2d6da31cc36d69d857b5ca302fb1161
---

# M112: Say what the checks don't prove

## Goal

Add the doc lines that the review-enforcement audit of 2026-11-13 calls for, so a reader's picture of the review check, the gate and the GitHub example matches what `main` does. The behaviour was decided earlier by the owner; only the reader's picture is wrong. Authority: the owner's answers to the audit's decision packet, "All recommended" and "Yes, same as the chains" (2026-11-13, `decisions.md`). Decisions 7(a) (no reviewer check in the Action; doc lines) and 8(a) (no stage-specific release policy now; the docs say reviews are stage-agnostic) are carried out here.

Done when:

1. Each line of Data rows 1 to 11 is on the branch, in the page named, worded as in the row (an executor may change wording only to fit a pinned-text test, and says so in its report), and each states only what the code at `origin/main` does when the pull request merges.
2. `dotnet build` (0 warnings), `dotnet test` (0 failed), `dotnet format --verify-no-changes` and `powerops plan status docs/plans/m112-docs-limits` (no violation) pass, and the leak scan (`bash reports/leakscan.sh origin/main <branch>`, run from the main checkout, where the git-ignored `reports/` folder is) finds 0 hits with its control found.
3. The diff touches only the files of Data rows 1 to 11, this plan folder and, if a pinned-text test needs it, that test (`DocsDriftTests`, `HandoffDocTests` or `EcosystemDocsTests`), changed only as far as the new line requires. `src/`, `skills/`, `plugins/`, `.claude/agents/`, `tests/eval/`, `tests/golden/`, the version files and the changelog are unchanged.

### Won't build

- Any change to code, to what a test checks beyond a pinned-text update, to skills, plugin copies, `tests/eval/` or the two kept roles.
- C-2 (Power BI wording; M115's), the canvas `.msapp` line (C-1; carried by M96), and everything the packet lists under "Not proposed" or assigns to M114, M115, M116 or M96.
- A reviewer check in the Action (decision 7(b)) and a stage-specific release policy (decision 8(b)).
- A change to the plugins' `plugin.json` description ("Plan, review and gate"): out of scope; Data row 2 says what the plugins install instead.
- The example workflows' comments that say the environment has required reviewers (`promote-dev-to-test.yml` lines 6 and 162, `ephemeral-pr.yml` line 28): they describe the setup the README asks for, which row 5 now says is the reader's step; the `.yml` files stay byte-identical, so no workflow test is touched.
- A `docs/BUILD-PLAN.md` decision: decisions 7(a) and 8(a) keep earlier decisions (M52 Q4, decision 67) and need no new number.

## Users

- A person reading `docs/COMMANDS.md`, `docs/examples/README.md` or `docs/GITHUB-ACTION.md` before relying on the review check or the example's approval.
- A person setting up an agent from `docs/USE-WITH-AGENTS.md`, `docs/HARNESS.md` and `docs/HANDOFF.md`.
- A maintainer editing a kept role (`AGENTS.md`), and the next ecosystem review (`docs/ecosystem/compatibility.md`).

## Data

Line numbers are those of `ccf9833`. Each row is one finding of the packet (its id in brackets), the page and place, and the text to add. Text in quotes is the wording to use.

1. [E-1, E-2 (one clause), E-3, E-4 and 8(a), E-8, C-3] `docs/COMMANDS.md`, section `deploy apply`, Options: a new paragraph and list after the paragraph of lines 722 and 723 (it ends "... unless its `require-reviews` input is `false` ([the Action](GITHUB-ACTION.md))."), before `### What it does`; lines 722 and 723 stay as they are:

   "The check reads each report's header lines only: `state:`, `plan-hash:`, `solution-hash:` and `deploy-hash:`. `ready` is the role's own word: the check reads no finding, no resolution and no check line, so a `ready` report with an open finding, or with every check `not verified`, covers the zip all the same. It doesn't read the plan's `status:`, its sign-off or its `review.md` either: only the plan body's hash. A cover binds the zip, always by its SHA-256 (and for a `deploy-hash:` link also by the digest of its unpack), and the plan body, by `plan-hash:`. It doesn't bind:

   - the settings file: the plan file's hash covers it ([deploy plan](#deploy-plan), Output), so a plan file made before the settings file changed is refused, but once `deploy plan` has run again the same reports cover the zip with the new settings;
   - the target: no header line names an environment, so reviews are stage-agnostic, and the reports that cover a zip for Test cover the same zip for Prod; the yes to that deployment (the form, the GitHub environment's approval or the person's yes in the session) picks the target;
   - the `powerops` version the review ran with, the version of the role that wrote a report, and when it was written: an old report covers as well as a new one.

   Two ready reports also say nothing about what the checks don't read: the Copilot Studio settings a solution doesn't carry ([HANDOFF step 11](HANDOFF.md#the-steps)), the behaviour of a code app's built bundle, and SharePoint lists, which no solution holds. Read both reports before the yes, and check those yourself."

   Code basis: `PlanChecker.ReadReports` reads `state:`, `plan-hash:`, `input:`, `solution-hash:` (first six lines) and `deploy-hash:` (first seven); `PlanChecker.Reviews` passes only the plan body's hash; `DeployReviews.Fault` links a report when its `solution-hash:` is the zip's SHA-256, or its `deploy-hash:` is and its `solution-hash:` equals the unpack's digest, and `Ready` then checks state and staleness against the body hash (`input:` feeds the evidence notes, not the cover); the plan file's hash covers each settings file's bytes (`docs/COMMANDS.md`, deploy plan, Output); no header line the check reads names an environment, a tool or role version, or a date. E-2 is in the packet's M114 scope, not this brief's list; the clause is added because it is true at `ccf9833` and the reader's picture of the cover is wrong without it (`decisions.md`).

2. [B-5] `docs/USE-WITH-AGENTS.md`, line 38 ("The plugin also starts the PowerOps MCP server: see [MCP server](#mcp-server)."): append "It installs no hook: the import gate is configuration you add yourself, the hooks and deny rules in [Gate Microsoft's imports](HANDOFF.md#gate-microsofts-imports)." Basis: no hook file or `hooks` key under `plugins/claude/` or `plugins/codex/` (checked 2026-11-13).

3. [B-6] `docs/HARNESS.md`, a new paragraph after line 27 ("Treat "not checked" as absent: ..."): "A fresh context is not a separate permission set. The builder and the two report roles run in one session, with its tools, permissions and sign-ins, so PowerOps adds nothing that keeps a builder from editing a report, a hook or the client's settings file: `powerops-implement` tells the builder never to edit a hook, a permission rule or a setting to get past a block, no instruction forbids editing a report, the gate doesn't block such an edit, and what your client's own permission prompts or deny rules stop is not checked here." Basis: roles get `context: fork` only (`SkillsCommand.cs`); the gate routes imports, uploads, installs and publishes, not file edits (`docs/HANDOFF.md`, What it doesn't do); `docs/USE-WITH-AGENTS.md:71` already says a role runs with the session's tools. No claim about what a client blocks.

4. [B-8] `docs/HANDOFF.md`, line 32 ("The steps go from Dev to Test. For Prod, repeat steps 7 to 9 with Prod as the target, with its own plan and approval."): append "Keep Prod's sign-ins away from the agent: it can use every `pac` and `az` sign-in of the operating-system user it runs as ([Sign-in state](#sign-in-state)), so sign that user in to Dev and Test only, and import into Prod through an Action apply job whose GitHub environment has required reviewers (as [the example](examples/README.md) does for Test). Where your GitHub plan gives none, run the Prod import as an operating-system user no agent runs as." Basis: `docs/HARNESS.md:12` (the MCP server runs `pac` and `az` with the user's sign-in); `docs/HANDOFF.md` Sign-in state (pac's MCP server uses the same auth profiles).

5. [B-3, decision 7(a)] `docs/examples/README.md`, line 9 (How it runs, item 3): replace its first sentence "The `apply` job runs in the GitHub environment `test`, which has required reviewers, so it waits for an approval." with "The `apply` job runs in the GitHub environment `test`, and waits for an approval when `test` has required reviewers. Adding them is your step ([below](#github-environments-and-secrets)): the action checks only that the job names an environment, not its reviewers, and a workflow that names a missing environment creates it with none, so without them every merge imports into Test with no approval." The item's other sentences stay. Basis: `action/check-apply.sh` reads only the environment's name from the OIDC token; the example sets `require-reviews: 'false'`; GitHub's *Managing environments for deployment* (Assumptions).

6. [H-6] `docs/examples/README.md`:
   - line 87: replace "Required reviewers are available on public repositories on GitHub Free." with "On GitHub Free, Pro and Team, required reviewers and the administrator bypass setting work only in public repositories, and Free can't configure environments for a private repository at all ([Deployments and environments](<page 1>), [Managing environments for deployment](<page 2>), read 2026-11-13). So on Free the example doesn't run in a private repository as written. Check your GitHub plan before you rely on reviewers in a private repository, and never make a client's repository public to get them: on Pro or Team without reviewers, drop the `apply` job and run the import yourself." Here `<page 1>` and `<page 2>` stand for the https addresses of the two pages named in Assumptions.
   - line 21 (end of the private-repository section's second paragraph): append "A private repository may not get the required reviewers the apply job relies on: see [GitHub environments and secrets](#github-environments-and-secrets)."
   - line 148: replace "Use an Entra app for nothing else, and a private repository." with "Use an Entra app for nothing else, and a private repository on a GitHub plan that gives it required reviewers ([GitHub environments and secrets](#github-environments-and-secrets))."
   - line 271 (the ephemeral recipe's first test prerequisite): after "a second person to approve each run (Prevent self-review)" insert " (in a private repository, only on a GitHub plan that gives it required reviewers: see [GitHub environments and secrets](#github-environments-and-secrets))".

7. [H-7] `docs/examples/README.md`, line 44: append "An agent that pushes with your GitHub token pushes as you, so with an agent at work the repository is no longer one only you can push to: use the two app registrations."

8. [H-6] `docs/GITHUB-ACTION.md`, line 79: after the sentence that ends "bound to that environment alone ([the example's setup](examples/README.md))." insert "On GitHub Free, Pro and Team, required reviewers work only in public repositories ([GitHub environments and secrets](examples/README.md#github-environments-and-secrets)); never make a client's repository public to get them."

9. [H-9] `docs/GITHUB-ACTION.md`, line 23: change "the pin in these snippets is `main` as of 2026-10-29" to "the pin in these snippets is `main` as of 2026-11-10" (the three snippets' comments say 2026-11-10), and after "Pin the full SHA of the newest commit of `main` when you copy them" insert ", and before you move a pin to a newer commit, read the diff of `action.yml` and `action/` between the two commits, because that code runs with your workflow's credentials". Also `docs/examples/README.md` line 108: change "the example pins `main` as of 2026-10-29" to "the example pins `main` as of 2026-11-10". Basis: the snippets at lines 14, 56 and 166 of `docs/GITHUB-ACTION.md` and the four `uses:` lines of the two example workflows pin `cbc31e7` "main on 2026-11-10".

10. [H-3] `AGENTS.md`, the kept-role rule (line 40): after "a test in `RoleEvalTests` holds both roles and `run.sh` to the pinned or recorded ids." insert "That test catches an edit with no recorded comparison, not a false record: the person's review of each recorded comparison is the control." Basis: the packet's reproduction `G/frozen-role-pin-test.txt` (a removed check plus a false "comparison recorded" line passes).

11. [H-6] `docs/ecosystem/compatibility.md`: a row after "GitHub Action SHAs":

    `| GitHub environments | hosted, no version | The apply jobs' environment in docs/examples/promote-dev-to-test.yml (test) and docs/examples/ephemeral-pr.yml (ephemeral); action/check-apply.sh reads only the environment's name from the job's OIDC token | GitHub's Deployments and environments and Managing environments for deployment pages, read 2026-11-13 (M112) | On Free, Pro and Team, required reviewers, wait timers and the administrator bypass setting work only in public repositories, and Free configures no environment for a private repository. A workflow that names a missing environment creates it with no protection rules or secrets. The action doesn't check reviewers (M112 decisions.md) | 2026-11-13 |`

    (paths and names in backticks in the file, as in the other rows). `EcosystemDocsTests` checks six cells and a real date not after today.

## Automation

None.

## Screens

None.

## Environments and route

None. No tenant call of any kind (`tenant: offline`), and no GitHub setting is read or changed.

## Solution and publisher

None.

## Constraints

- AGENTS.md: the smallest change that fully solves the task; `dotnet format --verify-no-changes` clean; no version or changelog edits; no tenant values; Conventional Commits (`docs:`).
- Each line is short, in the page's own voice (plain sentences, no new headings), and states only what `main` does at merge time. If `main` moves before the merge (the lock's rebase), the executor of step 3 rereads each row's code basis against the new `origin/main` and corrects any line that a merged change made stale.
- The frozen eval stays frozen: `tests/eval/` byte-for-byte, `RoleEvalTests` passing, the two kept roles untouched.
- Pinned-text tests are updated only as far as a new line requires, and the step's report names each such change.
- Ecosystem rule: the GitHub claims of rows 5, 6, 8 and 11 were verified through `powerops-researcher` on 2026-11-13 against GitHub's docs (Assumptions); only what those pages state is written. No Microsoft service is newly described.
- One writer at a time; the executor stages its own paths by name, never `git add -A`.

## Assumptions

- Verified 2026-11-13 by `powerops-researcher` (Opus) against GitHub's *Deployments and environments* (Actions reference) and *Managing environments for deployment* (Actions how-to) pages, and again by the Fable plan review the same day: on Free, Pro and Team, required reviewers, wait timers, custom protection rules and the administrator bypass setting are available only for public repositories; Free configures environments only for public repositories; a workflow naming a missing environment creates it with no protection rules or secrets; "Prevent self-review" stops the user who triggered a run from approving it; administrators may bypass protection rules by default. Not stated on those pages: that a private repository needs GitHub Enterprise for reviewers. So no line names Enterprise.
- The packet's code reading (at `ccf9833`) still holds at `origin/main` `ccf9833` (checked 2026-11-13: `PlanChecker.ReadReports`, `DeployReviews.Fault` and `Ready`, `action/check-apply.sh`, no plugin hook).
- M108 (open, not merged) also edits `docs/COMMANDS.md` and `docs/HANDOFF.md`; whichever merges second rebases. M114 and M115 will change row 1's E-1 and E-2 sentences (an open High, an unsigned plan) and the agent's yes, and update these lines then (hand-over note).

## Open questions

## Questions for your admin

## Steps

1. **Plan.** This plan and `decisions.md`; `powerops-review-plan` in a fresh context into `review.md`, and the Fable plan review; fixes; sign-off by the building session. Files: `docs/plans/m112-docs-limits/plan.md`, `decisions.md`, `review.md`. Depends on: nothing. Done when: Fable approved, `review.md` has no open finding, and `powerops plan sign-off` exited 0. Verified by: `powerops plan status docs/plans/m112-docs-limits`.
2. **The doc lines.** Rows 1 to 11, and any pinned-text test they need. Executor: Opus. Files: `docs/COMMANDS.md`, `docs/USE-WITH-AGENTS.md`, `docs/HARNESS.md`, `docs/HANDOFF.md`, `docs/examples/README.md`, `docs/GITHUB-ACTION.md`, `AGENTS.md`, `docs/ecosystem/compatibility.md`, and only if needed `tests/PowerOps.Cli.Tests/DocsDriftTests.cs`, `HandoffDocTests.cs` or `EcosystemDocsTests.cs`. Depends on: 1. Done when: each row's text is in place; `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass; one `docs:` commit. Verified by: the Fable work review reads each line against its row and its code basis.
3. **Checks and record.** The checks of Done when 2 and 3 on a clean tree, the leak scan, a Log line in `decisions.md`, and the header's `status:` set to `done` by hand (the body, and so its hash, untouched). Files: `docs/plans/m112-docs-limits/plan.md` (header), `decisions.md`. Depends on: 2. Done when: every check passes and the commit is made before the merge. Verified by: the commands' output in the Log line.
