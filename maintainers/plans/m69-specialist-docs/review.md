plan-hash: 5b09f52daddba22cbde70fdc84be37e9df4b8c845e5156f12a25f80d6d22666d
model: Sonnet 5.5 (claude-sonnet-5-5)
fresh-context: yes

Environment facts were not read: the plan names no environment. Checklist items 1 to 4 and 6 to 8 do not apply (docs and tests only). Items 5, 9, 10 and 11 apply.

First pass: every cited `docs/HANDOFF.md` line held; `SKILL.md` stays at 2,782 of 2,783 words; `skills/**` is embedded, so a golden refresh covers the new files; the frozen roles are untouched; `powerops deploy gate` exited 0 for `pa app share`, `pa connection create`, `pac copilot quarantine`, `pac copilot merge-translation` and `pac copilot delete`. Learn and GitHub claims agreed (rollup 200/50, `allowPurgeData`, `AllowedAgents` vs `AllowedBots`, 30 controls with 7 in preview, generative pages GA 2025-10-30, the successor README, "delete every duplicate", publish-and-test).

Second pass (build in the tree): each resolution below holds in `plan.md`, the diff and the new `design.md` and `logic.md`. I re-checked on Learn the new text on the Creator Kit (unsupported, use only when no modern control fits), the Fluent UI v8 deprecation and Update's seven controls, the `CustomTheme` and `AppHeaderColors` attributes and classic themes not honoured, and the two rollup limits; all agree. The six WCAG 2.2 A and AA additions in `design.md` are the right six. Not re-checked: code apps' Git-integration line, the XO and Power Pages accessibility lines, the "mandatory from 2026 wave 1" line.

Third pass: R11 and R12 hold. I read Learn's "Power BI Authoring MCP server" page: it says the tools are "grouped by object type", lists create, update and delete among what the agent can do, and has a "Work safely" section (back up, PBIP under Git). One new Low, R13, on the R11 wording.

## Findings

### High

No findings.

### Medium

#### R1. "Deny by full names" names no full names, and the count of writes is nine in one place and ten in another
- Area: 5 and 9
- Finding: Row 5 promised full tool names for servers it did not name, and the count differed (nine, ten).
- Suggested fix: Name the tools by `mcp__<server>__<tool>` and make the count agree.
- Resolution: fixed. Row 5 names the five MCP tools to deny under `mcp__<server>__<tool>`, says why the Power BI Authoring MCP server is listed and not denied, and the count is ten everywhere (decisions Q8). Second pass: holds in the plan and in HANDOFF's deny paragraph; the page itself does not say why Power BI is not denied (R11).

#### R2. The delete traps and the Copilot Studio successor have no test pin
- Area: 9
- Finding: Rows 1, 2 and 6 to 10 had no test.
- Suggested fix: Pin the PBIR sentence, the delete names and the plugin.
- Resolution: fixed. `HandoffDocTests` pins the PBIR sentence, the four delete names and the successor plugin with `migrate` and `delete`; `SkillsCommandTests` pins the PBIR sentence and `allowPurgeData` in `power-bi.md` and `copilot-studio-plugin` in `copilot-studio.md`. Second pass: confirmed in the diff.

#### R3. Generative pages are called GA without the region limit
- Area: 5 and 9
- Finding: The 2025-10-30 GA was US only; global GA came 2026-04-02; the page builder runs in US, GB, AU and SG.
- Suggested fix: Give both dates and the regions.
- Resolution: fixed. `model-driven.md` gives both release-plan dates (US 2025-10-30, globally 2026-04-02) and the page builder's regions, and the Won't build line no longer calls 2026-04-02 a tooling date. Second pass: holds.

#### R4. Another copy of the "no route" list is left to go stale
- Area: 10
- Finding: `docs/COMMANDS.md` line 131 and `docs/PLANNING-SKILLS.md:151` repeat lists that row 5 changes.
- Suggested fix: Change both or say why not.
- Resolution: fixed. `docs/COMMANDS.md`'s `deploy gate` paragraph and `docs/PLANNING-SKILLS.md`'s "Product references" paragraph are in rows 5 and 13 and changed with them (decisions Q9). Second pass: both edits are in the diff and agree with HANDOFF.

### Low

