using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporting.Domain.ReadModels;

namespace Reporting.Infrastructure.Persistence.Configurations;

public class StudentGradeReportViewConfiguration : IEntityTypeConfiguration<StudentGradeReportView> {
    public void Configure(EntityTypeBuilder<StudentGradeReportView> builder) {
        builder.ToTable("StudentGradeReports");

        // PK = EnrollmentId — 1 Enrollment tương ứng 1 row trong report này.
        builder.HasKey(v => v.EnrollmentId);

        builder.Property(v => v.EnrollmentId)
            .IsRequired();

        builder.Property(v => v.CourseId)
            .IsRequired();

        builder.Property(v => v.CourseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.StudentId)
            .IsRequired();

        builder.Property(v => v.StudentFullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.StudentEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(v => v.Grade)
            .HasColumnType("decimal(4,2)")
            .IsRequired(false);

        builder.HasIndex(v => v.CourseId);
    }
}