using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Synaptix.Backend.Api.Tests.TestHost
{
    // #405: TycoonApiFactory nulls the global deny-by-default FallbackPolicy so most integration
    // tests can exercise each endpoint's explicit auth posture. This variant KEEPS the fallback
    // active — mirroring production — so FallbackPolicyContractTests can verify that:
    //   (a) a protected endpoint is denied (401) without a token, and
    //   (b) endpoints marked .AllowAnonymous() stay reachable despite deny-by-default.
    // The re-enabling PostConfigure runs after the base factory's null-out, so it wins.
    public sealed class FallbackPolicyApiFactory : TycoonApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<AuthorizationOptions>(o =>
                    o.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());
            });
        }
    }
}
