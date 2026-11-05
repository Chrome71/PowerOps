---
status: signed-off
review: done
signed-off: 2026-11-05
signed-off-hash: c2da9d902d04d9032b5ca5968d0ca39b79822c43ef687476b8ceedceb34752bb
environment: null
depth: quick
supersedes: null
---

# M60: Security-role rules PO032 to PO034

## Goal

Read a solution's security roles and its model-driven apps' role maps, and warn in `analyze` when an app is shared only with admin roles, when a custom role can't open an app, and when a role can create or write a table the solution doesn't use. This is M60 of the improvement plan written after the live test of 2026-11-04 (P1). The plan, its reviews and the person's answers to its decisions D1 to D9 are kept outside the repo, because they quote a private test run. In that run the model-driven app was shared only with System Administrator and System Customizer, its two custom roles held none of the privileges a user needs to open an app, and both carried the role editor's SharePoint Create and Write privileges, which no component used (the security review's findings S1, S2 and S7; the plan's B-1 and B-3). `analyze` said nothing about any of it, because it reads no role. B-2, the flow's connection owner, is a live fact and not claimed here.

Done when:

1. `Solution` has a `Roles` field, read from `Roles/*.xml` in an unpacked folder and from the `Roles/Role` elements of a zip's `customizations.xml`, and `AppModule` has a `Roles` field, read from its `AppModuleRoleMaps`. A role file whose root element isn't `Role`, and a role with no `id` or no `name`, is an `InvalidDataException` whose message starts with the path. A role read twice (the same id in a `_managed` file or in `customizations.xml`) is kept once. PO001 to PO031 give the same findings on every solution as before.
2. **PO032, Warning:** an app module whose role map names only the System Administrator and System Customizer role templates, matched by the role template ids Learn documents, never by name (D6). A role map id that is neither a documented role template nor the id of a role in the solution gives a `note:` line instead of a finding when no other id makes the app quiet.
3. **PO033, Warning:** a custom role in the solution with table privileges but none of `prvReadAppModule`, `prvReadUser` and `prvReadUserQuery`. Learn names Read on the Model-driven App table, Read User and Read Saved View in words among what a user needs to open and use a model-driven app; the `prv` names are those of Learn's App for Outlook troubleshooting page. Holding any one of the three silences the rule, so it is far weaker than Learn's minimum: it finds only roles with none of it. A role with no `RolePrivileges` element gives a note.
4. **PO034, Warning:** a role in the solution with Create or Write on a table that no component in the solution uses. When the solution holds a component whose table use PowerOps doesn't read, a note replaces the findings.
5. Every message says "may". Each rule has a firing, a quiet and an unknown case in `WarningRuleTests`, a page with a dated Learn Sources line and a row in `docs/rules/README.md`.
6. A scrubbed fixture from the run's unpacked solution, `tests/fixtures/equipment-checkout/`, fires all three rules, and `tests/fixtures/NOTICE.md` describes it.
7. The goldens change only as the Data section says, and the change on each existing fixture is listed in the Log, not assumed empty.
8. The eval baselines: if `analyze` on any eval case now gives new output, the build stops before `tests/eval` is edited, and the report names each case, each changed line and why each finding is correct (the brief's step 6). `RoleEvalTests.Every_solution_case_has_a_baseline_file_that_matches_the_live_output` is then an expected failure, and so is `RoleEvalTests.Analyze_test_and_settings_validate_report_nothing_on_the_clean_solution_cases` when a case with no keyed flaw (`solution-02` or `solution-06`) gets a finding; `dotnet test` is not clean, and no PR is opened. The person decides on a rerun.
9. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, and one `feat:` PR is open and green, or the build stopped under Done when 8.

### Won't build

- A rule for an app with an empty or missing role map: Learn says only makers open such an app, but the M60 section asks for "only admin roles".
- Privilege depth (`level`): no rule reads it, so the reader doesn't keep it. S7, Front desk's organization-wide Write on a table its app uses, is a design choice the plan recorded, not something PO034 flags.
- Roles in the YAML layout (`solutions/<name>/`), and roles and role maps in `diff` and `drift`: the M60 section names `Roles/*.xml` and `customizations.xml` only.
- Reading flow definitions, plugin code, classic workflows or agents for the tables they use. PO034 gives a note when the solution holds them (Data row 7).
- Column security profiles, field-level privileges, team roles, business units and the misc privileges (such as `prvExportToExcel`).
- Telling users which roles they hold: a user may hold Basic User or App Opener beside a custom role, which is why every message says "may".
- App Opener's privileges: Learn doesn't list them by name and no export of App Opener or of a role copied from it was read. A role copied from App Opener, as PO033's fix suggests, may still give PO034 findings on system tables it can create or write; the PO034 page says so.
- A role with Create and Write on the Model-driven App table, which Learn says makes its user a maker who can open every app, but without `prvReadAppModule`: PO033 still fires on it. It is a listed limit on the PO033 page; no observed role has that shape.
- Any eval file. A changed baseline is the person's decision (Done when 8).

## Users

- A maker or agent who runs `analyze` on an exported or unpacked solution with security roles or a model-driven app, before it goes to Test or Production.
- CI: the Action runs `analyze` with `--fail-on error` by default, so the three Warnings change no exit code unless the job sets `fail-on: warning`. A `note:` line changes no exit code.
- An agent through the MCP `analyze` tool, which passes the findings and the notes on.
- Every command that loads a solution (`analyze`, `baseline`, `diff`, `test`, `settings`, `deploy` and their MCP tools) goes through the same parser, so it now opens the role files, and a role file whose XML is malformed or too deep, whose root element isn't `Role`, or whose role has no `id` or `name` stops it with exit 3, as any malformed file does today. Before M60 these files were never opened. M46 made the same change for app modules.

## Data

Line numbers are those of `0fb8f3d`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | `Role(string Id, string Name, IReadOnlyList<string>? Privileges, string File)`: `Id` normalized as other ids are (`SolutionParser.NormalizeId`), `Privileges` the trimmed, non-empty `name` of each `RolePrivileges/RolePrivilege` in file order, null when the role has no `RolePrivileges` element. `Solution` gains an optional `Roles` parameter, empty when absent, as `AppModules` is; `AppModule` gains an init property `Roles`, the normalized ids of `AppModuleRoleMaps/Role/@id`, empty ones skipped, so its constructor and its callers don't change | `src/PowerOps.Core/Model/Solution.cs:6-50,81` | row 3 |
| 2 | `SolutionParser.Parse` reads, with `LoadRoot` (so the read budget and the XML limits apply), each `Roles/<file>.xml` (path matched ignoring case; one level, as `pac solution unpack` writes it) with root `Role`, and the `Roles/Role` elements of `customizations.xml`, as app modules are read from both. A role with no `id` or no `name` attribute (trimmed, empty counts as missing) is refused with the path (decisions Q4). The roles are kept once per id, the first in that order (folder files in ordinal path order, so `X.xml` before `X_managed.xml`, then `customizations.xml`); no observed layout holds a role twice, but app modules come as `_managed` files too (decisions Q9). `ReadAppModule` adds the role map | `src/PowerOps.Core/Readers/SolutionParser.cs:110-149,418-424` | row 3 |
| 3 | Reader tests with `TempFolder`: a role with two privileges, a blank privilege name skipped; a role with an empty `RolePrivileges` (empty list) and one without it (null); an upper-case braced id normalized; a role file in another case of `roles/` read; a nested `Roles/a/b.xml` not read. An app module whose role map has two ids, one upper-case and braced, and an empty `id` skipped; no role map gives an empty list. The same records from a zip whose `customizations.xml` holds the `Role` and `AppModule` elements, with `File` `customizations.xml`. A wrong root element and a role with no `id` or `name`, in a folder and in a zip, are refused with the path. A role in `Roles/A.xml` and again in `Roles/A_managed.xml` and in `customizations.xml` is read once, from `Roles/A.xml`. A solution with none has an empty `Roles` | new `tests/PowerOps.Core.Tests/Readers/RoleReaderTests.cs` | itself |
| 4 | `SecurityRoles`, internal to the rules: the 17 standard role template ids of Learn's "Security roles and templates" page, normalized; the two admin ones (`SystemAdmin`, `Customizer`); the three app-opening privileges `prvReadAppModule`, `prvReadUser`, `prvReadUserQuery` (decisions Q2); `Table(privilege, verbs)`, which returns the table name of a `prv<verb><table>` privilege for the given verbs (ordinal, `AppendTo` before `Append`) or null | new `src/PowerOps.Core/Rules/SecurityRoles.cs` | row 8 |
| 5 | PO032, `IRule` and `INotChecked`. For each app module with a non-empty role map, its distinct ids are sorted into admin template, other documented template, role in the solution (by `Role.Id`), and unknown. Any id of the second or third kind: quiet. Else, only admin ids: one finding. Else (unknown ids, with or without admin ones): one note with the count of unknown ids (decisions Q1, Q3). The 15 other documented templates count as non-admin roles whatever they grant, so an app also shared with `Support` or `Proxy`, service roles no user opens an app through, is quiet: a missed finding, never a false one; the PO032 page says so (review R7) | new `src/PowerOps.Core/Rules/PO032AppSharedWithAdminsOnly.cs`; `src/PowerOps.Core/Rules/Analyzer.cs:39` | row 8 |
| 6 | PO033, `IRule` and `INotChecked`. For each role in the solution whose id is not a documented template (a system role carried in a solution is not custom): null `Privileges` gives a note that names PO033 and PO034; a role with at least one table privilege (any of the eight verbs, row 4) and none of the three app-opening privileges gives one finding | new `src/PowerOps.Core/Rules/PO033RoleWithoutAppBaseline.cs`; `Analyzer.cs:39` | row 8 |
| 7 | PO034, `IRule` and `INotChecked`. For each role as in row 6 with non-null `Privileges`: the tables of its `Create` and `Write` privileges, distinct ignoring case, minus `UserEntityUISettings` ("Create or edit a security role" lists Create, Read and Write on User Entity UI Settings among the minimum privileges; the privilege names are inferred from the `prv<verb><table>` pattern), minus the tables the solution uses. A table is used when, ignoring case, it is the `schemaName` of a type-1 root component or of a type-1 `Required` node in `MissingDependencies`, one of a canvas or code app's `Tables` (its `CdsDependencies`), or a substring of a workflow's `ClientData` (a cloud flow's definition names a table by its entity set name or logical name; decisions Q5). The tables left are one finding each, naming the verbs. When some are left and the solution holds a type-91 root component (a plugin assembly), a canvas app whose `Tables` is null, a workflow whose `ClientData` is null, or an agent, the role gets one note with the count and the first of those reasons, in that order, and no finding (decisions Q6) | new `src/PowerOps.Core/Rules/PO034RoleWritesUnusedTable.cs`; `Analyzer.cs:39` | row 8 |
| 8 | Tests in `WarningRuleTests` with a new `B.SecurityRole` builder and `B.App(..., roles)`. PO032 fires for {SystemAdmin, Customizer} (upper case and braces as Learn writes them, and lower case) and for SystemAdmin alone; quiet when a role in the solution or Basic User is also mapped, and for an empty role map; notes for {SystemAdmin, unknown} and for {unknown}, with no finding. PO033 fires for a role with `prvReadpops_loan` only; quiet with `prvReadAppModule` added, with `prvReadUser` alone, with no table privileges (an empty list, or only `prvExportToExcel`), and for a role whose id is the Basic User template; notes for null `Privileges`. PO034 fires once for `prvCreateSharePointData` and `prvWriteSharePointData` (message names "Create and Write"), once each for two tables; quiet for a table that is a root component (in another case), a `MissingDependencies` `Required` node, a canvas app's table, a flow's `ClientData` mention, for `prvWriteUserEntityUISettings`, for Read and Delete privileges, and for a template role; notes instead of findings for a plugin assembly, a canvas app with null `Tables`, a workflow with null `ClientData` and an agent, and none of these notes when no table is left | `tests/PowerOps.Core.Tests/Rules/WarningRuleTests.cs`, `tests/PowerOps.Core.Tests/Rules/Builders.cs` | itself |
| 9 | Fixture `tests/fixtures/equipment-checkout/` (decisions Q7): the run's `Roles/IT staff.xml` and `Roles/Front desk.xml` with fake role ids, its `AppModule.xml` without the site map component and the web resource id, and `Other/Solution.xml` and `Other/Customizations.xml` trimmed to the table, the two roles and the app. `FixtureRuleTests`: it gives PO032 once (the app), PO033 twice (both roles), PO034 twice (SharePointData, Create and Write, for each role), nothing else, and no note. `tests/fixtures/NOTICE.md` gets a "made for this project" entry with the scrub and trim list | new folder; `tests/fixtures/NOTICE.md`; `tests/PowerOps.Core.Tests/Rules/FixtureRuleTests.cs` | itself |
| 10 | `docs/rules/PO032.md` to `PO034.md` in the recipe's sections, Sources lines verified on the day they are written; three rows in the rule table. Each page names its input, its exact note text, and which parts are Learn's and which are observed exports | new pages; `docs/rules/README.md:37` | `RuleDocsTests`, `DocsDriftTests` |
| 11 | Goldens by "Update goldens": `rules.txt`, `rules.json` and the `analyze-fixtures.*` rule descriptors gain PO032 to PO034, and `analyze-fixtures.*` gains the new fixture as a 17th solution, so its summary line changes. The fixtures change as the Assumptions predict (`alm-sample`, the two `ColumnLevelSecuritySample` zips and the new fixture); every other fixture is unchanged. The diff is read and listed in the Log | `tests/golden/` | `ReportTests`, `RulesCommandTests`, `OutputSchemaTests` |
| 13 | The tests that count or list the fixtures gain the new folder (review R1): `SolutionDiscoveryTests.Finds_fixture_folders_and_zips_sorted` (its list), the `AnalyzeCommandTests` JSON test that expects 16 solutions and the Markdown test's `**16 solutions, ` prefix and 16-element check, and `PackedToolTests.Packed_tool_analyzes_fixtures`, each from 16 to 17. Two more tests read a fixture the rules now report on: `AnalyzeCommandTests.Clean_zip_exits_zero` moves from the `ColumnLevelSecuritySample` zip, which now gets PO033 and PO034, to `polymorphiclookupexamplesolution.zip`, which stays clean, and `McpCommandTests.Baseline_returns_the_file_the_cli_writes` expects `alm-sample`'s PO034 note after its existing note | `tests/PowerOps.Core.Tests/Readers/SolutionDiscoveryTests.cs`, `tests/PowerOps.Cli.Tests/AnalyzeCommandTests.cs`, `tests/PowerOps.Cli.Tests/PackedToolTests.cs`, `tests/PowerOps.Cli.Tests/McpCommandTests.cs` | themselves |
| 12 | `docs/COMMANDS.md`: the `analyze` paragraph says it reads security roles and app role maps of an unpacked folder or a zip, not of the YAML layout (PO032 to PO034) | `docs/COMMANDS.md:38` | `DocsDriftTests` if it covers it |

The rules (rows 5 to 7). Messages are one sentence; `<…>` are values.

| Rule | Title | Message (component, file) | Fix |
|---|---|---|---|
| PO032 | Model-driven app is shared only with admin roles | `App '<app>' is shared only with System Administrator or System Customizer, so users without an admin role may not be able to open it.` (the app, its file) | Share the app with the security roles its users hold, and remove System Administrator and System Customizer from it unless they need the app. |
| PO033 | Custom security role has no privilege to open a model-driven app | `Role '<role>' has table privileges but none of prvReadAppModule, prvReadUser and prvReadUserQuery, so a user who holds only this role may not be able to open a model-driven app.` (the role, its file) | Copy the role from App Opener, or give its users App Opener as well, and test the app as a user who holds only those roles. |
| PO034 | Security role can create or write a table that no component in the solution uses | `Role '<role>' has <Create, Write or Create and Write> on '<table>', which no component in the solution uses, so it may grant more than the solution needs.` (the role, its file) | Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't. |

The notes: `PO032 not checked for app '<app>': <n> of its role map ids are neither a documented role template nor a role in the solution.`, `PO033 and PO034 not checked for role '<role>': its file has no RolePrivileges element, so its privileges are not known.` and `PO034 not checked for role '<role>': <n> of the tables it can create or write are used by no component PowerOps reads, and the solution holds <a plugin assembly | a canvas app with no readable CdsDependencies | a workflow with no readable definition | a Copilot Studio agent>, whose table use isn't read.`

## Automation

None. This plan changes this repo.

## Screens

None.

## Environments and route

None. The plan reads the live run's exported files on this machine and targets no environment; the fixture is scrubbed before it is committed.

## Solution and publisher

None: no Power Platform solution is built. The fixture keeps the run's prefix `pops` (its privilege names carry it) and renames the publisher to `contoso`, "Contoso".

## Constraints

- Offline and read-only, as every `analyze` rule. No new dependency.
- The pinned roles (`skills/powerops-verify`, `skills/powerops-review-security`, their `plugins/*/skills/` copies) and `tests/eval` stay untouched (`docs/plans/m55-role-freeze/plan.md`, Data).
- The run's folder is read-only and unscrubbed. Its files go to no non-Claude model, and only the scrubbed copy is committed.
- No tenant URL, environment id, email or user name in the repo. The role ids in the fixture are fake; the two template ids are Learn's public constants.

## Assumptions

- The scrubbed export carries the documented template ids, confirmed before the rule design was settled: `tests/fixtures/equipment-checkout/AppModules/equipment_checkout_a1a05935/AppModule.xml` has an `AppModuleRoleMaps` with exactly two `Role` ids, in lower case and braces. Ignoring case and braces they equal Learn's `SystemAdmin` and `Customizer` constants on "Security roles and templates", which says these template ids are unique and constant in all environments, while role ids are not. Microsoft's public exports show the same two ids: microsoft/gov-apptemplates at `federal/hr-recruiting/src`, microsoft/Nonprofits at `GrantManagement/Solution`, and `alm-sample` in this repo.
- A role map names a custom role by the `id` of that role's `Role` element. Observed, not documented: microsoft/powercat-automation-kit at `c08e56f98320436767b7db0a84919f3e21937c0f` (MIT), `AutomationCoEMain/SolutionPackage/src`: the `autocoe_AutomationCenter` app's role map names three ids: the two admin templates and the `id` of `Roles/Automation Project Admin.xml`. Learn says role ids are not unique across environments, and the export is made in one, so the ids in one export agree. If a real export ever named a custom role otherwise, PO032 gives a note, not a false finding (row 5).
- In a zip the roles are `Role` elements of `customizations.xml` with the same children: observed in `tests/fixtures/zips/ColumnLevelSecuritySampleSolution.zip` (Microsoft's PowerApps-Samples, MIT). In an unpacked folder `customizations.xml` has an empty `<Roles />` and each role is a `Roles/<name>.xml` file (the run's export, `alm-sample` and the eval cases).
- Learn's privilege names: "Privileges required to view and access apps" says a user with only Read on the Model-driven App table must also hold a role the app is shared with, and that System Administrator, System Customizer and Environment Maker can open every app. "Create or edit a security role" lists, as minimum privileges, App Opener for opening a model-driven app, and Read on the table, Read Saved View, Create, Read and Write User Entity UI Settings and Read User for viewing tables. The privilege names `prvReadAppModule` (Model-driven App), `prvReadUser` (User) and `prvReadUserQuery` (Saved View) are in the table of Learn's troubleshooting page "A privilege or permission error occurs in Microsoft Dynamics 365 App for Outlook". `UserEntityUISettings` is the table name the User Settings reference uses. Learn doesn't list App Opener's privileges by name, so PO033 checks the three named ones (decisions Q2).
- Privilege names are `prv` + verb + the table's schema name (`prvReadpops_Loan` for the table `pops_Loan`, `prvReadContact`): observed in every role file read. A table's logical name is its schema name in lower case for these tables, so names are compared ignoring case. Where a system table's privilege name differs from its logical name (`prvReadUser` for `systemuser`), PO034 can give a false finding on that table if the solution carries it as a root component. No observed export does; the rule page says so.
- Expected changes on the existing fixtures, from reading them: `alm-sample` gives PO032 (its app maps only the two admin templates) and PO033 (`ALM Accelerator Sample Role` has none of the three privileges); it holds a plugin assembly (type 91), so its role's Create and Write on SharePointData give the PO034 note, not a finding. Each `ColumnLevelSecuritySample` zip gives PO033 and PO034 (SharePointData, Create and Write) for its one role, and has no app module. No other fixture holds a role or a role map. Every role-bearing export read (the run's two roles, `alm-sample` and both zips) holds Create and Write on SharePointData, the role editor's defaults that B-3 names, so PO034 is expected to fire on most roles made in the editor; it is a "may", and the fix says to check before removing. "Create or edit a security role" says a new role includes App Opener's privileges by default, which none of these roles holds; the run's roles were made that way all the same.
- The eval cases: `solution-01`, `solution-06`, `solution-07` and `solution-08` hold custom roles with table privileges on `pops_workitem` and none of the three app-opening privileges, so PO033 fires on each role (six findings), and their baselines' `analyze` sections change. `solution-06` has no keyed flaw: it is one of the two clean cases that `RoleEvalTests` expects `analyze` to report nothing on, so a PO033 finding there also changes what the eval treats as a clean case. PO032 is quiet there (their apps map their own roles), and PO034 is quiet (`pops_workitem` is a root component). The build therefore expects to stop under Done when 8 (decisions Q8).
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 3 and 8 and see them fail to compile or fail; then rows 1, 2 and 4 to 7 until they pass.
3. Row 9: the fixture, its `NOTICE.md` entry and its `FixtureRuleTests` case.
4. Rows 10 and 12; `RuleDocsTests` and `DocsDriftTests` pass.
5. Row 11 by "Update goldens", reading the diff and listing each fixture's change in the Log.
6. Run `RoleEvalTests`. If a baseline would change, stop: commit locally, change no file under `tests/eval`, open no PR, and report each case, each changed line, why each finding is correct, and that the baseline test is the one expected failure (Done when 8).
7. Otherwise `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main HEAD` from the main checkout); push and open one `feat:` PR, and wait for its 8 checks. The master session merges.
