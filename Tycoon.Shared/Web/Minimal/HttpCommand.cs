using Mediator;
using Microsoft.AspNetCore.Http;
using Tycoon.Shared.Abstractions.Web;

namespace Tycoon.Shared.Web.Minimal;

public record HttpCommand<TRequest>(
    TRequest Request,
    HttpContext HttpContext,
    IMediator Mediator,
    CancellationToken CancellationToken
) : IHttpCommand<TRequest>;
