using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
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
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<TokenStore>();
builder.Services.AddScoped<BearerTokenStore>();
builder.Services.AddScoped<AdminAuthService>();

// Named HttpClient — ops key set once on the factory-managed HttpClient.
// Aspire service discovery resolves "http://tycoon-api" via services__tycoon-api__http__0.
// Standalone: set ApiBaseUrl in appsettings or environment (e.g. "http://localhost:5100").
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://tycoon-api";
builder.Services.AddHttpClient("tycoon-api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

    // AdminApiClient is scoped — one shared instance per Blazor Server circuit.
    // It takes IHttpClientFactory in its constructor and creates the named "tycoon-api" client.
    // Scoped ensures Dashboard.razor and AdminAuthService share the same instance per circuit,
    // so SetToken() and all subsequent API calls use the same HttpClient headers.
    builder.Services.AddScoped<AdminApiClient>();

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

    
