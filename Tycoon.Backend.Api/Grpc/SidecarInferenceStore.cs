using System.Collections.Concurrent;

namespace Tycoon.Backend.Api.Grpc;

public interface ISidecarInferenceStore
{
    Task<string> StoreAsync(string modelName, string entityId, float score, string metadataJson, CancellationToken ct);
}

public sealed class InMemorySidecarInferenceStore : ISidecarInferenceStore
{
    private readonly ConcurrentDictionary<string, InferenceRecord> _records = new(StringComparer.Ordinal);

    public Task<string> StoreAsync(string modelName, string entityId, float score, string metadataJson, CancellationToken ct)
    {
        var id = Guid.NewGuid().ToString("N");
        _records[id] = new InferenceRecord(id, modelName, entityId, score, metadataJson, DateTimeOffset.UtcNow);
        return Task.FromResult(id);
    }

    private sealed record InferenceRecord(
        string Id,
        string ModelName,
        string EntityId,
        float Score,
        string MetadataJson,
        DateTimeOffset RecordedAtUtc);
}
