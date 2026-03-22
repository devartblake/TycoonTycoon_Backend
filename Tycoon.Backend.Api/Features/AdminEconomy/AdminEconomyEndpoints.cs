using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Application.Economy;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminEconomy
{
    public static class AdminEconomyEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/economy").WithTags("Admin/Economy").WithOpenApi();

            g.MapPost("/transactions", async ([FromBody] CreateEconomyTxnRequest req, EconomyService econ, CancellationToken ct) =>
            {
                var res = await econ.ApplyAsync(req, ct);
                return Results.Ok(res);
            });

            g.MapGet("/history/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                EconomyService econ,
                CancellationToken ct) =>
            {
                var res = await econ.GetHistoryAsync(playerId, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct);
                return Results.Ok(res);
            });

            g.MapGet("/balance", async (IGameBalancePolicyService policy, CancellationToken ct) =>
            {
                var cfg = await policy.GetConfigAsync(ct);
                return Results.Ok(cfg);
            });

            g.MapPatch("/balance", async ([FromBody] UpdateGameBalanceConfigRequest req, IGameBalancePolicyService policy, CancellationToken ct) =>
            {
                try
                {
                    var updated = await policy.UpdateConfigAsync(req, ct);
                    return Results.Ok(updated);
                }
                catch (BalanceValidationException ex)
                {
                    return AdminApiResponses.Error(
                        StatusCodes.Status400BadRequest,
                        "VALIDATION_ERROR",
                        ex.Message,
                        new { errors = ex.Errors });
                }
                catch (InvalidOperationException ex)
                {
                    return AdminApiResponses.Error(
                        StatusCodes.Status400BadRequest,
                        "VALIDATION_ERROR",
                        ex.Message,
                        new { errors = new[] { ex.Message } });
                }
            });

            g.MapPost("/simulate", async ([FromBody] EconomySimulationRequest req, IGameBalancePolicyService policy, CancellationToken ct) =>
            {
                var cfg = await policy.GetConfigAsync(ct);
                var earlyDiscount = req.SessionNumber.HasValue
                    && req.SessionNumber.Value <= cfg.Safeguards.FirstSessionsReducedCostCount
                    ? cfg.Safeguards.FirstSessionsEnergyDiscount
                    : 0;

                var casualCost = Math.Max(0, cfg.Modes.First(m => m.Mode == "casual").EnergyCost - earlyDiscount);
                var rankedCost = Math.Max(0, cfg.Modes.First(m => m.Mode == "ranked").EnergyCost - earlyDiscount);
                var guardianCost = Math.Max(0, cfg.Modes.First(m => m.Mode == "guardian").EnergyCost - earlyDiscount);

                var spend = (req.CasualMatches ?? 0) * casualCost
                    + (req.RankedMatches ?? 0) * rankedCost
                    + (req.GuardianMatches ?? 0) * guardianCost;

                var regen = req.SessionMinutes <= 0 ? 0 : req.SessionMinutes / Math.Max(1, cfg.RegenMinutesPerEnergy);
                var endEnergy = Math.Clamp(cfg.StartEnergy - spend + regen, 0, cfg.MaxEnergy);

                return Results.Ok(new EconomySimulationResponse(
                    StartingEnergy: cfg.StartEnergy,
                    EnergySpent: spend,
                    EnergyRegenerated: regen,
                    EndingEnergy: endEnergy,
                    EstimatedMatchesByMode: (req.CasualMatches ?? 0) + (req.RankedMatches ?? 0) + (req.GuardianMatches ?? 0),
                    EstimatedSessionMinutes: req.SessionMinutes
                ));
            });

            g.MapPost("/rollback", async (
                [FromBody] AdminRollbackEconomyRequest req,
                EconomyService econ,
                CancellationToken ct) =>
            {
                if (req.EventId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "EventId is required.");

                if (string.IsNullOrWhiteSpace(req.Reason))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Reason is required.");

                try
                {
                    var res = await econ.RollbackByEventIdAsync(req.EventId, req.Reason.Trim(), ct);
                    return Results.Ok(res);
                }
                catch (InvalidOperationException ex)
                {
                    var msg = ex.Message ?? "Rollback failed.";

                    if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", msg);

                    if (msg.Contains("already rolled back", StringComparison.OrdinalIgnoreCase))
                        return AdminApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", msg);

                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", msg);
                }
            });
        }
    }
}
