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

# Salida limitada a lo que la tarea necesita:
# - HTTPS a internet: descargar la imagen de GHCR y hablar con SSM y CloudWatch
#   (tienen IP publicas variables, por eso 0.0.0.0/0; ver AWS-0104 en
#   terraform/.trivyignore).
# - SQL Server, solo hacia el grupo de seguridad de la base de datos.
resource "aws_vpc_security_group_egress_rule" "api_https" {
  security_group_id = aws_security_group.api.id
  description       = "HTTPS a GHCR, SSM y CloudWatch"
  ip_protocol       = "tcp"
  from_port         = 443
  to_port           = 443
  cidr_ipv4         = "0.0.0.0/0"
}

resource "aws_vpc_security_group_egress_rule" "api_to_db" {
  security_group_id            = aws_security_group.api.id
  description                  = "SQL Server hacia RDS"
  ip_protocol                  = "tcp"
  from_port                    = 1433
  to_port                      = 1433
  referenced_security_group_id = aws_security_group.db.id
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

# Entrada a la API solo desde dentro de la VPC (el balanceador que crea Express
# Mode vive en ella). Desde internet no se llega directamente a las tareas.
resource "aws_vpc_security_group_ingress_rule" "api_from_vpc" {
  security_group_id = aws_security_group.api.id
  description       = "Puerto de la API desde el balanceador (dentro de la VPC)"
  ip_protocol       = "tcp"
  from_port         = 8080
  to_port           = 8080
  cidr_ipv4         = data.aws_vpc.default.cidr_block
}
