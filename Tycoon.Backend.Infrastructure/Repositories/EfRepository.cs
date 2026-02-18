using Microsoft.EntityFrameworkCore;

namespace Tycoon.Backend.Infrastructure.Repositories
{
    /// <summary>
    /// Lightweight generic repository helper.
    /// Does NOT call SaveChangesAsync — callers own the commit boundary
    /// to keep multiple operations inside a single Unit of Work.
    /// </summary>
    public class EfRepository<T>(DbContext db) where T : class
    {
        public Task<T?> GetAsync(Guid id) => db.Set<T>().FindAsync(id).AsTask();

        /// <summary>
        /// Stages the entity for insertion. Caller must call SaveChangesAsync
        /// (either via IAppDb or the DbContext) to persist.
        /// </summary>
        public void Add(T entity) => db.Add(entity);
    }
}