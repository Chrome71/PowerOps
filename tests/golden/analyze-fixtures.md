## PowerOps analyze

**20 solutions, 0 errors, 68 warnings**

### tests/fixtures/agents/dataverse-tools: PowerOpsAgentTools 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO030](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO030.md) | botcomponents/demo\_Helper.action.MicrosoftDataverse-UpdateRow/data | Tool 'demo\_Helper.action.MicrosoftDataverse-UpdateRow' of agent 'demo\_Helper' uses the maker's credentials, so every user acts with the maker's permissions. | Set the tool to use end-user credentials, unless every user may act with the maker's permissions or a trigger calls the tool. |
| warning | [PO034](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO034.md) | Roles/Service desk.xml | Role 'Service desk' has Create and Write on 'SharePointData', which no component in the solution uses, so it may grant more than the solution needs. | Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't. |

### tests/fixtures/agents/new-ui/v1-no-learn-tool.zip: PowerOpsLiveAgent 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | customizations.xml | Component 'cr123\_draft\_def456.cr.shared\_workiqmcp.shared-workiqmcp-00000000-0000-0000-0000-000000000002' does not use the publisher prefix 'pops'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | customizations.xml | Component 'cr123\_loanhelper\_abc123.cr.shared\_microsoftlearndocsmcpserver.00000000000000000000000000000001' does not use the publisher prefix 'pops'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |

Not checked:

- PO027 to PO031 not checked for agent 'cr123\_loanhelper\_abc123': its recognizer is 'CLICopilotRecognizer', not GenerativeAIRecognizer or VirtualAgentsRecognizer, so it may not run on the standard harness that these rules read.

### tests/fixtures/agents/new-ui/v1.zip: PowerOpsLiveAgent 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | customizations.xml | Component 'cr123\_draft\_def456.cr.shared\_workiqmcp.shared-workiqmcp-00000000-0000-0000-0000-000000000002' does not use the publisher prefix 'pops'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | customizations.xml | Component 'cr123\_loanhelper\_abc123.cr.shared\_microsoftlearndocsmcpserver.00000000000000000000000000000001' does not use the publisher prefix 'pops'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |

Not checked:

- PO027 to PO031 not checked for agent 'cr123\_loanhelper\_abc123': its recognizer is 'CLICopilotRecognizer', not GenerativeAIRecognizer or VirtualAgentsRecognizer, so it may not run on the standard harness that these rules read.

### tests/fixtures/agents/new-ui/v2.zip: PowerOpsLiveAgent 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | customizations.xml | Component 'cr123\_draft\_def456.cr.shared\_workiqmcp.shared-workiqmcp-00000000-0000-0000-0000-000000000002' does not use the publisher prefix 'pops'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | customizations.xml | Component 'cr123\_loanhelper\_abc123.cr.shared\_microsoftlearndocsmcpserver.00000000000000000000000000000001' does not use the publisher prefix 'pops'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |

Not checked:

- PO027 to PO031 not checked for agent 'cr123\_loanhelper\_abc123': its recognizer is 'CLICopilotRecognizer', not GenerativeAIRecognizer or VirtualAgentsRecognizer, so it may not run on the standard harness that these rules read.

