# Pruebas de la funcion que genera la contrasena de RDS.
# Se ejecutan sin Azure ni AWS:  Invoke-Pester .\db-password.Tests.ps1

BeforeAll {
    . (Join-Path $PSScriptRoot 'db-password.ps1')
}

Describe 'New-DbPassword' {
    It 'genera 24 caracteres, solo letras y numeros' {
        New-DbPassword | Should -MatchExactly '^[A-Za-z0-9]{24}$'
    }

    It 'siempre incluye mayuscula, minuscula y numero' {
        $sinAlgunaClase = 1..200 | ForEach-Object { New-DbPassword } |
            Where-Object { $_ -cnotmatch '[A-Z]' -or $_ -cnotmatch '[a-z]' -or $_ -cnotmatch '[0-9]' }

        $sinAlgunaClase | Should -BeNullOrEmpty
    }

    It 'cada llamada da una contrasena distinta' {
        $todas = 1..50 | ForEach-Object { New-DbPassword }

        ($todas | Select-Object -Unique).Count | Should -Be 50
    }
}
