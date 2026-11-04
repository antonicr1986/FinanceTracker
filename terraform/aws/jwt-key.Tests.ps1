# Pruebas de la funcion que valida la clave JWT leida del Key Vault.
# Se ejecutan sin Azure ni AWS:  Invoke-Pester .\jwt-key.Tests.ps1

BeforeAll {
    . (Join-Path $PSScriptRoot 'jwt-key.ps1')
}

Describe 'Resolve-JwtKey' {
    It 'si el Key Vault no devuelve la clave falla con un mensaje claro' -TestCases @(
        @{ Respuesta = '' }
        @{ Respuesta = $null }
        @{ Respuesta = '   ' }
    ) {
        { Resolve-JwtKey -Secret $Respuesta } |
            Should -Throw -ExpectedMessage '*Key Vault no ha devuelto*'
    }

    It 'con una clave valida la devuelve sin espacios ni saltos de linea sobrantes' -TestCases @(
        @{ Respuesta = 'clave-de-prueba' }
        @{ Respuesta = "clave-de-prueba`r`n" }
        @{ Respuesta = '  clave-de-prueba ' }
    ) {
        Resolve-JwtKey -Secret $Respuesta | Should -BeExactly 'clave-de-prueba'
    }
}
