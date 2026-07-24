using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Features.Payments;
using Synaptix.Backend.Api.Payments.PayPal;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Entitlements.Services;

namespace Synaptix.Backend.Api.Features.AdminPayments;

/// <summary>
/// Operator visibility, manual reconciliation, retry-fulfillment, and refund for PayPal/Stripe
/// checkout attempts. Nested under the shared /admin group in Program.cs, which already applies
/// RequireAdminOpsKey()/RequireAdminRoleClaims() — no additional auth attributes needed here.
/// </summary>
public static class AdminPaymentsEndpoints
{
    private const string AdminHeader = "X-Admin-User";

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/payments").WithTags("Admin/Payments");

        g.MapGet("", async (
            [FromQuery] string? provider,
            [FromQuery] string? status,
            [FromQuery] Guid? playerId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb db,
            CancellationToken ct) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);

            var query = db.PaymentCheckoutAttempts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(provider))
                query = query.Where(a => a.Provider == provider);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentCheckoutStatus>(status, true, out var statusValue))
                query = query.Where(a => a.Status == statusValue);

            if (playerId is not null)
                query = query.Where(a => a.PlayerId == playerId.Value);

            query = query.OrderByDescending(a => a.CreatedAtUtc);

            var total = await query.CountAsync(ct);
            var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return Results.Ok(new AdminPaymentListResponse(
                page, pageSize, total, rows.Select(ToListItemDto).ToList()));
        });

        g.MapGet("/{id:guid}", async (Guid id, IAppDb db, CancellationToken ct) =>
        {
            var attempt = await db.PaymentCheckoutAttempts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (attempt is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Payment checkout attempt not found.");

            var issues = await db.PaymentReconciliationIssues.AsNoTracking()
                .Where(i => i.PaymentCheckoutAttemptId == id)
                .OrderByDescending(i => i.CreatedAtUtc)
                .ToListAsync(ct);

            return Results.Ok(new AdminPaymentDetailDto(
                ToListItemDto(attempt),
                issues.Select(ToIssueDto).ToList()));
        });

        g.MapPost("/{id:guid}/reconcile", async (
            HttpContext ctx,
            Guid id,
            IAppDb db,
            PaymentReconciliationJob reconciliationJob,
            CancellationToken ct) =>
        {
            var attempt = await db.PaymentCheckoutAttempts.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (attempt is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Payment checkout attempt not found.");

            var issueRaised = await reconciliationJob.ReconcileAttemptAsync(attempt, ct);
            await db.SaveChangesAsync(ct);

            await AdminAuditLogger.WriteAsync(db, ctx, "payments.reconcile", "PaymentCheckoutAttempt", id.ToString(),
                before: null, after: new { attempt.Status, issueRaised }, ct);

            return Results.Ok(new AdminPaymentReconcileResponse(id, attempt.Status.ToString(), issueRaised));
        });

        g.MapPost("/{id:guid}/retry-fulfillment", async (
            HttpContext ctx,
            Guid id,
            IAppDb db,
            IEntitlementService entitlementService,
            CancellationToken ct) =>
        {
            var attempt = await db.PaymentCheckoutAttempts.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (attempt is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Payment checkout attempt not found.");

            var openIssue = await db.PaymentReconciliationIssues
                .Where(i => i.PaymentCheckoutAttemptId == id
                    && i.Category == PaymentReconciliationCategory.ProviderCapturedFulfillmentMissing
                    && i.ResolvedAtUtc == null)
                .OrderByDescending(i => i.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (openIssue is null)
                return ApiResponses.Error(StatusCodes.Status409Conflict, "NO_OPEN_ISSUE", "No open fulfillment-missing issue for this attempt.");

            var actor = ResolveActor(ctx);

            var existingTx = await db.PlayerTransactions.FirstOrDefaultAsync(t => t.Receipt == attempt.ProviderRef, ct);
            if (existingTx is not null)
            {
                attempt.MarkCaptured(existingTx.Id, attempt.ProviderCaptureRef);
                openIssue.Resolve(actor, "Fulfillment already existed on retry; issue closed without granting again.");
                await db.SaveChangesAsync(ct);

                return Results.Ok(new AdminPaymentRetryFulfillmentResponse(id, existingTx.Id, "AlreadyFulfilled"));
            }

            var storeItem = await db.StoreItems.FirstOrDefaultAsync(i => i.Sku == attempt.Sku, ct);
            if (storeItem is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", $"Store item '{attempt.Sku}' was not found.");

            var eventId = string.Equals(attempt.Provider, "paypal", StringComparison.OrdinalIgnoreCase)
                ? CreateDeterministicGuid($"paypal-order:{attempt.ProviderRef}")
                : CreateDeterministicGuid(attempt.ProviderRef);
            var kind = string.Equals(attempt.Provider, "paypal", StringComparison.OrdinalIgnoreCase)
                ? "paypal-order-payment"
                : "stripe-checkout-payment";

            var grantQuantity = storeItem.GrantQuantity * attempt.Quantity;

            var tx = new PlayerTransaction(eventId, kind, correlatedEventId: null, receipt: attempt.ProviderRef);
            tx.AddActor(attempt.PlayerId, PlayerTransactionActorRole.Buyer);
            tx.AddItemChange(storeItem.Sku, grantQuantity, ItemOperation.Grant);
            tx.MarkApplied();

            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync(ct);

            await entitlementService.GrantAsync(attempt.PlayerId, storeItem.Sku, storeItem.ItemType, grantQuantity, tx.Id, ct: ct);

            attempt.MarkCaptured(tx.Id, attempt.ProviderCaptureRef);
            openIssue.Resolve(actor, "Fulfilled via admin retry.");
            await db.SaveChangesAsync(ct);

            await AdminAuditLogger.WriteAsync(db, ctx, "payments.retry_fulfillment", "PaymentCheckoutAttempt", id.ToString(),
                before: new { status = "FulfillmentMissing" },
                after: new { status = "Captured", playerTransactionId = tx.Id }, ct);

            return Results.Ok(new AdminPaymentRetryFulfillmentResponse(id, tx.Id, "Fulfilled"));
        });

        g.MapPost("/{id:guid}/refund", async (
            HttpContext ctx,
            Guid id,
            [FromBody] AdminPaymentRefundRequest req,
            IAppDb db,
            IPayPalPaymentGateway payPalGateway,
            IStripePaymentGateway stripeGateway,
            IEntitlementService entitlementService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Reason is required.");

            var attempt = await db.PaymentCheckoutAttempts.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (attempt is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Payment checkout attempt not found.");

            if (attempt.Status != PaymentCheckoutStatus.Captured || attempt.PlayerTransactionId is null)
                return ApiResponses.Error(StatusCodes.Status409Conflict, "NOT_REFUNDABLE", $"Attempt status is '{attempt.Status}', not Captured.");

            var localTx = await db.PlayerTransactions
                .Include(t => t.ItemChanges)
                .FirstOrDefaultAsync(t => t.Id == attempt.PlayerTransactionId, ct);
            if (localTx is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Linked PlayerTransaction not found.");

            var isFullRefund = req.Amount is null || req.Amount.Value >= attempt.ExpectedAmount;

            string refundId;
            string refundStatus;

            if (string.Equals(attempt.Provider, "paypal", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(attempt.ProviderCaptureRef))
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "REFUND_UNAVAILABLE", "No PayPal capture id recorded for this attempt.");

                var refund = await payPalGateway.RefundCaptureAsync(attempt.ProviderCaptureRef, req.Amount, attempt.Currency, ct);
                refundId = refund.RefundId;
                refundStatus = refund.Status;
            }
            else if (string.Equals(attempt.Provider, "stripe", StringComparison.OrdinalIgnoreCase))
            {
                var session = await stripeGateway.GetCheckoutSessionAsync(attempt.ProviderRef, ct);
                if (string.IsNullOrWhiteSpace(session.PaymentIntentId))
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "REFUND_UNAVAILABLE", "No Stripe payment intent found for this session.");

                var amountCents = req.Amount.HasValue ? (long?)Math.Round(req.Amount.Value * 100m) : null;
                var refund = await stripeGateway.RefundPaymentIntentAsync(session.PaymentIntentId, amountCents, ct);
                refundId = refund.RefundId;
                refundStatus = refund.Status;
            }
            else
            {
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", $"Unsupported provider '{attempt.Provider}'.");
            }

            var refundTx = new PlayerTransaction(
                eventId: CreateDeterministicGuid($"{attempt.Provider}-refund:{attempt.Id}"),
                kind: $"{attempt.Provider}-refund",
                correlatedEventId: localTx.EventId,
                receipt: refundId);
            refundTx.AddActor(attempt.PlayerId, PlayerTransactionActorRole.Sender);

            var grantedItems = localTx.ItemChanges.Where(i => i.Operation == ItemOperation.Grant).ToList();
            if (isFullRefund)
            {
                foreach (var item in grantedItems)
                    refundTx.AddItemChange(item.ItemType, item.Quantity, ItemOperation.Revoke);
            }

            refundTx.MarkApplied();
            db.PlayerTransactions.Add(refundTx);
            await db.SaveChangesAsync(ct);

            if (isFullRefund)
            {
                foreach (var item in grantedItems)
                    await entitlementService.RevokeAsync(attempt.PlayerId, item.ItemType, item.Quantity, refundTx.Id, ct);
            }

            attempt.MarkRefunded();
            await db.SaveChangesAsync(ct);

            await AdminAuditLogger.WriteAsync(db, ctx, "payments.refund", "PaymentCheckoutAttempt", id.ToString(),
                before: new { status = "Captured" },
                after: new { status = "Refunded", refundId, refundStatus, isFullRefund, reason = req.Reason }, ct);

            return Results.Ok(new AdminPaymentRefundResponse(id, refundId, refundStatus, isFullRefund, refundTx.Id));
        });

        var issues = admin.MapGroup("/payment-reconciliation/issues").WithTags("Admin/Payments");

        issues.MapGet("", async (
            [FromQuery] bool? resolved,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb db,
            CancellationToken ct) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);

            var query = db.PaymentReconciliationIssues.AsNoTracking().AsQueryable();

            if (resolved is not null)
                query = resolved.Value ? query.Where(i => i.ResolvedAtUtc != null) : query.Where(i => i.ResolvedAtUtc == null);

            query = query.OrderByDescending(i => i.CreatedAtUtc);

            var total = await query.CountAsync(ct);
            var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return Results.Ok(new AdminPaymentIssueListResponse(page, pageSize, total, rows.Select(ToIssueDto).ToList()));
        });

        issues.MapPost("/{id:guid}/resolve", async (
            HttpContext ctx,
            Guid id,
            [FromBody] AdminPaymentIssueResolveRequest req,
            IAppDb db,
            CancellationToken ct) =>
        {
            var issue = await db.PaymentReconciliationIssues.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (issue is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Reconciliation issue not found.");

            if (issue.ResolvedAtUtc is not null)
                return ApiResponses.Error(StatusCodes.Status409Conflict, "ALREADY_RESOLVED", "This issue has already been resolved.");

            issue.Resolve(ResolveActor(ctx), req.Notes);
            await db.SaveChangesAsync(ct);

            await AdminAuditLogger.WriteAsync(db, ctx, "payments.resolve_issue", "PaymentReconciliationIssue", id.ToString(),
                before: new { resolved = false }, after: new { resolved = true, notes = req.Notes }, ct);

            return Results.Ok(ToIssueDto(issue));
        });
    }

    private static AdminPaymentListItemDto ToListItemDto(PaymentCheckoutAttempt a) => new(
        a.Id, a.PlayerId, a.Provider, a.Sku, a.Quantity, a.ExpectedAmount, a.Currency,
        a.ProviderRef, a.ProviderCaptureRef, a.Status.ToString(), a.PlayerTransactionId,
        a.CreatedAtUtc, a.ResolvedAtUtc);

    private static AdminPaymentIssueDto ToIssueDto(PaymentReconciliationIssue i) => new(
        i.Id, i.Category.ToString(), i.Provider, i.ProviderRef, i.PaymentCheckoutAttemptId, i.PlayerId,
        i.ExpectedAmount, i.ActualAmount, i.Details, i.CreatedAtUtc, i.ResolvedAtUtc, i.ResolvedBy, i.ResolutionNotes);

    private static Guid CreateDeterministicGuid(string source)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, bytes.Length);
        return new Guid(bytes);
    }

    private static string ResolveActor(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue(AdminHeader, out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString();

        var sub = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? http.User.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(sub) ? "unknown" : sub;
    }

    public sealed record AdminPaymentListItemDto(
        Guid Id, Guid PlayerId, string Provider, string Sku, int Quantity, decimal ExpectedAmount, string Currency,
        string ProviderRef, string? ProviderCaptureRef, string Status, Guid? PlayerTransactionId,
        DateTimeOffset CreatedAtUtc, DateTimeOffset? ResolvedAtUtc);

    public sealed record AdminPaymentListResponse(int Page, int PageSize, int Total, IReadOnlyList<AdminPaymentListItemDto> Items);

    public sealed record AdminPaymentIssueDto(
        Guid Id, string Category, string Provider, string ProviderRef, Guid? PaymentCheckoutAttemptId, Guid? PlayerId,
        decimal? ExpectedAmount, decimal? ActualAmount, string Details,
        DateTimeOffset CreatedAtUtc, DateTimeOffset? ResolvedAtUtc, string? ResolvedBy, string? ResolutionNotes);

    public sealed record AdminPaymentIssueListResponse(int Page, int PageSize, int Total, IReadOnlyList<AdminPaymentIssueDto> Items);

    public sealed record AdminPaymentDetailDto(AdminPaymentListItemDto Attempt, IReadOnlyList<AdminPaymentIssueDto> Issues);

    public sealed record AdminPaymentReconcileResponse(Guid AttemptId, string Status, bool IssueRaised);

    public sealed record AdminPaymentRetryFulfillmentResponse(Guid AttemptId, Guid PlayerTransactionId, string Status);

    public sealed record AdminPaymentRefundRequest(string Reason, decimal? Amount);

    public sealed record AdminPaymentRefundResponse(Guid AttemptId, string RefundId, string RefundStatus, bool IsFullRefund, Guid RefundTransactionId);

    public sealed record AdminPaymentIssueResolveRequest(string? Notes);
}
