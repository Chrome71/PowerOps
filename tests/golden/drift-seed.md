## PowerOps drift

**PowerOpsSeed: 2 drift items**

| Position | Environment | Source | Version | Managed | Layers read |
|---|---|---|---|---|---|
| 1 | Contoso Dev | snapshot | 1.0.0.0 | unmanaged | yes |
| 2 | Contoso Test | snapshot | 1.0.0.0 | managed | yes |

| Position | Environment | Kind | Subject | Drift | Expected | Actual |
|---|---|---|---|---|---|---|
| 2 | Contoso Test | managed | PowerOpsSeed | no | unmanaged | managed |
| 2 | Contoso Test | unmanaged-layer | Notify on new work item | yes |  |  |
| 2 | Contoso Test | variable-value | pops\_DefaultStatus | yes |  |  |
