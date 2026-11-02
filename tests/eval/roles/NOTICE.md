# Notice: the role evaluation set

Everything under `tests/eval/roles/` was made for this project and is licensed under the repository's Apache-2.0 licence. No third-party content is copied into it.

## Where the cases come from

- The plan cases (`cases/plan-*`) are plans and decision logs written for this project, in the layout `powerops-plan` writes. The projects, organisations and numbers in them are invented. They hold no real tenant, person or organisation.
- The solution cases (`cases/solution-*`) start from `tests/fixtures/powerops-seed/v1` (their `solution/previous`, kept byte for byte), a copy of it with a higher version and the change of the case's plan (`solution/current`), a settings file that begins as `tests/fixtures/settings/powerops-seed-v1.valid.json`, and the flow test `tests/fixtures/flowtests/seed/stamp-work-item-status-active.flowtest.json`. The seed's own component ids stay.
- What the cases add to the seed is hand-written in the layouts that `pac solution unpack` writes: security roles, field security profiles, one table column, environment variable definitions, connection references, cloud flows and flow tests. None of it was exported from a tenant, so the layouts are close approximations.
- Each solution case's plan was signed with `powerops plan sign-off`, and then only its `status` line was changed to `implementing`, which keeps the hash.
- `baseline/` holds the output of `powerops analyze`, `test`, `diff` and `settings validate`, run from each solution case's folder with relative paths. It is not part of any case.
- `pilot/` holds one plan case and one solution case with one flaw each, outside the 18 cases of the set.

## Fake values

The values that look like tenant data are fake:

- URLs on `contoso.example`.
- Email addresses on `example.invalid`.
- GUIDs made of zeros. Component ids that the cases add are patterned the same way, for example `00000000-0000-0000-0000-000000000f11`, and the seed's own ids are kept.
- Connection ids such as `fake-connection-id`, and channel names such as `supervisors-channel-test`.
- Token strings made of zeros, such as `wh-0000-0000-0000-0000`, and a vault reference whose subscription id is all zeros.
- The mock value `test-token` in flow tests.
