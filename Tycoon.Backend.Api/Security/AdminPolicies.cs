using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Tycoon.Backend.Api.Security
{
    public static class AdminPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOpsPolicy = "AdminOps";
        public const string AdminNotificationsWritePolicy = "AdminNotificationsWrite";

        public static void AddAdminPolicies(this AuthorizationOptions options)
        {
            options.AddPolicy(AdminOnly, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("Admin");
            });

            options.AddPolicy(AdminOpsPolicy, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("admin");
                p.RequireClaim("aud", "admin-app");
                p.RequireAssertion(ctx => HasScope(ctx.User, "users:read"));
            });

            options.AddPolicy(AdminNotificationsWritePolicy, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("admin");
                p.RequireClaim("aud", "admin-app");
                p.RequireAssertion(ctx => HasScope(ctx.User, "notifications:write"));
            });
        }

        public static IServiceCollection AddAdminPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(o => o.AddAdminPolicies());

            return services;
        }

        private static bool HasScope(System.Security.Claims.ClaimsPrincipal user, string requiredScope)
        {
            var scopeClaim = user.FindFirst("scope")?.Value;
            if (string.IsNullOrWhiteSpace(scopeClaim))
            {
                return false;
            }

            return scopeClaim
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(requiredScope, StringComparer.OrdinalIgnoreCase);
        }
    }
}
