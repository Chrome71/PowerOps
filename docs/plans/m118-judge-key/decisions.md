# Decisions: m118-judge-key

## Authority

- M117 (#167, squash `61fc080`) merged on the owner's "Yes, merge" in chat (2026-11-16) with its owner questions Q2 to Q6 open. Asked next "Answer Q2–Q6 now so the key and the text fix can follow?", the owner answered "Take all recommended" (2026-11-16). This milestone carries out those recommendations, as written in `docs/plans/m117-judge-consistency/decisions.md`, "Questions for the owner".
- It runs under the chain rules in `reports/trial-3/chain/chain-rules.md` (sessions sign off their own plans after clean reviews), and stops before merge for the owner's yes in chat, as M117 did, because it changes the key (Q17 of M97).

## What each answer means

- Q2: keep all five verdicts; key the recommended list adjusted by Q5 (row 1: 1, 7; row 2: 1, 4, 7; row 3: none; row 4: 4, 6, 8; row 5: 4), only once the rerun confirms it (plan Done line 4); `tests/sketch-eval/README.md` updated.
- Q3: row 2's plan line stays.
- Q4: M117's narrowings and step 9 rules stay.
- Q5: gate 7 gains the phone-footer rule; row 4's gate 7 is expected to drop from the key. The plan's wording (a site's phone frame with a `box` height of at most 932 needs only the header, whether or not the page ends inside it) defines the owner's phrase "drawn to its first screen"; it is shown to the owner at the merge ask.
- Q6: `baseline.md` gate 1 gains the replacement sentence.
- Q5 and Q6 are one text round, then all five rows rerun 3 times (AGENTS.md).

## Log
- 2026-11-16: Fable plan review round 1 REJECTED (3 findings): Q5's wording could split rows 4 and 5 (row 4's page ends inside its phone frames, row 5's frames cut the page), so Data row 2 now defines the owner's phrase "drawn to its first screen" by the frame's kind and a `box` height of at most 932, holding whether or not the page ends inside the frame, with an exact sentence per file; this definition is shown to the owner at the merge ask. Done lines 3 and 4 now key only the list the owner approved and stop on any other outcome. Frozen-path checks added to steps 3 and 6.
- 2026-11-16: Fable plan review round 2 APPROVED; assignments: steps 1, 2, 6 main session; 3 and 5 opus; 4 opus judges with haiku extraction. Stale Q2 lines in the Goal and above fixed before the checklist review.
- 2026-11-16: powerops-review-plan (fresh context) found 0 High, 1 Medium, 6 Low. Answers: R1 (rebase may change judged text) fixed in step 6, the judged-path diff check after the rebase. R3 (932 px unsourced) fixed in A1, PowerOps's own figure. R5 (what a stop leaves; resume after 92%) fixed in Constraints. R7 (key text claims all gates came from M118) fixed in Done line 4. R2 ("selected-row style" and "the size test" undefined in `baseline.md`): left; Q6's sentence is the owner's approved wording verbatim, "the size test" follows the size-then-weight sentence two sentences before it, and the judge's `SKILL.md` defines both; a wording fix would be a new owner question. R4 (owner first sees the 932 wording at the merge ask): accepted; the wording only makes the owner's recommended rule measurable, the merge ask shows both sentences, and if the owner rejects it the text round and 15 runs are redone. R6 (judge-file folder unnamed): the folder is outside the repo and private; the building session gives its path to the work review in the brief, as in M117.
- 2026-11-16: powerops-review-plan run 2: 0 High, 0 Medium, 1 Low. R1 (the rerun path after a rebase that changes judged text): answered here, without a body change: on that path the lock is released and the milestone stops; the work restarts on the rebased commit, with Done line 1 and step 3 comparing against the rebased base, and the lock is taken again only after the new runs. Run 2 was in the same context as run 1 (`fresh-context: no`); a fresh run follows if sign-off needs it.
- 2026-11-16: signed off by the building session under the chain rules, with the owner's "Take all recommended" (2026-11-16) as authority (`powerops plan sign-off`, body sha256 db2227058512…).
