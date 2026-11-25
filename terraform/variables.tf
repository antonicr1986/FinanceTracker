variable "subscription_id" {
  description = "Suscripcion de Azure (az account show --query id -o tsv)."
  type        = string
}

variable "location" {
  description = "Region de todos los recursos."
  type        = string
  default     = "belgiumcentral"
}

variable "resource_group_location" {
  description = "Region del grupo de recursos (solo guarda sus metadatos). Cambiarla lo recrea."
  type        = string
  default     = "spaincentral"
}

variable "resource_group_name" {
  type    = string
  default = "financetracker-rg"
}

variable "service_plan_name" {
  description = "Nombre del plan de App Service (F1)."
  type        = string
  default     = "financetracker-plan"
}

variable "github_repository" {
  description = "Repo de GitHub (propietario/nombre) cuyo job deploy puede desplegar en la web app."
  type        = string
  default     = "antonicr1986/FinanceTracker"
}

variable "web_app_name" {
  type    = string
  default = "financetracker-api"
}

variable "sql_server_name" {
  type    = string
  default = "financetrackerapp-srv"
}

variable "sql_server_resource_group_name" {
  description = "Grupo del servidor SQL, por si no es el mismo que el de la API."
  type        = string
  default     = "financetracker-rg"
}

variable "sql_location" {
  description = "Region del servidor SQL. No es la misma que la de la API."
  type        = string
  default     = "swedencentral"
}

variable "sql_database_name" {
  type    = string
  default = "sqldb-financetracker"
}

variable "container_image" {
  description = "Imagen inicial de la API. El CI la sustituye en cada despliegue."
  type        = string
  default     = "ghcr.io/antonicr1986/financetracker:latest"
}

variable "sql_admin_login" {
  type    = string
  default = "antonicr1986"
}

# Efimera: Terraform la usa durante el plan/apply pero nunca la escribe en el
# estado ni en el plan guardado. Se pasa con la variable de entorno
# TF_VAR_sql_admin_password, no en terraform.tfvars.
variable "sql_admin_password" {
  description = "Contrasena del administrador SQL."
  type        = string
  sensitive   = true
  ephemeral   = true
}

# 0 = no tocar la contrasena (asi el import no la reenvia). Para rotarla:
# nueva contrasena en TF_VAR_sql_admin_password y subir este valor en 1.
# Administrador de Entra ID del servidor SQL. No son secretos: un nombre y un
# identificador de usuario, inutiles sin iniciar sesion como el.
variable "sql_entra_admin_login" {
  description = "Usuario de Entra ID administrador del servidor SQL."
  type        = string
  default     = "antonicr1986@hotmail.com"
}

variable "sql_entra_admin_object_id" {
  description = "Object ID en Entra ID de ese usuario."
  type        = string
  default     = "2ef9ea81-a0ac-4bb6-ae1c-5be3342dc7a9"
}

variable "sql_admin_password_version" {
  description = "Version de la contrasena del administrador SQL."
  type        = number
  default     = 1 # 1: primera rotacion (octubre de 2026)
}

variable "log_analytics_name" {
  type    = string
  default = "financetracker-logs"
}

variable "app_insights_name" {
  type    = string
  default = "financetracker-api-insights"
}

# Sin valor por defecto a proposito: el repo es publico. En local va en
# terraform.tfvars (ignorado en git) y en el CI en la variable de GitHub
# ALERT_EMAIL.
variable "alert_email" {
  description = "Email que recibe las alertas de Azure Monitor."
  type        = string
}

variable "tenant_id" {
  description = "Tenant de Entra ID (az account show --query tenantId -o tsv)."
  type        = string
}

variable "key_vault_name" {
  description = "Nombre del Key Vault. Es global en todo Azure: 3-24 caracteres, letras, numeros y guiones."
  type        = string
  default     = "kv-financetracker"
}

variable "key_vault_admin_object_id" {
  description = "Object ID de quien administra los secretos (az ad signed-in-user show --query id -o tsv)."
  type        = string
}

variable "email_service_name" {
  description = "Nombre del Email Communication Service (global en Azure)."
  type        = string
  default     = "ecs-financetracker"
}

variable "communication_service_name" {
  description = "Nombre del Communication Service (global en Azure)."
  type        = string
  default     = "acs-financetracker"
}
