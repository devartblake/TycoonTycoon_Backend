using Tycoon.Shared.Core.Domain;

namespace Tycoon.Shared.Abstractions.Core.Domain
{
    public interface IEntity<out TId> : IHaveIdentity<TId>, IHaveCreator
        where TId : notnull;

    public interface IEntity<out TIdentity, in TId> : IEntity<TIdentity>
        where TIdentity : IIdentity<TId>;

    public interface IEntity : IEntity<EntityId>;
}
