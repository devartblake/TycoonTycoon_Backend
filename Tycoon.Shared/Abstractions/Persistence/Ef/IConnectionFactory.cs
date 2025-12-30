using System.Data.Common;

namespace Tycoon.Shared.Abstractions.Persistence.Ef
{
    /// <summary>
    ///     The connection factory interface.
    /// </summary>
    public interface IConnectionFactory : IDisposable
    {
        Task<DbConnection> GetOrCreateConnectionAsync();
    }
}
