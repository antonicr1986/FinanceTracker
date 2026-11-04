# --- Secretos de la API: Almacen de parametros (SSM), cifrados ----------------
# Equivalen a los secretos del Key Vault de Azure (ConnectionStrings--Default...
# y Jwt--Key). Los valores son write-only: llegan a AWS pero no quedan en el
# estado de Terraform. El servicio de ECS los inyecta como variables de entorno.
# SecureString con la clave gestionada por AWS (aws/ssm): sin coste.

resource "aws_ssm_parameter" "connection_string" {
  name        = "/financetracker/ConnectionStrings__DefaultConnection"
  description = "Cadena de conexion de la API a RDS"
  type        = "SecureString"

  # La direccion de RDS es estable aunque se destruya y se recree la instancia
  # con el mismo nombre, asi que solo cambia al rotar la contrasena.
  value_wo         = "Server=${aws_db_instance.main.address},1433;Database=acrfinancetracker;User Id=${var.db_username};Password=${var.db_password};TrustServerCertificate=True"
  value_wo_version = var.db_password_version
}

resource "aws_ssm_parameter" "jwt_key" {
  name        = "/financetracker/Jwt__Key"
  description = "Clave de firma de los JWT de la API"
  type        = "SecureString"

  value_wo         = var.jwt_key
  value_wo_version = var.jwt_key_version
}
