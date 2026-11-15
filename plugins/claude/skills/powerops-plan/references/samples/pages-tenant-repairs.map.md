# Map: Marrowby Homes repairs, "Task first" in "Lit windows" (Power Pages)

Route: a classic (Liquid) Power Pages site on Bootstrap 5.2.2, built in the design studio, the Styling workspace,
one custom CSS file and a few web templates. Not a code site. Sources: `../report.md` section 5.2 and
`../notes/pages-capability.md`. `PP/` = `https://learn.microsoft.com/power-pages/`. Story facts (RP-20417, times,
phone numbers, the 6-hour emergency target) follow `_build/STORY.md`.

## Shared chrome (every page)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Skip link "Skip to main content" (drawn focused on Your repairs) | First element in the Header web template, an anchor to the main region; hidden until focused, shown as a white strip by CSS | Custom (header template, CSS) | `PP/configure/web-templates`, `PP/configure/manage-css` |
| Ink top bar: "Emergency repairs: call 0808 157 0142, day or night", Accessibility | Default Header web template with an extra row; the number from a content snippet so it is edited in one place; colours in CSS | Custom (header template, snippet, CSS) | `PP/getting-started/edit-header`, `PP/configure/customize-content-snippets` |
| Header: logo, Noto Serif wordmark, four links, current link underline | Default Header web template; menu from the page hierarchy; logo via "Edit site header"; the 4 px underline, 80 px height and serif wordmark in CSS | GA (look: Custom CSS) | `PP/getting-started/edit-header`, `PP/configure/manage-web-links` |
| Signed out: outline "Sign in"; signed in: profile menu (avatar, name, chevron) | The header template's sign-in link and `profile_nav`; the button look and initials circle in CSS (initials 14 px where no name is beside them: the phone header). No call to action in the header on any page | GA (look: Custom CSS) | `PP/configure/web-templates` |
| Logo mark (M with two roof gables) in a 40 px brand square | An SVG web file uploaded as the header logo ("Edit site header", logo plus alt text "Marrowby Homes"); the same file in the footer | GA | `PP/getting-started/edit-header`, `PP/configure/web-files` |
| Inner-page title band (Report a repair, Your repairs): deep petrol, 160 px, breadcrumb in `#CBE4E7`, white serif h1, three houses at the far right, clean straight bottom edge | Studio section with a background colour from the palette; the houses are an image component (SVG web file) aligned right | GA section; houses Custom (content), GA mechanics | `PP/getting-started/add-sections`, `PP/configure/web-files`, `PP/configure/manage-css` |
| Breadcrumb (inside the title band) | Breadcrumb component (enhanced data model), or `{% include 'breadcrumbs' %}`; light colour on deep in CSS | GA (look: Custom CSS) | `PP/getting-started/add-breadcrumb` |
| Footer: straight top edge, logo line, three link columns, base line, faint street silhouette along the bottom | Footer web template with a web link set and the Bootstrap grid; the silhouette is a CSS background on the footer (see Imagery) | GA (layout in code); drawings Custom (CSS) | `PP/configure/web-templates`, `PP/configure/manage-web-links` |
| Focus outline, 3 px, 2 px offset: ink `#0E2A30` on light surfaces (15.1:1 on white, 13.8:1 on paper), white on the deep bands, top bar and footer (9.2:1 on deep `#0A4F58`, 15.1:1 on ink), because ink and brand reach only 1.6:1 and 1.5:1 on deep. Drawn once per frame: the amber Report a repair in the hero (white ring, Home), the amber Continue (ink ring, form), the skip link (ink ring, Your repairs) | Two CSS rules: the `:focus-visible` outline, and one rule that sets it to white inside the deep sections | Custom (CSS) | `PP/configure/manage-css` |

