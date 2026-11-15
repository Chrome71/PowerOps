# Map: Marrowby Homes repairs, "Task first" (Power Pages)

Route: a classic (Liquid) Power Pages site on Bootstrap 5.2.2, built in the design studio, the Styling workspace,
one custom CSS file and a few web templates. Not a code site. Sources: `../report.md` section 5.2 and
`../notes/pages-capability.md`. `PP/` = `https://learn.microsoft.com/power-pages/`. Story facts (RP-20417, times,
phone numbers, the 6-hour emergency target) follow `_build/STORY.md`.

## Shared chrome (every page)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Skip link "Skip to main content" (drawn focused on Your repairs) | First element in the Header web template, an anchor to the main region; hidden until focused, shown as a white strip by CSS | Custom (header template, CSS) | `PP/configure/web-templates`, `PP/configure/manage-css` |
| Ink top bar: "Emergency repairs: call 0808 157 0142, day or night", Accessibility | Default Header web template with an extra row; the number from a content snippet so it is edited in one place; colours in CSS | Custom (header template, snippet, CSS) | `PP/getting-started/edit-header`, `PP/configure/customize-content-snippets` |
| Header: logo, four links, current link underline | Default Header web template; menu from the page hierarchy; logo via "Edit site header"; the 3 px underline and 72 px height in CSS | GA (look: Custom CSS) | `PP/getting-started/edit-header`, `PP/configure/manage-web-links` |
| Signed out: outline "Sign in"; signed in: profile menu (avatar, name, chevron) | The header template's sign-in link and `profile_nav`; the button look and initials circle in CSS. No call to action in the header on any page | GA (look: Custom CSS) | `PP/configure/web-templates` |
| Logo mark (M with two roof gables) in a 36 px brand square | An SVG web file uploaded as the header logo ("Edit site header", logo plus alt text "Marrowby Homes"); the same file in the footer | GA | `PP/getting-started/edit-header`, `PP/configure/web-files` |
| Breadcrumb | Breadcrumb component (enhanced data model), or `{% include 'breadcrumbs' %}` | GA | `PP/getting-started/add-breadcrumb` |
| Footer: logo line, three link columns, base line | Footer web template with a web link set and the Bootstrap grid | GA (layout in code) | `PP/configure/web-templates`, `PP/configure/manage-web-links` |
| Focus outline (3 px ink, 2 px offset): first tile (Home), Continue (form), skip link (Your repairs) | CSS `:focus-visible` rule | Custom (CSS) | `PP/configure/manage-css` |

## Home

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Hero band on brand tint, two columns | Studio section, 2 Column, background colour from the palette; padding with the style panel | GA | `PP/getting-started/add-sections`, `PP/getting-started/customize-pages` |
| Label, serif h1, intro paragraph | Text component (Subheading, Title, Paragraph styles); font and sizes from Styling and CSS | GA | `PP/getting-started/add-text` |
| Report a repair (the one filled button) | Button component, primary style from Styling; 44 px height in CSS | GA | `PP/getting-started/add-button` |
| Track card: reference and postcode on one row with Track, hint under; overlaps the hero edge by 48 px with the soft shadow | Web template with a small script calling the Web API, or a page with a form; overlap by a negative margin in CSS. See "Who sees what" for the anonymous exposure | Custom | `PP/configure/web-api-overview`, `PP/configure/web-templates` |
| Track card preview: "Example" RP-20417, Scheduled badge, horizontal five-step tracker, "Visit booked: Today, 13:00 to 14:30" | Web template with Liquid holding static example content (no data read); the tracker is the same CSS as the Your repairs timeline, laid out in a row | Custom (web template, CSS) | `PP/configure/web-templates` |
| Emergency panel with the gas line 0808 157 0199 and the repairs line 0808 157 0142 | Bootstrap `alert` markup in a content snippet; the 4 px left bar in CSS | Custom (CSS) | `PP/configure/bootstrap-version-5`, `PP/configure/customize-content-snippets` |
| Six task tiles with icon, title, line, chevron | Static content in a 3 Column section, or a web template using Bootstrap `card` and grid classes; radius, stroke and icon tile in CSS | GA (look: Custom CSS) | `PP/getting-started/add-flex-container`, `PP/configure/web-templates` |
| "What happens after you report", four steps on the page tone | One third left section with Text; the numbered list and connecting line are CSS on an ordered list | Custom (CSS) | `PP/getting-started/add-sections` |
| "How we are doing": three 48 px figures, bullet bars, signed differences | Web template with `fetchxml` over a performance table, or a Power BI component. The bullet bars are inline SVG in that template | Custom (or Power BI component GA) | `PP/configure/liquid/template-tags`, `PP/getting-started/add-power-bi` |

