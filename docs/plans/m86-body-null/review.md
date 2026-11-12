plan-hash: c3b3480575da73b8ddf46d1b2ffc0d74a0491e2753bd9cbe3cab3353fe6c8be8
model: claude-opus-5-5
fresh-context: yes

This is a rerun: it replaces the review of plan hash `747e8e14…`, whose five findings were resolved. The resolutions were checked against the current plan. R1 (a failing mutant stops the build and goes to the person) holds in Assumptions. R2 (the 2026-11-11 run gets its own item in `docs/FLOW-TESTS.md`, and the "compared with one real run" sentence stays) holds in Data row 2. R3 (claim only what the run showed) holds in Assumptions, but row 2 now words it a little more strongly; see R2 below. R4 holds: `reports/leakscan.sh`, `reports/trial-3/chain/chain-rules.md` and `reports/trial-3/fix-plan.md` exist in the main checkout, and Done when 6 names the worktree's build. R5 (the Learn addresses in `decisions.md`, kept on purpose) is reported again below, as the skill requires.

Environment facts were not read: the plan's `environment:` is null, the review ran tenant-offline, and nobody confirmed a `pac` sign-in, so `powerops plan facts` was not run.

Tools checked: `dotnet`, `git`, `gh` and `bash` are on PATH; the worktree's build runs `powerops plan status` (status `approved`, 0 open questions, 0 admin questions); the `fable-reviewer` agent file and the `powerops-researcher` project agent exist. The leak scan and the merge protocol are outside the worktree, as Assumptions says.

Code checked against the plan at `b065f23`: the `body` entry is `ExpressionFunctions.cs:26` with `optional: false`; `Expression.Read` returns null for a null target only when `optional` is true and keeps "The property 'body' does not exist." for an object without it; `FlowSimulator.Outputs` gives null for Skipped and throws "has not run" for an action not yet run; the null-outputs lines `:562`, `:599`, `:617`, `:632`, `:642`, `:648`, `:697`, `:739`, `:785` and `:903` match; `Body_is_outputs_body_and_errors_when_there_is_none` is at `ExpressionTests.cs:316-322`; `flowtests/skipped-body` sorts right after `flowtests/capture/solution` in `SolutionDiscoveryTests` (`flowtests/seed` holds no solution); the `alm-makers` `$schema` path works at the same depth; the summary line format matches `TestReport`; the SARIF golden lists only findings, so it stays unchanged for a fixture with none; the eight eval cases each have `solution/current` and `flowtests`.

The plan is a change to PowerOps itself (the flow-test simulator), not a Power Platform solution. Checklist items 1 to 8 (solution boundaries against `plan facts`, connection references and environment variables, premium connectors and licences, DLP, the Dev to Test to Prod route, managed or unmanaged, delegation, security roles) do not apply: nothing is built in or moved to a tenant, and the fixture solution `PowerOpsSkippedBody` (publisher `demopub`, prefix `demo`) is hand-written, never imported and holds only fake values. The form and screen parts of item 9 and the per-product and `mockups/` parts of item 11 do not apply: the plan has no screens and no sketches. Item 11 otherwise: every artefact (the code row, the docs row, the tests, the fixture, the goldens) serves a Done when line, and `Won't build` is present.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The fixture's HTTP address and flow state are not pinned, and two rules could fire on them
- Area: 9 Testability
- Finding: Data row 4 says the HTTP action is "a GET on a `contoso` address" and doesn't say the flow is on. AGENTS.md suggests `contoso.sharepoint.com` as a fake value, and a SharePoint site or Dataverse host in a flow is PO006; a flow whose `.data.xml` has no `StateCode` of 1 is PO011. Either would fail the planned `FixtureRuleTests` no-finding case and change `analyze-fixtures.sarif`, which row 5 expects unchanged. The test would catch it, so the cost is a retry of step 4, not a wrong merge.
- Suggested fix: In row 4, name the address as a host that is neither SharePoint nor Dataverse (for example an `api.contoso.com` path) and say the flow's `.data.xml` has `StateCode` 1, as in `flow-navigation/`.
- Resolution: fixed (row 4 names an `api.contoso.com` address and `StateCode` 1)

#### R2. Data row 2 says the run showed null; Assumptions says it showed no error
- Area: 10 Unanswered areas
- Finding: Assumptions says the live run showed only that `body(x)?['k']` of a skipped action gives no error (the `coalesce` Compose succeeded), and that `body(x)` alone giving null is the simulator's reading. Row 2's new doc item says the run showed `body(x)?['k']` "gives `null` instead of an error". That holds only if the run's record shows the Compose gave the `coalesce` default, which the plan doesn't state. This is the earlier R3 drifting back in one sentence.
- Suggested fix: Make the two agree: either add to Assumptions that the Compose output was the default (if the record shows it), or word row 2 as "gives no error".
- Resolution: fixed (row 2 now says "gives no error", as Assumptions does)

#### R3. `decisions.md` contains URLs
- Area: URLs, GUIDs, emails and names
- Finding: The Research section of `decisions.md` still gives three web addresses next to the page titles. They are public Microsoft Learn pages, so nothing leaks, and the previous review's R5 kept them on purpose; the rule is to report them on every review.
- Suggested fix: None needed beyond the earlier resolution: keep them as public sources (the repo's house style), or drop the addresses and keep the titles.
- Resolution: kept on purpose (as the previous review's R5: public Learn sources in the repo's house style)
