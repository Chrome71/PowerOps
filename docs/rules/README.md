# Rules

`powerops analyze` checks these rules. Each rule has a page with what it finds, how to fix it and how to suppress it.

| Rule | Severity | Finds |
|---|---|---|
| [PO001](PO001.md) | error | Flow uses a connection reference that is not defined in the solution or its dependencies |
| [PO002](PO002.md) | error | Required environment variable has neither a default nor a current value |
| [PO003](PO003.md) | error | Missing dependency that no solution owns |
| [PO004](PO004.md) | error | Flow calls a child flow that is not in the solution or its dependencies |
| [PO005](PO005.md) | warning | Connection reference not used by any flow or canvas app |
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
