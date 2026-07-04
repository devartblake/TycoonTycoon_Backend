using Mediator;
using Synaptix.Commerce;
using Synaptix.Wallet;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Synaptix.Backend.Api.Features.AdminAnalytics;
using Synaptix.Backend.Api.Features.AdminAntiCheat;
using Synaptix.Backend.Api.Features.AdminAuth;
using Synaptix.Backend.Api.Features.AdminEconomy;
using Synaptix.Backend.Api.Features.AdminPlayerTransactions;
using Synaptix.Backend.Api.Features.AdminEventQueue;
using Synaptix.Backend.Api.Features.AdminMatches;
using Synaptix.Backend.Api.Features.AdminMedia;
using Synaptix.Backend.Api.Features.AdminModeration;
using Synaptix.Backend.Api.Features.AdminMongo;
using Synaptix.Backend.Api.Features.AdminNotifications;
using Synaptix.Backend.Api.Features.AppConfig;
using Synaptix.Backend.Api.Features.Quiz;
using Synaptix.Backend.Api.Features.AdminConfig;
using Synaptix.Backend.Api.Features.AdminEmailAcl;
using Synaptix.Backend.Api.Features.AdminPowerups;
using Synaptix.Backend.Api.Features.AdminQuestions;
using Synaptix.Backend.Api.Features.AdminStore;
using Synaptix.Backend.Api.Features.AdminStorage;
using Synaptix.Backend.Api.Features.AdminSetup;
using Synaptix.Backend.Api.Features.AdminSeasons;
using Synaptix.Backend.Api.Features.AdminExperiments;
using Synaptix.Backend.Api.Features.AdminPersonalization;
using Synaptix.Backend.Api.Features.AdminPlayerLookup;
using Synaptix.Backend.Api.Features.Experiments;
using Synaptix.Backend.Api.Features.AdminSkills;
using Synaptix.Backend.Api.Features.AdminUsers;
using Synaptix.Backend.Api.Features.Analytics;
using Synaptix.Backend.Api.Features.Auth;
using Synaptix.Backend.Api.Features.Friends;
using Synaptix.Backend.Api.Features.Leaderboards;
using Synaptix.Backend.Api.Features.Matches;
using Synaptix.Backend.Api.Features.Matchmaking;
using Synaptix.Backend.Api.Features.Messages;
using Synaptix.Backend.Api.Features.Missions;
using Synaptix.Backend.Api.Features.Ml;
using Synaptix.Backend.Api.Features.Mobile.Matches;
using Synaptix.Backend.Api.Features.Mobile.Seasons;
using Synaptix.Backend.Api.Features.Mobile.Players;
using Synaptix.Backend.Api.Features.Mobile.Leaderboards;
using Synaptix.Backend.Api.Features.Mobile.Economy;
using Synaptix.Backend.Api.Features.Notifications;
using Synaptix.Backend.Api.Features.Party;
using Synaptix.Backend.Api.Features.Players;
using Synaptix.Backend.Api.Features.Powerups;
using Synaptix.Backend.Api.Features.Qr;
using Synaptix.Backend.Api.Features.LearningModules;
using Synaptix.Backend.Api.Features.AdminLearningModules;
using Synaptix.Backend.Api.Features.Avatars;
using Synaptix.Backend.Api.Features.Personalization;
using Synaptix.Backend.Api.Features.Coach;
using Synaptix.Backend.Api.Features.Questions;
using Synaptix.Backend.Api.Features.Crypto;
using Synaptix.Backend.Api.Features.Store;
using Synaptix.Backend.Api.Features.Arcade;
using Synaptix.Backend.Api.Features.Events;
using Synaptix.Backend.Api.Features.Rewards;
using Synaptix.Backend.Api.Features.Progression;
using Synaptix.Backend.Api.Features.Account;
using Synaptix.Backend.Api.Features.Spins;
using Synaptix.Backend.Api.Features.Referrals;
using Synaptix.Backend.Api.Features.GameEvents;
using Synaptix.Backend.Api.Features.Guardians;
using Synaptix.Backend.Api.Features.Territory;
using Synaptix.Backend.Api.Features.Votes;
using Synaptix.Backend.Api.Features.Seasons;
using Synaptix.Backend.Api.Features.Skills;
using Synaptix.Backend.Api.Features.Study;
using Synaptix.Backend.Api.Features.Assets;
using Synaptix.Backend.Api.Features.Users;
using Synaptix.Backend.Api.Features.ParentalConsent;
using Synaptix.Backend.Api.Features.AdminPrivacy;
using Synaptix.Backend.Api.Features.Monitoring;
using Synaptix.Backend.Api.Middleware;
using Synaptix.Backend.Api.Observability;
using Synaptix.Backend.Api.Payments.PayPal;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Api.Realtime;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Api.Services;
using Synaptix.Backend.Application;
using Synaptix.Compliance.Client.Extensions;
using Synaptix.Security.Kms.Client.Extensions;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Backend.Api.Features.SecuritySessions;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.Analytics.Abstractions;
using Synaptix.Backend.Application.Analytics.Writers;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Backend.Application.Missions;
using Synaptix.Backend.Application.Rewards;
using Synaptix.Backend.Application.Guardians;
using Synaptix.Backend.Application.Matchmaking;
using Synaptix.Backend.Application.Notifications;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Backend.Application.Territory;
using Synaptix.Backend.Application.Social;
using Synaptix.Backend.Infrastructure;
using Synaptix.Backend.Infrastructure.Persistence.Extensions;
using Synaptix.Backend.Infrastructure.Persistence.HealthChecks;
using Synaptix.Backend.Infrastructure.Persistence.Startup;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Shared.Observability;
using Synaptix.Monitoring;
using Synaptix.Monitoring.Errors;
using Synaptix.Backend.Api.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting;
using Synaptix.Backend.Api.Contracts;
using StackExchange.Redis;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// Kestrel — dual port setup
//   Port 5000 → HTTP/1.1  (REST, SignalR, Swagger)
//   Port 5001 → HTTP/2    (gRPC — sidecar only, internal network)
// ─────────────────────────────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(5000, o => o.Protocols = HttpProtocols.Http1);
    kestrel.ListenAnyIP(5001, o => o.Protocols = HttpProtocols.Http2);
});

