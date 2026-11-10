---
status: approved
review: pending
environment: null
depth: quick
supersedes: null
---

# M79: PO035, navigation names in flows match the exported relationships

## Goal

In the 1.0 trial (finding F20), flows named a lookup's navigation property in the wrong case (all lower case where the table's navigation property has capitals). The Web API matches navigation property names by case, so the flows failed in the tenant. Their 38 flow tests passed anyway, because a mock echoes whatever name the flow sends. Today no reader parses the exported relationships. So `analyze` can't see the mismatch. This plan reads the navigation property names from the exported one-to-many relationships in all three layouts (unpacked folder, zip, YAML). It adds PO035 (Warning): a Dataverse action names a navigation property in `$expand` or in an `item/<name>@odata.bind` key that matches one of its table's exported navigation properties only when case is ignored. The finding names both spellings. This is milestone M79 of the agreed 1.0 fix plan.

Done when:

1. `Solution.NavigationProperties` lists, for each exported one-to-many relationship, the navigation property of each of its two roles with the table it belongs to and that table's entity set. It is read from `Other/Relationships/*.xml` and the customizations file's `EntityRelationships` (folder and zip), and from the solution's `entityrelationships/*/entityrelationship.yml` (YAML). Reader tests of all three layouts pass.
2. `FlowDefinition.DataverseActions` lists every action, at any depth, of the Microsoft Dataverse connector, with its literal `entityName`, its literal `$expand` and the names of its `item/<name>@odata.bind` keys.
3. PO035 fires on the fires cases of Data row 8 and stays quiet on its quiet cases. Its message names the flow, the action, the name used, the table and the exported name. It is registered in `Analyzer.Rules`, and `docs/rules/PO035.md` follows the recipe and says what isn't read.
4. The new fixture `tests/fixtures/flow-navigation/` gives exactly the PO035 findings of Data row 9 and no other finding. `analyze-fixtures.*` and `rules.*` goldens are updated by the recipe and their diff holds only PO035 lines.
5. `git diff origin/main -- tests/eval skills/powerops-verify skills/powerops-review-security plugins/*/skills/powerops-verify plugins/*/skills/powerops-review-security` is empty.
6. `dotnet build` (0 warnings), `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m79-po035-navigation` and the leak scan are clean.

### Won't build

- Schema-aware mocks in `powerops test`. The fix plan rejected them: the rule catches the name before any test runs.
- Many-to-many relationships. No export read so far has one, so their role layout is not observed. Learn names their navigation properties (`Entity1NavigationPropertyName`, `Entity2NavigationPropertyName`), but not how they are exported. PO035 doesn't read them, and the docs say so.
- `$expand` beyond a comma list of plain names, such as `demo_CostCentre($select=demo_name)` or a nested expand. A value with anything else is not read at all. The fix plan scoped this.
- A `note:` line (`INotChecked`) for what isn't read. A flow with an expression in `entityName` is common, and a note for each would be noise. `PO035.md` lists what isn't read, as the fix plan says.
- A name that matches no exported navigation property, even ignoring case. The relationship may be outside the export, so a finding there could be wrong.
- The legacy Common Data Service connector (`shared_commondataservice`) and `ApiConnection` actions. Both use other parameters.
- A `schemaVersion` change. A new rule id is a new value in the open `ruleId` string, not in a closed list (`docs/schemas/README.md`).

## Users

- A maker or an agent who builds flows and runs `powerops analyze --fail-on warning` before an import. The re-trial check: a wrong-case navigation name fails it with PO035 before any tenant write.
- A reviewer who reads the finding and fixes the flow.

## Data

