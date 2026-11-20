# --- Correo: Azure Communication Services -------------------------------------
# Envia el codigo de verificacion del registro. Tres piezas:
#   - Email Communication Service: el servicio de correo, con un dominio que
#     gestiona Azure (xxxx.azurecomm.net). Sin dominio propio que verificar.
#   - Communication Service: el recurso al que la API llama para enviar.
#   - La asociacion entre los dos.
# La API se autentica con la identidad administrada de la web app (sin
# cadena de conexion ni clave que guardar). Coste: centimos por miles de correos.

resource "azurerm_email_communication_service" "main" {
  name                = var.email_service_name
  resource_group_name = azurerm_resource_group.main.name
  data_location       = "Europe"

  tags = local.tags
}

resource "azurerm_email_communication_service_domain" "azure_managed" {
  name              = "AzureManagedDomain"
  email_service_id  = azurerm_email_communication_service.main.id
  domain_management = "AzureManaged"

  # Sin seguimiento de aperturas ni clics: el correo solo lleva un codigo.
  user_engagement_tracking_enabled = false

  tags = local.tags
}

resource "azurerm_communication_service" "main" {
  name                = var.communication_service_name
  resource_group_name = azurerm_resource_group.main.name
  data_location       = "Europe"

  tags = local.tags
}

resource "azurerm_communication_service_email_domain_association" "main" {
  communication_service_id = azurerm_communication_service.main.id
  email_service_domain_id  = azurerm_email_communication_service_domain.azure_managed.id
}

# La web app puede enviar correo, y solo a traves de este recurso. Rol
# integrado que Microsoft documenta para enviar con Entra ID; se limita al
# recurso de Communication Services, no al grupo ni a la suscripcion.
resource "azurerm_role_assignment" "api_acs_email" {
  scope                = azurerm_communication_service.main.id
  role_definition_name = "Communication and Email Service Owner"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id
}