// Normalize JWT secret configuration across legacy and current key names.
var normalizedJwtSecret =
    builder.Configuration["JwtSettings:SecretKey"]
    ?? builder.Configuration["Jwt:Secret"]
    ?? builder.Configuration["Auth:JwtKey"]
    ?? builder.Configuration["JwtKey"];

if (!string.IsNullOrWhiteSpace(normalizedJwtSecret)
    && string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:SecretKey"]))
{
    builder.Configuration["JwtSettings:SecretKey"] = normalizedJwtSecret;
}

builder.Services
    .AddOptions<JwtSettings>()
    .BindConfiguration("JwtSettings")   // binds appsettings "JwtSettings" section
    .ValidateDataAnnotations()          // enforces [Required], [MinLength], [Range]
    .ValidateOnStart();                 // fails at startup, not first request

builder.Services
    .AddOptions<MissionRewardOptions>()
    .BindConfiguration("RewardReactor:Missions");

builder.Services
    .AddOptions<RewardReactorRuntimeOptions>()
    .BindConfiguration("RewardReactor");

builder.Services.AddSingleton<RewardReactorRuntimeContextService>();

// Reward Reactor services
builder.Services.AddSingleton<IRewardRng, CryptoRewardRng>();
builder.Services.AddScoped<RewardOutcomeService>();
builder.Services.AddScoped<RewardPolicyService>();
builder.Services.AddScoped<RewardClaimService>();

// Register IAuthService
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AdminNotificationDispatchJob>();

// Observability + Serilog + OTEL
builder.AddObservability();
builder.AddObservability("Synaptix.Backend.Api");

// JSON configuration
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ IMPROVED SWAGGER CONFIGURATION
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Format = "date"
    });

    c.MapType<TimeOnly>(() => new OpenApiSchema
    {
        Type = JsonSchemaType.String,
        Format = "time"
    });

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Synaptix API",
        Version = "v1",
        Description = "Platform API for Synaptix gameplay, progression, live competition, and player systems.",
        Contact = new OpenApiContact
        {
            Name = "Synaptix Development Team"
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

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
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

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
allowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (builder.Environment.IsDevelopment())
        {
            // Dynamic dev servers (Flutter, Dart, etc.) use ephemeral ports — allow any localhost origin.
            policy.SetIsOriginAllowed(origin =>
            {
                var host = new Uri(origin).Host;
                return host is "localhost" or "127.0.0.1" or "::1";
            });
        }
        else if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
    });
});

