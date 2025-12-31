namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PlayerWallet
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }

        public int Xp { get; private set; }
        public int Coins { get; private set; }
        public int Diamonds { get; private set; }

        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerWallet() { } // EF

        public PlayerWallet(Guid playerId)
        {
            PlayerId = playerId;
        }

        public bool CanApply(int dxp, int dcoins, int ddiamonds)
            => (Xp + dxp) >= 0 && (Coins + dcoins) >= 0 && (Diamonds + ddiamonds) >= 0;

        public void Apply(int dxp, int dcoins, int ddiamonds)
        {
            Xp += dxp;
            Coins += dcoins;
            Diamonds += ddiamonds;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
