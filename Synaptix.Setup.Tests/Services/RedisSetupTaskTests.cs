using System.Net;
using Microsoft.Extensions.Configuration;
using Synaptix.Setup.Services;

namespace Synaptix.Setup.Tests.Services;

public sealed class RedisSetupTaskTests
{
    [Fact]
    public void BuildOptions_ParsesCompleteStructuredConnectionString()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:redis"] = "cache.internal:6380,password=secret,defaultDatabase=3,ssl=true,abortConnect=true",
            ["REDIS_HOST"] = "ignored",
            ["REDIS_PORT"] = "6379",
        });

        var result = RedisSetupTask.BuildOptions(cfg);
        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(result!.EndPoints));

        Assert.Equal("cache.internal", endpoint.Host);
        Assert.Equal(6380, endpoint.Port);
        Assert.Equal("secret", result.Password);
        Assert.Equal(3, result.DefaultDatabase);
        Assert.True(result.Ssl);
        Assert.False(result.AbortOnConnectFail);
        Assert.Equal(5000, result.ConnectTimeout);
        Assert.Equal(5000, result.SyncTimeout);
    }

    [Fact]
    public void BuildOptions_UsesRawRedisValuesWhenConnectionStringIsAbsent()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>
        {
            ["REDIS_HOST"] = "redis",
            ["REDIS_PORT"] = "6379",
            ["REDIS_PASSWORD"] = "secret",
        });

        var result = RedisSetupTask.BuildOptions(cfg);
        var endpoint = Assert.IsType<DnsEndPoint>(Assert.Single(result!.EndPoints));

        Assert.Equal("redis", endpoint.Host);
        Assert.Equal(6379, endpoint.Port);
        Assert.Equal("secret", result.Password);
    }

    [Fact]
    public void BuildOptions_ReturnsNullWithoutConnectionStringOrPassword()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>());

        Assert.Null(RedisSetupTask.BuildOptions(cfg));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
