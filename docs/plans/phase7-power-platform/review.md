plan-hash: 527940df7f56c69d2598f657cd5799d64a1f98f9a84498dbecfc1c2f55848769
model: gpt-6-astra
fresh-context: yes

The review ran three rounds and ended with no High or Medium finding open. The independent reviewer's verdict in the third round was agree. Round 1 raised R1 to R13. Round 2 settled most, reopened R9, R10 and R12, and raised R14 to R18. Round 3 confirmed all eight outstanding findings fixed and found nothing new.

The plan-hash was updated after the person's answers to D1 to D11 were recorded in Open questions and Steps, as in the earlier reviews. Reviewed hash: c185d33d8cbdf26e806ebbdca18796c4f4661e3773d66ce61e4a6a82c7261298.

The reviewer could not write files, so the planning session wrote this file from its replies and added the resolutions. The plan's author checked each finding against the repo at the M39 merge and the Learn page it cited, then changed the plan.

The fixes changed these parts of the plan: Goal (Completion), Data (the pinned-roles section, M40 to M42, M45 to M49, Held, Won't build), Automation, Environments and route, Constraints, Assumptions, Open questions, Steps. `decisions.md` gained Q9 to Q11.

The reviewer ran in a fresh context, changed no file and contacted no tenant. Environment facts were not read: `plan facts` was not run, and the plan targets no environment.

Checklist items 1 to 4 and 6 don't apply: the plan has no solution, publisher, connector or managed state. Items 5, 7 and 8 appear only as guidance the new skills give other plans. Items 9 and 10 drive the rest. The plan folder holds no GUID, email or person's name.

Open questions after the fixes: 13 (11 named decisions, 2 for the admin). The person answered D1 to D11 on 2026-11-02, taking each recommendation. Left at the end: 0 named decisions, 2 questions for the admin.

## Findings

### High

#### R1. New MCP gate routes bypass the hooks
- Area: 5 and 9
- Finding: `ImportCall` reads MCP calls only for `solution_import`, and the published hooks match only that tool, so new tools never reach the gate.
- Suggested fix: change both matchers, add a migration note, test each route.
- Resolution: fixed: M40 widens `ImportCall`'s MCP recognition to the four new tools, names all five in both clients' matchers in `docs/HANDOFF.md`, updates `HandoffDocTests`, and adds a migration line, per-tool unit tests and a by-hand toy-server check.

#### R2. The wildcard rule is an Error that can fail a working site
- Area: 9
- Finding: the rule fires on `fields = *` alone, ignoring whether the Web API is enabled (off by default) and exemptions an export can't show.
- Suggested fix: make it a Warning, with quiet tests for a disabled API, explicit fields and `UseFieldsFromView`.
- Resolution: fixed: M45's rule is a Warning that fires only where the table's Web API is enabled, with those quiet tests; M45 has no Error now.

#### R3. The agent transport Error is overbroad and lacks its input
- Area: 9
- Finding: "a connector not on the Streamable protocol" covers ordinary REST connectors, and the declaration sits in a definition the reader never reads.
- Suggested fix: limit the rule to MCP tools, name the input, report "not checked" without it.
- Resolution: fixed by a cut: M48 drops the transport Error for a review item in `references/copilot-studio.md`. Its one Error is an MCP tool on an agent with generative orchestration off.

### Medium

#### R4. Naming Power BI skills gives no usable hand-off
- Area: 5 and 9
- Finding: `powerops-implement` still demands a Dataverse Dev environment and solution membership, which can't apply to PBIP files.
- Suggested fix: add a small Power BI branch, with completion for Power BI-only and mixed plans.
- Resolution: fixed: M42 adds that branch: solution rules are "not applicable", the build is local PBIP work, service writes need a yes, and mixed plans finish each part by its own rule.

#### R5. "JSON schemas don't change" contradicts the `diff` output
- Area: 9
- Finding: agent tools and instruction risks need new values in the closed enums of `docs/schemas/diff.schema.json`.
- Suggested fix: name the new values, update schema and goldens, decide compatibility.
- Resolution: fixed: M47 no longer names code apps as a kind, so stage one changes no schema. M49 names its agent values and updates schema and goldens under new decision D9 (see R14).

#### R6. The reader milestones don't define their input contract
- Area: 9
- Finding: a standalone Pages site wouldn't load without a solution manifest; M46 names no zip mapping, and its dashboard rule needs an unread input.
- Suggested fix: state roots and zip mappings, test each layout from the CLI, cut form parsing no check uses.
- Resolution: fixed: M45 reads one root, the solution layout, with zip paths stated from a packed fixture. M46 reads folders and `customizations.xml` in a zip; form parsing is out. Each layout gets a CLI-level test. The dashboard rule was fixed by a cut: it became a plan line in M42.

#### R7. Absent role metadata is read as absent permission
- Area: 8
- Finding: the role warning can't tell "no read privilege" from "role not exported"; the `alm-sample` app module maps two roles missing from its `Roles/` folder.
- Suggested fix: resolve every mapped role first, else say "not checked".
- Resolution: fixed by a cut: M46 drops role parsing and the rule; a plan line and review item in M42 replace them (see R16).

#### R8. D1 and its evidence cover only part of the change
- Area: 10 and 9
- Finding: the ledger changes the `plan.md` layout both pinned roles read, but D1 named only the Verifier and the proposed case exercised neither; an unchanged path proves nothing about behaviour.
- Suggested fix: cover both roles; if an input changes, run the full pass on the new layout.
- Resolution: fixed: the pinned-roles section and D1 cover both roles, and the D6 cases are said to measure `powerops-review-plan` only. If D1 calls the ledger an input change, M41 runs both roles' full pass on the ledger layout and renews the decision first.

#### R9. The question and prototype flows conflict with the skill's rules
- Area: 9 and 10
- Finding: `quick` had to fit new data questions into a round already full, and the fast path omitted the prototype offer.
- Suggested fix: make data questions conditional, offer the sketch on both paths without a new gate, add a scenario test.
- Resolution: fixed: the first fix (one data question in a `quick` round, sketch on both menus) was reopened in round 2: `standard` and `deep` still asked six questions in one round, over the cap of three. Round 3 confirmed the second fix: successive rounds of at most three, and a `standard` smoke-test scenario.

#### R10. The store decision can stop before a mandatory constraint
- Area: 7
- Finding: first-match order picked a SharePoint list before "SQL is mandated" was checked, and an existing system of record ended the decision unexamined.
- Suggested fix: collect mandatory constraints first, then reuse through a compatible integration.
- Resolution: fixed: the first fix (constraints, then reuse, then store) was reopened in round 2: it still settled constraints "in order" and equated "can't move" with Azure SQL. Round 3 confirmed M41's rewrite: all constraints first, a named integration for existing data, conflicts sent to the person.

#### R11. "No double columns" is a blanket rule
- Area: 7
- Finding: it turns an observed sample preference into a rule, and fixed decimal can lose range.
- Suggested fix: require a type fitting precision and range, with no unconditional `double` warning.
- Resolution: fixed: M42's `power-bi.md` asks for a type that fits each column. The held `pbip check` loses its `double` half; Won't build lists it.

#### R12. Stage-one completion is contradictory
- Area: 10 and 9
- Finding: stage one needs `.msapp` reading while M44 waits on a live fact, and no rule says whether an accepted deferral opens stage two.
- Suggested fix: define completion as outcomes delivered plus deferrals accepted one by one.
- Resolution: fixed: the first fix (Completion: built, or deferred by named acceptance under D11) was reopened in round 2: required facts, optional evidence and experiments were blurred, and Step 9 and M46 disagreed. Round 3 confirmed Environments and route: only F1 is required, F2, F3 and F5 are optional evidence, F4 an experiment.

#### R14. Closed-enum values aren't compatible like new fields
- Area: 10
- Finding: D9 equated new enum values with new fields, but a consumer validating the published schema rejects unlisted values.
- Suggested fix: say so, then version the contract or accept and document the break.
- Resolution: fixed: D9 states the break and recommends `diff`'s `schemaVersion` 2, with costed alternatives: keep 1 with a migration note, or keep agents out of `diff`.

#### R15. "No Error on an absence" contradicts the checks
- Area: 9 and 10
- Finding: D10 contradicted M46's missing-target Error and the existing PO001 and PO004 Errors.
- Suggested fix: separate unavailable evidence from a proven missing component.
- Resolution: fixed: D10 and Constraints split them: evidence the layout lacks gives "not checked"; a missing reference of a kind the layout carries is an Error, as in PO001 and PO004. M46's `GenPageId` half waits for F2.

#### R16. The replacement role review can recommend excess access
- Area: 8
- Finding: "one role per persona, granting read on each custom table" judges single roles against the whole app, though roles accumulate.
- Suggested fix: review each persona's combined permissions against its tasks.
- Resolution: fixed: M42's `model-driven.md` and review item ask that each persona's roles together read the tables its tasks use; roles that split access are quiet.

### Low

#### R13. Several concrete statements need correction
- Area: 9
- Finding: the runner already supports `review-plan`; the Error count and generative-page hand-off were inconsistent; "standard harness" was unexplained.
- Suggested fix: reuse the pass, correct the count, state the exception, name the harness.
- Resolution: fixed: M41 and Automation reuse the existing pass, Compatibility counts Errors correctly, generative pages are the stated exception, and stage two names the rule-based harness.

#### R17. The held rule count and the visual-count policy disagree
- Area: 9
- Finding: the held `pbip check` said "at most seven" but listed eight checks, including the visual count D4 keeps out of rules.
- Suggested fix: drop visual count from the list.
- Resolution: fixed: Held lists seven; D4 keeps visual count a review item.

#### R18. The dashboard cut claims more than the evidence shows
- Area: 9
- Finding: the cut claimed the designer refuses the save, so no export holds an oversized dashboard, which the research never established; "under 20" excluded 20.
- Suggested fix: keep the cut on supported grounds, drop the claim, say "at most 20".
- Resolution: fixed: M46 and Won't build rest on no fixture, an unconfirmed layout and no documented refusing step; the plan line says "at most 20".
