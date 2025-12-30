using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    public sealed class ElasticRollupIndexer : IRollupIndexer
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticOptions _opt;
        private readonly ResiliencePipeline _retry;

        public ElasticRollupIndexer(IOptions<ElasticOptions> opt)
        {
            _opt = opt.Value;

            var settings = new ElasticsearchClientSettings(new Uri(_opt.Url))
                .EnableDebugMode();

            _client = new ElasticsearchClient(settings);

            _retry = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Constant
                })
                .Build();
        }

        public async Task IndexDailyRollupAsync(QuestionAnsweredDailyRollup rollup, CancellationToken ct)
        {
            await _retry.ExecuteAsync(async token =>
            {
                // Deterministic ID => idempotent indexing
                var resp = await _client.IndexAsync(rollup, i => i
                    .Index(_opt.DailyWriteAlias)
                    .Id(rollup.Id), token);

                if (!resp.IsValidResponse)
                    throw new InvalidOperationException($"Elastic indexing failed: {resp.ElasticsearchServerError}");
            }, ct);
        }

        public async Task IndexPlayerDailyRollupAsync(QuestionAnsweredPlayerDailyRollup rollup, CancellationToken ct)
        {
            await _retry.ExecuteAsync(async token =>
            {
                var resp = await _client.IndexAsync(rollup, i => i
                    .Index(_opt.PlayerDailyWriteAlias)
                    .Id(rollup.Id), token);

                if (!resp.IsValidResponse)
                    throw new InvalidOperationException($"Elastic indexing failed: {resp.ElasticsearchServerError}");
            }, ct);
        }

    }
}
