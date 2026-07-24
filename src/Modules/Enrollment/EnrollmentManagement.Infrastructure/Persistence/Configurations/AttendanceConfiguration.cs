using EnrollmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnrollmentManagement.Infrastructure.Persistence.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance> {
    public void Configure(EntityTypeBuilder<Attendance> builder) {
        builder.ToTable("Attendances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("AttendanceId")
            .IsRequired();

        builder.Property(a => a.StudentId)
            .IsRequired();

        // Cross-BC reference — FK trỏ sang ClassSession của CourseManagement BC by Id.
        builder.Property(a => a.ClassSessionId)
            .IsRequired();

        // Denormalized để query theo CourseId mà không cần JOIN.
        builder.Property(a => a.CourseId)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Unique constraint: 1 Student chỉ có 1 Attendance record cho mỗi ClassSession.
        builder.HasIndex(a => new { a.StudentId, a.ClassSessionId })
            .IsUnique();

        builder.HasIndex(a => a.ClassSessionId);
    }
}