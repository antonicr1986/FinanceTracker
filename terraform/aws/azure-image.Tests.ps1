# Pruebas de la funcion que interpreta la imagen que ejecuta Azure.
# Se ejecutan sin Azure ni AWS:  Invoke-Pester .\azure-image.Tests.ps1

BeforeAll {
    . (Join-Path $PSScriptRoot 'azure-image.ps1')
}

Describe 'Resolve-ContainerImage' {
    It 'devuelve la imagen sha-commit que ejecuta Azure' {
        $sha = 'a1e578cd601edece9dd5a6f60460ece7f4227908'
        $respuesta = "DOCKER|ghcr.io/antonicr1986/financetracker:sha-$sha"

        Resolve-ContainerImage -LinuxFxVersion $respuesta |
            Should -Be "ghcr.io/antonicr1986/financetracker:sha-$sha"
    }

    It 'con la etiqueta latest falla con un mensaje claro' {
        { Resolve-ContainerImage -LinuxFxVersion 'DOCKER|ghcr.io/antonicr1986/financetracker:latest' } |
            Should -Throw -ExpectedMessage '*sha-*'
    }

    It 'con una respuesta vacia o nula falla con un mensaje claro' -TestCases @(
        @{ Respuesta = '' }
        @{ Respuesta = $null }
        @{ Respuesta = '   ' }
    ) {
        { Resolve-ContainerImage -LinuxFxVersion $Respuesta } |
            Should -Throw -ExpectedMessage '*no ha devuelto*'
    }

    It 'con otra imagen o un sha incompleto falla: <Caso>' -TestCases @(
        @{ Caso = 'otro registro'; Respuesta = 'DOCKER|docker.io/antonicr1986/financetracker:sha-a1e578cd601edece9dd5a6f60460ece7f4227908' }
        @{ Caso = 'otro repo';     Respuesta = 'DOCKER|ghcr.io/otro/financetracker:sha-a1e578cd601edece9dd5a6f60460ece7f4227908' }
        @{ Caso = 'sha corto';     Respuesta = 'DOCKER|ghcr.io/antonicr1986/financetracker:sha-a1e578c' }
    ) {
        { Resolve-ContainerImage -LinuxFxVersion $Respuesta } |
            Should -Throw -ExpectedMessage '*sha-*'
    }
}
