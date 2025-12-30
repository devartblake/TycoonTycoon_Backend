namespace Tycoon.Shared.Abstractions.Persistence
{
    /// <summary>
    ///     The migration schema interface.
    /// </summary>
    public interface IMigrationManager
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
