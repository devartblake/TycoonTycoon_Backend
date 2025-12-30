using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Tycoon.Backend.Api.Security
{
    public static class AdminPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOpsPolicy = "AdminOps";

        public static void AddAdminPolicies(this AuthorizationOptions options)
        {
            options.AddPolicy(AdminOnly, p =>
            {
                // Require authenticated + admin role claim
                p.RequireAuthenticatedUser();
                p.RequireRole("Admin");
            });
        }

        public static IServiceCollection AddAdminPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(o =>
            {
                o.AddPolicy(AdminOpsPolicy, p =>
                {
                    // Minimal, adaptable:
                    // - if you use JWT: require role/claim
                    // - if you use internal auth: swap to your scheme
                    p.RequireAuthenticatedUser();

                    // Example: role-based
                    p.RequireRole("admin");

                    // Example alternative:
                    // p.RequireClaim("scope", "admin:ops");
                });
            });

            return services;
        }
    }
}
