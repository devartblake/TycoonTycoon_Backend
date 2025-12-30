namespace Tycoon.Backend.Domain.Entities
{
    public sealed class ReferralCode
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Code { get; private set; } = string.Empty;

        public Guid OwnerPlayerId { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private ReferralCode() { } // EF

        public ReferralCode(Guid ownerPlayerId, string code)
        {
            OwnerPlayerId = ownerPlayerId;
            Code = code;
        }
    }
}
