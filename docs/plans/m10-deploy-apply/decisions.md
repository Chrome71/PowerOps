# Decisions: m10-deploy-apply

## Round 1

### Q1. Does `deploy apply` need `--apply`, as `settings apply` does?
1. Yes: without `--apply` it re-plans, checks the hash and reports what it would import, and writes nothing; AGENTS.md's rule ("anything that writes to a tenant needs an explicit flag") and the `settings apply --apply` rule then cover it (recommended)
2. No: `deploy apply <plan file> --target <environment>` imports at once, as the BUILD-PLAN row writes it
3. In your own words
Answer: Yes: without `--apply` it re-plans, checks the hash and reports what it would import, and writes nothing; AGENTS.md's rule ("anything that writes to a tenant needs an explicit flag") and the `settings apply --apply` rule then cover it. Picked by this session under the user's delegation of 2026-10-22 (see Log).

### Q2. The Test settings file holds a connection id and a variable value. Where does the example workflow take it from?
1. A repository secret holding the file's JSON, written to the deploy folder by both jobs with the same bytes; it is configuration, not a credential, and a secret is masked in the public logs, while a repository variable would be printed in them; sign-in stays OIDC with no stored credential (recommended)
2. A file committed to the repository (a connection id and a value in a public repository)
3. In your own words
Answer: In your own words (the master session, 2026-10-22): secrets, but one secret per value, not a JSON blob (GitHub's security-hardening guide warns that redaction matches exact values and can miss parts of structured data), for example `TEST_CONNECTION_ID` and `TEST_ENVVAR_<NAME>`. The settings file is built at run time with `jq` from those values, under `$RUNNER_TEMP`, never printed, and anything derived from the values is masked with `::add-mask::`. For the apply they are environment secrets of the `test` GitHub environment (required reviewers), so only approved jobs read them; the PR plan job uses a second environment without reviewers (`test-plan`) holding the same values, so a plan does not wait for approval. Fork PRs get no secrets. The values never reach the PR comment, the job summary or the plan file (an artifact anyone can download from a public repository): only names, states and the hash; a test feeds sentinel values for a connection id and a variable value and checks they appear in no JSON or Markdown output of `deploy plan` or `deploy apply`. The docs example shows the pattern with fake values. Sign-in stays OIDC.

### Q3. How does the apply job get the reviewed plan?
1. The PR run uploads the deploy folder (zips and plan file) as an artifact; after the merge, the apply job downloads the artifact of the last successful run for the PR's head commit with `gh`, so it imports exactly the reviewed bytes, and the hash check refuses if the target changed since (recommended)
2. The apply job exports from Dev and plans again after the merge, then applies its own plan (the reviewed plan is then only compared by eye)
3. In your own words
Answer: The PR run uploads the deploy folder (zips and plan file) as an artifact; after the merge, the apply job downloads the artifact of the last successful run for the PR's head commit with `gh`, so it imports exactly the reviewed bytes, and the hash check refuses if the target changed since. Picked by this session under the user's delegation of 2026-10-22.

## Log

- plan started 2026-10-22 from the master session's brief: the BUILD-PLAN M10 row, decisions 9, 13 and 14, build-order tasks 28 and 29, and the M9 plan (`m9-deploy-plan`), which it builds on
- `powerops plan facts` not run: planning must not contact the tenant; `environment` stays null
- depth: quick (from `m9-deploy-plan`)
- the user delegated their parts on 2026-10-22, plan sign-off included ("the rest you can do including my parts ... as permissions have been lifted", relayed by the master session); this session answers the questions with the recommended options where they follow the BUILD-PLAN, and signs the plan off itself with `powerops plan sign-off`
- research verified on Microsoft Learn, docs.github.com and open repos on 2026-10-22 by powerops-researcher (Sonnet); no tenant contacted
- M9's live setup (2026-10-22) made the Entra app, both federated credentials (`test-plan`, `test`, immutable subject form), the Test application user, both GitHub environments and their secrets, one per value; this plan adds only the Dev application user
- approved without: users, environments and ALM, constraints (BUILD-PLAN decisions 9 to 14, the M9 plan and its live setup cover them)
- approved (2026-10-22)
- fresh review (Sonnet) 2026-10-22: 1 High, 7 Medium, 9 Low; all fixed in the plan text (R5's GitHub behaviours are checked in the live run; R7's `fail-on: warning` is advised in the docs, not set in the example, because the seed carries two deliberate warnings); no user decision needed; review.md plan-hash updated to the final body with a note
- master session's rules (2026-10-23): the zip artifact of a public repository is encrypted (`openssl enc -aes-256-cbc -pbkdf2`, the key an environment secret in both `test-plan` and `test`, made with `openssl rand` and piped into `gh secret set`), the apply job decrypts it before `deploy apply`, the plan file names the zips and holds no host; the docs recommend a private repository and give the encryption as the option for public ones, with the reason (the `.msapp` caches the source environment's URL); `blocked: true` is refused; plan and apply run on the same OS
- signed off 2026-10-23 by this session under the user's delegation (see above)
- code review (Sonnet) 2026-10-23: no High, 2 Medium (the Action's `fail-on` did not gate the import; the example promoted a PR merged into any branch), 7 Low and test gaps; all fixed
- main (M9, squash-merged) merged into this branch 2026-10-23 with the tree unchanged: main's tree was M9's head, which this branch holds
- live checks 2026-10-23 in the test repo with the example workflow (pinned to this branch's commit, pac 2.14.1): item 1 PR 4 got one plan comment against Test (upgrade 1.0.0.0 -> 1.0.0.1, plan ok) made from a managed export of Dev; item 2 after the merge the find job picked the artifact of the PR's last plan run (the plan hash equal to the comment's), the `test` reviewer approved, and the workflow imported the seed into Test, signed in with OIDC only (no client secret); item 3 the report said the flow was on as expected and the job passed; item 4 `drift` Dev vs Test found no drift apart from the managed state; item 5 PR 5 (1.0.0.2) was planned, then the seed flow was turned off in Test before the approval: apply refused ("the target's state of the solution ... changed since the plan; nothing was imported", exit 1, the job failed), Test stayed at 1.0.0.1, and the flow was turned back on
- the Action's gate step ran the analysis before the import in both apply runs
- observed 2026-10-23: a managed export of the seed from Dev carries Dev's host in the canvas app's `.msapp` (`Properties.json`, `References/DataSources.json`); it holds no environment variable value, no Test host and no tenant, environment or app id. This is why the artifact is encrypted in a public repository
- log scan of the 8 runs (2 plan, 2 apply, 4 analyze): no Dev or Test host in any form, no tenant, app or environment id, no email; every `***` is an env:/with: header, except that the apply job (environment `test`) masked the word "review" everywhere, also in step names, so one value registered in that job is that word; it is not the status secret (the import set Test's value from it, and it differs); cause not found, nothing shown that should be hidden
