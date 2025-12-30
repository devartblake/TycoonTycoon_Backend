namespace Tycoon.Shared.Abstractions.Persistence
{
    /// <summary>
    ///     The data seeder interface.
    /// </summary>
    public interface IDataSeeder
    {
        Task SeedAllAsync(CancellationToken cancellationToken);
        int Order { get; }
    }
}
