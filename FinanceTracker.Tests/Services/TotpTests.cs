using System.Text;
using FinanceTracker.Infrastructure.Services;

namespace FinanceTracker.Tests.Services;

public class TotpTests
{
    // Vectores de prueba del RFC 6238 (apendice B), con SHA-1: clave ASCII
    // "12345678901234567890" y codigos de 8 digitos.
    private static readonly byte[] RfcSeed = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1111111111L, "14050471")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    [InlineData(20000000000L, "65353130")]
    public void ComputeCode_ShouldMatchRfc6238TestVectors(long unixTime, string expected)
    {
        var step = Totp.GetStep(DateTimeOffset.FromUnixTimeSeconds(unixTime));

        Assert.Equal(expected, Totp.ComputeCode(RfcSeed, step, digits: 8));
    }

    [Fact]
    public void FindMatchingStep_ShouldAcceptTheCurrentCode_AndOneStepOfClockDrift()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1234567890);
        var step = Totp.GetStep(now);

        Assert.Equal(step, Totp.FindMatchingStep(RfcSeed, Totp.ComputeCode(RfcSeed, step), now));
        Assert.Equal(step - 1, Totp.FindMatchingStep(RfcSeed, Totp.ComputeCode(RfcSeed, step - 1), now));
        Assert.Equal(step + 1, Totp.FindMatchingStep(RfcSeed, Totp.ComputeCode(RfcSeed, step + 1), now));
    }

    [Fact]
    public void FindMatchingStep_ShouldRejectCodesOutsideTheWindow()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1234567890);
        var step = Totp.GetStep(now);

        Assert.Null(Totp.FindMatchingStep(RfcSeed, Totp.ComputeCode(RfcSeed, step - 2), now));
        Assert.Null(Totp.FindMatchingStep(RfcSeed, "000000x", now));
    }

    [Fact]
    public void Base32_ShouldMatchRfc4648_AndRoundTrip()
    {
        // RFC 4648, seccion 10 (sin el relleno "=").
        Assert.Equal("MZXW6YTBOI", Totp.ToBase32(Encoding.ASCII.GetBytes("foobar")));
        Assert.Equal("foobar", Encoding.ASCII.GetString(Totp.FromBase32("mzxw 6ytb oi======")));

        var random = Totp.GenerateSecret();
        Assert.Equal(random, Totp.FromBase32(Totp.ToBase32(random)));
    }
}
