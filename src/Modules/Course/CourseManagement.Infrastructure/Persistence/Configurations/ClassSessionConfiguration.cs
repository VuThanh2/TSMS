using CourseManagement.Domain.Entities;
using CourseManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseManagement.Infrastructure.Persistence.Configurations;

public class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession> {
    public void Configure(EntityTypeBuilder<ClassSession> builder) {
        builder.ToTable("ClassSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("ClassSessionId")
            .IsRequired();

        builder.Property(s => s.CourseId)
            .IsRequired();

        builder.Property(s => s.SessionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(s => s.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(s => s.SessionType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // Unique constraint: no two sessions with same date + type in the same course.
        builder.HasIndex(s => new { s.CourseId, s.SessionDate, s.SessionType })
            .IsUnique();
    }
}