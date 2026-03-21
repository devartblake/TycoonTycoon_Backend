using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Tycoon.OperatorDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Persist Data Protection keys so antiforgery tokens and auth cookies survive container restarts.
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/dp-keys";
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<TokenStore>();
// BearerTokenStore is scoped per Blazor circuit so every AdminApiClient instance
// in the same circuit shares the same token (fixes the transient-instance token bug).
builder.Services.AddScoped<BearerTokenStore>();
builder.Services.AddScoped<AdminAuthService>();
// Transient handler — DI creates one per HttpClient request; reads from scoped BearerTokenStore.
builder.Services.AddTransient<BearerTokenHandler>();

// Typed HttpClient that forwards the admin JWT + ops key to tycoon-api.
// Aspire service discovery resolves "http://tycoon-api" via services__tycoon-api__http__0.
// When running standalone (no Aspire/compose), set ApiBaseUrl in appsettings or the
// environment to point directly at the running API (e.g. "http://localhost:5100").
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://tycoon-api";
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);

    // Attach the ops key as a default header so every request — including the
    // initial login — passes the AdminOpsKeyMiddleware / endpoint filter gate.
    var opsKey = builder.Configuration["AdminOps:Key"] ?? string.Empty;
    if (!string.IsNullOrEmpty(opsKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-Ops-Key", opsKey);
})
.AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
