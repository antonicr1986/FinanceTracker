terraform {
  required_version = ">= 1.11" # variables efimeras (1.10) y argumentos write-only (1.11)

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Estado local de momento (ignorado en git). Siguiente paso: moverlo a un
  # Storage Account con un backend "azurerm" para poder ejecutar plan desde el CI.
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}
