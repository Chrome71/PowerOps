# Rules

`powerops analyze` checks these rules. Each rule has a page with what it finds, how to fix it and how to suppress it.

| Rule | Severity | Finds |
|---|---|---|
| [PO001](PO001.md) | error | Flow uses a connection reference that is not defined in the solution or its dependencies |
| [PO002](PO002.md) | error | Required environment variable has neither a default nor a current value |
| [PO003](PO003.md) | error | Missing dependency that no solution owns |
| [PO004](PO004.md) | error | Flow calls a child flow that is not in the solution or its dependencies |
| [PO005](PO005.md) | warning | Connection reference not used by any flow, canvas app or agent |
| [PO006](PO006.md) | warning | Flow contains a hard-coded environment or SharePoint URL |
| [PO007](PO007.md) | warning | Environment variable default looks environment-specific (URL, GUID, email) |
| [PO008](PO008.md) | warning | Environment variable current value is shipped in the solution |
| [PO009](PO009.md) | warning | Component name uses a prefix other than the publisher's |
| [PO010](PO010.md) | warning | Default or generated publisher |
| [PO011](PO011.md) | warning | Cloud flow is turned off |
| [PO012](PO012.md) | warning | Dataverse row-updated trigger has no filter columns |
| [PO013](PO013.md) | error | Root component in the YAML layout has no source folder |
| [PO014](PO014.md) | warning | Canvas app formula contains a hard-coded environment or SharePoint URL |
| [PO015](PO015.md) | error | Dataverse row-created trigger has filter columns |
| [PO016](PO016.md) | warning | Flow has more than 50 actions or more than 4 levels of nesting |
| [PO017](PO017.md) | warning | Flow runs a loop inside a loop |
| [PO018](PO018.md) | warning | Canvas app formula calls Patch or ForAll inside ForAll |
| [PO019](PO019.md) | warning | Power Pages Web API fields setting is * |
| [PO020](PO020.md) | warning | Power Pages Web API is enabled for a table with no fields setting |
| [PO021](PO021.md) | warning | Power Pages table permission has no web role |
| [PO022](PO022.md) | warning | Power Pages child table permission is not in effect through its parent |
| [PO023](PO023.md) | warning | Power Pages table permission lets anonymous users create, write or delete |
| [PO024](PO024.md) | error | Site map opens a custom page that is not in the solution or its dependencies |
| [PO025](PO025.md) | warning | Model-driven app has more than 25 custom pages, 10 connectors or 20 connection references |
| [PO026](PO026.md) | warning | Code app depends on a table or connector that is not in the solution |
| [PO027](PO027.md) | error | MCP server on an agent with generative orchestration off |
| [PO028](PO028.md) | warning | Agent instructions are empty or longer than 8,000 characters |
| [PO029](PO029.md) | warning | Agent authentication is None |
| [PO030](PO030.md) | warning | Agent tool uses the maker's credentials |
| [PO031](PO031.md) | warning | Agent has more than 30 tools |
| [PO032](PO032.md) | warning | Model-driven app is shared only with admin roles |
| [PO033](PO033.md) | warning | Custom security role has no privilege to open a model-driven app |
| [PO034](PO034.md) | warning | Security role can create or write a table that no component in the solution uses |
