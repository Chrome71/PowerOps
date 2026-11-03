# Power Pages

Read this when a plan names a Power Pages site. Write the answers in `Users`, `Data`, `Screens` and `Constraints`.

## 1. The plan

- **Site type.** A Bootstrap site built in the design studio, or a code site (a single-page app you build and upload). Say what the choice gives up: a code site is managed only through source code and the CLI, and Power Platform Git integration doesn't support it; a Bootstrap site gives up the free choice of a UI library.
- **Data model.** The enhanced data model only. New sites use it by default; a template that still uses the standard model is out.
- **Table permissions.** One row per table: web role, access type (Global, Contact, Account, Self or Parent), privileges, and the reason. Global gives every row of the table, so use it only for read-only public reference data.
- **Bootstrap.** Read the version from the site (3.3.6 or 5) before any design work. If it isn't known, stop and ask.
- **Sign-in.** No local sign-in (user name and password kept by the site) in production. Learn's security best practices say not to use it for production sites.
- **Load.** A site for thousands of users a month plans a load test before go-live. Learn says to consider one; the plan line is PowerOps's stricter reading.

## 2. The sketch

For a Bootstrap site, write a small hand-written CSS subset that uses Bootstrap's class names, never the bundled stylesheet. For a code site, name the UI library in the header comment. Whether `create-site` uses an outside sketch is untested.

## Sources

Checked on 2026-11-03:

- [Enhanced data model](https://learn.microsoft.com/power-pages/admin/enhanced-data-model): the default for new sites.
- [Configuring table permissions](https://learn.microsoft.com/power-pages/security/table-permissions): the access types, and Global for all records.
- [Bootstrap overview](https://learn.microsoft.com/power-pages/configure/bootstrap-overview) and [Bootstrap version 5](https://learn.microsoft.com/power-pages/configure/bootstrap-version-5): versions 3.3.6 and 5.
- [Create and deploy a single-page application](https://learn.microsoft.com/power-pages/configure/create-code-sites): managed only through source code and the CLI, with no Git integration.
- [Authentication in Power Pages](https://learn.microsoft.com/power-pages/security/authentication/): local authentication is not recommended.
- [Security best practices](https://learn.microsoft.com/power-pages/security/security-best-practices): no local authentication for production sites.
- [Go-live checklist](https://learn.microsoft.com/power-pages/go-live/checklist): turn off local authentication; consider load testing for thousands of users a month.
