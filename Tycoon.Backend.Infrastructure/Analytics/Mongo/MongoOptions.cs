namespace Tycoon.Backend.Infrastructure.Analytics.Mongo
{
    public sealed class MongoOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Database { get; set; } = "tycoon_analytics";
    }
}