Line numbers are those of `278b136`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | New record `NavigationProperty(string Name, string Table, string? EntitySet, string Relationship, string File)`, with a summary that says: one navigation property of one exported one-to-many relationship; `Table` the table it belongs to, as the relationship names it; `EntitySet` that table's `EntitySetName` from the solution's tables (ignoring case), null when the solution doesn't hold the table. New init property `Solution.NavigationProperties`, default `[]`, as the other optional lists are | `src/PowerOps.Core/Model/Solution.cs` | row 8 reader tests |
| 2 | `ReadEntitySets` (`SolutionParser.cs:346`) is split: a new `internal static Dictionary<string, string> EntitySets(IEnumerable<XElement> tables)` does the mapping from `Entity` elements, and `ReadEntitySets` gives it the `Entities/*/Entity.xml` roots and the customizations file's `Entities/Entity`, as today. `Parse` makes the one `Lazy` map that `ReadAgents` (`:211`) and row 3 share, so a solution with neither agents nor relationships still reads no `Entity.xml` | `src/PowerOps.Core/Readers/SolutionParser.cs:52-170, 208-211, 346-360` | existing agent tests; row 8 |
| 3 | New `internal static IEnumerable<NavigationProperty> NavigationProperties(XElement relationship, string file, Func<string, string?> entitySet)`. For an `EntityRelationship` whose `EntityRelationshipType` is `OneToMany`, each `EntityRelationshipRoles/EntityRelationshipRole` with a `NavigationPropertyName` gives one row: role type `1` belongs to `ReferencingEntityName`, type `0` to `ReferencedEntityName`. A role with another type, or with no name or table, and every relationship of another type, give none. `Relationship` is the `Name` attribute (`""` when missing). `Parse` calls it for each `EntityRelationship` under the root of each `Other/Relationships/*.xml` (ordinal path order; `Other/Relationships.xml`, which lists names only, is not matched) and then under the customizations file's `EntityRelationships`. `entitySet` looks the table up in the inverse of the row 2 map, ignoring case, the first entity set for a table winning; the map is built only when a row is found | `SolutionParser.cs` | row 8 reader tests |
| 4 | `YamlReader.Read` (`YamlReader.cs:25`) sets `NavigationProperties` from the solution's own `entityrelationships/<name>/entityrelationship.yml` files (key `EntityRelationship`), with entity sets from its `entities/<name>/entity.yml` files (key `Entity`) through row 2's `EntitySets`. The YAML converter already gives the XML shape (a list is one element per item, `'@Name'` an attribute) | `src/PowerOps.Core/Readers/YamlReader.cs:25-82, 140-152` | `YamlReaderTests` |
| 5 | New record `DataverseAction(string Name, string? EntitySet, string? Expand, IReadOnlyList<string> Binds)` and property `FlowDefinition.DataverseActions`, filled by `WalkActions` (`FlowDefinition.cs:139`) for every action whose `inputs.host.apiId` is a string ending in `/shared_commondataserviceforapps`, ignoring case. `EntitySet` is `inputs.parameters.entityName` and `Expand` is `inputs.parameters.$expand`, each only when it is a string with no `@` (an expression or an interpolation), else null. `Binds` are the `<name>` of each `parameters` key of the form `item/<name>@odata.bind`, in key order | `src/PowerOps.Core/Flows/FlowDefinition.cs:19-27, 53-137, 139-169` | `FlowDefinitionTests` |
| 6 | New rule `PO035NavigationName` (Warning), title "Flow names a navigation property in the wrong case", Fix "Write the navigation property name exactly as the table's relationship defines it, in the same case." For each cloud flow (`RuleHelpers.CloudFlows`), each Dataverse action with an `EntitySet`, the exported navigation properties whose `EntitySet` equals it ignoring case; none, and the action is skipped. The names it checks: `Expand` split on `,` and trimmed, only when every item is a plain name (`^[A-Za-z_][A-Za-z0-9_]*$`), then the `Binds`. A name with no exact (ordinal) match whose case-insensitive match exists gives one finding per action and name: `Flow '<flow>' action '<action>' names '<used>' on '<entity set>', but the navigation property of table '<table>' is '<exported>'; the Web API matches navigation property names by case.` `File` is the flow's file, `Component` the action name. Registered after PO034 in `Analyzer.Rules` | `src/PowerOps.Core/Rules/PO035NavigationName.cs` (new), `src/PowerOps.Core/Rules/Analyzer.cs:42` | `WarningRuleTests` |
| 7 | `docs/rules/PO035.md` by the recipe: what it finds, the message, what isn't read (many-to-many, `$expand` beyond a comma list of plain names, expressions, a table or relationship outside the export, the legacy connector), Learn's part and what is observed in exports (row 10), Sources, Suppress. A row in `docs/rules/README.md`. In `docs/COMMANDS.md`'s `analyze` paragraph, one sentence: one-to-many relationships are read from `Other/Relationships/`, a zip's `customizations.xml` and the YAML layout's `entityrelationships/` (PO035) | `docs/rules/PO035.md` (new), `docs/rules/README.md`, `docs/COMMANDS.md:44` | `RuleDocsTests`, `RulesCommandTests` |
| 8 | Tests, written first. `XmlUnpackReaderTests`: a `Other/Relationships/<t>.xml` with a one-to-many of two roles, a one-to-many with no roles, a many-to-many with roles and a role of type `2`, plus `Entities/*/Entity.xml` for one of the two tables: two rows, the one table's `EntitySet` set and the other's null; `Other/Relationships.xml` alone gives none. `ZipReaderTests`: the same relationship and table in a zip's `customizations.xml` give the same rows. `YamlReaderTests`: an `entityrelationship.yml` with a two-item role list and an `entity.yml` give the rows; a relationship file outside the solution's components gives none. `FlowDefinitionTests`: Dataverse actions inside a Scope, an If's `else` and a Switch case are read; an expression `entityName` or `$expand` is null; bind names are read in order; an action of another connector is not listed. `WarningRuleTests` for PO035, fires: `$expand` `demo_costcentre` where the exported name is `demo_CostCentre`; the key `item/demo_costcentre@odata.bind`; the collection-valued name of the referenced table in the wrong case; one wrong name in a comma list of two, with spaces. Quiet: the exact case; a name matching no exported name; no navigation properties for the entity set (no relationship files); an expression `entityName`; an expression `$expand`; `demo_costcentre($select=demo_name)`; an action of another connector with the same parameters. Order: findings in flow order, then action walk order | `tests/PowerOps.Core.Tests/Readers/XmlUnpackReaderTests.cs`, `ZipReaderTests.cs`, `YamlReaderTests.cs`; `tests/PowerOps.Core.Tests/FlowDefinitionTests.cs`; `tests/PowerOps.Core.Tests/Rules/WarningRuleTests.cs` | itself | <!-- plan-status: allow -->
| 9 | Fixture `tests/fixtures/flow-navigation/`: an unpacked solution `PowerOpsNavigation`, made for this project, fake names only (publisher `demopub`, prefix `demo`). Tables `demo_Expense` and `demo_CostCentre` (`Entities/*/Entity.xml` with `EntitySetName` `demo_expenses` and `demo_costcentres`), the relationship `demo_Expense_CostCentre_demo_CostCentre` in `Other/Relationships/demo_CostCentre.xml` (role 1 `demo_CostCentre` on `demo_Expense`, role 0 `demo_Expense_CostCentre_demo_CostCentre` on `demo_CostCentre`), its connection reference, and one cloud flow, on, with three Dataverse actions on `demo_expenses`: List rows with `$expand` `demo_costcentre` (fires), Add a new row with `item/demo_costcentre@odata.bind` (fires), Update a row with `item/demo_CostCentre@odata.bind` (quiet). Expected: PO035 twice, nothing else. A `NOTICE.md` entry with the scrub list. `FixtureRuleTests` asserts the two findings | `tests/fixtures/flow-navigation/`, `tests/fixtures/NOTICE.md`, `tests/PowerOps.Core.Tests/Rules/FixtureRuleTests.cs` | itself; `ReportTests` goldens | <!-- plan-status: allow -->
| 10 | Goldens by the recipe "Update goldens": `analyze-fixtures.*` (the fixture's two findings) and `rules.*` (the new rule). The diff is read: only PO035 lines and the fixture's summary counts change | `tests/golden/` | `ReportTests`, `RulesCommandTests`, `OutputSchemaTests` |

## Automation

None. The rule reads exported flow definitions offline, and this plan adds no flow.

## Screens

None.

## Environments and route

None. `analyze` stays offline and read-only, and nothing reads or writes a tenant.

## Solution and publisher

None. The fixture is made for this project with fake names (Data row 9).

## Constraints

- AGENTS.md: the recipes "Add a rule" and "Add a fixture"; the smallest change; new logic with tests; no new write; Conventional Commits; no version or changelog edit; no tenant data in Git. The trial's names are not used anywhere.
- Fix plan section 4: `COMMANDS.md` and golden regeneration land one PR at a time, through the chain's merge lock. Goldens are regenerated after the rebase, never hand-merged.
- The kept roles `powerops-verify` and `powerops-review-security`, their plugin copies and `tests/eval/` stay untouched (Done when 5). No eval case holds `$expand` or `@odata.bind`, so PO035 can't change a role's baseline; if it did, the build stops and reports.
- PO035 is checked against `docs/rules/` again before the commit: PO034 is the highest on `main` at `278b136`.
- Platforms are Windows and Linux. Nothing here depends on the platform.

## Assumptions

Sources checked on 2026-11-09 by `powerops-researcher` (Learn only; decisions, Log):

- Navigation property names are case-sensitive in the Web API: Learn's *Web API navigation properties* says so, and *Use multi-table lookup columns* says the same of the name in `@odata.bind`. Learn doesn't say that the Dataverse connector passes the name on unchanged; the trial observed that the wrong case failed through the connector (F20).
- A one-to-many relationship's single-valued navigation property is its `ReferencingEntityNavigationPropertyName`, and the collection-valued one its `ReferencedEntityNavigationPropertyName` (*Web API navigation properties*).
- The connector's "Expand Query" takes navigation property names (*Use lists of rows in flows*; *Get a row by ID from Dataverse*).
- Observed in exports, not on Learn: a flow's lookup input is the parameter key `item/<navigation property>@odata.bind` (`alm-makers` `DeployToStage`, `item/cat_Stage@odata.bind` on `cat_projects`). Learn shows only the value's `entityset(id)` form. <!-- plan-status: allow -->
- Observed in exports, not on Learn: in `EntityRelationshipRole`, `RelationshipRoleType` 1 holds the referencing table's navigation property and 0 the referenced table's. In all 8 relationships with roles in `alm-makers` the role-1 name equals `ReferencingAttributeName`; in the 3 of `polymorphiclookupexamplesolution.zip` (a multi-table lookup) it is `<attribute>_<referenced table>`, on the referencing table, as Learn's *Use multi-table lookup columns* names such properties. Learn's *Customization solutions file schema* gives the element names in its XSD only, with no meaning for the values.
- The YAML layout writes the same relationship keys. Not observed with roles: the two YAML fixtures hold only system relationships, without `EntityRelationshipRoles`. Learn's *Solution YAML source control format reference* lists the `entityrelationships/` folder. The YAML test uses the XML element names, which the converter keeps.
- Two navigation properties of one table never differ only by case, so a case-insensitive match names the property the flow meant. Not on Learn; it holds for names derived from schema names, which are unique ignoring case.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`; the Fable plan review and a fresh-context `powerops-review-plan` into `review.md`; fixes; sign-off by the building session on the person's instruction of 2026-11-09.
2. Row 8's tests, run and seen to fail.
3. Rows 1 to 6 until they pass.
4. Rows 7, 9 and 10, the goldens by the recipe, reading the diff, and the Fable work review.
5. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status docs/plans/m79-po035-navigation`, Done when 5, and the leak scan; a local commit; then the chain's merge protocol.
