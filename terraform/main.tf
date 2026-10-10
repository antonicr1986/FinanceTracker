# Infraestructura de FinanceTracker en Azure.
#
# Los recursos ya existian antes de Terraform y se adoptan con imports.tf, sin
# recrearlos. Los secretos de la app (cadena de conexion, Jwt__Key) NO se
# gestionan aqui: se cambian en Azure y Terraform ignora sus cambios. Aun asi
# el proveedor los LEE y quedan en el fichero de estado, que por eso nunca se
# sube a git. La contrasena del administrador SQL es write-only y no se guarda.

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.resource_group_location

  tags = local.tags

  lifecycle {
    # Contiene la API y la base de datos: nunca se debe recrear.
    prevent_destroy = true
  }
}

locals {
  tags = {
    project    = "financetracker"
    managed-by = "terraform"
  }
}

# --- API: App Service Linux, plan gratuito F1 ---------------------------------

resource "azurerm_service_plan" "api" {
  name                = var.service_plan_name
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "F1"

  tags = local.tags
}

resource "azurerm_linux_web_app" "api" {
  name                = var.web_app_name
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  service_plan_id     = azurerm_service_plan.api.id
  https_only          = true

  # FTP con usuario/contrasena apagado. Web Deploy si, porque el job deploy
  # del CI publica con el perfil de publicacion (AZURE_WEBAPP_PUBLISH_PROFILE).
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = true

  site_config {
    always_on  = false # F1 no lo permite: la API se duerme a los 20 min
    ftps_state = "Disabled"

    application_stack {
      docker_image_name   = trimprefix(var.container_image, "ghcr.io/")
      docker_registry_url = "https://ghcr.io"
    }
  }

  tags = local.tags

  lifecycle {
    ignore_changes = [
      # Los secretos viven solo en Azure (Jwt__Key, ConnectionStrings__...).
      app_settings,
      connection_string,
      # La imagen la cambia el job deploy del CI en cada push (sha-<commit>).
      site_config[0].application_stack,
    ]
  }
}

# --- Base de datos: Azure SQL serverless ---------------------------------------

data "azurerm_resource_group" "sql" {
  name = var.sql_server_resource_group_name
}

resource "azurerm_mssql_server" "main" {
  name                = var.sql_server_name
  resource_group_name = data.azurerm_resource_group.sql.name
  location            = var.sql_location
  version             = "12.0"
  minimum_tls_version = "1.2"

  administrator_login = var.sql_admin_login

  # Write-only: se envia a Azure pero no queda en el estado. Solo se vuelve a
  # enviar cuando cambia la version, que es como se rota la contrasena.
  administrator_login_password_wo         = var.sql_admin_password
  administrator_login_password_wo_version = var.sql_admin_password_version

  tags = local.tags

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_mssql_database" "main" {
  name      = var.sql_database_name
  server_id = azurerm_mssql_server.main.id

  # Valores actuales en Azure: serverless 2 vCores, pausa tras 60 min.
  sku_name                    = "GP_S_Gen5_2"
  min_capacity                = 0.5
  auto_pause_delay_in_minutes = 60
  max_size_gb                 = 32
  storage_account_type        = "Local" # copias en la misma region (el valor por defecto es "Geo")

  # Sin etiquetas a proposito: asi el import no escribe nada en la base de
  # datos. Si usa la oferta gratuita de Azure SQL, una actualizacion del
  # proveedor podria no conservar esa opcion (que azurerm no gestiona).

  lifecycle {
    # Los datos reales estan aqui: un "destroy" por error no debe borrarla.
    prevent_destroy = true
  }
}
