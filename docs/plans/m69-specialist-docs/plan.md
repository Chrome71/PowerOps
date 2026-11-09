---
status: done
review: done
signed-off: 2026-11-05
signed-off-hash: 5b09f52daddba22cbde70fdc84be37e9df4b8c845e5156f12a25f80d6d22666d
environment: null
depth: quick
supersedes: null
---

# M69: Bring the hand-off page and the plan references up to Microsoft's tooling

## Goal

Apply row A, and the docs part of row B, of the 2026-11-05 specialist-team research backlog to `docs/HANDOFF.md` and `skills/powerops-plan/references/`. Six research agents found that both lag Microsoft's tooling: the Copilot Studio plugin has a successor, deployment pipelines don't take PBIR reports, step 10 sends the agent to a skill that stops on a flow that never ran, step 11 misses four target settings, and several tenant writes reach no gate. The research is model output and stays private (`docs/research/` is git-ignored); every Microsoft claim below was checked again on Learn or in the named public repository on 2026-11-05 (Assumptions), and a claim that didn't hold was corrected or dropped. M63 is in flight (PR #106) and the live-test improvement plan reserves M64 to M68, so this is M69.

Done when:

1. `docs/HANDOFF.md` carries, each with its source and the date checked:
   1. the Power BI route caveat (deployment pipelines don't support PBIR, the default report format) and the deletes some Power BI routes make by design (`semantic-model-authoring`'s duplicate cleanup, fabric-cicd's orphan cleanup and `enable_hard_delete`, deployment pipelines' `allowPurgeData`);
   2. `microsoft/copilot-studio-plugin` named in the Copilot Studio row as the successor, with its `migrate` command and its writes;
   3. step 10 rewritten: `debug-flow` stops on a flow with no runs, Learn's causes of an off flow, and publish alone, never `manage-flows`' publish-and-test, in Test or Prod;
   4. step 11 with the Key Vault tags, the target's Authentication for agents setting, separate sharing of connected agents, and Add required objects before the next export;
   5. the writes no gate sees, with deny guidance for the MCP ones: FlowAgent's `copy_flow`, `publish_flow` and `update_flow` outside Dev, `pa app share`, `pa connection create`, the Copilot Studio MCP server's `manage_agents` and `manage_workflows`, `pac copilot quarantine` and `merge-translation`, and the Power BI Authoring MCP server's model edits.
