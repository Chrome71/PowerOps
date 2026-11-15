# Repairs desk (canvas app), direction "Triage first": build map

What builds each element of `canvas-repairs-desk.html`. Controls, statuses and pages are from `../report.md`
section 5.1 and `../notes/canvas-capability.md` (Learn, read 2026-11-11). Learn page prefixes as in the report:
`MC/` = power-apps/maker/canvas-apps/controls/modern-controls/, `CC/` = power-apps/maker/canvas-apps/controls/,
`CA/` = power-apps/maker/canvas-apps/, `FX/` = power-platform/power-fx/reference/,
`KIT/` = power-platform/guidance/creator-kit/ (all under learn.microsoft.com).

The story follows `_build/STORY.md`: Thursday 12 Nov 2026 at 12:20; RP-20417 reported 08:30 on the tenant portal,
marked Emergency 08:41, attend within 6 hours, so by 14:30 (2 h 10 min left, 64% of the time used).

## Repairs (1366 by 768)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| App header (ink band, logo, app name, org, user) | Horizontal container, Fill = ink; logo = Container Fill brand, Radius 4, holding an Image of the Marrowby mark (an SVG file uploaded as media, with AccessibleLabel "Marrowby Homes"); Text controls; modern Avatar (Name "Asha K."). The modern Header control is not used: it takes no custom colours and no navigation. | GA | `CC/control-horizontal-container`, `CC/control-image`, `MC/modern-controls-header`, `MC/modern-control-avatar` |
| Alerts and help buttons | Modern Button, Layout IconOnly, Tooltip, AccessibleLabel; the unread dot is a small Container (Radius, Fill) over it | GA | `MC/modern-control-button` |
| Left rail, 220 px | Vertical container of modern Buttons, Layout IconBefore; the selected one by Fill (brand tint) and IconStyle Filled; the 3 px bar is a Container, Visible when selected; Settings pinned by a flexible spacer | GA | `MC/modern-control-button`, `CC/control-horizontal-container` |
| Title row and "Log a repair" | Text (Size 24, Semibold), Text in fg3; Button Appearance Outline, Icon Add | GA | `MC/modern-control-text`, `MC/modern-control-button` |
| Summary strip, four tiles | Horizontal container of four Containers (Fill white, Border 1, Radius 8, Padding 16): label Text 12, value Text 24 Bold, comparison line = modern Icon 16 (Clock, ArrowUp, ArrowDown) plus two Texts, the difference coloured. Tile 1's line is the next deadline on purpose ("Next due in 2 h 10 min"). Not the Metrics card: it is preview and has no per-field text colour. | GA | `CC/control-horizontal-container`, `MC/modern-control-icon`, `MC/modern-control-metrics-card` |
| Queue tabs with counts | Tab list, Appearance Underline, 5 tabs ("Parts" is short for Awaiting parts in the tab only); Visible of the list content follows `Selected`. A count in a smaller grey size inside a tab label: not confirmed (the Tab list font properties apply to all tabs); fallback is the count in the label text at the same size. | GA; count styling not confirmed | `MC/modern-control-tabs-or-tabs-list` |
| "Showing 7 of 14", search, filter | Text; Buttons IconOnly (Search, Filter) with Tooltip; search opens a Text input Type Search | GA | `MC/modern-control-button`, `MC/modern-control-text-input` |
| Queue rows | Classic Gallery with a flexible-height template (GA: the Gallery is listed as vertical, horizontal or flexible-height), rows about 58 px and 76 px for a wrapped title, sorted by time left; row: trade icon tile, two Texts, Badge, Text for time left (Color Critical on the first row only; the other emergencies in fg1 Semibold). The modern Data Grid is not used: no badge column, fixed row height. | GA | `CC/control-gallery`, `MC/modern-control-data-grid` |
| Trade icon tile | Container 32 by 32, Radius 8, Fill neutral (`--fill`) for every priority, holding a modern Icon 16 in fg2 chosen by trade (Fire heating, Drop plumbing, Door carpentry, Flash electrical, Window glazing). Priority is carried by the badge, not the tile. | GA | `CC/control-horizontal-container`, `MC/modern-control-icon` |
| Long row title (row 4) | Text with Wrap on and AutoHeight: long titles wrap to two lines and the flexible-height row grows to about 76 px; no ellipsis is drawn anywhere | GA | `MC/modern-control-text`, `CC/control-gallery` |
| Time-used bar under an emergency's time left (the direction's signature) | Modern Progress bar inside the gallery row, 56 by 4, Value = minutes used, Max 360 (6 hours), ProgressColor per row: Error on the first row (64%), otherwise a neutral colour (6% and 1%); Visible only when Priority is Emergency. ProgressColor offers Brand, Success, Warning, Error: a grey fill is not one of them, so the neutral fill is not confirmed; fallback Brand, or a Container whose Width is set by formula. Same in the phone rows. | GA; neutral fill not confirmed | `MC/modern-control-progress-bar` |
| Selected row | Gallery TemplateFill = If(ThisItem.IsSelected, brand tint); bar = Container, Visible = ThisItem.IsSelected | GA | `CC/control-gallery` |
| Priority badges | Badge: Intent Critical / Caution / Neutral, Appearance Tint, Shape Circular | GA | `MC/modern-controls-badge` |
| Detail header | Text Size 16 Semibold with two Badges (Critical, Neutral) in a Horizontal container; Icon Location and Text for reference and address | GA | `MC/modern-controls-badge`, `MC/modern-control-icon` |
| Target block (the lead) | Container (Fill crit tint, Border, Radius 8); Icon Clock; Text "Attend by 14:30 today"; Text Size 32 Bold Color Critical "2 h 10 min" with "left"; under them a Progress bar, full width, Value 64, Max 100, ProgressColor Error (no label of its own; AccessibleLabel "64% of the time to target used") | GA | `MC/modern-control-progress-bar` |
| Facts, label over value, 3 equal columns | Horizontal containers with three Vertical containers of equal Fill portions, each two Texts: Tenant, Reported, Source; Trade, Access, Household (flag Icon in caution colour) | GA | `CC/control-horizontal-container` |
| What the tenant said | Text with Wrap on in a 260 px column (four lines); the 2 px rule is a Container | GA | `MC/modern-control-text` |
| Activity timeline | Small vertical Gallery (short entries: "Reported online", "Marked Emergency", "Needs a contractor", with time and who in grey): dot = Container (Radius, Border), line = Container 1 px, two Texts; current item brand dot by If | GA | `CC/control-gallery` |
| Pinned action bar | Container with a top border outside the scrolling body; Icon, Text; Button Outline "Book a visit" (Icon Calendar); Button Primary "Assign contractor" | GA | `MC/modern-control-button`, `CC/control-horizontal-container` |
| Focus ring on "Assign contractor" | The modern control's own focus border; checker error if FocusedBorderThickness is 0 | GA | `CC/properties-accessibility`, `CA/accessibility-checker` |

