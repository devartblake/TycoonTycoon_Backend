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
builder.Services.AddScoped<BearerTokenStore>();
builder.Services.AddScoped<AdminAuthService>();

// Named HttpClient — ops key set once on the factory-managed HttpClient.
// Aspire service discovery resolves "http://tycoon-api" via services__tycoon-api__http__0.
// Standalone: set ApiBaseUrl in appsettings or environment (e.g. "http://localhost:5100").
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://tycoon-api";
builder.Services.AddHttpClient("tycoon-api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    var opsKey = builder.Configuration["AdminOps:Key"] ?? string.Empty;
    if (!string.IsNullOrEmpty(opsKey))
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-Ops-Key", opsKey);
});

// AdminApiClient is scoped — one shared instance per Blazor Server circuit.
// This is the critical fix: AddHttpClient<T> registers T as transient, meaning
// Dashboard.razor and AdminAuthService receive different instances. SetToken() on
// AdminAuthService's instance never affected Dashboard.razor's instance → 401s.
// With a scoped registration, both resolve the same object from the circuit scope,
// so SetToken() and all subsequent Api.* calls use the same HttpClient headers.
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var config  = sp.GetRequiredService<IConfiguration>();
    return new AdminApiClient(factory.CreateClient("tycoon-api"), config);
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
