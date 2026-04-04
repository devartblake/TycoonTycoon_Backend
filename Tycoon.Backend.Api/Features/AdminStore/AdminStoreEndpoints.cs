using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Api.Features.AdminStore;

public static class AdminStoreEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/store").WithTags("Admin/Store").WithOpenApi();

        g.MapGet("/catalog", ListCatalog);
        g.MapPost("/catalog", CreateItem);
        g.MapPatch("/catalog/{id:guid}", UpdateItem);
        g.MapDelete("/catalog/{id:guid}", DeleteItem);
    }

    private static async Task<IResult> ListCatalog(
        [FromQuery] bool? activeOnly,
        [FromQuery] string? itemType,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IAppDb db,
        CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 200);

        var q = db.StoreItems.AsNoTracking();

        if (activeOnly == true)
            q = q.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(itemType))
            q = q.Where(i => i.ItemType == itemType);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new AdminStoreItemDto(
                i.Id, i.Sku, i.Name, i.Description, i.ItemType,
                i.PriceCoins, i.PriceDiamonds, i.GrantQuantity,
                i.MaxPerPlayer, i.IsActive, i.MediaKey, i.SortOrder,
                i.CreatedAtUtc, i.UpdatedAtUtc))
            .ToListAsync(ct);

        return Results.Ok(AdminApiResponses.Page(items, page, pageSize, total));
    }

    private static async Task<IResult> CreateItem(
        [FromBody] AdminCreateStoreItemRequest req,
        IAppDb db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Sku) || string.IsNullOrWhiteSpace(req.Name))
            return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "sku and name are required.");

        var sku = req.Sku.Trim().ToLowerInvariant();

        var exists = await db.StoreItems.AnyAsync(i => i.Sku == sku, ct);
        if (exists)
            return AdminApiResponses.Error(StatusCodes.Status409Conflict, "SKU_CONFLICT", $"A store item with SKU '{sku}' already exists.");

        if (req.PriceCoins < 0 || req.PriceDiamonds < 0)
            return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Prices cannot be negative.");

        if (req.GrantQuantity <= 0)
            return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "grantQuantity must be at least 1.");

        var item = new StoreItem
        {
            Sku = sku,
            Name = req.Name.Trim(),
            Description = req.Description?.Trim() ?? string.Empty,
            ItemType = req.ItemType?.Trim().ToLowerInvariant() ?? "misc",
            PriceCoins = req.PriceCoins,
            PriceDiamonds = req.PriceDiamonds,
            GrantQuantity = req.GrantQuantity,
            MaxPerPlayer = Math.Max(0, req.MaxPerPlayer),
            MediaKey = req.MediaKey?.Trim(),
            SortOrder = req.SortOrder,
            IsActive = true,
        };

        db.StoreItems.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/admin/store/catalog/{item.Id}", new { id = item.Id, sku = item.Sku });
    }

    private static async Task<IResult> UpdateItem(
        [FromRoute] Guid id,
        [FromBody] AdminUpdateStoreItemRequest req,
        IAppDb db,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "id is required.");

        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
            return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Store item not found.");

        if (req.Name is not null) item.Name = req.Name.Trim();
        if (req.Description is not null) item.Description = req.Description.Trim();
        if (req.PriceCoins is { } coins)
        {
            if (coins < 0)
                return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "priceCoins cannot be negative.");
            item.PriceCoins = coins;
        }
        if (req.PriceDiamonds is { } diamonds)
        {
            if (diamonds < 0)
                return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "priceDiamonds cannot be negative.");
            item.PriceDiamonds = diamonds;
        }
        if (req.IsActive is { } active) item.IsActive = active;
        if (req.MediaKey is not null) item.MediaKey = req.MediaKey.Trim();
        if (req.SortOrder is { } order) item.SortOrder = order;

        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { id = item.Id, updatedAt = item.UpdatedAtUtc });
    }

    private static async Task<IResult> DeleteItem(
        [FromRoute] Guid id,
        IAppDb db,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "id is required.");

        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
            return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Store item not found.");

        if (!item.IsActive)
            return AdminApiResponses.Error(StatusCodes.Status409Conflict, "ALREADY_INACTIVE", "Store item is already inactive.");

        item.IsActive = false;
        item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    public sealed record AdminStoreItemDto(
        Guid Id,
        string Sku,
        string Name,
        string Description,
        string ItemType,
        int PriceCoins,
        int PriceDiamonds,
        int GrantQuantity,
        int MaxPerPlayer,
        bool IsActive,
        string? MediaKey,
        int SortOrder,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record AdminCreateStoreItemRequest(
        string Sku,
        string Name,
        string? Description,
        string? ItemType,
        int PriceCoins,
        int PriceDiamonds,
        int GrantQuantity,
        int MaxPerPlayer,
        string? MediaKey,
        int SortOrder);

    public sealed record AdminUpdateStoreItemRequest(
        string? Name,
        string? Description,
        int? PriceCoins,
        int? PriceDiamonds,
        bool? IsActive,
        string? MediaKey,
        int? SortOrder);
}
