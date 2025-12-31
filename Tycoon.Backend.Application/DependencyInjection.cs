using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Tycoon.Backend.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var asm = Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(asm));
            services.AddValidatorsFromAssembly(asm);

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

            return services;
        }
    }
}
