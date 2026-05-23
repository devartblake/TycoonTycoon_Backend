namespace Tycoon.Backend.Application.Rewards;

public sealed record RewardLine(string Type, int Amount, string? Sku = null, string? SkillNodeId = null, string? MissionKey = null);
