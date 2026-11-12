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

## Log

- depth: standard; the fix plan and M61's record give the design
- researcher: none needed; no Microsoft tool, API or skill is described (plan, Assumptions)
- approved 2026-11-11 by the building session for the reviews
