# Mismas salidas que tendra el Terraform de Azure: los scripts y el runbook de
# migracion no necesitan saber en que nube estan.

output "database_host" {
  description = "Servidor de SQL Server para la cadena de conexion de la API."
  value       = aws_db_instance.main.address
}
