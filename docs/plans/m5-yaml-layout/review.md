plan-hash: 624a169dc5768f43babc18876b1472898ba2c637fb54627a4f749a915247b2c6
model: claude-opus-5-5
fresh-context: yes

Second review, of the plan body after findings R1-R19 of the first review were resolved. Those resolutions were checked against the current plan and hold, except where R1-R3 below show that two of them (one budget per YAML root, one discovered path per `solution.yml`) don't fit together or don't reach the outputs. `powerops plan facts` was not run, because this review must not touch a Power Platform environment.

Checklist items that apply only to the fixture, not to the offline tool: 2 (hard-coded values), 3 (licence), 4 (DLP), 5 (route), 6 (managed or unmanaged), 8 (roles). The plan covers each of them for the fixture, and none of them gives a finding below. Item 7 (data volume) applies to the reader's input limits.

## Findings

### High

No findings.

### Medium

#### R1. A `solution.yml` path breaks SARIF, annotation and JSON file locations
- Area: 9. Testability (and the plan's "Automation: none new" and "CI users ... without changes")
- Finding: Step 4 makes discovery return one path per `solutions/<name>/solution.yml`, and finding paths are relative to the YAML root. `AnalysisResult.Run` uses the discovered path as the solution's display `path`. `SarifReport` builds each location as `path + "/" + file`, and so does the annotation step in `action.yml`. For a YAML solution that gives `<root>/solutions/<name>/solution.yml/modernflows/...`, which doesn't exist, so code scanning and PR annotations point nowhere. The JSON schema also says `file` is inside the solution at `path`, which no longer holds. The goldens updated in Steps 4, 5 and 7 would lock this in, and Done-when only compares rule, component and message, so nothing catches it.
- Suggested fix: State in Step 4 which display `path` a YAML solution gets so that `path + "/" + file` resolves: for example the YAML root, with deduplication still keyed on the `solution.yml` path. Add a test that checks a SARIF location (or the JSON `path` plus `file`) for a YAML finding resolves to an existing file.
- Resolution: fixed: the reported path is the YAML root (Data, Step 4), with a SARIF location test

#### R2. One budget per root and read-once shared files don't fit a loader called once per `solution.yml`
- Area: 7. Data volume (input limits)
- Finding: Assumptions promise one `ReadBudget` for the whole YAML root, with shared component files read once even when several solutions list them. Step 4 keeps the current shape: discovery returns one path per `solution.yml`, and `AnalysisResult.Run` calls `SolutionLoader.Load(path)` once for each. Each call creates its own budget and its own read cache in `SolutionParser.Parse`. So a root with N `solution.yml` files reads the shared folders N times, with N full budgets. Many small `solution.yml` files that all list one large component set multiply the bytes read.
- Suggested fix: Pick one approach and write it in Step 4. Either the loader caches the YAML root's files and budget across calls for the same root, or the plan relaxes the promise to "one budget per `solution.yml`", caps the number of solutions per root, and drops "read once". Add a test for whichever limit is chosen.
- Resolution: fixed: the loader keeps the root file cache and budget across calls (Assumptions, Step 4)

#### R3. Multi-solution roots don't say how source folders and the publisher are picked for each solution
- Area: 1. Solution boundaries
- Finding: The Data section scopes each solution's components to its `solutioncomponents.yml`, but defines `SourceFolders` as all component folders present under the YAML root. In a two-solution root, a root component of solution A whose folder exists only for solution B (and isn't in A's `solutioncomponents.yml`) is left out when A is packed, but PO013 stays quiet for it. The plan also reads the publisher from `publishers/<name>/publisher.yml` without saying how a `solution.yml` names its publisher. In a root with two publishers, the wrong pick gives false PO009 and PO010 results for a whole solution. Step 4's two-solution test needs both answers.
- Suggested fix: State that `SourceFolders` holds only the folders that the solution's `solutioncomponents.yml` lists, and how `solution.yml` links to its `publisher.yml` (by the publisher unique name in the manifest, as in the public export). Extend the Step 4 test to two solutions with different publishers and one root component that only the other solution lists.
- Resolution: fixed: SourceFolders scoped to the solution, publisher matched by manifest unique name, two-publisher test (Data, Step 4)

#### R4. The fallback's Done-when can pass with a reader that matches no real export
- Area: 9. Testability
- Finding: If Step 1 falls back to hand-written YAML, the same person writes both the fixture and the reader from the same guesses. Flow, connection reference and environment variable shapes come only from two unlicensed repos that were read but not copied. The narrowed Done-when ("parsing tests pass on files copied in shape from real exports") then checks the reader against itself. FOUNDATION section 8 records that `pac solution pack` detects `solutions/*/solution.yml`, so there is an offline check that the plan doesn't use.
- Suggested fix: In the fallback, pack each hand-written version with `pac solution pack` by hand (offline, no tenant), analyze the zip with the existing zip reader, and require the same findings as the YAML reader, apart from PO013 and PO014. Record the pac version in NOTICE.md. The same check is a cheap extra on the Git integration fixture.
- Resolution: fixed: the fallback packs offline with pac and compares against the zip reader (Goal, Step 1)

### Low

#### R5. Step 1 doesn't say how v1 is taken out of the repo, or what the canvas edit may change
- Area: 9. Testability
- Finding: Step 1.5 says to clone the repo and copy the working-tree files into `v1` and `v2`, but after the v2 commit the working tree holds only v2. Step 1.4 adds a canvas formula with "a fake `contoso.sharepoint.com` site URL". PO006's pattern, which PO014 reuses, only matches SharePoint URLs with a `/sites/` or `/teams/` path. If the maker also adds a SharePoint data source or connection, `canvasapp.yml` and the connection references change, and v2 no longer gives exactly PO005 and PO006.
- Suggested fix: In Step 1.5, check out each commit in turn before copying. In Step 1.4, give the URL shape (a `/sites/<name>` path on `contoso.sharepoint.com`, for example in a `Launch` formula) and add no data source or connection.
- Resolution: fixed: each commit checked out before copying; the URL shape is given and no data source is added (Step 1)

#### R6. PO014's message and component are not defined, but Done-when compares them
- Area: 9. Testability
- Finding: Done-when and Step 8 compare findings on rule, component and message, but the plan gives PO014's severity only. It doesn't say what the message names (app, screen, control, property) or whether the component is the URL, as in PO006. It also doesn't say whether values that don't start with `=` are scanned.
- Suggested fix: In Step 7, state the message (for example "Canvas app '<app>' hard-codes the URL '<url>' in <control>.<property>.") and that the component is the URL, as in PO006, and say whether non-formula values are scanned.
- Resolution: fixed: PO014 message, component, file and scope defined (Step 7)

#### R7. A path below the YAML root makes the reader read outside the given path
- Area: 7. Data volume (input limits)
- Finding: `powerops analyze solutions/<name>` would find that folder's `solution.yml`. The loader then reads the shared component folders two levels up, outside the path the user named. AGENTS.md says to never read paths outside the solution, and the plan doesn't say whether this case is an error, a walk up, or something else.
- Suggested fix: State the behavior in Step 4: either the YAML root must be at or below the given path (and anything else is an `InvalidDataException` starting with the path), or walking up is allowed and documented. Add a test for it.
- Resolution: fixed: only YAML roots at or below the given path are found, with a test (Step 4)

#### R8. Two parts of PO013 are vague: the classic workflow folder and the Error severity
- Area: 9. Testability
- Finding: Step 5 maps type 29 to "`modernflows/<id>` or `workflows/`". The second form has no name or id segment, and the Data table doesn't show a `workflows/` layout, so the check for it can't be written as stated. AGENTS.md also defines Error as "the import or a run breaks". The pac reference cited in FOUNDATION says the component is silently left out, which doesn't by itself show that anything breaks.
- Suggested fix: Give the `workflows/` folder shape with its key segment, or drop it until an export shows it. In `PO013.md`, say what breaks when the component is left out, and cite the pac solution reference for the silent drop. Otherwise make it a Warning.
- Resolution: fixed: workflows/ dropped until an export shows it; Error severity justified and sourced in PO013.md (Step 5)

#### R9. The YAML depth cap has no test
- Area: 7. Data volume (input limits)
- Finding: Assumptions cap YAML nesting depth "like the XML reader's", but Step 3 lists tests only for `Managed: 1` and for anchors. Nothing checks the depth cap or its error message.
- Suggested fix: Add a deeply nested YAML file to Step 3's tests, rejected with an `InvalidDataException` that starts with the path.
- Resolution: fixed: depth-cap test added (Step 3)

#### R10. Overlap with other solutions was not checked in this review
- Area: 1. Solution boundaries
- Finding: The plan says `plan facts` lists two other solutions and that neither is bound to Git. This review did not run `plan facts`, because it must not touch an environment, so that claim was not checked here. M5 creates no solution, so the risk is low.
- Suggested fix: At sign-off, confirm that only `PowerOpsSeed` is bound when Step 1 starts.
- Resolution: accepted: no Git binding exists yet; Step 1 creates a solution binding for PowerOpsSeed only

#### R11. Users and Automation were approved without being asked
- Area: 10. Unanswered areas and open questions
- Finding: The `## Log` records "approved without: users, automation" at depth `quick`, so the planner wrote both sections from the repo. `Open questions` and `Questions for your admin` are empty, and `plan status` counts 0 open questions. The first review raised the same point, and it was accepted.
- Suggested fix: None needed, unless R1 changes what CI users get. In that case, recheck the Users and Automation sections at sign-off.
- Resolution: accepted: approved without on purpose (logged); R1 is fixed without changing what CI users get
