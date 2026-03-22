using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Config;

public interface IGameBalancePolicyService
{
    Task<GameBalanceConfigDto> GetConfigAsync(CancellationToken ct);
    Task<GameBalanceConfigDto> UpdateConfigAsync(UpdateGameBalanceConfigRequest req, CancellationToken ct);
    Task<(int SessionNumber, int Discount)> StartSessionAsync(Guid playerId, CancellationToken ct);
    Task<(bool Granted, int RemainingToday)> ClaimDailyTicketAsync(Guid playerId, CancellationToken ct);
    Task<int> ReportLossAsync(Guid playerId, CancellationToken ct);
    Task ResetLossAsync(Guid playerId, CancellationToken ct);
    Task<ModeEntryDecisionDto> TryEnterModeAsync(Guid playerId, string mode, CancellationToken ct);
}
