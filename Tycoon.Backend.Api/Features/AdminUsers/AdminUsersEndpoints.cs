using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Users;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminUsers;

public static class AdminUsersEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/users").WithTags("Admin/Users").WithOpenApi();

        g.MapGet("", ListUsers);
        g.MapGet("/{userId}", GetUser);
        g.MapPost("", CreateUser);
        g.MapPatch("/{userId}", UpdateUser);
        g.MapPost("/{userId}/ban", BanUser);
        g.MapPost("/{userId}/unban", UnbanUser);
        g.MapDelete("/{userId}", DeleteUser);
        g.MapGet("/{userId}/activity", UserActivity);
    }

    private static async Task<IResult> ListUsers(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] string? role,
        [FromQuery] string? ageGroup,
        [FromQuery] bool? isVerified,
        [FromQuery] bool? isBanned,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new AdminListUsers(new AdminUsersListRequest(
            Q: q,
            Status: status,
            Role: role,
            AgeGroup: ageGroup,
            IsVerified: isVerified,
            IsBanned: isBanned,
            Page: page,
            PageSize: pageSize,
            SortBy: sortBy,
            SortOrder: sortOrder
        )), ct);

        return Results.Ok(dto);
    }

    private static async Task<IResult> GetUser(string userId, IMediator mediator, CancellationToken ct)
    {
        var dto = await mediator.Send(new AdminGetUser(userId), ct);
        return dto is null ? NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> CreateUser([FromBody] AdminCreateUserRequest request, IMediator mediator, CancellationToken ct)
    {
        try
        {
            var dto = await mediator.Send(new AdminCreateUser(request), ct);
            return Results.Created($"/admin/users/{dto.Id}", dto);
        }
        catch (InvalidOperationException ex)
        {
            return Validation(ex.Message);
        }
    }

    private static async Task<IResult> UpdateUser(string userId, [FromBody] AdminUpdateUserRequest request, IMediator mediator, CancellationToken ct)
    {
        var dto = await mediator.Send(new AdminUpdateUser(userId, request), ct);
        return dto is null ? NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> BanUser(string userId, [FromBody] AdminBanUserRequest request, IMediator mediator, CancellationToken ct)
    {
        var dto = await mediator.Send(new AdminBanUser(userId, request), ct);
        return dto is null ? NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> UnbanUser(string userId, IMediator mediator, CancellationToken ct)
    {
        var dto = await mediator.Send(new AdminUnbanUser(userId), ct);
        return dto is null ? NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> DeleteUser(string userId, IMediator mediator, CancellationToken ct)
    {
        var ok = await mediator.Send(new AdminDeleteUser(userId), ct);
        return ok ? Results.NoContent() : NotFound();
    }

    private static async Task<IResult> UserActivity(
        string userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? type,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new AdminUserActivity(userId, from, to, type, page, pageSize), ct);
        return dto is null ? NotFound() : Results.Ok(dto);
    }

    private static IResult NotFound() => AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.");

    private static IResult Validation(string message) => AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", message);
}
