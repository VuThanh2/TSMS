# Authentication & Authorization Flow

# 1. JWT Storage

**Quyết định: `localStorage`** (không dùng httpOnly Cookie).

Lý do:

- Backend chỉ trả **`accessToken`**, không có `refreshToken` (xác nhận từ `LoginOutputDto` — chỉ có 1 field `AccessToken`). Không có refresh token đồng nghĩa không có nhu cầu bảo vệ 1 token "sống lâu" khỏi XSS bằng httpOnly Cookie — access token ngắn hạn (mặc định `ExpiryMinutes = 60`, cấu hình ở `appsettings.json` section `Jwt`), rủi ro nếu bị đánh cắp cũng giới hạn trong khung giờ đó.
- Dùng Cookie sẽ cần thêm CSRF protection (double-submit token hoặc SameSite strict) — over-engineering so với quy mô project. `localStorage` + gắn `Authorization: Bearer` thủ công qua Axios interceptor là đủ và đúng với REST API thuần (không dùng cookie-based session).
- Trade-off cần biết (ghi nhận, không chặn quyết định): `localStorage` có thể bị đọc bởi XSS nếu FE có lỗ hổng injection. Giảm thiểu bằng cách: không `dangerouslySetInnerHTML`/`v-html` với dữ liệu chưa sanitize, và giữ token sống ngắn (đã có sẵn từ Backend).

# 2. Token Expiry & Refresh Strategy

**Xác nhận từ code**: Backend **không có** cơ chế refresh token, không có endpoint `/refresh`, và JWT là stateless — `LogoutCommand` chỉ publish `UserLoggedOutEvent` để audit log, **không** revoke token phía server (không có blacklist/`SecurityStamp` check). Nghĩa là:

- Token hết hạn tự nhiên sau `ExpiryMinutes` phút → không có "silent refresh".
- Logout chỉ có tác dụng phía Client (xóa token khỏi `localStorage`) + ghi nhận audit event phía Server; nếu attacker đã có token cũ, nó vẫn hợp lệ tới khi hết hạn tự nhiên.

**Phương án FE:**

1. Axios response interceptor bắt lỗi `401 Unauthorized` → xóa token khỏi `localStorage`, clear Auth Context/state, redirect `/login` kèm message "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại".
2. Không implement auto-refresh (vì Backend không hỗ trợ) — nếu muốn UX tốt hơn có thể thêm optional: decode `exp` claim từ JWT, hiện toast cảnh báo "sắp hết phiên" trước ~2 phút, nhưng đây là optional, không bắt buộc.
3. Middleware `[Authorize]` ở Backend tự trả `401` khi token hết hạn nhờ `ValidateLifetime = true` trong `TokenValidationParameters` — FE không cần tự tính thời gian hết hạn để chủ động logout, chỉ cần bắt `401` là đủ.

# 3. Route Guard theo Role

**Nguồn xác định Role**: decode claim `role` (`ClaimTypes.Role`) trực tiếp từ JWT sau khi login — Backend đã đính kèm `role, fullName, isActive` vào token (xem `TokenService.GenerateToken`), **không cần gọi thêm API** nào để biết Role.

**Thiết kế:**

- Sau login thành công → decode token (dùng thư viện nhẹ như `jwt-decode`, chỉ decode payload, không cần verify signature ở Client vì đó là trách nhiệm của Server) → lưu `{ userId, fullName, role, isActive }` vào Auth Context (React Context + `useReducer`, hoặc state-lib nếu đã chọn ở bước Tech Stack).
- `ProtectedRoute` component (`app/routes/ProtectedRoute.tsx` — xem Folder Structure) nhận prop `allowedRoles: Role[]`:
    - Chưa có token / token đã hết hạn → redirect `/login`.
    - Có token nhưng `role` không nằm trong `allowedRoles` → redirect trang mặc định của chính Role đó (không phải lỗi, vì đây là điều hướng sai chỗ chứ không phải tấn công), hoặc trang `403 Forbidden` tùy UX.
- Redirect mặc định sau login theo `role`: `Admin → /admin/dashboard`, `Lecturer → /lecturer/dashboard` (khớp Menu "Dashboard" đã chốt ở Folder Structure — course-grid), `Student → /student/courses` (Student không có khái niệm Dashboard theo Screen Inventory).
- **Nhắc lại nguyên tắc từ Making Plan**: Route Guard ở FE là lớp UX (ẩn/chặn điều hướng, tránh render nhầm màn hình), **không thay thế** việc Backend enforce `[Authorize(Roles = "...")]` — mọi decision quan trọng (ai được sửa gì) vẫn phải được Backend kiểm tra lại, vì Client-side check luôn có thể bị bypass (sửa localStorage, gọi thẳng API bằng Postman).
    
    # 4. Axios Interceptor (Request/Response)
    

