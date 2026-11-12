# Decisions: m88-drift-choices

The change itself is item M88 of the fix plan after trial 3, approved by a Fable plan review and started by the owner's "Start the build" (2026-11-11). The owner's four answers to that plan's questions (Q1 "One yes per deployment", Q2 "Hide by default", Q3 "Keep out of Git", Q4 "Rerun it", 2026-11-11) don't touch `drift`'s comparison, so none applies here. Every decision below was made by the building session inside the fix plan, as the owner's direction of 2026-11-11 asks ("as autonomous as possible").

## Round 1 (planning)

### Q1. A global choice: leave it out by id, or compare it by name?
1. Leave type 9 out by id, in `Dropped`, and name it in `notCompared` like tables (recommended)
2. Compare it by name through a read of `GlobalOptionSetDefinitions`

Decided: 1, as the fix plan says. Option 2 is a metadata read (Web API, no `$filter`, read one at a time by MetadataId or name, per Learn, verified 2026-11-11) that the FetchXML reads of `drift` can't make; it is listed for 1.0.x. Type 9 is below 10000, so it is left out in pairs compared by name and by number alike.

### Q2. A setting definition: how is it left out, and with what line?
1. Count it by name with the counted kinds, with its own middle text (recommended)
2. A new line format and a new list for it
3. Leave it out by id, with no count

Decided: 1. `CountedByName` already leaves a kind out by name and counts it in every environment of the pairs compared by name; a second reason text keeps the reuse and gives the line the fix plan wrote. The reason differs from the others' (the definition's owner, not a new id), so the line says so. Option 3 would hide a custom setting definition that is really missing; the count keeps it visible.

### Q3. `variable-value` for a value the settings file set
1. Stays drift; the docs say why it appears after an import with a settings file and to check each such item against the file (recommended)
2. Information, or a `drift --settings <file>` that treats the file's values as expected

Decided: 1, as the fix plan says. `drift` can't tell a value the file set from a changed one, so making it information would hide real drift; option 2 is for 1.0.x.

### Q4. Which name replaces `settingdefinition` in the test of two definition names under one number?
Decided: `customapi`, a definition name nothing leaves out, so the test keeps its purpose (two components that share a number but not a definition name never match) and still sees a `component-missing` item.

### Q5. The words "and that value's record" in `docs/COMMANDS.md`
Decided: removed while that sentence is rewritten. `Keep` leaves out type 381, the variable's value record, so it never shows as a component item; the sentence would promise an item that does not come.

## Log

- 2026-11-11: started by the building session (Opus) in worktree branch `claude/m88-drift-choices-acc83d` at `b065f23` (`origin/main`). Depth `standard`: one Core class, one test class, one docs section.
- read on 2026-11-11: `reports/trial-3/chain/chain-rules.md`; the fix plan's M88 section, section 3's opening, sections 4 and 5; `AGENTS.md`; `CLAUDE.md`; M85's `plan.md` and `decisions.md` (format); `src/PowerOps.Core/Drift/DriftCompare.cs`; `tests/PowerOps.Core.Tests/Drift/DriftCompareTests.cs`; the `drift` section of `docs/COMMANDS.md`; the trial record's lines N9 and P7 (the tracked `record.md` only, not its evidence).
- grep on 2026-11-11: `DriftCompare` is called only by `DriftCommand` and `DriftReport`; `Dropped` and `CountedByName` are private; no golden holds type 9 or `settingdefinition`; `settingdefinition` appears in `src` and `tests` only in `DriftCompareTests.cs` and `DriftCommandTests.cs` (a pair compared by number, unchanged).
- 2026-11-11: `powerops-researcher` (Sonnet, Learn only, offline) on the claims: type 9 is "Option Set" (supported); whether a global choice's id survives an import (not on Learn); what a setting definition is and that a maker can add an existing one to a solution (supported); its import behaviour when the target already holds Microsoft's managed definition (not on Learn); `GlobalOptionSetDefinitions` is a Web API metadata path with no `$filter` (supported; "not FetchXML" inferred). Recorded under Assumptions.
- 2026-11-11: `docs/ecosystem/compatibility.md` has no row for the `solutioncomponent` read, and `changes.md` no open entry for `drift`.
- 2026-11-11: plan review under the model routing (a `fable-reviewer` subagent, Fable), round 1: REJECTED with four findings, all applied in `plan.md`: (1, Medium) the `notCompared` description in `docs/schemas/drift.schema.json` lists the left-out types and counted kinds, so it gains 9 and `settingdefinition` (row 5a, description only; Won't build now rules out a change of the schemas' shape only); (2, Medium) test (d) also holds an `appsetting` on both sides and spells the setting definition `SettingDefinition`, so it pins the casing, the per-name middle text and the line order, and Done when 2 says the name prints as read; (3, Low) line numbers corrected and members named; (4, Low) the definition name is cited as observed once in trial 2 (M76 F40/D5).
