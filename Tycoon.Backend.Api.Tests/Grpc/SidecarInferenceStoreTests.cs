using Tycoon.Backend.Api.Grpc;

namespace Tycoon.Backend.Api.Tests.Grpc;

public sealed class SidecarInferenceStoreTests
{
    [Fact]
    public async Task FileStore_Should_Return_Same_Id_For_Duplicate_Payload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-inference-{Guid.NewGuid():N}.jsonl");
        try
        {
            var store = new FileSidecarInferenceStore(path);

            var first = await store.StoreAsync("model-a", "entity-1", 0.42f, "{}", CancellationToken.None);
            var second = await store.StoreAsync("model-a", "entity-1", 0.42f, "{}", CancellationToken.None);

            Assert.Equal(first, second);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task FileStore_Should_Reload_Idempotency_Index_From_Disk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-inference-{Guid.NewGuid():N}.jsonl");
        try
        {
            var firstStore = new FileSidecarInferenceStore(path);
            var firstId = await firstStore.StoreAsync("model-a", "entity-1", 0.42f, "{}", CancellationToken.None);

            var secondStore = new FileSidecarInferenceStore(path);
            var secondId = await secondStore.StoreAsync("model-a", "entity-1", 0.42f, "{}", CancellationToken.None);

            Assert.Equal(firstId, secondId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task FileStore_Should_Ignore_Malformed_Lines_When_Reloading()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-inference-{Guid.NewGuid():N}.jsonl");
        try
        {
            await File.WriteAllLinesAsync(path,
            [
                "this is not json",
                """{"Id":"abc123","ModelName":"model-a","EntityId":"entity-1","Score":0.42,"MetadataJson":"{}","RecordedAtUtc":"2026-03-28T00:00:00Z"}"""
            ]);

            var store = new FileSidecarInferenceStore(path);
            var id = await store.StoreAsync("model-a", "entity-1", 0.42f, "{}", CancellationToken.None);

            Assert.Equal("abc123", id);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task FileStore_Should_Respect_Cancellation_Token()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sidecar-inference-{Guid.NewGuid():N}.jsonl");
        try
        {
            var store = new FileSidecarInferenceStore(path);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                store.StoreAsync("model-a", "entity-1", 0.42f, "{}", cts.Token));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
