plan-hash: eec1263c8133172b9fe68850adc5e17cf52bc16d5f65d67295f43e0f5c852634
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: the request did not say that `pac` is signed in, the plan names no environment (`environment: null`), and the live check is a later step. I ran no `powerops plan facts`, `pac` or `az`, made no tenant call and no GitHub call that writes. The review is against the repo at commit `8d7e304` (read with `git show`, not the working tree), GitHub's docs (fetched read-only from docs.github.com: "OpenID Connect reference", "Deployments and environments", "Contexts", "Variables") and Microsoft Learn (checked with the Learn MCP tools on 2026-11-04). The plan has 0 open questions.

Checked and found sound:
- Every file, line range and test name the plan cites exists at `8d7e304` and says what the plan says (`DeployApply.cs:12-15`, `DeployApplyReport.cs:23-47,66-68`, `DeployCommand.cs:28-63,261-337`, `Program.cs:39-77`, `McpCommand.cs:176-181,290-378,442-469`, `action.yml:116-152`, `apply.sh:33-35`, `plan.sh:14`, `DeployGateCommandTests.BlockNames` at 392, the two apply tamper tests at 244 and 413, `HANDOFF.md:100`, the doc line ranges).
- `McpCommand.Passed()` (277) deserializes only `status`, `target` and `hash` with `JsonSerializerOptions.Web`, so the new apply fields do not break it. `DeployPlanFile.ReadFields` (DeployPlanFile.cs:45-105) reads named properties and ignores others, so `plannedBy` is ignored. The plan hash is computed from inputs only (`DeployPlan.cs:405`), so neither new field enters it. `deploy gate` uses `DryRun` and never reads the report fields.
- `schemas/README.md` "Versioning": adding fields keeps `schemaVersion`; the rule about enum values applies to existing closed lists, not new fields, so keeping 1 is consistent.
- GitHub facts match the docs: the `environment` claim exists; the subject carries `environment:<name>` only when the job references one; the token is requested with `ACTIONS_ID_TOKEN_REQUEST_URL` and `ACTIONS_ID_TOKEN_REQUEST_TOKEN`, and the docs' own example appends `&audience=...`, so the URL already holds a query string and `&audience=` is right; no context or default variable holds a job's environment (the `job` and `github` contexts have none); required reviewers (up to six, one approval), "Prevent self-review", administrators' bypass on by default and switchable, and the Free/Pro/Team limit are as the plan states.
- Learn claims in Assumptions match their pages (SE:05 "Any action must be traceable"; the delegated-deployment FAQ errors for service principal and owner approval; approvals come with delegated deployments).
- Both example workflows that use `plan-file` already satisfy the new rules (`promote-dev-to-test.yml:236-242`: `path` = folder of `plan-file`, `environment: test`, `id-token: write`; `ephemeral-pr.yml:151-161`: `path: deploy` and `plan-file: deploy/powerops-plan.json`, `environment: ephemeral`, `id-token: write`). The in-process `mcp` flag cannot be set by an MCP caller: `McpCommand.DeployApply` builds the argument list itself (294) and only `Exec(Args(true), plan.Hash)` passes a hash (388). No write is added outside `SafeWrite`; the pinned roles and `tests/eval` are untouched. Checklist items 1 to 8 do not apply (no solution, connector, table, role or screen is built); item 11 is met (a `Won't build` list, each row traces to a Goal line).
- Not checked: the bash `cd`/`pwd -P` behaviour on a Windows Git Bash runner (no Windows host here); see R4.

## Findings

### High

No findings.

### Medium

#### R1. `base64 -d` on a JWT payload fails on real tokens, so every real `apply: 'true'` would be refused
- Area: Testability (checklist 9); Dev to Test to Prod route (5)
- Finding: Row 5 reads the payload "with `base64 -d` and `jq`". A JWT payload is base64url (`-` and `_` instead of `+` and `/`, no padding). I checked on this host: GNU `base64 -d` on an unpadded payload that holds a `-` exits 1 ("invalid input"); after `tr '_-' '/+'` it decodes (GNU tolerates the missing padding). A real token of 800 or more bytes will almost always hold a `-` or `_`. The script fails closed, so nothing unsafe happens, but the first live apply would be refused, and the tests would not show it if the fixture token is made with standard base64 or happens to have no `-` or `_`. Under `set -euo pipefail` a failed decode could also end the script with a non-2 code.
- Suggested fix: Say in row 5 that the payload is translated (`tr '_-' '/+'`) before `base64 -d`, and that a decode or `jq` failure exits 2 with `::error::`. Require the test fixture token to be unpadded base64url with at least one `-` and one `_` in the payload.
- Resolution: Fixed. Row 5 gives the base64url steps (`tr '_-' '/+'`, `=` padding, then `base64 -d` and `jq`) and says a decode or `jq` failure exits 2 with `::error::`; the passing test's token has an unpadded base64url payload that holds both `-` and `_`, and the test asserts that.

