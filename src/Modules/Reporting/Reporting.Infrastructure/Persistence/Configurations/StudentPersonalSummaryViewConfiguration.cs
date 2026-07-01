using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporting.Domain.ReadModels;

namespace Reporting.Infrastructure.Persistence.Configurations;

public class StudentPersonalSummaryViewConfiguration : IEntityTypeConfiguration<StudentPersonalSummaryView> {
    public void Configure(EntityTypeBuilder<StudentPersonalSummaryView> builder) {
        builder.ToTable("StudentPersonalSummaries");

        // Composite PK: mỗi Student có 1 row cho mỗi Course đã đăng ký.
        builder.HasKey(v => new { v.StudentId, v.CourseId });

        builder.Property(v => v.StudentId).IsRequired();
        builder.Property(v => v.CourseId).IsRequired();

        builder.Property(v => v.CourseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.Grade)
            .HasColumnType("decimal(4,2)")
            .IsRequired(false);

        builder.Property(v => v.TotalSessions).IsRequired();
        builder.Property(v => v.PresentCount).IsRequired();
        builder.Property(v => v.ExcusedCount).IsRequired();
        builder.Property(v => v.AbsentCount).IsRequired();

        builder.HasIndex(v => v.StudentId);
    }
}