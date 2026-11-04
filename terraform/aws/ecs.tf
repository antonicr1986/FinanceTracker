# --- API: ECS Express Mode (Fargate + balanceador gestionado) -----------------
# Equivale al App Service de Azure. Express Mode crea y gestiona el balanceador,
# el certificado HTTPS y la URL publica (<servicio>.ecs.<region>.on.aws).

resource "aws_ecs_cluster" "main" {
  name = "financetracker"
}

resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/financetracker-api"
  retention_in_days = 7
}

# --- Roles IAM ----------------------------------------------------------------

data "aws_iam_policy_document" "ecs_tasks_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

data "aws_iam_policy_document" "ecs_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs.amazonaws.com"]
    }
  }
}

# Rol de ejecucion: arrancar la tarea (escribir logs y leer los secretos).
resource "aws_iam_role" "execution" {
  name               = "financetracker-ecs-execution"
  assume_role_policy = data.aws_iam_policy_document.ecs_tasks_assume.json
}

resource "aws_iam_role_policy_attachment" "execution" {
  role       = aws_iam_role.execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# Solo puede leer los dos parametros de la API, no el resto del almacen.
data "aws_iam_policy_document" "read_api_secrets" {
  statement {
    actions = ["ssm:GetParameters"]
    resources = [
      aws_ssm_parameter.connection_string.arn,
      aws_ssm_parameter.jwt_key.arn,
    ]
  }
}

resource "aws_iam_role_policy" "execution_secrets" {
  name   = "read-api-secrets"
  role   = aws_iam_role.execution.id
  policy = data.aws_iam_policy_document.read_api_secrets.json
}

# Rol de infraestructura: deja a ECS crear el balanceador, el certificado y el
# escalado del servicio Express.
resource "aws_iam_role" "infrastructure" {
  name               = "financetracker-ecs-infrastructure"
  assume_role_policy = data.aws_iam_policy_document.ecs_assume.json
}

resource "aws_iam_role_policy_attachment" "infrastructure" {
  role       = aws_iam_role.infrastructure.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSInfrastructureRoleforExpressGatewayServices"
}

# --- Servicio -------------------------------------------------------------------

resource "aws_ecs_express_gateway_service" "api" {
  service_name            = "financetracker-api"
  cluster                 = aws_ecs_cluster.main.name
  execution_role_arn      = aws_iam_role.execution.arn
  infrastructure_role_arn = aws_iam_role.infrastructure.arn

  cpu               = "512"  # 0,5 vCPU
  memory            = "1024" # 1 GB
  health_check_path = "/health"

  # El apply espera a que la API este sana: si no arranca, falla aqui y no
  # mas tarde en la consola.
  wait_for_steady_state = true

  primary_container {
    image          = var.container_image
    container_port = 8080

    aws_logs_configuration {
      log_group         = aws_cloudwatch_log_group.api.name
      log_stream_prefix = "api"
    }

    secret {
      name       = "ConnectionStrings__DefaultConnection"
      value_from = aws_ssm_parameter.connection_string.arn
    }

    secret {
      name       = "Jwt__Key"
      value_from = aws_ssm_parameter.jwt_key.arn
    }
  }

  network_configuration {
    subnets         = data.aws_subnets.default.ids
    security_groups = [aws_security_group.api.id]
  }

  # Siempre una sola tarea: no escala (ni en coste).
  scaling_target {
    min_task_count            = 1
    max_task_count            = 1
    auto_scaling_metric       = "AVERAGE_CPU"
    auto_scaling_target_value = 60
  }

  # Sin esto, un destroy puede borrar los permisos antes que el servicio, y el
  # servicio se queda atascado en DRAINING sin poder borrar el balanceador.
  depends_on = [
    aws_iam_role_policy_attachment.execution,
    aws_iam_role_policy.execution_secrets,
    aws_iam_role_policy_attachment.infrastructure,
  ]
}
