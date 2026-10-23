#!/usr/bin/env bash
# Writes the Test deployment settings file of ContosoApp to the path in $1.
# The values come only from the environment, one GitHub secret each (TEST_CONNECTION_ID, TEST_ENVVAR_CONTOSO_SITEURL).
# The filter is fixed, so equal values give equal bytes in the plan job and the apply job, and the plan hash holds.
# The file is never printed. Add one --arg and one entry per environment variable or connection reference.
set -euo pipefail

: "${1:?usage: build-settings.sh <settings file>}"
: "${TEST_CONNECTION_ID:?TEST_CONNECTION_ID is not set}"
: "${TEST_ENVVAR_CONTOSO_SITEURL:?TEST_ENVVAR_CONTOSO_SITEURL is not set}"

jq -n --arg site "$TEST_ENVVAR_CONTOSO_SITEURL" --arg conn "$TEST_CONNECTION_ID" '{
  EnvironmentVariables: [{SchemaName: "contoso_SiteUrl", Value: $site}],
  ConnectionReferences: [{LogicalName: "contoso_sharepoint", ConnectionId: $conn, ConnectorId: "/providers/Microsoft.PowerApps/apis/shared_sharepointonline"}]
}' > "$1"
