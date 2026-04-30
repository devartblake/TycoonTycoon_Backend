using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.PlayerTransactions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminPlayerTransactions
{
    public static class AdminPlayerTransactionEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/player-transactions").WithTags("Admin/PlayerTransactions");

            // Create / execute a composite player transaction
            g.MapPost("", async ([FromBody] CreatePlayerTransactionRequest req, PlayerTransactionService svc, CancellationToken ct) =>
            {
                if (req.EventId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "EventId is required.");

                if (string.IsNullOrWhiteSpace(req.Kind))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Kind is required.");

                var res = await svc.ExecuteAsync(req, ct);
                return Results.Ok(res);
            });

            // Get paginated history (optionally filtered by playerId or correlatedEventId)
            g.MapGet("/history", async (
                [FromQuery] Guid? playerId,
                [FromQuery] Guid? correlatedEventId,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                PlayerTransactionService svc,
                CancellationToken ct) =>
            {
                var res = await svc.GetHistoryAsync(
                    playerId, correlatedEventId,
                    page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize, ct);
                return Results.Ok(res);
            });

            // Get full detail of a single transaction
            g.MapGet("/{id:guid}", async (
                [FromRoute] Guid id,
                PlayerTransactionService svc,
                CancellationToken ct) =>
            {
                var detail = await svc.GetDetailAsync(id, ct);
                return detail is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Player transaction not found.")
                    : Results.Ok(detail);
            });

            // Dispute a transaction
            g.MapPost("/dispute", async (
                [FromBody] DisputePlayerTransactionRequest req,
                PlayerTransactionService svc,
                CancellationToken ct) =>
            {
                if (req.PlayerTransactionId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "PlayerTransactionId is required.");

                if (string.IsNullOrWhiteSpace(req.Reason))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Reason is required.");

                try
                {
                    var res = await svc.DisputeAsync(req, ct);
                    return Results.Ok(res);
                }
                catch (InvalidOperationException ex)
                {
                    var msg = ex.Message ?? "Dispute failed.";

                    if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", msg);

                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", msg);
                }
            });

            // Reverse a transaction (rolls back all child economy transactions)
            g.MapPost("/reverse", async (
                [FromBody] ReversePlayerTransactionRequest req,
                PlayerTransactionService svc,
                CancellationToken ct) =>
            {
                if (req.PlayerTransactionId == Guid.Empty)
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "PlayerTransactionId is required.");

                if (string.IsNullOrWhiteSpace(req.Reason))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Reason is required.");

                try
                {
                    var res = await svc.ReverseAsync(req, ct);
                    return Results.Ok(res);
                }
                catch (InvalidOperationException ex)
                {
                    var msg = ex.Message ?? "Reverse failed.";

                    if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", msg);

                    if (msg.Contains("already reversed", StringComparison.OrdinalIgnoreCase))
                        return AdminApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", msg);

                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", msg);
                }
            });
        }
    }
}
