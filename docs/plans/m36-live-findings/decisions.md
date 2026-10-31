# Decisions: m36-live-findings

## Round 1

### Q1. What severity does the Create-only filter rule get?
1. `Error`, because the flow doesn't run, which is what AGENTS.md says an error means (recommended)
2. `Warning`, because the result is observed and Learn doesn't describe it
Answer: 1. The flow didn't run in the Windows run or in M35, and in M35 taking the filter off made it run. The page says where this comes from.

### Q2. Which change types does the rule cover?
1. Create only, the one type tested (recommended)
2. Create only, Delete only and Create or Delete, because Learn says filter columns apply to Update only
Answer: 1. A rule at error level shouldn't go past what was seen. Create or Update with a filter fired on create, so a guess from Learn's sentence alone isn't safe.

### Q3. Is a component in another solution drift?
1. No, an information kind that leaves the exit code alone (recommended)
2. Yes, drift with exit 1
Answer: 1. Sharing a component between solutions is legitimate, and the tool can't know what was meant. Exit 1 would fail every run of a solution that shares a table on purpose.

### Q4. Where does the read run?
1. Only on `drift`'s live read (recommended)
2. On `deploy`'s target read as well
Answer: 1. `deploy plan` doesn't report it, and a read there adds cost for nothing.

### Q5. Is the result stored in the snapshot file?
1. No: live only, and the docs say a saved snapshot holds none (recommended)
2. Yes, as a new field of the file
Answer: 1. It's a check of one environment now, not a value to compare later, and the file format stays as it is.

### Q6. Which form does the read take?
1. FetchXML through `DriftLiveReader.Read<T>`, like every other read of `drift` (recommended)
2. OData `In` with `$expand`, the form T8 proved
Answer: 1. Read on 2026-10-31 in the M35 environment, with the stored query, 50 ids (46 of them made up) and a URL of about 4,300 characters: HTTP 200, and 3 rows with the aliased field `s.uniquename`. The four real ids were the flows of `M35Build`: the three that joined `M35Other` had a row each, and the one that didn't had none. T8's paging isn't needed: `drift` reads one page, and `Rows` refuses more.

### Q7. How are the system solutions left out?
1. `ismanaged` 0, `isvisible` 1, `uniquename` not `Default`, and `friendlyname` not one of Learn's two spellings of the default solution's name (recommended)
2. `solutiontype` 0, which was on every user solution and on no system one in that environment
3. By the publisher's name
Answer: 1. The default solution's unique name is generated, so it can't be named. Read in that environment on components of the default solution: with no filter the rows named `Default`, the default solution and one user solution; with the filters only the user solution stayed. `Active` and `Basic` have `isvisible` false there. Option 2 rests on one environment and hides a stray if an old solution has a null type, while a name that doesn't match only adds a line.

### Q8. Which rows count?
1. Root rows only, with `rootsolutioncomponentid` null (recommended)
2. Every row
Answer: 1. On the same read, the condition turned 15 rows into 1, because a table's subcomponents each have a row. A flow is a root row, and it stayed.

### Q9. Does the read get a cap, as layers do?
1. No: it costs one read per 50 components, and read 3's limit of one page of 5,000 rows holds it to 100 reads (recommended)
2. A cap, with a new information kind when it's passed
3. Send only this solution's root rows, which cuts the reads
Answer: 1. Layers cost one read per component, so their cap matters. Here the worst case is 100 reads, and a failed read exits 3 like any other. A large solution wasn't tested, and the docs say so. Option 3 changes read 3's parser for a saving nobody has measured.

## Log

- 2026-10-31: plan started at the user's request ("Yes, plan M36", in answer to "Should I plan M36 from these results?")
- read on 2026-10-31: the M35 report; AGENTS.md; the M34 plan (layout); `powerops-plan`; PO012; `DriftCompare`, `DriftQueries`, `DriftParsers`, `Snapshot`, `SnapshotFile`, `DriftLiveReader`, `DriftReport` and their tests; Learn "Set the preferred solution", "Use a solution to customize" and "Create a solution"
- capture: read-only, in the M35 environment, on 2026-10-31, as Q6 to Q8 say. The raw read stays outside the repo. `dev-read8.json` is one of its rows with the fixture set's fake values
- depth: quick
- approved 2026-10-31 by the building session under the user's delegation of 2026-10-22 (autonomous milestone mode)
- areas not asked: users, data, automation, environments and ALM, and constraints; the changes are to this repo, and the plan answers each or marks it not applicable
- fresh review (general-purpose subagent following `powerops-review-plan`, Sonnet, fresh context) 2026-10-31: 0 High, 5 Medium, 5 Low (R1 to R10), all fixed in `plan.md` and this file (see `review.md`). For R3, Q9 stays "no cap", because read 3's own limit holds read 8 to 100 calls
- the same reviewer re-read the fixed plan 2026-10-31: R1 to R10 fixed, no new High or Medium; one new Low (R11), on how the last check reads its exit code, fixed in Step 5 by the building session and not re-read
- signed off 2026-10-31 by the building session under the user's delegation of 2026-10-22, with no High or Medium open
