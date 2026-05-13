using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Security.Kms.Api.Security;

namespace Synaptix.Security.Kms.Tests.Internal;

public sealed class ServiceTokenFilterTests
{
    [Fact]
    public async Task InvokeAsync_RejectsMissingServiceToken()
    {
        var filter = CreateFilter();
        var context = new TestEndpointFilterInvocationContext(CreateHttpContext());

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
        var response = (IResult)result!;
        context.HttpContext.Response.Body = new MemoryStream();
        await response.ExecuteAsync(context.HttpContext);
        context.HttpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_RejectsWrongServiceToken()
    {
        var filter = CreateFilter();
        var http = CreateHttpContext();
        http.Request.Headers["X-Service-Token"] = "wrong";
        var context = new TestEndpointFilterInvocationContext(http);

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next"));

        var response = result.Should().BeAssignableTo<IResult>().Subject;
        context.HttpContext.Response.Body = new MemoryStream();
        await response.ExecuteAsync(context.HttpContext);
        context.HttpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_AllowsValidServiceToken()
    {
        var filter = CreateFilter();
        var http = CreateHttpContext();
        http.Request.Headers["X-Service-Token"] = "svc-token";
        var context = new TestEndpointFilterInvocationContext(http);

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next"));

        result.Should().Be("next");
    }

    private static ServiceTokenFilter CreateFilter()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KmsApi:ServiceToken"] = "svc-token"
            })
            .Build();
        return new ServiceTokenFilter(configuration);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [];

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