// Analytics
var analyticsEnabled = builder.Configuration.GetValue("Analytics:Enabled", false);
if (analyticsEnabled)
{
    builder.Services.RemoveAll<IAnalyticsEventWriter>();
    builder.Services.AddScoped<IAnalyticsEventWriter, PostgresAnalyticsEventWriter>();
}

// Infrastructure & Application
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// Commerce and Wallet modules (override Application registrations for store/economy services)
builder.Services.AddCommerce();
builder.Services.AddWallet();

// KMS typed clients for secure-channel payload encryption
builder.Services.AddKmsClient(builder.Configuration);

// Forward the caller's bearer token onto the KMS secure-session client so the
// main API can proxy the handshake (POST /api/v1/security/sessions/*) on behalf
// of the authenticated user. KMS binds the session subject to this token.
// No-op for internal/server-initiated calls (no inbound HttpContext), which
// keep using the service token only.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<UserBearerForwardingHandler>();
builder.Services.AddHttpClient(nameof(IKmsSessionClient))
    .AddHttpMessageHandler<UserBearerForwardingHandler>();

// Compliance service client (COPPA, CCPA, PCI audit hooks)
builder.Services.AddComplianceClient(builder.Configuration);

// Register Authentication Service
builder.Services.AddScoped<Synaptix.Backend.Application.Auth.IAuthService, Synaptix.Backend.Application.Auth.AuthService>();

// Register OTP and Email Services
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient<EmailService>();

// Validate JWT configuration at startup
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT secret configuration is required but not set. Configure JwtSettings:SecretKey (or legacy Jwt:Secret/Auth:JwtKey).");
}
if (jwtSecret.Length < 32)
{
    Console.WriteLine("⚠️ WARNING: JWT secret key should be at least 32 characters long for security.");
}

HashSet<string> knownWeakJwtKeys = new(StringComparer.OrdinalIgnoreCase)
{
    "YOUR-SUPER-SECRET-KEY-MINIMUM-32-CHARACTERS-LONG-FOR-SECURITY",
    "dev-only-change-me-dev-only-change-me-dev-only-change-me",
    "CHANGE-ME-your-secure-jwt-secret-key-at-least-32-characters-long",
};
if (!builder.Environment.IsDevelopment() && knownWeakJwtKeys.Contains(jwtSecret))
{
    throw new InvalidOperationException(
        "Refusing to start: JWT secret is a known placeholder value. " +
        "Set a real secret via the JwtSettings:SecretKey environment variable.");
}

// SignalR with Redis
var redis = builder.Configuration.GetConnectionString("redis")
            ?? builder.Configuration.GetConnectionString("cache")
            ?? builder.Configuration.GetConnectionString("Redis");

// gRPC — sidecar service (port 5001, HTTP/2)
builder.Services.AddGrpc(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddSingleton<ISidecarInferenceStore>(_ =>
{
    var path = builder.Configuration["SidecarInference:StorePath"]
        ?? Environment.GetEnvironmentVariable("SIDECAR_INFERENCE_STORE_PATH")
        ?? "/tmp/tycoon-sidecar/inference-store.jsonl";

    try
    {
        return new FileSidecarInferenceStore(path);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Falling back to InMemorySidecarInferenceStore because file-backed store init failed for '{path}': {ex.Message}");
        return new InMemorySidecarInferenceStore();
    }
});

var signalr = builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });
var useInMemoryDbForTesting = builder.Configuration.GetValue("Testing:UseInMemoryDb", false);
if (!useInMemoryDbForTesting && !string.IsNullOrWhiteSpace(redis))
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

if (useInMemoryDbForTesting)
{
    Console.WriteLine("⚠️ Testing:UseInMemoryDb=true detected. Disabling Hangfire.");
    hangfireEnabled = false;
}

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
               .UsePostgreSqlStorage(
                   opts => opts.UseNpgsqlConnection(hangfireDb),
                   new PostgreSqlStorageOptions
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

// ================================
// Monitoring Services
// ================================
Console.WriteLine("✅ Configuring monitoring services (Hangfire metrics, error tracking)");
builder.Services.AddMonitoring();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "JWT secret not configured. Set JwtSettings:SecretKey in environment variables or secrets manager.");

    jwtSettings.SecretKey = "dev-only-change-me-dev-only-change-me-dev-only-change-me";
    Console.WriteLine("⚠️ Using default JWT key — Development only!");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.SaveToken = true;
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudiences = new[] { "mobile-app", "admin-app", "crypto-service", jwtSettings.Audience },
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "sub",
            RoleClaimType = "role"
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
            },
            OnChallenge = context =>
            {
                if (context.Response.HasStarted)
                    return Task.CompletedTask;

                context.HandleResponse();
                return ApiResponses.Error(
                    StatusCodes.Status401Unauthorized,
                    "UNAUTHORIZED",
                    "Authentication required.").ExecuteAsync(context.HttpContext);
            },
            OnForbidden = context =>
            {
                if (context.Response.HasStarted)
                    return Task.CompletedTask;

                return ApiResponses.Error(
                    StatusCodes.Status403Forbidden,
                    "FORBIDDEN",
                    "Authorization requirements not satisfied.").ExecuteAsync(context.HttpContext);
            }
        };
    });

var dpKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/dp-keys";
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = Math.Max(1, builder.Configuration.GetValue("ForwardedHeaders:ForwardLimit", 1));

    var knownProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? [];
    var knownNetworks = builder.Configuration
        .GetSection("ForwardedHeaders:KnownNetworks")
        .Get<string[]>() ?? [];

    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();

    foreach (var proxy in knownProxies.Select(x => x.Trim()).Where(x => x.Length > 0))
    {
        if (IPAddress.TryParse(proxy, out var address))
            options.KnownProxies.Add(address);
    }

    foreach (var network in knownNetworks.Select(x => x.Trim()).Where(x => x.Length > 0))
    {
        if (TryParseCidr(network, out var parsed))
            options.KnownIPNetworks.Add(parsed);
    }

    if (options.KnownProxies.Count == 0 && options.KnownIPNetworks.Count == 0)
    {
        options.KnownProxies.Add(IPAddress.Loopback);
        options.KnownProxies.Add(IPAddress.IPv6Loopback);
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, _) =>
    {
        var path = context.HttpContext.Request.Path.Value ?? "unknown";
        AdminSecurityMetrics.RecordRateLimitReject(path);

        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        return new ValueTask(response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "RATE_LIMITED",
                message = "Rate limit exceeded.",
                details = new { path }
            }
        }));
    };

    options.AddPolicy("matches-submit", httpContext =>
    {
        var key = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.Identity.Name ?? "anonymous"
            : GetClientIpPartition(httpContext);

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
        var key = GetClientIpPartition(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("admin-auth-login", httpContext =>
    {
        var key = $"admin-auth-login:{GetClientIpPartition(httpContext)}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("admin-auth-refresh", httpContext =>
    {
        var key = $"admin-auth-refresh:{GetClientIpPartition(httpContext)}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("admin-notifications-send", httpContext =>
    {
        var subject = httpContext.User.FindFirstValue("sub")
            ?? GetClientIpPartition(httpContext)
            ?? "unknown";
        var key = $"admin-notifications-send:{subject}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddSingleton<IMatchmakingNotifier, SignalRMatchmakingNotifier>();
builder.Services.AddSingleton<IPartyMatchmakingNotifier, SignalRPartyMatchmakingNotifier>();
builder.Services.AddSingleton<IPresenceReader, SignalRPresenceReader>();
builder.Services.AddSingleton<IPresenceNotifier, SignalRPresenceNotifier>();
builder.Services.AddSingleton<ILeaderboardNotifier, SignalRLeaderboardNotifier>();
builder.Services.AddSingleton<IGameEventNotifier, SignalRGameEventNotifier>();
builder.Services.AddSingleton<IGuardianNotifier, SignalRGuardianNotifier>();
builder.Services.AddSingleton<ITerritoryNotifier, SignalRTerritoryNotifier>();
builder.Services.AddSingleton<IPlayerNotificationNotifier, SignalRPlayerNotificationNotifier>();
builder.Services.AddSingleton<IDirectMessageNotifier, SignalRDirectMessageNotifier>();

var redisRequiredForRealtime = !useInMemoryDbForTesting
    && (builder.Environment.IsStaging()
        || builder.Environment.IsProduction()
        || builder.Configuration.GetValue("Realtime:RequireRedis", false));
if (redisRequiredForRealtime && string.IsNullOrWhiteSpace(redis))
{
    throw new InvalidOperationException(
        "Redis is required for realtime presence in Staging/Production. Configure ConnectionStrings:redis or ConnectionStrings:cache.");
}

if (!useInMemoryDbForTesting && !string.IsNullOrWhiteSpace(redis))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redis)));
    builder.Services.AddSingleton<RedisConnectionRegistry>();
    builder.Services.AddSingleton<IConnectionRegistry>(sp => sp.GetRequiredService<RedisConnectionRegistry>());
    builder.Services.AddSingleton<RedisPresenceSessionManager>();
    builder.Services.AddSingleton<IPresenceSessionManager>(sp => sp.GetRequiredService<RedisPresenceSessionManager>());
    builder.Services.AddHostedService(sp => sp.GetRequiredService<RedisPresenceSessionManager>());
}
else
{
    builder.Services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
    builder.Services.AddSingleton<IPresenceSessionManager, PresenceSessionManager>();
}

builder.Services.AddSchemaGate(builder.Configuration, builder.Environment);

// Ensure IHttpClientFactory is always available for minimal-API endpoints that
// take it as a service dependency (avoids startup parameter-inference failures).
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.Configure<PersonalizationOptions>(
    builder.Configuration.GetSection("Personalization"));

builder.Services.AddPersonalizationSidecarClient(builder.Configuration);
builder.Services.Configure<StorePremiumOptions>(builder.Configuration.GetSection("StorePremium"));
builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection("PayPal"));
builder.Services.AddSingleton<IPayPalPaymentGateway, PayPalPaymentGateway>();
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<IStripePaymentGateway, StripePaymentGateway>();

builder.Services.AddAuthorization(opts => opts.AddAdminPolicies());

// ================================
// Sentry Error Tracking (Phase 3)
// ================================
builder.AddSentryMonitoring();

var app = builder.Build();

// Re-read after Build() so that test-host overrides (e.g. WebApplicationFactory
// AddInMemoryCollection) are visible — they are applied during builder.Build().
hangfireEnabled = app.Configuration.GetValue("Hangfire:Enabled", true)
    && !app.Configuration.GetValue("Testing:UseInMemoryDb", false);

app.UseForwardedHeaders();
// ✅ CORRECT MIDDLEWARE ORDER
app.UseRouting();

// ✅ Sentry Error Tracking (Phase 3)
app.UseSentryMonitoring();

// ✅ Error Rate Tracking Middleware (must be early in pipeline)
app.UseErrorTracking();

// ✅ Show detailed errors in development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");
app.UseMiddleware<AdminOpsKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.UseRateLimiter();
app.UseSecureChannel();

// ✅ SWAGGER CONFIGURATION
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Synaptix API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Synaptix API Documentation";
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
        RecurringJob.AddOrUpdate<Synaptix.Backend.Application.Leaderboards.LeaderboardRecalculationJob>(
            "daily-leaderboard-recalc",
            job => job.Run(),
            "0 5 * * *"
        );

        RecurringJob.AddOrUpdate<AdminNotificationDispatchJob>(
            "admin-notification-dispatch",
            job => job.Run(default),
            "*/1 * * * *"
        );

        RecurringJob.AddOrUpdate<GameEventSchedulerJob>(
            "game-event-scheduler",
            job => job.RunAsync(CancellationToken.None),
            "*/1 * * * *"
        );

        RecurringJob.AddOrUpdate<GuardianAssignmentJob>(
            "guardian-assignment",
            job => job.RunAsync(CancellationToken.None),
            "0 2 * * *"
        );

        RecurringJob.AddOrUpdate<Synaptix.Backend.Application.Privacy.PrivacyRequestFulfillmentJob>(
            "privacy-request-fulfillment",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *"
        );

        RecurringJob.AddOrUpdate<Synaptix.Backend.Application.Entitlements.EntitlementExpiryJob>(
            "entitlement-expiry",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *"
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
    name = "Synaptix.Backend.Api",
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

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
}).AllowAnonymous();

app.MapGet("/healthz", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
})).AllowAnonymous().WithTags("Health");

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

