using Elastic.Clients.Elasticsearch;

namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    /// <summary>
    /// Ensures Elasticsearch base indices exist. Idempotent.
    /// </summary>
    public sealed class ElasticIndexBootstrapper
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticOptions _opt;

        public ElasticIndexBootstrapper(
            ElasticsearchClient client,
            ElasticOptions opt)
        {
            _client = client;
            _opt = opt;
        }

        public async Task EnsureCreatedAsync(CancellationToken ct)
        {
            // Create base indices explicitly so you can index immediately.
            // If you later switch to rollover, you will replace these with alias bootstrapping (Step 6.5).
            await EnsureIndexExistsAsync(_opt.DailyWriteAlias, ct);
            await EnsureIndexExistsAsync(_opt.PlayerDailyWriteAlias, ct);
        }

        private async Task EnsureIndexExistsAsync(string indexName, CancellationToken ct)
        {
            var exists = await _client.Indices.ExistsAsync(indexName, ct);
            if (exists.Exists) return;

            var create = await _client.Indices.CreateAsync(indexName, ct);
            if (!create.IsValidResponse)
                throw new InvalidOperationException($"Failed creating index '{indexName}': {create.ElasticsearchServerError}");
        }
    }
}
