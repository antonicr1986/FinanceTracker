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
