using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Infrastructure.Analytics.Mongo;

namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    /// <summary>
    /// Replays Mongo rollup documents into Elasticsearch using deterministic IDs (doc.Id).
    /// Idempotent: re-running overwrites the same _id documents.
    /// </summary>
    public sealed class ElasticRollupRebuilder : IRollupRebuilder
    {
        private readonly MongoRollupReader _reader;
        private readonly ElasticsearchClient _client;
        private readonly ElasticOptions _opt;

        public ElasticRollupRebuilder(
            MongoRollupReader reader,
            ElasticsearchClient client,
            ElasticOptions opt)
        {
            _reader = reader;
            _client = client;
            _opt = opt;
        }

        // ----------------------------
        // IRollupRebuilder (required)
        // ----------------------------

        public Task RebuildDailyAsync(DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken ct)
            => RebuildDailyCoreAsync(fromUtcDate, toUtcDate, ct);

        public Task RebuildPlayerDailyAsync(DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken ct)
            => RebuildPlayerDailyCoreAsync(fromUtcDate, toUtcDate, ct);

        public async Task RebuildElasticFromMongoAsync(DateOnly? fromUtcDate, DateOnly? toUtcDate, CancellationToken ct)
        {
            await RebuildDailyCoreAsync(fromUtcDate, toUtcDate, ct);
            await RebuildPlayerDailyCoreAsync(fromUtcDate, toUtcDate, ct);
        }

        // ----------------------------
        // Core implementations
        // ----------------------------

        private async Task RebuildDailyCoreAsync(DateOnly? fromUtcDate, DateOnly? toUtcDate, CancellationToken ct)
        {
            const int batchSize = 500;
            var ops = new List<IBulkOperation>(batchSize);

            await foreach (var doc in _reader.ReadDailyAsync(fromUtcDate, toUtcDate, ct))
            {
                TryComputeDerivedFields(doc);

                ops.Add(new BulkIndexOperation<object>(doc)
                {
                    Index = _opt.DailyWriteAlias, // alias or index
                    Id = doc.Id
                });

                if (ops.Count >= batchSize)
                {
                    await FlushAsync(ops, ct);
                    ops.Clear();
                }
            }

            if (ops.Count > 0)
                await FlushAsync(ops, ct);
        }

        private async Task RebuildPlayerDailyCoreAsync(DateOnly? fromUtcDate, DateOnly? toUtcDate, CancellationToken ct)
        {
            const int batchSize = 500;
            var ops = new List<IBulkOperation>(batchSize);

            await foreach (var doc in _reader.ReadPlayerDailyAsync(fromUtcDate, toUtcDate, ct))
            {
                TryComputeDerivedFields(doc);

                ops.Add(new BulkIndexOperation<object>(doc)
                {
                    Index = _opt.PlayerDailyWriteAlias, // alias or index
                    Id = doc.Id
                });

                if (ops.Count >= batchSize)
                {
                    await FlushAsync(ops, ct);
                    ops.Clear();
                }
            }

            if (ops.Count > 0)
                await FlushAsync(ops, ct);
        }

        private async Task FlushAsync(List<IBulkOperation> ops, CancellationToken ct)
        {
            var resp = await _client.BulkAsync(new BulkRequest
            {
                Operations = new BulkOperationsCollection(ops)
            }, ct);

            if (!resp.IsValidResponse)
                throw new InvalidOperationException(
                    $"Elastic bulk rebuild failed: {resp.ElasticsearchServerError}");

            Tycoon.Shared.Observability.TycoonObservability.RollupRebuildDocsIndexed.Add(ops.Count);
        }

        /// <summary>
        /// Best-effort derived fields: safe if your rollup models include Accuracy/AvgAnswerTimeMs.
        /// If they don't, this no-ops.
        /// </summary>
        private static void TryComputeDerivedFields(dynamic doc)
        {
            try
            {
                long total = doc.TotalAnswers;
                long correct = doc.CorrectAnswers;
                long sum = doc.SumAnswerTimeMs;

                doc.Accuracy = total <= 0 ? 0d : (double)correct / total;
                doc.AvgAnswerTimeMs = total <= 0 ? 0d : (double)sum / total;
            }
            catch
            {
                // Ignore: model may not include these properties.
            }
        }
    }
}
