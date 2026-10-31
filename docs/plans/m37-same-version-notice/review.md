plan-hash: 9b4d129997bca952878726f4e6ed678766c1eecf8bae0a06e1b3910a57255e0e
model: claude-sonnet-5-5
fresh-context: yes

Environment facts were not read: `powerops plan facts` was not run, because the request did not say the user confirmed a `pac` sign-in, and the plan's `environment:` is `null` anyway. No `pac`, `az` or tenant call was made.

Checklist items 1 to 8 do not apply: the plan changes this repo, not a tenant, and no solution, publisher, connector, table, role or environment is involved. Items 9 and 10 apply and are covered below. The review checked the plan against the code at `872389b`.

Checked and found right:

- Line numbers and claims: `DeployPlan.cs` 56-72 (kinds and `Order`; `SeverityOf` is 74-79, its `_` arm at 78), 89 (`Entry`), 185-188 (the `AddUpgrade` call), 207-228 (`Prepare`) and 362 (`Render`); `DeployPlanFile.cs:10` (`ReviewedPlan` holds the schema version, tool, hash, target, blocked and inputs, and nothing of the items); `docs/schemas/deploy-plan.schema.json:100` (the `kind` enum, last value `components-not-checked`); `docs/COMMANDS.md:83` (the sentence that lists the information kinds); `A_skip_checks_no_settings_dependencies_or_flows_but_an_upgrade_does` at `DeployPlanTests.cs:208`; `The_seed_upgrade_matches_the_golden` at `DeployCommandTests.cs:562`. `DriftLiveReader.ReadTarget` reads flows, variables and references for every zip's solution whatever the action (`:39`, `:57-60`), and `SnapshotFile.Write` puts all three lists in the snapshot that feeds `snapshotSha256`, so the item's inputs are hash inputs and the item needs no read. A same-version skip gets no settings, dependency or flow output (`Build` guards those with `Action != Skip`).
- The two sides use the same keys. The zip side is `FlowState.CloudFlows` (category 5, ids parsed to `Guid` in `Prepare`), `EnvironmentVariables` by the definition file's `schemaname` and `ConnectionReferences` by `connectionreferencelogicalname`. The target side is read 4 (`category eq 5`), read 5 (`schemaname`) and read 6 (`connectionreferencelogicalname`), and the parsers' own uniqueness checks for names are case-insensitive. I found no input where a component present on both sides is reported: ids compare as `Guid`, names ignore case, and a classic workflow, business rule or desktop flow is in neither list. The seed fixture, which is an unpacked-layout export zipped (its flow comes from `Workflows/*.data.xml`, its references from `Customizations.xml`), and the recorded Web API captures agree with this.
- Consumers: in `src/` only `DeployPlan`, `DeployPlanReport` (text, JSON, Markdown, and `Summary`, which counts `block` items) read items or kinds. `deploy apply` stops on a blocked plan file, a target name, a hash, the MCP `approvedHash` and a blocked new plan, none of which read an `info` item. `deploy gate` reads actions and input hashes. The MCP `deploy_apply` form shows the target, hash and zips. `action.yml` and `action/` use only the exit code and the Markdown, `skills/`, `plugins/` and `tests/golden/mcp-tools.txt` name no kind, and the kinds are listed in only two docs places (`COMMANDS.md:83`, the schema). `blocked` is any `block` item (`Build`, line 203) and the exit code is `Blocked ? 1 : 0` (`DeployCommand.cs:131`). The hash text (`Render`) holds no item.
- Trial of the build: I wrote row 1 (the kind, last in `Order`; `Entry.SameVersion`; both directions with `ExceptBy`) in a scratch copy of this checkout outside the repo and ran both test projects. Core: 1635 tests, 2 failed, and they are the two the plan names (`Kinds_are_in_the_documented_order` and `A_skip_checks_no_settings_dependencies_or_flows_but_an_upgrade_does`). Cli: 1144 tests, 0 failed, with no golden or schema change yet. So no existing test is broken that the plan does not name. The row 2 case (seed zip against the recorded snapshot at `1.0.0.1`) exits 0 and prints `plan ok`, and the row 4 case (a saved plan that holds the item, then `Apply` without `--apply`) exits 0 with `Kept:skipped` and `status` `planned`, with no `pac` call. The Core cases of row 1 also write and pass with the helpers the plan names (`Plan`, `Snap`, `Sol`, `Target`, `B.Flow`, `B.Env`, `B.Ref`, `HashText`): both directions for the three types, a name in another case, a flow id in upper case, `1.0.0.00` against `1.0.0.0`, a classic workflow, a lower version, an upgrade, `blocked` false, `Flows` empty, and the plan's hash equal to the hash of `HashText`, which holds no kind. The cases for the rows 2 and 4 use `SeedZip`, `Scrub`, `MakePlan`, `Apply` and `Installed` as they are.
- The docs sentences row 3 asks for are true of that behaviour: only a version equal to the installed one, three component types matched by flow id, schema name and logical name, no extra read, no block, the hash unchanged, and a changed flow action, a rename, a value, a connector and every other component type unseen.
- The plan and `decisions.md` hold no URL, GUID, email or person's name. Open questions: 0, and `Questions for your admin` is empty. Areas the log says were not asked (users, data, automation, environments and ALM, constraints) each have an answer in the plan, or `None`.

Not checked: whether the signed-in user's Web API reads can return fewer flows, variables or references than the solution holds (a privilege matter, which needs a tenant read), the pac wording behind the `skip` description at `COMMANDS.md:83`, and Windows and macOS.

