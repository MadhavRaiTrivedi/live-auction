using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LiveAuction.Api.OpenApi;

internal static class BearerSecurityTransformer
{
    private const string BearerSchemeName = "Bearer";

    public static OpenApiOptions AddBearerSecurity(this OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Get a token from POST /api/auth/dev-token.",
            };
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = [],
            });
            return Task.CompletedTask;
        });

        return options;
    }
}
