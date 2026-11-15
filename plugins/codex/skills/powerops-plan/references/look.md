# The look: what a sketch and a built screen are graded against

Read this when you write a sketch (`powerops-prototype`), check a built screen against its sketch (`powerops-implement`, section 4) or review either one. Planning doesn't read it: `Screens` holds no visual detail. A designer reviews the result, so the target is a professionally built app, not a wireframe. The file rules of a sketch are in `powerops-prototype`; this file is about how a screen looks and reads, and `baseline.md` holds the level it must reach (the statements S1 to S16, C1 to C10, P1 to P10, B1 to B12), the scale and the gates.

Canvas modern controls, the model-driven modern look and Power BI's default base theme follow Fluent 2, so this style transfers; the product file says how each rule maps to its controls and theme (`canvas.md` section 3, `model-driven.md`, `power-bi.md`).

## 1. Start from the job

Before any layout, write one line per screen and design from it:

- **Who and where.** The persona, the device, and how often they use the screen: all day at a desk, or twice a week on a phone in a corridor. It sets the density.
- **The one job.** What they come to do, from the screen's task in `Screens`.
- **First look.** The one thing the eye must land on first: the next item to act on, the number that says whether today is fine, the field to fill. A screen has one. If you can't name it, the screen has two jobs: say so.

## 2. Hierarchy and composition

- **Rank everything.** Three levels of emphasis and no more: the first look, what supports it, and the rest. Rank with size, weight, colour and position together, not with boxes. Half close your eyes: the first look must still be the first thing you see, and the primary action the second.
- **One primary action** per view, filled in the brand colour (on a site, the accent: `power-pages.md`), in the same place on every screen. Everything else is outlined or plain. Two filled buttons side by side mean the screen hasn't decided.
- **Layout from a known pattern.** Pick the layout the job calls for and name it: landing, dashboard, list with detail (split screen), form, or wizard for a guided sequence. Don't invent one.
- **A grid.** Every edge lines up with another edge. Content sits on a column grid (4 columns on a phone, 12 on a desktop), and what must be seen without scrolling is at the top.
- **Group by space, not by lines.** Related things sit close, unrelated things sit apart, and the gap says which is which. Add a stroke or a card only where space alone can't. No boxes inside boxes.
- **Restraint.** Most of a screen is neutral. The brand colour marks the primary action, the current selection and links, and nothing else; on a site the one accent takes the primary action (P9). Status colours mean status and nothing else.
- **Text.** Left-aligned, 45 to 75 characters a line; never centred paragraphs or ALL CAPS labels. Numbers align right with `font-variant-numeric: tabular-nums` and carry their unit. Dates in one format.
- **Words.** A button says what it does: "Submit request", not "OK" or "Submit". A label sits above its field and is never the placeholder. Headings name the content, not the control ("Open requests", not "Gallery").

## 3. The system

