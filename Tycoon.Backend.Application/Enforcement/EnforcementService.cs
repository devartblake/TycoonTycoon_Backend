using Tycoon.Backend.Application.Moderation;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Enforcement
{
    public sealed record EnforcementDecision(
        bool CanStartMatch,
        bool CanSubmitMatch,
        bool AllowRewards,
        bool AllowSeasonPoints,
        string QueueScope, // Global | TierOnly | Practice | Shadow
        string? Reason
    );

    public sealed class EnforcementService(ModerationService moderation)
    {
        public async Task<EnforcementDecision> EvaluateAsync(
            Guid playerId,
            CancellationToken ct)
        {
            var status = await moderation.GetEffectiveStatusAsync(playerId, ct);

            return status switch
            {
                ModerationStatus.Banned =>
                    new EnforcementDecision(
                        CanStartMatch: false,
                        CanSubmitMatch: false,
                        AllowRewards: false,
                        AllowSeasonPoints: false,
                        QueueScope: "None",
                        Reason: "Player banned"),

                ModerationStatus.Restricted =>
                    new EnforcementDecision(
                        CanStartMatch: true,
                        CanSubmitMatch: true,
                        AllowRewards: false,
                        AllowSeasonPoints: false,
                        QueueScope: "TierOnly",
                        Reason: "Player restricted"),

                ModerationStatus.Suspected =>
                    new EnforcementDecision(
                        CanStartMatch: true,
                        CanSubmitMatch: true,
                        AllowRewards: true,
                        AllowSeasonPoints: true,
                        QueueScope: "Global",
                        Reason: "Player suspected"),

                _ =>
                    new EnforcementDecision(
                        CanStartMatch: true,
                        CanSubmitMatch: true,
                        AllowRewards: true,
                        AllowSeasonPoints: true,
                        QueueScope: "Global",
                        Reason: null)
            };
        }
    }
}
