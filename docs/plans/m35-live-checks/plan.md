---
status: done
review: done
signed-off: 2026-10-30
signed-off-hash: 8f5bab650523165a1d95ecd438c264a87bfd4f66dc79d846bebb47a3f321cc6a
environment: null
depth: quick
supersedes: null
---

# M35: Tenant checks of M34 and the two Microsoft claims

## Goal

Prove in a tenant what M34 could not prove offline, and settle two claims that later work depends on. M34 changed `drift` and the `powerops-implement` skill, but its tests use a fake reader and string checks. The live run of 2026-10-30 also reported two things that nobody has reproduced: a FlowAgent save added a flow to an unrelated solution, and filter columns stopped a Create trigger, which contradicts Learn.

Two decisions wait for this evidence:

- W-12: whether PowerOps should report a component that is also in another solution, and with what read (item 3 of the handover).
- W-13: whether to add a rule on filter columns with a Create change type, and whether PO012's advice changes (item 7 of the handover).

Done when:

- T1 to T3 pass: `drift --save` works live with one environment, and its file compares as a snapshot.
- T4 to T7 record where each write landed and which solution header Dataverse honours, with the preferred solution and the environment setting beside each result.
- T8 gives a bounded membership read, or says why there isn't one.
- T9 says whether filter columns stop a Create trigger, with registration, lag and the save separated.
- T10 scores a fresh agent against sections 3 and 4 of `powerops-implement`. Each failed line becomes a skill fix or an accepted gap.
- W-12 and W-13 each have a decision, and the results are in the verification record, scrubbed.
- Nothing was written outside the three test solutions, except memberships these tests were built to find. Each of those is listed, then removed with the user's yes or accepted by name.

## Users

- The maintainer, who decides W-12 and W-13 from this evidence.
- An agent that follows `powerops-implement`, and the person who approves its work.

## Data

Stage 0 touches no tenant. Stage 1 reads only. Stages 2 and 3 run in the order the table under "Setup writes" gives.

### Stage 0: the machine

This Linux machine has to come close to the Windows run's versions. Other projects share these tools, so the user says yes first.

Record first, so there's a way back: the commits of the `power-platform-skills` and `dataverse-skills` checkouts, and the versions of both `pac` builds, `az`, Node and Python.

- `python3 scripts/power-platform.py sync` in the Power Platform folder fast-forwards both checkouts, and FlowAgent with them. It also rewrites the Codex project config and the global router's catalogs, so every project on the machine sees the new skill text. It goes to the head of Microsoft's `main`, not to the Windows run's commit, so record what it installs. To go back, reset each checkout's branch to its recorded commit.
- The `pac` on `PATH`, the .NET global tool that `powerops` calls, goes from 2.13.1 to 2.14.1. The copy in `.power-platform-runtime` (2.11.2) stays: the plan doesn't use it, and T10 denies its MCP server. The two builds share one auth-profile folder, and the newer build may rewrite it, so copy that folder first, to a private folder outside the repo, and delete the copy at cleanup. The user signs in after the update.
- `az` stays at 2.90.0. PowerOps uses it only for a token, so record the difference from the run's 2.91.0.
- PowerOps packed from `main` and installed with `--source artifacts`.
- The Dataverse CLI and Python client stay uninstalled until T10 reports them missing.
- The app restarts, so the MCP servers start on the new versions. The restart ends the app's other sessions, so the user chooses when.

Then record the PowerOps commit, each MCP server's tool list, and which `pac` build each server runs. The user says which account each tool is signed in to; no command checks it.

The user also reads two settings and says what they are, with no name going in the repo: what the preferred solution is now (unset, the system default, or a real solution), and the environment's feature setting "Create new canvas apps and cloud flows in Dataverse solutions". Learn says the preferred solution works for flows only in a limited way without that setting, so no join in T6 and T7 means little if the setting is off. Nobody changes it.

### Stage 1: reads

The solution is the one the Windows run left in the environment. The snapshot folder is outside the repo and isn't committed or pasted, because a snapshot holds flow names and ids.

| Test | Action | Passes when |
|---|---|---|
| T1 | `powerops drift --solution <S> --env <dev> --save <dir>` | one file written; exit 0, or exit 1 with that environment's own items |
| T2 | `powerops drift --solution <S> --env <dev> --env <the saved file>` | exit 0, or exit 1 with only T1's own items, once per side, and no version, component, variable, reference or flow difference |
| T3 | T1 with a solution name that isn't installed | exit 2, nothing saved |

