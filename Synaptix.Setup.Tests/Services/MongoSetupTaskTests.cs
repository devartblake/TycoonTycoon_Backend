using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Synaptix.Setup.Services;

namespace Synaptix.Setup.Tests.Services;

public sealed class MongoSetupTaskTests
{
    [Fact]
    public void BuildAdminConnectionString_PrefersMongoAdminConnectionString()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mongoAdmin"] = "mongodb://admin.example:27017/?authSource=admin",
            ["ConnectionStrings:mongo"] = "mongodb://runtime.example:27017/app",
        });

        var result = MongoSetupTask.BuildAdminConnectionString(cfg);

        Assert.Equal("mongodb://admin.example:27017/?authSource=admin", result);
    }

    [Fact]
    public void BuildAdminConnectionString_FallsBackToLegacyMongoConnectionString()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mongo"] = "mongodb://legacy.example:27017/?authSource=admin",
        });

        var result = MongoSetupTask.BuildAdminConnectionString(cfg);

        Assert.Equal("mongodb://legacy.example:27017/?authSource=admin", result);
    }

    [Fact]
    public void BuildAdminConnectionString_UsesRawRootValuesAndEscapesPassword()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>
        {
            ["MONGO_HOST"] = "mongodb",
            ["MONGO_PORT"] = "27017",
            ["MONGO_INITDB_ROOT_USERNAME"] = "root",
            ["MONGO_INITDB_ROOT_PASSWORD"] = "p@ss:#/word",
        });

        var result = MongoSetupTask.BuildAdminConnectionString(cfg);
        var parsed = new MongoUrl(result);

        Assert.Equal("mongodb", parsed.Server.Host);
        Assert.Equal(27017, parsed.Server.Port);
        Assert.Equal("root", parsed.Username);
        Assert.Equal("p@ss:#/word", parsed.Password);
        Assert.Equal("admin", parsed.AuthenticationSource);
    }

    [Fact]
    public void BuildAdminConnectionString_ReturnsNullWithoutRootPassword()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>());

        Assert.Null(MongoSetupTask.BuildAdminConnectionString(cfg));
    }

    [Fact]
    public void ResolveAuthDatabase_DefaultsToApplicationDatabase()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>());

        Assert.Equal("synaptix_analytics", MongoSetupTask.ResolveAuthDatabase(cfg, "synaptix_analytics"));
    }

    [Fact]
    public void ResolveAuthDatabase_UsesConfiguredDatabase()
    {
        var cfg = BuildConfiguration(new Dictionary<string, string?>
        {
            ["MONGO_AUTH_DB"] = "custom_auth",
        });

        Assert.Equal("custom_auth", MongoSetupTask.ResolveAuthDatabase(cfg, "synaptix_analytics"));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
