using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApiProject.Filters;

/// <summary>
/// Swagger operation filter to add Idempotency-Key header to POST endpoints
/// </summary>
public class IdempotencyKeyHeaderOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    // Only add the header to POST operations (specifically the /charge endpoint)
    if (context.ApiDescription.HttpMethod?.ToUpper() == "POST")
    {
      operation.Parameters ??= new List<OpenApiParameter>();

      operation.Parameters.Add(new OpenApiParameter
      {
        Name = "Idempotency-Key",
        In = ParameterLocation.Header,
        Description = "Unique key to prevent duplicate charge requests. Use a unique value for each transaction.",
        Required = true,
        Schema = new OpenApiSchema
        {
          Type = "string",
          Example = new Microsoft.OpenApi.Any.OpenApiString($"unique-key-{Guid.NewGuid().ToString("N")[..12]}")
        }
      });
    }
  }
}
