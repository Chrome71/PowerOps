plan-hash: c06fe8dd8b57b527349b13420f611c1ae99302baad985edb424057e8d26d9f3d
model: claude-opus-5-5
fresh-context: yes

Second pass on 2026-11-11, against the revised plan (the hash above) and the two new Log lines of `decisions.md`: each fix was checked in the plan text, and no new finding came up. The step 2 count of 9 existing PO007 tests matches the tests whose names hold `PO007` in the Core test project.

Environment facts were not read: the plan's `environment:` is null and no `pac` sign-in was confirmed, so `powerops plan facts` was not run. No tenant was called.

The plan changes the PowerOps analyzer (rule PO007), not a Power Platform solution. Checklist items 1 (solution and publisher), 2 (connection references and environment variables in the plan's own build), 3 (licences), 4 (DLP), 5 (route), 6 (managed or unmanaged), 7 (delegation), 8 (security roles) and 11 (size: no table, list, flow or screen) are not applicable; the plan marks each as none. Item 10: 0 open questions, 0 admin questions, and the areas not asked are logged with a reason. Item 12: `dotnet` and `powerops` are on PATH (the installed CLI is the commit `b065f23` the plan names); the leak scan is under R6.

Checked against the code at `b065f23`: the parameter loop is at `FlowDefinition.cs:104-111` as row 2 says; `DataverseActions` is an init property with a `[]` default, so row 1's pattern fits; PO007's three patterns and its trim are as row 3 assumes; PO006 is the only rule that reads `StringLiterals` and it skips `InParameters`, so no other rule prints a parameter default; the SARIF builder writes no snippet or region, so a finding whose message, file and component hold no value prints none in any of the four formats. A survey of every fixture flow (kinds and names only, no value printed) confirms Data row 6: five string defaults that match a pattern, all in `coe-auditlogs`, none of their three variables defined in the fixture; no zip fixture holds a bound default. The CLI test `Coe_auditlogs_has_no_errors_so_default_exits_zero_but_warning_exits_one` already expects exit 1 at `--fail-on warning`, and `FlowDefinitionTests` asserts nothing the change touches. The milestone's two requirements are met by the design: the new message, file and component hold only the flow name, the flow file and the schema name; and a flow default is left to the definition's finding only when the schema matches (ignoring case) and the trimmed values are equal, the same trim the definition check applies, recorded as a routine choice in the Log.

## Findings

### High

No findings.

### Medium

#### R1. The plan doesn't say who writes the default held in the flow, so the finding's fix may not hold
- Area: 9 (testability), Users, Data row 5
- Finding: Users says a reviewer "clears the default in the flow", and Done when 2 keeps PO007's fix text ("Leave the default empty and set the value per environment at deployment time ..."), which is written for a definition. The fixtures suggest the platform itself writes a variable's value into the bound parameter's `defaultValue` when a flow is saved: in `coe-auditlogs` and both `powerops-seed` versions, every bound parameter whose variable has a definition holds exactly that definition's default, and in `alm-sample` the flow holds a value that differs from the definition's default with no current value in the export (consistent with a Dev current value written at save). The Assumptions note that flows cache a variable's value until saved, but stop short of this. If the designer writes the current value on every save, the new warning fires on an ordinary export whenever Dev has a URL, GUID or address as the current value, and a maker who clears the default sees it come back on the next save; `--fail-on warning` then fails with no fix the maker can keep. PO007.md would also give a fix that doesn't apply to the flow case.
- Suggested fix: Add to Assumptions what the fixtures show (as observed in exports, not on Learn) and route the open part to `powerops-researcher` or an observed export: does a designer save write the variable's current value into the flow's `defaultValue`? Then make row 5 give the flow case its own "How to fix" line (what to clear and where, or that a value the platform writes back is accepted with `powerops baseline`), and say in "Why it matters" why a value held in the flow matters (it travels with the export into source control and can be what the flow runs with). If the fix text must change, say so in Done when 2 instead of pinning it.
- Resolution: fixed: Assumptions records the platform write-back as observed in exports and not verified, and no longer relies on the flow-held default being run; row 5 gives `PO007.md` a flow-case "Why it matters" (the value reaches source control and every target) and "How to fix" (empty `defaultValue` in the named file before packing, or accept with `powerops baseline` if a designer save writes it back); the one-sentence `Fix` stays, pinned by the fix plan; Users and the behaviour-change constraint say ordinary exports fire too.

### Low

#### R2. The Sources date would cover a page that wasn't reread
- Area: Data row 5, Done when 5
- Finding: Row 5 keeps PO007's two pages, adds a third, and sets `verified: 2026-11-11` for the whole line. The Log and Assumptions record the 2026-11-11 reading of the overview and the Power Automate page only; the troubleshooting page already on the line was not reread. AGENTS.md says the date is the date the pages were checked.
- Suggested fix: Reread the troubleshooting page in step 4 before setting the date, or say in the Log that it was reread, or drop it from the line if it no longer supports the text.
- Resolution: fixed: row 5 sets `verified: 2026-11-11` only after all three pages are reread that day, else the line keeps the oldest date; Done when 5 says the same.

#### R3. A Won't build line says no export holds a bound non-string default, but the fixtures hold six
- Area: Goal, Won't build
- Finding: "No export read so far holds one bound to a variable" follows a list that includes a number. Six number defaults bound to variables are in the fixtures (`alm-sample`, `coe-auditlogs` and `flowtests/capture`). None holds an object or array.
- Suggested fix: Say that no export read holds an object or array default bound to a variable, and that bound number defaults exist and can't match a URL, GUID or address pattern.
- Resolution: fixed: the Won't build line now says no export holds an object or array default bound to a variable, and that bound number defaults exist and match no pattern.

#### R4. No fires case for a flow default with surrounding whitespace, which the fixture count relies on
- Area: 9 (testability), Data row 4
- Finding: Both address defaults in `coe-auditlogs` have whitespace around them, so the email pattern matches only after the trim of row 3; two of Data row 6's five findings rest on it. Row 4 tests a whitespace-only default (quiet) but no padded value that fires, so a build that drops the trim fails only on the golden diff.
- Suggested fix: Add a fires case to row 4: an address default with leading and trailing spaces, bound to a variable with no definition.
- Resolution: fixed: row 4 adds a padded address that fires with no definition, and a padded default equal to the definition's after trimming that stays quiet.

#### R5. Leak-test inputs can collide with names the finding prints
- Area: 9 (testability), Data row 4
- Finding: The leak test asserts that the message, file and component hold neither the value nor its local part, host or domain. `B.Flow` writes its default id (a GUID) into the workflow file path, and the message holds the flow and schema names, so a GUID case that reuses the flow id, or an address whose local part matches part of a name such as `demo_Mail` (the check is easiest to write ignoring case), fails for the wrong reason or gets weakened to pass.
- Suggested fix: Say in row 4 that each fake value shares no part with the flow name, the flow id or the schema name, and that the GUID case uses a GUID other than the flow's id.
- Resolution: fixed: row 4 says each fake value shares no part with the flow name, flow id, schema name or any path, and the GUID case uses a GUID other than the flow's id; the CLI test's temp solution follows the same rule.

#### R6. The leak scan is named without its path, and it isn't in this worktree
- Area: 12 (tools), Done when 7, Step 5
- Finding: Done when 7 and step 5 call "the leak scan". The script is `reports/leakscan.sh` in the main checkout; `reports/` is excluded from Git, so the worktree the build runs in has no copy.
- Suggested fix: Name the script and say it runs from the main checkout against this worktree, or add it to Assumptions.
- Resolution: fixed: Assumptions names `reports/leakscan.sh` in the main checkout, says it is not in the worktree, and gives the command run from there.

#### R7. decisions.md holds three page addresses
- Area: privacy rule of this review
- Finding: The research line of the Log names three Microsoft documentation pages by address, kept past `powerops plan status` by an allow marker. This review's rule reports any address in the plan or `decisions.md`.
- Suggested fix: Name the pages by title only, as Assumptions already does, and drop the marker; PO007.md holds the links.
- Resolution: fixed: the research line of the Log names the pages by title only and the allow marker is gone; no address, GUID or email is left in `plan.md` or `decisions.md`.
