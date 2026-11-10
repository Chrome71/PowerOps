# Ecosystem watch: the procedure

This page is for the people and agents who develop PowerOps. It is not for its users. Microsoft's tools, docs and agent skills change often. This is how a change is noticed, checked, sorted and written down, so the next session does not repeat the research or propose a change that was already rejected.

## The files

| File | What it holds |
|---|---|
| `README.md` | This procedure. |
| `sources.json` | The sources the weekly watch reads, and the value it saw last. |
| `changes.md` | The register: each finding, what was decided and why, and a log of the reviews. |
| `compatibility.md` | The external versions PowerOps pins or was tested with, and their limits. |

The collector is `.github/scripts/ecosystem-watch.sh`. The workflow `.github/workflows/ecosystem-watch.yml` runs it every Monday, puts its report in the job summary and keeps at most one open issue titled `Ecosystem watch`. The workflow writes no file and calls no tenant.

### The fields of sources.json

The file is `{"sources": [...]}`. The script is the one validator, and it refuses a bad file before it makes a request. Every source has `id`, `title`, `kind`, `target`, `areas`, `affects` and `lastChecked` (a date or null).

- `kind` is one of `nuget`, `npm`, `github-release`, `github-commits` or `manual`.
- `target` is a NuGet id, an npm name, `owner/repo`, or for `manual` an `https` URL.
- `areas` come from a closed list: `pac`, `alm`, `dataverse`, `apps`, `automate`, `pages`, `copilot-studio`, `power-bi`, `mcp`, `agent-skills`, `agent-clients`, `governance`.
- `affects` is one line saying what in PowerOps depends on the source.
- The four automated kinds add `lastSeen` (null or the last value read). `github-commits` may add `path`, so it watches one file or folder.
- `manual` adds `everyDays`, the cadence, and has no `lastSeen`.

## The three modes

- **Mode A, the weekly review.** Run it when the `Ecosystem watch` issue is open, or when someone asks.
- **Mode B, before a change.** Run it when a planned change calls or describes an external Microsoft service, CLI, API, SDK, skill or MCP server, and its evidence in `compatibility.md` or `changes.md` is older than 90 days or in doubt. The weekly watch sees only the files, pages and releases in `sources.json`. A change inside a cited skill or plugin is for mode B to catch.
- **Mode C, when something breaks.** Run it when an integration fails, a dependency changes or a capability is deprecated.

A docs-only edit or an internal refactor needs none of them.

## Mode A: the weekly review

Run the collector. With a token, never printed and never written to a file:

```sh
GITHUB_TOKEN="$(gh auth token)" bash .github/scripts/ecosystem-watch.sh
```

Without a token a run makes 16 GitHub calls against a limit of 60 an hour, so space such runs.

1. Read the report. It has four parts: new, not checked, unchanged and checked by hand. A source that was not checked is never "no change". Say why, and try it again.
2. A new source with no `lastSeen` has no baseline yet. Take the commit or version the repository cites (`git grep` on the target), or else the newest `verified:` date in the files that cite it. With neither, write "first observation, value recorded, no baseline" in the review log and research nothing.
3. For each new observation, read the release notes, the compare link or the page diff. For a source with a `path`, read its commits from the new sha back to the old one.
4. The cheap path: if the compare or diff touches nothing PowerOps uses, close the observation by naming its id (`<id>@<value>`) in the review-log line. No register entry.
5. Read each manual source that is due. Set its `lastChecked` to today (UTC) in `sources.json`, by hand, in the review's commit. A source you could not read keeps its date, goes in the review-log line with the reason, and stays due. The roadmap has a JSON feed at `www.microsoft.com/releasecommunications/api/v1/m365`. Filter it with `jq` by the product names in its `affects` and by `modified` after `lastChecked`. The Fabric roadmap's page is drawn by script, from a feed that `curl` can read: `https://roadmap.fabric.microsoft.com/fabric-json/?productId=<id>`, one product a call (Power BI is `642a8375-05fc-ee11-a1ff-000d3a341a60`, Fabric Developer Experiences is `c6da6b3b-ded6-ee11-9079-000d3a310f67`; the others are the `li` ids of the product list on the rendered page). Its items are under `results` and carry no `modified` date, so read the planned items and the newest quarters; on 2026-11-10 the Data Factory and Data Warehouse feeds were not valid JSON.
6. Verify each remaining finding (see Verifying), compare it with the repository (`git grep` the tool name, the pin or the Learn path), classify it and record it.
7. Only after every new observation is triaged, run the collector again with `--update`. It fetches again, so its observation ids must equal the triaged ones. Triage any other id before the commit. No source of that run may be not checked because of a rate limit.
8. Commit on a branch as `docs: ecosystem review <date>`.

