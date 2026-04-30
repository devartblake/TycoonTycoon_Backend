using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Experiments;

public interface IExperimentService
{
    /// <summary>
    /// Returns the variant a player is assigned to for the given experiment, or null if they are
    /// excluded from the experiment (allocation gating, experiment not running, or not found).
    /// Assignment is created and persisted on first call; subsequent calls return the same variant.
    /// </summary>
    Task<ExperimentAssignmentDto?> GetAssignmentAsync(Guid playerId, string experimentKey, CancellationToken ct = default);

    /// <summary>
    /// Returns all active-experiment assignments for a player in a single query — used by the
    /// client to bootstrap all experiment state at session start.
    /// </summary>
    Task<PlayerExperimentsDto> GetAllAssignmentsAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>
    /// Records that the player saw the variant UI. Increments impression_count and sets first_seen_at.
    /// </summary>
    Task RecordImpressionAsync(Guid playerId, string experimentKey, CancellationToken ct = default);

    /// <summary>
    /// Records a conversion / outcome for this player's assignment.
    /// </summary>
    Task RecordOutcomeAsync(Guid playerId, string experimentKey, Dictionary<string, object>? outcomeData = null, CancellationToken ct = default);
}
