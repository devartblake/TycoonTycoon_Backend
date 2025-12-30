namespace Tycoon.Shared.Abstractions.Persistence
{
    /// <summary>
    ///     The migration schema interface.
    /// </summary>
    public interface IMigrationSchema
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
