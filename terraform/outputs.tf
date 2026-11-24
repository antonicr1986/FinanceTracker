output "api_url" {
  value = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "sql_server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}

# Sensible para que no salga en la consola ni en los logs del CI. Para verla:
# terraform output -raw app_insights_connection_string
output "app_insights_connection_string" {
  value     = azurerm_application_insights.api.connection_string
  sensitive = true
}

# Va a la variable AZURE_DEPLOY_CLIENT_ID de GitHub (no es secreto: sin el
# token de GitHub del environment production no sirve para nada).
output "deploy_client_id" {
  value = azurerm_user_assigned_identity.deploy.client_id
}

output "key_vault_uri" {
  value = azurerm_key_vault.main.vault_uri
}

# Valores para los app settings de la API (Email__Endpoint y Email__Sender).
output "email_endpoint" {
  value = "https://${azapi_resource.communication_service.output.properties.hostName}"
}

output "email_sender" {
  value = "DoNotReply@${azurerm_email_communication_service_domain.azure_managed.from_sender_domain}"
}
