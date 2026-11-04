# Pruebas de la variable container_image. No tocan AWS: el proveedor es simulado
# y solo se hace plan. Ejecutar desde terraform/aws:
#   terraform init -backend=false   (solo la primera vez)
#   terraform test

# Valores con forma real donde el proveedor los valida: los aleatorios del
# simulador no son JSON ni CIDR validos y el plan fallaria por eso.
mock_provider "aws" {
  mock_data "aws_iam_policy_document" {
    defaults = {
      json = "{\"Version\":\"2012-10-17\",\"Statement\":[]}"
    }
  }

  mock_data "aws_vpc" {
    defaults = {
      cidr_block = "172.31.0.0/16"
    }
  }
}

# Valores de relleno para las variables obligatorias; no son secretos reales.
variables {
  db_password = "SoloParaTests1"
  jwt_key     = "solo-para-tests-solo-para-tests-solo-para-tests"
}

run "rechaza_latest" {
  command = plan

  variables {
    container_image = "ghcr.io/antonicr1986/financetracker:latest"
  }

  expect_failures = [var.container_image]
}

run "acepta_sha_commit" {
  command = plan

  variables {
    container_image = "ghcr.io/antonicr1986/financetracker:sha-a1e578cd601edece9dd5a6f60460ece7f4227908"
  }
}
