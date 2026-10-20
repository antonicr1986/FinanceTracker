# --- Secretos: Azure Key Vault ---------------------------------------------------
#
# La cadena de conexion y la clave JWT dejan de estar escritas en la
# configuracion de la web app: viven en Key Vault y la app las lee con su
# identidad administrada (sin usuario ni contrasena). En los app_settings solo
# queda una referencia: @Microsoft.KeyVault(SecretUri=...).
#
# Los VALORES de los secretos NO se crean con Terraform: si lo hiciera,
# quedarian en el fichero de estado. Se cargan una vez con la CLI (ver README).

resource "azurerm_key_vault" "main" {
  name                = var.key_vault_name
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tenant_id           = var.tenant_id
  sku_name            = "standard"

  # Permisos con RBAC de Azure (roles), no con las antiguas access policies.
  rbac_authorization_enabled = true

  # Un secreto borrado se puede recuperar durante 7 dias, y nadie (ni un
  # administrador) puede purgarlo antes. No se puede desactivar despues.
  soft_delete_retention_days = 7
  purge_protection_enabled   = true

  # Acceso de red publico: ver AZU-0013 en .trivyignore.
  network_acls {
    default_action = "Allow"
    bypass         = "AzureServices"
  }

  tags = local.tags

  lifecycle {
    prevent_destroy = true
  }
}

# La web app solo puede LEER secretos, y solo de este Key Vault.
resource "azurerm_role_assignment" "api_kv_secrets_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_web_app.api.identity[0].principal_id
}

# Quien administra los secretos (cargarlos y rotarlos). Es una persona fija y no
# "quien ejecute Terraform", para que el plan del CI (otra identidad) no lo cambie.
resource "azurerm_role_assignment" "admin_kv_secrets_officer" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = var.key_vault_admin_object_id
}