## Report a repair (step 2 of 5)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Five steps, one topic each: Your home, The problem, Who is at home, Visit time, Check and send | Multistep form, one step per Dataverse form; the Back button is a step setting. Step 4 skips to "Today, as soon as we can" when step 3 says someone is at risk: a condition step, set in the Portal Management app | GA | `PP/getting-started/multistep-forms`, `PP/configure/multistep-form-steps` |
| "Step 2 of 5" with five segments and names | Progress indicator, type Numeric, position Top; its markup is fixed, the segments and names are restyled in CSS | GA (look: Custom CSS) | `PP/configure/multistep-form-properties` |
| Questions with 20 px labels and hints | Dataverse form fields; the hint is the field description placed below the label | GA | `PP/getting-started/add-form` |
| "What needs repairing?" as a 3 by 2 grid of radio boxes | Choice field with the radio list control style; the two-column grid, boxes and selected tint in CSS | GA (look: Custom CSS) | `PP/configure/configure-basic-form-metadata` |
| Textarea with characters left | Multiline text field; the "characters left" counter needs form script | GA field; counter Custom (script) | `PP/configure/client-api/` (status not checked) |
| Room select | Choice field rendered as a drop-down | GA | `PP/configure/configure-basic-form-metadata` |
| Photo upload with file row and progress | Form attachments with the new upload UI (name, size, progress bar, delete); 10 MB limit set on the form | GA | `PP/getting-started/add-form` |
| Back and Continue | Wizard Previous and Next buttons, CSS class per step | GA | `PP/configure/load-form-step` |
| Aside: Your home ("We will text the mobile ending 123"), Need it sooner? (gas 0808 157 0199; flood or no power 0808 157 0142; no heating with someone at risk: carry on with the form), help links, What happens next (three numbered lines) | One third right section: Text components and a web link set; "Your home" values via Liquid from step 1 (last three digits only) | GA (Your home values: Custom) | `PP/getting-started/add-sections`, `PP/configure/manage-web-links` |

## Your repairs (signed in)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Title row with greeting and the filled "Report a repair" | Text with the Liquid `user` value; Button component (primary) | GA | `PP/getting-started/add-text`, `PP/getting-started/add-button` |
| Next visit tile: "Today, 13:00 to 14:30" at 40 px, contractor, "Cannot be in? Call 0808 157 0142." | Web template with `fetchxml` on the visit row; "Today" is a Liquid date comparison; the call line comes from the same content snippet as the top bar | Custom | `PP/configure/liquid/template-tags` |
| Open repairs 3, Completed this year 5, with links | Web template with `fetchxml` counts and Bootstrap cards; the links go to the list with its status filter set | Custom | `PP/configure/liquid/template-tags` |
| List with search, status filter, "Showing 1 to 6 of 9", pagination | List component: search on, a Dropdown filter on status; numbered pages come from the classic list (the modern list scrolls instead) | GA | `PP/getting-started/add-list`, `PP/configure/list-configuration` |
| Status pills in the list | Not a list setting: CSS and script on the list, or a Liquid `fetchxml` template with Bootstrap `badge` | Custom | `PP/getting-started/add-list` |
| Selected row (tint and 3 px bar) | Script that marks the row the tracker shows, plus CSS | Custom | `PP/configure/manage-css` |
| Tracker: reference, title, badge, "Emergency: we attend within 6 hours, by 14:30 today", five-step timeline, "Cannot be in? Call 0808 157 0142." | Web template with `fetchxml` and own markup; there is no tracker component | Custom | `PP/configure/liquid/template-tags` |
| "View repair details" (tracker) and "View" (list rows) | The list's View details action, opening a details page with a read-only form | GA | `PP/configure/list-configuration` |
| Changing a visit (not drawn) | An emergency visit cannot be moved online, so the drawn repair shows the phone line instead. For non-emergency visits only: a link to a basic form in edit mode on the visit row, offering the slots the repairs desk allows; needs a table permission limited to the tenant's own visits | Custom | `PP/configure/basic-forms`, `PP/security/table-permissions` (not read) |

