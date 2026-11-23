# Ejecuta los tests en un contenedor Linux de .NET 8. En este PC, Smart App
# Control bloquea la DLL de los tests y "dotnet test" no ejecuta ninguno.
# Copia el repo dentro del contenedor (en solo lectura) y quita bin/obj de
# Windows antes de compilar.
# Uso: .\scripts\test.ps1                 (todos los tests)
#      .\scripts\test.ps1 ClientVersion   (solo los que contienen ese texto)
param([string]$Filter = '')

$repo = Split-Path -Parent $PSScriptRoot
$run = 'dotnet test'
if ($Filter) { $run += " --filter FullyQualifiedName~$Filter" }

docker run --rm -v "${repo}:/src:ro" mcr.microsoft.com/dotnet/sdk:8.0 bash -c "cp -r /src /work && cd /work && find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + && $run"
