using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Security.WebApi.Extensions;

public class BearerSecurityRequirementOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        OpenApiSecurityRequirement securityRequirement1 = new OpenApiSecurityRequirement();
        securityRequirement1.Add(new OpenApiSecurityScheme()
        {
            Reference = new OpenApiReference()
            {
                Type = new ReferenceType?(ReferenceType.SecurityScheme),
                Id = "Bearer"
            },
            Scheme = "oauth2",
            Name = "Bearer",
            In = ParameterLocation.Header
        }, (IList<string>) Array.Empty<string>());
        OpenApiSecurityRequirement securityRequirement2 = securityRequirement1;
        operation.Security.Add(securityRequirement2);
    }
}