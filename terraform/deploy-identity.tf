# --- Identidad del despliegue (OIDC) -------------------------------------------
#
# El job deploy del CI ya no usa el perfil de publicacion (usuario y contrasena
# fijos guardados como secreto en GitHub). En su lugar:
#
# 1. GitHub le da al job un token firmado que dice "soy el repo
#    antonicr1986/FinanceTracker, en el environment production".
# 2. Azure solo lo acepta si coincide EXACTAMENTE con la federated credential
#    de abajo (mismo repo y mismo environment) y a cambio entrega un acceso
#    temporal, que caduca en minutos.
# 3. Ese acceso solo vale para esta identidad, que solo puede desplegar en la
#    web app (Website Contributor sobre la web app, nada mas).
#
# No hay ninguna contrasena guardada en ningun sitio.

resource "azurerm_user_assigned_identity" "deploy" {
  name                = "id-financetracker-deploy"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tags                = local.tags
}

resource "azurerm_federated_identity_credential" "deploy_production" {
  name                      = "github-production"
  user_assigned_identity_id = azurerm_user_assigned_identity.deploy.id
  audience                  = ["api://AzureADTokenExchange"]
  issuer                    = "https://token.actions.githubusercontent.com"
  # Solo los jobs del environment production de este repo.
  subject = "repo:${var.github_repository}:environment:production"
}

resource "azurerm_role_assignment" "deploy_web_app" {
  scope                = azurerm_linux_web_app.api.id
  role_definition_name = "Website Contributor"
  principal_id         = azurerm_user_assigned_identity.deploy.principal_id
}