- **Tokens.** A `:root` block of CSS custom properties opens the `<style>`, and every colour, size and space after it is a `var()`. No raw hex value in CSS outside a `:root` block; an inline SVG's presentation attributes, such as `fill`, may hold one. Name status tokens by meaning (the canvas Badge intents: Positive, Caution, Critical, Informative, Neutral).
- **Colour.** Brand, its hover (the seed a step darker) and the text colour on it, from the Theme block's seed; neutrals for the page background, card surface, subtle fill, stroke, and primary and secondary text; the status tokens, each a text-and-tint pair. The status pairs don't come from the seed; unless the plan gives its own, use these, which pass 4.5:1: Positive `#0E700E` on `#F1FAF1`, Caution `#8A3707` on `#FFF9F5`, Critical `#B10E1C` on `#FDF3F4`, Informative `#0F548C` on `#EBF3FC`, Neutral `#424242` on `#F5F5F5`. One brand hue, one optional accent for a public site, the status pairs, and for data: a focus colour, a context grey and the bad and good pair. Each colour has one meaning (gate 4). Text pairs reach 4.5:1; large text, input and focus borders, icons used as buttons and chart marks reach 3:1.
- **Type** (S4). The Theme font in three or four sizes, each a full step apart on one ramp (12, 14, 16, 20, 24, 28, 32, 40 px), plus one display size for the lead number at least twice the body size. Weights 400 and 600, and 700 for the display number only. Secondary text is a lighter grey, not a smaller size. Line height 1.3 to 1.5 on a work screen. One page title per screen. A site's sizes and weights are P4's, a report's B8's.
- **Spacing** (S5). Every padding, gap and margin is 4, 8, 12, 16, 20, 24, 32, 40 or 48 px (a site's sections sit 48 to 96 px apart, P5): 8 to 16 inside a group, 24 to 32 between groups. Cards have 16 px padding and 16 px gaps (a report's visuals 8 to 12 px padding, B9); page margins are 16 px on a phone and 24 to 32 px wider.
- **Surfaces.** Three levels: the page on the neutral background; cards on the surface colour with an 8 px radius and one light shadow or a 1 px stroke, never both; raised layers (a dialog, or the screen that stands in for one) with a stronger shadow. No dashed boxes.
- **Header and navigation** (S14). The product's own frame, the same on every screen: a header, one navigation (top or side, never both) that marks the current section by more than colour, and a title row with the primary action; on a phone the primary action may dock at the bottom. Each product's frame: a canvas header with the app name and the user, and a left rail or a Tab list (C2); a site's header with the logo, at most six task-named links, sign-in and one filled call to action, and a footer with link columns (P3); a report's title row of at most 72 px on a 1920 by 1080 page (B1).
- **Icons** (S7). One family in one style: Fluent System Icons, regular, 20 px in navigation and buttons and 16 px in dense rows, filled only for the selected item; a site names its own set. In a sketch they come from one inline SVG sprite. An icon labels something: never decoration, never emoji.
- **Status.** A labelled pill: a short word (Draft, Submitted, Approved) in a fully rounded tint, coloured by meaning, with the same word and colour wherever it shows. Never colour alone, and never a bare "Status: X" line.
- **Lists.** A row or card has a title line, two or three facts in secondary text, the status pill, and amounts aligned right. Dense data goes in a table with a sticky header row, not in cards.
- **Charts** (Power BI, dashboards). Draw the real chart in inline SVG from the fake values: bars sorted and from zero, a line for seven or more points, a dashed target line with its label, direct labels, one highlighted series, and a subtitle that states the finding (B7, S15). Each visual has a title, and labelled axes or values. A KPI shows its number, its unit and a comparison in words with a signed difference ("12 more than last week", S2).
- **Responsive.** Each screen is drawn at the product's size (C1, P1, B1); a phone persona also gets a phone frame about 390 px wide, in one column. From 600 px wide, list with detail: the list about a third wide on the left, the selected record on the right. Targets are at least 24 by 24 px, and 44 px high on a phone.
- **States.** Empty: a heading, one sentence on why, and the action that fills it. Loading: skeleton rows in the subtle fill, or a spinner with a text label. Error: a message bar in the Critical tint with the cause in plain words and a retry or a way out. Failed save: that bar above the form, the input kept, and, when a field caused it, that field named in text with a 3:1 stroke.
- **Data that tests the layout.** Five to eight rows of plausible names, dates and amounts, a mix of statuses, one long value that must wrap or truncate well, and one empty optional field.

## 4. What fails

Any one of these means the screen isn't done. The eight gates in `baseline.md` section 6 fail a sketch whatever its scores: lead, contradiction, unbuildable, one meaning in two colours, more than one filled primary action, unreadable, not the product, and, for a site, could belong to anyone.

- Default controls on a blank page, or everything the same size and weight.
- No first look, or more than one filled primary button in a view.
- A border on every element, boxes inside boxes, or a different radius or shadow per card.
- Spacing off the grid, or edges that line up with nothing.
- More type sizes than S4 allows (P4 on a site, B8 in a report), centred body text, or a label used as a placeholder.
- A colour that isn't a token, a status shown by colour alone, or a decorative use of a status colour.
- A chart drawn as an empty box, or a KPI with no comparison.
- A state that is only a note ("shows an error"), or says "Something went wrong" and stops.
- Lorem ipsum, "Item 1, Item 2", or three identical rows.

## 5. The grade

A sketch is scored on the scale in `baseline.md` section 5 and failed by any of its gates (section 6). The author does not grade its own sketch: `powerops-review-sketch` does, in a fresh context. A built screen is checked against its sketch with the same scale (`powerops-implement`, section 4).

## Sources

Checked on 2026-11-08:

- [Overview of modern controls and themes in canvas apps](https://learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/overview-modern-controls), [Use modern themes in model-driven apps](https://learn.microsoft.com/power-apps/maker/model-driven-apps/modern-theme-overrides) and [Visual defaults in Power BI reports](https://learn.microsoft.com/power-bi/create-reports/power-bi-reports-visual-defaults): each follows Fluent 2, and new Power BI reports use the Fluent 2 base theme by default.

Checked on 2026-11-10:

- [Recommendations for optimizing user perception and aesthetics](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/visual-design) (XO:07): a clear hierarchy with focal points made by size, colour, contrast and position; the primary call to action as the strongest focal point, in the brand colour; white space to separate and raise importance; similar functions look the same; not too many competing focal points.
- [Recommendations for optimizing layout](https://learn.microsoft.com/power-platform/well-architected/experience-optimization/layout) (XO:06): a column grid used the same way on every screen, the established layouts (landing, dashboard, split screen, wizard and others), essential information visible without scrolling, and column widths that keep text readable.
- The numbers here (three levels, the column counts, 45 to 75 characters, the 44 px phone target, the default status pairs) are PowerOps's rules, and the type, spacing, icon and chart numbers are the statements of `baseline.md`, with its sources; the 24 by 24 px minimum and the contrast ratios are WCAG 2.2's (`design.md`).
