using Microsoft.AspNetCore.Authentication.Cookies;
using Tycoon.OperatorDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