## Home

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Hero band, deep petrol, 600 px (the street stands on a ground strip; the bottom 40 px sit under the track card) | Studio section, 2 Column, background colour (deep petrol) from the palette; fixed height and padding in CSS | GA (height: Custom CSS) | `PP/getting-started/add-sections`, `PP/getting-started/customize-pages` |
| Street at dusk: nine houses along the bottom, lit windows, the fully lit house with the amber pin and dotted line, van with the M mark, moon and stars | One SVG web file as the section's background image, anchored bottom, no repeat (see Imagery); decorative, no alt text | Custom (content), GA mechanics | `PP/getting-started/add-sections`, `PP/configure/web-files` |
| `#CBE4E7` label, 64 px white serif h1 on two lines, `#CBE4E7` intro | Text component (Subheading, Title, Paragraph styles); font, size and colours from Styling and CSS | GA (sizes: Custom CSS) | `PP/getting-started/add-text` |
| Report a repair (the one filled button, amber, 52 px) | Button component, primary style from Styling; 52 px height in CSS | GA | `PP/getting-started/add-button` |
| Track card: reference and postcode on one row with Track, hint under; a wide card in the container under the hero, overlapping its bottom edge by 40 px (only the ground strip) with the soft shadow; the Example preview sits at its right after a vertical stroke | Web template with a small script calling the Web API, or a page with a form; overlap by a negative margin in CSS. See "Who sees what" for the anonymous exposure | Custom | `PP/configure/web-api-overview`, `PP/configure/web-templates` |
| Track card preview: "Example" RP-20417, Scheduled badge, horizontal five-step tracker, "Visit booked: Today, 13:00 to 14:30" | Web template with Liquid holding static example content (no data read); the tracker is the same CSS as the Your repairs timeline, laid out in a row | Custom (web template, CSS) | `PP/configure/web-templates` |
| Emergency panel with the gas line 0808 157 0199 and the repairs line 0808 157 0142 | Bootstrap `alert` markup in a content snippet; the 4 px left bar in CSS | Custom (CSS) | `PP/configure/bootstrap-version-5`, `PP/configure/customize-content-snippets` |
| Six task tiles: 64 px pictogram, title, line, chevron | Static content in a 3 Column section, or a web template using Bootstrap `card` and grid classes; each pictogram an image component (SVG web file); radius and stroke in CSS | GA (look: Custom CSS); pictograms Custom (content), GA mechanics | `PP/getting-started/add-flex-container`, `PP/configure/web-files` |
| "What happens after you report": full-width tint band, four left-aligned steps in a row, pictogram above, numbered petrol circles with white numbers, joined by a dashed petrol line | Studio section with the tint background; four columns of image (SVG web file) and Text; the numbered circles and dashed line are CSS on an ordered list | GA section; circles and line Custom (CSS); pictograms Custom (content) | `PP/getting-started/add-sections`, `PP/configure/manage-css` |
| "How we are doing": three 56 px figures in white cards on the paper tone, thin bullet bars, signed differences | Web template with `fetchxml` over a performance table, or a Power BI component. The bullet bars are inline SVG in that template | Custom (or Power BI component GA) | `PP/configure/liquid/template-tags`, `PP/getting-started/add-power-bi` |

## Report a repair (step 2 of 5)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Five steps, one topic each: Your home, The problem, Who is at home, Visit time, Check and send | Multistep form, one step per Dataverse form; the Back button is a step setting. Step 4 skips to "Today, as soon as we can" when step 3 says someone is at risk: a condition step, set in the Portal Management app | GA | `PP/getting-started/multistep-forms`, `PP/configure/multistep-form-steps` |
| "Step 2 of 5" with five segments and names | Progress indicator, type Numeric, position Top; its markup is fixed, the segments and names are restyled in CSS | GA (look: Custom CSS) | `PP/configure/multistep-form-properties` |
| Questions with 22 px labels and hints | Dataverse form fields; the hint is the field description placed below the label | GA | `PP/getting-started/add-form` |
| "What needs repairing?" as a 3 by 2 grid of radio boxes | Choice field with the radio list control style; the two-column grid, boxes and the selected 2 px petrol border in CSS | GA (look: Custom CSS) | `PP/configure/configure-basic-form-metadata` |
| Textarea with characters left | Multiline text field; the "characters left" counter needs form script | GA field; counter Custom (script) | `PP/configure/client-api/` (status not checked) |
| Room select | Choice field rendered as a drop-down | GA | `PP/configure/configure-basic-form-metadata` |
| Photo upload with file row and progress | Form attachments with the new upload UI (name, size, progress bar, delete); 10 MB limit set on the form | GA | `PP/getting-started/add-form` |
| Back and Continue | Wizard Previous and Next buttons, CSS class per step | GA | `PP/configure/load-form-step` |
| Aside: Your home ("We will text the mobile ending 123"), Need it sooner? (gas 0808 157 0199; flood or no power 0808 157 0142; no heating with someone at risk: carry on with the form), help links, What happens next (three numbered lines) | One third right section: Text components and a web link set; "Your home" values via Liquid from step 1 (last three digits only) | GA (Your home values: Custom) | `PP/getting-started/add-sections`, `PP/configure/manage-web-links` |

