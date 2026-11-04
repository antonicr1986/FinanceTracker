<#
.SYNOPSIS
    Apaga el entorno de AWS levantado con up.ps1 (terraform destroy).

.DESCRIPTION
    terraform destroy exige las variables obligatorias aunque no las use para borrar.
    Este script las rellena con valores que no sirven para nada mas:
      - container_image: una sha-<commit> ficticia que solo cumple el formato.
      - db_password y jwt_key: valores aleatorios que no se envian a ningun sitio.

    No lee nada de Azure a proposito: asi se puede apagar AWS aunque Azure no responda.
    Terraform ensena el plan de borrado y pide "yes". No toca Azure.

.PARAMETER Plan
    Solo ensena lo que borraria (terraform plan -destroy), sin borrar nada.

.EXAMPLE
    .\down.ps1 -Plan   # ensayo, no borra nada
    .\down.ps1         # apaga AWS (unos 20 minutos)
#>
[CmdletBinding()]
param(
    [switch] $Plan,
    [string] $AwsProfile = 'financetracker'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'db-password.ps1')

# Cumple la validacion de variables.tf; destroy no despliega ninguna imagen.
$placeholderImage = 'ghcr.io/antonicr1986/financetracker:sha-' + ('0' * 40)

if (-not (Get-Command 'terraform' -ErrorAction SilentlyContinue)) {
    throw "No encuentro 'terraform'. Instala Terraform."
}

function Assert-ExitCode([string] $What) {
    if ($LASTEXITCODE -ne 0) {
        throw "$What ha terminado con error o se ha cancelado (codigo $LASTEXITCODE)."
    }
}

$saved = @{}
foreach ($name in 'AWS_PROFILE', 'TF_VAR_jwt_key', 'TF_VAR_db_password') {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

Push-Location $PSScriptRoot
try {
    $env:AWS_PROFILE = $AwsProfile
    [Environment]::SetEnvironmentVariable('TF_VAR_jwt_key', (New-DbPassword -Length 64), 'Process')
    [Environment]::SetEnvironmentVariable('TF_VAR_db_password', (New-DbPassword), 'Process')

    terraform init -reconfigure -input=false
    Assert-ExitCode 'terraform init'

    if ($Plan) {
        terraform plan -destroy -input=false -var "container_image=$placeholderImage"
        Assert-ExitCode 'terraform plan -destroy'
    }
    else {
        terraform destroy -var "container_image=$placeholderImage"
        Assert-ExitCode 'terraform destroy'
        Write-Host ''
        Write-Host 'AWS apagado. Para volver a levantarlo: .\up.ps1'
    }
}
finally {
    foreach ($name in $saved.Keys) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process')
    }
    Pop-Location
}
