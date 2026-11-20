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

The look in `look.md` and P1 to P10 of `baseline.md` map to Bootstrap: grid columns for one column on a phone and list with detail wider, `card` for surfaces (`panel` in Bootstrap 3), a badge for status (`label` in Bootstrap 3, `badge rounded-pill` in 5), and `alert` for the error and failed-save states. The Styling workspace sets the palette (13 preset themes; nine colours plus three free slots), the fonts (basic fonts and more than 30 Google Fonts) and button styles. What Styling doesn't set (in the samples: spacing, radius, inputs, tables, badges and the focus outline) goes in one custom CSS file, of at most 1 MB; Learn names no list of what needs CSS, and this one is what the design research's sample build needed. Section margins and padding don't work on sites made from starter templates.

Learn disagrees with itself on the Bootstrap version (seen 2026-11-14): its Bootstrap 5 page says that with the enhanced data model new sites use Bootstrap 5, while the Bootstrap overview and the CSS page still name Bootstrap 3 as the default. This file takes the stricter reading: read the version from the site (section 1), and assume neither. For Bootstrap 5 the exact version isn't on Learn, and its container widths (1140 and 1320 px) are Bootstrap's own.

- **P1.** A `browser` frame 1366 to 1440 px wide at the page's real scroll length, and a `phone` frame 390 px wide. Content sits in a Bootstrap container, a reading or form page in a narrower column.
- **P2 and P7.** The first screen and a signed-in home are studio sections and flex containers; counts for a signed-in visitor need a web template with `fetchxml` (custom).
- **P3.** The header is the default Header web template (its menu from the page hierarchy), restyled in "Edit site header" and CSS, or a web template of your own; the footer a Footer web template with a web link set.
- **P4, P5 and P9.** "The look of a site" below; the sizes, spacing, radius and focus outline go in the custom CSS file.
- **P6.** A multistep form with the Numeric progress indicator ("Step x of n"), the validation summary at the top, a read-only step to check the answers, and a redirect to a confirmation page that reads the reference with Liquid.
- **P8.** The List component, with status pills as custom CSS or a Liquid template; the timeline is a web template (there is no tracker component).
- **P10.** The plan's build route, and the map marks each part that needs custom CSS or a web template.

### The look of a site

**A site is the organisation's public face: correct and calm is not enough.**

- **Mark.** The header carries the organisation's own mark and a wordmark in the heading face. With no logo in the plan, draw a simple mark from the name and say in the map that it stands in.
- **One picture on the first screen.** A wide picture of the service or the place fills the hero behind or beside the headline, drawn in inline SVG in flat shapes from the brand palette, with one detail that carries the story (the sample: a street at dusk, one house fully lit, a pin above it). Never a stock photo under an overlay, a gradient alone, or abstract blobs. The map says the drawing is art direction and a designer or the organisation's photos supply the final image as a web file.
- **Pictograms in the same hand.** Task tiles, the "what happens next" steps and the empty, confirmation and failed-save states each get a small drawing built from the same shapes and colours as the hero picture, not a line icon alone. Look at each one enlarged: if it could be read as something else, redraw it.
- **A deep brand band.** The hero and each inner page's title band use a deep shade of the brand, with white text (at least 7:1) and a light tint for secondary text (at least 4.5:1). The rest of the page is paper and white, so the band is the page's one dark area besides the footer.
- **One accent with one meaning.** A second hue, warm against a cool brand or the reverse, is the one filled button in each view (ink text on it, at least 4.5:1) and the lit detail in the drawings. It is never a status colour, never text on white, never a second button in the same view.
- **Type with presence.** Two web faces: a heading face with character (the sample: Noto Serif 700) and a text face that is not the product default (the sample: Fira Sans). Body 18 px on 28; the home headline 56 to 64 px on at most two lines; inner page titles 40 to 44; sections 32; questions and card titles 22; small text and badges 14. At most five sizes a page plus the headline.
- **Focus on the dark band.** The focus outline is white on the deep band and ink on light surfaces, each at least 3:1 against what it sits on.
- **Layout stays the task's.** None of the above moves the task off the first screen (P2): the headline, the one filled button and the track card are still what a visitor sees first.

The map carries these lines for them: the hero picture and the pictograms as SVG web files placed as custom content (GA mechanics); the faces "from the Styling font list if offered, otherwise loaded in the custom CSS file"; the accent as the colour of the filled button in Styling's button styles; the deep shade and the sizes as custom CSS.

### What a site can build

"Studio" is the design studio; "CSS" is the custom CSS file uploaded in the Styling workspace. Learn pages are under `learn.microsoft.com/power-pages/`. Status as Learn stated it on 2026-11-11, spot-checked 2026-11-14.

