namespace SharedInfrastructure.Time;

// Nguồn duy nhất cho múi giờ Việt Nam (UTC+7), dùng chung cho các job/logic nhạy giờ-trong-ngày
// (vd nhắc lịch 30 phút trước ca học). Toàn hệ thống lưu thời gian bằng UTC; chỉ quy đổi sang giờ
// VN ở biên (khi so với "hôm nay" theo lịch địa phương hoặc khi canh giờ chạy cron).
public static class VietnamTimeZone {
    // Windows dùng "SE Asia Standard Time", Linux/macOS (IANA) dùng "Asia/Ho_Chi_Minh".
    // .NET tự map 2 dạng trên cùng 1 nền tảng nhưng không chắc chắn ở mọi bản OS, nên thử cả hai.
    public static readonly TimeZoneInfo Instance = Resolve();

    private static TimeZoneInfo Resolve() {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" }) {
            try {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) {
                // Thử id tiếp theo.
            }
        }

        // Fallback an toàn: VN không có DST nên UTC+7 cố định là đủ đúng.
        return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam Time", "Vietnam Time");
    }

    // Ngày hiện tại theo lịch VN — dùng thay cho DateOnly.FromDateTime(DateTime.UtcNow) ở các chỗ
    // cần "hôm nay" theo giờ địa phương.
    public static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Instance));
}
