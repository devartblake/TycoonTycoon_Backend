using Tycoon.Shared.Contracts.Realtime.Territory;

namespace Tycoon.Backend.Application.Territory
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
