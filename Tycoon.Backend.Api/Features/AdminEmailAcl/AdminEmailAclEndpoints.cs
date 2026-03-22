using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Api.Security;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminEmailAcl;

public static class AdminEmailAclEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/email-acl").WithTags("Admin/EmailAcl").WithOpenApi();

        // List all ACL entries (paginated, filterable by list type)
        g.MapGet("/", async (
            [FromQuery] string? listType,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb db,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var q = db.AdminEmailAcls.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(listType) &&
                Enum.TryParse<AdminAclListType>(listType, ignoreCase: true, out var parsed))
            {
                q = q.Where(e => e.ListType == parsed);
            }

            var totalItems = await q.CountAsync(ct);
            var items = await q.OrderBy(e => e.NormalizedEmail)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => ToDto(e))
                .ToListAsync(ct);
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            return Results.Ok(new AdminEmailAclListResponse(items, page, pageSize, totalItems, totalPages));
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);

        // Get a single entry by ID
        g.MapGet("/{id:guid}", async (Guid id, IAppDb db, CancellationToken ct) =>
        {
            var entry = await db.AdminEmailAcls.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            return entry is null
                ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ACL entry not found.")
                : Results.Ok(ToDto(entry));
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);

        // Create a new ACL entry
        g.MapPost("/", async (
            [FromBody] CreateAdminEmailAclRequest request,
            HttpContext httpContext,
            IAppDb db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "A valid email address is required.");

            if (!Enum.TryParse<AdminAclListType>(request.ListType, ignoreCase: true, out var listType))
                return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "ListType must be 'Allow' or 'Block'.");

            if (!Enum.TryParse<AdminRole>(request.Role, ignoreCase: true, out var role))
                return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "Role must be 'Viewer', 'Admin', or 'SuperAdmin'.");

            var normalized = request.Email.Trim().ToLowerInvariant();
            var existing = await db.AdminEmailAcls.FirstOrDefaultAsync(e => e.NormalizedEmail == normalized, ct);
            if (existing is not null)
                return AdminApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", "An ACL entry for this email already exists.");

            var actor = httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? "unknown";

            var entry = new Domain.Entities.AdminEmailAcl(request.Email, listType, role, actor, request.Notes);
            db.AdminEmailAcls.Add(entry);

            await AdminSecurityAudit.WriteAsync(db, "admin_email_acl_create", "success", new
            {
                email = normalized,
                listType = listType.ToString(),
                role = role.ToString(),
                actor
            }, ct);

            return Results.Created($"/admin/email-acl/{entry.Id}", ToDto(entry));
        })
        .RequireAuthorization(AdminPolicies.SuperAdminPolicy);

        // Update an existing ACL entry
        g.MapPatch("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateAdminEmailAclRequest request,
            HttpContext httpContext,
            IAppDb db,
            CancellationToken ct) =>
        {
            var entry = await db.AdminEmailAcls.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (entry is null)
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ACL entry not found.");

            if (!Enum.TryParse<AdminAclListType>(request.ListType, ignoreCase: true, out var listType))
                return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "ListType must be 'Allow' or 'Block'.");

            if (!Enum.TryParse<AdminRole>(request.Role, ignoreCase: true, out var role))
                return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "Role must be 'Viewer', 'Admin', or 'SuperAdmin'.");

            var actor = httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? "unknown";

            entry.Update(listType, role, request.Notes);

            await AdminSecurityAudit.WriteAsync(db, "admin_email_acl_update", "success", new
            {
                entryId = entry.Id,
                email = entry.NormalizedEmail,
                listType = listType.ToString(),
                role = role.ToString(),
                actor
            }, ct);

            return Results.Ok(ToDto(entry));
        })
        .RequireAuthorization(AdminPolicies.SuperAdminPolicy);

        // Delete an ACL entry
        g.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IAppDb db,
            CancellationToken ct) =>
        {
            var entry = await db.AdminEmailAcls.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (entry is null)
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ACL entry not found.");

            var actor = httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? "unknown";

            db.AdminEmailAcls.Remove(entry);

            await AdminSecurityAudit.WriteAsync(db, "admin_email_acl_delete", "success", new
            {
                entryId = entry.Id,
                email = entry.NormalizedEmail,
                actor
            }, ct);

            return Results.NoContent();
        })
        .RequireAuthorization(AdminPolicies.SuperAdminPolicy);
    }

    private static AdminEmailAclEntryDto ToDto(Domain.Entities.AdminEmailAcl e)
        => new(e.Id, e.Email, e.ListType.ToString(), e.Role.ToString(), e.Notes, e.AddedBy, e.CreatedAtUtc, e.UpdatedAtUtc);
}
