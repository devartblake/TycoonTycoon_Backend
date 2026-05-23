using Synaptix.Shared.Contracts.Realtime.Guardians;

namespace Synaptix.Backend.Application.Guardians
{
    public interface IGuardianNotifier
    {
        Task NotifyGuardianChangedAsync(GuardianChangedMessage message, CancellationToken ct);
    }

    public sealed class NullGuardianNotifier : IGuardianNotifier
    {
        public Task NotifyGuardianChangedAsync(GuardianChangedMessage message, CancellationToken ct) => Task.CompletedTask;
    }
}
