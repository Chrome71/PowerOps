plan-hash: 6f88bb73a619b796e2443cf4be3e9945004f90e681da88320412727eb08cd483
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read (`powerops plan facts` was not run: no confirmation that `pac` is signed in, and `tenant: offline`).
Tools checked: `powerops` (with `plan status` and `plan sign-off`) and `dotnet` are on PATH; the `fable-reviewer` and `powerops-researcher` agents are in this session's agent list; the leak scan is R1.
Checklist items 1 to 8 (solution and publisher, connection references and variables, licences, DLP, route, managed or unmanaged, delegation, security roles) are not applicable: the plan changes PowerOps's own `drift` comparison and docs, makes no tenant call and names no app, table, flow or environment. Open questions: 0; questions for your admin: 0.
The plan's line references (`DriftCompare.cs:94`, `:126-128`, `:222-224`, `:233-235`; `DriftCompareTests.cs:365-373`; `drift.schema.json:52`; `docs/COMMANDS.md:371`, `:379-380`, `:385`, `:402-404`) and its grep claims (callers of `DriftCompare`, `CountLine` and `TypeLine` in the test file) match the worktree.

## Findings

### High

No findings.

### Medium

#### R1. The leak scan and the chain rules are not in the worktree
- Area: 12. Tools
- Finding: Done when 6 and step 4 require "the leak scan" but name no command, and Constraints cite `reports/trial-3/chain/chain-rules.md`. `reports/` is excluded from git (`.git/info/exclude`), so neither `reports/leakscan.sh` nor the chain rules exist in the worktree the build runs in; both exist only in the main checkout. Assumptions does not say so, and an executor in the worktree cannot run the check as written.
- Suggested fix: name the command as M85 did (`bash <main checkout>/reports/leakscan.sh origin/main HEAD`, with the control found and 0 hits as the pass), and add to Assumptions that the leak scan and `chain-rules.md` live in the main checkout's git-excluded `reports/`, outside the worktree.
- Resolution: open

### Low

#### R2. The plan holds URLs
- Area: URLs, GUIDs, emails and names
- Finding: the first Assumptions bullet holds three `<url>` links (Microsoft Learn pages), marked `plan-status: allow`. They are public documentation, not tenant values, but the checklist reports every URL.
- Suggested fix: keep them if the allow marker is the intended exception for public sources, and say so in `decisions.md`; otherwise cite the page titles only.
- Resolution: open

#### R3. Goal and Done when 3 count the trial's items differently from the change
- Area: 9. Testability
- Finding: the trial gave four items (global choice `component-missing` and `component-extra`, setting definition `component-missing`, one `variable-value`). The plan removes three and keeps one, yet Goal says it "stops the first two ... keeps the third as drift" (read literally, the third is the setting definition, which the plan removes), and Done when 3 says "two of the four items go, two stay". The `flow-missing` in Done when 3 and test (d) is not one of the trial's items.
- Suggested fix: in Goal, say the global choice's two items and the setting definition's item go and the `variable-value` stays; in Done when 3, say "three of the trial's four items go and `variable-value` stays; a missing flow, added as a control, still gives `flow-missing`".
- Resolution: open

#### R4. Type 9 by name is tested only with ids on both sides
- Area: 9. Testability
- Finding: Done when 1 says type 9 gives no item "by name and by number". Row 4 (a) adds `9` to the theory `Types_whose_ids_change_on_import_are_left_out_of_the_component_kinds`, whose snapshots have no agents, so it runs only pairs compared by number; test (d) covers a pair compared by name only with a type 9 component on both sides. A pair compared by name with type 9 on one side only is not tested.
- Suggested fix: in row 4 (a), also run the theory's one-side-only cases with `agents: []` on both snapshots, or add that case to test (d).
- Resolution: open

#### R5. Why a global choice gets no count is not recorded
- Area: 10. Unanswered areas
- Finding: Q2 rejects leaving a setting definition out "with no count" because it "would hide a custom setting definition that is really missing", but Q1 leaves type 9 out by id with no count, so a global choice that is really missing from the target (for example after a failed import) now gives no item and no count. The trade is not weighed in `decisions.md`.
- Suggested fix: add to Q1 why no count is acceptable for type 9 (the same treatment as tables, apps and site maps; a name comparison is 1.0.x), or count it.
- Resolution: open

#### R6. Test (c)'s "other order" is ambiguous
- Area: 9. Testability
- Finding: row 4 (c) says "in the other order none either" and gives `1 Dev: 0, 2 Test: 1` for it, which means the component held only by the second snapshot, not the snapshots swapped (that would print `1 Test: ..., 2 Dev: ...`).
- Suggested fix: write "and with the component only in Test" instead of "in the other order".
- Resolution: open