// WebSocket endpoint — handles presence protocol
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connection required");
        return;
    }

    var presenceMgr = context.RequestServices.GetRequiredService<IPresenceSessionManager>();
    var registry = context.RequestServices.GetRequiredService<IConnectionRegistry>();
    var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();

    // Presence is bound to the authenticated principal — never the client-supplied
    // query playerId — to prevent presence spoofing. The token `sub` is the playerId
    // (Arcade/Study/Matches endpoints treat the authenticated sub as the playerId).
    var subject = context.User.FindFirstValue("sub")
                  ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(subject, out var playerId) || playerId == Guid.Empty)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Authentication required");
        return;
    }
    var playerIdStr = playerId.ToString();

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    // Send hello
    var helloBytes = Encoding.UTF8.GetBytes("{\"op\":\"hello\",\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}");
    await webSocket.SendAsync(new ArraySegment<byte>(helloBytes), WebSocketMessageType.Text, true, CancellationToken.None);

    var connectedFriendIds = new List<Guid>();

    if (playerId != Guid.Empty)
    {
        presenceMgr.Register(playerId, webSocket);
        registry.Add(playerId, playerIdStr); // use playerIdStr as connectionId for registry compat

        // Load friend IDs and send bulk presence snapshot
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
            connectedFriendIds = await db.FriendEdges
                .Where(e => e.PlayerId == playerId)
                .Select(e => e.FriendPlayerId)
                .ToListAsync(CancellationToken.None);
        }

        // Send initial bulk snapshot of which friends are online
        var onlineFriends = presenceMgr.GetConnectedPlayerIds()
            .Where(id => connectedFriendIds.Contains(id))
            .Select(id =>
            {
                var act = presenceMgr.GetActivity(id);
                return new
                {
                    userId = id.ToString(),
                    status = act?.Status ?? "online",
                    activity = act?.Activity,
                    gameActivity = act?.GameActivity,
                    lastSeen = DateTimeOffset.UtcNow
                };
            })
            .ToList();

        var bulkPayload = JsonSerializer.Serialize(new
        {
            op = "presence.bulk",
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            data = new { presences = onlineFriends }
        });
        await webSocket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(bulkPayload)),
            WebSocketMessageType.Text, true, CancellationToken.None);

        // Notify each connected friend that this player is now online
        var onlineNotify = JsonSerializer.Serialize(new
        {
            op = "presence.update",
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            data = new { userId = playerId.ToString(), status = "online", lastSeen = DateTimeOffset.UtcNow }
        });
        foreach (var friendId in connectedFriendIds)
            await presenceMgr.SendToPlayerAsync(friendId, onlineNotify, CancellationToken.None);
    }

    await HandleWebSocket(webSocket, playerId, connectedFriendIds, presenceMgr, registry, scopeFactory);

    // Cleanup on disconnect
    if (playerId != Guid.Empty)
    {
        presenceMgr.Unregister(playerId);
        registry.Remove(playerId, playerIdStr);

        // Notify friends this player went offline
        List<Guid> friendIds2;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
            friendIds2 = await db.FriendEdges
                .Where(e => e.PlayerId == playerId)
                .Select(e => e.FriendPlayerId)
                .ToListAsync(CancellationToken.None);
        }

        var offlineMsg = JsonSerializer.Serialize(new
        {
            op = "presence.update",
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            data = new { userId = playerId.ToString(), status = "offline", lastSeen = DateTimeOffset.UtcNow }
        });
        foreach (var friendId in friendIds2)
            await presenceMgr.SendToPlayerAsync(friendId, offlineMsg, CancellationToken.None);
    }
});

