plan-hash: 44114950689565149b241c53e8d19c5dc3df04bc4690d4f572efdf0c00de510d
model: claude-opus-5-5
fresh-context: yes

This review covers the final plan wording after the edit that gives PO006 and PO014 one shared fix guidance (Constraints, Step 7, and the last `decisions.md` Log entry). `powerops` is not on PATH, so `plan status` was not run. The header says `status: approved`, and the body hash, computed the same way as `PlanDocument.BodyHash`, matches the `plan-hash` above. `plan facts` was not run, because this review must not touch an environment. The plan header was not changed: it already says `review: done`.

Each part of the shared guidance was checked against Microsoft Learn on 2026-10-20 (page titles below; URLs are left out on purpose). The Dataverse "(Current)" environment option, Dataverse in the current environment for canvas apps, deployment settings files, Pipelines prompting for values, and data source environment variables for canvas SharePoint sources are all backed. Two points are not fully backed: how a canvas formula reads a text environment variable (R1), and the framing of settings tables (R6). The guidance matches the `decisions.md` Log entry. It differs from the current `docs/rules/PO006.md` only because Step 7 rewrites that page. Aligning PO006 is small enough for M5: one `Fix` string, one doc section, and golden updates. The bigger risks are in how the guidance turns into a one-line `Fix` (R2) and into PO014's docs (R1).

The third review's findings (R1 to R5, accepted as build notes) are not in the plan text. This file replaces that review, so they are carried forward below as R3, R4, R7 and R8 (the old R5 is folded into R9). The committed YAML fixture (`tests/fixtures/powerops-seed-yaml/`) was checked against the plan. Checklist items 2 to 6 and 8 apply only to how the fixture was made, and the plan covers them. The plan and `decisions.md` contain no URL, GUID, email or person's name. The only host is the fake placeholder SharePoint site.

## Findings

### High

No findings.

### Medium

#### R1. PO014's guidance doesn't say how a canvas formula gets a per-environment URL, and the only route Learn documents may need a premium licence
- Area: 2. Connection references and environment variables; 3. Licence impact
- Finding: PO014 reports a URL literal inside a formula, such as the v2 fixture's `Launch(...)` of the fake site. Guidance step 2 says "an environment variable", but Learn documents no Power Fx way to read a text environment variable directly. "Use data source environment variables in canvas apps" covers data source parameters (SharePoint site and list), and those never appear in the `.pa.yaml` formulas that PO014 scans, so the "canvas SharePoint data sources" clause gives advice for something the rule never reports. The route Learn does mention ("Environment variables frequently asked questions") is reading the environment variable tables through the Dataverse connector like any other record, and that page names it as the case where premium licences apply. If `PO014.md` just says "use an environment variable", it either makes an unsourced claim or sends makers to a route that can change an app's licence.
- Suggested fix: In Constraints or Step 7, say what `PO014.md`'s How to fix states for a formula URL: read the value of an environment variable from the Environment Variable Definition and Value tables through Dataverse, with the FAQ page as the source and its licence caveat. Keep the data source clause only as "if the URL is a SharePoint data source, bind it to data source environment variables", or drop it from PO014's page. If no Learn page backs a step, leave it out, as the plan's own rule says.
- Resolution: open

#### R2. Step 7 makes `Fix` follow a four-part guidance, but `Fix` must be one plain sentence and is repeated on every finding
- Area: 9. Testability
- Finding: AGENTS.md says `Fix` is "one imperative plain sentence, with no Markdown and no URL". `Fix` is printed on every finding row in the Markdown PR comment and in JSON, and in SARIF rule metadata and `rules`. Step 7 says "PO014's `Fix` ... follow the fix guidance under Constraints" and that PO006's `Fix` is aligned with it. Read literally, that puts the ordered list (current-environment option, environment variable, PO007, PO008, data sources, settings tables) into the one-liner. That breaks the AGENTS.md rule and bloats every PR comment row.
- Suggested fix: In Step 7, say that `Fix` is a one-sentence summary and the ordered guidance lives only in the docs page's How to fix. Give the sentence, for example: "Remove the URL where the platform can use the current environment, otherwise read it from an environment variable whose value is set per environment at deployment." Use the same sentence for PO006 and PO014.
- Resolution: open

#### R3. PO013 treats an empty `SourceFolders` as "not YAML", so it stays quiet when every folder is missing (carried from the third review's R1)
- Area: 9. Testability
- Finding: Step 5 still says PO013 "is quiet when `SourceFolders` is empty (XML and zip)". A YAML solution whose listed folders are all missing also has an empty `SourceFolders`. That is the worst case PO013 exists for: pack leaves out every component and exits 0. The plan text was not changed when this was accepted as a build note, and the note lived only in the review file this one replaces.
- Suggested fix: In Data ("Model changes") and Step 5, make `SourceFolders` null for XML and zip and a list (possibly empty) for YAML. Add a Step 5 test where the only root component's folder is missing and PO013 fires.
- Resolution: open

#### R4. The shared file cache and budget have no defined scope, and a static cache would break parallel tests (carried from the third review's R2)
- Area: 7. Data volume (input limits)
- Finding: Assumptions and Step 4 still say the loader "keeps the root's file cache and budget across calls for the same root". `AnalysisResult.Run` calls the static `SolutionLoader.Load(found)` once per path. A process-wide cache is never cleared, isn't thread-safe under xUnit's parallel test classes (`ReportTests`, `SarifReportTests` and `FixtureRuleTests` all load `tests/fixtures`), and can return stale text.
- Suggested fix: In Step 4, scope the cache and budget to one `AnalysisResult.Run` call: a per-run context keyed by YAML root, passed to `Load`. A plain `Load(path)` gets a fresh context. Say that nothing is static, and test that a shared file is read and counted once.
- Resolution: open

