using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NutriScan.WebApi.Infrastructure;

/// <summary>
/// Marks every property that cannot be null as required, so generated clients (openapi-typescript) type
/// <c>name: string</c> instead of <c>name?: string</c>. Nullability itself comes from the C# nullable annotations.
/// </summary>
public sealed class RequiredNotNullableSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach (var (name, property) in schema.Properties)
        {
            if (!property.Nullable)
            {
                schema.Required.Add(name);
            }
        }
    }
}
