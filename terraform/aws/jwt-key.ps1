# Valida la clave JWT que up.ps1 lee del Key Vault de Azure.
# Esta aparte para poder probarla sin Azure. Nunca imprime la clave.

function Resolve-JwtKey {
    param(
        # Valor del secreto Jwt--Key tal como lo devuelve "az keyvault secret show".
        [string] $Secret
    )

    if ([string]::IsNullOrWhiteSpace($Secret)) {
        throw "El Key Vault no ha devuelto la clave JWT (Jwt--Key). Comprueba az login y que tienes permiso de lectura sobre kv-financetracker."
    }

    $Secret.Trim()
}
