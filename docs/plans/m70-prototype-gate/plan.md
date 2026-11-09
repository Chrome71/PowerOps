---
status: done
review: done
signed-off: 2026-11-05
signed-off-hash: 692154058c3ed167e13fc1a4742f7819ae67d528554dff87085ee5a56b076711
environment: null
depth: quick
supersedes: null
---

# M70: A prototype every UI plan has before sign-off

## Goal

Make the HTML prototype a step every plan with screens goes through, not an option it can skip, and carry it into the build. Today `powerops-plan` offers the sketch as menu letter **D** beside **B** (approve), and both recorded runs skipped it: M42's simulated user chose B and logged `prototype: not chosen` (`docs/plans/m42-product-references/decisions.md:75`), and the 2026-11-04 live run's plan logged `prototype: not chosen` for a canvas app and got none for its model-driven app, which `route.md` gives no sketch at all. The built UI was poor although the solution worked. Phase 7 kept the prototype an offer "without an extra gate" (`docs/plans/phase7-power-platform/plan.md:143`, `:269`); this milestone reverses that choice on the evidence of those two runs. It doesn't add a second approval: the one sign-off refuses until the prototype exists or a person says why not.

1. `powerops plan sign-off` refuses a plan whose `## Screens` names anything (model-driven apps included; decisions Q1). `Screens` names nothing when the plan has no `## Screens` heading, its body is empty, or its first non-blank line starts with "None" ("None." and "None. <reason>" both count); anything else names something, a `### Theme` block alone included, since Q1 counts a theme. On such a plan the last `prototype:` item under the `## Log` heading of `decisions.md` decides, and it must be one of two:
   - `prototype: approved mockups/prototype.html`, with exactly that path and a file that passes Goal line 2. Anyone may write it; `plan status` warns when it doesn't end `(answered by: person)`, as it warns on an agent's sign-off (decisions Q9).
   - `prototype: skipped (<reason>)`, with a non-empty reason, ending `(answered by: person)`. A code app's mock-data first slice is a person's `skipped (mock-data slice)`.

   Everything else counts as missing: no line, a line outside the Log, another path, an empty reason, a skip without the person's mark, `not needed`, and the old `not chosen` and bare `skipped`. A plan whose `Screens` names nothing needs no line, and a `not needed (<reason>)` there stays a note. `powerops plan status` reports the same problems as warnings on a `draft` or `approved` plan. Plans already in progress when M70 ships are checked the same way; signed-off plans are never rechecked (decisions Q5).
2. The prototype checks are deterministic text checks, made after HTML comments are removed (the `route:` line is read from the first comment before that). They catch the honest mistake, as `AgentAnswers` does; they are no sandbox, and the tool never renders the file.
   - Read: a symbolic link, a binary file or a file over 1 MB fails, as in the folder's tenant-data scan; at most 40,960 bytes, and one to eight `data-screen` attributes (decisions Q3).
   - Tags, matched case-insensitively across spaces and line breaks: no `script`, `link`, `meta`, `base`, `iframe`, `frame`, `object`, `embed`, `img`, `picture`, `svg`, `form`, `video`, `audio` or `source`; no `src`, `srcset`, `action` or `on…` attribute; every `href` starts with `#`.
   - CSS, in `<style>` and `style=` values: no `url(`, `@import`, `@font-face`, `image-set(` or `expression(`.
   - Markers: the word "Sketch" in body text outside comments and `<style>`; a line starting `route:` in the first comment.
   - Screens: each `data-screen` value matches a phrase of the `Screens` text, case-insensitive and whole words. After each `data-screen` and before the next, `data-state` takes each of `empty`, `loading`, `error` and `failed-save`; a state's block may say "none" (`design.md`).
   - Each problem names its rule and line number, never the matched text.
3. A new skill, `powerops-prototype`, forked on Claude Code like the review roles (decisions Q2), writes the file from the plan's `Screens`, its Theme block (`design.md`), `route.md`'s sketch rules and the product files' "doesn't map" lists, with fake values under the ledger's display names, columns under column security or holding personal data shown masked, nothing from answers, environment facts or the tenant (decisions Q4), and a note per screen of who sees it. It writes no `@`, `http`, GUID or `<svg>`, so the tenant-data scan stays quiet: a fake person shows a name, not an email. It checks its file with `powerops plan status`, and never asks the person, publishes or uploads. `powerops-plan` starts it at approval when `Screens` names something and no passing sketch exists, then asks the person to approve the sketch or skip it with a reason. **D** stays in both menus as "sketch now", which starts the same skill before approval (decisions Q10). The fast path's **B** runs it too and waits for that answer before it signs off.
4. `powerops-implement` passes the sketch's path and the Theme block word for word in the first request to the build skill, as constraints.
5. Tests for each check (a fires case and a quiet case), the install and fork of the new skill, the plan skill's word cap, the smoke scenarios, goldens, docs; `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan clean.

