# The look: what a sketch and a built screen are graded against

Read this when you write a sketch (`powerops-prototype`), check a built screen against its sketch (`powerops-implement`, section 4) or review either one. Planning doesn't read it: `Screens` holds no visual detail. A designer reviews the result, so the target is a professionally built app, not a wireframe. The file rules of a sketch are in `powerops-prototype`; this file is only about how a screen looks and reads.

Canvas modern controls, the model-driven modern look and Power BI's default base theme follow Fluent 2, so this style transfers; the product file says how each rule maps to its controls and theme (`canvas.md` section 3, `model-driven.md`, `power-bi.md`).

## 1. Start from the job

Before any layout, write one line per screen and design from it:

- **Who and where.** The persona, the device, and how often they use the screen: all day at a desk, or twice a week on a phone in a corridor. It sets the density.
- **The one job.** What they come to do, from the screen's task in `Screens`.
- **First look.** The one thing the eye must land on first: the next item to act on, the number that says whether today is fine, the field to fill. A screen has one. If you can't name it, the screen has two jobs: say so.

## 2. Hierarchy and composition

- **Rank everything.** Three levels of emphasis and no more: the first look, what supports it, and the rest. Rank with size, weight, colour and position together, not with boxes. Half close your eyes: the first look must still be the first thing you see, and the primary action the second.
- **One primary action** per screen or region, filled in the brand colour, in the same place on every screen. Everything else is outlined or plain. Two filled buttons side by side mean the screen hasn't decided.
- **Layout from a known pattern.** Pick the layout the job calls for and name it: landing, dashboard, list with detail (split screen), form, or wizard for a guided sequence. Don't invent one.
- **A grid.** Every edge lines up with another edge. Content sits on a column grid (4 columns on a phone, 12 on a desktop), and what must be seen without scrolling is at the top.
- **Group by space, not by lines.** Related things sit close, unrelated things sit apart, and the gap says which is which. Add a stroke or a card only where space alone can't. No boxes inside boxes.
- **Restraint.** Most of a screen is neutral. The brand colour marks the primary action, the current selection and links, and nothing else. Status colours mean status and nothing else.
- **Text.** Left-aligned, 45 to 75 characters a line; never centred paragraphs or ALL CAPS labels. Numbers align right with `font-variant-numeric: tabular-nums` and carry their unit. Dates in one format.
- **Words.** A button says what it does: "Submit request", not "OK" or "Submit". A label sits above its field and is never the placeholder. Headings name the content, not the control ("Open requests", not "Gallery").

## 3. The system

