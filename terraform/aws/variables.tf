variable "region" {
  description = "Region de todos los recursos de AWS."
  type        = string
  default     = "eu-west-1"
}

variable "db_instance_class" {
  description = "Tamano de la instancia de RDS. db.t3.micro entra en la capa gratuita."
  type        = string
  default     = "db.t3.micro"
}

variable "db_username" {
  description = "Usuario administrador de SQL Server."
  type        = string
  default     = "ftadmin"
}

# Write-only y efimera, como en el Terraform de Azure: se envia a AWS pero no
# queda en el estado ni en el plan guardado. Se pasa con la variable de entorno
# TF_VAR_db_password, no en terraform.tfvars. Solo letras y numeros: un ";" o
# un "=" romperian la cadena de conexion de la API.
variable "db_password" {
  description = "Contrasena del administrador de SQL Server."
  type        = string
  sensitive   = true
  ephemeral   = true
}

# Para rotar la contrasena: nueva contrasena en TF_VAR_db_password y subir este
# valor en 1. Si no cambia, Terraform no vuelve a enviarla.
variable "db_password_version" {
  description = "Version de la contrasena del administrador de SQL Server."
  type        = number
  default     = 1
}

variable "container_image" {
  description = "Imagen de la API: la sha-<commit> que ejecuta Azure (la pasa up.ps1). Un solo build para las dos nubes."
  type        = string

  # Sin valor por defecto y nunca :latest, que se publica antes de pasar el
  # DAST: AWS solo arranca con una version ya validada en produccion.
  validation {
    condition     = can(regex("^ghcr\\.io/antonicr1986/financetracker:sha-[0-9a-f]{40}$", var.container_image))
    error_message = "container_image debe ser ghcr.io/antonicr1986/financetracker:sha-<commit> (40 caracteres). Usa up.ps1, que toma la version que ejecuta Azure."
  }
}

# Clave con la que la API firma los JWT. Debe ser la MISMA que en Azure: asi un
# usuario con sesion abierta no tiene que volver a entrar al migrar de nube.
# Efimera y write-only, como la contrasena: TF_VAR_jwt_key, nunca en tfvars.
variable "jwt_key" {
  description = "Clave de firma de los JWT (la misma que Jwt--Key en el Key Vault de Azure)."
  type        = string
  sensitive   = true
  ephemeral   = true
}

# Para rotarla: nueva clave en TF_VAR_jwt_key y subir este valor en 1.
variable "jwt_key_version" {
  description = "Version de la clave de firma de los JWT."
  type        = number
  default     = 1
}
