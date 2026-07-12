using Synaptix.Shared.Contracts.Realtime.GameEvents;

namespace Synaptix.Backend.Application.GameEvents
{
    public interface IGameEventNotifier
    {
        Task NotifyEliminationAsync(GameEventEliminationMessage message, CancellationToken ct);
        Task NotifyEventClosedAsync(GameEventClosedMessage message, CancellationToken ct);
        Task NotifyRoundStartedAsync(ChampionRoundStartedMessage message, CancellationToken ct);
        Task NotifyRoundResolvedAsync(ChampionRoundResolvedMessage message, CancellationToken ct);
        Task NotifyMatchEndedAsync(ChampionMatchEndedMessage message, CancellationToken ct);
        Task NotifyDuelStartedAsync(ChampionDuelStartedMessage message, CancellationToken ct);
        Task NotifyDuelResolvedAsync(ChampionDuelResolvedMessage message, CancellationToken ct);
    }

    public sealed class NullGameEventNotifier : IGameEventNotifier
    {
        public Task NotifyEliminationAsync(GameEventEliminationMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyEventClosedAsync(GameEventClosedMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyRoundStartedAsync(ChampionRoundStartedMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyRoundResolvedAsync(ChampionRoundResolvedMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyMatchEndedAsync(ChampionMatchEndedMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyDuelStartedAsync(ChampionDuelStartedMessage message, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyDuelResolvedAsync(ChampionDuelResolvedMessage message, CancellationToken ct) => Task.CompletedTask;
    }
}
