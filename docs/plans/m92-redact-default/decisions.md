# Decisions: m92-redact-default

The change itself is item M92 of the fix plan after trial 3, approved by a Fable plan review and started by the owner's "Start the build" (2026-11-11). The owner answered the plan's Q2, "Should outputs hide URLs and default values by default?", with option (a), "Hide by default" (2026-11-11, in the planning session's question form). That reverses their own D7 of 2026-11-04, which M61 built as an opt-in `--redact` (`docs/plans/m61-redact/decisions.md`). The fix plan's text for (a): `analyze` and `diff` redact by default, `--show-values` turns it off, `--redact` stays accepted and changes nothing, the MCP `analyze` and `diff` tools always redact, the Action's `redact` input defaults to `'true'` and `'false'` passes `--show-values`, and the baseline file and the MCP `baseline` tool stay unredacted. The owner's other three answers (Q1 "One yes per deployment", Q3 "Keep out of Git", Q4 "Rerun it") don't touch these outputs. Every decision below was made by the building session inside the fix plan, as the owner's direction of 2026-11-11 asks ("as autonomous as possible").

## Round 1 (planning)

### Q1. `--redact` and `--show-values` given together?
1. `--show-values` wins: `--redact` changes nothing, as the fix plan says (recommended)
2. A usage error (exit 2)
3. `--redact` wins

Decided: 1. The fix plan says `--redact` "changes nothing", and with option 1 that holds in every combination: the output depends on `--show-values` alone. `--show-values` is a new, explicit ask that no existing script passes, so nothing that passes `--redact` today gets values by accident. Option 2 adds a validator for a case no one has; option 3 makes `--redact` change something.

### Q2. How do the MCP `analyze` and `diff` tools redact?
1. Through the CLI default: they already call `analyze` and `diff` with fixed arguments, and never pass `--show-values`; their descriptions say so (recommended)
2. A `show_values` input

Decided: 1. Q2 (a) says they always redact; an input would be a way out the owner did not ask for. The description sentence tells the calling agent why it sees `[redacted URL]`.

### Q3. Which Action values show the values?
1. Only `'false'`; any other value, the empty string included, redacts (recommended)
2. Anything other than `'true'` shows them

Decided: 1. It fails closed: a typo or an empty expression keeps the values hidden. M61 read only `'true'` for the same reason in the other direction.

### Q4. The baseline file and the MCP `baseline` tool?
1. Unchanged, unredacted, and the docs and the decision say so (recommended, the fix plan)
2. Redact them too

Decided: 1. A baseline matches findings on component and message (`docs/COMMANDS.md`, "analyze and baseline"), and `analyze` applies it before it redacts, so a redacted baseline would match nothing. The docs say a committed baseline file holds the URLs its findings name.

### Q5. SARIF fingerprints and the goldens
1. No code change to `SarifReport`; the docs and the decision say that the first upload after M92 closes the code scanning alerts of findings that held a URL and opens new ones, once; the `analyze-fixtures.*` goldens stay, because Core's `ReportTests` writes them over unredacted results (recommended)
2. Make the Core goldens redacted

Decided: 1. The fingerprint is already taken over the printed text (M61 Q4); only the default moved. Core's writers are unchanged, and their goldens test the writers, not the CLI default; the CLI tests pin the default.

### Q6. Commit type?
1. `feat!:` with a `BREAKING CHANGE:` footer for the code, `docs:` for the docs (recommended)
2. `fix:`

Decided: 1. A default output changes, and SARIF alerts reopen once; the release notes should carry it. With `bump-minor-pre-major` this is a minor release, which release-please manages.

### Q7. The Verifier's eval rerun (asked of the owner)

Asked in a question form on 2026-11-11, because decision 40 needs a rerun of each pinned role whose eval baseline a change reaches, and M96's approved rerun (cap USD 30) covers only the security role. This change reaches one baseline line that both roles see: `solution-08`'s `diff` printed `defaultValue: New → Triage` and now prints `defaultValue changed`. Options: add the Verifier pass to M96 with a cap of USD 40 (recommended); a written exception to decision 40 for this one line; stop M92. The owner chose "Add to M96, cap USD 40 (Recommended)" (2026-11-11). So M92 touches nothing under `tests/eval/`: `RoleEvalTests` runs that recorded `diff` with `--show-values` (plan row 6a, a commented shortcut), and M96 reruns both passes, regenerates the baseline files and drops the bridge. The hand-over note tells M96.

## Round 2: the reviews

Fable's plan review (REJECTED: 1 High, 3 Medium, 1 Low) and the fresh `powerops-review-plan` (`review.md`: 0 High, 1 Medium, 5 Low) overlap; each finding is answered in `plan.md`.

- Fable 1 (High), the eval baseline `RoleEvalTests` matches: Q7; plan row 6a, step 2's files, the Done when 7 filter and the corrected Assumptions line (M96 reruns the security pass; the owner added the Verifier).
- Fable 2 (Medium) = review R1, R2, R4: the filter is project-scoped (`--project tests/PowerOps.Cli.Tests`), the checks run the branch's own `powerops.dll`, and the leak scan's command and folder are given (Done when 7, step 1).
- Fable 3 (Medium) = review R3: tests look for `redacted URL` without brackets where Markdown is among the formats (Done when 1, row 6 (a)).
- Fable 4 (Medium) = review R5: `docs/rules/PO006.md` and `PO014.md` gain one sentence (row 7a, step 3), and rows 7, 8 and 9 and Done when 6 say that two findings that differ only in their URL share one fingerprint by default.
- Fable 5 (Low) = review R6: row 2 gives `diff`'s own option texts; row 3 gives each MCP tool its own sentence; row 6 (b) gains the `--redact --show-values` case.
- Review's closing note (an Action pinned to an older release): the Action builds the CLI from its own commit, so script and CLI never differ (Assumptions).

## Round 3: the second pass

Fable approved the revised plan (step 2 and step 3 to Sonnet). The fresh review's second pass (`review.md`, plan-hash `34289c86…`) closed R1 to R6 and found 0 High, 0 Medium and 2 Low, answered here so the reviewed plan body stays as it is:

- R1 (Low), step 3's check mixes the two test projects: step 3 runs `dotnet test --project tests/PowerOps.Cli.Tests --filter "DocsDriftTests|ActionYmlTests"` and `dotnet test --project tests/PowerOps.Core.Tests --filter RuleDocsTests`.
- R2 (Low), the docs' Action snippets pin a commit with the old default: "Public outputs" says the hidden default starts with the first action commit that includes M92, and the pins pull request that moves the snippets is a named follow-up in the hand-over note.

## Log

- depth: standard; the fix plan and M61's record give the design
- researcher: none needed; no Microsoft tool, API or skill is described (plan, Assumptions)
- approved 2026-11-11 by the building session for the reviews
- Fable plan review 2026-11-11: REJECTED at `360ae5f` (1 High, 3 Medium, 1 Low), APPROVED at `2f69e13`; fresh `powerops-review-plan` (Opus, fresh context): 0 High, 1 Medium, 5 Low at `360ae5f`, then 0 High, 0 Medium, 2 Low at `2f69e13`, all answered (Rounds 2 and 3)
- signed off 2026-11-11 by the building session on the person's standing instruction (sessions sign off their own milestone plans after clean reviews, 2026-10-22, repeated 2026-11-09), under "Start the build" (2026-11-11): Fable approved, and `powerops-review-plan` has no finding open (`powerops plan sign-off`; `signed-off-hash` equals `review.md`'s `plan-hash`, `34289c8`)