// app.MapControllers();

// SignalR hubs — gated by realtime_multiplayer_enabled feature flag
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/ws"))
    {
        var flags = ctx.RequestServices.GetRequiredService<FeatureFlagService>();
        if (!await flags.IsEnabledAsync("realtime_multiplayer_enabled", ctx.RequestAborted))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { error = new { code = "FeatureDisabled", message = "This feature is not available in the current release.", details = new { } } });
            return;
        }
    }
    await next(ctx);
});
app.MapHub<MatchHub>("/ws/match");
app.MapHub<PresenceHub>("/ws/presence");
app.MapHub<NotificationHub>("/ws/notify");
app.MapHub<LeaderboardHub>("/ws/leaderboard");
app.MapHub<MatchmakingHub>("/ws/matchmaking");

// gRPC — sidecar service (internal; port 5001 via Kestrel dual-port config)
app.MapGrpcService<SidecarGrpcService>();
// gRPC — mobile match service (Flutter clients; port 5001)
app.MapGrpcService<MobileMatchGrpcService>();

// Feature endpoints
// Route ownership note:
// - /questions/* = gameplay-safe question retrieval and grading
// - /modules/* = learning and mastery flows
// - /quiz/* = solo quiz completion and reward grant (POST /quiz/complete)
//
// All public client-facing endpoints are versioned under /api/v1 (single
// source of truth for the mobile contract). Infra surfaces (/health, /healthz,
// /ws, /swagger, gRPC, /hangfire, /metrics) and the operator /admin surface
// stay un-prefixed by design.

