using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnrollmentManagement.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Domain.Entities.Enrollment> {
    public void Configure(EntityTypeBuilder<Domain.Entities.Enrollment> builder) {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("EnrollmentId")
            .IsRequired();

        builder.Property(e => e.StudentId)
            .IsRequired();

        builder.Property(e => e.CourseId)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // null khi chưa được chấm điểm (Status = Active).
        builder.Property(e => e.Grade)
            .HasConversion(
                g => g == null ? (decimal?)null : g.Value,
                v => v == null ? null : Grade.Create(v.Value).Value)
            .HasColumnName("Grade")
            .HasColumnType("decimal(4,2)")
            .IsRequired(false);

        builder.Property(e => e.EnrolledAt)
            .IsRequired();

        // EnrolledSessions collection — owned by Enrollment, map via backing field.
        builder.HasMany<EnrolledSession>("_enrolledSessions")
            .WithOne()
            .HasForeignKey(s => s.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_enrolledSessions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unique constraint: 1 Student chỉ đăng ký 1 Course một lần.
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique();
    }
}