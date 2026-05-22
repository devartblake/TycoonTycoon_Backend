using System.Data.Common;

namespace Synaptix.Shared.Abstractions.Persistence.Ef
{
    /// <summary>
    ///     The connection factory interface.
    /// </summary>
    public interface IConnectionFactory : IDisposable
    {
        Task<DbConnection> GetOrCreateConnectionAsync();
    }
}
