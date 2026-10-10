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
  description = "Nombre del plan de App Service (F1). Sale en: az appservice plan list -g financetracker-rg -o table"
  type        = string
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
  default = "acrfinancetracker"
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
variable "sql_admin_password_version" {
  description = "Version de la contrasena del administrador SQL."
  type        = number
  default     = 0
}
