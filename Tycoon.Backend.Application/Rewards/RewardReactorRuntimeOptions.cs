using Microsoft.Extensions.Options;

namespace Tycoon.Backend.Application.Rewards;

public sealed class RewardReactorRuntimeOptions
{
    public string? SeasonKey { get; init; }
    public string? SymbolSet { get; init; }
    public string? AssetBaseUrl { get; init; }
    public IReadOnlyList<RewardReactorEventOption> Events { get; init; } = [];
}

public sealed class RewardReactorEventOption
{
    public string EventId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; init; }
    public DateTimeOffset EndsAtUtc { get; init; }
    public double? EventMultiplier { get; init; }
}

public sealed record RewardReactorActiveEvent(
    string EventId,
    string DisplayName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    double? EventMultiplier
);

public sealed record RewardReactorSpinRuntimeContext(
    string? SeasonKey,
    string? EventId,
    double? EventMultiplier
);

public sealed record RewardReactorConfigSnapshot(
    string? SeasonKey,
    string? SymbolSet,
    string? AssetBaseUrl
);

public sealed class RewardReactorRuntimeContextService
{
    private readonly RewardReactorRuntimeOptions _options;

    public RewardReactorRuntimeContextService(IOptions<RewardReactorRuntimeOptions> options)
    {
        _options = options.Value;
    }

    public RewardReactorSpinRuntimeContext ResolveForSpin(DateTimeOffset utcNow)
    {
        var active = GetActiveEvents(utcNow)
            .OrderByDescending(e => e.StartsAtUtc)
            .FirstOrDefault();

        return new RewardReactorSpinRuntimeContext(
            SeasonKey: _options.SeasonKey,
            EventId: active?.EventId,
            EventMultiplier: active?.EventMultiplier);
    }

    public IReadOnlyList<RewardReactorActiveEvent> GetActiveEvents(DateTimeOffset utcNow)
    {
        return _options.Events
            .Where(e =>
                !string.IsNullOrWhiteSpace(e.EventId) &&
                e.StartsAtUtc <= utcNow &&
                e.EndsAtUtc > utcNow)
            .Select(e => new RewardReactorActiveEvent(
                e.EventId,
                e.DisplayName,
                e.StartsAtUtc,
                e.EndsAtUtc,
                e.EventMultiplier))
            .ToList();
    }

    public RewardReactorConfigSnapshot GetConfig()
        => new(_options.SeasonKey, _options.SymbolSet, _options.AssetBaseUrl);
}
