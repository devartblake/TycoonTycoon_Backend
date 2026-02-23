using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Users;

public sealed record AdminListUsers(AdminUsersListRequest Request) : IRequest<AdminUsersListResponse>;
public sealed record AdminGetUser(string UserId) : IRequest<AdminUserDetailDto?>;
public sealed record AdminCreateUser(AdminCreateUserRequest Request) : IRequest<AdminCreateUserResponse>;
public sealed record AdminUpdateUser(string UserId, AdminUpdateUserRequest Request) : IRequest<AdminUpdateUserResponse?>;
public sealed record AdminBanUser(string UserId, AdminBanUserRequest Request) : IRequest<AdminBanUserResponse?>;
public sealed record AdminUnbanUser(string UserId) : IRequest<AdminUnbanUserResponse?>;
public sealed record AdminDeleteUser(string UserId) : IRequest<bool>;
public sealed record AdminUserActivity(string UserId, DateTimeOffset? From, DateTimeOffset? To, string? Type, int Page, int PageSize) : IRequest<AdminUserActivityResponse?>;

public sealed class AdminListUsersHandler(IAppDb db) : IRequestHandler<AdminListUsers, AdminUsersListResponse>
{
    public async Task<AdminUsersListResponse> Handle(AdminListUsers r, CancellationToken ct)
    {
        var req = r.Request;
        var q = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Q))
        {
            var term = req.Q.Trim().ToLowerInvariant();
            q = q.Where(u => u.Email.ToLower().Contains(term)
                || u.Handle.ToLower().Contains(term)
                || u.Id.ToString().ToLower().Contains(term));
        }

        if (req.IsBanned.HasValue)
        {
            q = req.IsBanned.Value ? q.Where(u => !u.IsActive) : q.Where(u => u.IsActive);
        }

        q = ApplySort(q, req.SortBy, req.SortOrder);

        var totalItems = await q.CountAsync(ct);
        var pageSize = Math.Clamp(req.PageSize <= 0 ? 25 : req.PageSize, 1, 200);
        var page = req.Page <= 0 ? 1 : req.Page;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var users = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = users.Select(AdminUsersMapper.MapListItem).ToList();

        return new AdminUsersListResponse(items, page, pageSize, totalItems, totalPages);
    }

    private static IQueryable<User> ApplySort(IQueryable<User> query, string? sortBy, string? sortOrder)
    {
        var by = string.IsNullOrWhiteSpace(sortBy) ? "createdAt" : sortBy.Trim();
        var asc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        return by.ToLowerInvariant() switch
        {
            "email" => asc ? query.OrderBy(x => x.Email) : query.OrderByDescending(x => x.Email),
            "username" => asc ? query.OrderBy(x => x.Handle) : query.OrderByDescending(x => x.Handle),
            "lastactive" => asc ? query.OrderBy(x => x.LastLoginAt) : query.OrderByDescending(x => x.LastLoginAt),
            _ => asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt)
        };
    }
}

public sealed class AdminGetUserHandler(IAppDb db) : IRequestHandler<AdminGetUser, AdminUserDetailDto?>
{
    public async Task<AdminUserDetailDto?> Handle(AdminGetUser r, CancellationToken ct)
    {
        var id = AdminUsersMapper.ParseUserId(r.UserId);
        if (id is null) return null;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (user is null) return null;

        var mapped = AdminUsersMapper.MapListItem(user);
        return new AdminUserDetailDto(
            mapped.Id,
            mapped.Username,
            mapped.Email,
            mapped.Status,
            mapped.Role,
            mapped.AgeGroup,
            mapped.CreatedAt,
            mapped.LastActive,
            mapped.TotalGamesPlayed,
            mapped.TotalPoints,
            mapped.WinRate,
            mapped.IsVerified,
            mapped.IsBanned,
            new Dictionary<string, object>
            {
                ["country"] = user.Country ?? string.Empty,
                ["tier"] = user.Tier ?? "T1"
            }
        );
    }
}

public sealed class AdminCreateUserHandler(IAppDb db, ILogger<AdminCreateUserHandler> logger) : IRequestHandler<AdminCreateUser, AdminCreateUserResponse>
{
    public async Task<AdminCreateUserResponse> Handle(AdminCreateUser r, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email == r.Request.Email.ToLowerInvariant(), ct))
        {
            throw new InvalidOperationException("Email already exists.");
        }

        if (await db.Users.AnyAsync(x => x.Handle == r.Request.Username, ct))
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(r.Request.TemporaryPassword);
        var user = new User(r.Request.Email, r.Request.Username, passwordHash);

        if (string.Equals(r.Request.Role, "banned", StringComparison.OrdinalIgnoreCase))
        {
            user.SetActive(false);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin user created: UserId={UserId}, Email={Email}, Username={Username}", user.Id, user.Email, user.Handle);

        return new AdminCreateUserResponse(AdminUsersMapper.ToContractId(user.Id), user.CreatedAt);
    }
}

