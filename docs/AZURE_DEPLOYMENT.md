# Azure Deployment Guide for UserService

This guide provides step-by-step instructions for deploying the UserService to Azure with proper security configuration using Azure Key Vault and Azure App Configuration.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Step 1: Create Azure Resources](#step-1-create-azure-resources)
4. [Step 2: Configure Azure Key Vault](#step-2-configure-azure-key-vault)
5. [Step 3: Configure Azure App Configuration](#step-3-configure-azure-app-configuration)
6. [Step 4: Configure Azure Database for PostgreSQL](#step-4-configure-azure-database-for-postgresql)
7. [Step 5: Deploy to Azure App Service](#step-5-deploy-to-azure-app-service)
8. [Step 6: Configure Managed Identity](#step-6-configure-managed-identity)
9. [Step 7: Set Up CI/CD with GitHub Actions](#step-7-set-up-cicd-with-github-actions)
10. [Step 8: Configure Custom Domain and SSL](#step-8-configure-custom-domain-and-ssl)
11. [Step 9: Set Up Monitoring and Alerts](#step-9-set-up-monitoring-and-alerts)
12. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before starting, ensure you have:

- **Azure CLI** installed and authenticated (`az login`)
- **Azure Subscription** with appropriate permissions
- **.NET 9 SDK** installed locally
- **Docker** (optional, for container deployment)
- **Auth0 Account** with API configured
- **PostgreSQL Database** (Azure Database for PostgreSQL or existing Neon/other provider)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Azure Cloud                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐       │
│  │  Azure App      │────▶│  Azure App      │────▶│  Azure Key      │       │
│  │  Service        │     │  Configuration  │     │  Vault          │       │
│  │  (UserService)  │     │  (Config Store) │     │  (Secrets)      │       │
│  └────────┬────────┘     └─────────────────┘     └─────────────────┘       │
│           │                                                                  │
│           │  Managed Identity                                                │
│           ▼                                                                  │
│  ┌─────────────────┐     ┌─────────────────┐                               │
│  │  Azure Database │     │  Application    │                               │
│  │  for PostgreSQL │     │  Insights       │                               │
│  │  (or external)  │     │  (Monitoring)   │                               │
│  └─────────────────┘     └─────────────────┘                               │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  External Services: Auth0, Business Service                                 │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Step 1: Create Azure Resources

### 1.1 Set Environment Variables

```bash
# Set your preferred values
RESOURCE_GROUP="rg-userservice-prod"
LOCATION="eastus"
APP_NAME="userservice-api"
KEY_VAULT_NAME="kv-userservice-prod"
APP_CONFIG_NAME="appconfig-userservice-prod"
APP_SERVICE_PLAN="asp-userservice-prod"
```

### 1.2 Create Resource Group

```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 1.3 Create Azure Key Vault

```bash
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku standard \
  --enable-rbac-authorization true
```

### 1.4 Create Azure App Configuration

```bash
az appconfig create \
  --name $APP_CONFIG_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard
```

### 1.5 Create App Service Plan

```bash
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku B1 \
  --is-linux
```

### 1.6 Create App Service

```bash
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:9.0"
```

---

## Step 2: Configure Azure Key Vault

### 2.1 Add Secrets to Key Vault

Store all sensitive configuration in Key Vault:

```bash
# Database connection string
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ConnectionStrings--PostgresConnection" \
  --value "Host=your-db-host;Database=clereview;Username=your-user;Password=your-password;SSL Mode=Require"

# Auth0 Client Secret
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Auth0--ClientSecret" \
  --value "your-auth0-client-secret"

# Auth0 Management Client Secret
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Auth0--MgmtClientSecret" \
  --value "your-auth0-mgmt-client-secret"
```

### 2.2 List All Required Secrets

Create the following secrets in Key Vault:

| Secret Name | Description |
|------------|-------------|
| `ConnectionStrings--PostgresConnection` | PostgreSQL connection string |
| `Auth0--ClientSecret` | Auth0 application client secret |
| `Auth0--MgmtClientSecret` | Auth0 Management API client secret |

---

## Step 3: Configure Azure App Configuration

### 3.1 Add Non-Sensitive Configuration

```bash
# Auth0 Domain
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:Domain" \
  --value "your-tenant.auth0.com" \
  --yes

# Auth0 Audience
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:Audience" \
  --value "https://user-service.aerglotechnology.com" \
  --yes

# Auth0 Client ID
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:ClientId" \
  --value "your-client-id" \
  --yes

# Auth0 Management Client ID
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:MgmtClientId" \
  --value "your-mgmt-client-id" \
  --yes

# Auth0 Management Audience
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:ManagementAudience" \
  --value "https://your-tenant.auth0.com/api/v2/" \
  --yes

# Auth0 Roles
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:Roles:BusinessUser" \
  --value "rol_xxxxxxxxxx" \
  --yes

az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:Roles:SupportUser" \
  --value "rol_xxxxxxxxxx" \
  --yes

az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Auth0:Roles:EndUser" \
  --value "rol_xxxxxxxxxx" \
  --yes

# Business Service URL
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Services:BusinessServiceBaseUrl" \
  --value "https://businessservice.aerglotechnology.com" \
  --yes

# CORS Origins
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Cors:AllowedOrigins:0" \
  --value "https://aerglotechnology.com" \
  --yes

az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Cors:AllowedOrigins:1" \
  --value "https://www.aerglotechnology.com" \
  --yes

# Sentinel for refresh (change this value to trigger config refresh)
az appconfig kv set \
  --name $APP_CONFIG_NAME \
  --key "Settings:Sentinel" \
  --value "1" \
  --yes
```

### 3.2 Link Key Vault Secrets to App Configuration

Reference Key Vault secrets from App Configuration:

```bash
# Get Key Vault URI
KEY_VAULT_URI=$(az keyvault show --name $KEY_VAULT_NAME --query properties.vaultUri -o tsv)

# Create Key Vault references
az appconfig kv set-keyvault \
  --name $APP_CONFIG_NAME \
  --key "ConnectionStrings:PostgresConnection" \
  --secret-identifier "${KEY_VAULT_URI}secrets/ConnectionStrings--PostgresConnection" \
  --yes

az appconfig kv set-keyvault \
  --name $APP_CONFIG_NAME \
  --key "Auth0:ClientSecret" \
  --secret-identifier "${KEY_VAULT_URI}secrets/Auth0--ClientSecret" \
  --yes

az appconfig kv set-keyvault \
  --name $APP_CONFIG_NAME \
  --key "Auth0:MgmtClientSecret" \
  --secret-identifier "${KEY_VAULT_URI}secrets/Auth0--MgmtClientSecret" \
  --yes
```

---

## Step 4: Configure Azure Database for PostgreSQL

### Option A: Use Azure Database for PostgreSQL

```bash
# Create PostgreSQL Flexible Server
az postgres flexible-server create \
  --name "psql-userservice-prod" \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user adminuser \
  --admin-password "YourSecurePassword123!" \
  --sku-name Standard_B2s \
  --tier Burstable \
  --storage-size 32 \
  --version 15

# Create database
az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name "psql-userservice-prod" \
  --database-name userservice

# Allow Azure services
az postgres flexible-server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --name "psql-userservice-prod" \
  --rule-name "AllowAzureServices" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Option B: Use Existing External Database (Neon, etc.)

If using an external PostgreSQL provider like Neon:
1. Ensure the connection string is stored in Key Vault
2. Ensure the database allows connections from Azure App Service IP ranges
3. Use SSL Mode=Require for secure connections

---

## Step 5: Deploy to Azure App Service

### 5.1 Enable Managed Identity

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

echo "Principal ID: $PRINCIPAL_ID"
```

### 5.2 Grant Key Vault Access

```bash
# Get Key Vault resource ID
KEY_VAULT_ID=$(az keyvault show --name $KEY_VAULT_NAME --query id -o tsv)

# Grant "Key Vault Secrets User" role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Key Vault Secrets User" \
  --scope $KEY_VAULT_ID
```

### 5.3 Grant App Configuration Access

```bash
# Get App Configuration resource ID
APP_CONFIG_ID=$(az appconfig show --name $APP_CONFIG_NAME --resource-group $RESOURCE_GROUP --query id -o tsv)

# Grant "App Configuration Data Reader" role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "App Configuration Data Reader" \
  --scope $APP_CONFIG_ID
```

### 5.4 Configure App Service Settings

```bash
# Get connection strings for App Configuration and Key Vault
APP_CONFIG_CONN=$(az appconfig credential list --name $APP_CONFIG_NAME --query "[?name=='Primary Read Only'].connectionString" -o tsv)
KEY_VAULT_URI=$(az keyvault show --name $KEY_VAULT_NAME --query properties.vaultUri -o tsv)

# Set application settings
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    AZURE_APP_CONFIGURATION_CONNECTION_STRING="$APP_CONFIG_CONN" \
    AZURE_KEY_VAULT_URI="$KEY_VAULT_URI"
```

### 5.5 Deploy the Application

#### Option A: Deploy via ZIP

```bash
# Build the application
cd src/UserService.Api
dotnet publish -c Release -o ./publish

# Create ZIP
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy
az webapp deployment source config-zip \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip
```

#### Option B: Deploy via Docker

```bash
# Build and push to Azure Container Registry
ACR_NAME="acruserserviceprod"

az acr create \
  --name $ACR_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku Basic \
  --admin-enabled true

az acr build \
  --registry $ACR_NAME \
  --image userservice:latest \
  --file Dockerfile .

# Configure App Service to use container
az webapp config container set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --container-image-name "${ACR_NAME}.azurecr.io/userservice:latest" \
  --container-registry-url "https://${ACR_NAME}.azurecr.io"
```

---

## Step 6: Configure Managed Identity

The application uses `DefaultAzureCredential` which automatically uses:

1. **In Azure**: Managed Identity (no code changes needed)
2. **Locally**: Azure CLI, Visual Studio, or environment variables

### Verify Access

```bash
# Test Key Vault access
az keyvault secret show \
  --vault-name $KEY_VAULT_NAME \
  --name "Auth0--ClientSecret" \
  --query value

# Test App Configuration access
az appconfig kv show \
  --name $APP_CONFIG_NAME \
  --key "Auth0:Domain"
```

---

## Step 7: Set Up CI/CD with GitHub Actions

### 7.1 Create GitHub Actions Workflow

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: userservice-api
  AZURE_WEBAPP_PACKAGE_PATH: './publish'
  DOTNET_VERSION: '9.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --no-restore --verbosity normal

    - name: Publish
      run: dotnet publish src/UserService.Api/UserService.Api.csproj -c Release -o ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}

    - name: Login to Azure
      uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v3
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        package: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
```

### 7.2 Create Azure Service Principal

```bash
# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "github-actions-userservice" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

Add the output as `AZURE_CREDENTIALS` secret in GitHub repository settings.

---

## Step 8: Configure Custom Domain and SSL

### 8.1 Add Custom Domain

```bash
az webapp config hostname add \
  --webapp-name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname "api.aerglotechnology.com"
```

### 8.2 Configure SSL Certificate

```bash
# Create managed certificate
az webapp config ssl create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname "api.aerglotechnology.com"

# Bind certificate
az webapp config ssl bind \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --certificate-thumbprint <thumbprint> \
  --ssl-type SNI
```

### 8.3 Enforce HTTPS

```bash
az webapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true
```

---

## Step 9: Set Up Monitoring and Alerts

### 9.1 Create Application Insights

```bash
az monitor app-insights component create \
  --app "ai-userservice-prod" \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get instrumentation key
APPINSIGHTS_KEY=$(az monitor app-insights component show \
  --app "ai-userservice-prod" \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

# Add to App Service
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$APPINSIGHTS_KEY"
```

### 9.2 Create Alert Rules

```bash
# Create alert for HTTP 5xx errors
az monitor metrics alert create \
  --name "High-5xx-Errors" \
  --resource-group $RESOURCE_GROUP \
  --scopes $(az webapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query id -o tsv) \
  --condition "total Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --description "Alert when 5xx errors exceed threshold"
```

---

## Troubleshooting

### Common Issues

#### 1. "Access denied" to Key Vault

```bash
# Verify managed identity is enabled
az webapp identity show --name $APP_NAME --resource-group $RESOURCE_GROUP

# Verify role assignment
az role assignment list --assignee $PRINCIPAL_ID --scope $KEY_VAULT_ID
```

#### 2. App Configuration not loading

```bash
# Verify connection string
az webapp config appsettings list --name $APP_NAME --resource-group $RESOURCE_GROUP | grep APP_CONFIGURATION

# Check App Configuration labels match environment
az appconfig kv list --name $APP_CONFIG_NAME --label Production
```

#### 3. Database connection failures

```bash
# Test connection from Azure Cloud Shell
psql "host=psql-userservice-prod.postgres.database.azure.com dbname=userservice user=adminuser sslmode=require"

# Check firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group $RESOURCE_GROUP \
  --name "psql-userservice-prod"
```

#### 4. View Application Logs

```bash
# Enable logging
az webapp log config \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --application-logging filesystem \
  --level information

# Stream logs
az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP
```

---

## Security Checklist

Before going to production, ensure:

- [ ] All secrets are stored in Azure Key Vault
- [ ] Managed Identity is enabled (no credentials in code)
- [ ] HTTPS is enforced
- [ ] CORS is configured with specific allowed origins
- [ ] Database connections use SSL
- [ ] Application Insights is configured for monitoring
- [ ] Alerts are set up for critical metrics
- [ ] Swagger is disabled in production (`EnableSwagger=false`)
- [ ] Git repository has no committed secrets
- [ ] Auth0 credentials have been rotated after removing from code

---

## Cost Optimization

| Resource | Recommended SKU | Estimated Monthly Cost |
|----------|----------------|----------------------|
| App Service Plan | P1V3 | ~$140 |
| Azure Key Vault | Standard | ~$3 |
| Azure App Configuration | Standard | ~$38 |
| Azure Database for PostgreSQL | Standard_B2s | ~$35 |
| Application Insights | Pay-as-you-go | ~$5-20 |
| **Total** | | **~$220-240/month** |

For development/staging, use lower SKUs (B1 for App Service, Basic for DB).

---

## Environment-Specific Configuration

Use App Configuration labels for different environments:

```bash
# Production configuration
az appconfig kv set --name $APP_CONFIG_NAME --key "EnableSwagger" --value "false" --label Production --yes

# Staging configuration
az appconfig kv set --name $APP_CONFIG_NAME --key "EnableSwagger" --value "true" --label Staging --yes

# Development configuration
az appconfig kv set --name $APP_CONFIG_NAME --key "EnableSwagger" --value "true" --label Development --yes
```

The application automatically loads configuration matching `ASPNETCORE_ENVIRONMENT`.