Second pass: the plan changed to answer R1 to R5, so the hash above is the new body hash. Each Resolution below was checked against the new text and, for the code claims, against `872389b`. R1 to R4 are fixed and R5 is accepted; none needed a correction, and the edits introduced no new finding (0 High, 0 Medium, 0 Low). What I re-checked: row 2's two items are the ones the case gives (`pops_SeedDataverse`, then `pops_sharedoffice365_6a2f1`); `v0.22.0` is free, after `v0.21.0` of M36; the row 1 sentence on `deletes` is true of `AddUpgrade` (the zip's every workflow against the target's type 29 components); and the new same-name case passes in the scratch copy, giving the zip's item before the target's, also when a variable has the same name as the flow. That order is only a tie-breaker: the report is sorted by solution, kind and subject (the Constraints say so), so flows, variables and references interleave by subject. The edits add no URL, GUID, email or person's name, and `powerops plan status` reports no violation.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. Row 2's golden case gives two items, not one
- Area: 9 Testability
- Finding: Row 2 describes the case as the seed zip against the recorded snapshot at `1.0.0.1`, "whose reference isn't in the zip". Run at `872389b`, the case gives two information items: `pops_SeedDataverse` (the zip has it, the snapshot does not) and `pops_sharedoffice365_6a2f1` (the snapshot has it, the zip does not). The flow and the variable match. The golden will hold both messages, in that order (ordinal subjects, so `pops_SeedDataverse` first). An implementer who expects one item may take the second for a bug.
- Suggested fix: Say in row 2 that the golden holds one item for each direction, with the two subjects.
- Resolution: fixed. Row 2 now says the golden case expects exit 0 and two items, one for each direction, and names `pops_SeedDataverse` (only the zip) and `pops_sharedoffice365_6a2f1` (only the snapshot). Both match the run at `872389b`.

#### R2. Step 3 names build-order item 64 only
- Area: 9 Testability
- Finding: M33, M34 and M36 each have two build-order items in `docs/BUILD-PLAN.md`: the change and the pin PR (58 and 59, 60 and 61, 62 and 63). The plan's last Done line adds a pin PR but Step 3 names only item 64, and gives no value for the milestone row's Release cell (M36's is `v0.21.0`).
- Suggested fix: Name items 64 and 65 in Step 3 (65 comes with the pin PR and the merge commit), and say what the Release cell holds.
- Resolution: fixed. Step 3 gives the Release cell (`v0.22.0`, the next minor after M36's `v0.21.0`; no row uses it yet) and says item 65 comes with the pin PR because it names the merge commit.

#### R3. Row 1 says "the same three fields as `deletes`", but the flow sources differ
- Area: 9 Testability
- Finding: `AddUpgrade` finds a deleted flow by comparing the target's type 29 components with the zip's `Workflows` of every category (lines 271-275). The new item compares category 5 flows on both sides (`e.Flows` against `snap.Flows`). The plan's own sentence and its test (a classic workflow in the zip gives no item) are the right design, but a classic workflow, business rule or desktop flow can be named by `deletes` on an upgrade and never by this item. An implementer who copies `AddUpgrade`'s flow check would break the classic-workflow test.
- Suggested fix: Replace "in the same three fields `AddUpgrade` compares for `deletes`" with "cloud flows (category 5), environment variables and connection references", and let the docs sentence say the item sees cloud flows only.
- Resolution: fixed. The phrase "the same three fields" is gone. Row 1 says that only cloud flows count, on both sides, unlike `deletes`, which checks every workflow of the zip against the target's type 29 components (true of `AddUpgrade`), and row 3 asks the docs to say "cloud flows only, not other workflows".

#### R4. Two items can have the same kind and subject, and their order is not fixed
- Area: 9 Testability
- Finding: A flow re-created under the same name has a new id, so it gives a zip-side item and a target-side item with one subject, told apart only by the message. The sort key (solution order, kind, ordinal subject) ties, and a stable sort then keeps the emission order, which the plan does not fix ("both ways"). The golden and any test on the order depend on it.
- Suggested fix: State the emission order in row 1 (zip side before target side, flows, then variables, then references), and add a case with the same name and different ids that pins it.
- Resolution: fixed. Row 1 fixes the order items are added in (flows, variables, references; for each type the zip's side first) and lists a case for a flow with the same name and another id that expects the zip's item, then the target's. I wrote that case in the scratch copy and it passes.

#### R5. A large difference can push later solutions' rows out of the Markdown comment
- Area: 9 Testability
- Finding: `WriteMarkdown` fills one items table for all solutions in plan order and stops at 60,000 characters less a 300 reserve; once it stops, the rest of the items table and the flows table are dropped (`full = false`). Information rows are about 190 characters, so roughly 300 same-version items from one early zip would push a later solution's `block` or `warning` row out of the pull request comment. The summary line (`plan blocked: N blocking items`), the exit code, the text report and the JSON stay complete, and no hash, `blocked` or refusal changes. The plan's remark that the list "is as long as the difference, which is short in the case this serves" holds for a forgotten version bump, not for a zip of another solution state.
- Suggested fix: No code change needed: add to the docs sentence that the Markdown report can leave rows out for a very large difference and that the JSON is complete (the existing sentence about the 60,000 characters is in the paragraph after it), or accept the limit in Assumptions.
- Resolution: accepted. Any long list of items does this. The report says how many rows it left out, the summary still counts the blocking items, and text and JSON are complete. Assumptions now records it, with "about 300" and "nothing changes in `WriteMarkdown`", which matches the code.
