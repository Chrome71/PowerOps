plan-hash: 5873bc0ef023b9787df231d35a4e933d275ad943a5035993d47c91e65870b177
model: claude-sonnet-5-5
fresh-context: yes

Scope: `plan.md` and `decisions.md` of `m8-drift`, `AGENTS.md`, the M8 row, the Phase 3 section (environments, "What PowerOps reads", research basis), decisions 9 to 14 and build-order tasks 22 to 24 of `docs/BUILD-PLAN.md`, the house example `docs/plans/m7-settings-live/review.md`, the two earlier reviews of this plan (`git log -p`), and the code the plan names: `Program.cs`, `SettingsCommand.cs`, `PacRunner.cs`, `PlanFacts.cs`, `Settings/FlowState.cs`, `Output/JsonOutput.cs`, `Output/MarkdownReport.cs`, `OutputSchemaTests.cs`, `tests/Shared/Golden.cs`, `docs/schemas/README.md`, the `tests/fixtures/pac` samples and their `NOTICE.md` entry. `powerops plan status` reports `approved`, `openQuestions` 2 (the two admin questions; the `Open questions` section is empty), `review: done` (already in the header, left as it is) and one warning: the earlier review is stale for this hash. `powerops plan facts` was NOT run and `pac` was never called: contacting the tenant is forbidden for this review, so there are no environment facts and the overlap check of item 1 could not be done. No Microsoft Learn page was re-read; where a finding rests on Dataverse or `pac` behavior that the plan's Sources do not state, it says so and asks Step 1 to settle it. Tenant data: no URL with a scheme, GUID, email or person's name in `plan.md` or `decisions.md` (the Sources are public documentation paths and the fake environment name is not a finding).

Checklist items that do not apply, as in `m7-settings-live`: 1 (no solution or publisher is created; the existing seed, prefix `pops`, is only read), 2 (no hard-coded value: environments come in as `--env`, values are hashed, and a leak through fixtures is R8), 3 and 4 (no connector or table is added; the plan states a licence fact as settled while the admin question is still open, which is the same open question as in `m7-settings-live` and is not raised again; DLP does not apply to reads), 5 and 6 (the route is the user's Dev to Test and the tool only reads; managed in Test is shown as information by Q3), 7 (no app or delegation; the 5,000-row page is the only volume limit and the plan handles it), 8 (no roles are created; the reading account's role is the first admin question). Items 9 (testability) and 10 (unanswered areas) apply and carry the findings. Beyond the checklist the plan was checked against the BUILD-PLAN row and decisions (the deviations the plan states, no `--show-values`, flows from `workflow`, no `pac solution list`, the layer fallback, match Q2 to Q4 and are not findings; R14 lists the ones it does not state), against AGENTS.md (read-only, JSON contract and the "Add a command" recipe, `pac` as a subprocess with list arguments, smallest change, tests first, no tenant data in fixtures), and against the existing code. The plan's Core/CLI split, the `Program.Run` runner seam, the `Map` and prefix entries in `OutputSchemaTests` (no clash between `drift-`, `diff-` and `snapshot-`) and the "Update goldens" recipe all fit the code as it is.

Second review (14 findings, 1 High, 4 Medium, 9 Low, written for plan hash 1683b150), re-checked against the current plan. Eleven are really resolved in the text: the `--env` path rule (R1: a snapshot path ends in `.json` or exists, a URL is an environment, and a URL test is in Step 4), the type 29 exclusion (R2: only rows in the cloud-flow list are left out, with a test; but see R4 below), the join through `solutioncomponent` for reads 4 to 6 (R3), building only the layer mode Step 1 finds (R4), the fallbacks for assumptions 1 and 4 (R6), the failed layer read as exit 3 (R7), the snapshot's own `schemaVersion` (R8), names printed and saved (R9), the reason for not using `list-cloud-flows` (R11), single-environment checks running wherever the solution is installed (R12), and editability of the Test flow in Step 1 and item 3 (R13). Three are resolved but leave something behind. R5 (scrub list): the list and the user's read are in Step 1, but the grep rule as written cannot pass (R8 below). R10 (BUILD-PLAN update): Step 5 now names the reads, but other deviations are still missing (R14 below). R14 (the Q2 wording) is fixed in the plan and the log, but `decisions.md` still records the user's Q2 answer as "safe to keep as CI artifacts" with only a log line beside it, and the plan was changed after the user's approval without the user seeing it; sign-off in `powerops-plan` should show that narrowing. The first review's 16 findings were re-checked by the second review and are not audited again here.

