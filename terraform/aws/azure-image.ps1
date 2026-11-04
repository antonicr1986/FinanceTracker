# Interpreta la respuesta de Azure para saber que imagen esta en produccion.
# La usa up.ps1; esta aparte para poder probarla sin Azure.

function Resolve-ContainerImage {
    param(
        # Valor de siteConfig.linuxFxVersion del App Service, p. ej. "DOCKER|ghcr.io/...:sha-<commit>".
        [string] $LinuxFxVersion
    )

    if ([string]::IsNullOrWhiteSpace($LinuxFxVersion)) {
        throw "Azure no ha devuelto ninguna imagen. Comprueba la sesion de az (az login) y el nombre del App Service."
    }

    $image = $LinuxFxVersion -replace '^DOCKER\|', ''

    if ($image -cnotmatch '^ghcr\.io/antonicr1986/financetracker:sha-[0-9a-f]{40}$') {
        throw "Azure ejecuta '$image', que no es una version sha-<commit>. AWS solo se levanta con una imagen ya validada en produccion."
    }

    $image
}
