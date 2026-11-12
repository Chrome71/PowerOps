## tests/fixtures/agents/dataverse-tools (PowerOpsAgentTools)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | demo\_Helper.shared\_commondataserviceforapps.shared-commondataser-0000001 | none |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/agents/new-ui/v1-no-learn-tool.zip (PowerOpsLiveAgent)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_microsoftlearndocsmcpserver | tier not classified: check the connector's reference page |
| shared\_workiqmcp | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_microsoftlearndocsmcpserver | cr123\_loanhelper\_abc123.cr.shared\_microsoftlearndocsmcpserver.\[id\] | none |
| shared\_workiqmcp | cr123\_draft\_def456.cr.shared\_workiqmcp.shared-workiqmcp-\[id\] | agentTool 'cr123\_draft\_def456.tool.WorkIQMCPServer\_mVi' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/agents/new-ui/v1.zip (PowerOpsLiveAgent)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_microsoftlearndocsmcpserver | tier not classified: check the connector's reference page |
| shared\_workiqmcp | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_microsoftlearndocsmcpserver | cr123\_loanhelper\_abc123.cr.shared\_microsoftlearndocsmcpserver.\[id\] | agentTool 'cr123\_loanhelper\_abc123.tool.MicrosoftLearnDocsMCP-MicrosoftLearnDocsMCPServer\_Liw' |
| shared\_workiqmcp | cr123\_draft\_def456.cr.shared\_workiqmcp.shared-workiqmcp-\[id\] | agentTool 'cr123\_draft\_def456.tool.WorkIQMCPServer\_mVi' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/agents/new-ui/v2.zip (PowerOpsLiveAgent)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_microsoftlearndocsmcpserver | tier not classified: check the connector's reference page |
| shared\_workiqmcp | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_microsoftlearndocsmcpserver | cr123\_loanhelper\_abc123.cr.shared\_microsoftlearndocsmcpserver.\[id\] | agentTool 'cr123\_loanhelper\_abc123.tool.MicrosoftLearnDocsMCP-MicrosoftLearnDocsMCPServer\_Liw' |
| shared\_workiqmcp | cr123\_draft\_def456.cr.shared\_workiqmcp.shared-workiqmcp-\[id\] | agentTool 'cr123\_draft\_def456.tool.WorkIQMCPServer\_mVi' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/alm-makers (ALMAcceleratorforMakers)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| http-action | A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow. | flow 'AssignRole' action 'HTTP'; flow 'AssignRole' action 'GetRoleId'; flow 'AssignRole' action 'GetUserId'; flow 'AssignRole' action 'Get\_Token'; flow 'CreateSolution' action 'Create\_Solution'; flow 'CreateSolution' action 'Get\_Solution\_'; flow 'CreateSolution' action 'Get\_Token'; flow 'GetAppsAndFlows' action 'Get\_Apps'; flow 'GetAppsAndFlows' action 'Get\_Token'; flow 'GetSolution' action 'Get\_Solution\_'; flow 'GetSolution' action 'Get\_Token' | https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation https://learn.microsoft.com/training/modules/http-connectors/2-webhook https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types |

### notClassified

| Connector | Note |
|---|---|
| shared\_approvals | tier not classified: check the connector's reference page |
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |
| shared\_conversionservice | tier not classified: check the connector's reference page |
| shared\_github | tier not classified: check the connector's reference page |
| shared\_office365 | tier not classified: check the connector's reference page |
| shared\_powerappsforappmakers | tier not classified: check the connector's reference page |
| shared\_powerplatformforadmins | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_approvals | cat\_ApprovalConnection | flow 'CreateProject', flow 'DeployToProd' |
| shared\_commondataserviceforapps | cat\_CDSConnection, new\_sharedcommondataserviceforapps\_06411 | flow 'AssignRole', flow 'BuildRequestCancellation', flow 'CreateEnvironment', flow 'CreateProject', flow 'DeleteProjectCleanUp', flow 'DeployToProd', flow 'DeployToStage', flow 'FetchLocalizedText', flow 'GetAppsAndFlows', flow 'LogTelemetry', flow 'ReadEnvironmentVariable', flow 'SetUpDevEvnironment', flow 'WorkflowCompleteNotification' |
| shared\_conversionservice | cat\_ContentConversionConnection | flow 'DeployToStage' |
| shared\_github | cat\_GitHub | flow 'CreateRepo', flow 'DeployToProd', flow 'DeployToStage', flow 'SetUpDevEvnironment' |
| shared\_office365 | cat\_sharedoffice365\_4ee46 | flow 'SendEmailNotification' |
| shared\_powerappsforappmakers | new\_sharedpowerappsforappmakers\_87b9a | flow 'GetAppsAndFlows' |
| shared\_powerplatformforadmins | cat\_AdminConnection | flow 'AssignRole', flow 'BuildRequestCancellation', flow 'CreateEnvironment', flow 'DeleteEnvironment', flow 'WorkflowCompleteNotification' |