| Pattern | Built with | Status | Learn page |
|---|---|---|---|
| Section with columns, background, padding | Studio section: 1, 2 or 3 columns, one third left or right; background colour or image; the style panel sets margins, padding, borders, shadow and radius on sections, text, buttons, images and videos only | GA | `getting-started/add-sections`, `getting-started/customize-pages` |
| Flexible row of cards or tiles | Flex container (direction, justify, gap) with Text, Image and Button; or Bootstrap `card` and grid classes in a web template | GA | `getting-started/add-flex-container` |
| Brand colours, fonts, buttons | Styling workspace: 13 preset themes, nine colours plus three free slots, basic fonts and more than 30 Google Fonts, button styles. Section margins and padding don't work on starter-template sites | GA | `getting-started/style-site`, `known-issues` |
| Spacing scale, radius on cards and inputs, form and table styling, badges, focus outline | One custom CSS file, for styles Styling doesn't provide | GA | `configure/manage-css` |
| Header with logo, navigation, sign-in, search | The default Header web template (menu from the page hierarchy); restyled in "Edit site header" and CSS, or replaced by your own web template | GA | `getting-started/edit-header`, `configure/web-templates` |
| Footer with link columns | A Footer web template with a web link set and the Bootstrap grid; the studio edits only its background and text | GA | `configure/web-templates`, `configure/manage-web-links` |
| Icons | Font Awesome Free 6.2.1 is loaded on a Bootstrap 5 site (seen in a live site's files) | Observed, not on Learn | none |
| Data-driven cards | Card gallery component: 4 layouts; image, title, description, button; search | GA on Learn (its dialog shows a preview badge) | `getting-started/add-card-gallery` |
| List with search and filters | List component (modern: inline filters, infinite scroll, loading shimmer). Status pills are not a list setting: CSS and script, or a Liquid template with `fetchxml` and a Bootstrap `badge` | List GA; pills custom | `getting-started/add-list` |
| Form with sections, hints and errors | Form component from a Dataverse form (tabs, sections, columns); description above or below the field; required and pattern messages; a validation summary at the top with links to the fields | GA | `getting-started/add-form`, `configure/basic-forms` |
| Steps with progress | Multistep form; progress indicator Title, Numeric ("Step x of n") or Progress Bar, at the top, bottom, left or right; fixed markup, restyled in CSS | GA | `getting-started/multistep-forms`, `configure/multistep-form-properties` |
| Check-your-answers step | A further step in read-only mode; its default look: not confirmed | GA parts | `configure/multistep-form-steps` |
| Confirmation page with a reference | Redirect on submit with a record value in the query string, read by Liquid | GA | `configure/basic-forms` |
| File upload with a file list | Form attachments: name, type, size, progress bar, delete | GA | `getting-started/add-form` |
| Status timeline for one item | A web template with `fetchxml` and your own markup; there is no tracker component | Custom | `configure/liquid/template-tags` |
| Count tiles for a signed-in visitor | A web template with `fetchxml` counts and Bootstrap cards; or a Power BI component | Custom | `configure/liquid/template-tags`, `getting-started/add-power-bi` |
| Accordion, alert, breadcrumb, pagination, modal | Bootstrap 5 classes (exact version not on Learn) on a Bootstrap 5 site; Breadcrumb is also a studio component | GA | `configure/bootstrap-version-5`, `getting-started/add-breadcrumb` |
| Anything, freely | A code site: a React, Vue, Angular or Astro app uploaded with the CLI. It loses the design studio, Styling, Liquid, multiple languages and the built-in lists and forms | Not stated on Learn | `configure/create-code-sites` |

Not confirmed for Power Pages: the default look of the sign-in and profile pages, a built-in accessibility checker, an automatic table-to-cards layout on phones, the empty-state text of the modern list, and whether the Styling font list offers a given face (the map's fallback loads it in the custom CSS file).

## Sources

Checked on 2026-11-03:

- [Enhanced data model](https://learn.microsoft.com/power-pages/admin/enhanced-data-model): the default for new sites.
- [Configuring table permissions](https://learn.microsoft.com/power-pages/security/table-permissions): the access types, and Global for all records.
- [Bootstrap overview](https://learn.microsoft.com/power-pages/configure/bootstrap-overview) and [Bootstrap version 5](https://learn.microsoft.com/power-pages/configure/bootstrap-version-5): versions 3.3.6 and 5.
- [Create and deploy a single-page application](https://learn.microsoft.com/power-pages/configure/create-code-sites): managed only through source code and the CLI, with no Git integration.
- [Authentication in Power Pages](https://learn.microsoft.com/power-pages/security/authentication/): local authentication is not recommended.
- [Security best practices](https://learn.microsoft.com/power-pages/security/security-best-practices): no local authentication for production sites.
- [Go-live checklist](https://learn.microsoft.com/power-pages/go-live/checklist): turn off local authentication; consider load testing for thousands of users a month.

Checked on 2026-11-08:

- [Style your site](https://learn.microsoft.com/power-pages/getting-started/style-site): each Styling theme's colour palette, background colour, font styles, button styles and section margins.

Checked on 2026-11-11 by the design-research session, and spot-checked on 2026-11-14 for M97 (its plan's Assumptions A7 and A8):

- [Style your site](https://learn.microsoft.com/power-pages/getting-started/style-site): 13 preset themes, nine colours plus three free slots, basic fonts and more than 30 Google Fonts, button styles. [Manage CSS](https://learn.microsoft.com/power-pages/configure/manage-css): one CSS file up to 1 MB, for styles not provided out of the box. [Known issues](https://learn.microsoft.com/power-pages/known-issues): section margins and padding on starter-template sites.
- [Bootstrap version 5](https://learn.microsoft.com/power-pages/configure/bootstrap-version-5): Bootstrap 5 with the enhanced data model. [Bootstrap overview](https://learn.microsoft.com/power-pages/configure/bootstrap-overview) and [Manage CSS](https://learn.microsoft.com/power-pages/configure/manage-css): Bootstrap 3 still named as the default.
- [Add sections](https://learn.microsoft.com/power-pages/getting-started/add-sections), [Add a flex container](https://learn.microsoft.com/power-pages/getting-started/add-flex-container), [Edit the site header](https://learn.microsoft.com/power-pages/getting-started/edit-header), [Web templates](https://learn.microsoft.com/power-pages/configure/web-templates), [Add a list](https://learn.microsoft.com/power-pages/getting-started/add-list), [Add a form](https://learn.microsoft.com/power-pages/getting-started/add-form), [Multistep forms](https://learn.microsoft.com/power-pages/getting-started/multistep-forms) and [Liquid template tags](https://learn.microsoft.com/power-pages/configure/liquid/template-tags): the rows of the table.
