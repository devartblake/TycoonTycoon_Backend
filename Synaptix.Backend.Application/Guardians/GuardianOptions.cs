namespace Synaptix.Backend.Application.Guardians
{
    public sealed class GuardianOptions
    {
        public int PassiveCoins { get; set; } = 50;
        public int PassiveXp { get; set; } = 25;
        public int MaxGuardiansPerTier { get; set; } = 5;
        public int PromotionBonusXp { get; set; } = 200;
    }
}
