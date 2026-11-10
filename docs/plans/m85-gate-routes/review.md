plan-hash: 7ac185e32d7d8c5e686d7d801cc261f7b58603ce1540a68a35421d23217e6fa5
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null and nobody confirmed a signed-in `pac`, so `powerops plan facts` was not run. This is a new review from scratch, 2026-11-10. It replaces the first pass (plan-hash `45ce088`, findings R1 to R9), which `powerops plan status` reported as stale after the plan body changed. The skill gives no other rule for an existing review. It covers `plan.md` (body hash `7ac185e`) and `decisions.md` as they stand, uncommitted, on `feat/m85-gate-routes` at `d755cf1`, and the uncommitted M85 section and decision 59 in `docs/BUILD-PLAN.md`. `powerops plan status` (the installed CLI) shows `approved` and `review: done`, with 0 violations and one warning: the stale review. Open questions: 0; questions for your admin: 0.

The first pass's findings, against the plan as it stands. R1 is closed: row 6 (f) adds a sentence that names the non-preview writes, and row 8 tests for it. R2 is closed: step 3 logs the deny array, and step 4 compares the fence to it with `jq`. R3 is closed: row 12 has outcomes (a), (b), (c) and (i), and row 6 (f) says what the docs say for each. R4 is closed: rows 6 (e), 6 (g) and 7 are reworded. R5 is closed: row 3 says the segment stays unrecognized, and row 4 (e) pins two assignment-only cases. R6 is closed: the Log names the three `connectivity` tools and `auth_who`. R7 is closed: row 4 (a) rewords the comment. R8 is closed: the scope line in `docs/BUILD-PLAN.md` is reworded, and R3 below covers two other lines there. R9 was accepted by the owner's side, and the same three public documentation hosts are the only URLs in the plan files, so it is not raised again.

Checked in the repo: `ImportCall.cs` whole: `McpRoutes` and its suffix test, the `pac` switch, the `node` and `pa` branches, the leftover check and the tokenizer (quotes, backslash-newline, backslash-CR, `$'...'`, and the separators `;`, `|`, `&`, `(`, `)`). I walked each case of row 4 (a) to (f) through that code with rows 1 to 3 applied by hand, and each expected reading holds. That includes `words.Length > 2` against the one-word `"power-pages upload-certificate"`, and the claim that no word arrangement other than the literal pair matches the switch key. I also checked that the existing cases `X=1 <import>`, `X=1 <bundle> push`, `PA="npx --no-install pa"; $PA app push`, `X=a/npx pa app push` and `npx -p x pa app push` keep their lines. `Blocks()` asserts with `Contains`, and the other case lists are as the plan cites them.

Every line number the plan cites checks out: `ImportCall.cs`, `ImportCallTests.cs` (`:300`, `:650`, `:681`), `DeployGateCommandTests.cs`, `HandoffDocTests.cs` (`:12`, `:97`, `:166-174`), `docs/HANDOFF.md` (`:70`, `:95`, `:96`, `:99`, `:101`, `:103`, `:108`, `:113`), `docs/COMMANDS.md` (`:128`, `:136`), `changes.md` (ECO-0008, ECO-0019) and `compatibility.md` (`:21`, `:31`, `:32`). `docs/HANDOFF.md` has one `json` fence today, so the deny list becomes the second. Only `HandoffDocTests.HookGroups` reads a fence. `EcosystemDocsTests` sets no rule on the limits lines.

Checked outside the repo, read-only: Claude Code's changelog dates "glob pattern support in deny rule tool-name position" to 2.1.166. Codex's `ToolFilter::allows` at `rust-v0.167.0` tests `HashSet::contains` on `tool.tool.name`, and `mcp_types.rs` calls the two fields "Explicit allow-list" and "Explicit deny-list". In power-platform-skills `4076781`, `set-cli-origin.js` prepends the `if [ "${PA_CLI_ORIGIN+x}" != x ]; then export ...; fi` line, `cli-origin.test.js:84` holds the `env PA_CLI_ORIGIN=custom ...` form and `:184` the inline form. Observed in this reviewing session, on Claude Code 2.1.322: the five pac tools that the repo's local settings deny by exact name are missing from this session's tool list. That supports using the six exact names as the probe's control in row 12.

