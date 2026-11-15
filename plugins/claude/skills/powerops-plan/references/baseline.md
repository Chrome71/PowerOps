# The baseline: the level a sketch must reach

Read this when you draw a sketch (`powerops-prototype`), judge one (`powerops-review-sketch`), check a built screen against its sketch (`powerops-implement`, section 4) or review either. `look.md` says how a screen is composed; this file says the level it must reach: statements a judge can check in a rendered sketch, the scale it is scored on, and the gates that fail it whatever its scores. Each statement keeps its ID (S shared, C canvas, P Power Pages, B Power BI), so a judge's row and a fix can name it. "est." marks a number read off a picture; a statement without a number says what to look for.

What each product can build is in its own file: the canvas table in `canvas.md` section 3, the Power Pages table in `power-pages.md` section 2 and the Power BI table in `power-bi.md` section 3. A sketch draws only what its table allows, and its map (`mockups/prototype.map.md`) names each element's control, status and Learn page. Where a product statement sets type sizes or weights (P4, B8), it takes the place of S4 for that product.

## 1. Shared: every screen, page and report

- **S1. One lead.** Each screen has one lead element: the next thing to act on, or the number that says whether today is fine. It is the largest or strongest item, in the top or top-left region, and at most three things compete for the eye. Check: cover the title and name the lead within five seconds; count the focal points (1 to 3).
- **S2. Every headline number is compared.** It carries a labelled comparison (target, plan or prior period) and a signed difference. The colour sits on the difference, with a sign, an arrow or a word beside it. Check: each KPI has a value, a label, a "vs ..." text and a signed difference; no bare numbers.
- **S3. Colour carries meaning.** Neutrals cover about 90% of the area (est.). The brand hue marks the primary action, the current selection and links; a status colour appears only on a status, with a word or an icon. The brand hue is not a platform default: `#0078D4`, `#0F6CBD`, `#118DFF`, `#3860B2` and `#0B7F99` are tells. Check: count the hues and where the brand hue sits.
- **S4. Three or four type sizes.** A screen uses three or four sizes, each a full step apart on one ramp (12, 14, 16, 20, 24, 28, 32, 40 px), plus one display size for the lead number, at least twice the body size. Secondary text is a lighter grey (`#616161` on white), not a smaller size. Weights 400 and 600; 700 only for the display number. Sentence case, never all caps. Check: list the sizes and their ratios; sample the text colours (two or three neutrals).
- **S5. One spacing scale.** Every gap is one of 4, 8, 12, 16, 20, 24, 32, 40 or 48 px: 8 to 16 inside a group, 24 to 32 between groups, and never three different gaps in one row. Each region has one left edge. Check: measure five gaps; draw a vertical line through a region's left edges.
- **S6. Surfaces by tone.** A tinted page (`#F5F5F5` to `#FAFAFA`) under white cards. A card has a 1 px stroke or one soft shadow, never both; at most two shadow levels show at rest; one or two corner radii, plus the circle. Check: sample the page and card colours, inspect a card edge, list the radii.
- **S7. One icon family.** Fluent System Icons (MIT) in the regular style: 20 px in navigation and buttons, 16 px in dense rows, filled only for the selected item. A site may name its own set instead, one set in one style. Icons label things; they are never decoration and never emoji. Check: compare the stroke and size of every icon in a list.
- **S8. Real content.** Plausible names of varied length, numbers with thousands separators and units, one date format, amounts right-aligned in tabular figures, percentages with at most one decimal. No schema names and no placeholder copy. Check: read the content and one numeric column.
- **S9. One filled primary action per view,** labelled verb and noun, in the same place on every screen. Check: count the filled buttons in each view (1).
- **S10. Designed states.** Empty (a title, one line of reason, one action), loading (a skeleton or spinner where the content will be), error (a bar at the top and the message at the field), success, and the selected, hover and focus states of rows and controls. Check: each state is drawn as the user would see it, in place.
- **S11. Density fits the job.** Work screens: 32 px controls, rows 40 to 48 px, 14 px text. Public pages: 44 px controls and text of 16 px or more. Check: measure a control and a row.
- **S12. At the product's size, and filled.** The frame has the product's size (C1, P1, B1) and no region is mostly empty: the empty area of the content region is at most about 20% (est.; gate 7 is stricter). Check: the frame's size, and the empty share.
- **S13. Accessible in the sketch itself.** Text 4.5:1; large text and marks 3:1; targets at least 24 px (32 typical, 44 on touch); the smallest text 12 px in an app, 14 px on a site and 9 pt in a report (avatar initials beside the name are exempt, as gate 6 says); a visible 2 px focus ring; colour never the only signal. Check: measure the pairs; find the smallest text.
- **S14. The product's own frame,** the same on every screen: a header, one navigation (top or side, never both), and a title row with the primary action. Check: compare two screens; the frame is identical.
- **S15. Subtitles state the finding.** Every data visual's subtitle says what the data shows ("Heating is 15.8 points under; three other trades are also under"), not what the axis is. Check: read each subtitle on its own.
- **S16. The map says who sees what.** Per screen, the map says who sees it, which rows, and which sensitive data it shows. Check: every screen has that line in the map.

