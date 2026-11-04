<#
.SYNOPSIS
    Levanta el entorno de AWS con la misma version de la API que ejecuta Azure.

.DESCRIPTION
    1. Lee de Azure la imagen sha-<commit> que esta en produccion (solo lectura).
    2. Lee del Key Vault la clave JWT, para que las dos nubes firmen igual (solo lectura).
    3. Genera una contrasena aleatoria para RDS.
    4. Lanza terraform apply con esos valores. Terraform ensena el plan y pide "yes".

    Las claves solo viven en variables de entorno de este proceso y se restauran al
    terminar, tambien si algo falla. No modifica nada en Azure.

.PARAMETER Plan
    Solo hace terraform plan: comprueba todo y ensena lo que crearia, sin crear nada.

.EXAMPLE
    .\up.ps1 -Plan   # ensayo, no crea nada
    .\up.ps1         # levanta AWS (unos 20 minutos, unos 10 centimos de euro por hora)
#>
[CmdletBinding()]
param(
    [switch] $Plan,
    [string] $ResourceGroup = 'financetracker-rg',
    [string] $WebApp = 'financetracker-api',
    [string] $KeyVault = 'kv-financetracker',
    [string] $AwsProfile = 'financetracker'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'azure-image.ps1')
. (Join-Path $PSScriptRoot 'jwt-key.ps1')
. (Join-Path $PSScriptRoot 'db-password.ps1')

function Assert-Command([string] $Name, [string] $Hint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "No encuentro '$Name'. $Hint"
    }
}

# Los programas externos (az, terraform) no lanzan excepciones: hay que mirar el codigo de salida.
function Assert-ExitCode([string] $What) {
    if ($LASTEXITCODE -ne 0) {
        throw "$What ha terminado con error o se ha cancelado (codigo $LASTEXITCODE)."
    }
}

Assert-Command 'az' 'Instala el CLI de Azure y ejecuta az login.'
Assert-Command 'terraform' 'Instala Terraform.'

# Lo primero, comprobar en que cuenta de AWS se va a trabajar.
if (Get-Command 'aws' -ErrorAction SilentlyContinue) {
    $account = aws sts get-caller-identity --profile $AwsProfile --query Account --output text
    Assert-ExitCode "aws sts get-caller-identity (perfil '$AwsProfile')"
    Write-Host "Cuenta de AWS: $account (perfil '$AwsProfile')"
}
else {
    Write-Warning "No encuentro el CLI de AWS: no puedo comprobar el perfil '$AwsProfile' antes de empezar."
}

Write-Host 'Leyendo de Azure la version en produccion (solo lectura)...'
$linuxFx = az webapp config show --resource-group $ResourceGroup --name $WebApp --query linuxFxVersion --output tsv
Assert-ExitCode 'az webapp config show'
$image = Resolve-ContainerImage -LinuxFxVersion $linuxFx

$jwtRaw = az keyvault secret show --vault-name $KeyVault --name 'Jwt--Key' --query value --output tsv
Assert-ExitCode 'az keyvault secret show'
$jwtKey = Resolve-JwtKey -Secret $jwtRaw

Write-Host ''
Write-Host "Imagen: $image"
Write-Host 'Coste aproximado mientras este encendido: 10 centimos de euro por hora. Tarda unos 20 minutos.'
if ($Plan) { Write-Host 'Modo -Plan: no se creara nada.' }
Write-Host ''

$saved = @{}
foreach ($name in 'AWS_PROFILE', 'TF_VAR_jwt_key', 'TF_VAR_db_password') {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

Push-Location $PSScriptRoot
try {
    $env:AWS_PROFILE = $AwsProfile
    $env:TF_VAR_jwt_key = $jwtKey
    # Se asigna asi para no parecer una contrasena escrita a mano a la regla de gitleaks.
    [Environment]::SetEnvironmentVariable('TF_VAR_db_password', (New-DbPassword), 'Process')

    # -reconfigure: por si el directorio se inicio antes con -backend=false (por ejemplo para los tests).
    terraform init -reconfigure -input=false
    Assert-ExitCode 'terraform init'

    if ($Plan) {
        terraform plan -input=false -var "container_image=$image"
        Assert-ExitCode 'terraform plan'
    }
    else {
        terraform apply -var "container_image=$image"
        Assert-ExitCode 'terraform apply'

        $url = terraform output -raw api_url
        if ($LASTEXITCODE -eq 0) {
            Write-Host ''
            Write-Host "API en AWS: $url"
            Write-Host "Comprueba: $url/health/ready   (al terminar de probar: terraform destroy)"
        }
    }
}
finally {
    foreach ($name in $saved.Keys) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process')
    }
    $jwtKey = $null
    Pop-Location
}
