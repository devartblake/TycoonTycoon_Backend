using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Application.Moderation;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Security
{
    public static class PlayerAccessFilters
    {
        /// <summary>
        /// The access policy: a feature is withheld only from banned/suspended
        /// accounts. Banned covers permanent bans and active temporary
        /// suspensions (surfaced via the effective status); Restricted is the
        /// punitive limited state. Suspected (watch-only) and Normal keep access.
        /// </summary>
        public static bool IsFeatureBlocked(ModerationStatus status)
            => status is ModerationStatus.Banned or ModerationStatus.Restricted;

        /// <summary>
        /// Gate a feature group on the caller's moderation standing instead of a
        /// global operator feature flag. Only players whose effective status is
        /// Banned or Restricted (i.e. banned/suspended) are refused; Suspected
        /// and Normal players pass, and anonymous callers on public reads pass
        /// (there is no account to restrict). This replaces the old release
        /// feature-lock so features are available to all users by default and
        /// only removed from misbehaving accounts.
        /// </summary>
        public static RouteGroupBuilder RequireNotBanned(this RouteGroupBuilder group)
        {
            group.AddEndpointFilter(async (ctx, next) =>
            {
                var http = ctx.HttpContext;
                var claim = http.User.FindFirst(ClaimTypes.NameIdentifier)
                            ?? http.User.FindFirst("sub");

                if (claim is not null
                    && Guid.TryParse(claim.Value, out var playerId)
                    && playerId != Guid.Empty)
                {
                    var moderation = http.RequestServices.GetRequiredService<ModerationService>();
                    var status = await moderation.GetEffectiveStatusAsync(playerId, http.RequestAborted);
                    if (IsFeatureBlocked(status))
                    {
                        return Results.Json(
                            new
                            {
                                error = new
                                {
                                    code = "AccountRestricted",
                                    message = "Your account is currently restricted from this feature.",
                                    details = new { status = status.ToString() }
                                }
                            },
                            statusCode: StatusCodes.Status403Forbidden);
                    }
                }

                return await next(ctx);
            });

            return group;
        }
    }
}
