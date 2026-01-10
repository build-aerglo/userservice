# =============================================================================
# Aerglo Microservices - Azure Infrastructure
# =============================================================================
# This Terraform configuration deploys Azure resources for Aerglo microservices.
# Services: userservice, reviewservice, businessservice, etc.
# =============================================================================

terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.80"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.45"
    }
  }

  # Uncomment to use Azure Storage for remote state
  # backend "azurerm" {
  #   resource_group_name  = "rg-terraform-state"
  #   storage_account_name = "staaborglotfstate"
  #   container_name       = "tfstate"
  #   key                  = "aerglo.tfstate"
  # }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

provider "azuread" {}

# =============================================================================
# Data Sources
# =============================================================================

data "azurerm_client_config" "current" {}

data "azuread_user" "current" {
  object_id = data.azurerm_client_config.current.object_id
}

# =============================================================================
# Local Variables
# =============================================================================

locals {
  # Common tags for all resources
  common_tags = {
    Environment = var.environment
    Project     = "Aerglo"
    ManagedBy   = "Terraform"
  }

  # Service configurations
  services = {
    for service in var.services : service.name => service
  }
}

# =============================================================================
# Shared Resources
# =============================================================================

# Shared Resource Group (optional - for shared resources like App Config)
resource "azurerm_resource_group" "shared" {
  count    = var.create_shared_resources ? 1 : 0
  name     = "rg-aerglo-shared-${var.environment}"
  location = var.location
  tags     = local.common_tags
}

# =============================================================================
# Service Modules
# =============================================================================

module "services" {
  source   = "./modules/service"
  for_each = local.services

  service_name = each.key
  environment  = var.environment
  location     = var.location

  # App Service Configuration
  app_service_sku      = each.value.app_service_sku
  dotnet_version       = each.value.dotnet_version
  always_on            = each.value.always_on
  health_check_path    = each.value.health_check_path

  # Feature Flags
  create_key_vault       = each.value.create_key_vault
  create_app_config      = each.value.create_app_config
  enable_swagger         = each.value.enable_swagger

  # Secrets (from variables or Azure Key Vault)
  app_secrets = each.value.secrets

  # App Configuration values
  app_config_values = each.value.config_values

  # Tags
  tags = merge(local.common_tags, {
    Service = each.key
  })

  # Current user for RBAC
  current_user_object_id = data.azurerm_client_config.current.object_id
  tenant_id              = data.azurerm_client_config.current.tenant_id
}
