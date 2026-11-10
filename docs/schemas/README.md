# JSON Schemas

JSON Schemas (draft 2020-12) for the JSON that `powerops` prints. Every output has an integer `schemaVersion` as its first root property.

| Schema | Emitted by |
|---|---|
| [analyze.schema.json](analyze.schema.json) | `powerops analyze --format json` |
| [deploy-apply.schema.json](deploy-apply.schema.json) | `powerops deploy apply --format json` |
| [deploy-plan.schema.json](deploy-plan.schema.json) | `powerops deploy plan --format json` and `--save` (the plan file) |
| [diff.schema.json](diff.schema.json) | `powerops diff --format json` |
| [drift.schema.json](drift.schema.json) | `powerops drift --format json` |
| [manifest.schema.json](manifest.schema.json) | `powerops manifest --format json` |
| [plan-status.schema.json](plan-status.schema.json) | `powerops plan status --json` |
| [plan-facts.schema.json](plan-facts.schema.json) | `powerops plan facts` (both the available and the unavailable shape) |
| [rules.schema.json](rules.schema.json) | `powerops rules --json` |
| [settings-apply.schema.json](settings-apply.schema.json) | `powerops settings apply --format json` |
| [settings-validate.schema.json](settings-validate.schema.json) | `powerops settings validate --format json` |
| [test.schema.json](test.schema.json) | `powerops test --format json` |
| [flow-test.schema.json](flow-test.schema.json) | the file format of the `*.flowtest.json` files `powerops test` reads (not an output, so no `schemaVersion`). It is the one closed schema (`additionalProperties: false`): the code rejects a property that is not listed, a wrong type and a missing required property, so a test file cannot silently ignore a typo |
| [snapshot.schema.json](snapshot.schema.json) | the snapshot files `powerops drift --save` writes (a file format with its own `schemaVersion`, which moves only when the file format changes, not with the outputs above) |

The baseline file (`powerops-baseline.json`) is a file format with its own `version`, not an output. The settings file `powerops settings generate` writes is pac's deployment settings format, not an output, so it has no `schemaVersion`.

## Versioning

- Adding a field keeps `schemaVersion`. The schemas do not set `additionalProperties: false`, so a consumer built on the current schema still accepts output with new fields. Consumers should ignore fields they do not know.
- Removing or renaming a field, or changing its type or meaning, bumps `schemaVersion`.
- Adding a value to a closed list (an `enum` or `const`, such as `diff`'s `component` and `risks`) bumps `schemaVersion` too: a consumer that validates against the published schema rejects a value the schema doesn't list. This rule holds from `diff` 2 on; before it, the lists of `deploy plan`, `deploy apply` and `drift` gained values at version 1.
- Each output's version moves on its own. `diff` is at 3, `deploy plan` is at 2 and `drift` is at 2, and so is the snapshot file; every other output is at 1.

**`diff` from 1 to 2.** Version 2 added the components `agent` and `agentTool` and the risk labels `agent-tool` and `agent-instructions`. Nothing was removed or renamed, and every field keeps its type and meaning. A consumer that checks `schemaVersion` accepts 2, and one that validates the output takes the new `diff.schema.json`. The schema's `$id` points at `main`, so a consumer that fetches it there gets version 2 as soon as it merges; to stay on version 1 for a while, keep a copy of the schema from an earlier commit.

**`diff` from 2 to 3.** Version 3 added the risk label `same-version`. Nothing was removed or renamed, and every field keeps its type and meaning. A consumer that checks `schemaVersion` accepts 3, and one that validates the output takes the new `diff.schema.json`. The schema's `$id` points at `main`, so a consumer that fetches it there gets version 3 as soon as it merges; to stay on version 2 for a while, keep a copy of the schema from an earlier commit.

**`deploy plan` from 1 to 2.** Version 2 added the item kind `agent`, which names each Copilot Studio agent of a zip the plan would import. Nothing was removed or renamed, and every field keeps its type and meaning. A consumer that checks `schemaVersion` accepts 2, and one that validates the output takes the new `deploy-plan.schema.json`. `deploy apply` reads only plan files of version 2, so a plan file made by an earlier version has to be made again. The schema's `$id` points at `main`, so a consumer that fetches it there gets version 2 as soon as it merges; to stay on version 1 for a while, keep a copy of the schema from an earlier commit.

**`drift` from 1 to 2.** Version 2 added the item kinds `agent-missing` (drift) and `agent-published` (information), and the subjects of components of type 10000 and above can now read `<definition name> <schema name or id>` where both environments of a pair hold type names. Nothing was removed or renamed, and every field keeps its type and meaning. A consumer that checks `schemaVersion` accepts 2, and one that validates the output takes the new `drift.schema.json`. The schema's `$id` points at `main`, so a consumer that fetches it there gets version 2 as soon as it merges; to stay on version 1 for a while, keep a copy of the schema from an earlier commit.

**The snapshot file from 1 to 2.** Version 2 added `name` and `schemaName` on every component and the list `agents`, which hold the definition names of components of type 10000 and above and the schema names of the solution's agents and agent components. A version-1 file still loads (it has no names, so a pair with it is compared by type number and id, and the report says so). A version-2 file is refused by a powerops from before this change, which accepts version 1 only. A snapshot read without names (the solution is not installed, or a deploy target) is still written as version 1, byte for byte, so the digests and plan hashes of `deploy plan` do not move. The schema's `$id` points at `main`; a consumer that validates snapshot files takes the new `snapshot.schema.json`, which accepts both versions.

Tests validate golden output files in `tests/golden/` against these schemas, and fail for a golden `*.json` that has no schema.
