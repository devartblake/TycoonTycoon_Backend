using Mediator;
using Microsoft.AspNetCore.Http;
using Synaptix.Shared.Abstractions.Web;

namespace Synaptix.Shared.Web.Minimal;

public record HttpQuery(HttpContext HttpContext, IMediator Mediator, CancellationToken CancellationToken) : IHttpQuery;
