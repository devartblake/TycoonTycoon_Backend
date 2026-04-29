namespace Tycoon.Backend.Domain.Personalization;

public sealed class PlayerBehaviorEvent
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public string EventType { get; set; } = "";
    public string EventSource { get; set; } = "";

    public string? Category { get; set; }
    public string? Difficulty { get; set; }
    public string? Mode { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
}