## Log a repair (1366 by 768)

This frame is a new call (Dele O., Flat 6, 18 Ropewalk, Ferry Road), not RP-20417, which came in on the portal.

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Header and rail | As on Repairs (Repairs selected) | GA | as above |
| Back link, title | Button Appearance Subtle, Icon ArrowLeft (or Link, Type Subtle); Text Size 24 | GA | `MC/modern-control-button`, `MC/modern-control-link` |
| Three-step stepper | Horizontal Gallery of circles (Container Radius, Text or Icon Checkmark) and Text labels, joined by 1 px Containers; or the Creator Kit SubwayNav | GA; Kit preview | `CC/control-gallery`, `KIT/components` |
| Form card and its note | Container (Fill white, Border, Radius 8, Padding 24) with Text title, "Step 2 of 3" and the grey line "Every field is needed unless it says optional." (no asterisks; Required = true on each control) | GA | `CC/control-horizontal-container`, `MC/modern-control-form` |
| Trade, Where in the home | Combobox, Width 280, Required | GA | `MC/modern-control-combobox` |
| What is wrong? (focused) | Text input, Appearance Outline, full card width | GA | `MC/modern-control-text-input` |
| What the tenant said | Text input Type Multiline, three lines; the hint is a Text (no helper-text property) | GA | `MC/modern-control-text-input` |
| Photos: "Add a photo" and the file row | Add picture (classic) styled as an outline button with the camera icon, plus a small Gallery of added files (Icon, Text, size, an IconOnly Button to remove). Add picture cannot sit inside an auto-layout container: place it in a manual container. | GA | `CA/build-responsive-apps`, `CC/control-gallery` |
| Is anyone at risk? | Radio group, Layout Horizontal, Items Yes / No, aligned to the right edge of the field column; hint is a Text | GA | `MC/modern-controls-radio-group` |
| Back, Save draft, Continue | Buttons Outline, Subtle, Primary in a pinned Container with a top border; "Save draft" is Subtle everywhere | GA | `MC/modern-control-button` |
| Home card with open repairs | Container; Icon, Texts; small classic Gallery with Text and Badge (Intent Informative "Scheduled", Caution "Awaiting parts") | GA | `CC/control-gallery`, `MC/modern-controls-badge` |
| Is it an emergency? | Container; Icon Warning (caution colour); Text rows with 6 px dot Containers | GA | `MC/modern-control-icon` |
| Target times | Container with label and value Texts: within 6 hours, 7 days, 28 days | GA | `CC/control-horizontal-container` |

