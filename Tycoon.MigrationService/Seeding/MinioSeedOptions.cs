namespace Tycoon.MigrationService.Seeding
{
    public sealed class MinioSeedOptions
    {
        public string StoreItemsKey { get; set; } = "seeds/store-items.json";
        public string SkillNodesKey { get; set; } = "seeds/skill-nodes.json";
        public string SeasonRewardsKey { get; set; } = "seeds/season-rewards.json";
        public string QuestionsKey { get; set; } = "seeds/questions.json";

        /// <summary>
        /// Optional override for bundled seed file lookup. Defaults to AppContext.BaseDirectory.
        /// </summary>
        public string? BundledRootPath { get; set; }
    }
}
