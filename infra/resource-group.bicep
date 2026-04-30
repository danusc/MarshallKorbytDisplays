targetScope = 'subscription'

param resourceGroupName string = 'MarshallKorbytDisplays'
param location string = 'westus2'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

output resourceGroupName string = resourceGroup.name
