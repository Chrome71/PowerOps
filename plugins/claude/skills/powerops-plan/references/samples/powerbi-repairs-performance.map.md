# Map: Repairs performance (Power BI), direction "Answer, cause, detail"

Sources: `../report.md` section 5.3 and `../notes/bi-capability.md` (sections 1 to 8 and "Pattern to visual map").
`BI/` = the path under `learn.microsoft.com/power-bi/`. Sizes are design px on the 1920 by 1080 page (pt = px times 0.75).
The main frame draws only what this map can call GA. Everything that was not confirmed is listed under "Left out
because not confirmed" and is not drawn anywhere. The page has no primary action, so it has no filled brand button:
the brand marks the logo, the selected tab, the focus series (the 12-month line) and the keyboard focus ring.

Four hues: petrol teal (the series), grey (context), red `#B10E1C` (under target, bad), green `#0E700E` (good).
Type: four sizes plus the one lead callout (60 pt, 80 px): 48 px (36 pt) KPI values, 27 px (20 pt) page title, 19 px
(14 pt) visual titles, 13 px (10 pt) for everything else, including axis ticks and labels.

## Screen "Overview" (1920 by 1080, Fluent 2 base theme)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Page size and canvas | Page settings: 16:9, 1920 by 1080 (Fluent 2 default for new pages); canvas background `#F6F5F2` | GA | `BI/create-reports/power-bi-report-display-settings`, `BI/create-reports/power-bi-reports-visual-defaults` |
| White visual boxes, radius 8, 1 px border, padding | Once in the theme `visualStyles["*"]["*"]` (background, border with radius, padding); Format pane General, Effects per visual as the fallback | GA | `BI/create-reports/report-themes-create-custom`, `BI/visuals/power-bi-visualization-format-pane-overview` |
| Logo (the M with two gables) | Image visual: the mark as an uploaded SVG or PNG, white on petrol, Image fit Fit, corner radius 8; off the tab order as decorative | GA | `BI/visuals/power-bi-visualization-image-visual` |
| Title, "Marrowby Homes", as-of line | Text box; the as-of time is a dynamic value (a measure on the last refresh); the clock is a 16 px Image visual | GA | `BI/create-reports/power-bi-reports-add-text-and-shapes` |
| Page tabs | Page navigator, style preset Tab, Grid layout Horizontal, Selected state with a 3 px underline in the brand colour; 48 px right of the title block, centred on the title row | GA | `BI/create-reports/button-navigators` |
| Three slicers at the right of the title row | Classic slicer, dropdown style, header as the label (Month, Area, Priority); 216 by 56 each, 16 apart. The List slicer is preview and not used | GA | `BI/visuals/power-bi-visualization-slicers` |
| Lead pane background | A group with a group background (PBIR `visualGroup`) or a rectangle shape behind the visuals below | GA | `BI/create-reports/power-bi-reports-add-text-and-shapes`, `BI/developer/projects/projects-report` |
| Lead number 86.4% | Card visual: callout value at 60 pt (the theme's maximum font size); title "Completed on time"; subtitle by a text measure ("October 2026: 3.6 points under the 90% target") | GA | `BI/visuals/power-bi-visualization-card`, `BI/visuals/power-bi-visualization-customize-title-background-and-legend` |
| "Below target" badge | Callout image from an SVG measure (data category Image URL) returning the pill for under target | GA (documented SVG-measure route) | `BI/visuals/power-bi-visualization-card`, `BI/create-reports/power-bi-images-tables` |
| Three comparison rows | Same Card: reference labels with title (Target, Last month, October 2025), value, and Detail = the signed difference; Detail colour by a measure returning `good` or `bad`; sign and arrow from a custom format string with a positive and a negative section (`"&#9650; +0.0 pts";"&#9660; -0.0 pts"`); reference-label divider on | GA | `BI/visuals/power-bi-visualization-card`, `BI/create-reports/desktop-custom-format-strings` |
| 12-month line against the target | Line chart: straight interpolation (Fluent 2 default is smooth: change it), markers on, the last category's marker larger, width 2.5, colour the first data colour; Analytics pane constant line at 90%, dashed, data label "Target 90%"; y axis start 82% and end 92% (set); the tick step is automatic and may differ from the 2 points drawn; x axis labels Dec to Oct with years; legend off; visual title at 10 pt semibold | GA | `BI/visuals/power-bi-line-chart`, `BI/transform-model/desktop-analytics-pane`, `BI/visuals/power-bi-visualization-customize-x-axis-and-y-axis` |
| Insight under the chart | Text box with dynamic values (trade, its rate, its share of late repairs: 45% = 57 of 126, the second trade) and a light background | GA | `BI/create-reports/power-bi-reports-add-text-and-shapes` |
| Definition line | Text box: "On time = completed within 6 hours (emergency), 7 days (urgent) or 28 days (routine)." | GA | `BI/create-reports/power-bi-reports-add-text-and-shapes` |
| KPI strip (four cards in one box) | ONE Card visual with four measures, Multi-card layout one row, dividers on; callout 36 pt; labels carry the unit ("Average days to complete", "Tenant satisfaction, out of 5"); reference labels: comparison as title, signed difference as value (two-section format string), colour by a measure returning `good` or `bad`; "38 overdue" is a reference label coloured `bad` | GA | `BI/visuals/power-bi-visualization-card` |
| On time by trade | Clustered bar chart sorted ascending; bars 18 px thick (inner padding), colour by rules (under 0.9 = `bad` red, else `#767676`); data labels on, position Inside end, white 10 pt semibold (7:1 on red, 4.5:1 on `#767676`); value axis off, range 0 to 1; X constant line at 90%, dashed, label "Target 90%", behind the bars; gridlines off. Title fixed, subtitle by a text measure ("Heating is 15.8 points under; three other trades are also under") | GA | `BI/visuals/power-bi-visualization-conditional-formatting`, `BI/transform-model/desktop-analytics-pane`, `BI/visuals/power-bi-visualization-customize-title-background-and-legend` |
| Open repairs by time left | Column chart in time order (sort by a bucket column); Colors per data point: Overdue `bad` red, others grey; data labels on; y axis and gridlines off; category labels wrap to two lines. Subtitle by a text measure ("Most open repairs still have more than a week left") | GA | `BI/visuals/power-bi-visualization-column-charts` |
| Contractor table | Table visual, style preset Minimal, total row on; columns Contractor (semibold), Trade (grey 10 pt, its own column), Jobs completed, On time, Against target, Average days (plain, one decimal; the rows weight to the total 9.2), Six-month trend; rows about 36 px; subtitle by a text measure ("Three contractors are under; Hartwell is furthest, 11.4 points under") | GA | `BI/visuals/power-bi-visualization-tables` |
| On-time bar in the table | SVG image measure (data category Image URL) drawing a 6 px track and bar, red under 90%, grey otherwise; value in the next column. Built-in data bars take one colour per column | GA (documented SVG-measure route) | `BI/create-reports/power-bi-images-tables` |
| Against target | Font colour by rules (`bad` under, `good` at or above); arrow and sign from the format string | GA | `BI/create-reports/desktop-conditional-table-formatting` |
| Six-month trend | SVG image measure, 96 by 24, red under target, grey otherwise, last point as a dot (built-in sparklines are one colour per column) | GA (documented SVG-measure route) | `BI/create-reports/power-bi-images-tables`, `BI/create-reports/power-bi-sparklines-tables` |
| Focus ring on Hartwell's row | The product's own keyboard focus, drawn on one element to show the style | GA | `BI/create-reports/desktop-accessibility-creating-reports` |
| Alt text and tab order | General, Alt text on every data visual (by measure where the subtitle is a measure); Selection pane, Tab order as below | GA | `BI/create-reports/desktop-accessibility-creating-reports` |

### Positions (PBIR `visual.json` x, y, width, height) and tab order

| Tab | Visual | x | y | w | h |
|---|---|---|---|---|---|
| none | Logo (image, decorative) | 32 | 40 | 40 | 40 |
| 1 | Title text box (title, organisation, as-of) | 88 | 24 | 488 | 72 |
| none | Clock image (decorative) | 88 | 64 | 16 | 16 |
| 2 | Page navigator | 584 | 40 | 320 | 40 |
| 3 | Slicer Month | 1208 | 32 | 216 | 56 |
| 4 | Slicer Area | 1440 | 32 | 216 | 56 |
| 5 | Slicer Priority | 1672 | 32 | 216 | 56 |
| none | Lead pane background (group or shape) | 32 | 112 | 600 | 936 |
| 6 | Card "Completed on time" (lead, badge, comparisons) | 56 | 136 | 552 | 272 |
| 7 | Line chart "12 months against the 90% target" | 56 | 440 | 552 | 448 |
| 8 | Insight text box | 56 | 912 | 552 | 72 |
| 9 | Definition text box | 56 | 1000 | 552 | 24 |
| 10 | Card, four KPIs | 656 | 112 | 1232 | 168 |
| 11 | Bar chart, on time by trade | 656 | 304 | 744 | 360 |
| 12 | Column chart, open repairs by time left | 1424 | 304 | 464 | 360 |
| 13 | Table, contractors | 656 | 688 | 1232 | 360 |

Right column: 168 + 24 + 360 + 24 + 360 = 936, so every column ends at y 1048 (32 px bottom margin).

## Screen "Overview (phone layout)" (390 by 844)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Phone page | Mobile layout view: the same visuals re-placed in the phone canvas; size and position are not inherited; shows only in the iOS and Android apps in portrait | GA | `BI/create-reports/power-bi-create-mobile-optimized-report-mobile-layout-view` |
| Title, clock icon and as-of | The same title text box and clock Image visual, re-placed; slicers left off the phone layout | GA | as above |
| Lead card | The same Card visual; callout at 48 pt (64 px) as a mobile format override; the badge from the same SVG measure | GA | `BI/create-reports/power-bi-create-mobile-optimized-report-format-visuals` |
| 12-month line | The same Line chart at phone width | GA | as above |
| KPI cards, one column | The KPI Card visual placed full width; the cards stack in one column, the second cut by the frame | GA (a two-column multi-card on mobile is not confirmed, so it is not used) | `BI/visuals/power-bi-visualization-card` |
| Phone canvas width | Drawn at 390; the phone canvas width in px is not stated on Learn | Not confirmed | none |

## States

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Selected: Heating bar | Default interactions: the four-KPI Card and the table cross-filter to Heating (71 open, 19 overdue; Hartwell, Heating, 221 jobs, 74.2%, -15.8 pts; the tile crops the table to its title, header and that row); the bar and column charts cross-highlight (dimmed marks at about 40%; the column highlight 19 + 13 + 16 + 14 + 9 = 71). How data labels look on dimmed marks (drawn outside the bar end in dark grey) is not confirmed. The table subtitle is the same text measure under the filter | Not confirmed (interaction defaults and label behaviour on dimmed marks are not in the capability notes) | `BI/visuals/power-bi-visualization-customize-title-background-and-legend` |
| Tooltip on Hartwell's row | Report page tooltip, page type Tooltip, 320 by 240, Allow use as tooltip; on the table, Tooltips Type Report page; a card, a small line chart with a constant line at 90%, a two-row table. Not read by screen readers, so every fact in it is also on the page | GA | `BI/create-reports/desktop-tooltips`, `BI/guidance/report-page-tooltips` |
| Empty: "No repairs match these filters" | Not native: built with a measure. Title by a text measure that switches when the on-time measure is blank; subtitle lists the slicer values ("Castle Quarter, Emergency, October 2026"); the centred sentence is a text Card that shows only when blank | Not confirmed (not native: built with a measure) | `BI/visuals/power-bi-visualization-customize-title-background-and-legend` |
| Error: refresh failed | The whole top of the page with the last good values (418 open, 41 overdue, 208 replies). The as-of text box's dynamic value is a measure comparing the last refresh with the schedule; it returns "Data from 10 Nov, 06:00: the last two refreshes failed." (one line in the title row) with the text box background in the caution tint | GA parts (text box, dynamic value, background); the measure is custom | `BI/create-reports/power-bi-reports-add-text-and-shapes` |
| Loading | The product's own behaviour while a query runs, drawn for completeness; its exact look was not checked | Not confirmed | none |
| Failed save | None: a report page writes nothing (one line under the gallery) | GA (nothing to build) | none |

## Left out because not confirmed

None of these is drawn. Each was in an earlier draft:

- A fixed "Against target" column of differences right of the bar chart (data label Detail cannot be placed in a column). The differences are in the table and the tooltip.
- A shaded gap between the line and the 90% target (error bars in Fill style or theme-only `shadeRegion`).
- A warning glyph and red colour on the Overdue column's data label and category label.
- A 90% tick inside the table's data bars.
- A smaller unit after a callout value ("days", "of 5"): the unit is now in the label.
- Smaller grey trade names inside the Contractor cell: Trade is its own column.
- A data label on the last point of the line only: the lead number states 86.4%.
- An info icon in the insight text box and a warning icon in the "38 overdue" reference label and the error text.
- A two-column KPI grid on the phone.
- A 72 pt callout (above the theme's 60 pt maximum).

## Theme (report theme JSON)

```json
{
  "name": "Marrowby Homes 2026",
  "dataColors": ["#0B6A75", "#949494", "#B10E1C", "#767676", "#06434A", "#CBE4E7"],
  "good": "#0E700E", "bad": "#B10E1C", "neutral": "#424242",
  "background": "#FFFFFF", "foreground": "#242424", "tableAccent": "#0B6A75",
  "textClasses": {
    "callout": {"fontFace": "Segoe UI", "fontSize": 36, "fontWeight": "bold", "color": "#242424"},
    "title":   {"fontFace": "Segoe UI", "fontSize": 10, "color": "#616161"},
    "header":  {"fontFace": "Segoe UI Semibold", "fontSize": 10, "color": "#616161"},
    "label":   {"fontFace": "Segoe UI", "fontSize": 10, "color": "#242424"},
    "largeTitle": {"fontFace": "Segoe UI Semibold", "fontSize": 14, "color": "#242424"}
  },
  "visualStyles": {
    "*": {"*": {
      "background": [{"show": true, "color": {"solid": {"color": "#FFFFFF"}}, "transparency": 0}],
      "border": [{"show": true, "color": {"solid": {"color": "#E0E0E0"}}, "radius": 8}],
      "padding": [{"top": 16, "bottom": 16, "left": 20, "right": 20}],
      "dropShadow": [{"show": false}],
      "title": [{"show": true, "fontSize": 14}],
      "subTitle": [{"show": true, "fontSize": 10, "fontColor": {"solid": {"color": "#616161"}}}]
    }},
    "page": {"*": {"background": [{"color": {"solid": {"color": "#F6F5F2"}}, "transparency": 0}]}}
  }
}
```

- Keys `name`, `dataColors`, `good`, `bad`, `neutral`, `textClasses`, `visualStyles` (with `"*"` and `page`), `border.radius` and `dropShadow` are in `../notes/bi-capability.md` section 3. The exact property shape of `padding` and of the title and subtitle cards should be checked against schema 2.159 before use: not confirmed here.
- Token map: `--brand` (petrol `#0B6A75`, 6.3:1 on white, 5.8:1 on the canvas) = first data colour and `tableAccent`; `--mark-ctx` second; `--crit` = `bad` = the one "under target" colour; `#767676` is the rule colour for labelled bars at or above target; `--mark-target` = the constant lines; `--pos`/`--neu` = `good`/`neutral`; `--page` = page background; `--stroke` = border; `--fg3` = `title` and `header` text classes.
- The callout's 60 pt for the lead is a per-visual setting (the theme's `callout` stays 36 pt for the KPIs).
- Segoe UI is not embedded: viewers without it see a fallback font.

## Who sees what

| Screen | Role | Rows they see | Sensitive data |
|---|---|---|---|
| Overview, phone layout, tooltip | The board's operations lead and the head of repairs (Marrowby staff with the report shared through the app) | All areas and priorities; aggregates only (rates, counts, days) | Contractor names and their performance (commercially sensitive: not for tenants or other contractors). No tenant names, addresses, references or household vulnerability on any visual; the model's repair rows stay behind the aggregates (no drillthrough, "see data" off) |
| States | The same roles | As above, under the selection or the failed refresh | As above |

## Not drawn

- A map (the direction gives it up) and any decorative chart.
- The Contractors and Homes pages behind the tabs, and drillthrough from the table.
- The Filters pane, visual header icons and the hover state of slicers.
- A dark theme (Learn advises resetting to light before publishing).
