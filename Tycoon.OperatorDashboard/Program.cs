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
builder.Services.AddScoped<AdminAuthService>();

// Typed HttpClient that forwards the admin JWT + ops key to tycoon-api.
// When running under Aspire, the service name resolves via service discovery.
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    var backendUrl = builder.Configuration["services:tycoon-api:https:0"]
                  ?? builder.Configuration["services:tycoon-api:http:0"]
                  ?? "http://localhost:5000";
    client.BaseAddress = new Uri(backendUrl);

    // Attach the ops key as a default header so every request — including the
    // initial login — passes the AdminOpsKeyMiddleware / endpoint filter gate.
    var opsKey = builder.Configuration["AdminOps:Key"] ?? string.Empty;
    if (!string.IsNullOrEmpty(opsKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-Ops-Key", opsKey);
});

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
