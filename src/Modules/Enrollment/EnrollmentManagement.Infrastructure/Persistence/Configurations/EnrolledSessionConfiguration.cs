using EnrollmentManagement.Domain.Entities;
using EnrollmentManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnrollmentManagement.Infrastructure.Persistence.Configurations;

public class EnrolledSessionConfiguration : IEntityTypeConfiguration<EnrolledSession> {
    public void Configure(EntityTypeBuilder<EnrolledSession> builder) {
        builder.ToTable("EnrolledSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("EnrolledSessionId")
            .IsRequired();

        builder.Property(s => s.EnrollmentId)
            .IsRequired();

        // Cross-BC reference by Id — không có navigation property sang ClassSession.
        builder.Property(s => s.ClassSessionId)
            .IsRequired();

        builder.Property(s => s.SessionType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // Unique constraint: 1 Enrollment không thể có 2 session cùng ClassSessionId.
        builder.HasIndex(s => new { s.EnrollmentId, s.ClassSessionId })
            .IsUnique();
    }
}