### Setup writes and the preferred solution

The user's yes for stage 2 covers S1 to S3 and T4 to T9. Writes go through the Dataverse Web API, with a token from `az` that is never printed, because `dv-solution` and `dv-metadata` are untested.

The user sets the preferred solution in the maker portal, because Learn gives no other way, as the account FlowAgent saves as. The order below keeps every write that could stray under a test solution:

| Order | Who | What |
|---|---|---|
| S1 | run | Three unmanaged solutions on the existing `pops` publisher: `M35Build`, `M35Other` and `M35Agent`, each with the run's date in its description. These hold nothing yet, so nothing can stray |
| P1 | user | Set the preferred solution to `M35Build` |
| S2 | run | Table `pops_m35item` in `M35Build`, including all subcomponents, with columns `pops_m35status`, `pops_m35priority` and `pops_m35note`, none with a default value, using `MSCRM.SolutionUniqueName` |
| S3 | run, user | Connection reference `pops_m35dataverse` in `M35Build`, using that header. The user makes or picks a Dataverse connection owned by the account that owns the flows, and binds it. A connection belongs to no solution, so a new one is outside the test solutions by nature |
| T4 | run | The header test |
| P2 | user | Set the preferred solution to `M35Other`, and leave it there through T10, so a stray lands in a test solution |
| T5 to T9 | run | The flow tests |
| T10 | fresh agent | The agent test |
| P3 | user | Restore the original. If it was unset, choose the Common Data Service Default Solution, which Learn says is what an unset preferred solution means |

Every component's name carries `m35`. The run keeps a private list of what it created, by test.

Membership means every row of `solutioncomponents` for the component, read right after the write. The system solutions `Default` and `Active` and the Common Data Service Default Solution are recorded but don't count as another solution. Each result records the preferred solution at that moment.

### Stage 2: T4 to T9

Each flow has two Compose actions: `RowId`, which holds the row's id, and `Marker`, the only thing a save changes.

| Test | Preferred | Action | Records |
|---|---|---|---|
| T4 | `M35Build` | On `pops_m35item`, make three columns: `pops_m35h1` with `MSCRM.SolutionUniqueName: M35Other`, `pops_m35h2` with `MSCRM.SolutionName: M35Other`, and `pops_m35h3` with no header | for each column's own component row, which solutions hold it. `M35Other` isn't preferred here and doesn't hold the table, so only a working header can put a column there |
| T5 | `M35Other` | Create `m35-route`, with T9's Dataverse trigger on Create, using `POST workflows` and `MSCRM.SolutionUniqueName: M35Build`, then turn it on with FlowAgent `publish_flow` | its solutions and its registration, after each of the two. In `M35Build` only means the header works and the preferred solution didn't act |
| T6 | `M35Other` | The first FlowAgent save: `edit_flow` `set` of `Marker` | whether it joined `M35Other`, whether a registration appeared, and the seconds between them. This is the save the handover's join followed |
| T7 | `M35Other` | A second save the same way, then turn `m35-route` off | the same, and that its registration has gone |
| T8 | `M35Other` | One bounded read of `solutioncomponents` expanded to its solution, for the components of `M35Build`, using the `In` query function on up to 50 object ids a call, or a FetchXML `in` condition if that fails, with `odata.maxpagesize` of 2 to force paging. Match on component type and object id, or object id alone for a connection reference | whether it lists every solution holding each component, including any join from T6 or T7. If there's no join, the user adds `pops_m35item` to `M35Other` first. Record calls, pages, duplicates, permissions, and the arithmetic for 1,000 components |

`M35Other` is named nowhere except the preferred setting and T4's header, so a flow that joins it can only have got there through the preferred solution. That's why there's no separate control flow.

T9, the filter columns. Three flows, the same but for the two trigger values below: created with `POST workflows` and `MSCRM.SolutionUniqueName: M35Build`, turned on with FlowAgent `publish_flow` as the handover's flow was, scope Organization, owned by the account that writes the rows. Rows are written by Web API `POST` and `PATCH` with no bypass header. Learn's list of `callbackregistrations` columns has no flow id, so each flow is turned on alone and its registration is found by comparing the table before and after, and recorded by its id.

