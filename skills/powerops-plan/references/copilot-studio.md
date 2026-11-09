# Copilot Studio agents

Read this when a plan names a Copilot Studio agent. Write the answers in `Users`, `Automation`, `Environments and route`, `Solution and publisher` and `Constraints`, and the review items under `Open questions` until they are answered.

## 1. The harness first

- **Name the harness and why.** The standard harness runs rule-based agents with topics and predictable answers; the GitHub Copilot harness reasons through multi-step work; the Customer Conversation harness serves customers over voice and chat. An agent can't move between the GitHub Copilot harness and the standard one later: an upgrade (preview) copies a standard agent to the GitHub Copilot harness, without its knowledge sources, Power Fx or child agents.
- **Prefer the standard harness** unless the job needs another. This is PowerOps's advice, not Microsoft's: Learn calls the GitHub Copilot harness the most capable and says neither harness is better for every agent. PowerOps's agent checks (`analyze`, PO027 to PO031) read agents on the standard harness only, and say "not checked" for any other.
- **Microsoft's tooling.** `copilot-studio-plugin`, the successor of skills-for-copilot-studio, builds for the GitHub Copilot harness, and its `migrate` command converts an agent from the classic (standard) harness. A plan that uses either names the harness change and that PowerOps doesn't check the result.
- **The orchestration.** Generative or classic. An MCP tool needs generative orchestration.

## 2. The plan

- **Authentication.** Authenticate with Microsoft, or manually. No authentication only for a public agent, with its reason: anyone with the link can chat with it.
- **Tool credentials.** End-user credentials are the default. Name each tool that uses the maker's credentials and why every user may act with the maker's permissions. Tools a trigger calls must use the maker's credentials.
- **Tools.** One line per tool with its Goal line: connector, flow, MCP server, prompt or connected agent. Keep each agent to 25 to 30 tools, and move groups to child agents beyond that; PO031 still counts a child agent's tools with the agent's. An MCP server's tools change with the server, so name the server and who owns it.
- **Instructions.** Who writes them, in at most 8,000 characters.
- **Knowledge.** One line per source with its Goal line.
- **The route to Test.** The agent goes in the plan's solution. `pac copilot pack` makes a solution from a `.mcs.yml` workspace locally, and `analyze` reads that solution, not the workspace. `pac copilot init` with `--environment`, or with an auth profile active (its help says the flag then defaults to the profile's environment), `push` and `publish`, and Apply in VS Code, write to the environment: Dev only, each with the user's yes. For a local scaffold, run `init` with no auth profile.

## 3. MCP apps

MCP apps (widgets) are documented for declarative agents in Microsoft 365 Copilot, and App MCP in model-driven apps is in preview and needs a Microsoft 365 Copilot licence. Nothing shows a standard Copilot Studio agent rendering them. When a plan needs a widget, hand it to Microsoft's `mcp-apps` plugin and write the route in `Screens`; PowerOps has no checks for it.

## 4. Review items

Ask these in the review; offline checks can't see them:

- Each MCP connector names the Streamable transport. Copilot Studio no longer supports SSE for MCP.
- Knowledge within the limits: up to 25 websites and 25 SharePoint URLs with generative orchestration, four of each with classic. With more than 25 sources, uploaded files aside, generative orchestration filters them with a model.
- A clear, distinct description on every tool, topic, child agent and knowledge source: the orchestrator selects by description first.

## Sources

Checked on 2026-11-03:

- [Harnesses in Copilot Studio](https://learn.microsoft.com/microsoft-copilot-studio/harnesses-overview) and the [GitHub Copilot harness overview](https://learn.microsoft.com/microsoft-copilot-studio/agents-experience/overview): the three harnesses, and no transfer between the GitHub Copilot harness and the standard one.
- [Extend your agent with MCP](https://learn.microsoft.com/microsoft-copilot-studio/agent-extend-action-mcp) and [Connect to an existing MCP server](https://learn.microsoft.com/microsoft-copilot-studio/mcp-add-existing-server-to-agent): generative orchestration for MCP, server-driven tools, the Streamable transport.
- [Configure user authentication](https://learn.microsoft.com/microsoft-copilot-studio/configuration-end-user-authentication) and [Automatic security scan](https://learn.microsoft.com/microsoft-copilot-studio/security-scan): No authentication and maker-provided credentials.
- [FAQ for using generative orchestration](https://learn.microsoft.com/microsoft-copilot-studio/faqs-generative-orchestration): triggers use the maker's credentials.
- [Add tools to custom agents](https://learn.microsoft.com/microsoft-copilot-studio/add-tools-custom-agent): 128 tools, 25 to 30 advised. [Create and delete agents](https://learn.microsoft.com/microsoft-copilot-studio/authoring-first-bot): 8,000 characters.
- [Knowledge sources summary](https://learn.microsoft.com/microsoft-copilot-studio/knowledge-copilot-studio) and [Orchestrate agent behavior with generative AI](https://learn.microsoft.com/microsoft-copilot-studio/advanced-generative-actions): the per-type limits, the 25-source filter, and descriptions.
- [pac copilot](https://learn.microsoft.com/power-platform/developer/cli/reference/copilot): `pack` is local; `init --environment` imports a solution, and the flag defaults to the auth profile's environment.
- [Add MCP apps to declarative agents](https://learn.microsoft.com/microsoft-365/copilot/extensibility/plugin-mcp-apps) and [Enable your app and custom tools in Microsoft Copilot](https://learn.microsoft.com/power-apps/maker/model-driven-apps/enable-your-app-copilot): where MCP apps render; App MCP is in preview and needs a Microsoft 365 Copilot licence.
- [mcp-apps plugin](https://github.com/microsoft/power-platform-skills/tree/da8ea7a/plugins/mcp-apps) (MIT) at `da8ea7a`.

Checked on 2026-11-05:

- [Harnesses in Copilot Studio](https://learn.microsoft.com/microsoft-copilot-studio/harnesses-overview) and [Choose a harness](https://learn.microsoft.com/microsoft-copilot-studio/guidance/choose-harness): the GitHub Copilot harness "the most capable option", and neither universally better.
- [Upgrade to the GitHub Copilot harness](https://learn.microsoft.com/microsoft-copilot-studio/upgrade-to-github-copilot-from-standard-harness): a preview copy, without knowledge sources, Power Fx or child agents.
- [copilot-studio-plugin](https://github.com/microsoft/copilot-studio-plugin/tree/4014ae689d1228d86cb3aaab938b1e66f8e2e10d) (MIT) at `4014ae6`: the successor line in its README and `commands/migrate.md`.