#### R2. The path binding also blocks dry runs and existing `plan-file` users, and the plan does not say so
- Area: Dev to Test to Prod route (5); Testability (9)
- Finding: Done when 2 and row 5 apply rule (a) to every run with `plan-file` set, dry runs included. The default of `path` is `.` (`action.yml:8`), and until now `path: .` with `plan-file: deploy/powerops-plan.json` was allowed (analysis of the whole repo, import from the plan's folder). After this change that workflow fails on a dry run with exit 2. The plan says "a dry run needs neither an environment nor an approval" but never says it needs the folder match, and the Constraints and Done when do not call this a breaking change (M49 used `feat!:` for a schema change). `docs/GITHUB-ACTION.md:69` and `docs/examples/README.md` are in row 8, but a migration line is not.
- Suggested fix: State in Done when 2 and row 8 that the folder rule applies to dry runs too, add a migration line ("set `path` to the folder of `plan-file`"), decide the PR title (`feat!:` or `feat:` with a `BREAKING CHANGE:` footer) in a decisions entry, and add an `ActionScriptTests` case that a dry run with `path: .` and another plan folder exits 2.
- Resolution: Fixed (decisions Q12). The rule applies to dry runs too, on purpose; Done when 2 and Constraints call it a breaking change with a `feat!:` title, row 8 adds the migration line, and row 5 adds the dry-run case with `path: .`.

#### R3. An existing MCP test breaks and row 4 does not list it
- Area: Testability (9)
- Finding: `McpCommandTests.Deploy_plan_equals_the_cli_and_matches_its_schema_and_writes_nothing_without_save` (line 340-355) compares the tool's output with `CliWith(..., "deploy", "plan", "--target", Secret, "--format", "json", "--", folder)`. After row 4, the tool adds `--planned-by agent`, so its JSON has `plannedBy: "agent"` and the CLI call without the option has `null`: the test fails. `Save_writes_exactly_one_new_file_holding_the_result` (672) compares the file with the tool output and still holds. Row 4's Test column lists new tests only.
- Suggested fix: Name this test in row 4 and say its expected CLI arguments gain `--planned-by agent`.
- Resolution: Fixed. Row 4 names the test; its CLI call gains `--planned-by agent`.

#### R4. The plan-file folder derivation is unspecified, and a backslash path can make the binding pass wrongly on Windows
- Area: Testability (9); Platforms (Constraints)
- Finding: Row 5 says the script resolves "the folder of `plan-file`" with `cd` and `pwd -P`, but not how it gets the folder. On a Windows runner `runner.temp` renders as `D:\a\_temp`, and the example writes `${{ runner.temp }}/deploy/...` (mixed). If a user writes `D:\a\_temp\deploy\powerops-plan.json` (all backslashes), `dirname` or `${PO_PLAN_FILE%/*}` in Git Bash gives `.` or the whole string, not the folder. With the default `path: .` the two then compare equal, while `deploy apply` (.NET) reads the zips from `D:\a\_temp\deploy`. That is the bypass the rule exists to close. The tests run on Linux only (`ActionScriptTests` is skipped on Windows), so nothing covers it. A bare file name (no slash) has the same `.` result by design, which is right, but is untested.
- Suggested fix: Specify the derivation (turn `\` into `/` first, then strip the last segment; a value with no separator means `.`), and add cases for an all-backslash spelling, a bare file name and a plan file that does not exist (the folder check should not pass on a missing plan file; `deploy apply` would fail later, but say which). Record in the live check that the Windows runner was not run, or run the binding once on a Windows runner.
- Resolution: Fixed (decisions Q5, amended). Row 5 gives the derivation: `\` to `/` only on a Windows runner, the folder is everything before the last `/` (`.` without one), and a missing plan file is refused; tests cover a bare name, a missing plan file, and a backslash spelling with and without `OS=Windows_NT`. The live check runs on `ubuntu-latest`, so Windows Git Bash stays unrun, and `live.md` says so.

#### R5. The gate's own guidance still sends an agent to the unenforced local route
- Area: Dev to Test to Prod route (5); Security roles (8)
- Finding: `DeployCommand.cs:22` (`ImportGuidance`) tells a blocked agent: "run powerops deploy apply <plan file> --target ... --apply after the user's yes to that environment". `docs/COMMANDS.md:93` says the same. The plan's new docs say that outside an interactive client the yes is the Action's protected environment, "not an agent running the CLI", and that a local `--apply` is checked by nothing. Row 8 does not touch `ImportGuidance` or the `COMMANDS.md:93` Agents line (it adds to the Agents line but keeps the route), and `DeployGateCommandTests` asserts the guidance text. The self-plan hole of `HANDOFF.md:100` stays open for an agent that follows the hook's own message, with `approval.mode: cli` as the only trace.
- Suggested fix: Decide in decisions.md whether `ImportGuidance` changes (for example to name the MCP tool or the Action first and the local command as the person's own), and list the constant and its test in row 8 or in Won't build with the reason. Do not leave docs and the hook message contradicting each other.
- Resolution: Fixed (decisions Q13). New row 9: `ImportGuidance` names the two yes routes and leaves the local command to the user, and `docs/COMMANDS.md`'s Agents line says the same.

#### R6. `executor: action` and `approval.mode: action` can be written by any local caller of `--github-run`
- Area: Dev to Test to Prod route (5); Security roles (8)
- Finding: Row 2 makes `--github-run <text>` a public CLI option. Any shell caller, including an agent, can run `deploy apply --apply --github-run "run 1 attempt 1"` and get a report that says `executor: action, approval: {mode: action, receipt: ...}`. That reads as stronger than the honest `cli`/`cli`, and an auditor who reads the report alone is misled. Q3 admits the option is self-reported, but the docs line in row 8 ("no field proves who approved") does not say that a report cannot tell a real Action run from a typed flag. Row 2 also does not say what happens when `--github-run` is combined with the in-process form hash (precedence is untested).
- Suggested fix: Either require the Action's ambient facts as well (`GITHUB_ACTIONS=true` and `GITHUB_RUN_ID` contained in the receipt; still forgeable, but a typed flag alone no longer works), or write in row 8 and in COMMANDS.md that `executor` and `approval` are claims and an auditor must open the run named by the receipt. Add the combined-flag precedence to row 2's tests.
- Resolution: Fixed in the docs (decisions Q3, amended): `executor` and `approval` are claims, and an auditor opens the run the receipt names; the receipt now starts with the repository. The precedence of the approved hash over `--github-run` is in row 2 with a test. Checking `GITHUB_ACTIONS` too was not taken: still forgeable, and wrong for a hand run inside a workflow.

### Low

#### R7. `--github-run` can exceed 200 characters after the check passed, refusing an approved apply
- Area: Testability (9)
- Finding: Row 5 builds the receipt as `run <id> attempt <n>, environment <name>`. GitHub environment names can be long (up to 255 characters), and row 2 caps `--github-run` at 200 and exits 2. The check step accepts any name without a control character, so an approved job could pass the check, wait for its reviewer, and then fail in `Deploy apply` with a usage error. Nothing is imported, but the approval is spent.
- Suggested fix: Make `check-apply.sh` refuse a name that would push the receipt past 200 characters (exit 2, before approval), and test that case.
- Resolution: Fixed. The cap is 500 characters, above the longest receipt the Action can build (about 450), so the check can't fail after an approval (decisions Q3, amended).

#### R8. The text-report and `appliedAt` tests have two small traps
- Area: Testability (9)
- Finding: (a) Row 2 tests `appliedAt` "between the test's before and after", but the format is to the second, so a `before` taken with sub-second precision is later than a truncated `appliedAt`; the test needs `before` truncated to the second. (b) `--github-run` rejects "control characters" only; the MCP code (`McpCommand.cs:325`) also refuses format, line/paragraph separator and private-use characters in text that lands in a report, so a bidirectional control passes into `Executor`/`Approval` lines (text goes through `TextReport.Clean`, JSON does not). (c) A refused `--apply` run keeps a non-null `approval` (row 2); a reader may take `approval: form` with `status: refused` as approved. Row 8 should say to read `status` and `appliedAt` first.
- Suggested fix: Truncate `before` in the test; use the same character classes as the MCP check for `--github-run`; add the `status`/`appliedAt` reading rule to the docs.
- Resolution: Fixed. (a) The test cuts `before` to the second. (b) `--github-run` refuses the MCP check's character classes, with a bidirectional-control test. (c) Row 2 and the docs say to read `status` and `appliedAt` first.

#### R9. The environment claim is a guardrail, and row 8 does not say who can edit the workflow
- Area: Dev to Test to Prod route (5)
- Finding: Q4 and the Won't build list say the claim proves only that the job named an environment. Anyone who can change the workflow can name an unprotected environment, point at an older pin without the checks, or set `ACTIONS_ID_TOKEN_REQUEST_URL` in `env`. The real control is the protected environment plus a federated credential whose subject is that environment only, and a workflow file that the agent cannot change without review. Row 8's GitHub-Action list names the first two but not the third (the CODEOWNERS paragraph at `docs/GITHUB-ACTION.md:60` exists).
- Suggested fix: Add to row 8 for `GITHUB-ACTION.md` and `examples/README.md`: the workflow files are protected by CODEOWNERS or a ruleset, and the checks of the Action are guardrails against mistakes, not a defence against a workflow editor.
- Resolution: Fixed (decisions Q15). Row 8 adds the protected workflow files and says the checks are guardrails.

#### R10. The Action's new tool needs and the refusal order are not documented
- Area: Testability (9); Unanswered areas (10)
- Finding: (a) `docs/GITHUB-ACTION.md:73` lists the tools the scripts need (`gh`, `git`); `check-apply.sh` adds `curl`, `jq` and `base64` for a self-hosted runner. (b) On a fork pull request with `plan-file` and `apply: 'true'`, the new step fails first on the missing token (or the path) instead of `apply.sh`'s event refusal message (`ActionScriptTests.cs:283`), so the more helpful message is lost; the order of the two refusals is not stated. (c) The receipt (`run <id> attempt <n>`) holds no repository name, so it cannot be followed without knowing the repository; fine for a per-repository report, but say so.
- Suggested fix: Add the three tools to the docs sentence, state the order (event refusal first, or accept the new order and say so), and add one sentence on the receipt's scope.
- Resolution: Fixed (decisions Q14). Row 8 lists `curl`, `jq` and `base64`, the order of the refusals, and the receipt's scope; the receipt now names the repository.

#### R11. Examples are not pinned by a test against the new rules
- Area: Testability (9)
- Finding: Row 8 says the example "already passes both checks", and Test column lists `ExampleWorkflowTests` without a new case. Both `promote-dev-to-test.yml` and `ephemeral-pr.yml` use `plan-file` with `apply: 'true'`. Only the second is implied by "the example"; and nothing stops a later edit from breaking either (for example removing `id-token: write` or changing `path`).
- Suggested fix: Add one `ExampleWorkflowTests` case for each file: the job with the `apply: 'true'` call has an `environment`, `permissions.id-token: write`, and a `path` equal to the folder of `plan-file`. Name both files in row 8.
- Resolution: Fixed. Row 8 has a new `ExampleWorkflowTests` case for both example files.

#### R12. Path comparison uses physical resolution while .NET resolves `..` lexically
- Area: Testability (9)
- Finding: Q5 claims "anything but the same folder gates other bytes than those imported". `cd`/`pwd -P` resolves symbolic links and `..` physically, but `deploy apply` takes the folder from `Path.GetFullPath(planFile)`, which collapses `..` lexically (as the repo found in M28). A spelling such as `x/A` for `path` and `link/../A/plan.json` for `plan-file`, where `link` is a committed symlink to `x/y`, compares equal in bash but imports from `A`, not `x/A`. It needs a symlink in the repo and a spelling set in the workflow, so the reach is small.
- Suggested fix: Refuse a `plan-file` or `path` that holds a `..` segment, or normalize both lexically the way .NET does before the `cd`; add a test with a `..` spelling.
- Resolution: Fixed (decisions Q5, amended). A `..` segment in either input is refused, with a test for each.
