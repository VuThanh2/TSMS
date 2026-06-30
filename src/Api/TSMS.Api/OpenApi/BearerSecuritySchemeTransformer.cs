using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TSMS.Api.OpenApi;

// Microsoft.AspNetCore.OpenApi (AddOpenApi) không tự thêm Security Scheme như
// Swashbuckle's AddSecurityDefinition — transformer này bổ sung khai báo Bearer JWT
// vào spec, để Swagger UI hiển thị nút Authorize và đính kèm token vào request.
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer {
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken) {
        var securityScheme = new OpenApiSecurityScheme {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập JWT token theo định dạng: Bearer {token}"
        };

        // OpenApiComponents/OpenApiDocument mới khởi tạo không tự có sẵn
        // SecuritySchemes/Security — phải khởi tạo tường minh trước khi dùng,
        // tránh NullReferenceException lúc runtime (?? và ??= không đủ nếu
        // property con bên trong object mới tạo vẫn null).
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = securityScheme;

        var securityRequirement = new OpenApiSecurityRequirement {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(securityRequirement);

        return Task.CompletedTask;
    }
}