## Findings

### High

No findings.

### Medium

#### R1. Step 1 runs "the same text" as `DriftQueries`, which is built in Step 2 from what Step 1 finds
- Area: 9. Testability
- Finding: Data says `DriftQueries` owns the FetchXML and that "Step 1 runs the same text: reads 3 to 7 are templates". Step 1 says "Claude first writes the seven reads as commands ... Reads 3 to 7 are templates from `DriftQueries`' FetchXML", and it runs "before any code". `DriftQueries` is created in Step 2, test-first, and its read 7 text depends on what Step 1 learns (which filter the layer table needs, the component names, batched or per component). So the first step needs code that only exists after it. Step 2's "exact text of each query" tests also only repeat the text, so they do not show that the capture used the same query.
- Suggested fix: Say that Step 1's commands are written by hand (stored in the capture folder or its NOTICE entry, with the solution id left as a placeholder), and that Step 2 copies the exact text into `DriftQueries`, with the test comparing against those stored commands. Drop "from `DriftQueries`" in Step 1 and "the same text" in Data, or say "the same text, copied after Step 1".
- Resolution: fixed (Step 1 queries stored in tests/fixtures/pac/drift/queries/; Step 2 copies them and a test compares)

#### R2. Live item 1 expects the same variable value in both environments, but Step 1 sets a special value in Dev only
- Area: 9. Testability
- Finding: Item 1 starts from "the same environment variable value" in Dev and Test. Step 1 says that before the capture the user sets the seed variable's current value in Dev "to a made-up test value with a space, a comma and a line break". Test was imported from Dev's export before that edit and holds the earlier value, so after Step 1 the two values differ and `drift` correctly reports `variable-value` and exits 1 at item 1. The plan does not say to set the same value in Test, to restore Dev afterwards, or whether the maker portal accepts a line break in a text variable at all (a settings file can carry one).
- Suggested fix: In Step 1, have the user set the made-up value in Test too (or capture it through a Test import with a settings file), and either keep it in both until Step 6 or restore both and say so. If a line break cannot be entered, say which of space, comma and line break the capture covers and test the line-break case on a hand-written capture.
- Resolution: fixed (the same made-up value set in Dev and Test and kept until Step 6; line-break fallback named)

#### R3. A Dev unmanaged solution and a Test managed one may not list the same rows in `solutioncomponent`
- Area: 9. Testability
- Finding: Item 1 compares the unmanaged seed in Dev with the managed seed in Test and expects no `component-missing` or `component-extra`. Assumption 2 assumes the same type and id for each root component and "their subcomponents" in both environments, and its fallback is to leave whole types out. The Sources do not say what `solutioncomponent` lists for an unmanaged solution: I expect the components that were added to it, while a managed import creates rows for everything it brings (columns, forms, views and so on). If that holds, every Dev-to-Test comparison shows a block of `component-extra` and item 1 fails by design, not by a mistaken id. Dropping those types as a fallback would also blind `drift` to them between two managed environments (Test and Prod), which is its main use. This is a risk, not a confirmed fact: Step 1 can settle it, but the plan has no answer if it is true.
- Suggested fix: Add to Step 1 a count of `solutioncomponent` rows per type in Dev and in Test. Add a second named fallback to Assumption 2 for an unmanaged and a managed environment: compare components only between environments with the same managed flag, or only the types listed on the unmanaged side, and say in the README which. Keep the type-level fallback for ids that differ.
- Resolution: fixed (Step 1 counts rows per type; root-component and same-managed-flag fallbacks named)

