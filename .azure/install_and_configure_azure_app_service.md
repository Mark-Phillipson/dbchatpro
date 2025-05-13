# Install and Configure Azure App Service 
 If you need to access subscriptions in the following tenants, please use `az login --tenant TENANT_ID`.
09d59ec9-9c33-4f7a-8453-dac88d692c93 'Boston Academic Consulting Group Inc.'
1205a1ac-a274-425f-a6a7-d025773d6c24 'Default Directory'
15260f7d-cfb2-4318-ad41-ae696a0fbc71 'MSPs via Upwork'

# Clear any previous login sessions and cache
az logout
az account clear

# Non-interactive login (avoids browser prompt issues)
# First, create a service principal and save credentials
# az ad sp create-for-rbac --name "dbchatpro-deploy" --role contributor --output json > auth.json

# Login directly with the specific tenant and subscription ID
az login --tenant 09d59ec9-9c33-4f7a-8453-dac88d692c93 --allow-no-subscriptions

# Select the BACG subscription directly by ID (visible in your terminal)
az account set --subscription a332e2e6-33c9-4829-a8f3-58c8dcaeb3cd

# Set variables
RESOURCE_GROUP="dbchatpro-rg"
LOCATION="eastus"
APP_SERVICE_PLAN="dbchatpro-plan"
WEB_APP_NAME="boston-academic-dbchatpro-app"  # This must be globally unique

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service plan
az appservice plan create --name $APP_SERVICE_PLAN --resource-group $RESOURCE_GROUP --sku B1

# Create Web App
az webapp create --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --plan $APP_SERVICE_PLAN

# Deploy your application (choose one method below)

# Option 1: Deploy from local Git repository
# az webapp deployment source config-local-git --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP

# Option 2: Deploy from a GitHub repository
# az webapp deployment source config --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --repo-url https://github.com/yourusername/yourrepo --branch main

# Option 3: Deploy using ZIP deployment
# zip -r app.zip .
# az webapp deployment source config-zip --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME --src app.zip