## Repairs (phone), Repair (phone), Log a repair (phone) (390 by 844)

| Element | Built with | Status | Learn page |
|---|---|---|---|
| One column below 600 px | App.SizeBreakpoints [600, 900, 1200]; Screen.Size = ScreenSize.Small hides the rail and the detail pane, shows the bottom bar | GA | `CA/create-responsive-layout` |
| Phone header and logo | Horizontal container Fill ink; Image of the mark in a brand Container; Text; Avatar; back = Button IconOnly. The status strip with the time is the device, not the app. | GA | `CC/control-horizontal-container`, `MC/modern-control-button` |
| "Log a repair" (filled, 44 px, docked) | Button Primary, Icon Add, Height 44, in a fixed footer container above the navigation (top border, DropShadow), as on Repair (phone). Kept filled on purpose: on the phone list nothing else is primary, and the coordinator logs calls all day; docked, it is the second look after the top emergency. | GA | `MC/modern-control-button`, `CC/control-horizontal-container` |
| Summary chips cut off at the edge | Horizontal container, Horizontal Overflow Scroll, three Containers with Texts (value Text 20 Semibold, so the top emergency below leads) | GA | `CC/control-horizontal-container` |
| Three tabs | Tab list with three items (no horizontal scroll in the Tab list; Parts and Overdue move to the filter) | GA | `MC/modern-control-tabs-or-tabs-list` |
| Rows with wrapping titles | Classic Gallery, flexible height, TemplatePadding 12; title Text with Wrap on (two lines, no ellipsis), then a Horizontal container with Badge, time left with its time-used Progress bar (emergencies only) and reference; first row selected as on desktop | GA | `CC/control-gallery` |
| Bottom navigation | Horizontal container shown when Screen.Size = ScreenSize.Small, with a soft top shadow (DropShadow on the container); each item a modern Icon (OnSelect, AccessibleLabel) over a Text. An icon above its label inside one Button: not confirmed (Button Layout is IconBefore, IconAfter or IconOnly). | GA | `CA/create-responsive-layout`, `MC/modern-control-icon` |
| Repair detail body and dock | Vertical container: scrolling body (Vertical Overflow Scroll) and a fixed footer container (top border, DropShadow) with the suggestion line, "Book a visit" (Outline) and "Assign contractor" (Primary), both 44 px. The body is drawn at a scroll position that ends after two whole Activity entries. | GA | `CC/control-horizontal-container` |
| Facts in two columns, Activity | Horizontal containers of label and value Texts; Activity as on desktop (vertical Gallery) | GA | `CC/control-horizontal-container`, `CC/control-gallery` |
| Step 2 of 3 and three-segment bar | Text; three Containers with Fill by step (one Progress bar cannot show segments) | GA | `CC/control-horizontal-container` |
| 44 px fields, Add a photo | Combobox, Text input (Multiline), Height 44; Add picture (classic) as an outline button; Radio group Horizontal | GA | `MC/modern-control-combobox`, `MC/modern-control-text-input`, `MC/modern-controls-radio-group` |

## States

| Element | Built with | Status | Learn page |
|---|---|---|---|
| Empty: nothing needs triage | Container Visible = IsEmpty(gallery items), centred in the list area: circular Container (pos tint) with Icon CheckmarkCircle, Texts, Button Outline "View scheduled repairs" that sets the Tab list | GA | `CC/control-gallery`, `MC/modern-control-button` |
| Loading: skeleton rows and spinner | Spinner with Label "Loading repairs"; skeleton rows are grey Containers, or the Creator Kit Shimmer | GA; Kit | `MC/modern-control-spinner`, `KIT/components` |
| Error: data not reached | Message bar built as a Container (crit tint, Border) with Icon, Texts and Button Outline "Try again" (Refresh); summary Texts and the list footer show an em dash and "Not loaded" when IsError (no count is claimed) | GA | `CC/control-horizontal-container` |
| Failed save | Bar container at the top of the form; Combobox ValidationState Error; the message under it is Icon plus Text; Buttons Outline "Back", Subtle "Save draft", Primary "Try again" | GA | `MC/modern-control-combobox`, `MC/modern-control-updates` |
| Confirm assign | `Confirm("They are told now ...", {Title: "Assign Hartwell Plumbing and Heating?", ConfirmButton: "Assign", CancelButton: "Cancel"})` | Preview | `FX/function-confirm` |
| Success banner, list without RP-20417 | `Notify("RP-20417 assigned to Hartwell Plumbing and Heating. Visit today, 13:00 to 14:30. The tenant has been told.", NotificationType.Success)`; the gallery refilters (Need triage 13, Scheduled 177) and selects RP-20421. The banner's close control as drawn: not confirmed. | GA | `FX/function-showerror`, `CC/control-gallery` |

