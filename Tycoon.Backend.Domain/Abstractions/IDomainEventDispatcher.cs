using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Abstractions
{
    /// <summary>
    /// Dispatches domain events raised by aggregate roots.
    /// Implemented in Infrastructure and called after EF commits.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct);
    }
}
