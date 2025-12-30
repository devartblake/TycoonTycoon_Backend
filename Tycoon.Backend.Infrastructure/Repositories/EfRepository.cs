using Microsoft.EntityFrameworkCore;

namespace Tycoon.Backend.Infrastructure.Repositories
{
    public class EfRepository<T>(DbContext db) where T : class
    {
        public Task<T?> GetAsync(Guid id) => db.Set<T>().FindAsync(id).AsTask();
        public Task AddAsync(T entity) { db.Add(entity); return db.SaveChangesAsync(); }
    }
}
