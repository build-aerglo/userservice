# =============================================================================
# Service Module - Outputs
# =============================================================================

output "webapp_url" {
  description = "URL of the deployed web app"
  value       = "https://${azurerm_linux_web_app.service.default_hostname}"
}

output "webapp_name" {
  description = "Name of the web app"
  value       = azurerm_linux_web_app.service.name
}

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.service.name
}

output "principal_id" {
  description = "Managed Identity principal ID"
  value       = azurerm_linux_web_app.service.identity[0].principal_id
}

output "key_vault_uri" {
  description = "Key Vault URI (if created)"
  value       = var.create_key_vault ? azurerm_key_vault.service[0].vault_uri : null
}

output "key_vault_name" {
  description = "Key Vault name (if created)"
  value       = var.create_key_vault ? azurerm_key_vault.service[0].name : null
}

output "app_config_endpoint" {
  description = "App Configuration endpoint (if created)"
  value       = var.create_app_config ? azurerm_app_configuration.service[0].endpoint : null
}

output "app_config_name" {
  description = "App Configuration name (if created)"
  value       = var.create_app_config ? azurerm_app_configuration.service[0].name : null
}