### customConnectors

None.

### httpActions

| Flow | Action | Type | Endpoint | Host |
|---|---|---|---|---|
| AssignRole | HTTP | Http | expression |  |
| AssignRole | GetRoleId | Http | expression |  |
| AssignRole | GetUserId | Http | expression |  |
| AssignRole | Get\_Token | Http | literal | not shown |
| CreateSolution | Create\_Solution | Http | expression |  |
| CreateSolution | Get\_Solution\_ | Http | expression |  |
| CreateSolution | Get\_Token | Http | literal | not shown |
| GetAppsAndFlows | Get\_Apps | Http | expression |  |
| GetAppsAndFlows | Get\_Token | Http | literal | not shown |
| GetSolution | Get\_Solution\_ | Http | expression |  |
| GetSolution | Get\_Token | Http | literal | not shown |

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| AssignRole | manual | Button |
| CreateEnvironment | manual | Button |
| CreateRepo | manual | Button |
| CreateSolution | manual | Button |
| DeleteEnvironment | manual | Button |
| DeployToProd | manual | Button |
| FetchLocalizedText | manual | Button |
| GetAppsAndFlows | manual | Button |
| GetSolution | manual | Button |
| LogTelemetry | manual | Button |
| ReadEnvironmentVariable | manual | Button |
| SendEmailNotification | manual | Button |
| SetUpDevEvnironment | manual | Button |
| WorkflowCompleteNotification | manual | Http |

### childFlows

| Flow | Calls |
|---|---|
| AssignRole | FetchLocalizedText |
| AssignRole | ReadEnvironmentVariable |
| CreateEnvironment | FetchLocalizedText |
| CreateProject | FetchLocalizedText |
| CreateProject | LogTelemetry |
| CreateProject | SendEmailNotification |
| CreateProject | AssignRole |
| CreateProject | CreateSolution |
| CreateProject | CreateRepo |
| CreateProject | ReadEnvironmentVariable |
| CreateProject | CreateEnvironment |
| CreateProject | SetUpDevEvnironment |
| CreateRepo | FetchLocalizedText |
| CreateRepo | ReadEnvironmentVariable |
| CreateSolution | FetchLocalizedText |
| CreateSolution | ReadEnvironmentVariable |
| DeleteProjectCleanUp | DeleteEnvironment |
| DeployToProd | FetchLocalizedText |
| DeployToProd | LogTelemetry |
| DeployToProd | SendEmailNotification |
| DeployToStage | FetchLocalizedText |
| DeployToStage | LogTelemetry |
| DeployToStage | SendEmailNotification |
| DeployToStage | CreateEnvironment |
| DeployToStage | DeployToProd |
| DeployToStage | ReadEnvironmentVariable |
| FetchLocalizedText | ReadEnvironmentVariable |
| GetAppsAndFlows | FetchLocalizedText |
| GetAppsAndFlows | ReadEnvironmentVariable |
| GetSolution | FetchLocalizedText |
| GetSolution | ReadEnvironmentVariable |
| SendEmailNotification | FetchLocalizedText |
| SetUpDevEvnironment | ReadEnvironmentVariable |
| SetUpDevEvnironment | FetchLocalizedText |
| SetUpDevEvnironment | SendEmailNotification |
| WorkflowCompleteNotification | FetchLocalizedText |
| WorkflowCompleteNotification | LogTelemetry |
| WorkflowCompleteNotification | ReadEnvironmentVariable |
| WorkflowCompleteNotification | GetAppsAndFlows |
| WorkflowCompleteNotification | GetSolution |
| WorkflowCompleteNotification | SendEmailNotification |

### notRead

None.