#### R4. `componenttype` may print as a localized label, and the snapshot, the exclusions and the layer map all need the number
- Area: 9. Testability
- Finding: The snapshot's `type` is "the `componenttype` number", the table excludes types 29, 380, 381 and the connection reference by number, and read 7 may need a component name for each number. Assumption 1 covers only that "a state and a boolean" print as mappable values (a few labels). Step 1 records whether `componenttype` prints as a number or a label, but nothing says what happens if it is a label. The option set has over a hundred values, and labels follow the signed-in user's language, so a table that maps labels to numbers would break for another user or CI account. Step 2 cannot be written before this is known, and the go-back list does not include it.
- Suggested fix: Add `componenttype` to Assumption 1's wording, with a named fallback: store the label text as given (the `type` field becomes a string), keep the exclusions to the few labels Step 1 recorded, and say in the README that snapshots from users with different languages do not compare. Or stop M8 and revise the row, as for the other go-back items.
- Resolution: fixed (componenttype added to the go-back Assumption with the label fallback)

### Low

#### R5. "A flow that is off" exits 1 only when it differs from the first environment
- Area: 9. Testability
- Finding: The BUILD-PLAN row and the plan's done-when list "a flow that is off" as a case that exits 1, next to "an unbound connection reference". The table makes `reference-unbound` a check in every environment, the first included, but `flow-state` fires only when the states differ. A flow that is off in both environments, or off in the first, gives no item and exit 0. That may be intended (a Dev flow is often off on purpose), but the plan does not say so and the done-when wording reads the other way. A test written from "a flow that is off exits 1" has two readings.
- Suggested fix: One sentence under the table: a flow that is off in every environment is not drift (only a difference is), and the done-when case means off in one environment and on in the first. Or add a single-environment kind, if the user wants it, which is a decision.
- Resolution: fixed (sentence added: off everywhere is not drift)

#### R6. A variable value that differs by design always exits 1, and the user was never asked
- Area: 10. Unanswered areas
- Finding: Decision 12 says settings files "are meant to differ between environments" and that the useful question is whether an environment matches what was deployed. The plan compares each environment's value with the first one's, so any solution whose Dev and Test values differ (the case PO007 exists for) exits 1 on every run. The plan documents this as out of scope and tells JSON users to filter by `kind`, which is where the earlier review left it. It is still a product choice that none of Q1 to Q4 asked about, and it limits `drift` as a CI gate and in M10's "no drift apart from the managed state" check (which only works because the seed's value is the same in both).
- Suggested fix: Put it to the user as a short question at sign-off (as planned; or `variable-value` as an information item; or a flag, which is new scope), and record the answer in `decisions.md`. No plan change is needed if the user keeps it as it is.
- Resolution: fixed (asked the user (Q5): kept as drift, as the row says)

#### R7. The sign-in hint sends the user to `pac org select`, which the plan says is not needed
- Area: 9. Testability
- Finding: Automation step 2 adds "the sign-in hint `plan facts` prints" to the first read's failure. `PlanFacts.SignInFix` reads "Sign in with pac auth create, then pick the environment with pac org select." Drift pins each call with `--environment`, the plan says no `pac org select` is needed, and Assumption 4's fallback treats `pac org select` as a write to `pac`'s settings that needs a new decision. The hint contradicts the design.
- Suggested fix: Use a drift-specific hint (sign in with `pac auth create`; check that the account can read the environment), or reuse only the first half of `SignInFix`. Add the text to the CLI test for a failed first read.
- Resolution: fixed (drift-specific sign-in hint)

