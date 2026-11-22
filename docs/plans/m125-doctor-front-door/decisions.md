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

`missing` (`pac`, `az`) and `failed` (the two sign-in probes only; a failed licence read is `not-read`, C7) set exit 1; `not installed` and `not read` do not. A person who only runs `analyze` on files still sees `1` when `pac` is missing; the text says which line caused it, and COMMANDS says so.

### C6. The licence line counts the part numbers the table doesn't list

The brief says product names only. A bundle such as Microsoft 365 E3 is not in section 3, and a line with no product would read as "you hold nothing". So the line adds a count, `; <n> not in PowerOps' table`, or `none in PowerOps' table (<n> other)`: a number, never a part number, so nothing is widened.

### C7. A failed licence read is `not-read`, not `failed`

Microsoft Graph may refuse the read for an account with no directory role (A4), which is the owner's default newcomer. A failure that is the expected case shouldn't set exit 1; the line keeps the skill's rule (only a failure notice) and says the planning skill asks instead. The sign-in probes keep `failed`.

## Log
- 2026-11-22: `powerops-researcher` on A1 to A4 (Learn for Microsoft, Claude Code's docs, and each Microsoft repo's `marketplace.json` on `main`; read 2026-11-22). A1: `RetrieveCurrentOrganization` (Web API reference *RetrieveCurrentOrganization*, *OrganizationDetail*, *OrganizationType*) has `OrganizationType`, absent from the sample in *Web API functions and actions sample*; `pac org who` documents no fields; `pac environment-management get-environment-by-id-for-user` is preview and needs a Power Platform API token: no route fits, C4. A2: Claude Code *Plugin loading* and *Troubleshooting* pages document the plugins root, `CLAUDE_CODE_PLUGIN_CACHE_DIR`, and the `scope`, `installPath`, `version` fields under plugin ids; *Environment variables* says `CLAUDE_CONFIG_DIR` holds the plugins; the top-level shape is undocumented. A3: `power-platform-skills`, `dataverse-skills`, `skills-for-copilot-studio`, `fabric-collection`. A4: confirmed; least privileged for a work account `LicenseAssignment.Read.All`, and a directory-role note that may refuse a basic user (already in `docs/ecosystem/compatibility.md`).
- 2026-11-22: approved for review by the building session, under the owner's "Approve and start" (2026-11-22) for the build plan; `prototype: not needed (Screens names nothing)` (answered by: agent m125 session).
- 2026-11-22: Fable plan review round 1 REJECTED (6 findings); `powerops-review-plan` run 1 (fresh context): 0 High, 2 Medium, 6 Low. Answers: Fable 1 and review R2 (`az account show` proves no token): kept, worded "an account is selected (no token requested)", A5 with its local run and the 2026-10-30 Windows record, and a test of probe passing and read failing (Done 2, Data row 1). Fable 2 (no next step): every non-ok line has one fixed sentence (Data row 1), and the plain goldens hold a `missing` and a `not-installed` line (Done 3). Fable 3 (spelling): JSON `id` and `status` in kebab-case, `status` closed, `id` open (Done 3). Fable 4 and R3 (README unchecked): two tests (README holds the sample's result lines; every leaf command has a table row), Row 5 says the clone route until the first release and names what stays (Done 6). Fable 5 (licence line): C6, a non-token line is a failed read, and the argument-list and one-call tests (Done 2, Data row 3). Fable 6 and R5 (size check): `BoundedRead.ReadAllText` (Data row 2). R1 (seams): Data row 6. R4: a `doctor-live.json` golden (Done 3). R6 (accounts may differ; a refused read sets exit 1): COMMANDS says so (Done 4), and C7. R7: the leak scan's path (Done 8). R8: the Graph URL kept with the allow marker. Fable's NOTES go to the hand-over.
- 2026-11-22: Fable plan review round 2 REJECTED (2 findings); `powerops-review-plan` run 2: 1 High, 1 Medium, 2 Low. Answers: Fable 1 (the token pattern rejects real part numbers such as `Microsoft_365_E3_(no_Teams)`): a line is a part number when it has no control character and is at most 256 characters, with three named test cases (Data row 3, Done 2). Fable 2 and R1 High (a failed licence read was `failed` in A4 and C5, `not-read` elsewhere): A4 and C5 now say `not-read` (C7). R2 (the sample's commands don't run in a clone, and a clone fence would break `QuickStartProblems`): Row 5 gives the clone-root commands and keeps any `git clone` fence after the CI one. R3 (installed is not enabled; the hint lacks the marketplace step; the environment-type sentence): Row 1 and Row 2 reworded, COMMANDS says what the record shows. R4: the Graph URL kept with the allow marker.
- 2026-11-22: Fable plan review round 3 APPROVED (on `d72765d`); assignments: steps 1 none, 2, 3 and 6 main session, 4 and 5 opus, 5 after 4. `powerops-review-plan` run 3: 0 High, 1 Medium, 6 Low. Answers, all text in the plan, none a change of scope: R1 (CRLF from `az` on Windows would read as a control character): lines split on LF with a trailing CR dropped, and a CRLF test (Data row 3, Done 2; also Fable's round-3 WATCH). R2 (argument lists written twice): Done 2 points at Data row 3. R3 ("no id" while plugin ids print): Constraints says account, tenant or environment id. R4 (the found-on-PATH test bypasses the shipped lookup): the host holds PATH and PATHEXT, and the checks call `PacRunner.Resolve` (Data row 6). R5: A6 names ECO-0013; the plugin record's shape is dated with Claude Code 2.1.326. R6: the install prompt's step 5 stays, under Won't build. R7: the Graph URL and the repo handle in the install hint kept (the README already prints the handle).
- 2026-11-22: `powerops-review-plan` run 4 (same context, `fresh-context: no`): 0 High, 0 Medium, 1 Low. R1 to R7 of run 3 answered. R8 (Constraints repeats "account, tenant"): kept, a wording slip whose meaning holds, so the reviewed hash stays current; no finding open.
- 2026-11-22: signed off with `powerops plan sign-off` by the building session (body sha256 d054518110cc…), after Fable's round 3 APPROVED and `powerops-review-plan` run 4 with no finding open, on the owner's standing instruction for the debate chain: "Yes to all three" (2026-11-22), with "Approve and start" (2026-11-22) as the plan's approval.
