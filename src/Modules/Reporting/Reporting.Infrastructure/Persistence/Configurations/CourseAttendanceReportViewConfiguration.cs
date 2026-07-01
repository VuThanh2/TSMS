using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporting.Domain.ReadModels;

namespace Reporting.Infrastructure.Persistence.Configurations;

public class CourseAttendanceReportViewConfiguration : IEntityTypeConfiguration<CourseAttendanceReportView> {
    public void Configure(EntityTypeBuilder<CourseAttendanceReportView> builder) {
        builder.ToTable("CourseAttendanceReports");

        // PK = EnrollmentId — 1 Enrollment tương ứng 1 row báo cáo điểm danh.
        builder.HasKey(v => v.EnrollmentId);

        builder.Property(v => v.EnrollmentId).IsRequired();
        builder.Property(v => v.CourseId).IsRequired();

        builder.Property(v => v.CourseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.StudentId).IsRequired();

        builder.Property(v => v.StudentFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.StudentEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(v => v.TotalSessions).IsRequired();
        builder.Property(v => v.PresentCount).IsRequired();
        builder.Property(v => v.ExcusedCount).IsRequired();
        builder.Property(v => v.AbsentCount).IsRequired();

        builder.HasIndex(v => v.CourseId);
        builder.HasIndex(v => new { v.StudentId, v.CourseId }).IsUnique();
    }
}