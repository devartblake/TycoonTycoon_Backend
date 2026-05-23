using Synaptix.Shared.Contracts.Realtime.Territory;

namespace Synaptix.Backend.Application.Territory
{
    public interface ITerritoryNotifier
    {
        Task NotifyTileCapturedAsync(TerritoryCaptureMesage message, CancellationToken ct);
    }

    public sealed class NullTerritoryNotifier : ITerritoryNotifier
    {
        public Task NotifyTileCapturedAsync(TerritoryCaptureMesage message, CancellationToken ct) => Task.CompletedTask;
    }
}
