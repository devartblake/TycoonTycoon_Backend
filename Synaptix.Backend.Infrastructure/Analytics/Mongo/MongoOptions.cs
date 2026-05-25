namespace Synaptix.Backend.Infrastructure.Analytics.Mongo
{
    public sealed class MongoOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Database { get; set; } = "synaptix_analytics";
    }
}
