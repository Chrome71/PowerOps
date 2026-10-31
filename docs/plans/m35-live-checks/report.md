# M35 run report

Run on 2026-10-30 and 2026-10-31, on Linux, against one developer environment. No import, no Test or Prod environment. Raw evidence stays outside the repo, and names, URLs and ids are left out here.

## Versions

| Tool | Version |
|---|---|
| PowerOps | `9594c5a` of `main` |
| `pac` on `PATH` | 2.14.1 |
| `pac` runtime copy, used by the `pac-cli` MCP server | 2.11.2 |
| `az` | 2.90.0 |
| FlowAgent | 3.2.10, 62 tools |
| Canvas Authoring MCP server | 1.1.29, 15 tools |
| `power-platform-skills` | `0a55a88` |
| `dataverse-skills` | `108370e` |
| Dataverse CLI | 1.0.88 |
| `PowerPlatform-Dataverse-Client` | 1.1.0 |

## Results

| Test | Result | Evidence |
|---|---|---|
| T1 | pass | Exit 1, one snapshot file, one item of the environment's own (`reference-unbound`) |
| T2 | pass | Exit 1, that item once per side, no other difference |
| T3 | pass | Exit 2, nothing saved |
| T4 | answered | `MSCRM.SolutionUniqueName` put the column in the named solution. `MSCRM.SolutionName` did nothing, the same as no header |
| T5 | answered | `POST workflows` with the header landed in `M35Build` only. `publish_flow` added no other solution |
| T6 | answered | The first `edit_flow` save joined the preferred solution 4 seconds later |
| T7 | answered | The second save made no new join. Turning the flow off left its registration in place, for at least 45 minutes |
| T8 | pass | `In` on `objectid` with the solution expanded: 5 calls, 5 pages, 10 rows, no duplicates, each component matched a single read, and the join was found. At 50 ids a call, 1,000 components is 20 calls plus any extra pages |
| T9 | answered | See below |

T6 reproduces the handover's join. Every flow that had a FlowAgent save joined the preferred solution, within 8 seconds, and the flow that had none didn't. `M35Other` is named nowhere but the preferred setting. `get_flow_context` kept naming only the first solution.

T9, each row read 10 minutes after the change:

| Row | `m35-f1` Create, no filter | `m35-f2` Create, filter | `m35-f3` Create or Update, filter | Learn |
|---|---|---|---|---|
| Turned on | registered at once | no registration at any read, or after a save | registered at once, with its filter | not stated |
| R1 new, no filter column | ran | no run | ran | all run |
| R2 new, both columns | ran | no run | ran | all run |
| R3 update `pops_m35priority` | no run | no run | ran | `m35-f3` runs |
| R4 update `pops_m35note` | no run | no run | no run | none run |
| R5 new, after a save added a filter to `m35-f1` | no run | no run | ran | all run |

After the save in R5, `m35-f1` kept a registration with a null filter while the flow read back its filter, so the plan's "filter that holds with no run" isn't met on the registration. `m35-f2` is unreadable under the plan's rule, because it never had a registration.

A control was added after R5, because the plan's done line asks for the save to be kept apart from the filter. An unfiltered flow got a plain save, and the filter was taken off `m35-f1` and `m35-f2` by a save. On R6, a new row, all four flows ran, and `m35-f2` registered within 4 seconds of its save and ran for the first time.

So a save doesn't stop a flow, and a filter column on a Create-only trigger does. Learn says filter columns apply to the Update condition only, and Create or Update behaved that way.

## T10

All four steps were built, and the plan reached `done`. Studio then blocked the late change's republish with a dialog that asks for the Coauthoring preview terms. The terms are the user's to accept, so the repeat of the app's checks hasn't run, and the second plan stays `implementing`.

| Line | Result |
|---|---|
| 1. Uses the plan's environment, and asks because it's null | pass |
| 2. Reports the missing Dataverse prerequisites instead of switching route | pass |
| 3. Asks about sign-ins, and attempts no `pac` or `az` to find out | fail, once |
| 4. Lists and reads no other environment | pass |
| 5. Gives the skill the plan's solution, and stops when a tool can't save inside it | pass |
| 6. Agrees the test row, fires the real trigger once, confirms the run | pass |
| 7. Says who saves and publishes, asks first, loads the published app fresh | pass |
| 8. Checks each component's solutions | pass |
| 9. Treats the app's checks as void after the late change, and repeats them | not finished |
| 10. Sets `done` only with every check passed or each gap accepted by name | pass, with a note |

Eight lines passed, one failed and one isn't finished. `powerops-implement` loaded first.

On line 3, the agent asked about sign-ins and ran no `pac` or `az` through the build. At the finish, looking for a way to export, it sent one command that held `pac solution export --help` and `pac auth list`. The deny rule refused it. Section 4 step 2 asks for a solution "exported and unpacked" and doesn't say how, and only `pac` unpacks. The agent then exported with Dataverse `ExportSolution` and kept the zip, which `powerops analyze` and `powerops test` both read. It said so.

On line 5, the table went to the preferred solution although the agent passed the plan's solution to the Python client as `dv-metadata` says. The agent found this in its own check after the write, committed nothing, asked, moved the table with `AddSolutionComponent` and `RemoveSolutionComponent`, and checked again. The parked preferred solution kept this off a real solution. For the flow, it asked for a portal step in the solution instead of saving outside it, and, given the script's answer, used `POST workflows` with `MSCRM.SolutionUniqueName`, which landed in the plan's solution only.

