using Ardalis.Specification;

namespace Tycoon.Shared.Abstractions.Persistence.Ef.Repository
{
    // from Ardalis.Specification
    public interface IRepository<T> : IRepositoryBase<T>
        where T : class;
}
