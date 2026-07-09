using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedInfrastructure.Email;
using SharedInfrastructure.Time;

namespace EnrollmentManagement.Infrastructure.Services;

// Hangfire recurring job — chạy 2 lần/ngày (30 phút trước ca Sáng và 30 phút trước ca
// Chiều), gửi email nhắc lịch cho Student đã enroll các ClassSession diễn ra HÔM NAY
// đúng ca đó.
//
// Không cần bảng tracking chống gửi trùng: mỗi cron chỉ chạy đúng 1 lần/ngày cho 1
// SessionType cố định (do trường chỉ có đúng 2 ca/ngày), nên tự nhiên không có nguy cơ
// gửi trùng — khác với thiết kế "poll mỗi phút" ban đầu vốn cần idempotency vì có thể
// chạy chồng lấn nhiều lần trong cùng 1 window.
public class SendSessionReminderJobService {
    // Giờ bắt đầu cố định của từng ca, theo quy định của trường. Chỉ dùng để build nội
    // dung email
    private static readonly Dictionary<SessionType, TimeOnly> SessionStartTimes = new() {
        [SessionType.Morning] = new TimeOnly(7, 0),
        [SessionType.Afternoon] = new TimeOnly(13, 0)
    };

    private readonly EnrollmentDbContext _context;
    private readonly ICourseEnrollmentService _courseEnrollmentService;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SendSessionReminderJobService> _logger;

    public SendSessionReminderJobService(
        EnrollmentDbContext context,
        ICourseEnrollmentService courseEnrollmentService,
        IStudentEnrollmentService studentEnrollmentService,
        IEmailSender emailSender,
        ILogger<SendSessionReminderJobService> logger) {
        _context = context;
        _courseEnrollmentService = courseEnrollmentService;
        _studentEnrollmentService = studentEnrollmentService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task ExecuteAsync(SessionType sessionType, CancellationToken cancellationToken = default) {
        // "Hôm nay" phải tính theo lịch VN, khớp với timezone dùng để canh cron (xem
        // RegisterEnrollmentJobs). Nếu dùng UtcNow, job chạy lúc 06:30 VN = 23:30 UTC hôm trước
        // sẽ query nhầm sang ngày hôm qua.
        var today = VietnamTimeZone.Today();
        var startTime = SessionStartTimes[sessionType];

        var todaySessions = await _courseEnrollmentService.GetClassSessionsByDateAsync(today, cancellationToken);

        // Chỉ lấy đúng ca job này phụ trách, bỏ buổi đã bị hủy (nghỉ lễ...). Luôn query
        // "live" tại thời điểm job chạy nên Admin cancel trước giờ này sẽ tự động loại trừ.
        var sessionsToRemind = todaySessions
            .Where(s => s.SessionType == sessionType.ToString() && !s.IsCancelled)
            .ToList();

        if (sessionsToRemind.Count == 0)
            return;

        var courseIds = sessionsToRemind.Select(s => s.CourseId).Distinct().ToList();
        var courseMap = (await _courseEnrollmentService.GetCoursesByIdsAsync(courseIds, cancellationToken))
            .ToDictionary(c => c.CourseId);

        var weeklySlotIds = sessionsToRemind.Select(s => s.WeeklySlotId).Distinct().ToList();

        // Chỉ Student đã enroll đúng WeeklySlot này mới nhận email
        var studentIdsBySlot = (await _context.EnrolledSessions
                .Where(es => weeklySlotIds.Contains(es.WeeklySlotId))
                .Join(_context.Enrollments,
                    es => es.EnrollmentId,
                    e => e.Id,
                    (es, e) => new { es.WeeklySlotId, e.StudentId })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.WeeklySlotId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.StudentId).Distinct().ToList());

        var allStudentIds = studentIdsBySlot.Values.SelectMany(ids => ids).Distinct().ToList();
        var studentEmails = await _studentEnrollmentService.GetEmailsAsync(allStudentIds, cancellationToken);

        foreach (var session in sessionsToRemind) {
            if (!courseMap.TryGetValue(session.CourseId, out var course))
                continue; // Course bị xóa đồng thời — bỏ qua an toàn, không throw cả batch.

            if (!studentIdsBySlot.TryGetValue(session.WeeklySlotId, out var studentIds))
                continue;

            var body = $"Bạn có ca học môn {course.CourseName} vào lúc {startTime:HH:mm} " +
                       $"ngày {session.SessionDate:dd/MM/yyyy}.";

            foreach (var studentId in studentIds) {
                if (!studentEmails.TryGetValue(studentId, out var email))
                    continue; // Không có email hợp lệ hoặc account đã bị deactivate.

                try {
                    await _emailSender.SendAsync(
                        new EmailMessage(email, "Nhắc lịch học", body), cancellationToken);
                }
                catch (Exception ex) {
                    // Không throw — 1 email lỗi (SMTP timeout, sai địa chỉ...) không được
                    // làm fail toàn bộ batch của các session/recipient khác.
                    _logger.LogWarning(ex,
                        "Gửi reminder thất bại cho StudentId {StudentId}, SessionId {SessionId}",
                        studentId, session.ClassSessionId);
                }
            }
        }
    }
}