# JSON Schemas

JSON Schemas (draft 2020-12) for the JSON that `powerops` prints. Every output has an integer `schemaVersion` as its first root property.

| Schema | Emitted by |
|---|---|
| [analyze.schema.json](analyze.schema.json) | `powerops analyze --format json` |
| [plan-status.schema.json](plan-status.schema.json) | `powerops plan status --json` |
| [plan-facts.schema.json](plan-facts.schema.json) | `powerops plan facts` (both the available and the unavailable shape) |

The baseline file (`powerops-baseline.json`) is a file format with its own `version`, not an output.

## Versioning

- Adding a field keeps `schemaVersion`. The schemas do not set `additionalProperties: false`, so a consumer built on the current schema still accepts output with new fields. Consumers should ignore fields they do not know.
- Removing or renaming a field, or changing its type or meaning, bumps `schemaVersion`.

Tests validate golden output files in `tests/golden/` against these schemas, and fail for a golden `*.json` that has no schema.