## tests/fixtures/alm-sample (ALMAcceleratorSampleSolution)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| custom-connector | Custom connectors are premium: an app's users need Power Apps Premium or per app, and a flow needs Power Automate Premium or a Process licence. | cat\_5Fenvironmentvariableconnector | https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types |
| dataverse-in-canvas-app | A canvas app that uses Dataverse needs Power Apps Premium or per app for its users. | cat\_devopskitsamplecanvasapp\_c7ec5 | https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq |
| model-driven-app | A model-driven app is premium: its users need Power Apps Premium or per app. | cat\_AlmAcceleratorSampleMda | https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq |

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |
| shared\_logicflows | tier not classified: check the connector's reference page |
| shared\_msnweather | tier not classified: check the connector's reference page |
| shared\_office365 | tier not classified: check the connector's reference page |
| shared\_sharepointonline | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| cat\_5fenvironmentvariableconnector | none | canvasApp 'cat\_devopskitsamplecanvasapp\_c7ec5' |
| shared\_commondataserviceforapps | cat\_CDS\_Current | canvasApp 'cat\_devopskitsamplecanvasapp\_c7ec5', flow 'Like Quote' |
| shared\_logicflows | none | canvasApp 'cat\_devopskitsamplecanvasapp\_c7ec5' |
| shared\_msnweather | cat\_sharedmsnweather\_9d437 | canvasApp 'cat\_devopskitsamplecanvasapp\_c7ec5', flow 'Get Current Temperature' |
| shared\_office365 | cat\_sharedoffice365\_4ee46 | flow 'Email Quote', flow 'SÆmple Ø ærlig forsøk' |
| shared\_sharepointonline | none | canvasApp 'cat\_devopskitsamplecanvasapp\_c7ec5' |

### customConnectors

| Name | In solution |
|---|---|
| cat\_5Fenvironmentvariableconnector | yes |

### httpActions

None.

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| App Title | manual | PowerApp |
| Email Quote | manual | Button |
| Get Current Temperature | manual | PowerAppV2 |
| GetEnvironmentVariableValues | manual | PowerApp |
| Like Quote | manual | PowerAppV2 |
| SÆmple Ø ærlig forsøk | manual | Button |

### childFlows

| Flow | Calls |
|---|---|
| Email Quote | App Title |
| Like Quote | Email Quote |

### notRead

None.

## tests/fixtures/coe-auditlogs (CenterofExcellenceAuditLogs)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| custom-connector | Custom connectors are premium: an app's users need Power Apps Premium or per app, and a flow needs Power Automate Premium or a Process licence. | admin\_5Foffice-20365-20management-20api | https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types |

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |
| shared\_office365 | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| admin\_5foffice-20365-20management-20api | admin\_sharedadmin5foffice2036520management20api5f7fc9343cf45734fe\_8f08c, admin\_sharedadmin5foffice2036520management20api5ff18d20b7ec3dbffe\_a3e3b | flow 'Deprecated \| \[Child\] Admin \| Sync Logs' |
| shared\_commondataserviceforapps | admin\_sharedcommondataserviceforapps\_8f10b, admin\_sharedcommondataserviceforapps\_98924, new\_CoEAuditLogsDataverse | flow 'Deprecated \| Admin \| Sync Audit Logs', flow 'Deprecated \| \[Child\] Admin \| Sync Logs' |
| shared\_office365 | new\_CoEAuditLogsO365Outlook | none |

### customConnectors

| Name | In solution |
|---|---|
| admin\_5Foffice-20365-20management-20api | no |

### httpActions

None.

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| Deprecated \| \[Child\] Admin \| Sync Logs | manual | Button |

### childFlows

| Flow | Calls |
|---|---|
| Deprecated \| Admin \| Sync Audit Logs | Deprecated \| \[Child\] Admin \| Sync Logs |
| Deprecated \| Admin \| Sync Audit Logs | a flow not in this solution |
| Deprecated \| \[Child\] Admin \| Sync Logs | a flow not in this solution |

### notRead

None.

## tests/fixtures/core-portal (msgov\_coreportal)

### licenceDrivers

None.

### notClassified

None.

### connectors

None.

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/equipment-checkout (EquipmentCheckout)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| model-driven-app | A model-driven app is premium: its users need Power Apps Premium or per app. | equipment\_checkout\_a1a05935 | https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq |

### notClassified

None.

### connectors

None.

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/flow-limits (PowerOpsFlowLimits)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| http-action | A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow. | flow 'Quarterly cleanup' action 'Get\_status' | https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation https://learn.microsoft.com/training/modules/http-connectors/2-webhook https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types |

### notClassified

| Connector | Note |
|---|---|
| shared\_approvals | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_approvals | demo\_FlowApprovals | flow 'Request long approval' |

### customConnectors

None.

### httpActions

| Flow | Action | Type | Endpoint | Host |
|---|---|---|---|---|
| Quarterly cleanup | Get\_status | Http | literal | not shown |

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| Request long approval | manual | Button |

### childFlows

None.

### notRead

None.

