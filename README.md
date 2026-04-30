# Marshall Display Registry

Marshall Display Registry is an internal digital signage CMS-lite for Windows public display computers. Admins assign an allowed URL to each display, and a PowerShell endpoint agent checks in, launches Google Chrome in kiosk/fullscreen mode, and reports status.

## What is included

- ASP.NET Core 8 Razor Pages admin UI.
- `/api/agent/*` device API with per-device token authentication.
- EF Core SQL Server data model and initial migration.
- Azure Bicep for App Service, Azure SQL, Key Vault, Application Insights, and Log Analytics.
- PowerShell endpoint agent, installer, uninstaller, and validation script.
- GitHub Actions workflow for build/test/deploy.

## Local development

This project intentionally uses SQL Server, not SQLite. This workstation did not have LocalDB or Docker available during implementation, so the default local path is a dev Azure SQL database.

1. Create a dev Azure SQL database or use an existing one.
2. Grant your signed-in Entra user database access.
3. Set the local connection string:

```powershell
cd W:\Scripting\Azure\MarshallKorbytDisplays
dotnet user-secrets init --project .\src\MarshallDisplayRegistry
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=tcp:<server>.database.windows.net,1433;Database=<database>;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;" --project .\src\MarshallDisplayRegistry
dotnet user-secrets set "Security:TokenHashSecret" "<long-random-dev-secret>" --project .\src\MarshallDisplayRegistry
```

4. Apply migrations and run:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef database update --project .\src\MarshallDisplayRegistry --startup-project .\src\MarshallDisplayRegistry
dotnet run --project .\src\MarshallDisplayRegistry
```

In Development, `AdminAuth:BypassLocal` is true so the admin UI can be used without App Service Authentication.

## Data model

The EF Core schema includes:

- `DisplayDevice`
- `UrlProfile`
- `DisplayAssignment`
- `DisplayCheckIn`
- `DisplayCommand`
- `DeviceCredential`
- `AuditLog`

Seed data adds two URL profiles and the pilot display `JFFVW-DSP-01`.

## Agent API examples

Register:

```powershell
$body = @{
  computerName = 'JFFVW-DSP-01'
  serialNumber = 'SERIAL'
  macAddress = '00-11-22-33-44-55'
  agentVersion = '1.0.0'
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri 'https://<app>/api/agent/register' -ContentType 'application/json' -Body $body
```

Check in:

```powershell
$headers = @{
  'X-Display-ComputerName' = 'JFFVW-DSP-01'
  'X-Display-Token' = '<token-returned-once>'
}
$body = @{
  serialNumber = 'SERIAL'
  macAddress = '00-11-22-33-44-55'
  agentVersion = '1.0.0'
  currentUrl = 'https://usc.korbyt.com'
  chromeRunning = $true
  lastError = $null
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri 'https://<app>/api/agent/checkin' -Headers $headers -ContentType 'application/json' -Body $body
```

## Azure provisioning

Target context:

- Subscription: `Marshall Azure Enterprise` / `dcb4ce4c-4996-4752-bb86-6f7be4fa10ce`
- Tenant: `c0ccb9c9-d693-495d-8492-5136bb1940d6`
- Resource group: `MarshallKorbytDisplays`
- Region: `westus2`
- Admin group object ID: `f1a3237e-1402-4f30-82a7-bfdb0c70c1aa`
- Planned DNS: `mkd.marshall.usc.edu`

Create or verify the resource group:

```powershell
az deployment sub create --location westus2 --template-file .\infra\resource-group.bicep
```

Deploy infrastructure:

```powershell
$secret = '<long-random-production-token-hash-secret>'
$me = az ad signed-in-user show --query "{id:id,name:userPrincipalName}" -o json | ConvertFrom-Json

az deployment group create `
  --resource-group MarshallKorbytDisplays `
  --template-file .\infra\main.bicep `
  --parameters `
    sqlEntraAdminObjectId=$($me.id) `
    sqlEntraAdminLogin=$($me.name) `
    sqlEntraAdminPrincipalType=User `
    entraClientId='<app-registration-client-id>' `
    tokenHashSecret=$secret
```

The SQL server uses Entra-only authentication. The Bicep intentionally does not define SQL admin login or password.

## SQL managed identity access

ARM role assignments do not grant SQL data-plane access. After App Service is provisioned, grant the app managed identity access:

```powershell
.\scripts\grant-sql-access.ps1 `
  -ResourceGroupName MarshallKorbytDisplays `
  -AppServiceName '<app-service-name>' `
  -SqlServerName '<sql-server-name>' `
  -DatabaseName MarshallDisplayRegistry `
  -GrantDdlAdmin
```

Then apply migrations:

```powershell
dotnet ef database update --project .\src\MarshallDisplayRegistry --startup-project .\src\MarshallDisplayRegistry
```

After migrations are complete, remove `db_ddladmin` if you do not want the running app identity to keep schema-change rights.

## App Service Authentication

Create a Microsoft Entra app registration for App Service Authentication and provide its client ID as `entraClientId` during Bicep deployment. App Service Authentication is enabled with anonymous requests allowed so `/api/agent/*` can use device-token auth. The admin UI enforces membership in the configured Marshall Korbyt Display group.

## GitHub Actions

The workflow is in `.github/workflows/deploy.yml`. Configure these repository secrets:

- `AZURE_CLIENT_ID` for an OIDC-enabled federated credential.
- `SQL_ENTRA_ADMIN_OBJECT_ID`
- `SQL_ENTRA_ADMIN_LOGIN`
- `ENTRA_APP_CLIENT_ID`
- `DISPLAY_TOKEN_HASH_SECRET`

Optional repository variable:

- `SQL_ENTRA_ADMIN_PRINCIPAL_TYPE`, default `User`.

## Install the Windows display agent

Run from an elevated PowerShell prompt on the display computer:

```powershell
.\agent\Install-SignageAgent.ps1 -ApiBaseUrl 'https://<app-service-hostname>' -KioskUser axistvuser
```

The installer:

- Creates `C:\ProgramData\MarshallDigitalSignage\Agent`.
- Writes `signage-agent.json`.
- Creates scheduled tasks `Marshall Signage - Launch` and `Marshall Signage - Monitor`.
- Runs initial registration.

Optional old shortcut handling:

```powershell
.\agent\Install-SignageAgent.ps1 -ApiBaseUrl 'https://<app>' -RemoveOldShortcut
```

Validate an endpoint:

```powershell
C:\ProgramData\MarshallDigitalSignage\Agent\Test-SignageAgent.ps1
```

Uninstall:

```powershell
C:\ProgramData\MarshallDigitalSignage\Agent\Uninstall-SignageAgent.ps1
```

## Known limitations

- The MVP manages URLs only.
- No playlist scheduling, content editing, screenshot capture, or advanced analytics.
- The agent runs in the interactive kiosk user session through scheduled tasks, not as a Windows service.
- Chrome current URL detection is cache-based; the agent tracks the last known URL it was asked to launch.
- Custom domain and certificate binding are placeholders in Bicep and should be completed after DNS validation.
