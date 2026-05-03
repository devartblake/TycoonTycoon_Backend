using System;
using System.Collections.Generic;
using System.Text;

namespace Tycoon.MigrationService.Seeding
{
    public sealed class MinioSeedOptions
    {
        public string StoreItemsKey { get; set; } = "tycoon-assets/store-items.json";
        public string SkillNodesKey { get; set; } = "tycoon-assets/skill-nodes.json";
        public string SeasonRewardsKey { get; set; } = "tycoon-assets/season-rewards.json";
        public string QuestionsKey { get; set; } = "tycoon-assets/questions.json";
    }
}
