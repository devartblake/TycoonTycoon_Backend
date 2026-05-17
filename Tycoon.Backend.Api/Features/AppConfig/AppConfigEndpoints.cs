using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Api.Features.AppConfig;

public static class AppConfigEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/v1/app/config", async (
            IAppDb db,
            IHostEnvironment env,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var adminConfig = await db.AdminAppConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == "default", ct);

            Dictionary<string, bool> storedFlags = [];
            if (adminConfig is not null)
            {
                try
                {
                    storedFlags = JsonSerializer.Deserialize<Dictionary<string, bool>>(adminConfig.FeatureFlagsJson) ?? [];
                }
                catch { }
            }

            // Alpha/Beta defaults: core gameplay on, all experimental systems off.
            // DB flags stored in AdminAppConfig can override any value at runtime.
            bool Flag(string key, bool defaultOn) =>
                storedFlags.TryGetValue(key, out var v) ? v : defaultOn;

            var features = new
            {
                coreTriviaEnabled          = Flag("core_trivia_enabled",           true),
                walletEnabled              = Flag("wallet_enabled",                true),
                leaderboardEnabled         = Flag("leaderboard_enabled",           true),
                storeEnabled               = Flag("store_enabled",                 true),
                missionsEnabled            = Flag("missions_enabled",              true),
                skillTreeEnabled           = Flag("skill_tree_enabled",            false),
                realtimeMultiplayerEnabled = Flag("realtime_multiplayer_enabled",  false),
                matchmakingEnabled         = Flag("matchmaking_enabled",           false),
                tournamentsEnabled         = Flag("tournaments_enabled",           false),
                cryptoEnabled              = Flag("crypto_enabled",                false),
                tomPersonalizationEnabled  = Flag("tom_personalization_enabled",   false),
                socialEnabled              = Flag("social_enabled",                false),
                notificationsEnabled       = Flag("notifications_enabled",         false),
                experimentsEnabled         = Flag("experiments_enabled",           false),
                aiSidecarEnabled           = Flag("ai_sidecar_enabled",            false),
            };

            var minimumClientVersion = config["AppConfig:MinimumClientVersion"] ?? "0.0.1";
            var environment = env.EnvironmentName.ToLowerInvariant();

            return Results.Ok(new { environment, minimumClientVersion, features });
        })
        .WithTags("Config")
        .AllowAnonymous();
    }
}
