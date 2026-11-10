# Decisions: m79-po035-navigation

## Round 0 (the person's instruction for the chain, 2026-11-09)

The person: "Once 75 is finished with its PR continue with the other M sessions in new sessions each until you wrap up M83 autonomously."

### Q1. For M76 and M79–M83, who signs off each milestone's plan before it is built?
1. Sessions sign off: each session signs its own plan once the Fable plan review approves it and `powerops-review-plan` leaves no finding open, and records that it did so on the person's instruction of 2026-11-09 (recommended)

Answer: "Sessions sign off (Recommended)" (answered by: person, 2026-11-09).

### Q2. Who merges each milestone's PR once all 8 CI checks pass?
1. Sessions merge: each session rebases on main, reruns its checks, opens its PR and squash-merges it when green, one PR at a time (recommended)

Answer: "Sessions merge (Recommended)" (answered by: person, 2026-11-09).

## Log

- 2026-11-09: planned from the M79 entry of the agreed 1.0 fix plan (sections 3, 4 and 5), in a session started by the chain session, one of five running in parallel (M76, M79 to M82). The fix plan's names come from the trial, so this plan and its fixture use made-up `demo_` names.
- read on 2026-11-09: the fix plan's M79 entry and sections 1, 2, 4 and 5; `chain-rules.md`; `m74-done-handover.md`; AGENTS.md; `Solution.cs`; `SolutionParser.cs` (`Parse`, `ReadAgents`, `ReadToolTable`, `ReadEntitySets`); `YamlReader.cs`; `YamlXml.cs`; `FlowDefinition.cs`; `RuleHelpers.cs`; `PO012`, `PO019`, `PO034`; `INotChecked.cs`; `docs/rules/PO034.md`; `docs/COMMANDS.md` (`analyze`); the relationship files of every fixture (`alm-makers`, `alm-sample`, `powerops-seed`, `powerops-seed-yaml`, the four zips); the `DeployToStage` flow of `alm-makers`; `tests/eval/roles/cases` (no `$expand` or `@odata.bind`)
- 2026-11-09: fixture survey. 68 one-to-many relationships in the unpacked fixtures, 8 with `EntityRelationshipRoles` (each a role 1 and a role 0, role 1's name equal to `ReferencingAttributeName`), 60 without (system relationships); `polymorphiclookupexamplesolution.zip` holds 3 with roles in the same layout, role 1 on the referencing table named `<attribute>_<referenced table>` (a multi-table lookup). No many-to-many relationship in any fixture, and the YAML fixtures' relationships have no roles.
- 2026-11-09: research (a `powerops-researcher` subagent, Sonnet, Learn only). (1) Confirmed: *Web API navigation properties* (learn.microsoft.com/power-apps/developer/data-platform/webapi/web-api-navigation-properties) calls the navigation property name case-sensitive; *Use multi-table lookup columns* (…/webapi/multitable-lookup) says the same for `@odata.bind`; *Troubleshoot Dataverse Web API client errors* shows the error for an undeclared property. Learn shows no error text for a wrong-case `$expand`, and doesn't say the connector passes names on unchanged. (2) Confirmed: one-to-many `ReferencingEntityNavigationPropertyName` (single-valued) and `ReferencedEntityNavigationPropertyName` (collection-valued); many-to-many `Entity1NavigationPropertyName` and `Entity2NavigationPropertyName`. (3) Partly: *Use lists of rows in flows* and *Get a row by ID from Dataverse* give "Expand Query" with navigation property names; *Add a row* and *Update a row* give only the `entityset(id)` value form, not the `@odata.bind` key, which is observed in exports. (4) Not on Learn as prose: the export's `EntityRelationshipRole` layout; *Customization solutions file schema* names the elements in its XSD only.
- 2026-11-09, routine choices: a flat `NavigationProperties` list instead of a `Relationships` list of nested records, because the rule needs only (entity set, name) pairs; many-to-many not read (no export observed); no `note:` line for unread values (Won't build); a name with no case-insensitive match is quiet.
- depth: quick: the fix plan gives the design
- areas not asked: users, automation, environments and ALM, screens, solution and publisher; the change is to this repo's analyzer, and the plan answers each or marks it not applicable
- approved 2026-11-09 by the building session on the person's instruction of 2026-11-09, for the Fable plan review and a fresh-context review
