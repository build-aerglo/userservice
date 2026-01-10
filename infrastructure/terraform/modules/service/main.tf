# =============================================================================
# Service Module - Creates Azure resources for a single microservice
# =============================================================================
# Resources created:
# - Resource Group
# - App Service Plan
# - Web App (with Managed Identity)
# - Key Vault (optional)
# - App Configuration (optional)
# - RBAC role assignments
# =============================================================================

# =============================================================================
# Resource Group
# =============================================================================

resource "azurerm_resource_group" "service" {
  name     = "rg-${var.service_name}-${var.environment}"
  location = var.location
  tags     = var.tags
}

# =============================================================================
# App Service Plan
# =============================================================================

resource "azurerm_service_plan" "service" {
  name                = "asp-${var.service_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.service.name
  location            = azurerm_resource_group.service.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku

  tags = var.tags
}

# =============================================================================
# Web App
# =============================================================================

resource "azurerm_linux_web_app" "service" {
  name                = "${var.service_name}-api-cc"
  resource_group_name = azurerm_resource_group.service.name
  location            = azurerm_resource_group.service.location
  service_plan_id     = azurerm_service_plan.service.id

  https_only = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on         = var.always_on
    health_check_path = var.health_check_path
    minimum_tls_version = "1.2"

    application_stack {
      dotnet_version = var.dotnet_version
    }
  }

  app_settings = merge(
    {
      "ASPNETCORE_ENVIRONMENT" = var.environment == "prod" ? "Production" : title(var.environment)
      "EnableSwagger"          = tostring(var.enable_swagger)
    },
    var.create_key_vault ? {
      "AZURE_KEY_VAULT_URI" = azurerm_key_vault.service[0].vault_uri
    } : {},
    var.create_app_config ? {
      "AZURE_APP_CONFIGURATION_ENDPOINT" = azurerm_app_configuration.service[0].endpoint
    } : {}
  )

  tags = var.tags
}

# =============================================================================
# Key Vault (Optional)
# =============================================================================

resource "azurerm_key_vault" "service" {
  count = var.create_key_vault ? 1 : 0

  # Key Vault names must be 3-24 characters, globally unique
  name                = substr("kv-${replace(var.service_name, "service", "svc")}-${var.environment}", 0, 24)
  location            = azurerm_resource_group.service.location
  resource_group_name = azurerm_resource_group.service.name
  tenant_id           = var.tenant_id
  sku_name            = "standard"

  enable_rbac_authorization  = true
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  tags = var.tags
}

# Key Vault Administrator role for current user
resource "azurerm_role_assignment" "kv_admin" {
  count = var.create_key_vault ? 1 : 0

  scope                = azurerm_key_vault.service[0].id
  role_definition_name = "Key Vault Administrator"
  principal_id         = var.current_user_object_id
}

# Key Vault Secrets User role for Web App
resource "azurerm_role_assignment" "kv_secrets_user" {
  count = var.create_key_vault ? 1 : 0

  scope                = azurerm_key_vault.service[0].id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.service.identity[0].principal_id
}

# Store secrets in Key Vault
resource "azurerm_key_vault_secret" "secrets" {
  for_each = var.create_key_vault ? var.app_secrets : {}

  name         = replace(each.key, ":", "--")
  value        = each.value
  key_vault_id = azurerm_key_vault.service[0].id

  depends_on = [azurerm_role_assignment.kv_admin]
}

# =============================================================================
# App Configuration (Optional)
# =============================================================================

resource "azurerm_app_configuration" "service" {
  count = var.create_app_config ? 1 : 0

  name                = "appconfig-${var.service_name}-${var.environment}-cc"
  resource_group_name = azurerm_resource_group.service.name
  location            = azurerm_resource_group.service.location
  sku                 = "free"

  tags = var.tags
}

# App Configuration Data Reader role for Web App
resource "azurerm_role_assignment" "appconfig_reader" {
  count = var.create_app_config ? 1 : 0

  scope                = azurerm_app_configuration.service[0].id
  role_definition_name = "App Configuration Data Reader"
  principal_id         = azurerm_linux_web_app.service.identity[0].principal_id
}

# App Configuration Data Owner role for current user
resource "azurerm_role_assignment" "appconfig_owner" {
  count = var.create_app_config ? 1 : 0

  scope                = azurerm_app_configuration.service[0].id
  role_definition_name = "App Configuration Data Owner"
  principal_id         = var.current_user_object_id
}

# Store configuration values
resource "azurerm_app_configuration_key" "config" {
  for_each = var.create_app_config ? var.app_config_values : {}

  configuration_store_id = azurerm_app_configuration.service[0].id
  key                    = each.key
  value                  = each.value

  depends_on = [azurerm_role_assignment.appconfig_owner]
}
