## What and why

<!-- Short description of the change. -->

## Risk tier

<!-- Tick one: the highest tier any changed file reaches ("Change tiers" in AGENTS.md). -->

- [ ] Tier S: docs, tests, removals or typos
- [ ] Tier M: a bounded rule, reader, command or skill edit with a short plan (at most 40 lines), touching no protected path
- [ ] Tier L: touches a protected path, with a full signed plan and a fresh review; the `protected-paths` check needs the `tier-L` label, which only the owner applies

## Checklist

- [ ] PR title is a Conventional Commit (`feat:`, `fix:`, `docs:`, ...)
- [ ] Tests added or updated
- [ ] `dotnet format --verify-no-changes` is clean
