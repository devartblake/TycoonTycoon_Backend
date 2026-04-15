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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Tycoon.Backend.Api.Features.AdminAnalytics;
using Tycoon.Backend.Api.Features.AdminAntiCheat;
using Tycoon.Backend.Api.Features.AdminAuth;
using Tycoon.Backend.Api.Features.AdminEconomy;
using Tycoon.Backend.Api.Features.AdminPlayerTransactions;
using Tycoon.Backend.Api.Features.AdminEventQueue;
using Tycoon.Backend.Api.Features.AdminMatches;
using Tycoon.Backend.Api.Features.AdminMedia;
using Tycoon.Backend.Api.Features.AdminModeration;
using Tycoon.Backend.Api.Features.AdminNotifications;
using Tycoon.Backend.Api.Features.AdminConfig;
using Tycoon.Backend.Api.Features.AdminEmailAcl;
using Tycoon.Backend.Api.Features.AdminPowerups;
using Tycoon.Backend.Api.Features.AdminQuestions;
using Tycoon.Backend.Api.Features.AdminStore;
using Tycoon.Backend.Api.Features.AdminSeasons;
using Tycoon.Backend.Api.Features.AdminSkills;
using Tycoon.Backend.Api.Features.AdminUsers;
using Tycoon.Backend.Api.Features.Analytics;
using Tycoon.Backend.Api.Features.Auth;
using Tycoon.Backend.Api.Features.Friends;
using Tycoon.Backend.Api.Features.Leaderboards;
using Tycoon.Backend.Api.Features.Matches;
using Tycoon.Backend.Api.Features.Matchmaking;
using Tycoon.Backend.Api.Features.Missions;
using Tycoon.Backend.Api.Features.Ml;
using Tycoon.Backend.Api.Features.Mobile.Matches;
using Tycoon.Backend.Api.Features.Mobile.Seasons;
using Tycoon.Backend.Api.Features.Mobile.Players;
using Tycoon.Backend.Api.Features.Mobile.Leaderboards;
using Tycoon.Backend.Api.Features.Mobile.Economy;
using Tycoon.Backend.Api.Features.Party;
using Tycoon.Backend.Api.Features.Players;
using Tycoon.Backend.Api.Features.Powerups;
using Tycoon.Backend.Api.Features.Qr;
using Tycoon.Backend.Api.Features.Questions;
using Tycoon.Backend.Api.Features.Crypto;
using Tycoon.Backend.Api.Features.Store;
using Tycoon.Backend.Api.Features.Referrals;
using Tycoon.Backend.Api.Features.GameEvents;
using Tycoon.Backend.Api.Features.Guardians;
using Tycoon.Backend.Api.Features.Territory;
using Tycoon.Backend.Api.Features.Votes;
using Tycoon.Backend.Api.Features.Seasons;
using Tycoon.Backend.Api.Features.Skills;
using Tycoon.Backend.Api.Features.Users;
using Tycoon.Backend.Api.Middleware;
using Tycoon.Backend.Api.Observability;
using Tycoon.Backend.Api.Payments.PayPal;
using Tycoon.Backend.Api.Payments.Stripe;
using Tycoon.Backend.Api.Realtime;
using Tycoon.Backend.Api.Security;
using Tycoon.Backend.Application;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Writers;
using Tycoon.Backend.Application.Auth;
using Tycoon.Backend.Application.GameEvents;
using Tycoon.Backend.Application.Guardians;
using Tycoon.Backend.Application.Matchmaking;
using Tycoon.Backend.Application.Notifications;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Backend.Application.Territory;
using Tycoon.Backend.Application.Social;
using Tycoon.Backend.Infrastructure;
using Tycoon.Backend.Infrastructure.Persistence.Extensions;
using Tycoon.Backend.Infrastructure.Persistence.HealthChecks;
using Tycoon.Backend.Infrastructure.Persistence.Startup;
using Tycoon.Shared.Contracts.Dtos;
using Tycoon.Shared.Observability;
using Tycoon.Backend.Api.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting;

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

// Register IAuthService
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AdminNotificationDispatchJob>();

// Observability + Serilog + OTEL
builder.AddObservability();
builder.AddObservability("Tycoon.Backend.Api");

// JSON configuration
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// builder.Services.AddControllers();
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

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (allowedOrigins.Length > 0)
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
builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();

