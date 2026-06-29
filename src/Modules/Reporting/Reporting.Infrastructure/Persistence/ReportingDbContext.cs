using Microsoft.EntityFrameworkCore;
using Reporting.Domain.ReadModels;

namespace Reporting.Infrastructure.Persistence;

// EventHandlers (INotificationHandler) ghi vào các ReadModel tables thông qua
// IReportingRepository — không cần Outbox hay UnitOfWork pattern.
public class ReportingDbContext : DbContext {
    public DbSet<CourseStatisticsView> CourseStatistics => Set<CourseStatisticsView>();
    public DbSet<StudentGradeReportView> StudentGradeReports => Set<StudentGradeReportView>();
    public DbSet<CourseScoreDistributionView> ScoreDistributions => Set<CourseScoreDistributionView>();
    public DbSet<CourseAttendanceReportView> AttendanceReports => Set<CourseAttendanceReportView>();
    public DbSet<StudentPersonalSummaryView> PersonalSummaries => Set<StudentPersonalSummaryView>();

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}