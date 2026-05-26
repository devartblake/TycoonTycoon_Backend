using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Synaptix.Backend.Api.Security
{
    public static class AdminPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOpsPolicy = "AdminOps";
        public const string AdminNotificationsWritePolicy = "AdminNotificationsWrite";
        public const string CryptoSettlementPolicy = "CryptoSettlement";
        public const string SuperAdminPolicy = "SuperAdmin";

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

            options.AddPolicy(CryptoSettlementPolicy, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireAssertion(ctx =>
                    IsAdminOpsUser(ctx.User)
                    || IsCryptoSettlementService(ctx.User));
            });

            options.AddPolicy(SuperAdminPolicy, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("admin");
                p.RequireClaim("aud", "admin-app");
                p.RequireAssertion(ctx => HasScope(ctx.User, "acl:write"));
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

        private static bool IsAdminOpsUser(System.Security.Claims.ClaimsPrincipal user)
        {
            var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value);
            var audiences = user.FindAll("aud").Select(c => c.Value);

            return roles.Contains("admin", StringComparer.OrdinalIgnoreCase)
                   && audiences.Contains("admin-app", StringComparer.Ordinal)
                   && HasScope(user, "users:read");
        }

        private static bool IsCryptoSettlementService(System.Security.Claims.ClaimsPrincipal user)
        {
            var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value);
            var audiences = user.FindAll("aud").Select(c => c.Value);

            return roles.Contains("service", StringComparer.OrdinalIgnoreCase)
                   && audiences.Contains("crypto-service", StringComparer.Ordinal)
                   && HasScope(user, "crypto:settlement");
        }
    }
}
