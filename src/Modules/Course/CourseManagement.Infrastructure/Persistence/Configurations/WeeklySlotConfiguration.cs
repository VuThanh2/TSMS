using CourseManagement.Domain.Entities;
using CourseManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseManagement.Infrastructure.Persistence.Configurations;

public class WeeklySlotConfiguration : IEntityTypeConfiguration<WeeklySlot> {
    public void Configure(EntityTypeBuilder<WeeklySlot> builder) {
        builder.ToTable("WeeklySlots");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("WeeklySlotId")
            .IsRequired();

        builder.Property(s => s.CourseId)
            .IsRequired();

        builder.Property(s => s.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(s => s.SessionType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // Unique constraint: 1 Course không thể có 2 WeeklySlot cùng (DayOfWeek, SessionType).
        builder.HasIndex(s => new { s.CourseId, s.DayOfWeek, s.SessionType })
            .IsUnique();
    }
}