## Your repairs (signed in)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Title band with greeting and the amber "Report a repair" at the right | Text with the Liquid `user` value in the title band section; Button component (primary) | GA | `PP/getting-started/add-text`, `PP/getting-started/add-button` |
| Next visit tile: "Today, 13:00 to 14:30" at 44 px, contractor, "Cannot be in? Call 0808 157 0142." | Web template with `fetchxml` on the visit row; "Today" is a Liquid date comparison; the call line comes from the same content snippet as the top bar | Custom | `PP/configure/liquid/template-tags` |
| Open repairs 3, Completed this year 5, with links | Web template with `fetchxml` counts and Bootstrap cards; the links go to the list with its status filter set | Custom | `PP/configure/liquid/template-tags` |
| List with search, status filter, "Showing 1 to 6 of 9", pagination; the Problem column takes the spare width so every row wraps to at most two lines | List component: search on, a Dropdown filter on status; numbered pages come from the classic list (the modern list scrolls instead) | GA | `PP/getting-started/add-list`, `PP/configure/list-configuration` |
| Status pills in the list | Not a list setting: CSS and script on the list, or a Liquid `fetchxml` template with Bootstrap `badge` | Custom | `PP/getting-started/add-list` |
| Selected row (3 px bar; no tint, which the Scheduled badge uses) | Script that marks the row the tracker shows, plus CSS | Custom | `PP/configure/manage-css` |
| Tracker: reference, title, badge, "Emergency: we attend within 6 hours, by 14:30 today", five-step timeline, "Cannot be in? Call 0808 157 0142." | Web template with `fetchxml` and own markup; there is no tracker component | Custom | `PP/configure/liquid/template-tags` |
| "View repair details" (tracker) and "View" (list rows) | The list's View details action, opening a details page with a read-only form | GA | `PP/configure/list-configuration` |
| Changing a visit (not drawn) | An emergency visit cannot be moved online, so the drawn repair shows the phone line instead. For non-emergency visits only: a link to a basic form in edit mode on the visit row, offering the slots the repairs desk allows; needs a table permission limited to the tenant's own visits | Custom | `PP/configure/basic-forms`, `PP/security/table-permissions` (not read) |

## Phones (390 wide)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Collapsed header with menu button | Bootstrap navbar toggle in the default header | GA (toggle observed) | `PP/configure/bootstrap-version-5` |
| Stacked deep hero, 36 px h1, full-width amber button; "Track a repair" (outline on deep) because the track card is off-screen there; four houses of the street under it | Section columns stack under the Bootstrap breakpoints; full width in a CSS media query; the phone crop of the street is a second background image in the same media query | GA (widths: Custom CSS); street Custom (content) | `PP/getting-started/add-sections` |
| Your repairs: slim deep title band with three houses | The same title band section at the small breakpoint | GA section; houses Custom (content) | `PP/getting-started/add-sections` |
| Repairs as cards | Not a list setting: CSS media queries on the list table, or a Card gallery, or the `fetchxml` template | Custom | `PP/getting-started/add-card-gallery` |
| Docked "Report a repair" with a top stroke and soft shadow | CSS `position:fixed` on the button below the small breakpoint | Custom (CSS) | `PP/configure/manage-css` |

## States

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Error summary with two links ("Tell us what is wrong", "Choose the room"), the same words at the fields | The form's validation summary (class `validation-summary alert alert-error alert-block`, links to fields on). Its default link text "click here" must be changed so each link names the field | GA (look and link text: Custom) | `PP/configure/basic-forms` |
| Message at the field, red bar and border | Required-field messages from the form metadata; bar and border in CSS | GA (look: Custom CSS) | `PP/configure/configure-basic-form-metadata` |
| Empty list, with the house picture (lit window, tick) | Classic list Empty message (HTML), holding the picture from a web file. The modern list's empty state: not confirmed | GA (classic) / Not confirmed (modern); picture Custom (content) | `PP/configure/list-configuration`, `PP/configure/web-files` |
| Loading list | Modern list shimmer, or classic list Loading message; the sketch's skeleton rows stand for the shimmer | GA | `PP/getting-started/add-list` |
| Failed save panel with the house picture (unlit windows, red cross badge), Try again and 0808 157 0142 | Not a documented form state: a generic error comes from the `Portal Generic Error` snippet; the panel with two buttons is custom script and markup | Custom | `PP/configure/customize-content-snippets` |
| Check your answers (six rows, Room included; visit "Today, as soon as we can. We will call you first.") | A further multistep step in read-only mode loading the record; its default look is not confirmed, the summary rows are CSS | GA parts; look Not confirmed | `PP/configure/multistep-form-steps` |
| Confirmation with the house picture (all windows lit, amber tick), RP-20417 and the emergency line | On submit, redirect to a page with the record value in the query string, read by Liquid; the emergency sentence shows when the risk answer is Yes (Liquid condition) | GA (condition: Custom) | `PP/configure/basic-forms` |

## Icons: Font Awesome names for the built site

