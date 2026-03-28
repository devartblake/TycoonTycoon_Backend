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
}
