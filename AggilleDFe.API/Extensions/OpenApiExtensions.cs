using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AggilleDFe.API;

public static class OpenApiExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "AggilleDFe API",
                    Version = "v1",
                    Description = "API de download e gerenciamento de documentos fiscais eletrônicos (NFe, CTe, NFSe)."
                };
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var components = document.Components ??= new OpenApiComponents();
                components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Informe o token JWT obtido no login."
                };
                components.SecuritySchemes["Basic"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    Description = "Usuário e senha da API externa (/api/v1/dfe/*), definidos na tela Configuração."
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var allowsAnonymous = metadata.Any(m => m is IAllowAnonymous);

                if (!allowsAnonymous)
                {
                    // Rotas com a política BasicApi (grupo /api/v1/dfe) autenticam
                    // por Basic, não por JWT — anuncia o esquema certo para o
                    // "Authorize" do Swagger funcionar nelas.
                    var usaBasic = metadata.OfType<IAuthorizeData>().Any(a => a.Policy == "BasicApi");

                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(usaBasic ? "Basic" : "Bearer", context.Document)] = []
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "AggilleDFe API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