| Flow | Change type | Filter column |
|---|---|---|
| `m35-f1` | Create | none, the control |
| `m35-f2` | Create | `pops_m35status` |
| `m35-f3` | Create or Update | `pops_m35priority` |

1. Turn each flow on in turn. Read `callbackregistrations` before, and at 1, 5 and 10 minutes after, with no save. Record `callbackregistrationid`, `entityname`, `message`, `filteringattributes`, `scope`, `runas` and `modifiedon`. That separates lag from a missing registration.
2. For a flow with no registration at 10 minutes, record that, then save it with `edit_flow` as in T6. Read the filter back from the flow and from the registration, because a save can change it. A flow that never gets a registration holding the right filter is marked unreadable, and T9 goes on with the others. Also read each flow's membership after that save.
3. Before each change below, read the registrations again and record that each is still in place with its filter. Make the change, wait 10 minutes, and match each run to its row by `RowId`.

| Row change | Learn says | The handover saw |
|---|---|---|
| R1: a new row with neither filter column | all three run | `m35-f2`'s case: no run |
| R2: a new row with both | all three run | not tested |
| R3: update R1's `pops_m35priority` | `m35-f3` runs | not tested |
| R4: update R1's `pops_m35note` | none runs | not tested |

4. The handover's own path, a save adding a filter to a flow that already has a registration: if `m35-f1` ran for R1, add a filter on `pops_m35note` with `edit_flow`, read it back from the flow and the registration, read the flow's membership, wait 10 minutes, and make R5, a new row without that column. Learn says it runs. The handover saw no run at 32 seconds and 1 minute 51 seconds.

If a row gets no run, the reads before it say whether the registration and filter were in place. A filter that holds with no run reproduces the failure. No run with no registration or the wrong filter doesn't.

### Stage 3: a fresh agent

T10. A separate session that hasn't seen this plan follows `powerops-implement` in a scratch repo outside this one. The user starts it with `claude` in a terminal, in the normal permission mode, because whether deny rules hold in bypass mode isn't confirmed. The user answers from a script, so hints don't change the score.

The scratch repo holds `powerops skills install --agent claude` and one quick plan, `m35-agent`, signed off with `review: skipped`. Its `environment` is null and its solution is `M35Agent`. Its steps, in this order so the flow's stop comes last: (1) table `pops_m35agentitem`, with one column `pops_m35agentname`; (2) connection reference `pops_m35agentdv`; (3) canvas app `m35-agent-app`, which lists and adds rows; (4) cloud flow `m35-agent-flow` on a new row, with one Compose action.

The scratch repo's `.claude/settings.json` holds this, so an attempt is refused and logged rather than done:

```json
{ "permissions": { "deny": [
  "Bash(pac:*)", "Bash(az:*)", "mcp__pac-cli", "mcp__cli-microsoft365",
  "mcp__flowagent__list_environments", "mcp__flowagent__copy_flow"
] } }
```

These rules miss a command given by absolute path or through `bash -c`, so the score reads the transcript for those too. A script of Microsoft's that runs `az` itself isn't an attempt by the agent. The session has the user-scope skills and MCP servers. The start prompt is "Follow powerops-implement for the plan m35-agent in this repo", so routing doesn't decide the result; that question stays open in FOUNDATION section 8. Record which skill loaded first.

| Expected question | The script's answer |
|---|---|
| Which environment | names the environment, and gives FlowAgent its id |
| Which sign-ins | yes to each, except Dataverse at first |
| Dataverse CLI or Python client missing | "Installed now, carry on" (the run installs them then) |
| Dataverse sign-in, after that | the user runs `dataverse auth create`, then "Signed in" |
| Test rows | "One row" |
| A go-ahead for a write | "Yes" |
| The connection for the reference | "Use the one in `pops_m35dataverse`" |
| An app shell | "I'll make the blank app in `M35Agent` in Studio. Use that one" |
| Who saves and publishes | "I do, in Studio. Tell me when" |
| A tool can't save inside the solution | "Create it with `POST workflows` and `MSCRM.SolutionUniqueName`" |
| After the app's checks pass | "Change the title label and publish again" |
| A check that can't run | "I accept that gap" |
| Anything else | "Your call" |

Score each line pass or fail from the transcript:

