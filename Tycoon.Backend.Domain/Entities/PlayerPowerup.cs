using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PlayerPowerup
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public PowerupType Type { get; private set; }
        public int Quantity { get; private set; }
        public DateTimeOffset? CooldownUntilUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerPowerup() { } // EF

        public PlayerPowerup(Guid playerId, PowerupType type)
        {
            PlayerId = playerId;
            Type = type;
            Quantity = 0;
        }

        public void Add(int qty)
        {
            Quantity += Math.Max(0, qty);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public bool CanUse(DateTimeOffset now, out string reason)
        {
            if (Quantity <= 0) { reason = "Insufficient"; return false; }
            if (CooldownUntilUtc.HasValue && CooldownUntilUtc.Value > now) { reason = "Cooldown"; return false; }
            reason = "OK";
            return true;
        }

        public void Use(DateTimeOffset now, TimeSpan cooldown)
        {
            Quantity -= 1;
            CooldownUntilUtc = cooldown == TimeSpan.Zero ? null : now.Add(cooldown);
            UpdatedAtUtc = now;
        }
    }
}
