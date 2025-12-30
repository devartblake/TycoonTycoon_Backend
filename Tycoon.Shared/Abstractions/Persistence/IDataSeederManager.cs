namespace Tycoon.Shared.Abstractions.Persistence
{
    /// <summary>
    ///     The data seeder manager interface.
    /// </summary>
    public interface IDataSeederManager
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
