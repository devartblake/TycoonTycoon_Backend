namespace Tycoon.Backend.Domain.Services
{
    /// <summary>
    /// Output of tier assignment based on score.
    /// </summary>
    public sealed record TierAssignmentResult(
        Guid TierId,
        string TierName,
        int TierOrder
    );
}