## Phones (390 wide)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Collapsed header with menu button | Bootstrap navbar toggle in the default header | GA (toggle observed) | `PP/configure/bootstrap-version-5` |
| Stacked hero; the phone keeps "Track a repair" (outline) because the track card is off-screen there; it scrolls to the card | Section columns stack under the Bootstrap breakpoints; full width in a CSS media query | GA (widths: Custom CSS) | `PP/getting-started/add-sections` |
| Repairs as cards | Not a list setting: CSS media queries on the list table, or a Card gallery, or the `fetchxml` template | Custom | `PP/getting-started/add-card-gallery` |
| Docked "Report a repair" with a top stroke and soft shadow | CSS `position:fixed` on the button below the small breakpoint | Custom (CSS) | `PP/configure/manage-css` |

## States

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Error summary with two links ("Tell us what is wrong", "Choose the room"), the same words at the fields | The form's validation summary (class `validation-summary alert alert-error alert-block`, links to fields on). Its default link text "click here" must be changed so each link names the field | GA (look and link text: Custom) | `PP/configure/basic-forms` |
| Message at the field, red bar and border | Required-field messages from the form metadata; bar and border in CSS | GA (look: Custom CSS) | `PP/configure/configure-basic-form-metadata` |
| Empty list | Classic list Empty message (HTML). The modern list's empty state: not confirmed | GA (classic) / Not confirmed (modern) | `PP/configure/list-configuration` |
| Loading list | Modern list shimmer, or classic list Loading message; the sketch's skeleton rows stand for the shimmer | GA | `PP/getting-started/add-list` |
| Failed save panel with Try again and 0808 157 0142 | Not a documented form state: a generic error comes from the `Portal Generic Error` snippet; the panel with two buttons is custom script and markup | Custom | `PP/configure/customize-content-snippets` |
| Check your answers (six rows, Room included; visit "Today, as soon as we can. We will call you first.") | A further multistep step in read-only mode loading the record; its default look is not confirmed, the summary rows are CSS | GA parts; look Not confirmed | `PP/configure/multistep-form-steps` |
| Confirmation with RP-20417 and the emergency line | On submit, redirect to a page with the record value in the query string, read by Liquid; the emergency sentence shows when the risk answer is Yes (Liquid condition) | GA (condition: Custom) | `PP/configure/basic-forms` |

## Icons: Font Awesome names for the built site

The built site uses Font Awesome Free 6.2.1, loaded on Bootstrap 5 sites (observed on a live site's files, not
stated on Learn). The sketch draws Fluent icons as stand-ins of the same meaning. Names to verify against the 6.2.1
free set.

| Drawn (Fluent) | Where | Font Awesome |
|---|---|---|
| call | top bar, Need it sooner? | `phone` |
| warning | emergency panel | `triangle-exclamation` |
| info | "no account needed" line | `circle-info` |
| person | Sign in | `user` |
| fire | Heating and hot water | `fire-flame-simple` |
| drop | Leaks and plumbing | `droplet` |
| flash | Electrics and power | `bolt` |
| door | Doors, windows and locks | `door-closed` |
| water | Damp and mould | `water` (or `house-flood-water`) |
| wrench | Something else, Open repairs, empty state | `wrench` |
| chev-right, chev-down | tiles, selects, profile menu | `chevron-right`, `chevron-down` |
| arrow-right, arrow-left | buttons, pagination | `arrow-right`, `arrow-left` |
| arrow-up, arrow-down | How we are doing | `arrow-up`, `arrow-down` |
| check | progress, timeline | `check` |
| check-circle | Completed this year, Uploaded, confirmation | `circle-check` |
| error | error summary, field errors, failed save | `circle-exclamation` |
| upload | Upload a photo | `upload` |
| image | file row | `image` |
| add | Report a repair | `plus` |
| calendar-clock | Next visit | `calendar-day` |
| search | list search | `magnifying-glass` |
| menu | phone header | `bars` |
| send | Send repair report | `paper-plane` |

