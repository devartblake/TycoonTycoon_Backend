using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Synaptix.Backend.Application.Rewards;

namespace Synaptix.Backend.Api.Features.Events;

public static class ActiveEventsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/events/active", GetActiveEvents)
            .WithTags("Events")
            .WithName("GetActiveEvents");
    }

    private static IResult GetActiveEvents(RewardReactorRuntimeContextService runtime)
    {
        var active = runtime.GetActiveEvents(DateTimeOffset.UtcNow)
            .Select(e => new ActiveEventDto(
                e.EventId,
                e.DisplayName,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.EventMultiplier))
            .ToList();

        return Results.Ok(new ActiveEventsResponse(active));
    }
}

public sealed record ActiveEventsResponse(IReadOnlyList<ActiveEventDto> Events);

public sealed record ActiveEventDto(
    string EventId,
    string DisplayName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    double? EventMultiplier
);