#### R8. The Step 1 grep rule cannot pass with fake values, and read 7 may carry tenant data
- Area: 2. Environment variables (tenant values), 9. Testability
- Finding: Step 1 scrubs ids, URLs and names to "fake values", then greps the folder for URLs, emails and GUIDs "not on the allow list" and requires no hits. The allow list is the seed's own component ids, so a fake URL, a fake email or an all-zero GUID (the style of the existing `org-who.txt`) is a hit, and the check fails on its own scrubbed output, or the builder quietly widens the list. Separately, read 7 is the only read with no named columns, and `msdyn_componentlayer` rows hold the publisher name and the changed data of the layer (which for a flow can carry a hard-coded URL or mailbox).
- Suggested fix: Name the allow list as the seed's component ids, the all-zero fake GUID pattern and the fake host and email domain, and say what fails. Name read 7's columns in Data (the layer's name and order only) and say the changes column is never selected.
- Resolution: fixed (allow list named; read 7 selects three columns, never msdyn_changes or msdyn_componentjson)

#### R9. The value hash depends on the operating system's line endings
- Area: 9. Testability
- Finding: The hash is "the SHA-256 of the current value's UTF-8 bytes". Step 1 sets a value with a line break. `pac` prints through the console, so a line break may come out as `\n` on Linux and `\r\n` on Windows. A snapshot saved on one system and compared on the other (a CI runner against a developer's saved file, or M9's artifacts) reports `variable-value` for an unchanged value. The same applies to trailing spaces that a printed table may trim.
- Suggested fix: Say the parser turns `\r\n` into `\n` before hashing, and add a Step 2 test that two line-ending forms of one value give the same hash. Record in Step 1 whether `pac` trims.
- Resolution: fixed (line endings normalized before hashing; test; Step 1 records trimming)

#### R10. "No rows" is not told apart from an error, and the exit-code assumption is not a go-back one
- Area: 9. Testability
- Finding: Read 2 treats no row as "not installed" and skips the other reads. The existing `pac` samples show that `pac` prints its errors on stdout (see the `list-cloud-flows-bad-id` entry in `NOTICE.md`), and the plan's assumption that `pac` exits non-zero on a failed FetchXML is not among the four go-back ones. If a failed query exits 0, an error text parsed by a lenient reader gives zero rows, so a failed read 2 looks like "solution not installed" and the run can report `solution-missing`. Step 2 tests that "another shape" is rejected, but not that the empty-result output is recognized positively.
- Suggested fix: Say in Step 2 that every parser must recognize its output (header or table) and treat only a recognized empty result as no rows, and add a test with an error text and exit code 0. Keep the exit-code check in Step 1.
- Resolution: fixed (parsers recognize their output; error text with exit 0 test)

#### R11. Flow, variable and reference names go into the Markdown and text reports unescaped
- Area: 9. Testability
- Finding: Subjects are user-chosen names (a flow name can hold `|`, backticks, a line break or `@` mentions), and the Markdown report is meant for PR comments. The plan does not say how the Markdown table and the one-line-per-item text avoid breaking. `MarkdownReport.Escape` already exists and `DiffReport` uses it with a length cap.
- Suggested fix: Say in Step 3 that the Markdown report reuses `MarkdownReport.Escape`, the text report replaces line breaks in names, and add one test with a name holding a pipe and a line break.
- Resolution: fixed (MarkdownReport.Escape and line-break replacement, with a test)

#### R12. Live item 3 leaves Test with an unmanaged layer, and later runs expect it clean
- Area: 5. Dev to Test to Prod route
- Finding: The edit in item 3 puts an unmanaged layer on the managed seed flow in Test. Nothing says to remove it afterwards. A repeated item 1, and M10's "no drift apart from the managed state" check, would fail on that layer. The same applies to the layer read in Step 1 "after editing the flow", which comes before the live run.
- Suggested fix: Add to Step 6 that the user removes the unmanaged layer (or re-imports the managed seed) after item 3, and that a run showing no drift is recorded before and after.
- Resolution: fixed (Step 6 removes the layer and re-runs for no drift)

#### R13. The BUILD-PLAN update in Step 5 still misses some deviations from the row
- Area: 9. Testability
- Finding: Step 5 lists the read and layer changes. It does not list: the exit code 2 for a solution installed nowhere and the extra exit 3 causes (an unreadable snapshot file, `pac` not found), which the row words as "a `pac` read failed"; the capture folder `tests/fixtures/pac/drift/` against the row's `tests/fixtures/pac/`; and the README's "Built so far" line, which names the commands. A later reader of the row would not know them.
- Suggested fix: Add these three to the Step 5 list.
- Resolution: fixed (Step 5 lists the exit codes, the fixture folder and the README list)
