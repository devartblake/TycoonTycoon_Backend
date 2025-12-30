using Tycoon.Shared.Abstractions.Core.Domain.Events;

namespace Tycoon.Shared.Abstractions.Core.Domain
{
    public interface IHaveAggregate : IHaveDomainEvents, IHaveAggregateVersion { }

}
