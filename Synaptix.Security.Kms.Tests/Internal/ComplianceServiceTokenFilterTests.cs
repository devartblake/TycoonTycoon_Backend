using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ComplianceServiceTokenFilter = Synaptix.Compliance.Api.Security.ServiceTokenFilter;

namespace Synaptix.Security.Kms.Tests.Internal;

public sealed class ComplianceServiceTokenFilterTests
{
    [Fact]
    public async Task InvokeAsync_RejectsMissingServiceToken()
    {
        var filter = CreateFilter();
        var context = new TestEndpointFilterInvocationContext(CreateHttpContext());

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next"));

        var response = result.Should().BeAssignableTo<IResult>().Subject;
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

    [Fact]
    public async Task InvokeAsync_AllowsRequestWhenServiceTokenNotConfigured()
    {
        var filter = CreateFilter(serviceToken: null);
        var context = new TestEndpointFilterInvocationContext(CreateHttpContext());

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next"));

        result.Should().Be("next");
    }

    private static ComplianceServiceTokenFilter CreateFilter(string? serviceToken = "svc-token")
    {
        var values = new Dictionary<string, string?>();
        if (serviceToken is not null)
        {
            values["ComplianceApi:ServiceToken"] = serviceToken;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new ComplianceServiceTokenFilter(configuration);
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
