using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Config;

public sealed class BalanceValidationException(IReadOnlyList<string> errors)
    : InvalidOperationException(errors.Count == 0 ? "Invalid balance configuration." : string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

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
        Validate(next);

        var entity = await db.GameBalanceConfigs.FirstOrDefaultAsync(x => x.Id == "default", ct);
        if (entity is null)
            db.GameBalanceConfigs.Add(new GameBalanceConfig(JsonSerializer.Serialize(next, JsonOptions)));
        else
            entity.Update(JsonSerializer.Serialize(next, JsonOptions));

        await db.SaveChangesAsync(ct);
        return next;
    }

    private static void Validate(GameBalanceConfigDto cfg)
    {
        var errors = new List<string>();
        if (cfg.MaxEnergy <= 0)
            errors.Add("MaxEnergy must be greater than zero.");
        if (cfg.StartEnergy < 0)
            errors.Add("StartEnergy cannot be negative.");
        if (cfg.StartEnergy > cfg.MaxEnergy)
            errors.Add("StartEnergy cannot exceed MaxEnergy.");
        if (cfg.RegenMinutesPerEnergy <= 0)
            errors.Add("RegenMinutesPerEnergy must be greater than zero.");
        if (cfg.AdEnergyMin < 0 || cfg.AdEnergyMax < 0 || cfg.AdEnergyMin > cfg.AdEnergyMax)
            errors.Add("Ad energy range is invalid.");
        if (cfg.Modes is null || cfg.Modes.Count == 0)
            errors.Add("At least one mode rule is required.");
        if (cfg.Modes.Any(m => string.IsNullOrWhiteSpace(m.Mode)))
            errors.Add("Mode names cannot be empty.");
        if (cfg.Modes.Any(m => m.EnergyCost < 0))
            errors.Add("Mode energyCost cannot be negative.");
        if (cfg.Safeguards.FirstSessionsReducedCostCount < 0 || cfg.Safeguards.FirstSessionsEnergyDiscount < 0)
            errors.Add("First-session safeguard values cannot be negative.");
        if (cfg.Safeguards.DailyFreeJackpotTickets < 0)
            errors.Add("DailyFreeJackpotTickets cannot be negative.");
        if (cfg.Safeguards.ReviveBaseGemCost < 0)
            errors.Add("ReviveBaseGemCost cannot be negative.");
        if (cfg.Safeguards.AlmostWinReviveDiscountPercent is < 0 or > 100)
            errors.Add("AlmostWinReviveDiscountPercent must be between 0 and 100.");
        if (cfg.Safeguards.PityLossThreshold < 0)
            errors.Add("PityLossThreshold cannot be negative.");
        if (cfg.Safeguards.PityDifficultyReductionPercent is < 0 or > 1)
            errors.Add("PityDifficultyReductionPercent must be between 0 and 1.");

        if (errors.Count > 0)
            throw new BalanceValidationException(errors);
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
        var rule = cfg.Modes.FirstOrDefault(x => x.Mode.Equals(normalizedMode, StringComparison.OrdinalIgnoreCase))
                   ?? new ModeBalanceRuleDto(normalizedMode, 0, null, false, 0);

        state.EnsureEnergyInitialized(cfg.StartEnergy);
        state.RegenerateEnergy(cfg.MaxEnergy, cfg.RegenMinutesPerEnergy);

        var sessionDiscount = state.SessionsStarted <= cfg.Safeguards.FirstSessionsReducedCostCount
            ? cfg.Safeguards.FirstSessionsEnergyDiscount
            : 0;
        var energyCost = Math.Max(0, rule.EnergyCost - sessionDiscount);

        if (rule.RequiresTicket && !state.HasTicketAvailable(cfg.Safeguards.DailyFreeJackpotTickets))
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

        if (state.CurrentEnergy < energyCost)
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

        var ticketConsumed = false;
        if (rule.RequiresTicket)
        {
            _ = state.TryConsumeTicket(cfg.Safeguards.DailyFreeJackpotTickets);
            ticketConsumed = true;
        }
        _ = state.TryConsumeEnergy(energyCost);

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
