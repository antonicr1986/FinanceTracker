# --- Correo: Azure Communication Services -------------------------------------
# Envia el codigo de verificacion del registro. Tres piezas:
#   - Email Communication Service: el servicio de correo, con un dominio que
#     gestiona Azure (xxxx.azurecomm.net). Sin dominio propio que verificar.
#   - Communication Service: el recurso al que la API llama para enviar.
#   - La asociacion entre los dos (linkedDomains del Communication Service).
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

# Con azapi y no con azurerm: al leer el recurso, azurerm pide tambien sus
# claves de acceso (listKeys). La identidad del CI es de solo lectura y no
# puede pedirlas, asi que el plan fallaba; y ademas las claves acababan en el
# estado de Terraform sin usarse (la API envia con identidad administrada).
# azapi solo lee el recurso: ni permisos de mas para el CI ni claves en el estado.
resource "azapi_resource" "communication_service" {
  type      = "Microsoft.Communication/communicationServices@2023-04-01"
  name      = var.communication_service_name
  parent_id = azurerm_resource_group.main.id
  location  = "global"

  body = {
    properties = {
      dataLocation  = "Europe"
      linkedDomains = [azurerm_email_communication_service_domain.azure_managed.id]
    }
  }

  # Para la salida email_endpoint.
  response_export_values = ["properties.hostName"]

  tags = local.tags
}

# Paso de azurerm a azapi sin tocar Azure: el recurso existente se adopta con
# un import y los dos de azurerm salen del estado sin borrarse. Ya aplicado,
# esto no vuelve a hacer nada.
import {
  to = azapi_resource.communication_service
  id = "${local.rg_id}/providers/Microsoft.Communication/communicationServices/${var.communication_service_name}"
}

removed {
  from = azurerm_communication_service.main

  lifecycle {
    destroy = false
  }
}

removed {
  from = azurerm_communication_service_email_domain_association.main

  lifecycle {
    destroy = false
  }
}

# La web app puede enviar correo, y solo a traves de este recurso. Rol
# integrado que Microsoft documenta para enviar con Entra ID; se limita al
# recurso de Communication Services, no al grupo ni a la suscripcion.
resource "azurerm_role_assignment" "api_acs_email" {
  scope                = azapi_resource.communication_service.id
  role_definition_name = "Communication and Email Service Owner"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id
}
