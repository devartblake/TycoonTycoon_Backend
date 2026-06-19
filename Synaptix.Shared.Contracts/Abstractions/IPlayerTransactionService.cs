using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Shared.Contracts.Abstractions;

public interface IPlayerTransactionService
{
    Task<PlayerTransactionResultDto> ExecuteAsync(CreatePlayerTransactionRequest req, CancellationToken ct);
    Task<PlayerTransactionResultDto> DisputeAsync(DisputePlayerTransactionRequest req, CancellationToken ct);
    Task<PlayerTransactionResultDto> ReverseAsync(ReversePlayerTransactionRequest req, CancellationToken ct);
    Task<PlayerTransactionHistoryDto> GetHistoryAsync(Guid? playerId, Guid? correlatedEventId, int page, int pageSize, CancellationToken ct);
    Task<PlayerTransactionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct);
}
