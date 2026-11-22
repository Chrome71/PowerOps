# Known issues

This file records what was seen in the M35 run of 2026-10-30 and 2026-10-31, one app in one developer environment (see [the M35 run report](https://github.com/Chrome71/PowerOps/blob/main/docs/plans/m35-live-checks/report.md)), behind five warnings of `SKILL.md`. Read it when what you see doesn't match what a tool reported. It holds no instruction that `SKILL.md` lacks: each bullet names the rule it backs, and the rule is in `SKILL.md`.

- **Section 3, step 3, "A Canvas Authoring compile isn't a save."** In one run, after a compile Save stayed greyed out and the status read "All changes are saved" with an older time, and a reload then lost the compile.
- **Section 3, step 3, "A compile can report success while Studio has crashed."** In one run, two compiles of a whole screen with its gallery crashed Studio while the tool reported success; the screen first with the gallery alone after it didn't. The cause isn't known. In that run, modern controls also showed as empty blocks after a compile until a reload.
- **Section 3, step 3, "A table the Python client creates can land outside the solution passed in `solution=`."** In one run, the Python client's table create put the table in the preferred solution although `solution=` was passed.
- **Section 3, step 4, "Run the check and the commit as separate commands."** In one run a single command did both, and the step was committed after its check failed.
- **Section 4, step 3, "If the published app shows an old-version banner, select Refresh on it."** In one run the first load after a republish was still the old version.
