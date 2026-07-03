using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Synaptix.Compliance.Api.Features.AgeVerification;
using Synaptix.Compliance.Api.Features.Consent;
using Synaptix.Compliance.Api.Features.Internal;
using Synaptix.Compliance.Api.Features.ParentalConsent;
using Synaptix.Compliance.Api.Features.PrivacyRequests;
using Synaptix.Compliance.Application;
using Synaptix.Compliance.Infrastructure;
using Synaptix.Compliance.Infrastructure.Persistence;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext()
           .WriteTo.Console());

    builder.AddServiceDefaults();

    // ── Authentication ─────────────────────────────────────────────────────────
    var jwtAuthority = builder.Configuration["Jwt:Authority"];
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "compliance-api";
    var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
                    ?? builder.Configuration["Jwt:Secret"];

    if (string.IsNullOrWhiteSpace(jwtSecretKey)
        && string.IsNullOrWhiteSpace(jwtAuthority))
    {
        throw new InvalidOperationException(
            "Refusing to start: JWT configuration missing. " +
            "Set Jwt:Authority (OIDC) or JwtSettings:SecretKey (symmetric) in every environment.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            if (!string.IsNullOrWhiteSpace(jwtAuthority))
            {
                opt.Authority = jwtAuthority;
                opt.Audience = jwtAudience;
            }
            else if (!string.IsNullOrWhiteSpace(jwtSecretKey))
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecretKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            }
            else
            {
                // Unreachable: startup guard above refuses to start without a key/authority.
                // No insecure "accept any token" fallback is permitted in any environment.
                throw new InvalidOperationException(
                    "JWT configuration missing: no Authority or signing key.");
            }
        });

    builder.Services.AddAuthorization();

    // ── Application + Infrastructure ───────────────────────────────────────────
    builder.Services.AddComplianceApplication();
    builder.Services.AddComplianceInfrastructure(builder.Configuration);

    // ── OpenAPI ────────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── Health checks ──────────────────────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<ComplianceDb>(tags: ["live"]);

    var app = builder.Build();

    // ── Auto-migrate on startup (optional) ───────────────────────────────────────
    var autoMigrate = builder.Configuration.GetValue("ComplianceApi:AutoMigrate", builder.Environment.IsDevelopment());
    if (autoMigrate)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ComplianceDb>();
        await db.Database.MigrateAsync();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    // ── Endpoints ──────────────────────────────────────────────────────────────
    AgeVerificationEndpoints.Map(app);
    ParentalConsentEndpoints.Map(app);
    PrivacyRequestEndpoints.Map(app);
    ConsentEndpoints.Map(app);
    InternalEndpoints.Map(app);

    app.MapGet("/", () => new { service = "Synaptix.Compliance", version = "1.0" });

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Compliance API terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
