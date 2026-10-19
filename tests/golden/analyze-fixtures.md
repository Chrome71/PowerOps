## PowerOps analyze

**9 solutions, 0 errors, 33 warnings**

### tests/fixtures/alm-makers: ALMAcceleratorforMakers 0.0.0.0

| Severity | Rule | File | Message |
|---|---|---|---|
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUrl/environmentvariabledefinition.xml | Environment variable 'cat\_DependentEnvironmentUrl' has a default value that looks like a URL. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUserId/environmentvariabledefinition.xml | Environment variable 'cat\_DependentEnvironmentUserId' has a default value that looks like an email address. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_WebhookUrl/environmentvariabledefinition.xml | Environment variable 'cat\_WebhookUrl' has a default value that looks like a URL. |
| warning | [PO007](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO007.md) | environmentvariabledefinitions/cat\_clientId/environmentvariabledefinition.xml | Environment variable 'cat\_clientId' has a default value that looks like a GUID. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUrl/environmentvariablevalues.json | Environment variable 'cat\_DependentEnvironmentUrl' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_DependentEnvironmentUserId/environmentvariablevalues.json | Environment variable 'cat\_DependentEnvironmentUserId' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_DepndentEnvironmentSecretName/environmentvariablevalues.json | Environment variable 'cat\_DepndentEnvironmentSecretName' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_GitHubOrgPlanExist/environmentvariablevalues.json | Environment variable 'cat\_GitHubOrgPlanExist' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_OrgLanguage/environmentvariablevalues.json | Environment variable 'cat\_OrgLanguage' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_OrgName/environmentvariablevalues.json | Environment variable 'cat\_OrgName' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_TemplateRepoName/environmentvariablevalues.json | Environment variable 'cat\_TemplateRepoName' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_WebhookUrl/environmentvariablevalues.json | Environment variable 'cat\_WebhookUrl' ships a current value in the solution. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/cat\_clientId/environmentvariablevalues.json | Environment variable 'cat\_clientId' ships a current value in the solution. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_sharedcommondataserviceforapps\_06411' does not use the publisher prefix 'cat'. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_sharedpowerappsforappmakers\_87b9a' does not use the publisher prefix 'cat'. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/WorkflowCompleteNotification-518F39D7-F4E5-EA11-A817-000D3A56B702.json | Flow 'WorkflowCompleteNotification' is turned off. |
| warning | [PO012](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO012.md) | Workflows/DeleteProjectCleanUp-6F3FF001-D7EC-EA11-A817-000D3A1ABE26.json | Flow 'DeleteProjectCleanUp' runs on every update of 'cat\_project'; set filter columns. |
| warning | [PO012](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO012.md) | Workflows/BuildRequestCancellation-05878F85-5A13-EB11-A813-000D3AA3E77C.json | Flow 'BuildRequestCancellation' runs on every update of 'cat\_project'; set filter columns. |

### tests/fixtures/alm-sample: ALMAcceleratorSampleSolution 0.0.0.0

| Severity | Rule | File | Message |
|---|---|---|---|
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/EmailQuote-A4C44986-14D6-EC11-A7B5-000D3A1C0439.json | Flow 'Email Quote' is turned off. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/LikeQuote-90D90640-11BC-EC11-983F-0022483403A3.json | Flow 'Like Quote' is turned off. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/Smplerligforsk-37443C82-D03D-ED11-9DB0-0022482E56BE.json | Flow 'SÆmple Ø ærlig forsøk' is turned off. |

### tests/fixtures/coe-auditlogs: CenterofExcellenceAuditLogs 3.27.2

| Severity | Rule | File | Message |
|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'admin\_sharedadmin5foffice2036520management20api5f7fc9343cf45734fe\_8f08c' is not used by any flow or app. |
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'admin\_sharedcommondataserviceforapps\_8f10b' is not used by any flow or app. |
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'new\_CoEAuditLogsO365Outlook' is not used by any flow or app. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_CoEAuditLogsDataverse' does not use the publisher prefix 'admin'. |
| warning | [PO009](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO009.md) | Other/Customizations.xml | Component 'new\_CoEAuditLogsO365Outlook' does not use the publisher prefix 'admin'. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/DeprecatedAdminSyncAuditLogs-FA91974D-6E68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| Admin \| Sync Audit Logs' is turned off. |
| warning | [PO011](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO011.md) | Workflows/DeprecatedChildAdminSyncLogs-751183FC-DB68-EA11-A811-000D3A31CF04.json | Flow 'Deprecated \| \[Child\] Admin \| Sync Logs' is turned off. |

### tests/fixtures/powerops-seed/v1: PowerOpsSeed 1.0.0.0

No findings.

### tests/fixtures/powerops-seed/v2: PowerOpsSeed 1.0.0.0

| Severity | Rule | File | Message |
|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | Other/Customizations.xml | Connection reference 'pops\_SpareDataverse' is not used by any flow or app. |
| warning | [PO006](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO006.md) | Workflows/Stampworkitemstatus-4C8DD007-DFCB-F111-A05C-0022480697A8.json | Flow 'Stamp work item status' hard-codes the URL 'https://contoso.sharepoint.com/sites/ops'. |

### tests/fixtures/zips/ColumnLevelSecuritySampleSolution.zip: ColumnLevelSecuritySampleSolution 1.0.0.0

No findings.

### tests/fixtures/zips/ColumnLevelSecuritySampleSolution\_managed.zip: ColumnLevelSecuritySampleSolution 1.0.0.0

No findings.

### tests/fixtures/zips/Contoso\_sample\_1\_0\_0\_1\_managed.zip: Contoso\_sample 1.0.0.1

| Severity | Rule | File | Message |
|---|---|---|---|
| warning | [PO005](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO005.md) | customizations.xml | Connection reference 'new\_D365connection' is not used by any flow or app. |
| warning | [PO008](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO008.md) | environmentvariabledefinitions/new\_ContosoURL/environmentvariablevalues.json | Environment variable 'new\_ContosoURL' ships a current value in the solution. |
| warning | [PO010](https://github.com/Chrome71/PowerOps/blob/main/docs/rules/PO010.md) | solution.xml | Publisher 'DefaultPublisherorg8a403877' (prefix 'new') is a default or generated publisher. |

### tests/fixtures/zips/polymorphiclookupexamplesolution.zip: polymorphiclookupexamplesolution 1.0.0.0

No findings.
