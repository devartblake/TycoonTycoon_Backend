using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Referrals;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Referrals
{
    public static class ReferralsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/referrals").WithTags("Referrals");

            g.MapPost("/", async ([FromBody] CreateReferralCodeRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new CreateReferralCode(req), ct);
                return Results.Ok(dto);
            });

            g.MapGet("/{code}", async ([FromRoute] string code, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetReferralCode(code), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            g.MapPost("/{code}/redeem", async (
                [FromRoute] string code,
                [FromBody] RedeemReferralRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new RedeemReferralCode(code, req), ct);

                return dto.Status switch
                {
                    "Invalid" => Results.NotFound(dto),
                    "SelfRedeemNotAllowed" => Results.BadRequest(dto),
                    _ => Results.Ok(dto)
                };
            });
        }
    }
}
