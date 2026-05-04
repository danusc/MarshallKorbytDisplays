[CmdletBinding()]
param(
    [string]$ResourceGroupName = 'MarshallKorbytDisplays',
    [Parameter(Mandatory = $true)][string]$AppServiceName,
    [Parameter(Mandatory = $true)][string]$SqlServerName,
    [string]$DatabaseName = 'MarshallDisplayRegistry',
    [switch]$GrantDdlAdmin
)

$ErrorActionPreference = 'Stop'

$identity = az webapp identity show --resource-group $ResourceGroupName --name $AppServiceName | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw "Failed to read App Service managed identity for $AppServiceName."
}

if (-not $identity.principalId) {
    throw "App Service $AppServiceName does not have a system-assigned managed identity."
}

$accessToken = az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($accessToken)) {
    throw 'Failed to acquire an Azure SQL access token from Azure CLI.'
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

$connectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Database=$DatabaseName;Encrypt=True;TrustServerCertificate=False;"
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.AccessToken = $accessToken

try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    $command.CommandTimeout = 60
    [void]$command.ExecuteNonQuery()
}
finally {
    $connection.Dispose()
}

Write-Host "Granted Azure SQL data-plane access to managed identity for $AppServiceName."
