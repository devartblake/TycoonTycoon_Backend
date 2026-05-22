using Microsoft.Extensions.Options;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Personalization;

public sealed class PersonalizationGuardrailService : IPersonalizationGuardrailService
{
    private readonly PersonalizationOptions _options;

    public PersonalizationGuardrailService(IOptions<PersonalizationOptions> options)
    {
        _options = options.Value;
    }

    public PersonalizationGuardrailResult Apply(PlayerMindProfileDto profile, PersonalizationCandidateDto candidate)
    {
        var rules = new Dictionary<string, object>();

        if (!_options.Enabled)
        {
            rules["personalization_disabled"] = true;
            return new(false, "Personalization is disabled.", rules);
        }

        if (!profile.PersonalizationEnabled)
        {
            rules["player_personalization_disabled"] = true;
            return new(false, "Personalization is disabled for this player.", rules);
        }

        if (candidate.Type == "store_offer" &&
            profile.FrustrationRiskScore >= _options.FrustrationPaidOfferSuppressionThreshold)
        {
            rules["suppress_paid_offers_when_frustrated"] = true;
            return new(false, "Paid offers suppressed due to high frustration risk.", rules);
        }

        if (candidate.Type == "notification" &&
            profile.NotificationFatigueScore >= _options.NotificationFatigueThreshold)
        {
            rules["notification_fatigue_limit"] = true;
            return new(false, "Notification suppressed due to fatigue risk.", rules);
        }

        if (candidate.Type == "ranked_difficulty_modifier")
        {
            rules["ranked_fairness_lock"] = true;
            return new(false, "Ranked difficulty cannot be modified by personalization.", rules);
        }

        if (candidate.Type == "reward_grant")
        {
            rules["sidecar_direct_reward_grant_blocked"] = true;
            return new(false, "Sidecar cannot directly grant rewards.", rules);
        }

        rules["allowed"] = true;
        return new(true, null, rules);
    }
}
