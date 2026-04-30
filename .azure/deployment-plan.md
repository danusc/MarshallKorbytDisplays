# Azure Deployment Plan

> **Status:** Validated

Generated: 2026-04-30

## 1. Project Overview

**Goal:** Build Marshall Display Registry, an internal digital signage CMS-lite for managing display device URL assignments and endpoint agent check-ins.

**Path:** New Project

## 2. Requirements

| Attribute | Value |
|-----------|-------|
| Classification | Production MVP |
| Scale | Small |
| Budget | Cost-Optimized |
| Subscription | Marshall Azure Enterprise (`dcb4ce4c-4996-4752-bb86-6f7be4fa10ce`) |
| Tenant | `c0ccb9c9-d693-495d-8492-5136bb1940d6` |
| Location | `westus2` |

## 3. Components

| Component | Type | Technology | Path |
|-----------|------|------------|------|
| Marshall Display Registry | Web/API | ASP.NET Core 8 Razor Pages + API Controllers | `src/MarshallDisplayRegistry` |
| Tests | Test project | xUnit | `tests/MarshallDisplayRegistry.Tests` |
| Endpoint agent | PowerShell | Windows scheduled task agent | `agent` |
| Azure infrastructure | IaC | Bicep + Azure CLI | `infra` |

## 4. Recipe Selection

**Selected:** Bicep + Azure CLI

**Rationale:** The target is Azure App Service with Azure SQL and the prompt explicitly allows Bicep. Azure Developer CLI is not installed on the workstation, so the project will use direct Bicep deployment plus GitHub Actions.

## 5. Architecture

**Stack:** Azure App Service + Azure SQL Database

| Component | Azure Service | SKU |
|-----------|---------------|-----|
| Web/API | App Service | B1 |
| Database | Azure SQL Database | Basic |
| Secrets | Key Vault | Standard |
| Monitoring | Application Insights + Log Analytics | Workspace-based |

## 6. Security

- Admin UI uses App Service Authentication / Microsoft Entra and enforces admin group object ID `f1a3237e-1402-4f30-82a7-bfdb0c70c1aa`.
- Device APIs use per-device tokens, stored only as keyed HMAC hashes.
- App Service uses a system-assigned managed identity for Azure SQL and Key Vault.
- SQL Server Bicep uses Entra-only authentication and must not define SQL admin login/password.
- Allowed URL domains are `*.usc.edu` and `usc.korbyt.com`.

## 7. Execution Checklist

- [x] Generate solution, app, and tests
- [x] Implement data model, migrations, seed data, services
- [x] Implement admin UI and agent API
- [x] Add PowerShell endpoint agent scripts
- [x] Add Bicep infrastructure and GitHub Actions
- [x] Add README and pilot checklist
- [x] Build and test locally
- [x] Update status to Ready for Validation

## 8. Local Verification

| Check | Command | Result |
|-------|---------|--------|
| .NET tests | `dotnet test MarshallKorbytDisplays.sln` | Passed, 13 tests |
| Release tests | `dotnet test MarshallKorbytDisplays.sln --configuration Release` | Passed, 13 tests |
| Release publish | `dotnet publish src/MarshallDisplayRegistry/MarshallDisplayRegistry.csproj --configuration Release --output publish-check` | Passed |
| Main Bicep compile | `az bicep build --file infra/main.bicep` | Passed |
| Resource group Bicep compile | `az bicep build --file infra/resource-group.bicep` | Passed |
| Template validation | `az deployment group validate --resource-group MarshallKorbytDisplays --template-file infra/main.bicep --parameters infra/main.parameters.json` | Passed |
| What-if preview | `az deployment group what-if --resource-group MarshallKorbytDisplays --template-file infra/main.bicep --parameters infra/main.parameters.json --result-format ResourceIdOnly` | Passed, 11 resources to create |
| PowerShell parser | Parser over `agent/*.ps1` and `scripts/*.ps1` | Passed |
