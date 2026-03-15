using Tycoon.Shared.Contracts.Realtime.GameEvents;

namespace Tycoon.Backend.Application.GameEvents
{
    public interface IGameEventNotifier
    {
        Task NotifyEliminationAsync(GameEventEliminationMessage message, CancellationToken ct);
        Task NotifyEventClosedAsync(GameEventClosedMessage message, CancellationToken ct);
    }

    public sealed class NullGameEventNotifier : IGameEventNotifier
    {
        public Task NotifyEliminationAsync(GameEventEliminationMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyEventClosedAsync(GameEventClosedMessage message, CancellationToken ct) => Task.CompletedTask;
    }
}
