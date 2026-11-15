# Tenant repairs site

## Goal

1. A tenant reports a repair in a few minutes, without an account, and is told when someone will come.
2. A signed-in tenant follows their open repairs and their next visit.

## Screens

A Power Pages site for tenants, on Bootstrap 5, with phone views.

- Home (browser, 1366 wide, and a phone frame): who: any tenant, signed out. Job: start a repair report, or track one by reference. First look: the heading and the "Report a repair" button.
- Report a repair (browser): who: a tenant, signed out or in. Job: say what is wrong, in steps, and send it. First look: the question of the current step.
- Your repairs (browser and phone): who: a signed-in tenant. Job: see each open repair and the next visit. First look: the next visit's date and time.

## Assumptions

- The headings use a Google Font that the Styling font list may not offer, and the check-your-answers step's default look is not confirmed. Both are drawn by choice; the map gives the fallback of each (load the font in the custom CSS file; the summary rows are CSS).
