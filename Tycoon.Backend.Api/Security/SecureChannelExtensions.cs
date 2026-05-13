using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tycoon.Backend.Api.Security;

public sealed class AllowTrustedBffPlainJsonAttribute : Attribute { }

public static class SecureChannelExtensions
{
    /// Marks a minimal-API endpoint as requiring an active KMS secure session.
    /// Incoming bodies must be encrypted envelopes; responses are encrypted before sending.
    public static RouteHandlerBuilder RequireSecureChannel(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<SecureChannelFilter>()
                  .WithTags("secure-channel");

    public static RouteHandlerBuilder AllowTrustedBffPlainJson(this RouteHandlerBuilder builder)
        => builder.WithMetadata(new AllowTrustedBffPlainJsonAttribute());

    /// Applies secure-channel enforcement to every endpoint in a route group.
    public static RouteGroupBuilder RequireSecureChannel(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<SecureChannelFilter>();
        return group;
    }
}