## 2. Canvas apps

- **C1. Size.** Drawn at 1366 by 768 for desktop and tablet (the product's default 16:9 design size) and, when a persona uses a phone, also in a phone frame about 390 px wide. The app uses the product's breakpoints: 600, 900 and 1200.
- **C2. Frame.** A header with the app name and the signed-in user, and one navigation pattern: a left rail about 200 to 260 px wide with labels (or icons only), or a Tab list of 2 to 7 tabs. Never a tile hub as the only navigation.
- **C3. Summary first.** A list or home screen opens with a strip of 3 to 5 numbers, each larger than its label, each with its comparison (S2).
- **C4. Grid.** A column grid with 16 px gutters (24 on a wide screen), page margins of 24 to 32 px, and cards with 16 px padding, 16 px gaps and one radius (8 px).
- **C5. List rows.** Rows 44 to 48 px high (taller for two lines), with an avatar or icon, a title, one or two facts in grey, a status badge in a fixed column, and amounts or dates right-aligned. One row is shown selected. Search and filters sit above the list, with a count.
- **C6. Record.** An identity block (name, status, next action), then the facts in a label-over-value grid of 2 to 4 columns, in sections that carry their own title and action.
- **C7. Forms.** At most 600 px wide and shaped by the data: fields as wide as their content, titled sections, labels above, required fields marked, helper text, the error at the field, and Cancel (secondary) beside Save (primary). A long form becomes steps.
- **C8. Control size.** 32 px controls on a desktop; 44 px or more on a phone, where the primary action is docked at the bottom.
- **C9. Numbers the product can draw.** Tiles and charts from the canvas table: a value with a trend badge, a bar or a line chart. A ring, donut or sparkline only with its route named.
- **C10. Control and status named.** The map names, per element, the control that builds it and its status (GA, preview, Creator Kit or unsupported). A preview control is drawn only when the plan records that choice.

## 3. Power Pages

- **P1. Size.** Drawn as a browser page 1366 to 1440 px wide at its real scroll length, and in a 390 px phone frame. Content sits in a container of at most 1140 to 1200 px; reading and form pages in one of about 800 px.
- **P2. The task on the first screen.** The headline says what the visitor can do here, and a search box, 3 to 6 task tiles, or "start, continue, track" entries are visible without scrolling. No "Welcome to" headline, no stock photo under a dark overlay, no three centred feature columns.
- **P3. Header and footer.** A header with the logo, at most six task-named links, sign-in or the profile menu, and one filled call to action at its right end; a footer with link columns.
- **P4. Type with presence.** Two chosen web faces: a heading face with character and a text face, neither the product default nor Arial. Body 18 px at a line height of 1.5 to 1.6, headings 700, the home headline 56 to 64 px, inner page titles 40 to 44 px, at most five sizes a page plus the headline, and lines of at most 75 characters.
- **P5. Controls and rhythm.** 44 px controls with one radius, a 2 to 3 px focus outline, a skip link, 48 to 96 px between sections and 16 to 24 px inside them.
- **P6. Forms as public services build them.** One topic per step with "Step x of n", one column, fields as wide as their answer, a hint under the label, an error summary at the top linking to each field plus the message at the field, a check-your-answers step, and a confirmation page with a reference number and what happens next.
- **P7. A home for a signed-in visitor.** A greeting, 3 to 5 status tiles with counts, their recent items, and one primary action.
- **P8. My items.** A list with search and a status filter, status pills, "Showing x to y of n" and pagination; on a phone the rows become cards. One item's progress is a timeline of done, current and upcoming steps.
- **P9. Imagery of its own.** One large picture of the service or the place on the first screen (drawn in the sketch; the final art from a designer or the organisation's photos), a small set of pictograms in the same hand on tiles, steps and states, and a mark. One deep brand band carries the first screen; one accent colour means "the main action" and nothing else. No stock photo under an overlay, no placeholder copy, no template decoration.
- **P10. The build route.** Beside the design: the design studio and Styling alone; the same plus one custom CSS file and web templates; or a code site. Anything the Styling workspace doesn't set (in the samples: spacing, radius, inputs, tables, badges and the focus outline) is marked as custom CSS.

`power-pages.md` section 2, "The look of a site", is how P4 and P9 are met.

## 4. Power BI report pages

- **B1. Page and grid.** One 1920 by 1080 page, drawn at 16:9 on an 8 px grid: 24 to 32 px margins, 16 to 24 px gutters, a title row at most 72 px high. Every visual's edges sit on the grid, and visuals in a row share their top and height.
- **B2. How much.** 5 to 9 groups, at most 12 visuals, 3 to 6 KPIs and at most 3 slicers, at the right end of the title row. Four or more slicers go in a rail. No slicer sits top-left.
- **B3. Reading order.** Answer, comparison, detail: the lead number or visual top-left, the breakdowns in the middle, the table last and small. A bare single-value card is never the largest region.
- **B4. KPIs.** Each KPI has its label, a value of 28 to 36 pt, and a reference label with the comparison and a signed difference (S2), in one Card visual; a sparkline when the trend matters.
- **B5. Titles.** Every visual has a title and a subtitle. The title names the measure and the comparison, or states the finding; the subtitle states the finding (S15) and gives the period, unit or filter where it matters. Never "Sum of X by Y".
- **B6. Colour.** One hue with greys, one series or bar highlighted, and a good and bad pair kept for variance, always with a sign. At most five colours on an executive page and never more than eight; the same measure has the same colour everywhere.
- **B7. Plain charts.** Bars sorted and starting at zero; lines for seven or more points in time; direct labels for up to four series; a dashed grey target line; light dotted gridlines or none; 5 to 8 axis ticks. No gauges, no 3D, no pie or donut over five slices, no dual axis.
- **B8. Type.** At most four sizes: page title 20 pt semibold, visual titles 14 pt, KPI values 28 to 36 pt, labels 9 to 10 pt, nothing below 9 pt; plus, at most once a page, a lead callout of up to 60 pt (the theme's maximum).
- **B9. Surfaces.** A light grey canvas under white visuals: radius 8, a 1 px `#E8E8E8` border or one soft shadow, 8 to 12 px padding.
- **B10. Filled.** At most 15% of the content cells empty (20% on an executive page; gate 7 is stricter). No visual has a mostly empty plot.
- **B11. Numbers for reading.** K, M and bn with one decimal in cards and axes, percentages with none or one decimal, short dates, and an as-of or refresh stamp in the title row.
- **B12. Access.** Alt text planned for each data visual, a tab order equal to the reading order, and markers or labels so that colour is never the only cue.

## 5. The scale

The judge scores nine dimensions, each 0 to 4: **0** broken, **1** functional, **2** intentional, **3** crafted, **4** exceptional (the judge can't name an improvement). Every score cites a frame and an element.

1. First look and lead. 2. Hierarchy and type. 3. Colour and meaning. 4. Layout, spacing and fill. 5. Content and numbers. 6. States. 7. Product fit and frame. 8. Detail and finish. 9. Identity.

**At the baseline** means no gate fails and every dimension is 3 or more. The one exception is Identity, which may be 2 for canvas, model-driven and Power BI, products whose chrome and font the maker can't change, with the reason written. The author does not grade its own sketch: `powerops-review-sketch` does, in a fresh context.

## 6. The gates

Any one gate fails the sketch, whatever its scores:

1. **Lead.** With the title covered, as S1's check does, the element the job line names as the first look is not the largest or strongest item in its frame, or two items compete for it (decision Q16 of M97). One docked full-width primary action at the bottom of a phone frame is the second look and does not compete with the lead (decision Q17).
2. **Contradiction.** Two numbers, dates, references or statuses disagree, or one screen breaks a promise another makes.
3. **Unbuildable.** A main frame draws a part the map marks "not confirmed", "not native" or "preview" without the plan recording that choice. The fallback is what gets drawn.
4. **One meaning, two colours,** or one colour with two meanings.
5. **More than one filled primary action** in a view.
6. **Unreadable.** Text under 4.5:1, a mark under 3:1, clipped or overlapping text, or text under the product's floor: 12 px in an app, 14 px on a site, 9 pt in a report. The text of a disabled control is exempt, as WCAG 1.4.3 exempts inactive components. The initials in an avatar shown beside the person's name are exempt from the floor, because they repeat that name; their contrast still counts (decision Q15 of M97).
7. **Not the product.** The frame is not at the product's size (C1, P1, B1), lacks the product's chrome (S14), or more than 15% of a frame's content area is empty. Where a statement allows more empty area (S12, B10), this 15% holds.
8. **Could belong to anyone,** for a Power Pages site: no mark, no imagery, or a default font. For the other products this is the Identity score, not a gate.

## 7. The ladder

The step from poor to average is removal: gauges, extra slicers, rainbow colour, decoration. The step from average to high is adding meaning. Over 13 makeovers the changes that lifted a design were, most often first: colour cut to one accent with neutrals or given a meaning, a chart type that fits the data, elements removed, summary numbers moved to the top, hierarchy by weight and colour instead of more sizes, and titles rewritten. A target or comparison was added least often, and it is the clearest mark of the high side. The test: cover the page title and ask what the screen wants you to notice or do. If nothing answers, it is average; if clutter hides the answer, it is poor.

**Canvas apps.**

- Poor: controls placed by position with uneven gaps and edges that line up with nothing; colour alone for status; raw schema names as labels; emoji beside Fluent icons; no feedback on save.
- Average: the generated structure, a header bar over one full-width gallery, or a list on the left and a form on the right, with half the screen empty below a few rows.
- Average: one default blue for the header, labels, borders and icons alike; one font in two sizes; labels the same weight as their values.
- Average: every column of the table in schema order, the table name as the title, sample names like "Item 3"; default hover fills and no loading, empty or error state. Nothing is wrong and nothing is chosen.
- High: summary numbers first, then the list, then the detail; containers with gaps and one left edge for the title, labels and the first button.
- High: one brand accent for selection, actions and badges, neutrals for the rest, status colours only on a status.
- High: three text tiers by weight and colour (a semibold title, a grey secondary line, a smaller meta line); only the fields the task needs, the rest demoted to grey.
- High: fields sized to their data in titled sections, Cancel outlined beside a filled Save, inline validation; hover, pressed and focus states themed; a spinner, a success message and a plain error.
- High: the user's words, a record titled by its own name with a context line; one icon set at 16, 20 or 24 px; one radius, one shadow, one border weight.

**Power Pages.**

- Poor: unstyled Bootstrap, full-width stacked sections with no maximum width; placeholder copy left live; the same illustration repeated; a light grey headline on light grey.
- Average: the starter layout kept, a half-and-half hero of text and photo, three centred feature columns and a full-width coloured band, with large equal padding and dead zones.
- Average: Arial everywhere with weight-400 headlines; one theme accent (a teal or a navy) on bands, links and buttons alike; the preset theme unchanged.
- Average: a "Welcome to <name>" headline over a stock photo with a dark overlay, and generic copy that could belong to any organisation; a pipe-separated menu of Home, Pages and Contact us.
- Average: full-width inputs, the native file control, a small Submit at the left, no help text.
- High: the page built around the visitor's task (search, sign in, start a request) on a deliberate grid, the headline stating what they can do here and the primary action visible.
- High: a chosen web face with weight contrast, a brand palette of two or three colours with neutrals, the accent kept for actions.
- High: task-named menu items, sign-in and search where visitors expect them; spacing that groups, headings close to their content.
- High: forms fitted to their data in grouped steps, inline validation, a success page; specific copy, sample pages removed.
- High: imagery that shows the service or the place, a mark of its own, template decoration replaced by the brand's; WCAG 2.2 AA checked and reflow at 400% zoom.

**Power BI.**

- Poor: scrolling, overlapping visuals or misaligned borders; rainbow series, gauges, 3D, pies of ten slices, a line over categories; six or more slicers; the same KPI twice; dark backgrounds with low-contrast labels.
- Average: a 2 by 2 or 3 by 2 grid of equal tiles under a filled title band, with a row of equal KPI cards that show no target, trend or comparison.
- Average: the default blue in every bar, or a second colour for no reason; Segoe UI with every title the same weight.
- Average: four to six visuals, all with data labels and axis titles; four or five slicers in a band across the top.
- Average: titles that name the fields ("Profit by country"); drop shadows on every card. Everything is formatted and aligned, and the reader still has to work out the answer.
- High: answer top-left, comparisons in the middle, detail bottom-right, with equal margins and section gaps wider than visual gaps.
- High: one lead number with its status against the target; big numbers (24 to 32 pt) only for the one or two values that matter.
- High: greys for context and one accent for the series in focus, colour kept for "off target".
- High: bars sorted by value, lines for time, variance bars against the target, sparklines in a matrix; data labels only where the point matters.
- High: the comparison or the finding in the title; a refresh stamp; numbers in K and M with one decimal; axes from zero; no icon or a status glyph only; alt text and a tab order.

## Sources

Checked on 2026-11-11 by the design-research session, and spot-checked on 2026-11-14 for M97 (its plan's Assumptions A1 to A11):

- [Recommendations for optimizing user perception and aesthetics](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/visual-design) (XO:07): one to three focal points and the primary call to action as the strongest (S1, S9). [Recommendations for optimizing layout](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/layout) (XO:06): the same frame on every screen (S14). [Recommendations for design standards](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/design-standards) (XO:02): the contrast ratios (S13).
- [Create responsive layouts](https://learn.microsoft.com/power-apps/maker/canvas-apps/create-responsive-layout): the 16:9 default of 1366 by 768, and `SizeBreakpoints` 600, 900 and 1200 (C1). [Tab list](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-tabs-or-tabs-list): 2 to 7 tabs (C2). [Button](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-button): 96 by 32 by default (C8).
- [Bootstrap version 5](https://learn.microsoft.com/power-pages/configure/bootstrap-version-5) (P1), [Style your site](https://learn.microsoft.com/power-pages/getting-started/style-site) and [Manage CSS](https://learn.microsoft.com/power-pages/configure/manage-css) (P10).
- [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): new pages are 1920 by 1080 under the Fluent 2 defaults (B1). [Create a card visual](https://learn.microsoft.com/power-bi/visuals/power-bi-visualization-card): several measures and reference labels in one visual (B4). [Design reports for accessibility](https://learn.microsoft.com/power-bi/create-reports/desktop-accessibility-creating-reports): alt text and tab order (B12).
- [What's new in WCAG 2.2](https://www.w3.org/WAI/standards-guidelines/wcag/new-in-22/) (W3C): the 24 by 24 CSS pixel target (S13). WCAG's success criterion 1.4.3 exempts the text of an inactive component (gate 6, decision Q14 of M97).
- The numbers were compared with Microsoft's open agent skills (MIT), read at pinned commits on 2026-11-11: [power-platform-skills](https://github.com/microsoft/power-platform-skills/tree/cf7dd6542cb762be4c6fd37a92bfe402daf3b8d0) at `cf7dd65` (the canvas and Power Pages design rules) and [skills-for-fabric](https://github.com/microsoft/skills-for-fabric/tree/f8fac110ac3b93e9d1f7b0bd8feac5c6a468ce43) at `f8fac11` (the report skill, behind B1, B2, B4, B6 to B8, B10 and B11). None of their text is copied.
- Everything else here (the ramp, the spacing scale, the density, the ladder, the scale and the gates) is PowerOps's rule, set by the design-research session from Fluent 2's token source, other public design systems and data-visual practice, and three samples the owner passed (`samples/`).
