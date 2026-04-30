[CmdletBinding()]
param(
    [string]$ResourceGroupName = 'MarshallKorbytDisplays',
    [Parameter(Mandatory = $true)][string]$AppServiceName,
    [Parameter(Mandatory = $true)][string]$SqlServerName,
    [string]$DatabaseName = 'MarshallDisplayRegistry',
    [switch]$GrantDdlAdmin
)

$ErrorActionPreference = 'Stop'

$extension = az extension show --name rdbms-connect 2>$null | ConvertFrom-Json
if (-not $extension) {
    az extension add --name rdbms-connect
}

$identity = az webapp identity show --resource-group $ResourceGroupName --name $AppServiceName | ConvertFrom-Json
if (-not $identity.principalId) {
    throw "App Service $AppServiceName does not have a system-assigned managed identity."
}

$escapedName = $AppServiceName.Replace("'", "''")
$ddlRole = if ($GrantDdlAdmin) { "ALTER ROLE db_ddladmin ADD MEMBER [$escapedName];" } else { "" }
$query = @"
IF NOT EXISTS (SELECT [name] FROM sys.database_principals WHERE [name] = N'$escapedName')
BEGIN
    CREATE USER [$escapedName] FROM EXTERNAL PROVIDER;
END
ALTER ROLE db_datareader ADD MEMBER [$escapedName];
ALTER ROLE db_datawriter ADD MEMBER [$escapedName];
$ddlRole
"@

az sql db query `
    --resource-group $ResourceGroupName `
    --server $SqlServerName `
    --name $DatabaseName `
    --auth-type aad `
    --querytext $query

Write-Host "Granted Azure SQL data-plane access to managed identity for $AppServiceName."
