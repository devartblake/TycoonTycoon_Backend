using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Config;

public sealed class GameBalancePolicyService(IAppDb db) : IGameBalancePolicyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static GameBalanceConfigDto DefaultConfig() => new(
        MaxEnergy: 20,
        StartEnergy: 20,
        RegenMinutesPerEnergy: 10,
        DailyFreeEnergy: 5,
        AdEnergyMin: 2,
        AdEnergyMax: 4,
        LevelUpFullRefill: true,
        PremiumEnergyCapBonus: 5,
        PremiumRegenMultiplier: 1.25m,
        Modes:
        [
            new ModeBalanceRuleDto("casual", 3, null, false, 0),
            new ModeBalanceRuleDto("ranked", 4, null, false, 100),
            new ModeBalanceRuleDto("jackpot", 0, 3, true, 25),
            new ModeBalanceRuleDto("guardian", 5, 2, false, 150)
        ],
        Safeguards: new SafeguardConfigDto(
            FirstSessionsReducedCostCount: 3,
            FirstSessionsEnergyDiscount: 1,
            DailyFreeJackpotTickets: 1,
            ReviveBaseGemCost: 5,
            AlmostWinReviveDiscountPercent: 20,
            PityLossThreshold: 3,
            PityDifficultyReductionPercent: 0.10m
        ),
        UpdatedAtUtc: DateTimeOffset.UtcNow
    );

    public async Task<GameBalanceConfigDto> GetConfigAsync(CancellationToken ct)
    {
        var entity = await db.GameBalanceConfigs.FirstOrDefaultAsync(x => x.Id == "default", ct);
        if (entity is null)
        {
            var created = DefaultConfig();
            db.GameBalanceConfigs.Add(new GameBalanceConfig(JsonSerializer.Serialize(created, JsonOptions)));
            await db.SaveChangesAsync(ct);
            return created;
        }

        try
        {
            return JsonSerializer.Deserialize<GameBalanceConfigDto>(entity.ConfigJson, JsonOptions) ?? DefaultConfig();
        }
        catch
        {
            return DefaultConfig();
        }
    }

    public async Task<GameBalanceConfigDto> UpdateConfigAsync(UpdateGameBalanceConfigRequest req, CancellationToken ct)
    {
        var current = await GetConfigAsync(ct);
        var next = new GameBalanceConfigDto(
            MaxEnergy: req.MaxEnergy ?? current.MaxEnergy,
            StartEnergy: req.StartEnergy ?? current.StartEnergy,
            RegenMinutesPerEnergy: req.RegenMinutesPerEnergy ?? current.RegenMinutesPerEnergy,
            DailyFreeEnergy: req.DailyFreeEnergy ?? current.DailyFreeEnergy,
            AdEnergyMin: req.AdEnergyMin ?? current.AdEnergyMin,
            AdEnergyMax: req.AdEnergyMax ?? current.AdEnergyMax,
            LevelUpFullRefill: req.LevelUpFullRefill ?? current.LevelUpFullRefill,
            PremiumEnergyCapBonus: req.PremiumEnergyCapBonus ?? current.PremiumEnergyCapBonus,
            PremiumRegenMultiplier: req.PremiumRegenMultiplier ?? current.PremiumRegenMultiplier,
            Modes: req.Modes ?? current.Modes,
            Safeguards: req.Safeguards ?? current.Safeguards,
            UpdatedAtUtc: DateTimeOffset.UtcNow
        );

        var entity = await db.GameBalanceConfigs.FirstOrDefaultAsync(x => x.Id == "default", ct);
        if (entity is null)
            db.GameBalanceConfigs.Add(new GameBalanceConfig(JsonSerializer.Serialize(next, JsonOptions)));
        else
            entity.Update(JsonSerializer.Serialize(next, JsonOptions));

        await db.SaveChangesAsync(ct);
        return next;
    }

    public async Task<(int SessionNumber, int Discount)> StartSessionAsync(Guid playerId, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        var state = await GetOrCreateStateAsync(playerId, ct);
        var sessionNumber = state.StartSession();
        await db.SaveChangesAsync(ct);

        var discount = sessionNumber <= cfg.Safeguards.FirstSessionsReducedCostCount
            ? cfg.Safeguards.FirstSessionsEnergyDiscount
            : 0;
        return (sessionNumber, discount);
    }

    public async Task<(bool Granted, int RemainingToday)> ClaimDailyTicketAsync(Guid playerId, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        var state = await GetOrCreateStateAsync(playerId, ct);
        var result = state.TryClaimDailyTicket(cfg.Safeguards.DailyFreeJackpotTickets);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<int> ReportLossAsync(Guid playerId, CancellationToken ct)
    {
        var state = await GetOrCreateStateAsync(playerId, ct);
        var streak = state.ReportLoss();
        await db.SaveChangesAsync(ct);
        return streak;
    }

    public async Task ResetLossAsync(Guid playerId, CancellationToken ct)
    {
        var state = await GetOrCreateStateAsync(playerId, ct);
        state.ResetLossStreak();
        await db.SaveChangesAsync(ct);
    }

    public async Task<ModeEntryDecisionDto> TryEnterModeAsync(Guid playerId, string mode, CancellationToken ct)
    {
        var cfg = await GetConfigAsync(ct);
        var state = await GetOrCreateStateAsync(playerId, ct);
        var normalizedMode = (mode ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedMode is "" or "solo")
            normalizedMode = "casual";
        var rule = cfg.Modes.FirstOrDefault(x => x.Mode.Equals(normalizedMode, StringComparison.OrdinalIgnoreCase));
        if (rule is null)
        {
            return new ModeEntryDecisionDto(
                Allowed: false,
                ReasonCode: "UNKNOWN_MODE",
                Message: $"Mode '{mode}' is not configured.",
                EnergyCostApplied: 0,
                TicketConsumed: false,
                CurrentEnergy: state.CurrentEnergy
            );
        }

        state.EnsureEnergyInitialized(cfg.StartEnergy);
        state.RegenerateEnergy(cfg.MaxEnergy, cfg.RegenMinutesPerEnergy);

        var sessionDiscount = state.SessionsStarted < cfg.Safeguards.FirstSessionsReducedCostCount
            ? cfg.Safeguards.FirstSessionsEnergyDiscount
            : 0;
        var energyCost = Math.Max(0, rule.EnergyCost - sessionDiscount);

        var ticketConsumed = false;
        if (rule.RequiresTicket)
        {
            if (!state.TryConsumeTicket(cfg.Safeguards.DailyFreeJackpotTickets))
            {
                return new ModeEntryDecisionDto(
                    Allowed: false,
                    ReasonCode: "NO_TICKET",
                    Message: "No ticket available for this mode.",
                    EnergyCostApplied: energyCost,
                    TicketConsumed: false,
                    CurrentEnergy: state.CurrentEnergy
                );
            }
            ticketConsumed = true;
        }

        if (!state.TryConsumeEnergy(energyCost))
        {
            return new ModeEntryDecisionDto(
                Allowed: false,
                ReasonCode: "INSUFFICIENT_ENERGY",
                Message: "Not enough energy to enter this mode.",
                EnergyCostApplied: energyCost,
                TicketConsumed: false,
                CurrentEnergy: state.CurrentEnergy
            );
        }

        await db.SaveChangesAsync(ct);
        return new ModeEntryDecisionDto(
            Allowed: true,
            ReasonCode: "OK",
            Message: "Entry allowed.",
            EnergyCostApplied: energyCost,
            TicketConsumed: ticketConsumed,
            CurrentEnergy: state.CurrentEnergy
        );
    }

    private async Task<PlayerEconomySafeguardState> GetOrCreateStateAsync(Guid playerId, CancellationToken ct)
    {
        var state = await db.PlayerEconomySafeguardStates.FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
        if (state is not null) return state;

        var created = new PlayerEconomySafeguardState(playerId);
        db.PlayerEconomySafeguardStates.Add(created);
        await db.SaveChangesAsync(ct);
        return created;
    }
}