// Register Authentication Service
builder.Services.AddScoped<Tycoon.Backend.Application.Auth.IAuthService, Tycoon.Backend.Application.Auth.AuthService>();

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
var useInMemoryDbForTesting = builder.Configuration.GetValue("Testing:UseInMemoryDb", false);

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
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    jwtSettings.SecretKey = "dev-only-change-me-dev-only-change-me-dev-only-change-me";
    Console.WriteLine("⚠️ Using default JWT key for development!");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.SaveToken = true;
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudiences = new[] { "mobile-app", "admin-app", jwtSettings.Audience },
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
            }
        };
    });

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("var/dpkeys"));

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

    options.AddPolicy("admin-auth-login", httpContext =>
    {
        var key = $"admin-auth-login:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
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
        var key = $"admin-auth-refresh:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
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
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
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
builder.Services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
builder.Services.AddSingleton<IPresenceReader, SignalRPresenceReader>();
builder.Services.AddSingleton<IPresenceSessionManager, PresenceSessionManager>();
builder.Services.AddSingleton<IGameEventNotifier, SignalRGameEventNotifier>();
builder.Services.AddSingleton<IGuardianNotifier, SignalRGuardianNotifier>();
builder.Services.AddSingleton<ITerritoryNotifier, SignalRTerritoryNotifier>();

builder.Services.AddSchemaGate(builder.Configuration, builder.Environment);

// Ensure IHttpClientFactory is always available for minimal-API endpoints that
// take it as a service dependency (avoids startup parameter-inference failures).
builder.Services.AddHttpClient();
builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection("PayPal"));
builder.Services.AddSingleton<IPayPalPaymentGateway, PayPalPaymentGateway>();
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<IStripePaymentGateway, StripePaymentGateway>();

builder.Services.AddAuthorization(opts => opts.AddAdminPolicies());

var app = builder.Build();

// Re-read after Build() so that test-host overrides (e.g. WebApplicationFactory
// AddInMemoryCollection) are visible — they are applied during builder.Build().
hangfireEnabled = app.Configuration.GetValue("Hangfire:Enabled", true)
    && !app.Configuration.GetValue("Testing:UseInMemoryDb", false);

// ✅ CORRECT MIDDLEWARE ORDER
app.UseRouting();

// ✅ Show detailed errors in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
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
        RecurringJob.AddOrUpdate<Tycoon.Backend.Application.Leaderboards.LeaderboardRecalculationJob>(
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

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

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

    // Extract playerId from query string (same pattern as MatchHub)
    var playerIdStr = context.Request.Query["playerId"].ToString();
    Guid.TryParse(playerIdStr, out var playerId);

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

// SignalR hubs
app.MapHub<MatchHub>("/ws/match");
app.MapHub<PresenceHub>("/ws/presence");
app.MapHub<NotificationHub>("/ws/notify");

// gRPC — sidecar service (internal; port 5001 via Kestrel dual-port config)
app.MapGrpcService<SidecarGrpcService>();
// gRPC — mobile match service (Flutter clients; port 5001)
app.MapGrpcService<MobileMatchGrpcService>();

// Feature endpoints
AnalyticsEndpoints.Map(app);
AuthEndpoints.Map(app);
UsersEndpoints.Map(app);
UserFriendsEndpoints.Map(app);
PlayerPreferencesEndpoints.Map(app);
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
QuestionsEndpoints.Map(app);
VoteEndpoints.Map(app);
StoreEndpoints.Map(app);
CryptoEconomyEndpoints.Map(app);
MlScoringEndpoints.Map(app);
GameEventsEndpoints.Map(app);
GameEventStatsEndpoints.Map(app);
GameEventStatsEndpoints.MapTerritory(app);
GuardiansEndpoints.Map(app);
TerritoryEndpoints.Map(app);

// Mobile endpoints (separate route surface for mobile-specific contracts/workflows)
var mobile = app.MapGroup("/mobile").WithTags("Mobile").WithOpenApi();
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
AdminEventQueueEndpoints.Map(admin);
AdminNotificationsEndpoints.Map(admin);
AdminConfigEndpoints.Map(admin);
AdminMediaEndpoints.Map(admin);
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
public partial class Program { }

public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User?.Identity?.IsAuthenticated ?? false;
    }
}
