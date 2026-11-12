plan-hash: 1bc5078645cc3b2540b1f03f63fec19fb569b5e42a9c4c3240f6666ac3fce1f1
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request says `tenant: offline`, so `powerops plan facts` was not run and no tenant was called. The plan's `environment:` is null.

Second pass, on the plan as revised in commit `38dcc8f` for this review's first pass (R1 to R6) and for the Fable plan review's five findings. This context has not seen the planning chat. It holds only this review's first pass and the coordinator's brief. Status read with `dotnet run --project src/PowerOps.Cli -- plan status docs/plans/m100-secret-rules --json`: `approved`, review `pending`, 0 open questions, 0 admin questions, no violations. Its one warning was that the first-pass review was stale.

Tools the steps name, checked here: `dotnet`, `git` and `powerops` are on PATH; `reports/leakscan.sh` exists in the main checkout; the Fable review and `powerops-review-plan` are agents the session can spawn.

How the first pass's findings were answered:
- R1 (message (b) overclaimed): answered. The message now states only what the export shows (`... has an authentication block and its secure inputs are off.`). Won't build names the "upstream read secured, HTTP action not" case as an accepted false-positive class, and row 4 has a case that pins it firing. Row 7 has the page record the Logic Apps page's two statements.
- R2 (Key Vault api id unproven): answered. The id now rests on an observed public export. I checked it here: at the cited commit, the cited `Customizations.xml` holds the `connectorid` `/providers/Microsoft.PowerApps/apis/shared_keyvault`. The action-level `apiId` is still inferred from the `connectorid` rule. That is stated, and a wrong guess costs only a missed finding.
- R3 (stubs): answered in step 2.
- R4 (unsourced page claims): answered. The plain-text claim is marked as observed, and the Logic Apps wording now reads "documented for Azure Logic Apps and observed in exported cloud flows".
- R5 (name edges): answered. `demo_TokenEndpoint` and `demo_SecretVersion` are pinned as firing and `demo_PasswordValid` as quiet, and the page lists them. These match the lists in row 2.
- R6 (golden grep): answered in step 3, scoped to message and component lines.

New lines checked:
- Fable's additions are in place: the `unsubscribe.authentication`-only cases, the object form of a connector action's `authentication` as a reader case and a PO042 quiet case, and triggers named as not read.
- The first pass's measurements still hold: both rules are quiet on every existing fixture (zips included) and every eval case. The golden totals of row 6 are unchanged.
- No URL, GUID, email or person's name was found in `plan.md` or `decisions.md`. The repository path and the api id path are not URLs.
- Not applicable to a change to this repo's analyzer, as in the first pass: checklist items 1, 2, 4, 5, 6, 7, 8 and the per-product lines of 11. Item 3 is complete. Item 10: 0 open questions, 0 admin questions.

## Findings

### High

No findings.

### Medium

No findings.

### Low

#### R1. The object form of `authentication` is described wrongly for `alm-makers`
- Area: 9. Testability
- Finding: Row 4's PO042 quiet case says every `alm-makers` connector action holds the object form `{ "type": ..., "value": "@parameters('$authentication')" }`. In `alm-makers`, 54 connector actions hold the string form `"@parameters('$authentication')"`. Ten flows also hold an object form, `{ "type": "Raw", "value": <an expression reading the trigger's `X-MS-APIM-Tokens` header> }`. No `alm-makers` action holds the object form the plan names. The test outcome doesn't change, because the type gate keeps every connector action quiet.
- Suggested fix: Reword the case to name both observed forms, string and `Raw` object, and use the observed `Raw` value for the object-form quiet case (and the matching reader case).
- Resolution: open

#### R2. The redaction check on `http` can trip on an action's name
- Area: 9. Testability
- Finding: Row 4's redaction check says no message or component contains `http`. A PO042 component is the action name, and the natural test name for an `Http` action is `HTTP` (as in `alm-makers`). The check would then fail without any value leaking.
- Suggested fix: Say in row 4 that the redaction cases' actions have names without `http` (as the fixture's `Call_partner` does), or check for a URL scheme and the host instead of the bare word.
- Resolution: open