// ================================
// Monitoring Endpoints (Infra Surface)
// ================================
app.MapMonitoringEndpoints();

var v1 = app.MapGroup("/api/v1");

AppConfigEndpoints.Map(v1);
AnalyticsEndpoints.Map(v1);
AuthEndpoints.Map(v1);
SecuritySessionsEndpoints.Map(v1);
UsersEndpoints.Map(v1);
UserFriendsEndpoints.Map(v1);
PlayerPreferencesEndpoints.Map(v1);
PlayersEndpoints.Map(v1);
MatchesEndpoints.Map(v1);
QuizEndpoints.Map(v1);
MatchmakingEndpoints.Map(v1);
MissionsEndpoints.Map(v1);
LeaderboardsEndpoints.Map(v1);
ReferralsEndpoints.Map(v1);
QrEndpoints.Map(v1);
SkillsEndpoints.Map(v1);
PowerupsEndpoints.Map(v1);
SeasonsEndpoints.Map(v1);
FriendsEndpoints.Map(v1);
PlayerNotificationsEndpoints.Map(v1);
MessagesEndpoints.Map(v1);
PartyEndpoints.Map(v1);
RankedLeaderboardsEndpoints.Map(v1);
ArcadeLeaderboardEndpoints.Map(v1);
SeasonRewardsEndpoints.Map(v1);
QuestionsEndpoints.Map(v1);
LearningModulesEndpoints.Map(v1);
StudySetsEndpoints.Map(v1);
StudySessionsEndpoints.Map(v1);
VoteEndpoints.Map(v1);
StoreEndpoints.Map(v1);
ArcadeSpinEndpoints.Map(v1);
ReactorEndpoints.Map(v1);
ActiveEventsEndpoints.Map(v1);
UserRewardsEndpoints.Map(v1);
RewardsEndpoints.Map(v1);
AccountRewardsEndpoints.Map(v1);
AccountMigrationEndpoints.Map(v1);
ProgressionEndpoints.Map(v1);
SpinsEndpoints.Map(v1);
AvatarEndpoints.Map(v1);
AssetManifestEndpoints.Map(v1);
PersonalizationEndpoints.Map(v1);
CoachEndpoints.Map(v1);
ExperimentEndpoints.Map(v1);
CryptoEconomyEndpoints.Map(v1);
MlScoringEndpoints.Map(v1);
GameEventsEndpoints.Map(v1);
GameEventStatsEndpoints.Map(v1);
GameEventStatsEndpoints.MapTerritory(v1);
GuardiansEndpoints.Map(v1);
TerritoryEndpoints.Map(v1);

// Mobile endpoints (separate route surface for mobile-specific contracts/workflows)
var mobile = v1.MapGroup("/mobile").WithTags("Mobile");
MobileMatchesEndpoints.Map(mobile);
MobilePlayersEndpoints.Map(mobile);
MobileLeaderboardsEndpoints.Map(mobile);
MobileSeasonsEndpoints.Map(mobile);
MobileEconomyEndpoints.Map(mobile);

// Admin endpoints
var adminAuth = app.MapGroup("/admin").RequireAdminOpsKey();
AdminAuthEndpoints.Map(adminAuth);

var admin = app.MapGroup("/admin")
    .RequireAdminOpsKey()
    .RequireAdminRoleClaims();

AdminGameEventsEndpoints.Map(admin);
AdminQuestionsEndpoints.Map(admin);
AdminUsersEndpoints.Map(admin);
AdminPlayerLookupEndpoints.Map(admin);
AdminEventQueueEndpoints.Map(admin);
AdminNotificationsEndpoints.Map(admin);
AdminConfigEndpoints.Map(admin);
AdminMediaEndpoints.Map(admin);
AdminMongoEndpoints.Map(admin);
AdminAnalyticsEndpoints.Map(admin);
AdminAuditEndpoints.Map(admin);
AdminEconomyEndpoints.Map(admin);
AdminPlayerTransactionEndpoints.Map(admin);
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
AdminSeasonPointsEndpoints.Map(admin);
AdminEmailAclEndpoints.Map(admin);
AdminStoreEndpoints.Map(admin);
AdminStorageEndpoints.Map(admin);
AdminSetupEndpoints.Map(admin);
AdminLearningModulesEndpoints.Map(admin);
AdminPersonalizationEndpoints.Map(admin);
AdminExperimentEndpoints.Map(admin);
AdminPrivacyEndpoints.Map(admin);

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