### Won't build

- A post-build design check (screenshots against the sketch, App checker results): it needs Dev and a signed-in browser; a later milestone (the Design Lead research's candidate 4).
- A contrast calculation from the prototype's CSS: the cascade needs a CSS parser. The skill states its colour pairs in the header comment, and the plan review reads them.
- A control map or `data-control` names: still waiting for one canvas build with and without a prototype (BUILD-PLAN item 47, F4), which hasn't run.
- `.pa.yaml`, React or Bootstrap output from the prototype; a chart library; a Figma or image pipeline.
- A GitHub Action step that runs the check on a pull request: sign-off and `plan status` hold it (decisions Q6).
- Shipping a specialist agent (`powerops-design-lead`) as the prototype's author: the agent files serve this repo only, and shipping one needs an M38-style eval.
- Any change to `powerops-verify`, `powerops-review-security` or `tests/eval`.

## Users

- A person planning a canvas app, Power Pages site, model-driven app, generative page or Power BI report with `powerops-plan`, who gets a sketch to approve before sign-off.
- The agent that builds from the signed-off plan with `powerops-implement` and a Microsoft build skill.
- An orchestrating agent that answers for the person under a brief: it may approve a sketch, and `plan status` warns about that (decisions Q9); only a person may skip one.

## Data

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `PlanChecker.PrototypeProblems(folder, plan, decisions)` by Goal lines 1 and 2. The Log walk inside the private `AgentAnswers` moves to one helper that both use. The file is read through `ReadScannable`'s link, binary and size refusals, and a null read is a problem, not a warning | `src/PowerOps.Core/Plans/PlanChecker.cs` | new `PlanCheckerPrototypeTests` (Core): a fires and a quiet case per rule; `Screens` as "None.", "None" with a reason, no heading, a table, bullets and a Theme block alone; two `prototype:` lines, and one outside the Log; a symbolic link; an email in fake data (the tenant scan flags it); a state that says "none" |
| 2 | `PlanSignOff.Run` refuses on any prototype problem; `PlanChecker.Check` adds them as warnings for `draft` and `approved` plans, and warns on an `approved` line without the person's mark | `PlanSignOff.cs`, `PlanChecker.cs` | sign-off tests: refused with no line, on a failing file, on an agent's skip, on `not needed` with screens; allowed on a passing file, on a person's skip, and with no line when `Screens` is "None." |
| 3 | New `skills/powerops-prototype/SKILL.md` by Goal line 3: inputs, the file rules and markers, fake data, the per-screen "who sees this" note from the ledger and roles, the Theme block's values as inline CSS, the self-check with `powerops plan status`, and its report; no questions | `skills/powerops-prototype/` (new) | `SkillsCommandTests`: the `Skills` list and the "all five skills" test become six; installed for both agents; forked on Claude Code (the fork `InlineData` lists) |
| 4 | Fork the new skill on Claude Code like the review roles | `src/PowerOps.Cli/SkillsCommand.cs` (`ForkedSkills`) | as row 3; the fork loops of `PluginManifestTests` |
| 5 | `powerops-plan` sections 6, 7, 8 and 11 by Goal line 3: D stays as "sketch now"; section 7 starts the skill and asks approve or skip with a reason; section 8's B runs it and waits; section 11's file rules move to the new skill; the log lines become `approved mockups/prototype.html` and `skipped (<reason>)`, with `not needed (<reason>)` only when `Screens` names nothing; the word count stays at or under 2,783 | `skills/powerops-plan/SKILL.md` | the cap test is the gate; rewritten together in `Plan_skills_carry_the_product_references_the_sketch_and_the_power_bi_branch`: `**D** sketch the screens first` twice (the last after `## 8. Fast path`), the three log strings, and "Keep visual detail out of `Screens` and `Steps`" |
| 6 | `route.md` section 2: the file rules move to 8 screens, 40 KB and the markers; model-driven gets a sketch of its theme, forms and views; code apps log a person's `skipped (mock-data slice)` | `skills/powerops-plan/references/route.md` | the same test's "at most 6 screens and 15 KB" pin becomes the new limits |
| 7 | `powerops-implement` step 3: pass the Theme block verbatim with the sketch's path | `skills/powerops-implement/SKILL.md` | its hand-off test |
| 8 | Docs: `README.md` line 120 (five skills become six), `docs/PLANNING-SKILLS.md` (the skills table, the diagram, and who writes the mockups), the skill list in `docs/USE-WITH-AGENTS.md`, the refusals of `plan sign-off` and `plan status` in `docs/COMMANDS.md`, step 1 of `docs/HANDOFF.md`, both plugin descriptions (and the Codex `longDescription`). The two marketplace files list no skills and stay | those files | `DocsDriftTests`, `HandoffDocTests`, `PluginManifestTests` |
| 9 | Smoke: scenario 5 (`quick`, B and never D) gets one more turn, and its simulated user's goal adds "when asked to approve or skip the sketch, skip it with a reason"; it expects the file written at approval and a `prototype: skipped (` line ending `(answered by: person)` instead of `not chosen`; scenario 6 (`standard`) keeps D, greps the `approved` line, checks 40,960 bytes instead of 15,360, and expects no prototype warning from `powerops plan status` | `tests/smoke/skills-smoke.sh` | one run on Claude Code |
| 10 | Plugin copies; `tests/golden/plan-status.json` has no `Screens`, so no golden file changes | `plugins/*/skills/` | `PluginManifestTests` |

## Automation

None.

## Screens

None. The prototype rules are for the plans this milestone's users write, not for this repo.

## Environments and route

No tenant. Code, skills, docs and tests in this repo; the prototype is a local file in a plan folder and is never uploaded.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, new logic with tests, Conventional Commits, no version or changelog edits, writes through `SafeWrite`; `plan sign-off` keeps its exit codes (`1` refused, `2` usage).
- `powerops-plan/SKILL.md` stays at or under 2,783 words, so the rules go in the new skill and `route.md`.
- Cost: the new skill pins the top model on Claude Code, like the review roles, one extra top-model run per plan with screens (decisions Q7). Verification: one smoke run on Claude Code (decisions Q8).
- The fast path changes: section 8's "Ask nothing else" gains one exception, the approve-or-skip question, and its B waits for the sketch before it signs off. This follows Q1, and reverses Phase 7's "no new question" for plans with screens.
- `plan status` and `plan sign-off` print problems as text lines; their JSON schemas and exit codes don't change.
- Eval: the `tests/eval/roles/cases/plan-*` plans are `approved` with screens, so `plan status` now prints prototype warnings for them, which `powerops-review-plan` reads. That role isn't frozen, and no recorded baseline holds `plan status` output; a rerun of its plan cases is the person's call, outside M70. The frozen roles read only `status` and `bodyHash` from `plan status --json`, and their cases are `implementing` with `Screens` "None", so nothing reaches them.
- A mockup edited after its approval line is accepted, as in Phase 7: the body hash doesn't cover `mockups/`, and sign-off checks the file as it is at sign-off. `powerops-review-plan` item 11 stays as it is; the tool's screen-name check overlaps it harmlessly.
- The frozen roles and `tests/eval` aren't touched.
- Platforms are Windows and Linux.

## Assumptions

- Code at `db7bc45` (M69): `PlanSignOff.Run` refuses only on status, review and tenant data; `PlanChecker.ScanTenantData` already scans `mockups/` recursively for URLs, GUIDs and emails; `PlanChecker.AgentAnswers` already reads the Log's items with their `answered by` marks; `SkillsCommand.ForkedSkills` lists the three role skills.
- `powerops-implement` step 3 already passes the sketch's path when the log says `approved`, and says nothing shows a build skill follows a sketch.
- `app-builder` writes the model-driven modern theme from `app.theme` (M69's `model-driven.md`), so a model-driven sketch's theme carries into the build; its forms and views stay metadata.
- 8 screens and 40 KB are the person's choice (Q3) and unmeasured; Phase 7's 6 screens and 15 KB came from two small samples. The smoke run records the file's size and screen count.
- Environment facts were not read: the plan names no environment and changes only this repo.

## Open questions


## Questions for your admin

## Steps

1. This plan and `decisions.md`; three question rounds with the person; a fresh-context review into `review.md`; fixes, a fourth round on two findings, and a second review pass; the person's sign-off.
2. Rows 1 and 2, test first (the new tests fail), then the code.
3. Rows 3 to 7: the skill text, the fork, the plan skill within its cap, `route.md`, the hand-off line.
4. Rows 8 to 10, and the "Update goldens" recipe.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; one smoke run on Claude Code; commit on the branch.