- **Tokens.** A `:root` block of CSS custom properties opens the `<style>`, and every colour, size and space after it is a `var()`. No raw hex values elsewhere. Name status tokens by meaning (the canvas Badge intents: Positive, Caution, Critical, Informative, Neutral).
- **Colour.** Brand, its hover and the text colour on it, from the Theme block's seed; neutrals for the page background, card surface, subtle fill, stroke, and primary and secondary text; the status tokens, each a text-and-tint pair. Add no other hue. Text pairs reach 4.5:1; large text, input and focus borders, icons used as buttons and chart marks reach 3:1.
- **Type.** The Theme font in at most five sizes from one scale, for example 12, 14, 16, 20 and 28 px (caption, body, subtitle, section title, page title), line height 1.3 to 1.5, weights 400 and 600. One page title per screen.
- **Spacing.** An 8 px grid with 4 px steps for tight pairs: every padding, gap and margin is 4, 8, 12, 16, 24, 32 or 48 px. Cards have 16 px padding and 16 px gaps; page margins are 16 px on a phone and 24 to 32 px wider.
- **Surfaces.** Three levels: the page on the neutral background; cards on the surface colour with an 8 px radius and one light shadow or a 1 px stroke, never both; raised layers (a confirm screen standing in for a dialog) with a stronger shadow. No dashed boxes.
- **Header and navigation.** The same header on every screen: the app or page title, the user, and the primary action in the same place. Top-level sections use one pattern (tabs on a phone, a left rail on a desktop) and mark the current one by more than colour.
- **Status.** A labelled pill: a short word (Draft, Submitted, Approved) in a fully rounded tint, coloured by meaning, with the same word and colour wherever it shows. Never colour alone, and never a bare "Status: X" line.
- **Lists.** A row or card has a title line, two or three facts in secondary text, the status pill, and amounts aligned right. Dense data goes in a table with a sticky header row, not in cards.
- **Charts** (Power BI, dashboards). Draw the shape, not a labelled box: bars as flex items with heights from the fake values, a ring with `conic-gradient`, a trend as stepped bars. Each visual has a title that states the question it answers, labelled axes or values, and one sentence that says what the data shows. A KPI shows its number, its unit and a comparison in words ("12 more than last week").
- **Responsive.** One column at 360 px; from 600 px, list with detail: the list about a third wide on the left, the selected record on the right. Use `flex-wrap`, or one `@media (min-width: 600px)` rule. Targets are at least 24 by 24 px, and 44 px high on a phone.
- **States.** Empty: a heading, one sentence on why, and the action that fills it. Loading: skeleton rows in the subtle fill, or a spinner with a text label. Error: a message bar in the Critical tint with the cause in plain words and a retry or a way out. Failed save: that bar above the form, the input kept, and the field in error named in text with a 3:1 stroke.
- **Data that tests the layout.** Five to eight rows of plausible names, dates and amounts, a mix of statuses, one long value that must wrap or truncate well, and one empty optional field.

## 4. What fails

Any one of these means the screen isn't done:

- Default controls on a blank page, or everything the same size and weight.
- No first look, or more than one filled primary button in a region.
- A border on every element, boxes inside boxes, or a different radius or shadow per card.
- Spacing off the grid, or edges that line up with nothing.
- More than five type sizes, centred body text, or a label used as a placeholder.
- A colour that isn't a token, a status shown by colour alone, or a decorative use of a status colour.
- A chart drawn as an empty box, or a KPI with no comparison.
- A state that is only a note ("shows an error"), or says "Something went wrong" and stops.
- Lorem ipsum, "Item 1, Item 2", or three identical rows.

## 5. The grade

Score each screen 1 to 5 on each line, and fix anything under 4 before you show it. Report the scores as they are, with the weakest point in a few words.

1. **Job.** A new user can say what the screen is for and what to do next within five seconds.
2. **Hierarchy.** The first look and the primary action win the half-closed-eyes test.
3. **System.** Every colour, size and space is a token on the scale; the same thing looks the same everywhere.
4. **Craft.** Aligned edges, consistent gaps, no clipped or overlapping text at 360 px or 1280 px.
5. **States and data.** The four states are drawn and say what to do next; the data looks real and tests the layout.
6. **Access.** Contrast, visible labels, focus outline, target sizes, and colour never the only signal.

## Sources

Checked on 2026-11-08:

- [Overview of modern controls and themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/overview-modern-controls), [Use modern themes in model-driven apps](https://learn.microsoft.com/power-apps/maker/model-driven-apps/modern-theme-overrides) and [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): each follows Fluent 2, and new Power BI reports use the Fluent 2 base theme by default.

Checked on 2026-11-10:

- [Recommendations for optimizing user perception and aesthetics](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/visual-design) (XO:07): a clear hierarchy with focal points made by size, colour, contrast and position; the primary call to action as the strongest focal point, in the brand colour; white space to separate and raise importance; similar functions look the same; not too many competing focal points.
- [Recommendations for optimizing layout](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/layout) (XO:06): a column grid used the same way on every screen, the established layouts (landing, dashboard, split screen, wizard and others), essential information visible without scrolling, and column widths that keep text readable.
- The numbers here (three levels, the column counts, five type sizes, the 8 px grid, 45 to 75 characters, the 44 px phone target, the grade) are PowerOps's rules; the 24 by 24 px minimum and the contrast ratios are WCAG 2.2's (`design.md`).
