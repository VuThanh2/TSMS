using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TSMS.Api.HealthChecks;

// Custom response writer cho /health — trả JSON liệt kê trạng thái từng check
// (mỗi DbContext của 4 BC) thay vì chỉ text "Healthy"/"Unhealthy" mặc định.
public static class HealthCheckResponseWriter {
    public static Task WriteJsonResponse(HttpContext context, HealthReport report) {
        context.Response.ContentType = "application/json";

        var payload = new {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message
            })
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions {
            WriteIndented = true
        });

        return context.Response.WriteAsync(json);
    }
}