public sealed class AdminUpdateUserHandler(IAppDb db, ILogger<AdminUpdateUserHandler> logger) : IRequestHandler<AdminUpdateUser, AdminUpdateUserResponse?>
{
    public async Task<AdminUpdateUserResponse?> Handle(AdminUpdateUser r, CancellationToken ct)
    {
        var id = AdminUsersMapper.ParseUserId(r.UserId);
        if (id is null) return null;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (user is null) return null;

        user.UpdateProfile(r.Request.Username, user.Country);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin user updated: UserId={UserId}, Username={Username}", user.Id, user.Handle);
        return new AdminUpdateUserResponse(AdminUsersMapper.ToContractId(user.Id), DateTimeOffset.UtcNow);
    }
}

public sealed class AdminBanUserHandler(IAppDb db, ILogger<AdminBanUserHandler> logger) : IRequestHandler<AdminBanUser, AdminBanUserResponse?>
{
    public async Task<AdminBanUserResponse?> Handle(AdminBanUser r, CancellationToken ct)
    {
        var id = AdminUsersMapper.ParseUserId(r.UserId);
        if (id is null) return null;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (user is null) return null;

        user.SetActive(false);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin user banned: UserId={UserId}, Reason={Reason}, Until={Until}", user.Id, r.Request.Reason, r.Request.Until);

        return new AdminBanUserResponse(AdminUsersMapper.ToContractId(user.Id), true, r.Request.Until);
    }
}

public sealed class AdminUnbanUserHandler(IAppDb db, ILogger<AdminUnbanUserHandler> logger) : IRequestHandler<AdminUnbanUser, AdminUnbanUserResponse?>
{
    public async Task<AdminUnbanUserResponse?> Handle(AdminUnbanUser r, CancellationToken ct)
    {
        var id = AdminUsersMapper.ParseUserId(r.UserId);
        if (id is null) return null;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (user is null) return null;

        user.SetActive(true);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin user unbanned: UserId={UserId}", user.Id);

        return new AdminUnbanUserResponse(AdminUsersMapper.ToContractId(user.Id), false);
    }
}

public sealed class AdminDeleteUserHandler(IAppDb db, ILogger<AdminDeleteUserHandler> logger) : IRequestHandler<AdminDeleteUser, bool>
{
    public async Task<bool> Handle(AdminDeleteUser r, CancellationToken ct)
    {
        var id = AdminUsersMapper.ParseUserId(r.UserId);
        if (id is null) return false;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (user is null) return false;

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin user deleted: UserId={UserId}, Email={Email}", user.Id, user.Email);
        return true;
    }
}

public sealed class AdminUserActivityHandler(IAppDb db) : IRequestHandler<AdminUserActivity, AdminUserActivityResponse?>
{
    public async Task<AdminUserActivityResponse?> Handle(AdminUserActivity r, CancellationToken ct)
    {
        var id = AdminUsersMapper.ParseUserId(r.UserId);
        if (id is null) return null;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (user is null) return null;

        var items = new List<AdminUserActivityItemDto>
        {
            new($"evt_created_{user.Id:N}", "USER_CREATED", "User account created", user.CreatedAt, new Dictionary<string, object>()),
        };

        if (user.LastLoginAt.HasValue)
        {
            items.Add(new AdminUserActivityItemDto($"evt_login_{user.Id:N}", "LOGIN", "User signed in", user.LastLoginAt.Value, new Dictionary<string, object>()));
        }

        if (r.From.HasValue) items = items.Where(x => x.CreatedAt >= r.From.Value).ToList();
        if (r.To.HasValue) items = items.Where(x => x.CreatedAt <= r.To.Value).ToList();
        if (!string.IsNullOrWhiteSpace(r.Type)) items = items.Where(x => x.Type.Equals(r.Type, StringComparison.OrdinalIgnoreCase)).ToList();

        items = items.OrderByDescending(x => x.CreatedAt).ToList();

        var pageSize = Math.Clamp(r.PageSize <= 0 ? 50 : r.PageSize, 1, 200);
        var page = r.Page <= 0 ? 1 : r.Page;
        var totalItems = items.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new AdminUserActivityResponse(paged, page, pageSize, totalItems, totalPages);
    }
}

internal static class AdminUsersMapper
{
    internal static AdminUserListItemDto MapListItem(User user)
    {
        var status = (user.LastLoginAt.HasValue && user.LastLoginAt.Value >= DateTimeOffset.UtcNow.AddMinutes(-10)) ? "online" : "offline";

        return new AdminUserListItemDto(
            Id: ToContractId(user.Id),
            Username: user.Handle,
            Email: user.Email,
            Status: status,
            Role: "user",
            AgeGroup: "adult",
            CreatedAt: user.CreatedAt,
            LastActive: user.LastLoginAt,
            TotalGamesPlayed: 0,
            TotalPoints: 0,
            WinRate: 0,
            IsVerified: true,
            IsBanned: !user.IsActive
        );
    }

    internal static Guid? ParseUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var raw = userId.StartsWith("usr_", StringComparison.OrdinalIgnoreCase)
            ? userId[4..]
            : userId;

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    internal static string ToContractId(Guid id) => $"usr_{id:N}";
}
