terraform {
  required_version = ">= 1.11" # variables efimeras (1.10) y argumentos write-only (1.11)

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    # Solo para el recurso de Communication Services (ver email.tf).
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.0"
    }
  }

  # Estado remoto en Azure Storage: cifrado, con bloqueo (lease del blob) y
  # versionado. La cuenta se creo a mano con az (no la gestiona este codigo,
  # porque tiene que existir antes que el propio estado). Se entra con la
  # identidad de Azure (az login / OIDC en el CI): la cuenta no admite claves.
  backend "azurerm" {
    subscription_id      = "754fb5f7-5cb6-4c76-8e23-02bbb683bb7e"
    resource_group_name  = "financetracker-rg"
    storage_account_name = "stfinancetrackertfstate"
    container_name       = "tfstate"
    key                  = "financetracker.tfstate"
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id

  # Los proveedores de recursos ya estan registrados. Sin esto, azurerm intenta
  # registrarlos al arrancar, y la identidad del CI (solo lectura sobre el
  # grupo de recursos) no tiene permiso para hacerlo.
  resource_provider_registrations = "none"
}

# Misma identidad que azurerm (az login en local, OIDC en el CI).
provider "azapi" {
  subscription_id = var.subscription_id

  # Como en azurerm: los proveedores de recursos ya estan registrados.
  skip_provider_registration = true
}
