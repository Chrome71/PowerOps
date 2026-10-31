## PowerOps drift

**PowerOpsSeed: 1 drift item**

| Position | Environment | Source | Version | Managed | Layers read |
|---|---|---|---|---|---|
| 1 | Contoso Dev | live | 1.0.0.0 | unmanaged | yes |
| 2 | Contoso Test | live | 1.0.0.0 | managed | yes |

| Position | Environment | Kind | Subject | Drift | Expected | Actual |
|---|---|---|---|---|---|---|
| 1 | Contoso Dev | also-in-solution | Stamp work item status | no |  | ContosoOther |
| 2 | Contoso Test | managed | PowerOpsSeed | no | unmanaged | managed |
| 2 | Contoso Test | unmanaged-layer | Stamp work item status | yes |  |  |