The session whose review reaches `main` closes the issue. The workflow also closes it on a run that finds nothing new, nothing failed and nothing due. Expect an issue on most Mondays, most of it cheap path.

## Mode B: before a change

1. List what the change depends on: the tool, API or skill, and the version.
2. Read their rows in `compatibility.md` and their open entries in `changes.md`.
3. Verify what is older than 90 days or in doubt.
4. Write what you verified into the plan's Assumptions or Constraints.

## Mode C: when something breaks

1. Reproduce it offline first.
2. Read the docs and the release history of what changed.
3. Tell an upstream change from a PowerOps defect or a configuration mistake.
4. Take the smallest fix, with a regression test, through the normal plan, build and review.
5. A tenant read or write needs the person's yes, as `AGENTS.md` says.

## Verifying

Prefer these sources, in this order: Microsoft Learn; official release notes and roadmaps; Microsoft's GitHub repositories and releases; official API, SDK and CLI references; Microsoft architecture and security guidance. Community posts are only a pointer.

- A P2 or P3 finding needs one `powerops-researcher` answer with sources.
- A P0 or P1 finding needs `/verify-research`, which asks for approval before it runs. Where it cannot run, use two independent `powerops-researcher` refuters per claim, who are given the claim and its sources but not the first answer.
- Tell "documented" from "tested". Name rollout, region, licensing and preview limits.

## Classifying

| Priority | Meaning |
|---|---|
| P0 | A command, a gate route or a safety rule is wrong or unsafe now. Tell the owner at once. |
| P1 | It will break soon: a dated deprecation, or a change that will make a rule, route or doc claim wrong. |
| P2 | Something should change, but nothing breaks: a stale claim, a capability worth using. |
| P3 | Minor: wording, a nice-to-have. |

| Disposition | Meaning |
|---|---|
| implement now | Small, verified, touches no public command, schema, kept role or version file, and goes through the usual plan, build and review. |
| plan for later | Needs a milestone of its own, or the owner's decision. |
| monitor | Not yet real or not yet GA. `Why` names the date or trigger to look again. |
| no action | Checked and PowerOps is not affected, or the change was rejected. Its status is `closed`. |

## Recording

Search `changes.md` before you add an entry, so a rejected change is not proposed again. To reopen one with new evidence, add a new entry with `Supersedes: ECO-<n>`. Never renumber or delete an entry. The format is tested by `EcosystemDocsTests`:

```text
## ECO-0001: A short title
- Priority: P2
- Disposition: monitor
- Status: open
- Release status: preview
- Found: 2026-01-01, mode A
- Verified: 2026-01-01
- Affects: docs/HANDOFF.md
- Sources: [A Learn page](https://learn.microsoft.com/power-platform/example) (updated 2026-01-01)
- Evidence: documented
- Why: Why it matters, with the rollout and licensing limits.
- Link: none
```

Every field is one line, in this order. `Supersedes` is optional and comes last. A `done` entry needs a `Link` other than `none`.

## Adding a source

Add a source of an existing kind, and only a file, page or release whose change would alter a PowerOps rule, doc claim or pin. A source that needs a new kind is a `plan for later` entry. These Learn pages have a public docs repository, so the collector can watch their file:

- `learn.microsoft.com/power-platform/<p>` is `MicrosoftDocs/power-platform`, `power-platform/<p>.md`.
- `learn.microsoft.com/power-apps/<p>` is `MicrosoftDocs/powerapps-docs`, `powerapps-docs/<p>.md`.

Power Automate pages are manual sources, not watched through a repository: the public `MicrosoftDocs/power-automate-docs` repository's `main` has had no commit since 2026-04-28 while the live Learn page moved on (observed 2026-11-10), so a commit watch on it would never see an edit. Copilot Studio and Power BI have no public repository, so they are manual sources too.

## Boundaries

- No pin moves without a test of the new version.
- The kept roles and `tests/eval` are not touched outside their own rules.
- No tenant call without the person's yes.
- Nothing is merged or deployed by the watch or by this procedure.

## A stopped schedule

GitHub disables a scheduled workflow in a public repository after 60 days with no repository activity. A stopped watch looks like a quiet one. Check the workflow's state on the Actions page, or with `gh workflow list --all` (look for `disabled_inactivity`). Turn it back on with the page's button or `gh workflow enable ecosystem-watch.yml`. The workflow needs nothing beyond reaching `main`.

## How it fits with the rest

- Open questions stay in [FOUNDATION section 8](../FOUNDATION.md#8-research-topics-still-open). An entry links the item.
- The rule pages keep their `Sources:` lines with a `verified:` date.
- Dependabot keeps the NuGet packages, the Playwright example's npm packages and the Action SHAs current. `pin-drift.yml` watches PowerOps' own pins. Neither is repeated here.
