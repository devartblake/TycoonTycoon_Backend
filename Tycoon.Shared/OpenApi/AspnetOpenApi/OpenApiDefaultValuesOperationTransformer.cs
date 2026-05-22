using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tycoon.Shared.OpenApi.AspnetOpenApi;

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-9.0#customize-openapi-documents-with-transformers
public class OpenApiDefaultValuesOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation openApiOperation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var apiDescription = context.Description;

        // IsDeprecated() extension was removed in Swashbuckle 10 — check endpoint metadata directly.
        var isDeprecated = apiDescription.ActionDescriptor.EndpointMetadata
            .OfType<ObsoleteAttribute>()
            .Any();
        openApiOperation.Deprecated |= isDeprecated;

        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1752#issue-663991077
        foreach (var responseType in context.Description.SupportedResponseTypes)
        {
            // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/b7cf75e7905050305b115dd96640ddd6e74c7ac9/src/Swashbuckle.AspNetCore.SwaggerGen/SwaggerGenerator/SwaggerGenerator.cs#L383-L387
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            if (openApiOperation.Responses is null
                || !openApiOperation.Responses.TryGetValue(responseKey, out var response)
                || response.Content is null)
            {
                continue;
            }

            foreach (var contentType in response.Content.Keys.ToList())
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (openApiOperation.Parameters == null)
        {
            return Task.CompletedTask;
        }

        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/412
        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/pull/413
        // OpenAPI 2.0: Parameters is IList<IOpenApiParameter>; cast to OpenApiParameter for mutation.
        foreach (var parameter in openApiOperation.Parameters)
        {
            if (parameter is not OpenApiParameter concreteParam) continue;

            var description = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == concreteParam.Name);
            if (description == null)
                continue;

            concreteParam.Description ??= description.ModelMetadata?.Description;

            if (concreteParam.Schema is OpenApiSchema concreteSchema
                && concreteSchema.Default == null
                && description.DefaultValue != null
                && description.DefaultValue is not DBNull
                && description.ModelMetadata is { } modelMetadata)
            {
                // REF: https://github.com/Microsoft/aspnet-api-versioning/issues/429#issuecomment-605402330
                var json = JsonSerializer.Serialize(description.DefaultValue, modelMetadata.ModelType);
                concreteSchema.Default = JsonNode.Parse(json);
            }

            concreteParam.Required |= description.IsRequired;
        }

        return Task.CompletedTask;
    }
}
