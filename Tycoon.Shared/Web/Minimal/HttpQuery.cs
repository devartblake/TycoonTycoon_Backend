using Mediator;
using Microsoft.AspNetCore.Http;
using Tycoon.Shared.Abstractions.Web;

namespace Tycoon.Shared.Web.Minimal;

public record HttpQuery(HttpContext HttpContext, IMediator Mediator, CancellationToken CancellationToken) : IHttpQuery;
