# --- Base de datos: RDS SQL Server Express ------------------------------------
# Express es la edicion sin coste de licencia. Mismo motor que Azure SQL, asi
# que la API no cambia ni de proveedor de EF Core ni de migraciones. No se
# indica nombre de base de datos (RDS SQL Server no lo admite): la API crea
# "acrfinancetracker" al arrancar, al aplicar las migraciones.

resource "aws_db_subnet_group" "main" {
  name        = "financetracker-db-subnets"
  description = "Subredes de la VPC por defecto para RDS"
  subnet_ids  = data.aws_subnets.default.ids
}

resource "aws_db_instance" "main" {
  identifier = "financetracker-db"
  engine     = "sqlserver-ex"
  # Solo la version mayor (SQL Server 2022): AWS aplica las menores solo
  # (auto_minor_version_upgrade) y no hay que perseguirlas en el codigo. Con
  # la version exacta, un apply futuro fallaria al retirarla AWS o intentaria
  # "volver atras" tras una actualizacion automatica.
  engine_version = "16.00"
  license_model  = "license-included"
  instance_class = var.db_instance_class

  allocated_storage = 20 # minimo de SQL Server; sin escalado automatico
  storage_type      = "gp3"
  storage_encrypted = true

  username            = var.db_username
  password_wo         = var.db_password
  password_wo_version = var.db_password_version

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.db.id]
  publicly_accessible    = false
  multi_az               = false

  # Pensado para levantar y destruir a demanda: copias minimas, sin proteccion
  # contra borrado y sin instantanea final al destruir. Los datos reales siguen
  # en Azure hasta la migracion.
  backup_retention_period = 1
  deletion_protection     = false
  skip_final_snapshot     = true
  apply_immediately       = true

  # Sin metricas de pago.
  performance_insights_enabled = false
  monitoring_interval          = 0
}
