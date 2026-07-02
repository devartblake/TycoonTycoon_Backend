using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Shared.Core.Extensions.ServiceCollectionsExtensions;

namespace Synaptix.Shared.Web.Extensions;

// https://learn.microsoft.com/en-us/aspnet/core/security/cors
public static class Extensions
{
    public static WebApplicationBuilder AddCustomCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedOptions<CorsOptions>();

        builder.Services.AddCors();

        return builder;
    }

    public static WebApplication UseCustomCors(this WebApplication app)
    {
        var options = app.Services.GetService<CorsOptions>();
        app.UseCors(p =>
        {
            if (options?.AllowedUrls is { } && options.AllowedUrls.Any())
            {
                p.WithOrigins(options.AllowedUrls.ToArray());
            }
            // Deny by default: with no configured origins, add none rather than
            // falling back to AllowAnyOrigin() (which would open the API to any site).

            p.AllowAnyMethod();
            p.AllowAnyHeader();
        });
        return app;
    }
}
