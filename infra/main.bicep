param location string = resourceGroup().location
param namePrefix string = 'mkd'
param sqlDatabaseName string = 'MarshallDisplayRegistry'
param adminGroupObjectId string = 'f1a3237e-1402-4f30-82a7-bfdb0c70c1aa'
param allowedDomains string = '*.usc.edu,usc.korbyt.com'
param defaultPollSeconds int = 300
param launchMode string = 'kiosk'
param customDomainName string = 'mkd.marshall.usc.edu'
param entraClientId string = '00000000-0000-0000-0000-000000000000'
param sqlEntraAdminObjectId string
param sqlEntraAdminLogin string
@allowed([
  'User'
  'Group'
  'Application'
])
param sqlEntraAdminPrincipalType string = 'User'
@secure()
param tokenHashSecret string

var suffix = uniqueString(resourceGroup().id)
var appServicePlanName = '${namePrefix}-asp-${suffix}'
var appServiceName = '${namePrefix}-app-${suffix}'
var sqlServerName = '${namePrefix}-sql-${suffix}'
var keyVaultName = take('${namePrefix}-kv-${suffix}', 24)
var workspaceName = '${namePrefix}-law-${suffix}'
var appInsightsName = '${namePrefix}-appi-${suffix}'
var tokenSecretName = 'display-token-hash-secret'
var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6d')

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

resource tokenSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: tokenSecretName
  properties: {
    value: tokenHashSecret
  }
}

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: sqlEntraAdminPrincipalType
      login: sqlEntraAdminLogin
      sid: sqlEntraAdminObjectId
      tenantId: tenant().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;'
        }
        {
          name: 'KeyVaultUri'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'Security__TokenHashSecret'
          value: '@Microsoft.KeyVault(SecretUri=${tokenSecret.properties.secretUriWithVersion})'
        }
        {
          name: 'Security__TokenHashSecretName'
          value: tokenSecretName
        }
        {
          name: 'Signage__AllowedDomains__0'
          value: split(allowedDomains, ',')[0]
        }
        {
          name: 'Signage__AllowedDomains__1'
          value: split(allowedDomains, ',')[1]
        }
        {
          name: 'Signage__DefaultPollSeconds'
          value: string(defaultPollSeconds)
        }
        {
          name: 'Signage__LaunchMode'
          value: launchMode
        }
        {
          name: 'Signage__AutoEnableNewDevices'
          value: 'false'
        }
        {
          name: 'Signage__AdminGroupObjectId'
          value: adminGroupObjectId
        }
        {
          name: 'AdminAuth__BypassLocal'
          value: 'false'
        }
        {
          name: 'SeedData__Enabled'
          value: 'true'
        }
      ]
    }
  }
}

resource appAuth 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: appService
  name: 'authsettingsV2'
  properties: {
    platform: {
      enabled: true
      runtimeVersion: '~1'
    }
    globalValidation: {
      requireAuthentication: false
      unauthenticatedClientAction: 'AllowAnonymous'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: entraClientId
          openIdIssuer: '${environment().authentication.loginEndpoint}${tenant().tenantId}/v2.0'
        }
        validation: {
          allowedAudiences: [
            'api://${entraClientId}'
          ]
        }
      }
    }
    login: {
      tokenStore: {
        enabled: true
      }
    }
  }
}

resource keyVaultSecretUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, appService.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: appService.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output appServiceName string = appService.name
output appServiceDefaultHostName string = appService.properties.defaultHostName
output appServicePrincipalId string = appService.identity.principalId
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output keyVaultName string = keyVault.name
output customDomainPlaceholder string = customDomainName
