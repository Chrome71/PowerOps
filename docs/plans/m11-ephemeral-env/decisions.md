# Decisions: m11-ephemeral-env

## Round 1

### Q1. Does M11 get a live run that creates and deletes an environment in the user's tenant?
1. Yes: one manual run, one cancelled run, and the service principal question answered, as the BUILD-PLAN row's done-when says
2. No: docs and an offline test only; the live done-when is recorded as not run, with the reason
3. In your own words
Answer: No (the master session, 2026-10-23): "docs only, with no live run. Don't create or delete any environment." The tenant also has no Dataverse capacity for a sandbox (the reason decision 9 changed on 2026-10-22).

### Q2. Which environment type does the recipe create?
1. A repository variable, `Sandbox` by default, so a tenant that allows another type can use it (recommended)
2. Always `Sandbox`, as the BUILD-PLAN row writes it
3. In your own words
Answer: A repository variable, `Sandbox` by default, so a tenant that allows another type can use it. Picked by this session under the user's delegation of 2026-10-22; the master session asked on 2026-10-22 that the examples assume no environment type.

## Log

- plan started 2026-10-22 from the master session's brief (the BUILD-PLAN M11 row, decision 10, build-order task 30), drafted beside M10 and finished on 2026-10-23
- `powerops plan facts` not run: planning must not contact the tenant; `environment` stays null
- depth: quick (from `m10-deploy-apply`)
- the user delegated their parts on 2026-10-22, plan sign-off included ("the rest you can do including my parts ... as permissions have been lifted", relayed by the master session); this session answers the questions it can with the recommended options and signs the plan off itself with `powerops plan sign-off`
- research verified on Microsoft Learn, docs.github.com and open repos on 2026-10-22 by powerops-researcher (Sonnet); no tenant contacted
- approved without: users, environments and ALM, constraints (BUILD-PLAN decision 10, the M10 plan and Q1 cover them)
- approved (2026-10-23)
- observed 2026-10-23 in this session's live setup (a read of the user's tenant with pac 2.13.1, for M9 and M10): `pac admin list --json` printed only JSON, an array whose objects have `DisplayName`, `DomainName`, `EnvironmentId`, `EnvironmentUrl`, `GroupName`, `OrganizationId`, `Type` and `Version`; the recipe reads three of them
- fresh review (Sonnet) 2026-10-23: 3 High, 8 Medium, 4 Low; all fixed in the plan text (R15 needed no change; R10's repository-wide queue left to the reader); no user decision needed; review.md plan-hash updated to the final body with a note
- signed off 2026-10-23 by this session under the user's delegation (see above)
- 2026-10-23: built (step 1); one deviation from the plan text, to follow M10's review fix: the PowerOps action is pinned like every other action, as `Chrome71/PowerOps@0000000000000000000000000000000000000000 # replace with the full commit SHA of the PowerOps release you use, the same in both jobs`, so the pinning test covers every action, PowerOps included, by a 40-character hex SHA with a comment
- code review (Sonnet) 2026-10-24: no High, 2 Medium, 10 Low and 5 test gaps; all fixed
