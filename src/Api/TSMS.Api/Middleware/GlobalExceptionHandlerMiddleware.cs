using System.Net;
using System.Text.Json;

namespace TSMS.Api.Middleware;

// Catch tất cả unhandled exception — tránh để ASP.NET Core trả HTML error page
// hoặc stack trace ra ngoài. Trả JSON nhất quán với error format của project.
public class GlobalExceptionHandlerMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex) {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new {
            Code = "Server.InternalError",
            Message = "Đã xảy ra lỗi không mong đợi. Vui lòng thử lại sau."
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}