## Who sees what

| Screen | Role | Rows they see | Sensitive data on it |
|---|---|---|---|
| Home | Anyone, signed out | Performance figures (no personal rows). The track card shows one repair's status, problem title and visit time to anyone who enters a matching reference and postcode | Anonymous tracking: reference plus postcode reveals that a home has an open repair, what it is and when a contractor visits (a burglary and safety risk). Needs an anonymous table permission or a script endpoint limited to those fields, rate limiting, and a security review. Fallback: "Track" asks for sign-in and shows only the tenant's own repairs |
| Report a repair | Anyone, signed out (sign-in optional) | Only the report being created in this session | The household's vulnerability ("Who is at home"), the tenant's words and photos, the address, the mobile's last three digits. The create permission must not allow reading other reports |
| Your repairs (desktop and phone) | Signed-in tenant (web role Tenant) | Repairs whose home is the tenant's tenancy (Contact to Account or tenancy scope); visits for those repairs only | Name, address, contractor name, visit times, the emergency flag |
| Change the visit (non-emergency, not drawn) | Signed-in tenant | The visit row of their own repair | Visit time only; write limited to the slot field |
| States (error, check answers, confirmation) | As the form above | The same session's record | Check answers repeats the vulnerability answer and the full description |

## Preview or unsupported parts and the GA fallback for each

- Font Awesome 6.2.1 is observed, not stated on Learn. Fallback: inline SVG icons in the web templates (SVG is served by the site).
- Custom component (web template with `{% manifest %}` added from the studio) is Preview; the sketch does not need it. Fallback: include the web templates in page copy.
- Card gallery shows a Preview badge in its dialog although Learn has no banner. The sketch uses the list with CSS on phones instead.
- Editing page HTML in VS Code for the Web is Preview. Fallback: desktop VS Code with `pac pages download` and `upload`.
- The anonymous track card: see "Who sees what"; fallback sign-in only.

## Theme

- Styling workspace palette (12 slots): brand `--brand` #0B6A75, brand hover `--brand-hover` #095861, ink `--ink` #0E2A30 (top bar and footer), page `--page` #F6F5F2, white `--surface`, text `--fg1` #242424, secondary text `--fg3` #616161, brand tint `--brand-tint` #E4F1F2 (hero background), stroke `--stroke` #E0E0E0. Three slots stay free.
- Styling workspace buttons: primary filled `--brand` with white text; secondary white with a `--stroke-input` border and `--fg1` text. Radius 4.
- Fonts: headings (h1, h2) in Noto Serif 600, a Google Font; whether the Styling workspace's font list has it is not confirmed (the list is not on Learn). Fallback: load it in the custom CSS file, or pick another serif from Styling's list. Body text, labels and figures in a sans (the sketch renders in the system UI sans).
- One custom CSS file: type sizes (Home: 48 display, 28, 20, 16, 14; other pages: 40 display, 28, 20, 16, 14; badges 24 px high with 14 px text), the spacing scale (4 to 64), card radius 8 and 1 px stroke, 44 px inputs and controls, radio boxes, the progress segments, error bars, table and badge styles (status pairs `--pos`, `--warn`, `--crit`, info on brand tint, `--neu`), the timeline, the tiles, the steps list, the skip link, the focus outline, the phone card layout and the docked button.

## Not drawn

- Steps 1, 3 and 4 of the form (Your home, Who is at home, Visit time): same pattern as step 2; step 5 is shown as a state.
- Sign-in and profile pages: their default look is not confirmed on Learn.
- The visit-change form (non-emergency visits only) and the repair details page behind "View".
- Hover states and the header's mobile menu open.
- The rest of the phone pages below the fold: the Home phone cuts off like a real phone after a whole tile.
- A cookie banner: not built in to Power Pages; essential cookies only on this site.
