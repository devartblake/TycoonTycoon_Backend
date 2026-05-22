using Synaptix.Shared.Abstractions.Core.Domain.Events;

namespace Synaptix.Shared.Abstractions.Core.Domain
{
    public interface IHaveAggregate : IHaveDomainEvents, IHaveAggregateVersion { }

}
