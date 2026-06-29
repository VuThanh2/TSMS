using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporting.Domain.ReadModels;

namespace Reporting.Infrastructure.Persistence.Configurations;

public class CourseStatisticsViewConfiguration : IEntityTypeConfiguration<CourseStatisticsView> {
    public void Configure(EntityTypeBuilder<CourseStatisticsView> builder) {
        builder.ToTable("CourseStatistics");

        builder.HasKey(v => v.CourseId);

        builder.Property(v => v.CourseId).IsRequired();

        // LecturerId lưu để query GetCourseStatisticsByLecturerIdAsync
        // khi UserUpdatedEvent fire — cần biết tất cả Course của Lecturer đó.
        builder.Property(v => v.LecturerId).IsRequired();

        builder.Property(v => v.CourseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.LecturerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(v => v.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(v => v.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.EnrolledCount).IsRequired();

        builder.Property(v => v.AverageScore)
            .HasColumnType("decimal(4,2)")
            .IsRequired(false);

        builder.Property(v => v.GradedStudentCount).IsRequired();
        builder.Property(v => v.UngradedStudentCount).IsRequired();

        builder.HasIndex(v => v.LecturerId);
    }
}