## PowerOps diff

**PowerOpsSeed 1.0.0.0 → PowerOpsSeed 1.0.0.0**

2 changes (1 added, 0 removed, 1 changed)

- `connection-reference`: A connection reference was added or removed.
- `same-version`: The two solutions differ but carry the same version number, so importing the new one is not an upgrade: deploy plan skips it; raise the version.

| Change | Component | Name | Detail | Risk |
|---|---|---|---|---|
| added | connectionReference | pops\_SpareDataverse |  | `connection-reference` `same-version` |
| changed | workflow | Stamp work item status (4c8dd007-dfcb-f111-a05c-0022480697a8) | definition changed | `same-version` |