#### R5. Step 10 can still send the agent to the run it forbids
- Area: 9
- Finding: `debug-flow` itself offers to run or test the flow.
- Suggested fix: Say to decline, and fix the Cloud flow row.
- Resolution: fixed. Step 10 says to decline `debug-flow`'s offer to run or test the flow, and the Cloud flow row says "a publish". Second pass: holds.

#### R6. The gate does not see `publish_flow`
- Area: 5
- Finding: Step 10 and row 5 should agree that the user's yes is the only check.
- Suggested fix: One clause in step 10.
- Resolution: fixed. Step 10 says the gate doesn't see `publish_flow`, so the user's yes is the only check. Second pass: holds.

#### R7. Learn gives two rollup limits
- Area: 10
- Finding: The maker page says 200 and 50; the developer page says 100 and 10.
- Suggested fix: Cite the maker page, name the other.
- Resolution: fixed. `logic.md` gives the maker page's 50 and 200 and names the developer page's 10 and 100, both in its Sources. Second pass: holds.

#### R8. `logic.md` is reached only through `data.md`
- Area: 10
- Finding: A plan with no new data would not load it.
- Suggested fix: Also link it from `route.md`.
- Resolution: fixed. `route.md` section 1 also sends an app's rules, status columns and approvals to `logic.md`, and `SkillsCommandTests` pins it. Second pass: holds.

#### R9. `route.md` changes without a dated source
- Area: 9
- Finding: The v8 note had no 2026-11-05 source.
- Suggested fix: Add one.
- Resolution: fixed. `route.md` has a `Checked on 2026-11-05:` source for the v8 note; the `data.md` line is a pointer with no Microsoft claim, so it needs none. Second pass: holds.

#### R10. Done-when 5 excused three failures this worktree cannot have
- Area: 9
- Finding: `docs/research/` is absent here.
- Suggested fix: Require a green `dotnet test` here.
- Resolution: fixed. Done-when 5 now asks the whole `dotnet test` in this worktree and says the three failures occur only in the main checkout. Second pass: holds.

#### R11. The page does not say why the Power BI Authoring MCP server is not in the deny list
- Area: 5
- Finding: HANDOFF's deny paragraph lists FlowAgent and the Copilot Studio MCP server, and says the latter's reads go with the denial. The Power BI Authoring MCP server appears only in "What it doesn't do", with no reason it is not denied (Q8 gave the reason in `decisions.md`, not on the page). A reader may take it for an omission.
- Suggested fix: One clause in the deny paragraph: its reads and writes share tools, so deny it only if you never use it.
- Resolution: fixed. The deny paragraph says the Power BI Authoring MCP server has no write tools to deny on their own, because Learn's page groups its tools by object type, not by read and write, and that each model edit needs the user's yes, with Learn's advice to back up the model and work in PBIP under Git (checked 2026-11-05 on "Power BI Authoring MCP server"); plan row 5 and decisions Q8 say the same. Third pass: holds, with the wording caveat in R13.

#### R12. `logic.md` overstates what a table-scope business rule covers
- Area: 10
- Finding: Its first row says a table-scope business rule holds "however the row is written: app, flow, API, import or agent". The Assumptions support only "Entity scope: forms and server" and canvas apps; imports and agents are not sourced.
- Suggested fix: Say "forms, canvas apps and server-side writes" (the sourced claim), or check import on Learn and cite it.
- Resolution: fixed. The row now reads "A rule that must hold on every write, not only on a form or in one app", and its reason says what the business rule page states: table scope runs on the server as well as on forms. Third pass: holds; the row matches the Assumptions.

#### R13. "Has no write tools to deny on their own" reads as "has no write tools"
- Area: 5
- Finding: Learn's page lists create, update and delete among what the server does, so it has write tools; what it says is that tools are grouped by object type, not that reads and writes cannot be split. The `docs/HANDOFF.md` sentence can be read as "this server cannot write". The page also names a stronger control the text omits: with only Build permission the server can run DAX queries only.
- Suggested fix: Say "its tools are grouped by object type, so a deny rule can't keep its reads and refuse its writes; give the agent Build permission only, or ask first for each edit".
- Resolution: fixed. The deny paragraph now says the server creates, changes and deletes model objects, that Learn groups its tools by object type and names no tool that only writes, so a deny rule can't keep its reads and refuse its edits, and gives the two controls: Build permission only on the model (DAX queries only, Learn's prerequisites) or a yes for each edit. The change is in the page, not the plan, so the plan body and its hash did not change.
