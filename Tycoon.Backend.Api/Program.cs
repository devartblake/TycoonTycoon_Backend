using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Tycoon.Backend.Api.Features.AdminAnalytics;
using Tycoon.Backend.Api.Features.AdminAntiCheat;
using Tycoon.Backend.Api.Features.AdminEconomy;
using Tycoon.Backend.Api.Features.AdminMatches;
using Tycoon.Backend.Api.Features.AdminMedia;
using Tycoon.Backend.Api.Features.AdminModeration;
using Tycoon.Backend.Api.Features.AdminPowerups;
using Tycoon.Backend.Api.Features.AdminQuestions;
using Tycoon.Backend.Api.Features.AdminSeasons;
using Tycoon.Backend.Api.Features.AdminSkills;
using Tycoon.Backend.Api.Features.Friends;
using Tycoon.Backend.Api.Features.Leaderboards;
using Tycoon.Backend.Api.Features.Matches;
using Tycoon.Backend.Api.Features.Matchmaking;
using Tycoon.Backend.Api.Features.Missions;
using Tycoon.Backend.Api.Features.Players;
using Tycoon.Backend.Api.Features.Powerups;
using Tycoon.Backend.Api.Features.Qr;
using Tycoon.Backend.Api.Features.Referrals;
using Tycoon.Backend.Api.Features.Seasons;
using Tycoon.Backend.Api.Features.Skills;
using Tycoon.Backend.Api.Middleware;
using Tycoon.Backend.Api.Realtime;
using Tycoon.Backend.Api.Security;
using Tycoon.Backend.Application;
using Tycoon.Backend.Application.Matchmaking;
using Tycoon.Backend.Infrastructure;
using Tycoon.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

// Observability + Serilog + OTEL
builder.AddObservability();
builder.AddObservability("Tycoon.Backend.Api");

// JSON / Swagger / CORS
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer().AddSwaggerGen();
builder.Services.AddCors();

// Infra & App
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// SignalR (Redis backplane optional: Aspire uses "cache" for Redis in your AppHost pattern)
var redis = builder.Configuration.GetConnectionString("cache")
            ?? builder.Configuration.GetConnectionString("redis");

var signalr = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redis))
    signalr.AddStackExchangeRedis(redis);

var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);

if (!hangfireEnabled)
{
    // Hangfire (Postgres storage). Aspire typically names Postgres db "tycoon-db".
    var hangfireDb = builder.Configuration.GetConnectionString("tycoon-db")
                    ?? builder.Configuration.GetConnectionString("db");

    if (string.IsNullOrWhiteSpace(hangfireDb))
        throw new InvalidOperationException("Missing PostgreSQL connection string for Hangfire. Provide ConnectionStrings:tycoon-db (Aspire) or ConnectionStrings:db.");

    builder.Services.AddHangfire(cfg => cfg.UseSimpleAssemblyNameTypeSerializer()
                                           .UseRecommendedSerializerSettings()
                                           .UsePostgreSqlStorage(hangfireDb));
    builder.Services.AddHangfireServer();
}

// JWT Authentication
var jwtKey = builder.Configuration["Auth:JwtKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    // For dev only: allow boot without explicit config, but do not ship like this.
    jwtKey = "dev-only-change-me-dev-only-change-me-dev-only-change-me";
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false; // OK for local dev
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("matches-submit", httpContext =>
    {
        // Key by IP by default; if you have auth later, key by user id.
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,                 // 10 submits
                Window = TimeSpan.FromSeconds(10),// per 10 seconds
                QueueLimit = 0
            });
    });
});

builder.Services.AddSingleton<IMatchmakingNotifier, SignalRMatchmakingNotifier>();

// Authorization + policies
builder.Services.AddAuthorization(opts => opts.AddAdminPolicies());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

// Admin ops key gate (path-based on /admin/analytics and optional metadata)
app.UseMiddleware<AdminOpsKeyMiddleware>();

// Global exception handler
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger().UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire" /*, new Hangfire.Dashboard.DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
}*/);

app.UseRouting();
app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true));

// SignalR hubs
app.MapHub<MatchHub>("/ws/match");
app.MapHub<PresenceHub>("/ws/presence");
app.MapHub<NotificationHub>("/ws/notify");

// Map feature endpoints
PlayersEndpoints.Map(app);
MatchesEndpoints.Map(app);
MatchmakingEndpoints.Map(app);
MissionsEndpoints.Map(app);
LeaderboardsEndpoints.Map(app);
ReferralsEndpoints.Map(app);
QrEndpoints.Map(app);
SkillsEndpoints.Map(app);
PowerupsEndpoints.Map(app);
SeasonsEndpoints.Map(app);
FriendsEndpoints.Map(app);

var admin = app.MapGroup("/admin")
    .RequireAdminOpsKey();
AdminQuestionsEndpoints.Map(admin);
AdminMediaEndpoints.Map(admin);
AdminAnalyticsEndpoints.Map(admin);
AdminEconomyEndpoints.Map(admin);
AdminPowerupsEndpoints.Map(admin);
AdminSkillsEndpoints.Map(admin);
AdminMatchesEndpoints.Map(admin);
AdminSeasonsEndpoints.Map(admin);
AdminAntiCheatEndpoints.Map(admin);
AdminModerationEndpoints.Map(admin);
AdminEscalationEndpoints.Map(admin);
AdminEconomyEndpoints.Map(admin);
AdminAntiCheatAnalyticsEndpoints.Map(admin);

// IMPORTANT:
// Do NOT migrate here anymore. Tycoon.MigrationService owns migrations + seeding now.

// Hangfire recurring job placeholder
RecurringJob.AddOrUpdate<Tycoon.Backend.Application.Leaderboards.LeaderboardRecalculationJob>(
    "daily-leaderboard-recalc",
    job => job.Run(),
    "0 5 * * *" // 5:00 UTC daily; adjust as desired
);

app.Run();

public partial class Program { } // for WebApplicationFactory
