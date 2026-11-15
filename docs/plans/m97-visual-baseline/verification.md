# M97 verification run

The fix plan's "Verified by": `powerops-prototype` run once on a sample plan from the smoke script's idea, and the new judge run on its output. Run on 2026-11-14 by the building session.

## Setup

- Branch commit `f599fc2` (the S8 line of `45288ea` was copied in before round 2; see below), built in Release, with `powerops` on PATH running that build (`0.0.0+f599fc2`), not the installed tool.
- A scratch git repo outside this one, with `powerops skills install . --agent claude` from that build, so the skills were read from the scratch repo's `.claude/skills/`, not from the user's plugin.
- Each skill ran in its own Opus context (effort high) told to follow the installed `SKILL.md` exactly: the prototype as a subagent, and each judge round in a new fresh context that read no author report and no earlier pictures.
- The sample plan: "an app for tracking site visits by our field engineers" (the smoke script's idea), canvas, three screens (My visits and Log a visit on a phone, Team today on a desktop), with no Theme block for the options. A Theme block (seed `#2E6B4F`) was added after the options, then `pick a`: the M93 follow-up "a run that sets the Theme after the pick".
- Rendering: Chrome 157.0.8103.78 headless through `measure.py` on Linux; Segoe UI is not installed here, so every picture is in the fallback Adwaita Sans.

## Options

Three lead frames of Team today at 1366 by 768, 29,899, 30,027 and 36,587 bytes: A "Summary and split" (list with detail, numbers first), B "Action queue" (the six visits that need the supervisor, safety first), C "Team board" (one row per engineer on a timeline). They differ in layout, first look and density; one picture shows them side by side. 3, 4 and 4 render-and-fix passes. Recommended and picked: A.

## The pick and the judge loop

| Round | Author's change | Judge's scores (1 to 9) | Gates failed | Verdict |
|---|---|---|---|---|
| 1 | `pick a`: three frames, 12 state tiles, the map; 3 render passes | 3 3 3 3 2 2 3 2 3 | 2 (a state tile dropped a safety badge), 3 (a tab-count style the map marks not confirmed) | not at baseline |
| 2 | both gates and the placeholder names, dates, labels and states fixed; 3 render passes | 3 3 3 3 3 2 3 3 3 | none | not at baseline (States: no success state, no field error) |
| 3 | success and field-error states, who is behind named, comparisons on one basis; 3 render passes | 3 3 3 3 3 3 3 3 3 | none | at baseline |

`prototype.html` is 55,835 bytes; `plan status` reads the round-3 verdict as current (its `sketch-hash` matches) and warns only that no `prototype:` line is logged, which the person's answer writes.

## What the run showed

- **The fork fallback works.** A subagent can't start a fresh context, so each prototype round ended with "judge needed: mockups/prototype.html" and the caller started the judge, as decisions Q9 and `powerops-plan` section 7 step 6 say. Whether a forked skill can start a forked skill in Claude Code is left to the smoke's Claude leg.
- **The judge is not the author and is strict.** It failed rounds 1 and 2 on real faults: a contradiction between two drawings of one visit, a drawn style the author's own map called unconfirmed, and missing success and field-error states.
- **A skill line contradicted the baseline.** Round 1 marked the names "Sample Person A to T" as placeholder copy (S8). They came from `powerops-prototype` section 5, which said "a name such as \"Sample Person A\"". That line now asks for a made-up first name and initial, as the samples do (`45288ea`).
- **Cost.** Seven Opus contexts (three prototype rounds, three judge rounds, one options run), about 1.3 million subagent tokens, roughly 45 minutes of wall time.

## Not checked

- Any render in Segoe UI or on Windows.
- A Power Pages or Power BI sketch: the run is canvas only (the calibration covers the other two products).
- Whether the person, looking at the pictures, agrees with the verdict: that is the comparison step 9 asks for.

## Where the output is

Outside the repo, in the session group's shared notes folder, `m97-verification/`: `plan.md`, `option-a.html` to `option-c.html`, `prototype.html`, `prototype.map.md`, `prototype.judge.md`, and `pictures/` (the options side by side, the three frames, the full board and the 15 state tiles at 1:1).
