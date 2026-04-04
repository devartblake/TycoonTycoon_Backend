namespace Tycoon.Shared.Abstractions.Core.Domain
{
    public interface IHaveIdentity<out TId> : IHaveIdentity
        where TId : notnull
    {
        new TId Id { get; }
        object IHaveIdentity.Id => Id;
    }

    public interface IHaveIdentity
    {
        object Id { get; }
    }

}