### tests/fixtures/alm-makers: ALMAcceleratorforMakers 0.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUrl/environmentvariabledefinition.xml | Environment variable 'cat\_DependentEnvironmentUrl' has a default value that looks like a URL. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUserId/environmentvariabledefinition.xml | Environment variable 'cat\_DependentEnvironmentUserId' has a default value that looks like an email address. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_WebhookUrl/environmentvariabledefinition.xml | Environment variable 'cat\_WebhookUrl' has a default value that looks like a URL. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_clientId/environmentvariabledefinition.xml | Environment variable 'cat\_clientId' has a default value that looks like a GUID. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUrl/environmentvariablevalues.json | Environment variable 'cat\_DependentEnvironmentUrl' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUserId/environmentvariablevalues.json | Environment variable 'cat\_DependentEnvironmentUserId' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_DepndentEnvironmentSecretName/environmentvariablevalues.json | Environment variable 'cat\_DepndentEnvironmentSecretName' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_GitHubOrgPlanExist/environmentvariablevalues.json | Environment variable 'cat\_GitHubOrgPlanExist' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_OrgLanguage/environmentvariablevalues.json | Environment variable 'cat\_OrgLanguage' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_OrgName/environmentvariablevalues.json | Environment variable 'cat\_OrgName' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_TemplateRepoName/environmentvariablevalues.json | Environment variable 'cat\_TemplateRepoName' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_WebhookUrl/environmentvariablevalues.json | Environment variable 'cat\_WebhookUrl' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_clientId/environmentvariablevalues.json | Environment variable 'cat\_clientId' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_sharedcommondataserviceforapps\_06411' does not use the publisher prefix 'cat'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_sharedpowerappsforappmakers\_87b9a' does not use the publisher prefix 'cat'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/WorkflowCompleteNotification-518F39D7-F4E5-EA11-A817-000D3A56B702.json | Flow 'WorkflowCompleteNotification' is turned off. | Turn the flow on before export, or remove it from the solution if it is no longer needed. |
| warning | [PO012](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO012.md) | Workflows/DeleteProjectCleanUp-6F3FF001-D7EC-EA11-A817-000D3A1ABE26.json | Flow 'DeleteProjectCleanUp' runs on every update of 'cat\_project'; set filter columns. | Set the trigger's filter columns to the columns whose update should start the flow. |
| warning | [PO012](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO012.md) | Workflows/BuildRequestCancellation-05878F85-5A13-EB11-A813-000D3AA3E77C.json | Flow 'BuildRequestCancellation' runs on every update of 'cat\_project'; set filter columns. | Set the trigger's filter columns to the columns whose update should start the flow. |
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/CreateEnvironment-46735D3D-D2E1-EA11-A813-000D3A1ABE26.json | Flow 'CreateEnvironment' has 28 actions and a nesting depth of 5. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/CreateProject-0405E906-DAE1-EA11-A813-000D3A56B702.json | Flow 'CreateProject' has 76 actions and a nesting depth of 4. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/DeployToProd-076AFC29-3CE8-EA11-A817-000D3A1ABE26.json | Flow 'DeployToProd' has 109 actions and a nesting depth of 5. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/DeployToStage-692CD95A-C0E2-EA11-A813-000D3A1ABE26.json | Flow 'DeployToStage' has 67 actions and a nesting depth of 4. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/WorkflowCompleteNotification-518F39D7-F4E5-EA11-A817-000D3A56B702.json | Flow 'WorkflowCompleteNotification' has 72 actions and a nesting depth of 4. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |

### tests/fixtures/alm-sample: ALMAcceleratorSampleSolution 0.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/EmailQuote-A4C44986-14D6-EC11-A7B5-000D3A1C0439.json | Flow 'Email Quote' is turned off. | Turn the flow on before export, or remove it from the solution if it is no longer needed. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/LikeQuote-90D90640-11BC-EC11-983F-0022483403A3.json | Flow 'Like Quote' is turned off. | Turn the flow on before export, or remove it from the solution if it is no longer needed. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/Smplerligforsk-37443C82-D03D-ED11-9DB0-0022482E56BE.json | Flow 'SÆmple Ø ærlig forsøk' is turned off. | Turn the flow on before export, or remove it from the solution if it is no longer needed. |
| warning | [PO032](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO032.md) | AppModules/cat\_AlmAcceleratorSampleMda/AppModule.xml | App 'cat\_AlmAcceleratorSampleMda' is shared only with System Administrator or System Customizer, so users without an admin role may not be able to open it. | Share the app with the security roles its users hold, and remove System Administrator and System Customizer from it unless they need the app. |
| warning | [PO033](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO033.md) | Roles/ALM Accelerator Sample Role.xml | Role 'ALM Accelerator Sample Role' has table privileges but none of prvReadAppModule, prvReadUser and prvReadUserQuery, so a user who holds only this role may not be able to open a model-driven app. | Copy the role from App Opener, or give its users App Opener as well, and test the app as a user who holds only those roles. |

Not checked:

- PO014, PO018 and PO036 not checked for canvas app 'CanvasApps/cat\_devopskitsamplecanvasapp\_c7ec5\_DocumentUri.msapp': no Src/\*.pa.yaml was read from its .msapp (an older app has none until it is resaved in Studio, and a file that isn't a zip, such as a Git LFS pointer, is not opened).
- PO034 not checked for role 'ALM Accelerator Sample Role': 1 of the tables it can create or write are used by no component PowerOps reads, and the solution holds a plugin assembly, whose table use isn't read.

### tests/fixtures/coe-auditlogs: CenterofExcellenceAuditLogs 3.27.2

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'admin\_sharedadmin5foffice2036520management20api5f7fc9343cf45734fe\_8f08c' is not used by any flow or app. | Remove the connection reference from the solution, or use it in the flow or app that needs it. |
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'admin\_sharedcommondataserviceforapps\_8f10b' is not used by any flow or app. | Remove the connection reference from the solution, or use it in the flow or app that needs it. |
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'new\_CoEAuditLogsO365Outlook' is not used by any flow or app. | Remove the connection reference from the solution, or use it in the flow or app that needs it. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | Workflows/DeprecatedAdminSyncAuditLogs-FA91974D-6E68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| Admin \| Sync Audit Logs' holds a default for environment variable 'admin\_AdminMail' that looks like an email address. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | Workflows/DeprecatedChildAdminSyncLogs-751183FC-DB68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| \[Child\] Admin \| Sync Logs' holds a default for environment variable 'admin\_AdminMail' that looks like an email address. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | Workflows/DeprecatedAdminSyncAuditLogs-FA91974D-6E68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| Admin \| Sync Audit Logs' holds a default for environment variable 'admin\_PowerAutomateEnvironmentVariable' that looks like a URL. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | Workflows/DeprecatedChildAdminSyncLogs-751183FC-DB68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| \[Child\] Admin \| Sync Logs' holds a default for environment variable 'admin\_PowerAutomateEnvironmentVariable' that looks like a URL. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | Workflows/DeprecatedAdminSyncAuditLogs-FA91974D-6E68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| Admin \| Sync Audit Logs' holds a default for environment variable 'admin\_TenantID' that looks like a GUID. | Leave the default empty and set the value per environment at deployment time, or use a default that is valid everywhere. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_CoEAuditLogsDataverse' does not use the publisher prefix 'admin'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_CoEAuditLogsO365Outlook' does not use the publisher prefix 'admin'. | Recreate the component under the solution's publisher, or keep it in a solution of the publisher that owns it and add that solution as a dependency. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/DeprecatedAdminSyncAuditLogs-FA91974D-6E68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| Admin \| Sync Audit Logs' is turned off. | Turn the flow on before export, or remove it from the solution if it is no longer needed. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/DeprecatedChildAdminSyncLogs-751183FC-DB68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| \[Child\] Admin \| Sync Logs' is turned off. | Turn the flow on before export, or remove it from the solution if it is no longer needed. |
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/DeprecatedChildAdminSyncLogs-751183FC-DB68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| \[Child\] Admin \| Sync Logs' has 36 actions and a nesting depth of 5. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |
| warning | [PO017](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO017.md) | Workflows/DeprecatedChildAdminSyncLogs-751183FC-DB68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| \[Child\] Admin \| Sync Logs' runs loop 'Apply\_to\_each\_Audit\_Log' inside loop 'Apply\_to\_each\_cluster'. | Replace the inner loop with one query, filter or bulk action over the whole set, or fetch the related rows with the outer query's expand option and use one loop. |

### tests/fixtures/core-portal: msgov\_coreportal 1.0.0.0

No findings.

### tests/fixtures/equipment-checkout: EquipmentCheckout 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO032](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO032.md) | AppModules/equipment\_checkout\_a1a05935/AppModule.xml | App 'equipment\_checkout\_a1a05935' is shared only with System Administrator or System Customizer, so users without an admin role may not be able to open it. | Share the app with the security roles its users hold, and remove System Administrator and System Customizer from it unless they need the app. |
| warning | [PO033](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO033.md) | Roles/Front desk.xml | Role 'Front desk' has table privileges but none of prvReadAppModule, prvReadUser and prvReadUserQuery, so a user who holds only this role may not be able to open a model-driven app. | Copy the role from App Opener, or give its users App Opener as well, and test the app as a user who holds only those roles. |
| warning | [PO033](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO033.md) | Roles/IT staff.xml | Role 'IT staff' has table privileges but none of prvReadAppModule, prvReadUser and prvReadUserQuery, so a user who holds only this role may not be able to open a model-driven app. | Copy the role from App Opener, or give its users App Opener as well, and test the app as a user who holds only those roles. |
| warning | [PO034](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO034.md) | Roles/Front desk.xml | Role 'Front desk' has Create and Write on 'SharePointData', which no component in the solution uses, so it may grant more than the solution needs. | Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't. |
| warning | [PO034](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO034.md) | Roles/IT staff.xml | Role 'IT staff' has Create and Write on 'SharePointData', which no component in the solution uses, so it may grant more than the solution needs. | Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't. |

### tests/fixtures/flow-navigation: PowerOpsNavigation 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO035](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO035.md) | Workflows/Reviewexpenses-00000000-0000-0000-0000-000000000061.json | Flow 'Review expenses' action 'Add\_expense' names 'demo\_costcentre' on 'demo\_expenses', but the navigation property of table 'demo\_Expense' is 'demo\_CostCentre'; the Web API matches navigation property names by case. | Write the navigation property name exactly as the table's relationship defines it, in the same case. |
| warning | [PO035](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO035.md) | Workflows/Reviewexpenses-00000000-0000-0000-0000-000000000061.json | Flow 'Review expenses' action 'List\_expenses' names 'demo\_costcentre' on 'demo\_expenses', but the navigation property of table 'demo\_Expense' is 'demo\_CostCentre'; the Web API matches navigation property names by case. | Write the navigation property name exactly as the table's relationship defines it, in the same case. |

### tests/fixtures/flowtests/capture/solution: PowerOpsCapture 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO016](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO016.md) | Workflows/PowerOpscapture-00000000-0000-0000-0000-000000000058.json | Flow 'PowerOps capture' has 120 actions and a nesting depth of 1. | Move self-contained parts of the flow into child flows, and reduce nested scopes, conditions and loops. |

### tests/fixtures/flowtests/skipped-body: PowerOpsSkippedBody 1.0.0.0

No findings.

### tests/fixtures/powerops-seed-yaml/v1: PowerOpsSeed 1.0.0.0

No findings.

### tests/fixtures/powerops-seed-yaml/v2: PowerOpsSeed 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | connectionreferences/pops\_SpareDataverse/connectionreference.yml | Connection reference 'pops\_SpareDataverse' is not used by any flow or app. | Remove the connection reference from the solution, or use it in the flow or app that needs it. |
| warning | [PO006](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO006.md) | modernflows/4c8dd007-dfcb-f111-a05c-0022480697a8/Stampworkitemstatus-4C8DD007-DFCB-F111-A05C-0022480697A8.json | Flow 'Stamp work item status' hard-codes the URL 'https:/​/contoso.sharepoint.com/sites/ops'. | Remove the URL where the platform can use the current environment, otherwise read it from an environment variable whose value is set per environment at deployment. |
| warning | [PO014](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO014.md) | canvasapps/pops\_workitems\_2af60/pops\_workitems\_2af60\_DocumentUri/Src/Screen1.pa.yaml | Canvas app 'Work Items' hard-codes the URL 'https:/​/contoso.sharepoint.com/sites/ops' in Button1.OnSelect. | Remove the URL where the platform can use the current environment, otherwise read it from an environment variable whose value is set per environment at deployment. |

### tests/fixtures/powerops-seed/v1: PowerOpsSeed 1.0.0.0

No findings.

### tests/fixtures/powerops-seed/v2: PowerOpsSeed 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'pops\_SpareDataverse' is not used by any flow or app. | Remove the connection reference from the solution, or use it in the flow or app that needs it. |
| warning | [PO006](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO006.md) | Workflows/Stampworkitemstatus-4C8DD007-DFCB-F111-A05C-0022480697A8.json | Flow 'Stamp work item status' hard-codes the URL 'https:/​/contoso.sharepoint.com/sites/ops'. | Remove the URL where the platform can use the current environment, otherwise read it from an environment variable whose value is set per environment at deployment. |

### tests/fixtures/zips/ColumnLevelSecuritySampleSolution.zip: ColumnLevelSecuritySampleSolution 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO034](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO034.md) | customizations.xml | Role 'Column-level security sample role' has Create and Write on 'SharePointData', which no component in the solution uses, so it may grant more than the solution needs. | Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't. |

### tests/fixtures/zips/ColumnLevelSecuritySampleSolution\_managed.zip: ColumnLevelSecuritySampleSolution 1.0.0.0

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO034](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO034.md) | customizations.xml | Role 'Column-level security sample role' has Create and Write on 'SharePointData', which no component in the solution uses, so it may grant more than the solution needs. | Check whether the role's users need to create or write that table, and remove those privileges from the role if they don't. |

### tests/fixtures/zips/Contoso\_sample\_1\_0\_0\_1\_managed.zip: Contoso\_sample 1.0.0.1

| Severity | Rule | File | Message | Fix |
|---|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | customizations.xml | Connection reference 'new\_D365connection' is not used by any flow or app. | Remove the connection reference from the solution, or use it in the flow or app that needs it. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/new\_ContosoURL/environmentvariablevalues.json | Environment variable 'new\_ContosoURL' ships a current value in the solution. | Remove the current value before export, and supply per-environment values with deployment settings. |
| warning | [PO010](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO010.md) | solution.xml | Publisher 'DefaultPublisherorg8a403877' (prefix 'new') is a default or generated publisher. | Create a publisher with a meaningful prefix and build the solution under it. |

### tests/fixtures/zips/polymorphiclookupexamplesolution.zip: polymorphiclookupexamplesolution 1.0.0.0

No findings.
