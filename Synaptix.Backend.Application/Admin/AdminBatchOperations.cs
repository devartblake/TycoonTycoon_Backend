using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Moderation;
using Synaptix.Backend.Application.Skills;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Admin
{
    // #413 batch operations. Semantics shared by all three commands:
    //  - ids are deduped; more than MaxBatchSize ids is a caller error (endpoints 400 first,
    //    handlers throw as a second line of defense);
    //  - per-player try/catch — one bad id never aborts the rest (partial-failure results);
    //  - reward/reset derive a deterministic per-player EventId from (BatchId, playerId), so
    //    retrying the same batch is idempotent through the economy/transaction dedupe paths.
    public sealed record AdminBulkBan(
        IReadOnlyList<Guid> PlayerIds,
        string Reason,
        DateTimeOffset? Until,
        string? Actor) : IRequest<BatchOperationResultDto>;

    public sealed record AdminBulkReward(
        Guid BatchId,
        IReadOnlyList<Guid> PlayerIds,
        IReadOnlyList<EconomyLineDto> Rewards,
        string? Note,
        string? Actor) : IRequest<BatchOperationResultDto>;

    public sealed record AdminBulkResetProgress(
        Guid BatchId,
        IReadOnlyList<Guid> PlayerIds,
        int RefundPercent,
        string? Actor) : IRequest<BatchOperationResultDto>;

    public static class AdminBatchOperations
    {
        public const int MaxBatchSize = 500;

        internal static Guid[] DedupeAndValidate(IReadOnlyList<Guid> ids)
        {
            var deduped = (ids ?? Array.Empty<Guid>()).Distinct().ToArray();
            if (deduped.Length > MaxBatchSize)
                throw new ArgumentException($"Batch operations accept at most {MaxBatchSize} player ids.");
            return deduped;
        }

        // Deterministic per-player event id so retrying a batch is idempotent.
        internal static Guid PerPlayerEventId(Guid batchId, Guid playerId)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"admin-batch:{batchId:N}:{playerId:N}"));
            var bytes = new byte[16];
            Array.Copy(hash, bytes, bytes.Length);
            return new Guid(bytes);
        }

        internal static BatchOperationResultDto ToResult(IReadOnlyList<BatchOperationItemResultDto> items) =>
            new(items.Count, items.Count(i => i.Success), items.Count(i => !i.Success), items);
    }

    public sealed class AdminBulkBanHandler(
        IAppDb db,
        ModerationService moderation,
        ILogger<AdminBulkBanHandler> logger) : IRequestHandler<AdminBulkBan, BatchOperationResultDto>
    {
        public async ValueTask<BatchOperationResultDto> Handle(AdminBulkBan r, CancellationToken ct)
        {
            var ids = AdminBatchOperations.DedupeAndValidate(r.PlayerIds);
            var items = new List<BatchOperationItemResultDto>(ids.Length);

            foreach (var playerId in ids)
            {
                try
                {
                    if (playerId == Guid.Empty)
                        throw new ArgumentException("Player id must not be empty.");

                    await moderation.SetStatusAsync(
                        playerId,
                        ModerationStatus.Banned,
                        r.Reason,
                        notes: null,
                        setByAdmin: r.Actor,
                        expiresAtUtc: r.Until,
                        relatedFlagId: null,
                        ct);

                    // Best effort: also deactivate the login account when one exists
                    // (mirrors AdminBanUserHandler; Player/User are unlinked aggregates,
                    // so a missing User row is not a failure).
                    var user = await db.Users.FirstOrDefaultAsync(x => x.Id == playerId, ct);
                    if (user is not null)
                    {
                        user.SetActive(false);
                        await db.SaveChangesAsync(ct);
                    }

                    items.Add(new BatchOperationItemResultDto(playerId, true, null));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    items.Add(new BatchOperationItemResultDto(playerId, false, ex.Message));
                }
            }

            var result = AdminBatchOperations.ToResult(items);
            logger.LogInformation(
                "Admin bulk ban: Actor={Actor}, Requested={Requested}, Succeeded={Succeeded}, Failed={Failed}",
                r.Actor, result.Requested, result.Succeeded, result.Failed);
            return result;
        }
    }

    public sealed class AdminBulkRewardHandler(
        IEconomyService economy,
        ILogger<AdminBulkRewardHandler> logger) : IRequestHandler<AdminBulkReward, BatchOperationResultDto>
    {
        public async ValueTask<BatchOperationResultDto> Handle(AdminBulkReward r, CancellationToken ct)
        {
            var ids = AdminBatchOperations.DedupeAndValidate(r.PlayerIds);
            var items = new List<BatchOperationItemResultDto>(ids.Length);

            foreach (var playerId in ids)
            {
                try
                {
                    if (playerId == Guid.Empty)
                        throw new ArgumentException("Player id must not be empty.");

                    var result = await economy.ApplyAsync(new CreateEconomyTxnRequest(
                        EventId: AdminBatchOperations.PerPlayerEventId(r.BatchId, playerId),
                        PlayerId: playerId,
                        Kind: "admin-bulk-reward",
                        Lines: r.Rewards,
                        Note: r.Note), ct);

                    // Duplicate = this batch already rewarded this player (idempotent retry).
                    var ok = result.Status is EconomyTxnStatus.Applied or EconomyTxnStatus.Duplicate;
                    items.Add(new BatchOperationItemResultDto(
                        playerId, ok, ok ? null : result.Status.ToString()));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    items.Add(new BatchOperationItemResultDto(playerId, false, ex.Message));
                }
            }

            var result2 = AdminBatchOperations.ToResult(items);
            logger.LogInformation(
                "Admin bulk reward: Actor={Actor}, BatchId={BatchId}, Requested={Requested}, Succeeded={Succeeded}, Failed={Failed}",
                r.Actor, r.BatchId, result2.Requested, result2.Succeeded, result2.Failed);
            return result2;
        }
    }

    public sealed class AdminBulkResetProgressHandler(
        SkillTreeService skills,
        ILogger<AdminBulkResetProgressHandler> logger) : IRequestHandler<AdminBulkResetProgress, BatchOperationResultDto>
    {
        public async ValueTask<BatchOperationResultDto> Handle(AdminBulkResetProgress r, CancellationToken ct)
        {
            var ids = AdminBatchOperations.DedupeAndValidate(r.PlayerIds);
            var items = new List<BatchOperationItemResultDto>(ids.Length);

            foreach (var playerId in ids)
            {
                try
                {
                    if (playerId == Guid.Empty)
                        throw new ArgumentException("Player id must not be empty.");

                    await skills.RespecAsync(new RespecSkillsRequest(
                        AdminBatchOperations.PerPlayerEventId(r.BatchId, playerId),
                        playerId,
                        r.RefundPercent), ct);

                    items.Add(new BatchOperationItemResultDto(playerId, true, null));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    items.Add(new BatchOperationItemResultDto(playerId, false, ex.Message));
                }
            }

            var result = AdminBatchOperations.ToResult(items);
            logger.LogInformation(
                "Admin bulk skills reset: Actor={Actor}, BatchId={BatchId}, Requested={Requested}, Succeeded={Succeeded}, Failed={Failed}",
                r.Actor, r.BatchId, result.Requested, result.Succeeded, result.Failed);
            return result;
        }
    }
}
