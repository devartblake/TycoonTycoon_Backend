using Ardalis.Specification;

namespace Tycoon.Shared.Abstractions.Persistence.Ef.Repository
{
    // from Ardalis.Specification
    public interface IReadRepository<T> : IReadRepositoryBase<T>
        where T : class;
}