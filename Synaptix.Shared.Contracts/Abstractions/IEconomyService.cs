using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Shared.Contracts.Abstractions;

public interface IEconomyService
{
    Task<EconomyTxnResultDto> ApplyAsync(CreateEconomyTxnRequest req, CancellationToken ct);
    Task<EconomyHistoryDto> GetHistoryAsync(Guid playerId, int page, int pageSize, CancellationToken ct);
    Task<EconomyTxnResultDto> RollbackByEventIdAsync(Guid eventId, string reason, CancellationToken ct);
    Task<AdminPlayerEconomyDto?> GetPlayerSummaryAsync(Guid playerId, CancellationToken ct);
    Task<AdminEconomyStatsDto> GetEconomyStatsAsync(CancellationToken ct);
}
