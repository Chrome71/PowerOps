# Build standard: leave a solution the next person can change

Read this before the first build step, and again before each commit. It applies to everything the plan's `Steps` build, whichever Microsoft skill or tool writes it. The solution will be maintained for years by people and agents who weren't here: a build that passes its checks and breaks these rules isn't finished. These are PowerOps's rules; the sources say what Learn states.

## 1. The rules

1. **One home for each rule.** A calculation, a validation, a status change or a permission lives in the one place the plan gives it (`logic.md` of `powerops-plan`), once. Before you write logic, search the solution for it. If it exists, call it. If two places need it, give it a name and use the name: a named formula or user-defined function in `App.Formulas`, a component, a child flow, a column or a server-side rule. Never paste an expression into a second control, action or flow.
2. **Change in place.** To change a thing, edit that thing: the property, the action, the column, the instruction. Don't add a second control, flow, column, rule or topic beside the old one, and don't rebuild a screen, a flow or an agent to change part of it. When a tool can only replace the whole (a canvas compile of a screen, a flow definition update, a push), start from the current saved source, change only the lines the step needs, and compare before and after: the difference is the step and nothing else.
3. **A fix replaces what it fixed.** When an attempt fails, take it out before you try the next. Leave no overlay over a broken control, no hidden or disabled copy, no `_old`, `_new`, `v2`, `Copy of` or `(1)`, and no action that is switched off "for now". If the platform forces a workaround, it is one named thing, with a comment that says why it exists and when it can go, and a line in the build notes (section 3).
4. **Names say what a thing is for.** No default names (`Button1`, `Compose 2`, `Condition 3`, `new_table1`). Use the product's convention from the table below, and the same word for the same thing in the plan, the tables, the app and the flows.
5. **Small pieces.** A formula a person can't read at a glance splits into named formulas. A flow stays under 50 actions and 4 levels of nesting (PO016), with shared steps in a child flow and failure handling in one scope, not copied after each action. A screen does one job.
6. **Nothing unused.** No variable, collection, data source, connection reference, screen, control, column, view, flow or topic that nothing uses. No sample or mock data, no commented-out block, no test rows left behind.
7. **Say why, once.** Comment only what the thing can't show: why a query is shaped for delegation, why a workaround exists. One line, at the place: a `//` comment in Power Fx, a note on a flow action, the description of a column, table, flow, variable or tool.
8. **Never bend a check to pass.** Don't delete, weaken or skip a test, don't add a suppression or a baseline entry, don't hide an App checker or solution checker result, and don't lower a state, a contrast pair or a limit to get a green run. Fix the cause. If the cause is outside the plan, stop and ask (the skill's rule 6): a suppression is the person's decision, with their reason written beside it.

## 2. Per product

| Product | Reuse through | Names | Read before changing |
|---|---|---|---|
| Canvas app, custom page | `App.Formulas` (named formulas, user-defined functions), components, the theme | Screens in plain words ending in "Screen"; controls camelCase with a type prefix and the purpose (`btnSubmitRequest`, `galOpenRequests`); `gbl`, `loc` and `col` prefixes for variables and collections | The app's current `.pa.yaml` from a fresh sync, never an older local copy |
| Cloud flow | Child flows in the same solution, one Try and one Catch scope, environment variables | The flow by what it does and when; every trigger and action renamed to what it does, with a note where the logic isn't obvious | The saved definition, and the flow's tests |
| Dataverse | Columns, choices, business rules, formula and rollup columns, plug-ins, by `logic.md` | Publisher prefix; singular table names; a description on every table and column the plan adds | The table's existing columns, rules and relationships |
| Model-driven app | One main form per table and role, views, the site map | Forms and views named for their audience or filter | The app's forms and views |
| Power Pages, code app, PCF | Shared templates, components and one style sheet or token set | The repo's conventions for the language | The site or app source in the repo |
| Copilot Studio agent | One topic or tool per job; instructions in one place | Tools and topics by what they do; descriptions the orchestrator can choose by | The cloned agent files |

## 3. Before each commit, and at the end

- **Read the diff as the next maintainer.** Every changed line belongs to this step. Nothing is duplicated (rule 1). Nothing unrelated was rewritten or reformatted (rule 2); put back what was. Nothing from a failed attempt is left (rule 3). If the diff is larger than the step, find out why before you commit.
- **Run `powerops analyze`** when the solution is on disk: PO016 to PO018 and PO036 report oversized flows, loops in loops, writes in loops and a formula repeated in three or more places. Fix them at the cause.
- **Build notes.** Keep `docs/plans/<name>/build-notes.md`, committed with the step that changes it, so that what you learned doesn't end with the chat. Start it with `# Build notes: <name>`, then these headings, each with `- ` items or the line "None.":
  - `## Where the logic lives`: each rule, calculation and status change the build made, with its place and name. One line each.
  - `## Workarounds`: what, why, and the condition under which it can be removed.
  - `## Differs from the plan`: each agreed difference, with the `decisions.md` line that agreed it.
  - `## Left for the maintainer`: known gaps, things not verified, and anything that looks wrong but was out of scope.

  Write no URL, id, email, person's name or environment name in it: `powerops plan status` scans the folder.

## Sources

Checked on 2026-11-10:

- [Code readability](https://learn.microsoft.com/power-apps/guidance/coding-guidelines/code-readability) (Power Apps coding guidelines): screen names in plain language ending in "Screen", control names in camel case with a type prefix and the purpose, the `gbl`, `loc` and `col` prefixes, and comments for the next developer.
- [App object in Power Apps](https://learn.microsoft.com/power-platform/power-fx/reference/object-app#formulas-property): named formulas and user-defined functions in `App.Formulas`, defined once and reused. [Build large and complex canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/working-with-large-apps): a copied control duplicates its formulas, and long formulas split into parts that are reused.
- [Use consistent naming for flow components](https://learn.microsoft.com/power-automate/guidance/coding-guidelines/use-consistent-naming-conventions): descriptive names instead of defaults such as "Trigger1", and notes on actions. [Create reusable code with child flows](https://learn.microsoft.com/power-automate/guidance/coding-guidelines/create-reusable-code): child flows for logic several flows share, created in the same solution.
- Rules 2, 3, 6 and 8, the rows of the table that name no page above, the diff check and the build notes are PowerOps's rules. Rule 3 comes from a trial build in which a transparent button was laid over each card to run a formula the card could not, and stayed.
