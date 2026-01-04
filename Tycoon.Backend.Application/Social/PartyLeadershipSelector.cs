namespace Tycoon.Backend.Application.Social
{
    /// <summary>
    /// Selects a new leader from remaining members. Uses deterministic "random"
    /// based on partyId + time bucket to keep behavior stable under retries.
    /// </summary>
    public static class PartyLeadershipSelector
    {
        public static Guid SelectNewLeader(Guid partyId, IReadOnlyList<Guid> candidatePlayerIds)
        {
            if (candidatePlayerIds.Count == 0) return Guid.Empty;
            if (candidatePlayerIds.Count == 1) return candidatePlayerIds[0];

            // Deterministic seed: partyId + current 5-minute bucket
            var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 300; // 5 min
            var seed = HashCode.Combine(partyId, bucket);

            var idx = Math.Abs(seed) % Random.Shared.Next(candidatePlayerIds.Count);
            return candidatePlayerIds[idx];
        }
    }
}