Tools (item 12): `claude` 2.1.322, `codex` 0.167.0, `pac`, `jq`, `dotnet`, `git`, `bash` and `powerops` were found with `command -v`, and `reports/leakscan.sh` exists. The `pac` on the `PATH` is the 2.14.1 tool install. I did not run `pac`, `claude -p` or the gate, so the 2.14.1 tool names and counts, the "today" readings in the Log and the probe's behaviour are taken from the Log. Not checked: Claude Code's permissions page and Codex's MCP page (the changelog and code above cover the claims they support).

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The probe's "must see" check has a failure that maps to no outcome, and the `pac` it starts is not pinned
- Area: 9. Testability
- Finding: Row 12 says the control must show `pac-cli` connected, the six gated tools and tools of each of the 11 groups. It retries, and then records (i), in only two cases: the server is not connected, or the control lists no `mcp__pac-cli__` tool at all. In a third case, the control lists pac tools but lacks some of the six or some groups, and that case gets no retry and no outcome. Read literally, the run then falls through to (a) to (c), and (a) "no tool of the 11 groups" is true for free for a group the control never listed. The docs could then say "tested" for globs that did not run. This case can happen on this machine. `pac.json` starts the bare `pac`, which today resolves to the 2.14.1 install. But a second `pac`, 2.11.2, is installed too: the user's own pac MCP server runs it, and ECO-0014 says 2.11.2 lists none of the preview groups. The Log line records "`pac` 2.14.1" as a given, not as something observed. Also, the reason given for `--setting-sources project` places the ECO-0014 deny rules in the user's settings, but on this machine they are in the repo's local project settings. Running in a scratch folder outside the repo keeps them out either way, so the isolation holds.
- Suggested fix: In row 12 and step 3, record (i) after the retry for any failure of the must-see check, including a control that lacks any of the six names or any of the 11 groups. Have `pac.json` name the 2.14.1 `pac` by its absolute path, or have the Log line record which `pac` the server ran (`command -v pac` from the probe's shell, and the version it prints).
- Resolution: open

#### R2. The list "Reading changes" leaves out some changes that rows 2 and 3 cause
- Area: 9. Testability (Done-when 3: "in no other way")
- Finding: Rows 2 and 3 change three readings that the list doesn't name and no case pins:
  - A `pac` or `node` route led by an assignment whose file name is `pac` or `node` goes from the route's line to "can't read". Examples: `PATH=/x/pac pcf push`, `X=a/pac model genpage upload`, and `X=a/node <bundle> push`, which today gives the manage-agent route. Item 4 (a) names only "a `pac` import, a `node` pass, or the `pa` route".
  - The certificate upload segment now adds no line, so the rest of a command reads as it would alone. A certificate upload whose `--path` is an unquoted `$(pac solution import ...)` becomes a `Check` of that import, where today it blocks as "can't read". A certificate upload with an unclosed quote keeps only "the quotes are not closed".

  Each of these follows from the rule the plan chose: the `;` [Fact] of row 4 (c) already accepts the `Check`, and every other change is still a block. But Done-when 3 and the work review rely on the list being complete. Step 2's stop rule covers only pinned cases, so nobody would see these.
- Suggested fix: Widen item 4 (a) to "an import, route or pass of `pac`, `node` or `pa`". Add to item 2 that the upload segment adds no line, so the rest of the command reads as it would alone. Pin `PATH=/x/pac pcf push` and `X=a/node <bundle> push` (unreadable, without the route's line) in row 4 (e), and the unquoted `$(...)` and unclosed-quote certificate uploads in row 4 (c).
- Resolution: open

#### R3. Two lines of the M85 scope in `docs/BUILD-PLAN.md` don't match the plan
- Area: 10. Unanswered areas (record consistency)
- Finding: The scope bullet says "`docs/HANDOFF.md` and `docs/COMMANDS.md` name the sixth MCP tool and the 16 preview groups". But row 7, Q7 and Done-when 5 say `docs/COMMANDS.md` copies no group names and adds only one clause. The probe bullet says "the result is recorded, or the blocker is and the docs keep 'documented, not tested'". It leaves out outcome (b), where the fence is cut to the six exact names and the globs are not offered.
- Suggested fix: Reword the docs bullet: `docs/HANDOFF.md` names the 16 groups, and `docs/COMMANDS.md` says in one clause that they pass. Reword the probe bullet: the docs say what the probe's outcome supports.
- Resolution: open

#### R4. Step 3's Log line is committed by no named step, and the leak scan runs before the last commit
- Area: 9. Testability (step 6)
- Finding: Step 3 commits nothing, and steps 4 and 5 stage only their own paths. So step 3's Log line in `decisions.md` is still uncommitted when step 6 starts, and step 6's "on a clean tree" can't hold as written. `reports/leakscan.sh` scans only the committed diff `origin/main..HEAD`. Step 6 runs it before its own commit, so neither the probe's Log line nor step 6's record is ever scanned. The probe's Log line may name the scratch folder's local path, and those two entries are the ones most likely to hold local paths or identifiers.
- Suggested fix: Name the commit that carries step 3's Log line (step 4's, staged by name). Have step 6 run the leak scan again after its own commit and give the result in its report, not in the Log.
- Resolution: open

## Checklist items not applicable

This plan changes PowerOps itself: one Core class, its tests, two docs pages and the ecosystem records. It builds no Power Platform solution and makes no tenant read or write. Its probes run with no Power Platform sign-in: pac's `tools/list`, `pac <group> help`, and two headless Claude Code runs whose prompt needs no tool.

1. Solution boundaries and publisher prefix: not applicable (no solution; `plan facts` not read).
2. Connection references and environment variables: not applicable. The tests use fake values (`x`, `/x/cert.pfx`, the test files' own environment constants), and Constraints forbids tenant values.
3. Premium connectors and licences: not applicable.
4. DLP fit: not applicable (no connector is used).
5. Dev to Test to Prod route: not applicable to a solution. The code goes branch, PR (after M84's) and merge, and each waits for the owner's yes. Step 2's `fix:` commit makes a patch release, and step 6 sets `done`.
6. Managed or unmanaged: not applicable.
7. Data volume and delegation: not applicable.
8. Security roles: not applicable to Dataverse. Each block that becomes a pass either holds no import or route marker or is a segment whose program the shell takes from a later word. The new `Check` in R2 is an import the gate reads in full and checks against the plan. Row 6 (f) says the deny fence covers only the gated tools and the preview groups, and it names the non-preview writes to add.
9. Testability: every step has a done-condition and a check. Row 4 pins the reading changes, and step 2 runs the tests red before green and stops on a pinned case that disagrees with the code. Findings: R1, R2 and R4.
10. Unanswered areas: Q1 is the owner's. The planner decided Q2 to Q8 within it, and the orchestrating session decided the first pass's findings, all logged. There are 0 open questions and 0 admin questions. Finding: R3.
11. Size: there are no tables, lists, flows or screens. Each change traces to a Done-when line, and `Won't build` is present. No finding.
12. Tools: all the tools the steps name are on the `PATH` at the versions Assumptions gives. No finding. R1 covers which `pac` the probe starts.
