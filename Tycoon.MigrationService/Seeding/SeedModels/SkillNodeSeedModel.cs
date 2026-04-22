namespace Tycoon.MigrationService.Seeding.SeedModels;

public sealed record SkillNodeSeedModel(
    string Key,
    string Branch,
    int Tier,
    string Title,
    string Description,
    string[] PrereqKeys,
    SkillCostSeedModel[] Costs,
    Dictionary<string, double> Effects
);

public sealed record SkillCostSeedModel(
    string Currency,
    int Amount
);
