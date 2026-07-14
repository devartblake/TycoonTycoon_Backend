using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Synaptix.Security.Kms.Api.Features.Internal;
using Synaptix.Security.Kms.Api.Features.Keys;
using Synaptix.Security.Kms.Api.Features.Payload;
using Synaptix.Security.Kms.Api.Features.Sessions;
using Synaptix.Security.Kms.Application;
using Synaptix.Security.Kms.Infrastructure;
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

    // ── Authentication ────────────────────────────────────────────────────────
    var jwtAuthority = builder.Configuration["Jwt:Authority"];
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "kms-api";
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

    // ── Application + Infrastructure ─────────────────────────────────────────
    builder.Services.AddKmsApplication(builder.Configuration);
    builder.Services.AddKmsInfrastructure(builder.Configuration);

    // ── OpenAPI ───────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();

    // ── Health — always available regardless of environment ───────────────────
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });

    // ── OpenAPI — dev only ────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    SessionEndpoints.Map(app);
    PayloadEndpoints.Map(app);
    KeyEndpoints.Map(app);
    InternalEndpoints.Map(app);

    app.MapGet("/", () => new { service = "Synaptix.Security.Kms", version = "1.0" });

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "KMS API terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
