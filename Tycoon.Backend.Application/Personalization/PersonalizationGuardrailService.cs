using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationGuardrailService : IPersonalizationGuardrailService
{
    public PersonalizationGuardrailResult Apply(PlayerMindProfileDto profile, PersonalizationCandidateDto candidate)
    {
        var rules = new Dictionary<string, object>();

        if (candidate.Type == "store_offer" && profile.FrustrationRiskScore >= 0.75m)
        {
            rules["suppress_paid_offers_when_frustrated"] = true;
            return new(false, "Paid offers suppressed due to high frustration risk.", rules);
        }

        if (candidate.Type == "notification" && profile.NotificationFatigueScore >= 0.70m)
        {
            rules["notification_fatigue_limit"] = true;
            return new(false, "Notification suppressed due to fatigue risk.", rules);
        }

        if (candidate.Type == "ranked_difficulty_modifier")
        {
            rules["ranked_fairness_lock"] = true;
            return new(false, "Ranked difficulty cannot be modified by personalization.", rules);
        }

        if (!profile.PersonalizationEnabled)
        {
            rules["personalization_disabled"] = true;
            return new(false, "Personalization is disabled for this player.", rules);
        }

        rules["allowed"] = true;
        return new(true, null, rules);
    }
}
