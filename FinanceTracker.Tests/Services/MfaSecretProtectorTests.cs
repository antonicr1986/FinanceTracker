using System.Security.Cryptography;
using FinanceTracker.Infrastructure.Services;

namespace FinanceTracker.Tests.Services;

public class MfaSecretProtectorTests
{
    private static MfaSecretProtector NewProtector() =>
        new(RandomNumberGenerator.GetBytes(MfaSecretProtector.KeySize));

    [Fact]
    public void Unprotect_ShouldReturnTheOriginalSecret()
    {
        var protector = NewProtector();

        var stored = protector.Protect("JBSWY3DPEHPK3PXP");

        Assert.NotEqual("JBSWY3DPEHPK3PXP", stored);
        Assert.Equal("JBSWY3DPEHPK3PXP", protector.Unprotect(stored));
    }

    [Fact]
    public void Protect_ShouldGiveADifferentValueEachTime()
    {
        // Nonce aleatorio: el mismo secreto no deja la misma huella dos veces.
        var protector = NewProtector();

        Assert.NotEqual(protector.Protect("JBSWY3DPEHPK3PXP"), protector.Protect("JBSWY3DPEHPK3PXP"));
    }

    [Fact]
    public void Unprotect_ShouldFail_WhenTheStoredValueWasTampered()
    {
        var protector = NewProtector();
        var bytes = Convert.FromBase64String(protector.Protect("JBSWY3DPEHPK3PXP"));
        bytes[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Unprotect_ShouldFail_WithAnotherKey()
    {
        var stored = NewProtector().Protect("JBSWY3DPEHPK3PXP");

        Assert.ThrowsAny<CryptographicException>(() => NewProtector().Unprotect(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-base64!")]
    [InlineData("c2hvcnQ=")] // "short": 5 bytes, no 32
    public void FromBase64_ShouldFail_WhenTheKeyIsMissingOrInvalid(string? key)
    {
        Assert.Throws<InvalidOperationException>(() => MfaSecretProtector.FromBase64(key));
    }

    [Fact]
    public void FromBase64_ShouldAcceptA32ByteKey()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(MfaSecretProtector.KeySize));

        var protector = MfaSecretProtector.FromBase64(key);

        Assert.Equal("abc", protector.Unprotect(protector.Protect("abc")));
    }
}