### Low

#### R5. For a URL, "a default that is not environment-specific" means an empty default, and the guidance doesn't say so or mention PO002
- Area: 2. Environment variables
- Finding: Guidance step 2 asks for a default that is "not environment-specific (PO007)" and no shipped current value (PO008). PO007 flags any URL default, so for these rules the default must be empty. A variable marked required with an empty default and no value then triggers PO002 (error), whose own fix points to deployment settings. That outcome is right, but a reader following PO006 or PO014 runs into an error rule without warning. The fixture's variable is not required, so nothing tests this.
- Suggested fix: In the shared guidance, say "leave the default empty" for URLs, and add one clause: the value then comes from deployment settings or Pipelines at import, and a required variable also needs that to avoid PO002.
- Resolution: open

#### R6. The settings-table clause doesn't match Learn's framing, and the guidance names no source pages
- Area: 10. Unanswered areas
- Finding: "Settings tables only for values that change at runtime" is PowerOps advice. Learn ("Environment variables frequently asked questions") frames the choice differently: use environment variables for non-relational key-value data that differs per environment, and use other tools for relational configuration data. Under the plan's own rule, the clause must either be cited in Learn's terms or left out of the docs. The plan also leaves `/verify-research` to find the pages for every other part, although they are known.
- Suggested fix: In Constraints, list the pages behind each part, so Step 7 and Step 9 reuse them: "Connect to other environments using the Microsoft Dataverse connector" ((Current)), "Environment variables for Power Platform overview" (canvas apps use Dataverse in the current environment without a variable; values at import and in Pipelines), "Pre-populate connection references and environment variables for automated deployments" (deployment settings), "Use data source environment variables in canvas apps", and the FAQ. Word the settings-table clause as the FAQ does, or keep it out of the rule docs.
- Resolution: open

#### R7. The "no solutions found" message still says the YAML layout is not supported (carried from the third review's R3)
- Area: 9. Testability
- Finding: `AnalyzeCommand` still prints "(the YAML layout is not supported yet)". After M5 that is wrong, including in the case Step 4 creates on purpose: a path below a YAML root. Screens says the text output doesn't change.
- Suggested fix: In Step 4, change the message (for example: analyze a YAML layout from the folder that holds `solutions/`), and assert it in the "path below the YAML root" test. Change Screens to match.
- Resolution: open

#### R8. A missing `publisher.yml` or `solutioncomponents.yml` has no defined behavior (carried from the third review's R4)
- Area: 1. Solution boundaries
- Finding: If either file is missing, filling in blanks gives an empty prefix (PO009 fires on everything, PO010 names a blank publisher) or an empty component set (every rule goes quiet). The plan text still doesn't say what happens.
- Suggested fix: In Step 3, make both cases an `InvalidDataException` that starts with the `solution.yml` path, with a test for each.
- Resolution: open

#### R9. The Data section doesn't record what the committed fixture now settles
- Area: 9. Testability
- Finding: The real export answers three things the plan leaves open. (1) `JsonFileName` is `/modernflows/<id>/<file>.json`, relative to the YAML root (the third review's R5). `ReadWorkflow` already trims the slash. (2) `solutioncomponents.yml` lists the paths of folders and of single files at any depth (for example `/entities/<name>/attributes/<file>.yml`, form and view subfolders), plus `/publishers/<name>`. So "the component folders it lists" needs a rule for nested and file entries. (3) `canvasapp.yml` references files that aren't there (`DocumentUri` names a `.msapp`, and the background image and identity files were trimmed, as NOTICE.md says). So the reader must not resolve those references, and must find `.pa.yaml` files only through the `<name>_DocumentUri/Src/` folder.
- Suggested fix: Add these three points to the Data table or to Steps 3, 4 and 6. A file belongs to a solution when its path is at or below a listed entry. `SourceFolders` holds the top-level component folder of each entry (for example `entities/pops_workitem`). Canvas file references other than the `Src` folder are ignored.
- Resolution: open

#### R10. Step 1 is done, but the plan still reads as if it were to do, and the cleanup isn't recorded
- Area: 5. Route
- Finding: Commit "test: add the YAML layout fixture for PowerOpsSeed (M5 step 1)" added both versions and a NOTICE.md entry. Done-when's fallback paragraph, Step 1's fallback and the 2026-12-01 and 2026-12-15 dates no longer apply. Neither NOTICE.md nor the Log records step 1.6: disconnecting the solution and deleting the Azure DevOps repo that holds the unscrubbed commits.
- Suggested fix: Add a Log line saying Step 1 is done, with the commit. Confirm there that the repo was deleted, or keep that one step open. Mark the fallback as unused, or remove it when the plan is next edited.
- Resolution: open

#### R11. "First control and property in file order" is not defined across several `.pa.yaml` files
- Area: 9. Testability
- Finding: PO014's message names the first control and property where a URL appears. An app has several `Src/*.pa.yaml` files (`App`, each screen, `_EditorState`). If the same URL appears in two screens, the message depends on the order the files are listed in. That order can differ by operating system, and the message is part of the baseline match and the goldens.
- Suggested fix: In Step 6 or 7, sort the files by ordinal forward-slash path, as `SolutionDiscovery` does, then go in document order within each file. Add a test with the same URL in two files.
- Resolution: open

#### R12. Step 9 updates the BUILD-PLAN M5 row but not its rule table
- Area: 10. Unanswered areas
- Finding: BUILD-PLAN's "First rule set" table lists PO013 for M5 but not PO014. Step 9 names only "the BUILD-PLAN M5 row" and the README rule table.
- Suggested fix: In Step 9, also add a PO014 row (warning, M5) to the BUILD-PLAN rule table.
- Resolution: open
