namespace Tycoon.Backend.Domain.Personalization;

public sealed class PersonalizationRule
{
    public Guid Id { get; set; }
    public string RuleKey { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string RuleJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
