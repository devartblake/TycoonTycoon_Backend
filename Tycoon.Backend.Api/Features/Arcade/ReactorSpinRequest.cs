namespace Tycoon.Backend.Api.Features.Arcade;

public sealed record ReactorSpinRequest(
    string IdempotencyKey,
    string ReactorId,
    ReactorSpinContext? Context
);

public sealed record ReactorSpinContext(
    string Source,
    string? MissionId,
    string? EventId
);
