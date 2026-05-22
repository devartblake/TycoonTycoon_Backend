using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Application.Config
{
    /// <summary>
    /// Reads feature flags from AdminAppConfig once per DI scope (i.e. once per request).
    /// Missing flags default to <c>true</c> (enabled) so new flags are safe to add without
    /// requiring every existing deployment to explicitly set them.
    /// </summary>
    public sealed class FeatureFlagService(IAppDb db)
    {
        public const string GameEventsEnabled  = "game_events_enabled";
        public const string GuardianEnabled    = "guardian_enabled";
        public const string TerritoryEnabled   = "territory_enabled";

        private Dictionary<string, bool>? _flags;

        /// <summary>Returns the flag value, defaulting to <c>true</c> if the key is absent.</summary>
        public async Task<bool> IsEnabledAsync(string flagKey, CancellationToken ct)
        {
            if (_flags is null)
                await LoadAsync(ct);

            return _flags!.GetValueOrDefault(flagKey, true);
        }

        private async Task LoadAsync(CancellationToken ct)
        {
            var config = await db.AdminAppConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == "default", ct);

            if (config is null)
            {
                _flags = [];
                return;
            }

            try
            {
                _flags = JsonSerializer.Deserialize<Dictionary<string, bool>>(config.FeatureFlagsJson) ?? [];
            }
            catch
            {
                _flags = [];
            }
        }
    }
}
