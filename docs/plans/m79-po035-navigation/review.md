plan-hash: 011c869a454cdb419714312f2cdc99974086820a8cbaa8cead89223ad33b72aa
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the plan's `environment:` is null and no `pac` sign-in was confirmed, so `powerops plan facts` was not run and no tenant call was made. The review is from the repo at `e652005` (only this plan folder differs from `278b136`).

The plan changes the PowerOps CLI itself (a reader, a model field and analyzer rule PO035), not a Power Platform solution. Checklist items 1 (solution boundaries and publisher), 2 (connection references and environment variables), 3 (premium licences), 4 (DLP), 5 (Dev to Test to Prod), 6 (managed or unmanaged), 7 (delegation) and 8 (security roles) don't apply: the plan answers each with "None" or "Not applicable", and the fixture is a made-up offline solution with fake names and the `demo` prefix. Item 11's per-product checks (canvas, model-driven, Power Pages, code apps, Power BI, mockups) don't apply either: there are no screens, tables, lists or flows to build. Items 9, 10 and 11 (as code size) and the URL scan were checked.

Checked against the code and fixtures and found to hold: `ReadEntitySets` at `SolutionParser.cs:346-360` and the agents' `Lazy` map at `:211`; `Parse` at `:52-165`; `PO034` at `Analyzer.cs:42` and as the highest id in `docs/rules/`; `YamlReader.Read` at `:25-81`; the role layout (role 1 on `ReferencingEntityName`, role 0 on `ReferencedEntityName`, names in schema-name case) in `alm-makers/Other/Relationships/` and `polymorphiclookupexamplesolution.zip`; `Other/Relationships.xml` holding names only; the unpacked `Other/Customizations.xml` having an empty `EntityRelationships`; `entities/<name>/entity.yml` with `Entity/EntityInfo/entity/@Name` and `EntitySetName` in the YAML fixture; every Dataverse action in the fixtures having `inputs.host.apiId` ending in `shared_commondataserviceforapps`; the fixture bind keys (`cat_Stage`, `cat_ProjectID` and the others) matching their exported names exactly, and `alm-makers` having no `Entities/` folder, so no existing fixture gives a PO035 finding; no `$expand` or `@odata.bind` in `tests/eval`; Learn's *Web API navigation properties* calling the navigation property name case-sensitive and mapping the single- and collection-valued names to `ReferencingEntityNavigationPropertyName` and `ReferencedEntityNavigationPropertyName`.

Open questions: 0. Questions for your admin: 0.

## Findings

### High

No findings.

### Medium

#### R1. A new fixture folder breaks `SolutionDiscoveryTests`, which the plan doesn't list
- Area: 9. Testability
- Finding: `tests/PowerOps.Core.Tests/Readers/SolutionDiscoveryTests.cs` (`Finds_fixture_folders_and_zips_sorted`) asserts the exact list of solutions under `tests/fixtures/`. Adding `tests/fixtures/flow-navigation/` (Data row 9) makes it fail. Row 9 names `NOTICE.md` and `FixtureRuleTests.cs` but not this test, and Done when 4 and 6 don't mention it.
- Suggested fix: Add `SolutionDiscoveryTests.cs` to row 9's Where, with `"flow-navigation"` inserted in the expected list (between `equipment-checkout` and `flowtests/capture/solution`).
- Resolution: Answered. Row 9 adds `SolutionDiscoveryTests.cs:17-36` with `flow-navigation` between `equipment-checkout` and `flowtests/capture/solution`.

#### R2. The rule id PO035 is checked before the commit, not after the rebase, while four other milestones run in parallel
- Area: 5. Route (of this change) and 10. Unanswered areas
- Finding: The Constraints say PO035 is checked against `docs/rules/` "before the commit", and the Log says this session is one of five running in parallel (M76, M79 to M82). If another milestone of the chain also adds a rule and merges first, it takes PO035, and this branch finds out only at the rebase. The plan says goldens are regenerated after the rebase but not what happens to the rule id, which reaches the class name, `Analyzer.Rules`, `docs/rules/PO035.md`, `docs/rules/README.md`, the `COMMANDS.md` sentence, the tests, the fixture test and the goldens.
- Suggested fix: In Constraints, check the next free id again after the rebase onto `main`, and if PO035 is taken, renumber to the next free id in every file of rows 6, 7, 8, 9 and 10 before the goldens are regenerated.
- Resolution: Answered. Constraints check PO035 against `docs/rules/` and `Analyzer.Rules` after the final rebase under the merge lock and list every file to renumber; Done when 6 makes it a done condition.

### Low

#### R3. Done when 4 says the golden diff holds only PO035 lines, but a new fixture adds a solution section too
- Area: 9. Testability
- Finding: Done when 4 says the `analyze-fixtures.*` diff "holds only PO035 lines". Row 10 allows "only PO035 lines and the fixture's summary counts". A new solution in `tests/fixtures/` also adds its own header or section (name, version, per-solution counts) in the text, Markdown, JSON and SARIF goldens, so Done when 4 can't be met as written.
- Suggested fix: Word Done when 4 like row 10: the diff holds the new `PowerOpsNavigation` solution entry with its two PO035 findings, the summary counts and the new rule in `rules.*`, and nothing else.
- Resolution: Answered. Done when 4 lists the whole golden diff (the new solution entry with two PO035 findings, the summary counts, the PO035 rule entry and its SARIF descriptor); row 10 points to it.

