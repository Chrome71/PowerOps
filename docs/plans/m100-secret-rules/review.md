plan-hash: c728e0f61989c13d156cf4e8e4ef8dc347088d0bec71f6bb7355bc9ac7c746a4
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request says `tenant: offline`, so `powerops plan facts` was not run and no tenant was called. The plan's `environment:` is null.

First pass. This context has not seen the planning chat; it holds only the coordinator's brief. Status read with `dotnet run --project src/PowerOps.Cli -- plan status docs/plans/m100-secret-rules --json`: `approved`, review `pending`, 0 open questions, 0 admin questions, no violations, no warnings.

Tools the steps name, checked here: `dotnet`, `git` and `powerops` are on PATH; `reports/leakscan.sh` exists in the main checkout; the Fable review and `powerops-review-plan` are agents the session can spawn.

Checked against the code and the upgrade plan:
- The plan matches the upgrade plan's section "M100. Secret-handling rules": the same word and suffix lists, the same `Fix` lines, the same fixture contents, PO050 and the other reads left out. It adds `UnboundAction` and `PerformUnboundActionWithOrganization`, and records why in the Log.
- `FlowAction` (`FlowDefinition.cs` line 43) is a positional record built once in `WalkActions` and changed only by `with { Descendants = ... }`, so init properties set there carry through. The only test that compares whole `FlowAction` values (`FlowDefinitionTests.cs` 295-296) uses actions with no `authentication`, `secureData` or `actionName`, so it stays green. `InputLimitsTests` has the 100 MB allocation test the plan names. `RuleHelpers.CloudFlows`, `FlowDefinition.Literal` and `B.Env` (which defaults to type `100000000`) exist as the plan uses them. `EnvironmentVariable.Type` is the raw `<type>` text.
- Counted hits, measured again here with a scratch script over every flow JSON and `environmentvariabledefinition.xml` under `tests/fixtures/` (zips included) and `tests/eval/`. Secret-worded variables: `cat_DepndentEnvironmentSecretName` (Text, excluded by the suffix `name`), `cat_ALMAcceleratorSampleSecret` and the three eval tokens (all Secret). Secret reads: only eval `solution-01` and `solution-02`, both with `outputs` secured. No `Http` or `HttpWebhook` action anywhere has an `authentication` block. No test or builder call uses a Text variable with a secret word. Both rules are quiet on every existing fixture and eval case, as the plan says.
- Goldens: `analyze-fixtures.txt` ends `21 solutions, 0 errors, 73 warnings`; the SARIF lists every rule's descriptor (40 now). `flow-secrets` sorts after `flow-navigation` and before `flowtests` in `SolutionDiscoveryTests`. The ids ending in `93` and up are unused.
- Not applicable to a change to this repo's analyzer, as in M99's review: checklist items 1, 2, 4, 5, 6, 7, 8 and the per-product lines of 11. Item 3 is complete (`### Licences`). Item 10: 0 open questions, 0 admin questions; the Log names the areas not asked. No URL, GUID, email or person's name in `plan.md` or `decisions.md` (`api.contoso.com` is a fake host).

## Findings

### High

No findings.

### Medium

#### R1. PO042's authentication message says something the plan's own source contradicts
- Area: 13. Buildability
- Finding: Part (b) says the action's "credentials show in the run history" whenever an `Http` or `HttpWebhook` has an `authentication` block and no secured inputs. *Secure access and data for workflows in Azure Logic Apps* is one of PO042's sources, and it says this: when a downstream action explicitly uses another action's secured outputs as inputs, those inputs are hidden in the run history even though its Secure Inputs setting stays off. So an `Http` whose `authentication` value comes only from a secret read with `outputs` secured still gets a finding, and the message about it is untrue. No case in Data row 4 covers this, so the behaviour is not chosen on purpose.
- Suggested fix: Choose the behaviour and record it in the Log. Either keep the finding, since Microsoft's Key Vault walkthrough still secures the HTTP action's inputs, and reword the message to something like "its credentials can show in the run history unless every value in the block comes from a secured output". Or keep the wording and leave out that case. Add a row 4 case for an `Http` whose `authentication` uses only a secured read's output, with its expected result, and explain the choice on `PO042.md`.
- Resolution: open

#### R2. The Key Vault api name is unproven and has no probe step
- Area: 13. Buildability
- Finding: Assumptions marks it `unproven:` that the Key Vault connector's api id ends in `/shared_keyvault`, but says "No probe step". Item 13 needs either a source or an `unproven: <capability>; probed by step <n>` line with the probe before first use. Without the probe, PO042 might never fire on a Key Vault read. The rule page would then document a read the rule can't see, and the fires tests would only prove the plan's guess.
- Suggested fix: Add an offline probe before step 2. For example, search public exported solutions (open Microsoft or community sample repos, the CoE kit) for an `apiId` holding the Key Vault connector, and record the path and date. Or change the line to `unproven: ...; probed by step <n>` with a follow-up step, and log the person's or the session's acceptance as M99 did for `limit.timeout`.
- Resolution: open

### Low

#### R3. The test-first order needs the two rule classes as stubs
- Area: 9. Testability
- Finding: Step 2 adds row 1's properties first "so the tests compile", then has the row 4 tests fail on their own assertions. But the `WarningRuleTests` cases construct `PO041TextSecretVariable` and `PO042SecretWithoutSecureData`, which don't exist yet, so the tests fail to compile instead of failing their assertions.
- Suggested fix: In step 2, also create both rule classes as stubs whose `Evaluate` returns nothing, before writing the tests.
- Resolution: open

#### R4. Two claims for the rule pages have no listed source or observation note
- Area: 13. Buildability (AGENTS.md "Add a rule" step 5)
- Finding: Row 7 has `PO041.md` say that a Text variable's value "is plain text in the solution and the settings files". The two Learn pages it cites say this only for a custom connector's client secret, and the Log says so. Row 7 also has `PO042.md` say Logic Apps "runs cloud flows", and none of PO042's five sources supports that.
- Suggested fix: In row 7, have each page say the plain-text claim is observed in exports (the `defaultvalue` element and `environmentvariablevalues.json`, for example in `alm-makers`). For the Logic Apps claim, add a Learn page that says it, or reword it as "the same `runtimeConfiguration` key Power Automate exports, documented for Logic Apps".
- Resolution: open

#### R5. PO041's name test has edge cases no test pins
- Area: 9. Testability
- Finding: Common non-secret names such as `demo_TokenEndpoint`, `demo_TokenAudience`, `demo_SecretVersion` and `demo_TokenLifetime` will fire. The `id` suffix quietly drops any name ending in those letters (`..._PasswordValid`). Both follow from the upgrade plan's lists and are acceptable for a Warning, but row 4 has no case for either, so the trade-off isn't recorded in a test.
- Suggested fix: Add `demo_TokenEndpoint` (fires) to row 4 to pin the behaviour, and list the known false positives and the suffix check on `PO041.md` next to the `powerops baseline` advice. Or add `endpoint` to the suffixes and log that change from the upgrade plan.
- Resolution: open

#### R6. The WATCH note's golden grep is not a check of its own
- Area: 9. Testability
- Finding: The upgrade plan's binding WATCH note says the goldens and MCP results are grepped for header values, hosts, GUIDs and addresses. Done when 3 checks only the findings in tests. Step 3's line-by-line read covers the goldens only implicitly, and the finding `File` paths in the goldens hold the all-zero GUID, so a plain grep needs scoping.
- Suggested fix: In step 3, grep the new message and component lines of `analyze-fixtures.*` and the MCP test output for `contoso`, `@` and the GUID form, and record the result in the step's report.
- Resolution: open
