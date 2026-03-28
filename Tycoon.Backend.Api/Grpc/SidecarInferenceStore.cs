using System.Collections.Concurrent;
using System.Text.Json;

namespace Tycoon.Backend.Api.Grpc;

public interface ISidecarInferenceStore
{
    Task<string> StoreAsync(string modelName, string entityId, float score, string metadataJson, CancellationToken ct);
}

public sealed class InMemorySidecarInferenceStore : ISidecarInferenceStore
{
    private readonly ConcurrentDictionary<string, InferenceRecord> _records = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _idempotencyIndex = new(StringComparer.Ordinal);

    public Task<string> StoreAsync(string modelName, string entityId, float score, string metadataJson, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var key = BuildIdempotencyKey(modelName, entityId, score, metadataJson);
        var id = _idempotencyIndex.GetOrAdd(key, _ =>
        {
            var newId = Guid.NewGuid().ToString("N");
            _records[newId] = new InferenceRecord(newId, modelName, entityId, score, metadataJson, DateTimeOffset.UtcNow);
            return newId;
        });

        return Task.FromResult(id);
    }

    internal int Count => _records.Count;

    private static string BuildIdempotencyKey(string modelName, string entityId, float score, string metadataJson)
        => $"{modelName.Trim()}|{entityId.Trim()}|{score:R}|{metadataJson.Trim()}";

    private sealed record InferenceRecord(
        string Id,
        string ModelName,
        string EntityId,
        float Score,
        string MetadataJson,
        DateTimeOffset RecordedAtUtc);
}

public sealed class FileSidecarInferenceStore : ISidecarInferenceStore
{
    private readonly string _storeFilePath;
    private readonly ConcurrentDictionary<string, string> _idempotencyIndex = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileSidecarInferenceStore(string storeFilePath)
    {
        _storeFilePath = storeFilePath;
        var dir = Path.GetDirectoryName(_storeFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(_storeFilePath))
            return;

        foreach (var line in File.ReadLines(_storeFilePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var rec = JsonSerializer.Deserialize<FileInferenceRecord>(line);
                if (rec is null) continue;

                _idempotencyIndex.TryAdd(BuildIdempotencyKey(rec.ModelName, rec.EntityId, rec.Score, rec.MetadataJson), rec.Id);
            }
            catch
            {
                // Skip malformed legacy lines and continue loading valid entries.
            }
        }
    }

    public async Task<string> StoreAsync(string modelName, string entityId, float score, string metadataJson, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var key = BuildIdempotencyKey(modelName, entityId, score, metadataJson);
        if (_idempotencyIndex.TryGetValue(key, out var existingId))
            return existingId;

        await _gate.WaitAsync(ct);
        try
        {
            if (_idempotencyIndex.TryGetValue(key, out existingId))
                return existingId;

            var id = Guid.NewGuid().ToString("N");
            var record = new FileInferenceRecord(id, modelName, entityId, score, metadataJson, DateTimeOffset.UtcNow);
            var line = JsonSerializer.Serialize(record);
            await File.AppendAllTextAsync(_storeFilePath, line + Environment.NewLine, ct);
            _idempotencyIndex[key] = id;
            return id;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string BuildIdempotencyKey(string modelName, string entityId, float score, string metadataJson)
        => $"{modelName.Trim()}|{entityId.Trim()}|{score:R}|{metadataJson.Trim()}";

    private sealed record FileInferenceRecord(
        string Id,
        string ModelName,
        string EntityId,
        float Score,
        string MetadataJson,
        DateTimeOffset RecordedAtUtc);
}
