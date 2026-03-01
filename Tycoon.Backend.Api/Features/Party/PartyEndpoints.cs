using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Social;

namespace Tycoon.Backend.Api.Features.Party
{
    public static class PartyEndpoints
    {
        public sealed record CreatePartyBody(Guid LeaderPlayerId);
        public sealed record InviteBody(Guid FromPlayerId, Guid ToPlayerId);
        public sealed record RespondInviteBody(Guid PlayerId);
        public sealed record LeavePartyBody(Guid PlayerId);
        public sealed record PartyEnqueueBody(Guid LeaderPlayerId, string Mode, int Tier);
        public sealed record PartyCancelQueueBody(Guid LeaderPlayerId);


        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/party")
                .WithTags("Party").WithOpenApi();

            // POST /party
            g.MapPost("", async (
                [FromBody] CreatePartyBody body,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    var roster = await party.CreatePartyAsync(body.LeaderPlayerId, ct);
                    return Results.Ok(roster);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // GET /party/{partyId}
            g.MapGet("/{partyId:guid}", async (
                Guid partyId,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    var roster = await party.GetRosterAsync(partyId, ct);
                    return roster is null ? Results.NotFound() : Results.Ok(roster);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
            });

            // POST /party/{partyId}/invite
            g.MapPost("/{partyId:guid}/invite", async (
                Guid partyId,
                [FromBody] InviteBody body,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    var invite = await party.InviteAsync(partyId, body.FromPlayerId, body.ToPlayerId, ct);
                    return Results.Ok(invite);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // POST /party/invites/{inviteId}/accept
            g.MapPost("/invites/{inviteId:guid}/accept", async (
                Guid inviteId,
                [FromBody] RespondInviteBody body,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    var invite = await party.AcceptInviteAsync(inviteId, body.PlayerId, ct);
                    return invite is null ? Results.NotFound() : Results.Ok(invite);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // POST /party/invites/{inviteId}/decline
            g.MapPost("/invites/{inviteId:guid}/decline", async (
                Guid inviteId,
                [FromBody] RespondInviteBody body,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    var invite = await party.DeclineInviteAsync(inviteId, body.PlayerId, ct);
                    return invite is null ? Results.NotFound() : Results.Ok(invite);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // POST /party/{partyId}/leave
            g.MapPost("/{partyId:guid}/leave", async (
                Guid partyId,
                [FromBody] LeavePartyBody body,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    await party.LeavePartyAsync(partyId, body.PlayerId, ct);
                    return Results.NoContent();
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // GET /party/invites?playerId=...&box=incoming|outgoing|all&page=1&pageSize=50
            g.MapGet("/invites", async (
                [FromQuery] Guid playerId,
                [FromQuery] string? box,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                PartyService party,
                CancellationToken ct) =>
            {
                try
                {
                    var res = await party.ListInvitesAsync(playerId, box ?? "incoming", page, pageSize, ct);
                    return Results.Ok(res);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
            });

            // POST /party/{partyId}/enqueue
            g.MapPost("/{partyId:guid}/enqueue", async (
                Guid partyId,
                [FromBody] PartyEnqueueBody body,
                Tycoon.Backend.Application.Social.PartyMatchmakingService mm,
                CancellationToken ct) =>
            {
                try
                {
                    var res = await mm.EnqueuePartyAsync(partyId, body.LeaderPlayerId, body.Mode, body.Tier, ct);

                    if (res.Status == "Forbidden")
                        return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Party is not allowed to enter matchmaking.");

                    return res.Status == "Queued"
                        ? Results.Accepted(value: res)
                        : Results.Ok(res);
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

            // POST /party/{partyId}/queue/cancel
            g.MapPost("/{partyId:guid}/queue/cancel", async (
                Guid partyId,
                [FromBody] PartyCancelQueueBody body,
                Tycoon.Backend.Application.Social.PartyMatchmakingService mm,
                CancellationToken ct) =>
            {
                try
                {
                    await mm.CancelPartyQueueAsync(partyId, body.LeaderPlayerId, ct);
                    return Results.NoContent();
                }
                catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
                catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
            });

        }
    }
}
