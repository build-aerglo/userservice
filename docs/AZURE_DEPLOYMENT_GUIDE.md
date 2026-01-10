# Azure Deployment Guide for Aerglo Microservices

A step-by-step guide for deploying .NET microservices to Azure App Service.

---

## Naming Convention

| Resource Type | Pattern | UserService | ReviewService | BusinessService |
|--------------|---------|-------------|---------------|-----------------|
| Resource Group | `rg-{service}-prod` | `rg-userservice-prod` | `rg-reviewservice-prod` | `rg-businessservice-prod` |
| App Service Plan | `asp-{service}-prod` | `asp-userservice-prod` | `asp-reviewservice-prod` | `asp-businessservice-prod` |
| Web App | `{service}-api-cc` | `userservice-api-cc` | `reviewservice-api-cc` | `businessservice-api-cc` |
| Key Vault | `kv-{service}-prod` | `kv-userservice-prod` | `kv-reviewservice-prod` | `kv-businessservice-prod` |
| App Configuration | `appconfig-{service}-prod-cc` | `appconfig-userservice-prod-cc` | `appconfig-reviewservice-prod-cc` | `appconfig-businessservice-prod-cc` |

> **Note:** Key Vault names have a 24-character limit. Abbreviate if necessary.

---

## Prerequisites

```bash
# Login to Azure
az login

# Set subscription (replace with your subscription ID)
az account set --subscription "6a94074b-8c03-416e-9d6e-dc0c6b34b70c"
```

---

## Step 1: Set Variables

Replace `SERVICE_NAME` with your service (e.g., `userservice`, `reviewservice`, `businessservice`):

```bash
SERVICE_NAME="userservice"
LOCATION="canadacentral"
RESOURCE_GROUP="rg-${SERVICE_NAME}-prod"
APP_SERVICE_PLAN="asp-${SERVICE_NAME}-prod"
WEB_APP_NAME="${SERVICE_NAME}-api-cc"
KEY_VAULT_NAME="kv-${SERVICE_NAME}-prod"
APP_CONFIG_NAME="appconfig-${SERVICE_NAME}-prod-cc"
```

---

## Step 2: Create Azure Resources

### Create Resource Group
```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### Create App Service Plan
```bash
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku B1 \
  --is-linux
```

### Create Web App
```bash
az webapp create \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:9.0"
```

### Enable Managed Identity
```bash
az webapp identity assign \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get Principal ID (save this for later)
PRINCIPAL_ID=$(az webapp identity show \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

echo "Principal ID: $PRINCIPAL_ID"
```

---

## Step 3: Create Key Vault (Optional - for secrets management)

```bash
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true
```

### Grant Permissions
```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Grant yourself Key Vault Administrator
USER_ID=$(az ad signed-in-user show --query id -o tsv)
az role assignment create \
  --role "Key Vault Administrator" \
  --assignee $USER_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME

# Grant Web App Key Vault Secrets User
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $PRINCIPAL_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME
```

### Add Secrets
```bash
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ConnectionStrings--PostgresConnection" \
  --value "<your-connection-string>"

az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Auth0--ClientSecret" \
  --value "<your-auth0-client-secret>"
```

---

## Step 4: Create App Configuration (Optional - for centralized config)

```bash
# Register provider (first time only)
az provider register --namespace Microsoft.AppConfiguration

# Create App Configuration
az appconfig create \
  --name $APP_CONFIG_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Free

# Grant Web App access
az role assignment create \
  --role "App Configuration Data Reader" \
  --assignee $PRINCIPAL_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.AppConfiguration/configurationStores/$APP_CONFIG_NAME
```

### Add Configuration Values
```bash
az appconfig kv set --name $APP_CONFIG_NAME --key "Auth0:Domain" --value "your-tenant.auth0.com" --yes
az appconfig kv set --name $APP_CONFIG_NAME --key "Auth0:Audience" --value "https://your-api.example.com" --yes
az appconfig kv set --name $APP_CONFIG_NAME --key "Auth0:ClientId" --value "<client-id>" --yes
az appconfig kv set --name $APP_CONFIG_NAME --key "EnableSwagger" --value "true" --yes
```

---

## Step 5: Configure Web App Settings

```bash
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    EnableSwagger="true"
```

### Enable HTTPS
```bash
az webapp update \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true

az webapp config set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-tls-version 1.2
```

---

## Step 6: Deploy Application

Navigate to your service API project:

```bash
cd /path/to/service/src/ServiceName.Api
```

### Build and Deploy
```bash
# Clean old files
rm -rf ./publish ./app.zip

# Build
dotnet publish -c Release -o ./publish

# Create zip
cd publish && zip -r ../app.zip . && cd ..

# Deploy
az webapp deploy \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src-path app.zip \
  --type zip \
  --clean true

# Restart
az webapp restart \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

---

## Step 7: Verify Deployment

### Check Status
```bash
az webapp show \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "state" \
  --output tsv
```

### View Logs
```bash
az webapp log tail \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

### Test Endpoints
```bash
# Swagger
curl https://${WEB_APP_NAME}.azurewebsites.net/swagger/index.html

# API endpoint
curl https://${WEB_APP_NAME}.azurewebsites.net/api/auth/social/providers
```

---

## Quick Deploy Script

Copy and run this script for quick deployments:

```bash
#!/bin/bash
SERVICE_NAME="userservice"  # Change this
RESOURCE_GROUP="rg-${SERVICE_NAME}-prod"
WEB_APP_NAME="${SERVICE_NAME}-api-cc"

cd /path/to/service/src/ServiceName.Api

rm -rf ./publish ./app.zip
dotnet publish -c Release -o ./publish
cd publish && zip -r ../app.zip . && cd ..

az webapp deploy \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src-path app.zip \
  --type zip \
  --clean true

az webapp restart \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP

echo "Deployed to: https://${WEB_APP_NAME}.azurewebsites.net"
```

---

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| 404 on Swagger | `EnableSwagger=false` | `az webapp config appsettings set --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --settings EnableSwagger="true"` |
| Key Vault Access Denied | Missing RBAC | Assign "Key Vault Secrets User" role to Web App's Managed Identity |
| App Not Starting | Config error | Check logs: `az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP` |
| RBAC Not Working | Propagation delay | Wait 1-5 minutes for RBAC to propagate |

---

## Cleanup

To delete all resources:

```bash
az group delete --name $RESOURCE_GROUP --yes --no-wait
```
