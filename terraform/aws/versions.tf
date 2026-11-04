terraform {
  required_version = ">= 1.11" # misma version minima que el Terraform de Azure

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }

  # Estado remoto en S3: cifrado, versionado y con bloqueo (use_lockfile crea un
  # .tflock en el propio bucket; ya no hace falta DynamoDB). El bucket se creo a
  # mano con el CLI (no lo gestiona este codigo, porque tiene que existir antes
  # que el propio estado). Las credenciales no van aqui: en local se usa
  # AWS_PROFILE=financetracker y en el CI entrara por OIDC.
  backend "s3" {
    bucket       = "financetracker-tfstate-188656931134"
    key          = "financetracker.tfstate"
    region       = "eu-west-1"
    encrypt      = true
    use_lockfile = true
  }
}

provider "aws" {
  region = var.region

  # Etiquetas en todos los recursos: asi se encuentran y se filtran los costes
  # del proyecto en Billing.
  default_tags {
    tags = {
      project    = "financetracker"
      managed-by = "terraform"
    }
  }
}
