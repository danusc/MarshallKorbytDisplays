[CmdletBinding()]
param(
    [string]$Project = 'src/MarshallDisplayRegistry/MarshallDisplayRegistry.csproj',
    [string]$StartupProject = 'src/MarshallDisplayRegistry/MarshallDisplayRegistry.csproj',
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'

if ($ConnectionString) {
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
}

dotnet ef database update --project $Project --startup-project $StartupProject
