using Enrollment.UnitTests.Fakes;
using EnrollmentManagement.Application.Common.Interfaces;
using EnrollmentManagement.Domain.ValueObjects;
using EnrollmentManagement.Infrastructure.Persistence;
using EnrollmentManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SharedInfrastructure.Time;
using EnrollmentAggregate = EnrollmentManagement.Domain.Entities.Enrollment;

namespace Enrollment.UnitTests;

// Kiểm tra business logic của job nhắc lịch: đúng ca (SessionType), bỏ buổi đã hủy, chỉ Student
// đã enroll ĐÚNG WeeklySlot mới nhận, bỏ Student không có email, và 1 email lỗi không làm hỏng
// cả batch. Job tự tính "hôm nay" từ UtcNow rồi query ClassSession qua cross-BC — ta điều khiển
// dữ liệu trả về bằng FakeCourseEnrollmentService để test độc lập với thời gian thật.
public class SendSessionReminderJobServiceTests {
    private static EnrollmentDbContext CreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EnrollmentDbContext(options);
    }

    // Seed 1 Enrollment (Student đăng ký 1 slot Sáng + 1 slot Chiều của cùng 1 Course).
    private static async Task SeedEnrollmentAsync(
        EnrollmentDbContext context,
        Guid studentId, Guid courseId, Guid morningSlotId, Guid afternoonSlotId) {
        var enrollment = EnrollmentAggregate.Create(
            studentId: studentId,
            courseId: courseId,
            slots: [(morningSlotId, SessionType.Morning), (afternoonSlotId, SessionType.Afternoon)],
            studentFullName: "Nguyen Van A",
            studentEmail: "a@example.com",
            courseName: "OOP",
            courseStatus: "Upcoming",
            totalSessionsInCourse: 20).Value;

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();
    }

    private static ClassSessionLookup Session(
        Guid courseId, Guid weeklySlotId, SessionType type, bool isCancelled = false) {
        return new ClassSessionLookup(
            ClassSessionId: Guid.NewGuid(),
            CourseId: courseId,
            WeeklySlotId: weeklySlotId,
            SessionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            SessionType: type.ToString(),
            IsCancelled: isCancelled);
    }

    private static SendSessionReminderJobService CreateJob(
        EnrollmentDbContext context,
        FakeCourseEnrollmentService courseService,
        FakeStudentEnrollmentService studentService,
        FakeEmailSender emailSender) {
        return new SendSessionReminderJobService(
            context, courseService, studentService, emailSender,
            NullLogger<SendSessionReminderJobService>.Instance);
    }

    [Fact]
    public async Task Execute_Morning_SendsToEnrolledStudent_WithCorrectContent() {
        await using var context = CreateInMemoryContext();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, studentId, courseId, morningSlot, afternoonSlot);

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [
                Session(courseId, morningSlot, SessionType.Morning),
                Session(courseId, afternoonSlot, SessionType.Afternoon)
            ]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [studentId] = "a@example.com" }
        };
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        // Chỉ 1 email cho ca Sáng (không gửi ca Chiều).
        var sent = Assert.Single(emailSender.Sent);
        Assert.Equal("a@example.com", sent.To);
        Assert.Equal("Nhắc lịch học", sent.Subject);
        Assert.Contains("OOP", sent.Body);
        Assert.Contains("07:00", sent.Body); // giờ bắt đầu cố định ca Sáng
    }

    [Fact]
    public async Task Execute_Afternoon_UsesAfternoonStartTime() {
        await using var context = CreateInMemoryContext();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, studentId, courseId, morningSlot, afternoonSlot);

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [Session(courseId, afternoonSlot, SessionType.Afternoon)]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [studentId] = "a@example.com" }
        };
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Afternoon);

        var sent = Assert.Single(emailSender.Sent);
        Assert.Contains("13:00", sent.Body);
    }

    [Fact]
    public async Task Execute_SkipsCancelledSession() {
        await using var context = CreateInMemoryContext();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, studentId, courseId, morningSlot, afternoonSlot);

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [Session(courseId, morningSlot, SessionType.Morning, isCancelled: true)]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [studentId] = "a@example.com" }
        };
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task Execute_Morning_DoesNotSendForAfternoonSessions() {
        await using var context = CreateInMemoryContext();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, studentId, courseId, morningSlot, afternoonSlot);

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [Session(courseId, afternoonSlot, SessionType.Afternoon)]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [studentId] = "a@example.com" }
        };
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task Execute_OnlySendsToStudentsEnrolledInThatSlot() {
        await using var context = CreateInMemoryContext();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();

        // Student1 enroll đúng morningSlot. Student2 enroll morningSlot của Course KHÁC (slot khác).
        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();
        var otherMorningSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, student1, courseId, morningSlot, afternoonSlot);
        await SeedEnrollmentAsync(context, student2, Guid.NewGuid(), otherMorningSlot, Guid.NewGuid());

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [Session(courseId, morningSlot, SessionType.Morning)]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [student1] = "s1@example.com", [student2] = "s2@example.com" }
        };
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        var sent = Assert.Single(emailSender.Sent);
        Assert.Equal("s1@example.com", sent.To);
    }

    [Fact]
    public async Task Execute_SkipsStudentWithoutEmail() {
        await using var context = CreateInMemoryContext();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, studentId, courseId, morningSlot, afternoonSlot);

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [Session(courseId, morningSlot, SessionType.Morning)]
        };
        // Student không có email (deactivate / thiếu email) → GetEmailsAsync không trả về.
        var studentService = new FakeStudentEnrollmentService();
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task Execute_OneEmailFails_OthersStillSent() {
        await using var context = CreateInMemoryContext();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();
        await SeedEnrollmentAsync(context, student1, courseId, morningSlot, afternoonSlot);
        await SeedEnrollmentAsync(context, student2, courseId, morningSlot, Guid.NewGuid());

        var courseService = new FakeCourseEnrollmentService {
            Courses = [new CourseLookup(courseId, "OOP", Guid.NewGuid(), "Active", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))],
            ClassSessionsByDate = [Session(courseId, morningSlot, SessionType.Morning)]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [student1] = "fail@example.com", [student2] = "ok@example.com" }
        };
        var emailSender = new FakeEmailSender {
            FailWhen = m => m.To == "fail@example.com"
        };

        // Không throw dù 1 email lỗi.
        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        var sent = Assert.Single(emailSender.Sent);
        Assert.Equal("ok@example.com", sent.To);
    }

    [Fact]
    public async Task Execute_NoSessionsToday_SendsNothing() {
        await using var context = CreateInMemoryContext();
        var courseService = new FakeCourseEnrollmentService(); // ClassSessionsByDate rỗng
        var studentService = new FakeStudentEnrollmentService();
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task Execute_CourseMissing_SkipsSessionSafely() {
        await using var context = CreateInMemoryContext();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var morningSlot = Guid.NewGuid();
        var afternoonSlot = Guid.NewGuid();
        await SeedEnrollmentAsync(context, studentId, courseId, morningSlot, afternoonSlot);

        // Course không có trong lookup (bị xóa đồng thời) → session bị bỏ qua, không throw.
        var courseService = new FakeCourseEnrollmentService {
            Courses = [],
            ClassSessionsByDate = [Session(courseId, morningSlot, SessionType.Morning)]
        };
        var studentService = new FakeStudentEnrollmentService {
            Emails = { [studentId] = "a@example.com" }
        };
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        Assert.Empty(emailSender.Sent);
    }

    [Fact]
    public async Task Execute_QueriesTodaysDate() {
        await using var context = CreateInMemoryContext();
        var courseService = new FakeCourseEnrollmentService();
        var studentService = new FakeStudentEnrollmentService();
        var emailSender = new FakeEmailSender();

        await CreateJob(context, courseService, studentService, emailSender)
            .ExecuteAsync(SessionType.Morning);

        // Job phải query "hôm nay" theo lịch VN (khớp timezone canh cron), KHÔNG theo UTC.
        var requested = Assert.Single(courseService.RequestedDates);
        Assert.Equal(VietnamTimeZone.Today(), requested);
    }
}
