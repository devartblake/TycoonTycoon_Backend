using Synaptix.Shared.Core.Domain;

namespace Synaptix.Shared.Abstractions.Core.Domain
{
    public interface IAggregate<out TId> : IEntity<TId>, IHaveAggregate
        where TId : notnull;

    public interface IAggregate<out TIdentity, TId> : IAggregate<TIdentity>
        where TIdentity : Identity<TId>;

    public interface IAggregate : IAggregate<AggregateId, long>;

}
