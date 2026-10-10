# Adopta los recursos que ya existen en Azure (Terraform >= 1.5).
# Con el primer "terraform apply" pasan al estado sin tocarse. Despues este
# fichero puede quedarse: un import ya hecho no vuelve a ejecutarse.

locals {
  sub    = "/subscriptions/${var.subscription_id}"
  rg_id  = "${local.sub}/resourceGroups/${var.resource_group_name}"
  sql_rg = "${local.sub}/resourceGroups/${var.sql_server_resource_group_name}"
}

import {
  to = azurerm_resource_group.main
  id = local.rg_id
}

import {
  to = azurerm_service_plan.api
  id = "${local.rg_id}/providers/Microsoft.Web/serverFarms/${var.service_plan_name}"
}

import {
  to = azurerm_linux_web_app.api
  id = "${local.rg_id}/providers/Microsoft.Web/sites/${var.web_app_name}"
}

import {
  to = azurerm_mssql_server.main
  id = "${local.sql_rg}/providers/Microsoft.Sql/servers/${var.sql_server_name}"
}

import {
  to = azurerm_mssql_database.main
  id = "${local.sql_rg}/providers/Microsoft.Sql/servers/${var.sql_server_name}/databases/${var.sql_database_name}"
}
