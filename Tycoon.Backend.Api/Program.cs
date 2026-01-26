using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
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
using Tycoon.Backend.Api.Features.Party;
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
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Writers;
using Tycoon.Backend.Application.Matchmaking;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Backend.Application.Social;
using Tycoon.Backend.Infrastructure;
using Tycoon.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

// Observability + Serilog + OTEL
builder.AddObservability();
builder.AddObservability("Tycoon.Backend.Api");

// JSON configuration
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ IMPROVED SWAGGER CONFIGURATION
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<DateOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "date"
    });

    c.MapType<TimeOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "time"
    });

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tycoon Backend API",
        Version = "v1",
        Description = "Trivia Tycoon Game Backend - Multiplayer Quiz Game API",
        Contact = new OpenApiContact
        {
            Name = "Tycoon Development Team"
        }
    });

    // ✅ Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // ✅ Try to load XML documentation (with error handling)
    try
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
            Console.WriteLine($"✅ Loaded XML documentation from: {xmlPath}");
        }
        else
        {
            Console.WriteLine($"⚠️ XML documentation not found at: {xmlPath}");
            Console.WriteLine("   Add <GenerateDocumentationFile>true</GenerateDocumentationFile> to .csproj");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Could not load XML documentation: {ex.Message}");
    }

    // ✅ Handle schema generation properly
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddCors();

// Analytics
var analyticsEnabled = builder.Configuration.GetValue("Analytics:Enabled", false);
if (analyticsEnabled)
{
    builder.Services.RemoveAll<IAnalyticsEventWriter>();
    builder.Services.AddScoped<IAnalyticsEventWriter, PostgresAnalyticsEventWriter>();
}

// Infrastructure & Application
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// SignalR with Redis
var redis = builder.Configuration.GetConnectionString("redis")
            ?? builder.Configuration.GetConnectionString("cache")
            ?? builder.Configuration.GetConnectionString("Redis");

var signalr = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redis))
{
    Console.WriteLine($"✅ Configuring SignalR with Redis: {redis}");
    signalr.AddStackExchangeRedis(redis);
}
else
{
    Console.WriteLine("⚠️ SignalR running without Redis backplane");
}

// Hangfire
var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);

if (hangfireEnabled)
{
    var hangfireDb =
        builder.Configuration.GetConnectionString("db")
        ?? builder.Configuration.GetConnectionString("PostgreSQL");

    if (string.IsNullOrWhiteSpace(hangfireDb))
    {
        Console.WriteLine("⚠️ No Hangfire database connection. Hangfire disabled.");
        hangfireEnabled = false;
    }
    else
    {
        Console.WriteLine("✅ Configuring Hangfire with PostgreSQL");

        builder.Services.AddHangfire(cfg =>
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings()
               .UsePostgreSqlStorage(hangfireDb, new PostgreSqlStorageOptions
               {
                   QueuePollInterval = TimeSpan.FromSeconds(5),
                   InvisibilityTimeout = TimeSpan.FromMinutes(5),
                   PrepareSchemaIfNecessary = true,
                   SchemaName = "hangfire"
               }));

        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid()}";
        });
    }
}

// JWT Authentication
var jwtKey = builder.Configuration["Auth:JwtKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "dev-only-change-me-dev-only-change-me-dev-only-change-me";
    Console.WriteLine("⚠️ Using default JWT key for development!");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
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

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("matches-submit", httpContext =>
    {
        var key = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.Identity.Name ?? "anonymous"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            });
    });

    options.AddPolicy("api", httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddSingleton<IMatchmakingNotifier, SignalRMatchmakingNotifier>();
builder.Services.AddSingleton<IPartyMatchmakingNotifier, SignalRPartyMatchmakingNotifier>();
builder.Services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
builder.Services.AddSingleton<IPresenceReader, SignalRPresenceReader>();

builder.Services.AddAuthorization(opts => opts.AddAdminPolicies());

var app = builder.Build();

// ✅ CORRECT MIDDLEWARE ORDER
app.UseRouting();

// ✅ Show detailed errors in development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(c => c.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .SetIsOriginAllowed(_ => true));
app.UseRateLimiter();
app.UseMiddleware<AdminOpsKeyMiddleware>();

// ✅ SWAGGER CONFIGURATION
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tycoon Backend API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Tycoon API Documentation";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
    });
}

