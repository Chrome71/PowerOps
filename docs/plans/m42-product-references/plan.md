---
status: approved
review: pending
signed-off: null
signed-off-hash: null
environment: null
depth: standard
supersedes: null
---

# M42: Product references, the prototype and the Power BI branch

## Goal

Give the plan skill the facts each product needs, offer a sketch of the screens before approval, and give the build skill a Power BI branch. This is M42 of Phase 7 ([plan](../phase7-power-platform/plan.md), build-order item 77, release label v0.25.0). It changes the text of three unpinned skills, `powerops-plan`, `powerops-implement` and `powerops-review-plan`, and `docs/HANDOFF.md`, with no product code. Decision 43 (D4) fixes the visual-count line at 12, and decision 47 (D8) holds the control map back until one canvas build has run.

1. A plan that names an app, page, site or report gets the route rules and that product's facts, read only for the products it names.
2. When `## Screens` names content laid out freely, the person is offered a sketch before approval, on the question-round path and on the fast path, and `decisions.md` records what happened.
3. A build step for Power BI follows its own rules, with no Dataverse solution checks.
4. The review asks the per-product size questions, and the hand-off page names the build skill for each product.

Done when:

- `skills/powerops-plan/references/` holds `route.md`, `canvas.md`, `model-driven.md`, `power-pages.md`, `code-apps.md` and `power-bi.md` beside `data.md`, each with a dated Sources list of Learn pages or MIT-licensed repos at a pinned commit, pointing at Microsoft's text rather than copying it.
- `powerops-plan` section 5 points to `references/route.md` for an app, page, site or report, and `route.md` names the product file to read next.
- `powerops-plan` offers **D**, "sketch the screens first", on the what-next menu (section 6) and the fast-path menu (section 8) when `route.md` says the content is laid out freely, and logs one `prototype:` line at approval: `approved mockups/prototype.html`, `skipped` or `not chosen`, the last only after D was shown. No new question.
- `powerops-implement` has a Power BI section, `pcf` and the two Power BI skills in its product list, `pac pcf push` among the writes that need a yes, and the sketch hand-off in step 3.
- `powerops-review-plan` item 11 has the per-product lines and the sketch line, each Medium.
- `docs/HANDOFF.md` has a per-product table with a row for each product, `pcf`, Power BI and Code Apps included.
- `tests/smoke/skills-smoke.sh` has a `quick` and a `standard` scenario that name new data and screens, and both ran once by hand on Claude Code, with whether each reference file was opened recorded in `decisions.md`.
- `powerops-plan/SKILL.md` stays at or under the M41 ceiling of 2,783 words and ASCII-only.
- `powerops-verify` and `powerops-review-security` keep their blob ids (`69208a2ed044a346bbae9877c8cd74d9e4794df9` and `6a6a6a1fdb71eb16d09cf3ed99406d04a0982a1f`), and no eval case, key or baseline file changes.
- The plugin copies are regenerated, `docs/PLANNING-SKILLS.md` and `docs/BUILD-PLAN.md` say what changed, and `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan are clean, with one `feat:` PR open.

### Won't build

- A control map or `data-control` names in the sketch, theme token work beyond the canvas theme YAML, or any claim that a build skill follows the sketch: decision 47's one build comes first.
- A chart library, a Figma or image pipeline, a `pbip check` (held, decision 44) or a Power BI row in `analyze`.
- A copy of Microsoft's reference text: the files point at it.
- A seventh reference file for the sketch: its rules go in `route.md`, which every plan with screens reads.
- A rerun of the `review-plan` pass on `plan-13` and `plan-14`: the new item 11 lines are Medium, so they can't change the pass rule's Highs (Q4).
- A check in the smoke script that a reference file was read: the transcripts differ per client, so the hand run records it instead.
- A new word ceiling: the brief keeps M41's, so the six data questions move to `data.md` (Q1).

## Users

- A maker who plans a canvas app, model-driven app, site, code app or Power BI report with an agent, and wants the product's limits in the plan before Microsoft's build skill starts.
- The person who signs plans off, and chooses whether to sketch the screens first.
- An agent running `powerops-implement` on a step that builds Power BI.

## Data

Line numbers are those of `ff04d60`.

| # | Change | Where | Test |
|---|---|---|---|
| 1 | Section 5's pointer bullet names the data round among what `data.md` holds, and gains: when the plan names an app, page, site or report, read `references/route.md`, then the file it names for each product | `skills/powerops-plan/SKILL.md:86` | wording test (row 15) |
| 2 | Section 6: the six data questions move to `data.md`; the Data round paragraph keeps every rule and names question 1 (where the data lives today). Step 6's menu gains **D** "sketch the screens first", when section 11 offers it, and the answers line gains "**D:** section 11." | `SKILL.md:124,131-140,154` | wording test |
| 3 | Section 8's menu gains **D**, the same option | `SKILL.md:180-183` | wording test |
| 4 | Section 11: offer D only when `## Screens` names content laid out freely, as `route.md` says; on D, or when asked for a screen, wireframe, mockup or prototype, write `docs/plans/<name>/mockups/prototype.html` by `route.md`'s rules, give its path and show the same menu again; the static-HTML, fake-data, Sketch-label, no-publish and section 4 rules stay; at approval, log `prototype: approved mockups/prototype.html`, `prototype: skipped` (the user dropped it) or `prototype: not chosen` (D shown, never picked); keep visual detail out of `Screens` and `Steps` | `SKILL.md:213-220` | wording test |
| 5 | `data.md`: the intro names the data round; a new section 6 holds the six questions as M41 wrote them | `skills/powerops-plan/references/data.md` | wording test |
| 6 | New `route.md`: when to read it; the route (form and view; generative page; custom page; PCF; Code App; Power Pages; Power BI for reports over a model), each line naming its product file; the sketch: which products get the offer and how, the file rules (one file, inline CSS, no scripts, fonts, images or external links, fake data, a "Sketch" label, at most 6 screens and 15 KB, a header comment with the route and for canvas the theme YAML, flex and `gap`, 4.5:1 and 3:1 contrast, visible labels, DOM order as reading order, no positive `tabindex`), that it proves intent and not the build, how it is recorded, and that the control map waits for decision 47 (the D8 line); Sources | new file | install test (row 16), wording test |
| 7 | New `canvas.md`: personas and form factor; rows per source and delegation; the theme and Paste theme; the first step creates the blank app inside the named solution with coauthoring on; a person saves and publishes, because a compile isn't a save; `canvas-app`'s own plan stays; the dated "doesn't map" list, each element with a workaround or dropped; Sources | new file | same |
| 8 | New `model-driven.md`: `Screens` lists each form's tabs and sections and each page's type and reason; planned removals; the phone and tablet form sizes as a question only when the plan names them; each persona's roles together read the tables its tasks use; a dashboard within Learn's default of six components; custom pages, generative pages (the sketch goes in as a screenshot) and code components; Sources | new file | same |
| 9 | New `power-pages.md`: site type and what it gives up; the enhanced data model only; one table-permission row per table (web role, scope, privileges, reason); Global only for read-only public reference data; the Bootstrap version read from the site, and stop if unknown; no local sign-in in production; a load test for thousands of users a month; the sketch's Bootstrap note; Sources | new file | same |
| 10 | New `code-apps.md`: the route row and why; every user's licence and Entra sign-in; the admin setting confirmed; each data source; the target solution by id; each CSP exception; nothing sensitive in app assets; the baseline push needs a yes; the mock-data first slice in place of a sketch; Sources | new file | same |
| 11 | New `power-bi.md`: the efficiency list and the five design tests of the Phase 7 plan, with Learn's wording where the fact check corrected it (numeric types, Pro's limits, the deploy routes); 12 data visuals a page as the review line (decision 43, from Microsoft's skill; Learn gives no number); the one-page sketch; Sources | new file | same |
| 12 | `powerops-implement`: step 3's product list gains `pcf` and Power BI (`semantic-model-authoring`, `powerbi-report-cli`, in microsoft/skills-for-fabric); step 3 gains the sketch hand-off (pass the path, not a screenshot; for a generative page the user attaches a screenshot in the page builder; nothing shows a build skill follows it); step 7's writes gain `pac pcf push`; a new section 5, Power BI: solution checks are "not applicable", the build is local PBIP work, a workspace is named only for a step that deploys, every service write needs a yes, the finish runs `powerbi-report-author validate` and the five design tests on screenshots the user gives, a mixed plan finishes each part by its own rule, and the two reports read the solution part only | `skills/powerops-implement/SKILL.md:38,51`, new section | wording test |
| 13 | `powerops-review-plan` item 11 gains per-product lines, each Medium and asked only when the plan names the product: canvas (several personas with different tasks in one app and no reason; a source above 500 rows with no delegation note; a new component where a library has one); model-driven (a page type with no reason; removals not planned; a persona whose roles together can't read a table its tasks use, roles that split access across personas being fine; a dashboard over six components; phone or tablet forms over 5 tabs, 75 columns or 10 subgrids); Power Pages (Global scope beyond read-only public reference data); Code Apps (no reason a code app is needed); Power BI (a page over 12 data visuals); and the sketch (a screen not in `Screens`, or a canvas element on the "doesn't map" list with no workaround) | `skills/powerops-review-plan/SKILL.md:53` | wording test |
| 14 | `docs/HANDOFF.md`: a "Per product" section with a table, one row per product (canvas, model-driven, `pcf`, Power Pages, cloud flows, Code Apps, Dataverse, Power BI): the Microsoft skill and plugin, what PowerOps does after it, and how it reaches Test and Prod, with Power BI's own route and its "needs a yes"; step 2 names `pcf` | `docs/HANDOFF.md:12-15` | `HandoffDocTests` (row 17) |
| 15 | `Plan_skills_carry_the_product_references_the_sketch_and_the_power_bi_branch`: the plan skill holds the route pointer before section 6, D on both menus and the three log values; `route.md` holds the offer table, the file rules and the D8 line; each product file holds its Sources line; `data.md` holds the six questions; the review holds the product lines; the implement skill holds the Power BI section and the hand-off lines. The M41 wording test drops the asserts that moved | `tests/PowerOps.Cli.Tests/SkillsCommandTests.cs` | itself |
| 16 | `Install_writes_the_plan_reference_beside_its_skill` covers all seven reference files for both targets. `Plan_skill_stays_under_its_word_ceiling` keeps 2,783 | same file | itself |
| 17 | `HandoffDocTests`: the per-product table has a row for each product, and each row names its Microsoft skill | `tests/PowerOps.Cli.Tests/HandoffDocTests.cs` | itself |
| 18 | `skills-smoke.sh`: `menu_letters` reads A to D, and the two fast-path scenarios still expect `ABC`; a `quick` scenario (a repo with an earlier plan at `depth: quick`, a canvas app over a new list) that approves at the first menu and checks one round, at most three questions, a licensing and an environments question among them, a lettered sketch option, and `prototype: not chosen`; a `standard` scenario (the same idea, two lists) that picks A, A, D, then B, and checks the rounds' shape, a question about rows, the sketch file (present, a "Sketch" label, no `<script`, at most 15 KB) and `prototype: approved` | `tests/smoke/skills-smoke.sh` | the hand run (Automation) |
| 19 | The plugin copies, by "Update goldens" | `plugins/claude/skills/`, `plugins/codex/skills/` | `PluginManifestTests` |
| 20 | `docs/PLANNING-SKILLS.md`: a paragraph on the product references and the sketch under "Plan files"; the mockup line under "Skills" | `docs/PLANNING-SKILLS.md:101,148` | none, docs only |
| 21 | `docs/BUILD-PLAN.md`: M42 marked built in the Phase 7 table and item 77 with the plan link, and an M42 line in the verification record | `docs/BUILD-PLAN.md:522,539` and the record | none, docs only |

## Automation

The hand run, after rows 1 to 19 are committed, on a copy of `skills-smoke.sh` that runs only the two new scenarios: `claude` once, with the client version recorded. Afterwards the session transcripts of that run are searched for each reference file's name, and `decisions.md` records for each scenario which files were opened. Codex runs too if its quota allows, recorded the same way; if it doesn't, it is written as not run. A failed check is fixed in the skill text or the scenario and the scenario run again, at most twice; a third failure stops the build before the PR and is reported to the master session.

## Screens

None.

## Environments and route

None. No step reads or writes a tenant or runs `pac`. The smoke run works in temp folders.

## Solution and publisher

None.

## Constraints

- AGENTS.md: no dependency, no product code, fake values only, no version or changelog edits, Conventional Commits, `dotnet format` clean.
- No change to `powerops-verify`, `powerops-review-security`, any eval case, the key or a baseline.
- Sources are Learn pages or openly licensed repos (MIT) at a pinned commit, with the date checked.
- The smoke run costs model quota: two scenarios of about ten turns each, plus the simulated user.

## Assumptions

- Facts checked on Learn on 2026-11-03 by two researcher subagents (Sonnet): the modern controls reference lists 28 controls, 7 in preview, and none of accordion, breadcrumb, dialog, drawer, menu, nav, toast, toolbar or tree; Paste theme imports a theme as YAML; canvas apps in a solution by default is a preview setting, and coauthoring is turned on per app; phone and tablet forms are limited to 5 tabs or 75 columns and 10 subgrids, on that presentation only; a dashboard holds up to six components by default, and 20 appears only on the on-premises page, so the cloud default is the line written; model-driven apps share through security roles; new Power Pages sites use the enhanced data model; Global table permissions apply to all records; Bootstrap 3.3.6 or 5; local sign-in is not recommended and the go-live checklist recommends turning it off; load testing is advised for thousands of users a month; code apps need an admin setting, Entra sign-in and a qualifying licence (Premium, pay-as-you-go, App Pass or auto-claim), a per-environment CSP, and keep sensitive data out of app assets, which are on a public endpoint; generative pages take images and "clear and detailed sketches or wireframes" work best; custom pages don't use the modern theme. Power BI: dimension or fact; minimise bi-directional relationships; turn off auto date/time where it isn't needed; remove columns that serve neither reporting nor the model; Import is the default mode; incremental refresh for fact tables that grow; Fixed decimal is exact to four places, Decimal is floating point; Pro has a 1 GB model, 8 scheduled refreshes a day, no XMLA write and no deployment pipelines; RLS guidance; PBIP and PBIR in Git; fabric-cicd, Git integration and deployment pipelines; alt text, tab order, 4.5:1 contrast and colour never the only signal; "limit the number of visuals" with no number; the embedded dashboard's environment variable.
- Read in MIT repos on 2026-11-03: skills-for-fabric at `4c3986d` names `powerbi-report-cli` and `semantic-model-authoring` in its `powerbi-authoring` plugin, documents `powerbi-report-author validate`, and warns above 12 visuals a page (`references/design/anti-patterns.md`); power-platform-skills at `96d16d0` has a `pcf` plugin whose skill deploys with `pac pcf push`.
- The route lines ("generative page for overviews, queues and guided work", and the Code App conditions) are the Phase 7 plan's rules, not Learn's; `route.md` says so and cites Learn only for what it states.
- `run.sh` copies only `SKILL.md` into a run, so item 11 carries its product lines itself and never reads the plan skill's references (as M41's Q2).
- `skills install` and the plugin copies carry every file under `skills/`, so the new references need no code (M41's finding).
- The item 11 lines are Medium, so `plan-14`'s pass rule, no item 11 High, holds as worded; whether the longer item changes what a reviewer finds on `plan-13` and `plan-14` is not measured.
- A model that asks a rows question in the `standard` scenario has either read `data.md`, where the questions now are, or improvised one; the transcripts tell the two apart.
- Environment facts were not read: this plan changes this repo and targets no environment.

## Open questions

## Questions for your admin

## Steps

1. This plan, a review in a fresh context, fixes, sign-off.
2. Rows 15 to 17 first, and see them fail; then rows 1 to 14 until they pass.
3. Row 18, and row 19 by "Update goldens", reading the diff.
4. Commit, then the hand run (Automation), and its record in `decisions.md`.
5. Rows 20 and 21.
6. `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `powerops plan status .` and the leak scan (`bash reports/leakscan.sh origin/main <this branch's commit>` from the main checkout); push and open one `feat:` PR. The master session merges.
