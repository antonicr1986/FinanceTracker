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

  # FTP y Web Deploy con usuario/contrasena apagados: el job deploy del CI
  # entra con OIDC (ver deploy-identity.tf), sin ningun perfil de publicacion.
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false

  site_config {
    always_on     = false # F1 no lo permite: la API se duerme a los 20 min
    ftps_state    = "Disabled"
    http2_enabled = true

    application_stack {
      docker_image_name   = trimprefix(var.container_image, "ghcr.io/")
      docker_registry_url = "https://ghcr.io"
    }
  }

  # Identidad administrada: la Web App puede autenticarse en otros servicios
  # de Azure (Key Vault, SQL) sin usuario ni contrasena. Gratis.
  identity {
    type = "SystemAssigned"
  }

  tags = local.tags

  lifecycle {
    ignore_changes = [
      # Los secretos viven solo en Azure (Jwt__Key, ConnectionStrings__...).
      app_settings,
      connection_string,
      # La imagen la cambia el job deploy del CI en cada push (sha-<commit>).
      site_config[0].application_stack,
      # Azure la añade sola para enlazar la web app con Application Insights en el portal.
      tags["hidden-link: /app-insights-resource-id"],
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

  # Administrador de Microsoft Entra ID: el que crea en la base de datos el
  # usuario de la identidad administrada de la API, que entra sin contrasena.
  # Solo Entra ID (azuread_authentication_only = true): Azure rechaza cualquier
  # login con usuario y contrasena, tambien el del administrador SQL de arriba,
  # que el proveedor sigue exigiendo pero ya no sirve para entrar. El acceso de
  # emergencia es el administrador de Entra ID (con MFA), no una contrasena.
  azuread_administrator {
    login_username              = var.sql_entra_admin_login
    object_id                   = var.sql_entra_admin_object_id
    azuread_authentication_only = true
  }

  tags = local.tags

  lifecycle {
    prevent_destroy = true
  }
}

# Unica regla de firewall de SQL: "permitir servicios de Azure" (0.0.0.0).
# La necesita la Web App, que en el plan F1 no tiene IP de salida fija ni VNet.
# Abre la red a servicios de cualquier cliente de Azure, pero ya no hay
# contrasena que probar: solo entra quien tenga identidad de Entra ID con
# permisos en la base de datos. Revisado el 28 nov 2026 (issue #42): ninguna
# otra regla. Si se sube de plan, sustituirla por VNet + private endpoint.
resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAllWindowsAzureIps"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "main" {
  name      = var.sql_database_name
  server_id = azurerm_mssql_server.main.id

  # Valores actuales en Azure: serverless 1 vCore, pausa tras 60 min.
  # Usa la oferta gratuita de Azure SQL (useFreeLimit = true, BillOverUsage):
  # con 1 vCore el cupo de 100.000 vCore-segundos/mes dura mas.
  sku_name                    = "GP_S_Gen5_1"
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
