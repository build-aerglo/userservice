# =============================================================================
# Input Variables
# =============================================================================

variable "environment" {
  type        = string
  description = "Environment name (e.g., prod, staging, dev)"
  default     = "prod"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  type        = string
  description = "Azure region for resources"
  default     = "canadacentral"
}

variable "create_shared_resources" {
  type        = bool
  description = "Whether to create shared resources (App Config, etc.)"
  default     = false
}

# =============================================================================
# Service Configurations
# =============================================================================

variable "services" {
  type = list(object({
    name              = string
    app_service_sku   = string
    dotnet_version    = string
    always_on         = bool
    health_check_path = string
    create_key_vault  = bool
    create_app_config = bool
    enable_swagger    = bool
    secrets           = map(string)
    config_values     = map(string)
  }))
  description = "List of services to deploy"
  default     = []
}
