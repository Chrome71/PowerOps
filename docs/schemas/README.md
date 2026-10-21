# JSON Schemas

JSON Schemas (draft 2020-12) for the JSON that `powerops` prints. Every output has an integer `schemaVersion` as its first root property.

| Schema | Emitted by |
|---|---|
| [analyze.schema.json](analyze.schema.json) | `powerops analyze --format json` |
| [diff.schema.json](diff.schema.json) | `powerops diff --format json` |
| [plan-status.schema.json](plan-status.schema.json) | `powerops plan status --json` |
| [plan-facts.schema.json](plan-facts.schema.json) | `powerops plan facts` (both the available and the unavailable shape) |
| [rules.schema.json](rules.schema.json) | `powerops rules --json` |
| [settings-apply.schema.json](settings-apply.schema.json) | `powerops settings apply --format json` |
| [settings-validate.schema.json](settings-validate.schema.json) | `powerops settings validate --format json` |

The baseline file (`powerops-baseline.json`) is a file format with its own `version`, not an output. The settings file `powerops settings generate` writes is pac's deployment settings format, not an output, so it has no `schemaVersion`.

## Versioning

- Adding a field keeps `schemaVersion`. The schemas do not set `additionalProperties: false`, so a consumer built on the current schema still accepts output with new fields. Consumers should ignore fields they do not know.
- Removing or renaming a field, or changing its type or meaning, bumps `schemaVersion`.

Tests validate golden output files in `tests/golden/` against these schemas, and fail for a golden `*.json` that has no schema.
