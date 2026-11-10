# Model-driven apps

Read this when a plan names a model-driven app, a custom page, a generative page or a code component (PCF). Write the answers in `Users`, `Screens` and `Steps`.

## 1. The plan

- **Screens.** List each form with its tabs and sections, each view with its columns, and each page with its type (form, view, dashboard, custom page, generative page) and the reason for that type (`route.md`).
- **Removals.** Plan each removal as a step of its own. Taking a form, view or page out of a spec doesn't take it out of an environment: in one public version of `app-builder`, a rebuild didn't remove what the spec dropped, and in Test and Prod only a managed upgrade removes a component.
- **Phones and tablets.** Only when the plan names them: Learn limits a main form on the phone and tablet apps to 5 tabs or 75 columns and 10 subgrids. Write the counts beside each form that exceeds them, and the reason.
- **Roles.** For each persona, name its security roles, and check that the roles together read every table its tasks use. Roles may split access between personas on purpose: say so.
- **Dashboards.** At most six components (charts, grids, IFRAMEs, web resources) on a standard dashboard, Learn's default (the on-premises version of the page lets a setting raise it to 20). More needs a reason in the plan.
- **Generative pages.** Name the tables and the page's question. A sketch goes in as a screenshot the user attaches in the page builder. Learn's release plans list generative pages as generally available in US environments from 2025-10-30 and globally from 2026-04-02. The page builder in Power Apps runs only in the regions Learn lists; elsewhere AI code generation tools build the pages, so a plan names which one. Connector data and custom APIs in them are still preview: a page that needs either says so under Constraints. A page made during the preview goes into a solution only after it has been opened once in the app designer, which migrates it.
- **Theme and look.** Forms, views and the site map take their look from the modern theme alone: a `CustomTheme` XML web resource with the seed, font, logo (156 by 48 px) and `AppHeaderColors` pairs at 4.5:1 in rest, hover, pressed and selected, named by the Custom theme definition setting. It needs the New look, and some areas, such as legacy grids and focus view, don't use it yet. The look in `look.md` applies in full to what the maker lays out: custom pages, generative pages and code components. On forms and views it becomes the status in the form header and in each view, and tabs, sections and columns in the order of the task. The new look doesn't honour classic themes, and Learn makes it mandatory from 2026 release wave 1. Microsoft's `app-builder` (model-apps 2.21.0 or later) writes the theme from `app.theme` in its spec (preview), as a web resource and the app's theme setting, both added to the solution. Take the values from the plan's Theme block (`design.md`).
- **Code components.** Only for a reusable field or dataset control. Space them with the platform's Fluent tokens, not fixed pixels: `spacingVerticalS`, `spacingVerticalM`, `spacingVerticalL` and `spacingHorizontalL` follow display density. Microsoft's `pcf` skill deploys with `pac pcf push`, a write to Dev that needs the user's yes.

## 2. Microsoft's gates

`app-builder` writes a spec and asks for approval, `genpage` has its own planner, and both print a preview. They come after this plan's sign-off. Add no third gate.

## Sources

Checked on 2026-11-03:

- [How main forms appear on different devices](https://learn.microsoft.com/power-apps/maker/model-driven-apps/main-form-presentations): 5 tabs or 75 columns and 10 subgrids, on phones and tablets.
- [Dashboard components and FormXML](https://learn.microsoft.com/power-apps/developer/model-driven-apps/understand-dashboards-dashboard-components-formxml): six components on a standard dashboard by default.
- [Share a model-driven app](https://learn.microsoft.com/power-apps/maker/model-driven-apps/share-model-driven-app): sharing through security roles.
- [Generate a page using natural language](https://learn.microsoft.com/power-apps/maker/model-driven-apps/generative-pages): attached images.
- [Overview of custom pages](https://learn.microsoft.com/power-apps/maker/model-driven-apps/model-app-page-overview).
- [Upgrade or update a solution](https://learn.microsoft.com/power-apps/maker/data-platform/update-solutions): an upgrade removes components the new version doesn't hold.
- [pac pcf](https://learn.microsoft.com/power-platform/developer/cli/reference/pcf): `push` and the `field` and `dataset` templates.
- [model-apps plugin](https://github.com/microsoft/power-platform-skills/tree/3fb3604b3a7e29e75a5250f1fd574e276b2ba88f/plugins/model-apps) (MIT) at `3fb3604`: the rebuild that kept dropped artefacts, read in source and not run. [pcf plugin](https://github.com/microsoft/power-platform-skills/tree/96d16d0/plugins/pcf) (MIT) at `96d16d0`: `pac pcf push`.

Checked on 2026-11-05:

- [Generate a page using natural language](https://learn.microsoft.com/power-apps/maker/model-driven-apps/generative-pages): connectors and custom APIs in preview, the one-time migration of a page made in the preview, and the page builder's regions with AI code generation tools elsewhere.
- [Generate model-driven app pages using natural language](https://learn.microsoft.com/power-platform/release-plan/2025wave2/power-apps/generate-model-driven-app-pages-using-natural-language) (release plan, archived): generally available on 2025-10-30, in US environments only. [What's new in Power Apps, 2026 release wave 1](https://learn.microsoft.com/power-platform/release-plan/2026wave1/power-apps/planned-features): "Use generative pages globally" generally available on 2026-04-02.
- [Modern theme overrides](https://learn.microsoft.com/power-apps/maker/model-driven-apps/modern-theme-overrides): the `CustomTheme` web resource, the Custom theme definition setting, and classic theming not honoured in the new look.
- [Modern, refreshed look](https://learn.microsoft.com/power-apps/user/modern-fluent-design): mandatory with 2026 release wave 1 (its Revert section still names the admin setting).
- [model-apps plugin](https://github.com/microsoft/power-platform-skills/tree/4076781a3c6862896ad98bd51f32e180d9ddd4fd/plugins/model-apps) (MIT) at `4076781`: `app.theme` in `references/app-spec-schema.md`.

Checked on 2026-11-08:

- [Use modern themes in model-driven apps](https://learn.microsoft.com/power-apps/maker/model-driven-apps/modern-theme-overrides): the `CustomTheme` attributes, the 156 by 48 px logo, `AppHeaderColors` and their 4.5:1, the Custom theme definition setting, the New look, and the areas that don't use the theme yet.
- [Style components with modern theming](https://learn.microsoft.com/power-apps/developer/component-framework/fluent-modern-theming): the four spacing tokens that follow display density.
