namespace Tycoon.Backend.Application.Analytics.Abstractions
{
    public interface IRollupRebuilder
    {
        Task RebuildDailyAsync(DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken ct);
        Task RebuildPlayerDailyAsync(DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken ct);
        Task RebuildElasticFromMongoAsync(
            DateOnly? fromUtcDate,
            DateOnly? toUtcDate,
            CancellationToken ct);
    }
}
