using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Tycoon.Shared.Abstractions.Web
{
    public interface ISharedModulesConfiguration
    {
        WebApplicationBuilder AddSharedModuleServices(WebApplicationBuilder builder);

        Task<WebApplication> ConfigureSharedModule(WebApplication app);

        IEndpointRouteBuilder MapSharedModuleEndpoints(IEndpointRouteBuilder endpoints);
    }
}
