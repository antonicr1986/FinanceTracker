# Observabilidad de la API: Application Insights sobre un Log Analytics
# Workspace, y alertas por email.
#
# La API envia peticiones, dependencias, excepciones y logs con OpenTelemetry
# (Azure.Monitor.OpenTelemetry.AspNetCore). La connection string de Application
# Insights llega a la Web App como app setting APPLICATIONINSIGHTS_CONNECTION_STRING,
# que se pone con az: Terraform ignora los app_settings (ver main.tf).

resource "azurerm_log_analytics_workspace" "main" {
  name                = var.log_analytics_name
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30

  # Tope de ingesta diaria: Log Analytics incluye 5 GB/mes gratis y una demo
  # se queda muy por debajo, pero el tope evita sorpresas si algo se dispara.
  daily_quota_gb = 0.15

  tags = local.tags
}

resource "azurerm_application_insights" "api" {
  name                = var.app_insights_name
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = local.tags
}

# --- Alertas -------------------------------------------------------------------

resource "azurerm_monitor_action_group" "email" {
  name                = "financetracker-alertas"
  resource_group_name = azurerm_resource_group.main.name
  short_name          = "ftalertas"

  email_receiver {
    name                    = "propietario"
    email_address           = var.alert_email
    use_common_alert_schema = true
  }

  tags = local.tags
}

# Errores del servidor: algo se ha roto en produccion.
resource "azurerm_monitor_metric_alert" "http_5xx" {
  name                = "api-errores-5xx"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_linux_web_app.api.id]
  description         = "Mas de 5 respuestas 5xx en 5 minutos."
  severity            = 1
  frequency           = "PT1M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 5
  }

  action {
    action_group_id = azurerm_monitor_action_group.email.id
  }

  tags = local.tags
}

# Pico de 401: muchos intentos de acceso sin credenciales validas en poco
# tiempo, la huella tipica de un ataque de fuerza bruta contra el login.
resource "azurerm_monitor_metric_alert" "http_401" {
  name                = "api-pico-401"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_linux_web_app.api.id]
  description         = "Mas de 50 respuestas 401 en 5 minutos (posible fuerza bruta)."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http401"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 50
  }

  action {
    action_group_id = azurerm_monitor_action_group.email.id
  }

  tags = local.tags
}

# Pico de 403: alguien con sesion intentando entrar donde no debe.
resource "azurerm_monitor_metric_alert" "http_403" {
  name                = "api-pico-403"
  resource_group_name = azurerm_resource_group.main.name
  scopes              = [azurerm_linux_web_app.api.id]
  description         = "Mas de 20 respuestas 403 en 5 minutos."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http403"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 20
  }

  action {
    action_group_id = azurerm_monitor_action_group.email.id
  }

  tags = local.tags
}
