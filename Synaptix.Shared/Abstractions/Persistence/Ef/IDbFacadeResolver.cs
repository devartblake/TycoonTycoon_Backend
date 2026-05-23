using Microsoft.EntityFrameworkCore.Infrastructure;
namespace Synaptix.Shared.Abstractions.Persistence.Ef
{
    /// <summary>
    ///     The database facade resolver interface.
    /// </summary>
    public interface IDbFacadeResolver
    {
        DatabaseFacade Database { get; }
    }
}
