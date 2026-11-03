# --- Red: VPC por defecto de la cuenta ----------------------------------------
# No se crea una VPC propia: la que trae la cuenta tiene subredes publicas en
# las 3 zonas, que es lo que necesitan el balanceador y RDS, y no cuesta nada.
# Una VPC propia con subredes privadas obligaria a pagar un NAT Gateway.

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# --- Grupo de seguridad de la API ---------------------------------------------
# Lo usan las tareas de ECS. La regla de entrada desde el balanceador se decide
# en ecs.tf, cuando se defina el servicio.

resource "aws_security_group" "api" {
  name        = "financetracker-api-sg"
  description = "Tareas de ECS de la API de FinanceTracker"
  vpc_id      = data.aws_vpc.default.id
}

# Salida libre: la tarea descarga la imagen de GHCR y habla con RDS.
resource "aws_vpc_security_group_egress_rule" "api_all" {
  security_group_id = aws_security_group.api.id
  description       = "Salida a internet (GHCR) y a RDS"
  ip_protocol       = "-1"
  cidr_ipv4         = "0.0.0.0/0"
}

# --- Grupo de seguridad de la base de datos -----------------------------------
# Solo entra la API, por el puerto de SQL Server. Sin reglas de salida: los
# grupos de seguridad guardan estado y las respuestas salen igualmente.

resource "aws_security_group" "db" {
  name        = "financetracker-db-sg"
  description = "RDS SQL Server de FinanceTracker: solo acceso desde la API"
  vpc_id      = data.aws_vpc.default.id
}

resource "aws_vpc_security_group_ingress_rule" "db_from_api" {
  security_group_id            = aws_security_group.db.id
  description                  = "SQL Server desde las tareas de la API"
  ip_protocol                  = "tcp"
  from_port                    = 1433
  to_port                      = 1433
  referenced_security_group_id = aws_security_group.api.id
}