The built site uses Font Awesome Free 6.2.1, loaded on Bootstrap 5 sites (observed on a live site's files, not
stated on Learn). The sketch draws Fluent icons as stand-ins of the same meaning. Names to verify against the 6.2.1
free set. The tile icons (fire, drop, flash for the socket, door, water, wrench for the spanner) are drawn as pictograms now; their Font Awesome names stay as the fallback if the pictogram files are not made.

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

- Styling workspace palette (12 slots): deep petrol `--deep` #0A4F58 (hero and title bands), amber `--accent` #F4B740 (the primary button), brand petrol `--brand` #0B6A75 (links, current nav item, selected rows, radios, progress), ink `--ink` #0E2A30 (top bar, footer, text on amber), paper `--page` #F6F5F2, white `--surface`, text `--fg1` #242424, secondary text `--fg3` #616161, brand tint `--brand-tint` #E4F1F2 (step band, pictogram circles), stroke `--stroke` #E0E0E0, light on deep `#CBE4E7`. One slot stays free. `--deep-2` #0D5E68, `--deep-3` #083F47 and `#12707B` live only inside the drawings.
- Measured pairs: ink on amber 8.4:1; white on deep 9.2:1; `#CBE4E7` on deep 6.9:1 (the "Repairs" label, intro, breadcrumb, greeting). Amber is never text on white (`--accent-ink` #8A5A00, 5.9:1, if a small amber label is ever needed) and never a status colour.
- Styling workspace buttons: Styling has two button styles, primary and secondary. Primary: amber fill and border, ink text, radius 4; the 3 px focus outline in CSS (ink on light surfaces, white on the deep bands). Secondary: white with a `--stroke-input` border and `--fg1` text (on deep: transparent with a `#CBE4E7` border and white text, by a CSS class).
- Fonts: body and controls in Fira Sans, headings (h1, h2, the wordmark) in Noto Serif 700; both are Google Fonts. Whether the Styling workspace's font list offers them is not confirmed (the list is not on Learn). Fallback: load them in the custom CSS file (an import from the font service), with "Segoe UI" and Georgia behind them.
- One custom CSS file: type sizes (Home: 64 display, with the 56 figures sharing the display role, 32, 22, 18, 14; other pages: 44 display, 32, 22, 18, 14; 14 is small text, labels, tracker labels and badges), line height 28 on 18 px body, the spacing scale (4 to 96, Home sections 80 to 96 apart), card radius 8 and 1 px stroke, 44 px controls (52 px for the hero button), radio boxes, progress segments, error bars, table and badge styles (status pairs `--pos`, `--warn`, `--crit`, info on brand tint, `--neu`), the timelines, the tiles, the numbered amber circles and dashed line, the skip link, the focus outline, the phone card layout and the docked button, and the footer silhouette background.

## Imagery

Every drawing is a static SVG web file that a designer supplies; the sketch's inline drawings are the art direction for them (flat shapes, the deep petrol family, `#CBE4E7`, white, amber, ink; the gable angle of the M mark; square corners), not final art. All are decorative (empty alt text). Status: Custom (content), GA mechanics.

| Drawing | Where it goes | Learn page |
|---|---|---|
| Hero street at dusk (1366 by 600, a focal group of taller houses about 70% across, plus a 390-wide crop of four houses for phones) | Background image of the hero section in the design studio, anchored bottom; the phone crop swapped in by a CSS media query | `PP/getting-started/add-sections`, `PP/configure/web-files` |
| Footer street silhouette in `--ink-2` | CSS background along the footer's bottom edge | `PP/configure/manage-css` |
| Three houses for the title band | Image component in the title band section, right aligned | `PP/configure/web-files` |
| Six task pictograms (radiator and flame; tap with a petrol drop and an amber handle; UK socket plate with an amber switch; door and key; wall with a lit window, drops and mould dots; spanner crossed with a screwdriver) | Image component in each tile, 64 px | `PP/configure/web-files` |
| Four step pictograms (phone with tick, clipboard, calendar, toolbox) | Image component above each step, 80 px | `PP/configure/web-files` |
| Three state pictures (house with a lit window and tick; all windows lit with an amber tick; unlit windows and a red cross badge, built like the other two) | In the list's Empty message HTML, the confirmation page and the failed-save panel markup, 96 px | `PP/configure/list-configuration`, `PP/configure/web-files` |

## Not drawn

- Steps 1, 3 and 4 of the form (Your home, Who is at home, Visit time): same pattern as step 2; step 5 is shown as a state.
- Sign-in and profile pages: their default look is not confirmed on Learn.
- The visit-change form (non-emergency visits only) and the repair details page behind "View".
- Hover states and the header's mobile menu open.
- The rest of the phone pages below the fold: the Home phone cuts off like a real phone at the top of the first tile.
- Final illustration: the drawings are art direction; a designer draws the web files.
- A cookie banner: not built in to Power Pages; essential cookies only on this site.
