# Decisions: m95-references

The owner's answer that applies to this milestone, given 2026-11-11 in the planning session's question form to "Q3. Keep canvas packages (`.msapp`) out of Git by default?": "Keep out of Git" (option a: "Git holds the rest of the unpacked source, and the canvas app is checked on the exported zips"). The fix plan's other answers (Q1 "One yes per deployment", Q2 "Hide by default", Q4 "Rerun it") were built by M94, M92 and M96 and change nothing here; M94's consent wording is kept. The routine choices below were made by the building session under the owner's direction of 2026-11-11 ("as autonomous as possible") and the standing instruction that sessions sign off their own milestone plans.

## Round 1 (planning)

### Q1. What does the `.gitignore` block in Hand-off step 3 hold?
1. Every path that holds a canvas package or the target's values, anchored: `*.msapp`, `/ContosoApp.zip`, `/deploy/*.zip`, `/deploy/*.settings.json` and `/review/` (recommended)
2. Only `**/CanvasApps/*.msapp`, as the fix plan names it, with the zips still kept out of public repositories only
3. In your own words
Answer: 1. The blind grade's point was Dev's host in a tracked file; a zip or the managed unpack folder holds the same `.msapp`, so ignoring the folder copy alone would leave the host in Git. `*.msapp` replaces the fix plan's `**/CanvasApps/*.msapp` because it also covers the YAML layout, which Learn keeps under `canvasapps/<name>/` (A14), and any other path; the other lines are anchored so that a folder named `review` deeper in the repo isn't ignored. The settings file already "stays out of Git in any repository" (step 3), so its line is that sentence made copyable. The ephemeral recipe, which commits zips in `deploy/`, is named as the exception, and the CI import of a review-linked zip gets its sentence in step 7 (row 7). (answered by: agent m95 session, on the owner's standing direction)

### Q2. Does `analyze` get a note for a canvas app whose `.msapp` the folder doesn't hold?
1. Not in M95: document it, and log a follow-up for after M96 (recommended)
2. Yes, in M95
3. In your own words
Answer: 1. It changes `analyze` output, which the chain keeps still until M96's eval has run, and the brief allows no `src/` change. Hand-off steps 3 and 4 say the canvas checks run on the zip. The follow-up goes in the done hand-over. (answered by: agent m95 session, on the owner's standing direction)

### Q3. Learn now names the other harness the Microsoft Copilot harness. What changes?
1. `copilot-studio.md` maps the name in its new bullet; the other files keep "GitHub Copilot harness"; ECO-0020 records the rename with the files that use the old name, `done` for the references and the rest left as they are (recommended)
2. Rename it in every file now
3. In your own words
Answer: 1. The old name still matches Learn's URL slugs and the copilot-studio-plugin README ("GHCP Harness"), so no reader is misled once the reference maps it; a rename across `docs/COMMANDS.md` (M107 writes it now), `docs/rules/PO027.md`, `HANDOFF.md`, the specialist and a code comment is a wider edit than the trial asked for. (answered by: agent m95 session, on the owner's standing direction)

### Q4. Does `.claude/agents/powerops-copilot-studio.md` change?
1. No: its checklist item 7 ("Tools that change data ask the user before running") is right and names no route, and line 43 (M94's "Won't build") is re-read and holds (recommended)
2. Add the tool setting's name to item 7
3. In your own words
Answer: 1. The route belongs in the reference the specialist reads, `copilot-studio.md`; adding it to item 7 would duplicate it. (answered by: agent m95 session, on the owner's standing direction)

### Q5. A smoke run?
1. No (recommended)
2. Yes, the Claude leg
3. In your own words
Answer: 1, for the reasons in Constraints: no scenario asserts on a changed line, the reference text changes no question, round or order, and trial 4's R-package and R5 test the result live. (answered by: agent m95 session, on the owner's standing direction)

## Log

- 2026-11-12: started by the M95 building session (Opus) in its own worktree at `87a31dd` (`origin/main`). Depth `standard`: reference and doc text, three test files; no product code.
- 2026-11-12: research by `powerops-researcher` (Opus, Learn and Microsoft's public GitHub only, no tenant call), recorded in `plan.md` Assumptions A1 to A13. Mode B of `docs/ecosystem/README.md`: rows power-platform-skills, skills-for-copilot-studio, copilot-studio-plugin and skills-for-fabric of `compatibility.md` (verified 2026-11-10) and the open entries of `changes.md` read; none of the open entries (ECO-0009 to ECO-0013, ECO-0015 to ECO-0018) covers the new experience, the canvas references or the Power BI validator.
- 2026-11-12: checked before planning: M91's follow-up on `docs/PLANNING-SKILLS.md:189` ("the trade-offs") has no match at `87a31dd`, so it was closed by a later milestone.