2. In `skills/powerops-plan/references/`: `canvas.md` (30 modern controls, the Creator Kit caveat, Fluent UI v8 deprecated), `code-apps.md` (no Git integration; Learn's status line), `model-driven.md` (generative pages GA, the preview migration, the modern theme and `app-builder`'s `app.theme`), `copilot-studio.md` (the harness advice as PowerOps's, the preview upgrade, the successor plugin) and `power-bi.md` (the PBIR caveat, the delete traps, sensitivity labels) are corrected, each changed claim under a `Checked on 2026-11-05:` source list.
3. New `references/design.md` (one Theme block, WCAG 2.2 AA, the states of each screen), linked from `route.md`, and new `references/logic.md` (placement table, state-model card, approval expiry, retries), linked from `data.md`. `SKILL.md` doesn't change.
4. `SkillsCommandTests` and `HandoffDocTests` pin the new links and files, step 10's publish line, step 11's items, the ten unrouted writes, the PBIR caveat, the delete traps and the successor plugin; goldens are refreshed by the "Update goldens" recipe, so `plugins/*/skills/` copy the references.
5. `dotnet build`, `dotnet test` (all of it in this worktree; the three `DocsDriftTests` failures on the git-ignored `docs/research/` notes occur only in the main checkout, and CI doesn't see them), `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean; the work is committed on its own branch, not pushed.

### Won't build

- Gate routes or a matcher that reads `copy_flow`'s target environment (backlog row B's code part: M, and it needs a capture of each call's shape first).
- Rules, readers, the simulator or the held PBIP check (backlog rows C to H).
- The rest of finding 5 that the brief didn't name: the Canvas Authoring MCP server's tool-name drift, the mobile-apps plugin, Microsoft Entra Agent ID, the retirement of "Describe it to design it", and `powerbi-report-author validate` being structural only. A later docs pass.
- Any change to `powerops-plan/SKILL.md` (at its 2,783-word cap), `powerops-verify` or `powerops-review-security` (frozen roles).
- The claim that code apps are generally available, and a GA date: Learn's pages give none (Assumptions).
- The research's single GA date for generative pages: Learn's release plans give 2025-10-30 for US environments and 2026-04-02 globally, so `model-driven.md` gives both.

## Users

- An agent that follows `docs/HANDOFF.md` after a Microsoft skill built in Dev, and the person who says yes to each write.
- An agent that runs `powerops-plan` and reads its references for a plan with screens, logic or Power BI.

## Data

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Power BI row: deployment pipelines don't support PBIR reports, the default report format, so a PBIR report goes by fabric-cicd or Git integration (Git integration marks reports and semantic models as preview); point to a new "Power BI deletes" paragraph under the table: the model skill's "delete every duplicate" after a retried deploy, fabric-cicd's orphan cleanup (`unpublish_all_orphan_items`, the item types in scope) and its `enable_hard_delete` flag, and `allowPurgeData` in a deployment pipelines deploy (deletes the target's data on a schema mismatch). Each is a delete: the person runs it | `docs/HANDOFF.md:26` and after the table | `HandoffDocTests` keeps "needs a yes" and "never runs a delete" on the row, and pins the PBIR sentence and the four delete names (R2) |
| 2 | Copilot Studio row: `microsoft/copilot-studio-plugin` (MIT, "work in progress") calls itself the successor of skills-for-copilot-studio and builds for the GitHub Copilot harness; its `migrate` converts a classic (standard) agent and pushes it; PO027 to PO031 don't check the result; its writes are `pac copilot init`, `push`, `publish` (the gate blocks them) and `delete` (no route) | `docs/HANDOFF.md:25` | row keeps `copilot-studio` and "(step 11)", and pins `copilot-studio-plugin`, `migrate` and `delete` (R2) |
| 3 | Step 10: Learn's causes (a connection reference with no connection; connections not shared with the importing user; an existing flow keeps its state, so an off flow stays off); such a flow has usually never run, and `debug-flow` then says so and stops; read the flow and its connections instead; turn it on with publish alone (`publish_flow`, or `manage-flows`' batch publish), never its publish-and-test, which runs the flow, in Test or Prod; decline `debug-flow`'s offer to run or test the flow; the gate doesn't see `publish_flow`, so the user's yes is the only check (R5, R6). The Cloud flow row's last cell says "a publish" instead of `manage-flows` | `docs/HANDOFF.md:22`, `:51` | new `HandoffDocTests` fact: step 10 names `publish_flow` and "publish-and-test" |
| 4 | Step 11: a secret variable's Key Vault secret tagged for the target (`AllowedEnvironments`, or the agent's `{envId}/{schemaName}`, which Copilot Studio's page names `AllowedAgents` and the Power Apps page `AllowedBots`), and the Key Vault Secrets User role for the Microsoft Copilot Studio Service; the target's Authentication for agents setting (preview) can stop the agent from answering; a connected agent is shared on its own; before the next export, Add required objects. Sources updated to 2026-11-05 | `docs/HANDOFF.md:52` | the step 11 fact gets "Key Vault", "Authentication for agents", "connected agent", "Add required objects" |
| 5 | "What it doesn't do": the no-route list gains `pac copilot quarantine` and `merge-translation`, `pa app share` and `pa connection create`; the "runs no shell command" item gains `manage_workflows`, FlowAgent's `copy_flow` to another environment and `publish_flow` and `update_flow` outside Dev, and the Power BI Authoring MCP server's model edits. The deny paragraph says to deny FlowAgent's `copy_flow`, `publish_flow` and `update_flow` and the Copilot Studio MCP server's `manage_agents` and `manage_workflows` by their full names (`mcp__<server>__<tool>`) while the hooks are on; the Power BI Authoring MCP server is listed, not denied, because Learn groups its tools by object type, not by read and write, and the paragraph says so (R1, R11). Sources line gains the pages. `docs/COMMANDS.md`'s `deploy gate` paragraph repeats the same list (R4) | `docs/HANDOFF.md:98`, `:103-104`, `:108`; `docs/COMMANDS.md:131` | new `HandoffDocTests` fact: the section names each of the ten writes |
| 6 | `canvas.md`: 30 controls on 2026-11-05; the Creator Kit isn't officially supported, so only when no modern control fits; Fluent UI (v8) controls in custom pages and Teams apps are deprecated, Studio's Update covers seven and drops properties it can't map; the theme line points to `design.md` | `references/canvas.md` | — |
| 7 | `code-apps.md`: code apps don't support Power Platform Git integration, so the source lives in the team's repository; Learn's overview carries no preview label and sends bugs to standard support, with no date | `references/code-apps.md` | — |
| 8 | `model-driven.md`: generative pages GA (release plans: US environments 2025-10-30, globally 2026-04-02; the page builder only in Learn's regions, AI code generation tools elsewhere, R3), connectors and custom APIs in them preview, a preview-made page migrates when opened once in the app designer; a Theme item: the `CustomTheme` web resource named by Custom theme definition, classic themes not honoured in the new look (mandatory from 2026 release wave 1), `app-builder`'s `app.theme` (preview, 2.21.0 or later) writes both into the solution, values from `design.md` | `references/model-driven.md` | — |
| 9 | `copilot-studio.md`: "Prefer the standard harness" is PowerOps's advice, since Learn calls the GitHub Copilot harness the most capable and says neither is better for every agent; no move between harnesses, but a preview upgrade copies a standard agent without its knowledge, Power Fx or child agents; the successor plugin and `migrate` take an agent out of PO027 to PO031 | `references/copilot-studio.md` | — |
| 10 | `power-bi.md`: the PBIR caveat and the DirectQuery or composite model with auto date/time or variation tables that deployment pipelines don't support; the delete traps under Microsoft's gates; sensitivity labels aren't supported in a PBIP | `references/power-bi.md` | keeps "12 data visuals" |
| 11 | New `design.md`: section 1 one `### Theme` block in `Screens` (brand values; canvas Paste-theme YAML; model-driven `CustomTheme` attributes; Power Pages Styling theme and Bootstrap version; Power BI theme JSON by name; code apps and generative pages name the UI library); section 2 WCAG 2.2 AA (the six A and AA additions, contrast from XO:02, Microsoft pages that cite 2.1); section 3 per screen: primary task, form factor, and the empty, loading, error and failed-save states with the next step | `references/design.md` (new) | Install and pointer tests list it |
| 12 | New `logic.md`: section 1 placement table (rule on every write path, form-only actions, formula, rollup, transaction, side effects, named states, approval); section 2 state-model card; section 3 approval expiry, over 30 days, retries, together or apart | `references/logic.md` (new) | Install and pointer tests list it |
| 13 | Links: `route.md` section 1 sends any screen, page, site or report to `design.md` and an app's rules, status columns and approvals to `logic.md` (R8), and its Custom pages row notes v8 with a `Checked on 2026-11-05:` source (R9); `data.md` section 5 sends rules, state models, approvals and retries to `logic.md`; `docs/PLANNING-SKILLS.md`'s "Product references" paragraph names `copilot-studio.md`, `design.md` and `logic.md` (R4) | `references/route.md`, `references/data.md`, `docs/PLANNING-SKILLS.md:151` | `SkillsCommandTests` checks the pointers |
| 14 | Plugin copies of the references | `plugins/claude/skills/`, `plugins/codex/skills/` | `PluginManifestTests` |

## Automation

None.

## Screens

None.

## Environments and route

No tenant. Docs, two plan-skill references and two test files in this repo; nothing is read from or written to an environment.

## Solution and publisher

None.

## Constraints

- AGENTS.md: smallest change, Conventional Commits, no version or changelog edits, no tenant URLs, ids, emails or names; open sources only (Learn and Microsoft's MIT repositories).
- `SKILL.md` stays at or under 2,783 words (`SkillsCommandTests`), so the new text goes in references.
- `HandoffDocTests` pin the product table's skills, the step numbers 1 to 11, step 11's items and the delete claim; `DocsDriftTests` check every `powerops` command, flag and MCP tool name on the docs pages.
- The product references keep `## Sources\n\nChecked on 2026-11-03` (`SkillsCommandTests`); claims checked again go in a second `Checked on 2026-11-05:` list.
- The frozen roles and `tests/eval` aren't touched, so no eval rerun is due.
- Platforms are Windows and Linux.

## Assumptions

Each was checked on 2026-11-05 with the Learn MCP tools or `gh api`; the Learn fetch shows no page date.

- Power BI: "Understand the deployment process" lists "PBIR reports aren't supported" and doesn't support a DirectQuery or Composite model that uses variation or auto date/time tables; "the PBIP report folder" says PBIR is generally available and the default, and that Git integration and the Fabric REST APIs export PBIR; "fabric-cicd for PBIP" deploys a PBIR project and its orphan cleanup removes items no longer in source control; the "Azure DevOps tutorial" names `unpublish_all_orphan_items` and the types in scope; "fabric-cicd's optional features" list `enable_hard_delete`; "Git integration" marks Report and Semantic model preview; "Pipelines Deploy All" documents `allowPurgeData` (the research's `purgeData` isn't a documented name); "Power BI projects" says sensitivity labels aren't supported; "Power BI Authoring MCP" edits semantic models in a workspace (no documented read-only mode on Learn). skills-for-fabric `4c3986d` (MIT), `semantic-model-authoring/SKILL.md` line 264: "delete every duplicate with that name" after a retried deploy.
- Copilot Studio: "Variables" names `AllowedEnvironments` and `AllowedAgents` (`{envId}/{schemaName}`) and the Key Vault Secrets User role for the Microsoft Copilot Studio Service; "Key Vault secrets" names `AllowedBots` instead; "Authentication for agents" (preview) blocks updates and stops a non-complying agent responding; "connected agents" are shared separately, and the "standard-harness page" says end users may need it shared; "Export and import agents": Add required objects; "Copilot Studio MCP": `manage_agents` and `manage_workflows` create, update, publish and delete by action; "pac copilot": `quarantine` sets an agent's quarantine status and `merge-translation` updates its components unless `--sourcedir` (an inference from the parameters); "Harnesses" calls the GitHub Copilot harness "the most capable option", "Choose a harness" says neither is universally better, "the upgrade page" (preview) copies an agent without knowledge, Power Fx or child agents. `microsoft/copilot-studio-plugin` `4014ae6` (MIT): the README's successor line and "work in progress"; `commands/migrate.md` (classic harness to the new agentic loop, then push); the skills run `pac copilot init`, `push`, `publish` and `delete`.
- Power Apps: the "modern controls reference" lists 30, the same 7 in preview; "Creator Kit explained": not officially supported, modern controls first; "Upgrade Fluent UI controls": v8 deprecated in Teams canvas apps and custom pages, seven controls, unmapped properties dropped; "code apps overview": no Git integration, no preview label, bugs to standard support, no GA date (the client library's v1.0 guide says code apps "approach general availability"); "generative pages": connectors and custom APIs preview, the one-time migration, and the page builder only in the US, Great Britain, Australia and Singapore with AI code generation tools worldwide; the "2025 wave 2 release plan" (archived): GA 2025-10-30, US environments only; the 2026 wave 1 planned features: "Use generative pages globally" GA 2026-04-02; "modern look": mandatory with 2026 wave 1 (its Revert section still names an admin setting); "modern theme overrides": `CustomTheme` XML, Custom theme definition, 4.5:1 for the header, classic theming not honoured; "Power Apps CLI": `pa app share` grants play or edit, `pa connection create` creates a connection. power-platform-skills `4076781` (MIT): `model-apps/references/app-spec-schema.md` `app.theme` (preview, 2.21.0); `power-automate/skills/debug-flow/SKILL.md` stops on no runs; `manage-flows/SKILL.md` publish-and-test calls `run_flow`.
- Power Automate: "the plugin page" gives "Copy the Invoice Processing flow to the test environment"; "Import a solution": connections, sharing them with the importing user, an existing flow keeps its state; "limits": 30-day runs; "modern approvals": over 30 days, Dataverse and two flows; "error reference": an expiration and a timeout branch, Timeout as an ISO 8601 duration.
- Design: "modern theming": the YAML fields and WCAG 2.1 AA at least; "report themes": only `name` required; "Style a site": 13 presets; "Power Pages accessibility": WCAG 2.2; "W3C, new in 2.2": the six A and AA criteria, 24 by 24 CSS pixels; "XO design standards": 4.5:1 and 3:1; "XO feedback and guidance": errors with next steps (it names no loading or empty state, so those are PowerOps's rule).
- Logic: "business rules": Entity scope forms and server, canvas apps need it, show, hide, lock and required only on forms; "formula columns": no workflows or plug-ins, no value in mobile offline; "rollup columns": asynchronous, 50 per table and 200 per environment by default, while the developer page "Specialized columns using code" still gives 10 and 100 (R7), so `logic.md` names both; "event framework": PreOperation and PostOperation in the transaction, asynchronous steps outside; "changeset": all or nothing, add, update and delete only, no earlier outputs inside; "custom state model transitions": with `EnforceStateTransitions`, a disallowed status reason throws (whether it covers every write path isn't stated, so the card asks where it is enforced).
- Gate code at `f22c002`: `ImportCall`'s route regex matches no `pa app share`, `pa connection create`, `pac copilot quarantine` or `merge-translation`, so they pass the gate; the hook matcher names only pac's MCP tools, so FlowAgent, Copilot Studio MCP and Power BI MCP calls never reach it.
- Environment facts were not read: the plan names no environment and changes only this repo.

## Open questions

## Questions for your admin

## Steps

1. This plan and `decisions.md`, a fresh-context review into `review.md`, fixes, and sign-off by the building session under the delegation of 2026-10-22 once no High or Medium finding is open.
2. The test pins of rows 3 to 5 and 11 to 13 first (they fail on the current text), then rows 1 to 13.
3. The "Update goldens" recipe for row 14, and a read of `git diff tests/golden plugins`.
4. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .`, the leak scan; commit on the branch.
