# Genera la contrasena de RDS que usa up.ps1. Esta aparte para poder probarla.
# Solo letras y numeros: RDS no admite algunos simbolos (/ " @ y espacio).
# Usa un generador criptografico (Get-Random no es apto para contrasenas) y
# funciona en Windows PowerShell 5.1 y en PowerShell 7.

function New-DbPassword {
    param(
        # Minimo 8 (lo que pide RDS) y un maximo razonable; ademas evita que el
        # bucle de abajo no termine nunca con una longitud demasiado corta.
        [ValidateRange(8, 128)]
        [int] $Length = 24
    )

    $chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        # Se vuelve a generar hasta que tenga mayuscula, minuscula y numero.
        do {
            $result = New-Object System.Text.StringBuilder
            $byte = New-Object byte[] 1
            while ($result.Length -lt $Length) {
                $rng.GetBytes($byte)
                # 248 = 62 * 4: descartar el resto evita que unos caracteres salgan mas que otros.
                if ($byte[0] -lt 248) {
                    [void] $result.Append($chars[$byte[0] % $chars.Length])
                }
            }
            $password = $result.ToString()
        } until ($password -cmatch '[A-Z]' -and $password -cmatch '[a-z]' -and $password -cmatch '[0-9]')

        $password
    }
    finally {
        $rng.Dispose()
    }
}
