using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Events;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminEventQueue;

public static class AdminEventQueueEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/event-queue").WithTags("Admin/EventQueue").WithOpenApi();

        g.MapPost("/upload", Upload);
        g.MapPost("/reprocess", Reprocess);
    }

    private static async Task<IResult> Upload(
        [FromBody] AdminEventQueueUploadRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        if (request.Events is null || request.Events.Count == 0)
        {
            return Validation("At least one event is required.");
        }

        var adminUser = httpContext.Request.Headers["X-Admin-User"].FirstOrDefault();
        var dto = await mediator.Send(new AdminUploadEventQueue(request, adminUser), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> Reprocess(
        [FromBody] AdminEventQueueReprocessRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        if (request.Limit <= 0)
        {
            return Validation("limit must be greater than zero.");
        }

        var adminUser = httpContext.Request.Headers["X-Admin-User"].FirstOrDefault();
        var dto = await mediator.Send(new AdminReprocessEventQueue(request, adminUser), ct);
        return Results.Accepted(value: dto);
    }

    private static IResult Validation(string message) => AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", message);
}
