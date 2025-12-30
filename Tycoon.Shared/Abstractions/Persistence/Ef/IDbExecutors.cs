using Microsoft.Extensions.DependencyInjection;

namespace Tycoon.Shared.Abstractions.Persistence.Ef
{
    /// <summary>
    ///     The database executors interface.
    /// </summary>
    public interface IDbExecutors
    {
        public void Register(IServiceCollection services);
    }
}
