using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// Codigos de un solo uso basados en el tiempo (TOTP, RFC 6238), los que
/// generan Google Authenticator, Microsoft Authenticator y similares:
/// HMAC-SHA1, 6 digitos, intervalos de 30 segundos.
///
/// Implementado aqui, sin paquetes externos, para no añadir una dependencia
/// a la cadena de suministro por unas pocas lineas. Lo comprueban los vectores
/// de prueba del propio RFC (TotpTests).
/// </summary>
public static class Totp
{
    public const int Digits = 6;
    public const int StepSeconds = 30;

    // 160 bits: el tamaño que recomienda el RFC 4226 para HMAC-SHA1.
    public const int SecretSize = 20;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static byte[] GenerateSecret() => RandomNumberGenerator.GetBytes(SecretSize);

    public static long GetStep(DateTimeOffset time) => time.ToUnixTimeSeconds() / StepSeconds;

    public static string ComputeCode(byte[] secret, long step, int digits = Digits)
    {
        var counter = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        var hash = HMACSHA1.HashData(secret, counter);

        // Truncado dinamico (RFC 4226, seccion 5.3).
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];

        var code = binary % (int)Math.Pow(10, digits);

        return code.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    /// <summary>
    /// Intervalo cuyo codigo coincide, o null. Acepta el intervalo actual y
    /// uno a cada lado (±30 s) por si el reloj del movil va algo desfasado.
    /// La comparacion es de tiempo constante para no dar pistas por el tiempo
    /// de respuesta.
    /// </summary>
    public static long? FindMatchingStep(byte[] secret, string code, DateTimeOffset now)
    {
        var received = Encoding.ASCII.GetBytes(code);
        var current = GetStep(now);

        for (var step = current - 1; step <= current + 1; step++)
        {
            var expected = Encoding.ASCII.GetBytes(ComputeCode(secret, step));

            if (CryptographicOperations.FixedTimeEquals(expected, received))
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>Base32 (RFC 4648) sin relleno, el formato de las apps autenticadoras.</summary>
    public static string ToBase32(byte[] data)
    {
        var result = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;

        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bits += 8;

            while (bits >= 5)
            {
                result.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            result.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return result.ToString();
    }

    public static byte[] FromBase32(string text)
    {
        var clean = text.Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant();
        var result = new List<byte>(clean.Length * 5 / 8);
        int buffer = 0, bits = 0;

        foreach (var character in clean)
        {
            var value = Base32Alphabet.IndexOf(character);

            if (value < 0)
            {
                throw new FormatException("Invalid base32 character.");
            }

            buffer = (buffer << 5) | value;
            bits += 5;

            if (bits >= 8)
            {
                result.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }

        return result.ToArray();
    }
}
