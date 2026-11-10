# Ecosystem changes: the register

What was found in Microsoft's ecosystem, what was decided and why. The procedure is in [README.md](README.md). `EcosystemDocsTests` holds this file to its format.

One section per finding, `## ECO-<4 digits>: <title>`, newest last. Ids rise by one from `ECO-0001` and are never renumbered or deleted. Each entry has these one-line fields, in this order: `Priority` (P0 to P3), `Disposition` (implement now, plan for later, monitor, no action), `Status` (open, done, closed), `Release status` (announced, preview, GA, deprecated, retired, unverified), `Found` (date and mode), `Verified` (date), `Affects` (paths or none), `Sources` (https links, each with its published, updated or observed date), `Evidence` (documented, tested, documented and tested), `Why`, `Link` (plan, commit, issue, FOUNDATION item or none) and, last and optional, `Supersedes`.

The review log below has one line per review: `- <date>, mode <A|B|C>: <n> automated read, <n> not checked, <n> by hand; <ids or "no entries">`. A line may go on, after a `.` or `;`, to name the cheap-path observation ids and the sources that had no baseline.

## Review log
