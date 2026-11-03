# Model-driven apps

Read this when a plan names a model-driven app, a custom page, a generative page or a code component (PCF). Write the answers in `Users`, `Screens` and `Steps`.

## 1. The plan

- **Screens.** List each form with its tabs and sections, each view with its columns, and each page with its type (form, view, dashboard, custom page, generative page) and the reason for that type (`route.md`).
- **Removals.** Plan each removal as a step of its own. Taking a form, view or page out of a spec doesn't take it out of an environment: in one public version of `app-builder`, a rebuild didn't remove what the spec dropped, and in Test and Prod only a managed upgrade removes a component.
- **Phones and tablets.** Only when the plan names them: Learn limits a main form on the phone and tablet apps to 5 tabs or 75 columns and 10 subgrids. Write the counts beside each form that exceeds them, and the reason.
- **Roles.** For each persona, name its security roles, and check that the roles together read every table its tasks use. Roles may split access between personas on purpose: say so.
- **Dashboards.** At most six components, Learn's default for a dashboard (the on-premises version of the page lets a setting raise it to 20). More needs a reason in the plan.
- **Generative pages.** Name the tables and the page's question. A sketch goes in as a screenshot the user attaches in the page builder.
- **Code components.** Only for a reusable field or dataset control. Microsoft's `pcf` skill deploys with `pac pcf push`, a write to Dev that needs the user's yes.

## 2. Microsoft's gates

`app-builder` writes a spec and asks for approval, `genpage` has its own planner, and both print a preview. They come after this plan's sign-off. Add no third gate.

## Sources

Checked on 2026-11-03:

- [How main forms appear on different devices](https://learn.microsoft.com/power-apps/maker/model-driven-apps/main-form-presentations): 5 tabs or 75 columns and 10 subgrids, on phones and tablets.
- [Dashboard components and FormXML](https://learn.microsoft.com/power-apps/developer/model-driven-apps/understand-dashboards-dashboard-components-formxml): six components by default.
- [Share a model-driven app](https://learn.microsoft.com/power-apps/maker/model-driven-apps/share-model-driven-app): sharing through security roles.
- [Generate a page using natural language](https://learn.microsoft.com/power-apps/maker/model-driven-apps/generative-pages): attached images.
- [Overview of custom pages](https://learn.microsoft.com/power-apps/maker/model-driven-apps/model-app-page-overview).
- [Upgrade or update a solution](https://learn.microsoft.com/power-apps/maker/data-platform/update-solutions): an upgrade removes components the new version doesn't hold.
- [pac pcf](https://learn.microsoft.com/power-platform/developer/cli/reference/pcf): `push` and the `field` and `dataset` templates.
- [model-apps plugin](https://github.com/microsoft/power-platform-skills/tree/3fb3604b3a7e29e75a5250f1fd574e276b2ba88f/plugins/model-apps) (MIT) at `3fb3604`: the rebuild that kept dropped artefacts, read in source and not run. [pcf plugin](https://github.com/microsoft/power-platform-skills/tree/96d16d0/plugins/pcf) (MIT) at `96d16d0`: `pac pcf push`.
