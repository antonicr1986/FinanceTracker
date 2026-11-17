using System.Net;
using FinanceTracker.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Tests.Api;

public class AuthRateLimitingTests
{
    private static HttpContext ContextFrom(string ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }

    [Fact]
    public void Partition_ShouldRejectRequests_OverTheLimit()
    {
        // Arrange
        var partition = AuthRateLimiting.Partition(ContextFrom("203.0.113.10"));
        using var limiter = partition.Factory(partition.PartitionKey);

        // Act
        var allowed = Enumerable.Range(0, AuthRateLimiting.PermitLimit)
            .Select(_ => limiter.AttemptAcquire().IsAcquired)
            .ToList();

        var extra = limiter.AttemptAcquire();

        // Assert
        Assert.All(allowed, isAcquired => Assert.True(isAcquired));
        Assert.False(extra.IsAcquired);
    }

    [Fact]
    public void Partition_ShouldUseOneCounterPerClientIp()
    {
        // Act
        var first = AuthRateLimiting.Partition(ContextFrom("203.0.113.10"));
        var second = AuthRateLimiting.Partition(ContextFrom("198.51.100.20"));

        // Assert
        Assert.NotEqual(first.PartitionKey, second.PartitionKey);
    }
}
