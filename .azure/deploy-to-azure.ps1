# Install the Az module if not already installed
if (-not (Get-Module -ListAvailable -Name Az)) {
    Install-Module -Name Az -AllowClobber -Scope CurrentUser
}

# Connect to Azure
Connect-AzAccount -TenantId "09d59ec9-9c33-4f7a-8453-dac88d692c93"

# Select subscription
Select-AzSubscription -SubscriptionId "a332e2e6-33c9-4829-a8f3-58c8dcaeb3cd"

# Set variables
$resourceGroup = "dbchatpro-rg"
$location = "eastus"
$appServicePlan = "dbchatpro-plan"
$webAppName = "boston-academic-dbchatpro-app"

# Create resource group
New-AzResourceGroup -Name $resourceGroup -Location $location -Force

# Create App Service plan
New-AzAppServicePlan -Name $appServicePlan -Location $location -ResourceGroupName $resourceGroup -Tier Basic -WorkerSize Small

# Create Web App
New-AzWebApp -Name $webAppName -Location $location -ResourceGroupName $resourceGroup -AppServicePlan $appServicePlan

Write-Host "Deployment completed successfully. Your web app URL is: https://$webAppName.azurewebsites.net"
