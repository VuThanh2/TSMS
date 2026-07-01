using TSMS.Api.OpenApi;

namespace TSMS.Api.Extensions;

public static class SwaggerExtensions {
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services) {
        services.AddOpenApi(options => {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }

    // Map endpoint sinh spec JSON (native) + map UI Swashbuckle trỏ vào spec đó.
    public static WebApplication UseSwaggerDocumentation(this WebApplication app) {
        app.MapOpenApi();

        app.UseSwaggerUI(options => {
            options.SwaggerEndpoint("/openapi/v1.json", "TSMS API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}