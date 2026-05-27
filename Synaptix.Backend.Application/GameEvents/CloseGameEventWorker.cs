using Mediator;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed class CloseGameEventWorker(IMediator mediator)
    {
        public async Task RunAsync(Guid gameEventId, CancellationToken ct)
        {
            await mediator.Send(new CloseGameEventAndDistributePrizes(gameEventId), ct);
        }
    }
}