#### R4. `NavigationProperty.Relationship` and `File` have no reader in the plan
- Area: 11. Size
- Finding: Row 1's record carries `Relationship` and `File`, but PO035 (row 6) uses only `Name`, `Table` and `EntitySet`: its message names the flow, action, used name, table and exported name, and its finding's `File` is the flow's file. No Goal line or test needs the two fields.
- Suggested fix: Drop `Relationship` and `File` from the record, or name the line of Goal or the test that reads them.
- Resolution: Answered. Row 1's record is `NavigationProperty(string Name, string Table, string? EntitySet)`, and row 3 drops the `file` parameter.

#### R5. The list of what PO035 doesn't read misses relationships without roles and the Relate rows action
- Area: 10. Unanswered areas
- Finding: Row 7's "what isn't read" list omits two cases the plan's own survey shows. First, relationships exported without `EntityRelationshipRoles` give no row (row 3), and those are 60 of the 68 one-to-many relationships in the fixtures (the system relationships such as `owner_`, `business_unit_` and `lk_..._createdby`), so a wrong-case `ownerid`, `owningbusinessunit` or `createdby` is never caught. Second, the Dataverse connector's Relate rows and Unrelate rows actions take a relationship name in another parameter, which row 5 doesn't read.
- Suggested fix: Add both to row 7's list for `PO035.md`, and to Won't build if they are out of scope.
- Resolution: Answered. Row 7's not-read list adds relationships exported without roles (the system relationships) and the relationship parameter of Relate rows and Unrelate rows, among others.

#### R6. The YAML reader's handling of an `entity.yml` without an `Entity` key is not stated
- Area: 9. Testability
- Finding: Row 4 reads entity sets from `entities/<name>/entity.yml` (key `Entity`) but doesn't say whether a file without that key, or without `EntityInfo/entity`, is skipped or refused. The XML side (`ReadEntitySets`) skips such a table silently, while `YamlReader.Required` refuses with the path. The two layouts could then disagree on the same solution.
- Suggested fix: State that a missing `Entity` or `EntityInfo/entity` is skipped, as in `ReadEntitySets`, and add that case to the `YamlReaderTests` line of row 8.
- Resolution: Answered. Row 4: a YAML file without its key is skipped, as the XML reader skips an `Entity.xml` without `EntityInfo`; malformed YAML is refused with its path.

#### R7. Cross-reference and line-number slips
- Area: 9. Testability
- Finding: Row 7 says PO035.md gives "what is observed in exports (row 10)", but row 10 is the goldens; the observed facts are in Assumptions. Row 5 gives `WalkActions` as `FlowDefinition.cs:139`; at `278b136` it is declared at line 137 (139 is its first statement). Row 2's range `SolutionParser.cs:52-170` runs past `Parse`, which ends at 165.
- Suggested fix: Point row 7 at Assumptions, and use `FlowDefinition.cs:137` and `SolutionParser.cs:52-165`.
- Resolution: Answered. Row 7 points at Assumptions; row 5 gives `FlowDefinition.cs:137`; row 2 gives `SolutionParser.cs:52-165`.

#### R8. `decisions.md` contains a URL
- Area: URL, GUID, email or name check
- Finding: The Log's research line in `decisions.md` contains one URL (a Learn page path, without a scheme). It is a public documentation page, but the review checklist reports every URL in the plan files.
- Suggested fix: Replace it with the page title alone, as the other sources on that line are given, or confirm it is meant to stay.
- Resolution: Answered. The decisions Log keeps it on purpose: a public Learn page, no tenant, environment or personal value.

## Second pass

Checked on 2026-11-09 against `plan.md` at `4d9a71f` (body hash above), the code at `278b136` and the fixtures. Environment facts were still not read and no tenant call was made. Open questions: 0. Questions for your admin: 0.

R1 to R8 are all answered (see each Resolution). The revision adds no finding. The new claims were checked and hold:

- `alm-makers` gives 16 rows (8 relationships with roles, 2 each), among them `cat_Stage` on `cat_Project`, with no `Entities/` folder, so every entity set is null.
- `polymorphiclookupexamplesolution.zip` gives 6 rows; `sample_MediaPolymorphicLookup_sample_audio` is role 1 on `sample_Media` (`sample_medias`) and `sample_media_sample_audio` role 0 on `sample_Audio` (`sample_audios`).
- The four `alm-makers` bind keys are `cat_BuildRequest` and `cat_ProjectID` on `cat_telemetries` and `cat_Environment` and `cat_Stage` on `cat_projects`; each equals, in case, a role-1 name of that table, so PO035 is quiet on them and fires on a lower-cased one.
- `AgentReaderTests.cs:903-921` holds the malformed-`Entity.xml` and node-budget tests row 8 copies; `tests/PowerOps.Core.Tests/FixtureTests.cs` exists; `Analyzer.Run` (`Analyzer.cs:54-56`) sorts findings; `docs/COMMANDS.md:40` has the sentence row 7 keeps.

### High

No findings.

### Medium

No findings.

### Low

No findings.
