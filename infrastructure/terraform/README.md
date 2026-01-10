# Terraform Infrastructure for Aerglo Microservices

This directory contains Terraform configurations for deploying Aerglo microservices to Azure.

## Architecture

```
infrastructure/terraform/
├── main.tf                 # Main configuration, service module instantiation
├── variables.tf            # Input variables
├── outputs.tf              # Output values
├── environments/
│   └── prod/
│       ├── userservice.tfvars          # UserService configuration
│       ├── reviewservice.tfvars.example # ReviewService template
│       └── businessservice.tfvars.example # BusinessService template
└── modules/
    └── service/            # Reusable service module
        ├── main.tf         # Resource definitions
        ├── variables.tf    # Module variables
        └── outputs.tf      # Module outputs
```

## Resources Created Per Service

| Resource | Name Pattern | Description |
|----------|-------------|-------------|
| Resource Group | `rg-{service}-{env}` | Contains all service resources |
| App Service Plan | `asp-{service}-{env}` | Linux hosting plan |
| Web App | `{service}-api-cc` | .NET application |
| Key Vault | `kv-{service}-{env}` | Secrets storage (optional) |
| App Configuration | `appconfig-{service}-{env}-cc` | Centralized config (optional) |

## Prerequisites

1. **Azure CLI**: Install from [aka.ms/installazurecli](https://aka.ms/installazurecli)
2. **Terraform**: Version >= 1.5.0
3. **Azure Subscription**: With appropriate permissions

## Quick Start

### 1. Login to Azure

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

### 2. Initialize Terraform

```bash
cd infrastructure/terraform
terraform init
```

### 3. Plan Changes

```bash
terraform plan -var-file=environments/prod/userservice.tfvars
```

### 4. Apply Changes

```bash
terraform apply -var-file=environments/prod/userservice.tfvars
```

## Adding a New Service

1. **Copy the example tfvars file**:
   ```bash
   cp environments/prod/reviewservice.tfvars.example environments/prod/reviewservice.tfvars
   ```

2. **Edit the configuration**:
   ```hcl
   services = [
     {
       name              = "reviewservice"
       app_service_sku   = "B1"
       dotnet_version    = "9.0"
       always_on         = false
       health_check_path = "/health"
       create_key_vault  = true
       create_app_config = true
       enable_swagger    = true

       secrets = {
         # Add secrets here
       }

       config_values = {
         "EnableSwagger" = "true"
         # Add config values here
       }
     }
   ]
   ```

3. **Deploy**:
   ```bash
   terraform apply -var-file=environments/prod/reviewservice.tfvars
   ```

## CI/CD Integration

### GitHub Actions Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `terraform-plan.yml` | PR to main | Validates and plans changes |
| `terraform-apply.yml` | Push to main | Applies infrastructure changes |
| `deploy-app.yml` | Manual | Builds and deploys application |

### Setting Up GitHub Secrets

1. **Create Azure Service Principal** (for OIDC):
   ```bash
   az ad sp create-for-rbac --name "github-terraform-sp" \
     --role contributor \
     --scopes /subscriptions/YOUR_SUBSCRIPTION_ID \
     --sdk-auth
   ```

2. **Configure Federated Credentials** (for OIDC - recommended):
   ```bash
   # Create federated credential for main branch
   az ad app federated-credential create \
     --id YOUR_APP_ID \
     --parameters '{
       "name": "github-main",
       "issuer": "https://token.actions.githubusercontent.com",
       "subject": "repo:YOUR_ORG/YOUR_REPO:ref:refs/heads/main",
       "audiences": ["api://AzureADTokenExchange"]
     }'
   ```

3. **Add GitHub Secrets**:
   - `AZURE_CLIENT_ID`: Application (client) ID
   - `AZURE_TENANT_ID`: Directory (tenant) ID
   - `AZURE_SUBSCRIPTION_ID`: Azure subscription ID

### Manual Deployment

Trigger the "Deploy Application" workflow from GitHub Actions:
1. Go to Actions → Deploy Application
2. Select the service (userservice, reviewservice, etc.)
3. Select the environment (prod, staging)
4. Click "Run workflow"

## State Management

### Local State (Default)
By default, Terraform state is stored locally. This is fine for single-developer setups.

### Remote State (Recommended for Teams)
Uncomment the backend configuration in `main.tf`:

```hcl
backend "azurerm" {
  resource_group_name  = "rg-terraform-state"
  storage_account_name = "staaborglotfstate"
  container_name       = "tfstate"
  key                  = "aerglo.tfstate"
}
```

Create the storage account:
```bash
az group create --name rg-terraform-state --location canadacentral
az storage account create \
  --name staaborglotfstate \
  --resource-group rg-terraform-state \
  --sku Standard_LRS
az storage container create \
  --name tfstate \
  --account-name staaborglotfstate
```

## Common Operations

### View Current State
```bash
terraform show
```

### List Outputs
```bash
terraform output
terraform output -json
```

### Destroy Resources
```bash
terraform destroy -var-file=environments/prod/userservice.tfvars
```

### Import Existing Resources
```bash
# Import existing resource group
terraform import 'module.services["userservice"].azurerm_resource_group.service' \
  /subscriptions/SUBSCRIPTION_ID/resourceGroups/rg-userservice-prod
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Permission denied" | Ensure you're logged in: `az login` |
| Key Vault name too long | Names max 24 chars; module auto-abbreviates |
| RBAC not working | Wait 1-5 minutes for propagation |
| State lock error | Run `terraform force-unlock LOCK_ID` |

## Security Notes

- **Never commit secrets** to tfvars files
- Use **Azure Key Vault** for sensitive values
- **RBAC** is configured automatically for Key Vault and App Configuration
- **Managed Identity** is enabled on all Web Apps

## File Structure for Multiple Services

To deploy multiple services, create separate tfvars files:

```
environments/
└── prod/
    ├── userservice.tfvars
    ├── reviewservice.tfvars
    └── businessservice.tfvars
```

Deploy each independently:
```bash
terraform apply -var-file=environments/prod/userservice.tfvars
terraform apply -var-file=environments/prod/reviewservice.tfvars
terraform apply -var-file=environments/prod/businessservice.tfvars
```

Or deploy all via GitHub Actions using the "all" option in the workflow dispatch.
