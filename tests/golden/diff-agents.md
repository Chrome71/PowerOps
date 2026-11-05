## PowerOps diff

**PowerOpsSeed 1.0.0.0 → PowerOpsSeed 1.0.0.0**

3 changes (1 added, 0 removed, 2 changed)

- `agent-instructions`: An agent's instructions changed.
- `agent-tool`: An agent tool was added or removed.
- `same-version`: The two solutions differ but carry the same version number, so importing the new one is not an upgrade: deploy plan skips it; raise the version.

| Change | Component | Name | Detail | Risk |
|---|---|---|---|---|
| changed | agent | demo\_agent | instructions changed | `agent-instructions` `same-version` |
| changed | agentTool | demo\_agent.topic.Orders | mode: Invoker → Maker | `same-version` |
| added | agentTool | demo\_agent.topic.Refunds |  | `agent-tool` `same-version` |
