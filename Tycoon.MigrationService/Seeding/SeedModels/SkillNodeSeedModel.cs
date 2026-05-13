namespace Tycoon.MigrationService.Seeding.SeedModels;

public sealed class SkillNodeSeedModel
{
    public string? Key { get; set; }
    public string? Id { get; set; }
    public string? Branch { get; set; }
    public string? Category { get; set; }
    public int Tier { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[]? PrereqKeys { get; set; }
    public SkillCostSeedModel[]? Costs { get; set; }
    public int Cost { get; set; }
    public Dictionary<string, double>? Effects { get; set; }
}

public sealed class SkillCostSeedModel
{
    public string Currency { get; set; } = "Coins";
    public int Amount { get; set; }
}
