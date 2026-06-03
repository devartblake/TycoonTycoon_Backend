namespace Synaptix.MigrationService.Seeding
{
    public sealed class MinioSeedOptions
    {
        public string StoreItemsKey { get; set; } = "seeds/store-items.json";
        public string SkillNodesKey { get; set; } = "seeds/skill-nodes.json";
        public string SeasonRewardsKey { get; set; } = "seeds/season-rewards.json";

        // Single-file fallback — used when QuestionDatasetKeys is empty.
        public string QuestionsKey { get; set; } = "seeds/questions.json";

        // When non-empty, replaces QuestionsKey; all files are merged and deduplicated.
        public List<string> QuestionDatasetKeys { get; set; } = [];

        // Asset catalog for non-purchasable 3D assets (environments, effects, etc.)
        public string AssetCatalogKey { get; set; } = "seeds/asset-catalog.json";

        /// <summary>
        /// Optional override for bundled seed file lookup. Defaults to AppContext.BaseDirectory.
        /// </summary>
        public string? BundledRootPath { get; set; }
    }
}
