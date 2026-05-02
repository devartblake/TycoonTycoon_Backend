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

    if (!builder.Environment.IsDevelopment()
        && string.IsNullOrWhiteSpace(jwtSecretKey)
        && string.IsNullOrWhiteSpace(jwtAuthority))
    {
        throw new InvalidOperationException(
            "Refusing to start: JWT configuration missing in non-Development environment. " +
            "Set Jwt:Authority (OIDC) or JwtSettings:SecretKey (symmetric).");
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
                // Development-only: accept any token (insecure — dev only)
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    SignatureValidator = (token, _) =>
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        return handler.ReadJwtToken(token);
                    }
                };
            }
        });

    builder.Services.AddAuthorization();

    // ── Application + Infrastructure ─────────────────────────────────────────
    builder.Services.AddKmsApplication();
    builder.Services.AddKmsInfrastructure(builder.Configuration);

    // ── OpenAPI ───────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    // ── Endpoints ─────────────────────────────────────────────────────────────
    SessionEndpoints.Map(app);
    PayloadEndpoints.Map(app);
    KeyEndpoints.Map(app);
    InternalEndpoints.Map(app);

    app.MapDefaultEndpoints();
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
