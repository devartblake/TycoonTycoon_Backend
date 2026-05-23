using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Economy;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    public sealed record CompleteModule(Guid ModuleId, Guid PlayerId)
        : IRequest<CompleteModuleResultDto>;

    public sealed class CompleteModuleHandler
        : IRequestHandler<CompleteModule, CompleteModuleResultDto>
    {
        private readonly IAppDb _db;
        private readonly EconomyService _economy;
        private readonly IPlayerMindProfileService? _mindProfiles;

        public CompleteModuleHandler(IAppDb db, EconomyService economy, IPlayerMindProfileService? mindProfiles = null)
        {
            _db = db;
            _economy = economy;
            _mindProfiles = mindProfiles;
        }

        public async Task<CompleteModuleResultDto> Handle(
            CompleteModule request,
            CancellationToken ct)
        {
            // 1. Load module
            var module = await _db.LearningModules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId && m.IsPublished, ct);

            if (module is null)
                return new CompleteModuleResultDto(
                    request.ModuleId, request.PlayerId,
                    "ModuleNotFound", 0, 0, 0, 0);

            // 2. Check for existing completion (idempotent fast path)
            var existing = await _db.ModuleCompletions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.PlayerId == request.PlayerId && c.ModuleId == request.ModuleId,
                    ct);

            if (existing is not null)
            {
                var wallet = await _db.PlayerWallets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId, ct);

                return new CompleteModuleResultDto(
                    request.ModuleId, request.PlayerId,
                    "AlreadyCompleted",
                    module.RewardXp, module.RewardCoins,
                    wallet?.Xp ?? 0, wallet?.Coins ?? 0);
            }

            // 3. Create completion record (unique index on (PlayerId, ModuleId) catches races)
            var completion = new ModuleCompletion(request.PlayerId, request.ModuleId);
            _db.ModuleCompletions.Add(completion);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Race: another request beat us — treat as AlreadyCompleted
                var wallet = await _db.PlayerWallets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId, ct);

                return new CompleteModuleResultDto(
                    request.ModuleId, request.PlayerId,
                    "AlreadyCompleted",
                    module.RewardXp, module.RewardCoins,
                    wallet?.Xp ?? 0, wallet?.Coins ?? 0);
            }

            // 4. Grant reward via EconomyService (EventId comes from the completion record)
            var lines = new List<EconomyLineDto>();
            if (module.RewardXp > 0)
                lines.Add(new EconomyLineDto(CurrencyType.Xp, module.RewardXp));
            if (module.RewardCoins > 0)
                lines.Add(new EconomyLineDto(CurrencyType.Coins, module.RewardCoins));

            var txnResult = await _economy.ApplyAsync(
                new CreateEconomyTxnRequest(
                    EventId: completion.EconomyEventId,
                    PlayerId: request.PlayerId,
                    Kind: "module-completion",
                    Lines: lines,
                    Note: $"Module: {module.Title}"
                ),
                ct);

            if (_mindProfiles is not null)
            {
                try
                {
                    await _mindProfiles.RecordEventAsync(request.PlayerId, new PlayerBehaviorEventDto(
                        EventType: "learning_module_completed",
                        EventSource: "learning",
                        Category: module.Category,
                        Difficulty: module.Difficulty.ToString(),
                        Mode: "study",
                        Metadata: new Dictionary<string, object>
                        {
                            ["moduleId"] = request.ModuleId,
                            ["title"] = module.Title
                        },
                        OccurredAt: DateTimeOffset.UtcNow), ct);
                }
                catch { /* personalization must never break module completion */ }
            }

            return new CompleteModuleResultDto(
                request.ModuleId, request.PlayerId,
                "Completed",
                module.RewardXp, module.RewardCoins,
                txnResult.BalanceXp, txnResult.BalanceCoins);
        }
    }
}
