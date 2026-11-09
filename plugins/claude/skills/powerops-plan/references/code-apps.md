# Code Apps

Read this when a plan names a code app. Write the answers in `Users`, `Data`, `Screens`, `Environments and route`, `Solution and publisher` and `Constraints`.

## 1. The plan

- **The route.** Quote the Code App line of `route.md` and say how each of its four conditions holds. If one doesn't, a form, a page or a canvas app is the route.
- **Licences.** Every user signs in with Microsoft Entra ID and needs a qualifying licence (Power Apps Premium, pay-as-you-go, an App Pass or auto-claim). Name which.
- **The admin setting.** An admin turns on code apps for the environment. Confirm it, or put it under Questions for your admin.
- **Data sources.** Each one the app reads or writes, and whether it is premium.
- **The solution.** The target solution by its id, which the first push takes as `--solution-id`, so the app lands in that solution.
- **Content security policy.** It is set per environment. List each exception the app needs, with its reason.
- **Source control.** Code apps don't support Power Platform Git integration, so the app's source lives in the team's own repository and CI, and the solution carries the pushed app.
- **Nothing sensitive in app assets.** They are served from an endpoint anyone can reach, so data belongs in a data source.
- **The first push.** Microsoft's `create-code-app` ends with a baseline `pa app push` that it treats as approved. It is a write: it needs the user's yes, and goes to Dev only.

## 2. Instead of a sketch: a mock-data first slice

The real UI is HTML, so a sketch would be rebuilt. Make Step 1 of the build a first slice with hand-written mock data under plain `npm run dev`, with no push and no data source, and record it in `## Log`. This is read from Microsoft's sample code, not a documented path, and Local Play still needs a tenant.

## Sources

Checked on 2026-11-03:

- [Power Apps code apps overview](https://learn.microsoft.com/power-apps/developer/code-apps/overview): the admin setting, Entra sign-in and licences.
- [Configure content security policy](https://learn.microsoft.com/power-apps/developer/code-apps/how-to/content-security-policy): per environment.
- [System configuration](https://learn.microsoft.com/power-apps/developer/code-apps/system-limits-configuration): app assets on a public endpoint; keep sensitive data out of the app.
- [Power Apps CLI command reference](https://learn.microsoft.com/power-apps/developer/code-apps/reference/cli): `pa app push`.
- [code-apps plugin](https://github.com/microsoft/power-platform-skills/tree/96d16d0/plugins/code-apps) (MIT) at `96d16d0`: `create-code-app` and its baseline push.

Checked on 2026-11-05:

- [Power Apps code apps overview](https://learn.microsoft.com/power-apps/developer/code-apps/overview): no Power Platform Git integration. The page carries no preview label and sends bugs to standard Microsoft support, but no Learn page read states a general availability date.
