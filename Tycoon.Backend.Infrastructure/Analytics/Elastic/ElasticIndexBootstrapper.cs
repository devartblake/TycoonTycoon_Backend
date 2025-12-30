using Elastic.Clients.Elasticsearch;

namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    /// <summary>
    /// Ensures Elasticsearch templates and base indices exist. Idempotent.
    /// </summary>
    public sealed class ElasticIndexBootstrapper
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticAdmin _admin;
        private readonly ElasticOptions _opt;

        public ElasticIndexBootstrapper(
            ElasticsearchClient client,
            ElasticAdmin admin,
            ElasticOptions opt)
        {
            _client = client;
            _admin = admin;
            _opt = opt;
        }

        public async Task EnsureCreatedAsync(CancellationToken ct)
        {
            // Step 6: templates (no ILM required)
            await _admin.EnsureTemplatesAsync(ct);

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
