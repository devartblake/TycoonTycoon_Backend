namespace Synaptix.Backend.Application.Personalization;

public sealed class PersonalizationOptions
{
    public bool Enabled { get; set; } = true;
    public bool UseSidecar { get; set; } = true;
    public bool AdaptiveQuestions { get; set; } = false;
    public bool AdaptiveMissions { get; set; } = true;
    public bool AdaptiveStore { get; set; } = true;
    public bool AdaptiveNotifications { get; set; } = true;
    public bool CoachEnabled { get; set; } = true;

    public decimal FrustrationPaidOfferSuppressionThreshold { get; set; } = 0.75m;
    public decimal NotificationFatigueThreshold { get; set; } = 0.70m;
}
