## PowerOps deploy plan

**plan ok**

Target: Contoso Test

Plan hash: `0000000000000000000000000000000000000000000000000000000000000000`

| Order | Solution | Zip | Action | In target | Zip version | Managed | Settings |
|---|---|---|---|---|---|---|---|
| 1 | PowerOpsSeed | PowerOpsSeed.zip | upgrade | 1.0.0.0 | 1.0.0.1 | managed | PowerOpsSeed.settings.json |

| Solution | Severity | Kind | Subject | Message |
|---|---|---|---|---|
| PowerOpsSeed | warning | deletes | pops\_sharedoffice365\_6a2f1 | The upgrade deletes this connection reference because the zip no longer has it. |
| PowerOpsSeed | warning | unmanaged-layer | Notify on new work item | An unmanaged layer stays on top of the imported change. |

| Solution | Flow | Before | Expected |
|---|---|---|---|
| PowerOpsSeed | Stamp work item status | on | on |