async Task HandleWebSocket(
    WebSocket webSocket,
    Guid playerId,
    List<Guid>? friendIds,
    IPresenceSessionManager presenceMgr,
    IConnectionRegistry registry,
    IServiceScopeFactory scopeFactory)
{
    var buffer = new byte[1024 * 16];

    while (webSocket.State == WebSocketState.Open)
    {
        WebSocketReceiveResult result;
        try
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
        catch (WebSocketException)
        {
            break;
        }

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            break;
        }

        if (result.MessageType != WebSocketMessageType.Text || playerId == Guid.Empty)
            continue;

        var msgText = Encoding.UTF8.GetString(buffer, 0, result.Count);

        JsonDocument doc;
        try { doc = JsonDocument.Parse(msgText); }
        catch { continue; }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("op", out var opEl))
                continue;

            var op = opEl.GetString();

            switch (op)
            {
                case "presence.subscribe":
                {
                    // Return a bulk snapshot for the requested userIds
                    List<string> requestedIds = new();
                    if (doc.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("userIds", out var userIdsEl))
                    {
                        foreach (var el in userIdsEl.EnumerateArray())
                        {
                            var idStr = el.GetString();
                            if (idStr is not null) requestedIds.Add(idStr);
                        }
                    }

                    var allowedIds = new HashSet<Guid>((friendIds ?? Enumerable.Empty<Guid>()).Append(playerId));

                    var presences = requestedIds
                        .Where(id => Guid.TryParse(id, out var parsedId) && allowedIds.Contains(parsedId))
                        .Select(id =>
                        {
                            var pid = Guid.Parse(id);
                            var act = presenceMgr.GetActivity(pid);
                            var isOnline = presenceMgr.GetConnectedPlayerIds().Contains(pid);
                            return new
                            {
                                userId = id,
                                status = isOnline ? (act?.Status ?? "online") : "offline",
                                activity = act?.Activity,
                                gameActivity = act?.GameActivity,
                                lastSeen = DateTimeOffset.UtcNow
                            };
                        })
                        .ToList();

                    var bulkResp = JsonSerializer.Serialize(new
                    {
                        op = "presence.bulk",
                        ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        data = new { presences }
                    });
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(bulkResp)),
                        WebSocketMessageType.Text, true, CancellationToken.None);
                    break;
                }

                case "presence.update":
                {
                    // Update this player's activity and broadcast to friends
                    if (!doc.RootElement.TryGetProperty("data", out var data))
                        break;

                    var status = data.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "online";
                    var activity = data.TryGetProperty("activity", out var actEl) ? actEl.GetString() : null;
                    JsonElement? gameActivity = data.TryGetProperty("gameActivity", out var gaEl) ? gaEl : null;

                    presenceMgr.SetActivity(playerId, new PresenceActivity(
                        status ?? "online",
                        activity,
                        gameActivity));

                    // Broadcast to friends who are connected
                    if (friendIds is null)
                    {
                        using var scope = scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
                        friendIds = await db.FriendEdges
                            .Where(e => e.PlayerId == playerId)
                            .Select(e => e.FriendPlayerId)
                            .ToListAsync(CancellationToken.None);
                    }

                    var updateMsg = JsonSerializer.Serialize(new
                    {
                        op = "presence.update",
                        ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        data = new
                        {
                            userId = playerId.ToString(),
                            status = status ?? "online",
                            activity,
                            gameActivity,
                            lastSeen = DateTimeOffset.UtcNow
                        }
                    });

                    foreach (var friendId in friendIds)
                        await presenceMgr.SendToPlayerAsync(friendId, updateMsg, CancellationToken.None);

                    break;
                }

                case "presence.unsubscribe":
                    // No-op: server routes via friend graph, not per-subscription
                    break;
            }
        }
    }
}

static string GetClientIpPartition(HttpContext context)
{
    var address = context.Connection.RemoteIpAddress;
    if (address is null)
        return "ip:unknown";

    if (address.IsIPv4MappedToIPv6)
        address = address.MapToIPv4();

    return $"ip:{address}";
}

static bool TryParseCidr(string value, out System.Net.IPNetwork network)
{
    try
    {
        network = System.Net.IPNetwork.Parse(value);
        return true;
    }
    catch (FormatException)
    {
        network = System.Net.IPNetwork.Parse("127.0.0.1/32");
        return false;
    }
}
public partial class Program { }

public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User?.Identity?.IsAuthenticated ?? false;
    }
}
