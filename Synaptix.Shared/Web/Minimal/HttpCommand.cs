using Mediator;
using Microsoft.AspNetCore.Http;
using Synaptix.Shared.Abstractions.Web;

namespace Synaptix.Shared.Web.Minimal;

public record HttpCommand<TRequest>(
    TRequest Request,
    HttpContext HttpContext,
    IMediator Mediator,
    CancellationToken CancellationToken
) : IHttpCommand<TRequest>;