## tests/fixtures/flow-navigation (PowerOpsNavigation)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | demo\_FlowDataverse | flow 'Review expenses' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| Review expenses | manual | Button |

### childFlows

None.

### notRead

None.

## tests/fixtures/flow-secrets (PowerOpsFlowSecrets)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| http-action | A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow. | flow 'Call partner API' action 'Call\_partner' | https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation https://learn.microsoft.com/training/modules/http-connectors/2-webhook https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types |

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | demo\_Dataverse | flow 'Call partner API' |

### customConnectors

None.

### httpActions

| Flow | Action | Type | Endpoint | Host |
|---|---|---|---|---|
| Call partner API | Call\_partner | Http | literal | not shown |

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| Call partner API | manual | Button |

### childFlows

None.

### notRead

None.

## tests/fixtures/flowtests/capture/solution (PowerOpsCapture)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | pops\_CaptureDataverse | flow 'PowerOps capture' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| PowerOps capture | manual | Button |

### childFlows

None.

### notRead

None.

## tests/fixtures/flowtests/skipped-body (PowerOpsSkippedBody)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| http-action | A flow with a premium connector, such as HTTP, needs Power Automate Premium for its users or a Process licence for the flow. | flow 'Skipped body' action 'Get\_status' | https://learn.microsoft.com/power-apps/maker/canvas-apps/license-designation https://learn.microsoft.com/training/modules/http-connectors/2-webhook https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types |

### notClassified

None.

### connectors

None.

### customConnectors

None.

### httpActions

| Flow | Action | Type | Endpoint | Host |
|---|---|---|---|---|
| Skipped body | Get\_status | Http | literal | not shown |

### requestTriggers

| Flow | Trigger | Kind |
|---|---|---|
| Skipped body | manual | Button |

### childFlows

None.

### notRead

None.

## tests/fixtures/powerops-seed-yaml/v1 (PowerOpsSeed)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | pops\_SeedDataverse | flow 'Stamp work item status' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

| Not read |
|---|
| canvas apps, model-driven apps and agents are not read in the YAML layout, so dataverse-in-canvas-app, model-driven-app and their users are not reported |

## tests/fixtures/powerops-seed-yaml/v2 (PowerOpsSeed)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | pops\_SeedDataverse, pops\_SpareDataverse | flow 'Stamp work item status' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

| Not read |
|---|
| canvas apps, model-driven apps and agents are not read in the YAML layout, so dataverse-in-canvas-app, model-driven-app and their users are not reported |

## tests/fixtures/powerops-seed/v1 (PowerOpsSeed)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| dataverse-in-canvas-app | A canvas app that uses Dataverse needs Power Apps Premium or per app for its users. | pops\_workitems\_2af60 | https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq |

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | pops\_SeedDataverse | flow 'Stamp work item status' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/powerops-seed/v2 (PowerOpsSeed)

### licenceDrivers

| Driver | Needs | Where | Sources |
|---|---|---|---|
| dataverse-in-canvas-app | A canvas app that uses Dataverse needs Power Apps Premium or per app for its users. | pops\_workitems\_2af60 | https://learn.microsoft.com/power-platform/admin/powerapps-licensing-faq |

### notClassified

| Connector | Note |
|---|---|
| shared\_commondataserviceforapps | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_commondataserviceforapps | pops\_SeedDataverse, pops\_SpareDataverse | flow 'Stamp work item status' |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/zips/ColumnLevelSecuritySampleSolution.zip (ColumnLevelSecuritySampleSolution)

### licenceDrivers

None.

### notClassified

None.

### connectors

None.

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/zips/ColumnLevelSecuritySampleSolution\_managed.zip (ColumnLevelSecuritySampleSolution)

### licenceDrivers

None.

### notClassified

None.

### connectors

None.

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/zips/Contoso\_sample\_1\_0\_0\_1\_managed.zip (Contoso\_sample)

### licenceDrivers

None.

### notClassified

| Connector | Note |
|---|---|
| shared\_dynamicscrmonline | tier not classified: check the connector's reference page |

### connectors

| Connector | References | Users |
|---|---|---|
| shared\_dynamicscrmonline | new\_D365connection | none |

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

## tests/fixtures/zips/polymorphiclookupexamplesolution.zip (polymorphiclookupexamplesolution)

### licenceDrivers

None.

### notClassified

None.

### connectors

None.

### customConnectors

None.

### httpActions

None.

### requestTriggers

None.

### childFlows

None.

### notRead

None.

Drivers come from a closed set with sources; a solution with no driver listed may still need a premium licence.
