using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Config;

namespace Tycoon.Backend.Api.Features.Mobile.Economy;

public static class MobileEconomyEndpoints
{
    public static void Map(RouteGroupBuilder mobile)
    {
        var g = mobile.MapGroup("/economy").WithTags("Mobile/Economy");

        g.MapGet("/state", async (IGameBalancePolicyService policy, CancellationToken ct) =>
        {
            var config = await policy.GetConfigAsync(ct);

            return Results.Ok(new
            {
                energy = new
                {
                    current = config.StartEnergy,
                    max = config.MaxEnergy,
                    regenMinutesPerEnergy = config.RegenMinutesPerEnergy,
                    dailyFreeEnergy = config.DailyFreeEnergy
                },
                modes = config.Modes,
                safeguards = config.Safeguards
            });
        });

        g.MapPost("/session/start", async ([FromQuery] Guid playerId, IGameBalancePolicyService policy, CancellationToken ct) =>
        {
            if (playerId == Guid.Empty)
                return Results.BadRequest(new { error = "playerId is required" });

            var config = await policy.GetConfigAsync(ct);
            var start = await policy.StartSessionAsync(playerId, ct);

            var adjustedCosts = config.Modes.Select(mode => new
            {
                mode = mode.Mode,
                baseCost = mode.EnergyCost,
                adjustedCost = Math.Max(0, mode.EnergyCost - start.Discount)
            });

            return Results.Ok(new
            {
                playerId,
                sessionNumber = start.SessionNumber,
                earlySessionDiscountApplied = start.Discount > 0,
                energyDiscount = start.Discount,
                adjustedCosts
            });
        });

        g.MapPost("/daily-jackpot-ticket/claim", async ([FromQuery] Guid playerId, IGameBalancePolicyService policy, CancellationToken ct) =>
        {
            if (playerId == Guid.Empty)
                return Results.BadRequest(new { error = "playerId is required" });

            var claim = await policy.ClaimDailyTicketAsync(playerId, ct);

            return Results.Ok(new
            {
                playerId,
                granted = claim.Granted,
                remainingToday = claim.RemainingToday
            });
        });

        g.MapPost("/revive/quote", async ([FromQuery] Guid playerId, [FromQuery] bool almostWin, IGameBalancePolicyService policy, CancellationToken ct) =>
        {
            if (playerId == Guid.Empty)
                return Results.BadRequest(new { error = "playerId is required" });

            var safeguards = (await policy.GetConfigAsync(ct)).Safeguards;
            var discountPercent = almostWin ? safeguards.AlmostWinReviveDiscountPercent : 0;
            var discountedCost = (int)Math.Ceiling(safeguards.ReviveBaseGemCost * (1 - (discountPercent / 100.0)));

            return Results.Ok(new
            {
                playerId,
                baseGemCost = safeguards.ReviveBaseGemCost,
                almostWin,
                discountPercent,
                finalGemCost = Math.Max(0, discountedCost)
            });
        });

        g.MapPost("/pity/report-loss", async ([FromQuery] Guid playerId, IGameBalancePolicyService policy, CancellationToken ct) =>
        {
            if (playerId == Guid.Empty)
                return Results.BadRequest(new { error = "playerId is required" });

            var safeguards = (await policy.GetConfigAsync(ct)).Safeguards;
            var streak = await policy.ReportLossAsync(playerId, ct);
            var pityActive = streak >= safeguards.PityLossThreshold;

            return Results.Ok(new
            {
                playerId,
                lossStreak = streak,
                pityActive,
                difficultyReductionPercent = pityActive ? safeguards.PityDifficultyReductionPercent : 0m
            });
        });

        g.MapPost("/pity/report-win", async ([FromQuery] Guid playerId, IGameBalancePolicyService policy, CancellationToken ct) =>
        {
            if (playerId == Guid.Empty)
                return Results.BadRequest(new { error = "playerId is required" });

            await policy.ResetLossAsync(playerId, ct);
            return Results.Ok(new { playerId, lossStreak = 0, pityActive = false });
        });
    }
}