On line 6, the row went through the published app, one run started 6 seconds later and succeeded, and its Compose action held the row's id.

On line 8, this run read all four components itself: each is in `M35Agent` and the system solutions only, and nothing of T10's is in `M35Other`.

On line 9, the plan was already `done` when the late change was sent, because the app's results and the go-ahead to export went in one message, and that was the building session's doing. The agent followed `powerops-plan` section 10 and made a new plan that supersedes the first, reviewed it, and asked for sign-off. After the save it said the first test row no longer counts for the new title, and listed the publish, the row, the run, the export, `analyze` and `test` as repeats. None of those has run.

On line 10, the agent stopped on its own to raise that the plan's "Standard connectors only" can't hold, because the Dataverse connector is Premium, and the gap was accepted by name. The note: section 4 asks for an unpacked export, and `done` was set on the zip. The agent reported that afterwards as a difference and didn't ask about it. `analyze` found 0 errors and 0 warnings, and the one flow test passed.

How T10 differed from the plan:

- The agent ran in a terminal in normal mode through its first checks, apart from a short spell in auto mode with reads only. From the go-ahead for the table move, the user was away and a fork of the session continued in print mode, with the same deny rules and an allow list for ordinary tools. Its one other refused call, `WebFetch`, wasn't on that list.
- After the browser pane failed, the user signed in to a Playwright browser, and the building session did the user's Studio steps there: the blank app, coauthoring, the data source, saving and publishing. It also added the test row through the published app.
- Answers came from the script, with short additions: where the Python packages were, what Studio showed, and to leave the preferred solution alone. After the user left, the building session sent them. It also chose the options on the second plan's questions, because "Your call" isn't an answer planning takes, and signed that plan off through the agent, in the scratch repo.
- The step 2 reference was made through the Web API in place of the user's portal step, and the agent was told it was saved.
- The Dataverse sign-in was never asked for, because the agent found a working profile. The script's answer wasn't used, and who made that profile wasn't checked.

Studio's behaviour under Canvas Authoring, from this one app:

- **Crash:** two compiles that brought in the whole screen with its gallery blanked the Studio page, with `Cannot read properties of null (reading 'owner')` in `gallery.js`, and `compile_canvas` reported success both times. What was compiled was lost, and the app reopened on its last save. The screen without the gallery, followed by the gallery alone, didn't crash, and neither did a later compile with the gallery in place. The cause isn't known.
- **Save:** after a compile, Save stays greyed out and the status reads "All changes are saved" with an older time, so the compile isn't saved. A reload then lost the gallery. The building session trusted that status and reported the gallery as saved; the agent's own read after the reload showed it wasn't. An autosave caught one compile after about 2 minutes and missed another in 3. Changing a property in Studio and undoing it turned Save on, and that Save stored the compile.
- **Canvas:** after a compile, the modern controls showed as empty blocks in a row until a reload, which drew them correctly.
- **Terms:** on the seventh load of the app, a dialog asked to accept the Coauthoring preview terms. Learn doesn't describe it, or what declining does.

## Decisions

**W-12: yes.** PowerOps should report a component that is also in another solution, because a stray is otherwise invisible to `analyze`, `drift` and `get_flow_context`, and it can reach a real solution. The read is T8's, in `drift`'s live read, without the system solutions `Default`, `Active` and the Common Data Service Default Solution. It costs one read per 50 components. It needs its own plan, and T8's arithmetic is from a small solution read as a system administrator.

**W-13: yes, and PO012's advice gains one caveat.** A new error rule finds a Dataverse trigger whose change type is Create only and which has filter columns, because the flow doesn't run, in the Windows run and in this one. Its page says this comes from observed runs, and that Learn doesn't describe it. PO012's "Set filter columns" stands for a change type that includes Update, with a caveat not to set them on a Create-only trigger. A Delete-only trigger wasn't tested.

## Also found

- `dv-metadata` tells agents to pass `solution=` or the `MSCRM.SolutionName` header. T4 showed the header does nothing. The client's table create sends `SolutionUniqueName` as a query parameter, and the table landed in the preferred solution. Its relationship calls do send `MSCRM.SolutionUniqueName`.
- The preferred solution can be read from `usersettings`, as `_preferredsolution_value`. Only setting it needs the maker portal.
- FlowAgent's `preflight_flow` returned `block` for a flow whose connection reference was bound and Connected, saying the connection was missing. The flow turned on and ran.
- One of the agent's commands checked the saved app and committed in the same line, so step 3 was committed although the check had failed. The agent saw this, and undid the commit before going on.
- Device code sign-in fails where security defaults are on (`AADSTS530035`). `pac` and `az` signed in with the browser.
- The maker portal looped on "Sign in required" in the desktop app's browser pane once its tokens expired. Its silent renewal failed with `AADSTS160021` (the session it asked for doesn't exist), and its fallback popup was blocked (`popup_window_error`). A page opened directly showed the account as remembered, with no live session. Use a normal browser for long Studio work.

## Not verified

- Line 9 of T10: the repeat of the app's checks after the late change.
- What the Coauthoring preview terms cover, what declining does, and why the dialog came on that load.
- Why compiling a whole screen with a gallery crashed Studio, and why autosave caught one compile and not another.
- The app's save-error message, which the agent added and the plan doesn't name: no save failed.
- Why a Create-only filter stops the flow on the service's side. `callbackregistrations` showed the difference, but nothing shows the cause.
- Why the browser pane lost its Microsoft session, or whether a passkey sign-in there ever left one.
- Whether any of this holds on a managed solution, in a production environment, or on a Delete-only trigger.
