namespace Tycoon.Shared.Contracts.Dtos;

public record AdminAppConfigDto(
    string ApiBaseUrl,
    bool EnableLogging,
    Dictionary<string, bool> FeatureFlags
);

public record UpdateAdminAppConfigRequest(
    bool? EnableLogging,
    Dictionary<string, bool>? FeatureFlags
);

public record UpdateAdminAppConfigResponse(DateTimeOffset UpdatedAt);
