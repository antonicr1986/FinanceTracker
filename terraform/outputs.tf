output "api_url" {
  value = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "sql_server_fqdn" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}