// Hangfire Dashboard
if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = app.Environment.IsDevelopment()
            ? System.Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
            : new[] { new HangfireAuthorizationFilter() }
    });

    try
    {
        RecurringJob.AddOrUpdate<Tycoon.Backend.Application.Leaderboards.LeaderboardRecalculationJob>(
            "daily-leaderboard-recalc",
            job => job.Run(),
            "0 5 * * *"
        );
        app.Logger.LogInformation("✅ Hangfire recurring jobs configured");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to configure Hangfire recurring jobs");
    }
}

// Health endpoints
app.MapGet("/", () => Results.Ok(new
{
    name = "Tycoon.Backend.Api",
    version = "1.0.0",
    status = "healthy",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    services = new
    {
        hangfire = hangfireEnabled,
        redis = !string.IsNullOrWhiteSpace(redis),
        analytics = analyticsEnabled,
        swagger = app.Environment.IsDevelopment()
    }
})).AllowAnonymous().WithTags("Health");

app.MapGet("/healthz", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
})).AllowAnonymous().WithTags("Health");

app.MapGet("/health/ready", () =>
{
    var health = new Dictionary<string, object>
    {
        ["status"] = "ready",
        ["timestamp"] = DateTime.UtcNow,
        ["checks"] = new Dictionary<string, string>
        {
            ["api"] = "healthy",
            ["hangfire"] = hangfireEnabled ? "enabled" : "disabled",
            ["redis"] = !string.IsNullOrWhiteSpace(redis) ? "configured" : "not configured"
        }
    };
    return Results.Ok(health);
}).AllowAnonymous().WithTags("Health");

// ✅ Swagger test endpoint (for debugging)
app.MapGet("/swagger-debug", () =>
{
    try
    {
        var endpoints = app.Services.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>();
        var count = endpoints?.Endpoints.Count() ?? 0;

        return Results.Ok(new
        {
            message = "Swagger is working",
            endpointCount = count,
            swaggerUrl = "/swagger/v1/swagger.json"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            error = ex.Message,
            stackTrace = ex.StackTrace
        });
    }
}).AllowAnonymous().WithTags("Debug");

app.MapControllers();

// SignalR hubs
app.MapHub<MatchHub>("/ws/match");
app.MapHub<PresenceHub>("/ws/presence");
app.MapHub<NotificationHub>("/ws/notify");

// Feature endpoints
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
PartyEndpoints.Map(app);
RankedLeaderboardsEndpoints.Map(app);
SeasonRewardsEndpoints.Map(app);

// Admin endpoints
var admin = app.MapGroup("/admin").RequireAdminOpsKey();
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
AdminAntiCheatAnalyticsEndpoints.Map(admin);
AdminPartyAntiCheatEndpoints.Map(admin);
AdminSeasonRewardsEndpoints.Map(admin);
AdminSeasonLifecycleEndpoints.Map(admin);

// Startup logging
app.Logger.LogInformation("🚀 Tycoon Backend API starting...");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Hangfire: {Status}", hangfireEnabled ? "Enabled" : "Disabled");
app.Logger.LogInformation("Redis: {Status}", !string.IsNullOrWhiteSpace(redis) ? "Configured" : "Not Configured");
app.Logger.LogInformation("Analytics: {Status}", analyticsEnabled ? "Enabled" : "Disabled");

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("📚 Swagger UI: http://localhost:5000/swagger");
    if (hangfireEnabled)
        app.Logger.LogInformation("📊 Hangfire Dashboard: http://localhost:5000/hangfire");
    app.Logger.LogInformation("🔍 Swagger Debug: http://localhost:5000/swagger-debug");
}

app.Run();

public partial class Program { }

public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User?.Identity?.IsAuthenticated ?? false;
    }
}