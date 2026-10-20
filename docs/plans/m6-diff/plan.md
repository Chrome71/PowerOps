---
status: approved
review: done
signed-off: null
signed-off-hash: null
environment: null
depth: quick
supersedes: null
---

# M6: diff

## Goal

`powerops diff <old> <new>` compares two versions of a solution component by component, offline, and labels the changes that carry deployment risk. Each argument is an exported zip or an unpacked solution folder. Release v0.7.0.

This plan is written after the fact: the build ran autonomously from the BUILD-PLAN M6 row, and the plan records what was built and the decisions made, for review and the user's sign-off. Q1 to Q7 in `decisions.md` were decided by the implementing session during the build, not asked of the user; the user confirms them at sign-off. Q8 and Q9 are the user's answers.

Done when (BUILD-PLAN M6 row):

- Diffing the managed/unmanaged sample pair and two versions of a live export shows only meaningful changes.
- The output fits in a pull-request comment.

How the build meets it:

- `ColumnLevelSecuritySampleSolution.zip` against `ColumnLevelSecuritySampleSolution_managed.zip`: one change, the managed flag (asserted in a test).
- `powerops-seed/v1` against `v2` (the live export): the added connection reference `pops_SpareDataverse` (risk `connection-reference`) and the changed definition of the flow "Stamp work item status" (asserted in a test, with text, JSON and Markdown goldens).
- A solution against itself, and an unpacked folder against a zip built from it: no changes, exit 0.
- Markdown is cut below 60,000 characters (GitHub's comment limit is 65,536).

The BUILD-PLAN deliverable "ignores noise (timestamps, ordering, volatile IDs)" maps to the build like this:

- Timestamps: the model holds no export or modified timestamps, so none are compared. The one exception is on purpose: a canvas app's `AppVersion` is reported as "republished" (decision Q5), because an app edit is otherwise invisible. Studio version stamps in the canvas meta (for example `CreatedByClientVersion`) change on a republish too and add "definition changed" to the same change line.
- Ordering: every component kind is compared as a keyed set, flow JSON ignores key order, and canvas meta ignores XML whitespace. Tested by the folder-against-zip tests and the JSON normalization tests.
- Volatile IDs: file paths and file names (which differ between zip and folder layouts) and the place where XML namespaces are declared are not identity and never compared. Ids that identify a component (workflow ids, root component ids) are kept as keys and matched case-insensitively. Tested by the folder-against-zip tests, including a zip in real export layout with a canvas app.

## Users

- Developers and reviewers who want to see what a new solution version changes before it is imported.
- CI jobs and agents that post the Markdown as a pull-request comment or read the JSON.

## Data

Input only. Both solutions are loaded with the existing `SolutionLoader` into the `Solution` model (`src/PowerOps.Core/Model/Solution.cs`) and compared there, so zip and folder inputs compare equal.

| Component | Key | Compared |
|---|---|---|
| Solution | one | unique name, version, managed flag |
| Publisher | one | unique name, prefix |
| Root components | type plus schema name or id | added or removed |
| Missing dependencies | required plus dependent node | added or removed |
| Connection references | logical name | connector id |
| Environment variables | schema name | type, required, default value, current value (never printed) |
| Workflows | id (lowercase) | name, category, state, flow definition as normalized JSON |
| Canvas apps | `<Name>` from the meta XML | `AppVersion` (reported as republished), the rest of the meta as parsed XML |

Noise that is not a change: file paths, ordering, flow JSON key order and whitespace, XML whitespace in canvas app metadata. Array order in flow JSON is a change.

Keys match case-insensitively. A workflow without an id is keyed by its name. Known limit: when one solution holds two items with the same key, only the first is compared, and canvas apps whose meta has no `<Name>` (or does not parse) all share the key `(unnamed)`, so a change to the second one is not reported. Exports name every canvas app, so this is accepted.

Not compared: components the model does not read (tables, views, forms and their labels), classic workflow and business rule XAML, and the `.msapp` package. This is why the managed/unmanaged pair shows only the managed flag, although its `customizations.xml` also differs in table and view labels.

Each change has a kind (`added`, `removed`, `changed`), the component type, its key, an optional name and detail, and a list of risk ids:

- `removed-component`: a managed upgrade deletes components that are no longer in the new version (Learn, solution concepts). Put on every removal except a missing dependency, whose removal is a fix.
- `flow-state`: a cloud flow (category 5) was turned on or off.
- `connection-reference`: a connection reference was added or removed.
- `environment-variable`: an environment variable was added or removed.

All comparison lives in `SolutionDiff.Compare` (`src/PowerOps.Core/Diff/SolutionDiff.cs`), one block per component kind, so a new `Solution` field is one added call plus a test.

## Automation

None. `diff` is a command; CI can run it and post its Markdown, but no workflow or action is added.

## Screens

None. Command-line output:

- Text (default): a header line `<old name> <version> → <new name> <version>`, one line per change with its risks, and a summary line.
- JSON: `schemaVersion` first, `old`, `new`, `changes`, `summary`; schema `docs/schemas/diff.schema.json`. No input paths, so the output is stable across machines.
- Markdown: a heading, the summary, a legend of the risk ids used, and one table row per change. Cells are capped at 200 characters; the report is cut at a whole row below 60,000 characters with a note on how many changes were left out.
- `--out <file>` writes the report to a file, as in `analyze`.
- Exit codes: `0` no differences, `1` differences, `2` usage error, `3` unreadable input or internal error.

## Environments and route

The tool is offline and needs no environment. It ships as v0.7.0 through the normal release. The fixtures it is tested on are already committed.

## Solution and publisher

None created or changed. The fixtures are the committed `ColumnLevelSecuritySampleSolution` zips and `powerops-seed` (publisher prefix `pops`).

## Constraints

- Offline first and read-only, as everywhere in PowerOps. No new dependencies.
- Smallest complete change: only `--format` and `--out`.
- Environment variable current values are never printed, and input paths are left out of JSON. Default values are printed, cut at 100 characters (user decision Q9): they ship in the solution anyway, and PO007 flags environment-specific defaults.
- Licensing, DLP, security roles and managed or unmanaged do not apply to the offline tool.

## Assumptions

- `AppVersion` in a canvas app's meta XML changes when the app is republished, and an edit to the app is only visible in the `.msapp` and that timestamp. Observed in exports; not documented by Microsoft.
- The table and view label differences between the managed and unmanaged sample zips are export differences, not changes a user made. Comparing tables, views and forms is a later milestone (user decision Q8).

## Open questions

## Questions for your admin

## Steps

1. `feat(diff):` compare two solutions component by component (commit aa8b4ee). `SolutionDiff.Compare` over the `Solution` model as under Data, with flow definitions compared as normalized JSON and the risk labels. Core tests for each risk label, JSON normalization, the managed/unmanaged pair, seed v1 against v2, a solution against itself and a folder against its zip.
2. `feat(diff):` add the `diff` command with text, JSON and Markdown output (commit 413c5ff). `DiffReport` in `src/PowerOps.Core/Output/`, a thin `DiffCommand`, `docs/schemas/diff.schema.json` with its row in `docs/schemas/README.md` and its `OutputSchemaTests` entry, goldens `diff-seed.txt`, `.json` and `.md`, the README usage line and paragraph, CLI tests for exit codes 0 to 3, and a Markdown truncation test.
3. `fix(diff):` review fixes (commit d55cecf). Canvas `AppVersion` reported as republished; canvas meta compared as parsed XML; duplicate JSON keys fall back to text comparison instead of exit 3; no `removed-component` on a removed missing dependency; solution unique name compared; truncation never splits a surrogate pair; environment variable current values never printed; README states what is not compared. Each code fix has a test.
4. `fix(diff):` plan review fixes (commit 196f815). Canvas metadata is compared without regard to where XML namespaces are declared, so an exported zip (canvas app inside `customizations.xml`) and its unpacked folder compare equal, tested with a zip in real export layout; Markdown cells are escaped first, then capped at 200 characters, tested; the README lists the unique name.

## Follow-ups

- After M5 merges, add the YAML layout to `diff`: one `SolutionDiff.Compare` call for each new `Solution` field that is a component (`CanvasFormulas`; `SourceFolders` is not one) plus a test diffing `powerops-seed-yaml/v1` against `v2`.
- README conflict with M7: both branches add a usage line after `powerops rules` and a paragraph after the exit-codes line. Keep both when the second branch merges.
- A later milestone compares tables, views and forms (labels and columns), per Q8.
