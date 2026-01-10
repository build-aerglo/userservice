# =============================================================================
# Service Module - Input Variables
# =============================================================================

variable "service_name" {
  type        = string
  description = "Name of the service (e.g., userservice, reviewservice)"
}

variable "environment" {
  type        = string
  description = "Environment name (e.g., prod, staging, dev)"
}

variable "location" {
  type        = string
  description = "Azure region for resources"
}

# =============================================================================
# App Service Configuration
# =============================================================================

variable "app_service_sku" {
  type        = string
  description = "App Service Plan SKU (e.g., B1, S1, P1v2)"
  default     = "B1"
}

variable "dotnet_version" {
  type        = string
  description = ".NET version for the runtime"
  default     = "9.0"
}

variable "always_on" {
  type        = bool
  description = "Keep the app always running"
  default     = false
}

variable "health_check_path" {
  type        = string
  description = "Health check endpoint path"
  default     = "/health"
}

# =============================================================================
# Feature Flags
# =============================================================================

variable "create_key_vault" {
  type        = bool
  description = "Create Azure Key Vault for secrets"
  default     = true
}

variable "create_app_config" {
  type        = bool
  description = "Create Azure App Configuration"
  default     = true
}

variable "enable_swagger" {
  type        = bool
  description = "Enable Swagger UI in the deployed app"
  default     = true
}

# =============================================================================
# Secrets and Configuration
# =============================================================================

variable "app_secrets" {
  type        = map(string)
  description = "Secrets to store in Key Vault"
  default     = {}
  sensitive   = true
}

variable "app_config_values" {
  type        = map(string)
  description = "Configuration values to store in App Configuration"
  default     = {}
}

# =============================================================================
# Identity and Access
# =============================================================================

variable "current_user_object_id" {
  type        = string
  description = "Object ID of the current user (for RBAC)"
}

variable "tenant_id" {
  type        = string
  description = "Azure AD Tenant ID"
}

# =============================================================================
# Tags
# =============================================================================

variable "tags" {
  type        = map(string)
  description = "Tags to apply to all resources"
  default     = {}
}
