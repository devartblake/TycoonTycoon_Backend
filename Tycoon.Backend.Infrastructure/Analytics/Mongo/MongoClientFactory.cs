using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Tycoon.Backend.Infrastructure.Analytics.Mongo
{
    public sealed class MongoClientFactory
    {
        private readonly IMongoDatabase _db;

        public MongoClientFactory(IOptions<MongoOptions> opt)
        {
            var client = new MongoClient(opt.Value.ConnectionString);
            _db = client.GetDatabase(opt.Value.Database);
        }

        public IMongoDatabase Database => _db;
    }
}