## Who sees what

| Screen | Role | Rows they see | Sensitive data on it |
|---|---|---|---|
| Repairs, Repair (phone) | Repairs coordinator (desk and on call) | All open repairs in their areas (Northbank, Castle Quarter, Ferry Road); Business-unit read on repairs | Tenant first name and initial, full address, key safe access note, household vulnerability ("Child under 5"), the tenant's own words. Contractors see none of this here; the assign step sends them only address, access and the problem. |
| Log a repair (desk and phone) | Repairs coordinator | The caller's home and its open repairs | Tenant name, address, whether someone is at risk, photos of the home |
| States | Same roles | Same rows | The success banner names the contractor and the visit window only |

Tenant tracking by reference and postcode happens on the Power Pages site, not in this app. The household flag
needs a security role check before the build: only coordinators, not contractors or reporting users.

## Preview or unsupported parts and the GA fallback for each

- `Confirm()` (confirm state): preview. Fallback: an overlay container with a dimmed full-screen Container behind it.
  Its accessibility cost: it cannot trap focus or close on Esc, so keyboard and screen-reader users can tab behind
  it; mitigate with SetFocus to "Cancel" on open and by hiding the controls behind it (Visible false) while it shows.
- Attachments (modern, preview) is the option that needs a plan decision: it gives drag and drop and a file list in
  one control. The sketch draws the GA route (Add picture plus a gallery).
- Creator Kit SubwayNav (stepper) and Shimmer (skeleton): Kit, preview or sample. Fallback: the gallery of circles and grey Containers as drawn.
- Not confirmed, drawn anyway: a smaller grey count inside each tab label; the grey fill of the time-used bars on the
  second and third emergencies; the close icon on the `Notify` banner. Fallbacks: count at label size; Brand fill or a
  width-driven Container; banner without a close icon.
- The logo mark is an SVG file uploaded as an app media file and shown in an Image control; this is not the SVG
  data-URI route. Status of SVG media rendering in the Image control: not confirmed; fallback a PNG of the mark.

## Theme

Modern theme YAML (shape from `MC/modern-theming`; check the key names against that page when building):

```yaml
Theme:
  Name: Marrowby
  Font: "Segoe UI"
  BasePaletteColor: "#0B6A75"
  HueTorsion: 0
  Vibrancy: 0
  # ColorOverrides: Lighter30 ... Darker40 only if the generated ramp misses the kit's tint #E4F1F2 or press #06434A
```

- The theme seed is `#0B6A75`.
- HueTorsion and Vibrancy must be tuned in Studio until Colors.Primary, the tint and the pressed shade match the kit.
- The ink header (`#0E2A30`) is a Fill set by hand on the header containers, or App.Theme.Colors.Darker70 if it
  comes close. The page tone (`#F6F5F2`) is the screen Fill.
- Status colours come from Badge Intent (Critical, Caution, Positive, Informative, Neutral), not from the theme: the
  theme has one ramp. Crit tints on the target block and message bars are set per container with ColorValue.
- Type: 12 meta, 14 body, 16 record and card titles, 24 screen title and desktop summary values, 20 Semibold for the phone summary values, 32 Bold for the one lead number.
- No dark mode (not supported for canvas).

## Not drawn

- A calendar view: the direction gives it up on this screen (Schedule in the rail holds it).
- Steps 1 and 3 of Log a repair, and the Schedule, Contractors, Homes, Reports and Settings screens: outside this sample.
- Hover and pressed states of rows and buttons: the sketch is static; Gallery hover needs classic HoverFill.
- Charts and the data-mark colours: no chart on these screens; the swatches are shown in System for the shared kit.
- The third Activity entry on the phone detail: below the scroll position drawn.
- Layout choices that differ from the first prompt so the 768 px frame fits: search and filter sit in a toolbar
  under the tabs with the "Showing 7 of 14" line; "What the tenant said" and "Activity" sit side by side; the
  suggestion line sits above the two buttons; Trade and Where in the home share one row; the risk question is one row.
