using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Tycoon.Backend.Application.Leaderboards;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Backend.Application.Seasons;

namespace Tycoon.Backend.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var asm = Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(asm));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
            services.AddValidatorsFromAssembly(asm);

            // Match
            services.AddScoped<Matches.StartMatch>();
            services.AddScoped<Matchmaking.MatchmakingService>();
            services.TryAddSingleton<Matchmaking.IMatchmakingNotifier,Matchmaking.NullMatchmakingNotifier>();

            // Missions
            services.AddScoped<Missions.MissionProgressService>();
            services.AddTransient<Missions.Jobs.QuestionAnsweredMissionJob>();

            // Tiers
            services.AddScoped<Tiers.TierResolver>();

            // Leaderboard recalculation
            services.AddScoped<Leaderboards.LeaderboardRecalculator>();
            services.AddScoped<Leaderboards.LeaderboardRecalculationJob>();

            // Media
            services.AddSingleton<Media.MediaService>();

            // Economy
            services.AddScoped<Economy.EconomyService>();

            // Powerups
            services.AddScoped<Powerups.PowerupService>();

            // Skill Tree
            services.AddScoped<Skills.SkillTreeService>();

            // Submit Match
            services.AddScoped<Matches.SubmitMatch>();

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

            // Realtime
            services.TryAddSingleton<IPresenceReader, NullPresenceReader>();

            // Seasonal Ranks
            services.Configure<RankedSeasonOptions>(cfg => { /* defaults ok */ });
            services.AddScoped<RankedLeaderboardService>();
            services.AddScoped<SeasonRewardsService>();

            //services.Configure<SeasonRewardOptions>(configuration.GetSection("SeasonRewards"));
            services.AddScoped<SeasonRewardJob>();

            return services;
        }
    }
}
