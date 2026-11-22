# Decisions: m125-doctor-front-door

## Authority

- The agreed plan of 2026-11-22 (`reports/three-way-debate-2026-11-22/AGREED-PLAN.md`, git-ignored), Phase 1 item 3, and its build plan's step 6 (M125). On the plan's first question the owner answered "The target should be a newcomer that doesn't start with premium authority but may have so depending on the license check. Assume basic authority only unless noted otherwise." (2026-11-22), and, asked "Approve the plan and start with the Fable plan review, then Phase 0?", "Approve and start" (2026-11-22).
- Asked "May the milestone sessions sign off their own plans after clean reviews, and merge their own pull requests once every check passes? May I merge PR #173 once its checks pass, leaving #172 for you? May M124 spend up to USD 10 on one local planning smoke run?", the owner answered "Yes to all three" (2026-11-22). That is this milestone's authority to sign off its own plan and merge its own green pull request.

## Choices

### C1. `--format text|json`, not a `--json` flag

The build plan says "`--json` with `schemaVersion`". `rules` and `plan status` keep `--json` only as "the older spelling of `--format json`"; the newer commands (`inventory`, `diff`, `test`) take `--format`. A new command takes the current form, so `doctor` has `--format text|json` and no `--json`.

### C2. The licence table is read from the embedded `licensing.md`

Options: (a) a C# copy of section 3's part-number table, with a test that it matches; (b) parse section 3 from the copy of `skills/powerops-plan/references/licensing.md` that the CLI already embeds for `skills install`. Chosen (b): one table for the skill and `doctor`, so an edit to `licensing.md` reaches both. A test fails if the parse finds no rows or a row count other than the file's.

### C3. Plugin record read as a file, not through `claude plugin list --json`

`claude plugin list --json` is documented, but starting `claude` is a process whose network use is undocumented, and plain `doctor` must start none. The file read is offline; its shape is observed, so an unknown shape is `not read`.

### C4. Environment type stays `not read`

See plan A1: the documented function's field is absent from Learn's own sample and its values have no documented mapping to environment types, so a value printed today could be wrong. A read needs a live capture first, which needs a tenant (follow-up).

### C5. What sets exit 1

`missing` (`pac`, `az`) and `failed` (a sign-in or the licence read) set exit 1; `not installed` and `not read` do not. A person who only runs `analyze` on files still sees `1` when `pac` is missing; the text says which line caused it, and COMMANDS says so.

## Log
- 2026-11-22: `powerops-researcher` on A1 to A4 (Learn for Microsoft, Claude Code's docs, and each Microsoft repo's `marketplace.json` on `main`; read 2026-11-22). A1: `RetrieveCurrentOrganization` (Web API reference *RetrieveCurrentOrganization*, *OrganizationDetail*, *OrganizationType*) has `OrganizationType`, absent from the sample in *Web API functions and actions sample*; `pac org who` documents no fields; `pac environment-management get-environment-by-id-for-user` is preview and needs a Power Platform API token: no route fits, C4. A2: Claude Code *Plugin loading* and *Troubleshooting* pages document the plugins root, `CLAUDE_CODE_PLUGIN_CACHE_DIR`, and the `scope`, `installPath`, `version` fields under plugin ids; *Environment variables* says `CLAUDE_CONFIG_DIR` holds the plugins; the top-level shape is undocumented. A3: `power-platform-skills`, `dataverse-skills`, `skills-for-copilot-studio`, `fabric-collection`. A4: confirmed; least privileged for a work account `LicenseAssignment.Read.All`, and a directory-role note that may refuse a basic user (already in `docs/ecosystem/compatibility.md`).
