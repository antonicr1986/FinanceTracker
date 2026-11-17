using System.Security.Cryptography;
using System.Text;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// Cifra y descifra los secretos TOTP del MFA antes de guardarlos en la base de
/// datos, con AES-256-GCM. Si alguien se lleva una copia de la base de datos
/// (backup, inyeccion SQL), sin esta clave no puede generar codigos.
///
/// La clave (Mfa:EncryptionKey, 32 bytes en base64) es distinta de Jwt:Key a
/// proposito: rotar la de los JWT no debe dejar inservible el MFA de nadie.
/// En Azure vive en Key Vault, como el resto de secretos.
///
/// Formato guardado: base64(nonce de 12 bytes | tag de 16 bytes | texto cifrado).
/// GCM ademas autentica: un valor manipulado en la base de datos no se descifra.
/// </summary>
public sealed class MfaSecretProtector
{
    public const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public MfaSecretProtector(byte[] key)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException($"The MFA encryption key must be {KeySize} bytes (AES-256).", nameof(key));
        }

        _key = key;
    }

    /// <summary>
    /// Crea el protector a partir de la configuracion. Falla al arrancar, igual
    /// que con Jwt:Key, en vez de fallar mas tarde con el primer usuario.
    /// </summary>
    public static MfaSecretProtector FromBase64(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException("MFA encryption key (Mfa:EncryptionKey) is not configured.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("MFA encryption key (Mfa:EncryptionKey) must be base64.");
        }

        if (key.Length != KeySize)
        {
            throw new InvalidOperationException($"MFA encryption key (Mfa:EncryptionKey) must be {KeySize} bytes once decoded.");
        }

        return new MfaSecretProtector(key);
    }

    public string Protect(string plaintext)
    {
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    /// <summary>
    /// Lanza CryptographicException si el valor se ha manipulado o se cifro
    /// con otra clave.
    /// </summary>
    public string Unprotect(string protectedValue)
    {
        var data = Convert.FromBase64String(protectedValue);

        if (data.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Protected MFA secret is too short.");
        }

        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var cipher = data.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}