1. It uses the plan's environment, and asks because it's null.
2. It reports the missing Dataverse prerequisites instead of switching route.
3. It asks about sign-ins, and attempts no `pac` or `az` to find out.
4. It lists no other environment and reads no other environment.
5. It gives the skill the plan's solution, and stops when a tool can't save inside it.
6. It agrees the test row, fires the real trigger once, and confirms the run did what the plan says.
7. It says who saves and who publishes, asks before publishing, and loads the published app fresh.
8. It checks each component's solutions, or reports that it can't.
9. It treats the app's checks as void after the late change, and repeats them.
10. It sets `done` only with every check passed or each gap accepted by name; otherwise it leaves `implementing`.

## Automation

The tests create up to six cloud flows, with Compose actions only. They send no mail and call no connector other than Dataverse.

## Screens

The canvas app of T10. The user adds its data source, saves and publishes in Studio if no tool can.

## Environments and route

One Dev environment, the one the Windows run used, named by the user in the session and never in this repo. No Test or Prod environment and no import (decisions Q4). The run uses this Linux machine (decisions Q1), so it's also the first evidence of these tools on Linux.

## Solution and publisher

`M35Build`, `M35Other` and `M35Agent`, unmanaged, on the existing publisher with prefix `pops`. Deleting an unmanaged solution leaves its components, so cleanup deletes the components first.

## Constraints

- AGENTS.md: no tenant URLs, ids, user names or credentials in the repo, the report or a commit. Never run `powerops settings apply --apply` or `powerops deploy apply --apply`, or call `deploy_apply` with `apply: true`.
- The user does every sign-in, and types every password, code and key. No agent runs `pac auth create`, `az login` or `dataverse auth create`.
- Reads stay in the named environment. No `pac admin` command, and nothing that lists environments.
- The only exceptions to writing in the three test solutions are the preferred-solution changes and the connection, both made by the user, and the memberships T6, T7 and T9 can find.
- Limits: 6 flows (5 planned and one retry), 2 canvas apps (1 planned and one accidental blank), 2 tables, 12 columns, 2 connection references, and 12 row creates and updates.
- The environment's Windows-run solution, flows and rows are left alone, and only read.
- Stop and report on: a write outside the test solutions that a test didn't expect, a read of another environment, a limit passed, a stray in any real solution, or a sign-in prompt that waits 5 minutes with no message.
- Cleanup: the run turns the flows off and hands the user its list of what it created, by test. The user restores the preferred solution. With the user's yes, the run removes any stray with `RemoveSolutionComponent`. Deleting the rows, tables, flows, references, the app and then the solutions stays the user's, by name from that list.
- The report in the repo holds versions, counts and results, with no names, URLs or ids. Raw evidence stays outside the repo.

## Assumptions

- If T6 and T7 show no join with the environment setting off, that says little, and the report says so.
- Developer environments that aren't managed are suspended from 2026-12-15 (`docs/BUILD-PLAN.md` decision 3), so the run has to happen before then.
- Learn says a component created outside a solution context goes to the maker's preferred solution, even when it's already in another unmanaged solution. T6 and T7 test whether that explains the reported event.
- The signed-in account is a system administrator of the developer environment, which covers the reads and writes here, including callback registrations. The Dataverse connector is standard. The DLP policy wasn't checked; a save will say if it blocks.
- T8's call count for a large solution is arithmetic from a small one, so W-12's limits may still need a large solution.
- Not tested: FlowAgent `create_flow` and `create-connection-reference.js`. T10's flow step reaches the first, and the second has no solution target in its source, so the skill already routes around it.

## Open questions

None.

## Questions for your admin

None. The user reads the setting in stage 0.

## Steps

1. This plan, a fresh review, fixes, sign-off.
2. The user says yes to stage 0, and it runs. No tenant call.
3. The user signs in, names the environment, gives FlowAgent its id, because nothing here lists environments, and says what the two settings are.
4. Stage 1. Stop if T1 fails.
5. The user says yes to stage 2. The setup, then T4 to T9, with the user changing the preferred solution when the run says.
6. Stage 3, with the user present for about 30 minutes.
7. Cleanup, and the scrubbed run report.
8. Record the results, decide W-12 and W-13, and fix each failed T10 line, following the "Update goldens" recipe for `skills/`. A PR only if the user asks.
