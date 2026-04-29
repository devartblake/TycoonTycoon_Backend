using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Application.EventStats;
using Tycoon.Backend.Application.GameEvents;
using Tycoon.Backend.Application.Guardians;
using Tycoon.Backend.Application.Leaderboards;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Backend.Application.Territory;

namespace Tycoon.Backend.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var asm = Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(asm));
            services.AddValidatorsFromAssembly(asm);

            // Match
            //services.AddScoped<Matches.StartMatch>();
            services.AddScoped<Matchmaking.MatchmakingService>();
            services.TryAddSingleton<Matchmaking.IMatchmakingNotifier, Matchmaking.NullMatchmakingNotifier>();

            // Missions
            services.AddScoped<Missions.MissionProgressService>();
            services.AddTransient<Missions.Jobs.QuestionAnsweredMissionJob>();

            // Tiers
            services.AddScoped<Tiers.TierResolver>();

            // Leaderboard recalculation
            services.AddScoped<Leaderboards.LeaderboardRecalculator>();
            services.AddScoped<Leaderboards.LeaderboardRecalculationJob>();
            services.AddScoped<Seasons.TierRecalculationJob>();

            // Media
            services.AddSingleton<Media.MediaService>();

            // Economy
            services.AddScoped<Economy.EconomyService>();

            // Player Transactions (aggregate)
            services.AddScoped<PlayerTransactions.PlayerTransactionService>();

            // Powerups
            services.AddScoped<Powerups.PowerupService>();

            // Skill Tree
            services.AddScoped<Skills.SkillTreeService>();

            // Submit Match
            //services.AddScoped<Matches.SubmitMatch>();

            // Seasons
            services.AddScoped<Seasons.SeasonService>();
            services.AddScoped<Seasons.SeasonPointsService>();
            services.AddScoped<Seasons.TierAssignmentService>();

            // Anti-Cheat
            services.AddSingleton<AntiCheat.AntiCheatService>();

            // Moderation
            services.AddScoped<Moderation.ModerationService>();
            services.AddScoped<Moderation.EscalationService>();

            // Enforcement - Centralize moderation, anti-cheat and season enforcement
            services.AddScoped<Enforcement.EnforcementService>();

            // Social
            services.AddScoped<Social.FriendsService>();
            services.AddScoped<Social.PartyService>();
            services.AddScoped<Social.PartyMatchmakingService>();
            services.AddScoped<Social.PartyLifecycleService>();
            services.TryAddSingleton<Social.IPartyMatchmakingNotifier, Social.NullPartyMatchmakingNotifier>();
            services.AddScoped<Social.PartyIntegrityService>();
            services.AddScoped<Notifications.PlayerInboxService>();
            services.AddScoped<Messaging.DirectMessagingService>();

            // Realtime
            services.TryAddSingleton<IPresenceReader, NullPresenceReader>();
            services.TryAddSingleton<IPlayerNotificationNotifier, NullPlayerNotificationNotifier>();
            services.TryAddSingleton<IDirectMessageNotifier, NullDirectMessageNotifier>();

            // Feature Flags
            services.AddScoped<FeatureFlagService>();
            services.AddScoped<IGameBalancePolicyService, GameBalancePolicyService>();

            // Event Stats
            services.AddScoped<PlayerEventStatsService>();

            // Game Events
            services.AddScoped<GameEventSchedulerJob>();
            services.AddScoped<CloseGameEventWorker>();
            services.TryAddSingleton<IGameEventNotifier, NullGameEventNotifier>();

            // Guardians
            services.Configure<GuardianOptions>(cfg => { /* defaults ok */ });
            services.AddScoped<GuardianAssignmentJob>();
            services.TryAddSingleton<IGuardianNotifier, NullGuardianNotifier>();

            // Territory
            services.TryAddSingleton<ITerritoryNotifier, NullTerritoryNotifier>();

            // Seasonal Ranks
            services.Configure<RankedSeasonOptions>(cfg => { /* defaults ok */ });
            services.AddScoped<RankedLeaderboardService>();
            services.AddScoped<SeasonRewardsService>();

            //services.Configure<SeasonRewardOptions>(configuration.GetSection("SeasonRewards"));
            services.AddScoped<SeasonRewardJob>();
            services.AddScoped<SeasonCloseOrchestrator>();

            // Personalization
            services.AddScoped<Personalization.IPlayerMindProfileService, Personalization.PlayerMindProfileService>();
            services.AddScoped<Personalization.IPersonalizationService, Personalization.PersonalizationService>();
            services.AddScoped<Personalization.IPersonalizationGuardrailService, Personalization.PersonalizationGuardrailService>();

            return services;
        }
    }
}
