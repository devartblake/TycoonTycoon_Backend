using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Synaptix.Shared.Web.Minimal.Extensions;

public static class EndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder Produces(
        this RouteHandlerBuilder builder,
        string description,
        int statusCode,
        Type? responseType = null,
        string? contentType = null,
        params string[] additionalContentTypes
    )
    {
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            SetResponseDescription(operation, statusCode, description);
            return Task.CompletedTask;
        });

        builder.Produces(
            statusCode,
            responseType,
            contentType: contentType,
            additionalContentTypes: additionalContentTypes
        );

        return builder;
    }

    public static RouteHandlerBuilder Produces<TResponse>(
        this RouteHandlerBuilder builder,
        string description,
        int statusCode,
        string? contentType = null,
        params string[] additionalContentTypes
    )
    {
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            SetResponseDescription(operation, statusCode, description);
            return Task.CompletedTask;
        });

        builder.Produces<TResponse>(
            statusCode,
            contentType: contentType,
            additionalContentTypes: additionalContentTypes
        );

        return builder;
    }

    public static RouteHandlerBuilder ProducesProblem(
        this RouteHandlerBuilder builder,
        string description,
        int statusCode,
        string? contentType = null
    )
    {
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            SetResponseDescription(operation, statusCode, description);
            return Task.CompletedTask;
        });

        builder.ProducesProblem(statusCode, contentType: contentType);

        return builder;
    }

    public static RouteHandlerBuilder ProducesValidationProblem(
        this RouteHandlerBuilder builder,
        string description,
        int statusCode = StatusCodes.Status400BadRequest,
        string? contentType = null
    )
    {
        builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            SetResponseDescription(operation, statusCode, description);
            return Task.CompletedTask;
        });
        builder.ProducesValidationProblem(statusCode, contentType: contentType);

        return builder;
    }

    private static void SetResponseDescription(OpenApiOperation operation, int statusCode, string description)
    {
        var responseKey = statusCode.ToString(CultureInfo.InvariantCulture);
        if (operation.Responses?.TryGetValue(responseKey, out var response) == true)
        {
            response.Description = description;
        }
    }
}
