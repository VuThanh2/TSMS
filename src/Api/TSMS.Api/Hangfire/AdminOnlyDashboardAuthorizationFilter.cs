using Hangfire.Dashboard;

namespace TSMS.Api.Hangfire;

// Thay filter mặc định của Hangfire (LocalRequestsOnlyAuthorizationFilter — chỉ cho phép
// localhost) bằng check Role Admin thật qua JWT. Bắt buộc để Dashboard truy cập được cả khi
// deploy Production (Railway)
public class AdminOnlyDashboardAuthorizationFilter : IDashboardAuthorizationFilter {
    public bool Authorize(DashboardContext context) {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole("Admin");
    }
}