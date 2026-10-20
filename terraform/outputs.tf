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

output "key_vault_uri" {
  value = azurerm_key_vault.main.vault_uri
}
