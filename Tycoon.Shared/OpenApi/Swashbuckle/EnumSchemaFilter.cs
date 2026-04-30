using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tycoon.Shared.OpenApi.Swashbuckle;

//https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1269#issuecomment-577182931
// `SchemaFilter` is used to customize `schemas` in the OpenAPI document.
public class EnumSchemaFilter : ISchemaFilter
{
    // Swashbuckle 10 / Microsoft.OpenApi 2.0: parameter changed to IOpenApiSchema.
    // Cast to the concrete OpenApiSchema to access mutable Enum list.
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum) return;
        if (schema is not OpenApiSchema concreteSchema) return;

        concreteSchema.Enum.Clear();
        Enum.GetNames(context.Type).ToList()
            .ForEach(name => concreteSchema.Enum.Add(JsonValue.Create(name)!));
        concreteSchema.Type = "string";
    }
}
