# Decisions: m91-planning-licences

The owner's words behind this milestone (trial 3, 2026-11-11): "more user-friendly options given among the choices to avoid too much information overload", and "A license check at the start of the project before doing work as well would likely be needed as well, since many projects do not or not require premium and there are times where you need workarounds that don't rely on premium to save on costs that might depend on the org." None of the four answers of the fix plan's section 5 (Q1 to Q4) applies to this milestone. The routine choices below were made by the building session under the owner's direction of 2026-11-11, "as autonomous as possible".

## Round 1 (planning)

### Q1. Which tier option is recommended in the cost-tier question?
1. Not sure: standard first (recommended)
2. Standard licences only
3. Premium where needed
4. In your own words
Answer: Not sure: standard first. It presumes no licence, so picking the recommended option asserts no fact (the trial's S34); the fix plan lists the three labels but names no recommendation, and the skill's rule puts the recommended one first. (answered by: agent m91 session, on the owner's standing direction)

### Q2. Where is the defaults batch asked?
1. At approval, in section 7, under `standard` and `deep`; under `quick` and the fast path, B keeps them (recommended)
2. In the last question round
3. In your own words
Answer: At approval. The agent can't tell which round is the last, and the smoke script's `quick` scenario allows one round only, so `quick` keeps the defaults with the approval and logs `defaults kept: <N>`. (answered by: agent m91 session, on the owner's standing direction)

### Q3. How does the read keep account details out of the plan and the context?
1. `--query "value[].skuPartNumber" -o tsv`, and only product names written (recommended)
2. The full reply, filtered by the agent
3. In your own words
Answer: The `--query` filter. Part numbers alone are not account details, and the plan files get product names only. (answered by: agent m91 session, on the owner's standing direction)

## Log

- 2026-11-11: started by the M91 building session (Opus) in its own worktree at `b065f23` (`origin/main`). Depth `standard`: skill text, one new reference, tests and docs; no product code.
- 2026-11-11: rejected, as the fix plan did: a `plan status` warning for a plan with no `### Licences` table (every plan under `docs/plans/` would warn). Not added: a `docs/BUILD-PLAN.md` section, which the fix plan's file list for M91 doesn't name.
- 2026-11-11: research by `powerops-researcher` (Opus, Learn and the azure-cli repository, no tenant call), recorded in `plan.md` Assumptions. Sources: Graph "List licenseDetails" (learn.microsoft.com/graph/api/user-list-licensedetails); `az rest` (learn.microsoft.com/cli/azure/reference-index, updated 2026-11-03); azure-cli `dev` at `51552fd` (`src/azure-cli-core/azure/cli/core/util.py`, `cloud.py`, `auth/util.py`); the licensing service plan reference and its CSV (learn.microsoft.com/entra/identity/users/licensing-service-plan-reference, updated 2026-09-29); the Power Platform licensing FAQ (learn.microsoft.com/power-platform/admin/powerapps-flow-licensing-faq, 2026-10-08); Fabric licences (learn.microsoft.com/fabric/enterprise/licenses, updated 2026-11-10); Copilot Studio billing (learn.microsoft.com/microsoft-copilot-studio/billing-licensing, updated 2026-11-05); Managed Environment licensing (learn.microsoft.com/power-platform/admin/managed-environment-licensing, updated 2026-09-01); Power Automate licence deep dive (learn.microsoft.com/power-platform/admin/power-automate-licensing/deep-dive-on-specific-license); Azure CLI 2.92.0 release (github.com/Azure/azure-cli/releases, 2026-11-03). Verdict on the `az` read: unsupported, not refuted (the Graph scopes of the CLI's default token and the directory-role note are undocumented), so the read stays with its fallback and an empty reply counts as a failed read.
- 2026-11-11: approved for review by the building session; all five areas are covered by the plan's sections (no person-facing rounds: this is a PowerOps milestone plan). (answered by: agent m91 session)
