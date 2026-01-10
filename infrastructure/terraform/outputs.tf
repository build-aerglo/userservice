# =============================================================================
# Outputs
# =============================================================================

output "services" {
  description = "Deployed service information"
  value = {
    for name, service in module.services : name => {
      webapp_url          = service.webapp_url
      webapp_name         = service.webapp_name
      resource_group_name = service.resource_group_name
      key_vault_uri       = service.key_vault_uri
      app_config_endpoint = service.app_config_endpoint
      principal_id        = service.principal_id
    }
  }
}

output "deployment_commands" {
  description = "Quick deployment commands for each service"
  value = {
    for name, service in module.services : name => <<-EOT
      # Deploy ${name}
      az webapp deploy --name ${service.webapp_name} --resource-group ${service.resource_group_name} --src-path app.zip --type zip --clean true
    EOT
  }
}