```
Request interceptor:
  - Đọc token từ localStorage → gắn header Authorization: Bearer {token} cho mọi request
    (trừ 2 endpoint AllowAnonymous: /api/auth/login, /api/auth/reset-password)

Response interceptor:
  - 401 → clear token + Auth Context, redirect /login (xem mục 2)
  - 403 → KHÔNG logout, chỉ hiện toast "Bạn không có quyền thực hiện thao tác này"
    (403 nghĩa là đã đăng nhập nhưng role/policy không đủ quyền — khác hẳn 401)
```

# 5. SignalR GradeHub — Xác thực & Re-auth khi token hết hạn giữa chừng

**Vấn đề kỹ thuật cốt lõi**: SignalR Client (JS) không thể gắn header `Authorization` khi bắt tay WebSocket — bắt buộc truyền token qua query string (`accessTokenFactory` trong `@microsoft/signalr`). Vì vậy tầng Authentication cần nhận token từ 2 nguồn: header (REST API) và query string (Hub).

**Thiết kế:**

- `IdentityModuleExtensions.AddJwtBearer` cấu hình `JwtBearerEvents.OnMessageReceived`: nếu path bắt đầu bằng `/hubs` và có query `access_token`, lấy token từ đó thay vì header. Dùng prefix chung `/hubs` (không hardcode `/hubs/grade`) để Identity module — nơi cấu hình Auth — không cần biết route riêng của từng Hub ở module khác, giữ đúng ranh giới Bounded Context.
- `GradeHub` (`EnrollmentManagement.Infrastructure/Hubs/GradeHub.cs`) đánh dấu `[Authorize]` — chỉ connection có JWT hợp lệ mới bắt tay thành công. Override `OnConnectedAsync` để tự `Groups.AddToGroupAsync(Context.ConnectionId, userId)` ngay khi connect, `userId` lấy theo đúng pattern `FindFirstValue(ClaimTypes.NameIdentifier) ?? FindFirstValue("sub")` dùng thống nhất ở mọi Controller trong repo. Nhờ vậy `SignalRNotificationService.NotifyGradeUpdatedAsync` gửi đúng `Clients.Group(studentId)` mà Client không cần tự gọi thêm Hub method nào để join.
- `GradeHub` nằm ở file riêng, tách khỏi `SignalRNotificationService` — đúng nguyên tắc 1 class 1 file và tách rõ trách nhiệm: `SignalRNotificationService` lo publish message (Application-facing), `GradeHub` lo lifecycle kết nối + auth (transport-facing).

| Thành phần | Vị trí | Vai trò |
| --- | --- | --- |
| `JwtBearerEvents.OnMessageReceived` | `Identity.Infrastructure/Extensions/IdentityModuleExtensions.cs` | Nhận token từ query string cho mọi Hub (`/hubs/*`) |
| `GradeHub` | `EnrollmentManagement.Infrastructure/Hubs/GradeHub.cs` | `[Authorize]`  • tự join group theo `userId` khi connect |
| `SignalRNotificationService` | `EnrollmentManagement.Infrastructure/Services/SignalRNotificationService.cs` | Publish `GradeUpdated` đến đúng group Student |

**Phương án FE:**

- Connect Hub bằng `accessTokenFactory: () => localStorage.getItem("accessToken")` — SignalR tự gọi factory này **mỗi lần reconnect**, nên nếu token đổi (login lại) thì kết nối sau sẽ dùng token mới.
- Khi token hết hạn giữa chừng (đang mở tab, không thao tác gì) → Hub connection tự bị Server đóng (do JWT hết hạn) → bắt sự kiện `onclose`/`onreconnecting` của SignalR Client → không tự động thử reconnect vô hạn (vì token cũ chắc chắn sai) → điều hướng như case `401` ở mục 2 (yêu cầu đăng nhập lại).
- Kết nối/ngắt kết nối Hub: `connect()` khi vào trang `My Courses` / `Personal Summary` (nơi hiển thị điểm), `disconnect()` khi rời trang (cleanup trong `useEffect` return) — tránh giữ connection ở những trang không cần real-time.

# Tóm tắt quyết định

| Vấn đề | Quyết định |
| --- | --- |
| JWT storage | `localStorage` |
| Refresh token | Không có (Backend không hỗ trợ) — hết hạn thì bắt đăng nhập lại |
| Phát hiện hết hạn | Bắt `401` ở Axios response interceptor |
| Role source | Decode claim `role` từ JWT sau login, không gọi thêm API |
| Route Guard | `ProtectedRoute` theo `allowedRoles`, chỉ là lớp UX — Backend vẫn là nguồn kiểm soát quyền thật sự |
| SignalR re-auth | Token truyền qua query string (`OnMessageReceived`), `GradeHub` `[Authorize]`  • tự join group theo `userId` khi connect (`OnConnectedAsync`) |