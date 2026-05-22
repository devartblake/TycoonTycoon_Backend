using Ardalis.Specification;

namespace Synaptix.Shared.Abstractions.Persistence.Ef.Repository
{
    // from Ardalis.Specification
    public interface IReadRepository<T> : IReadRepositoryBase<T>
        where T : class;
}