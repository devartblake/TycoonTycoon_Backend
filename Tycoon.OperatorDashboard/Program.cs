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
// Use the logical Aspire service name so service discovery resolves it via
// the services__tycoon-api__http__0 env var (set in docker/compose.yml).
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = new Uri("http://tycoon-api");

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
