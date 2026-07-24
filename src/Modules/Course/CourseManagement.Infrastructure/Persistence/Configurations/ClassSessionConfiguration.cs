using CourseManagement.Domain.Entities;
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

        // FK tới WeeklySlot sinh ra buổi học này — dùng để nhóm ClassSession theo lịch lặp lại hàng tuần.
        builder.Property(s => s.WeeklySlotId)
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

        builder.Property(s => s.IsCancelled)
            .HasDefaultValue(false)
            .IsRequired();

        // Unique constraint: no two sessions with same date + type in the same course.
        builder.HasIndex(s => new { s.CourseId, s.SessionDate, s.SessionType })
            .IsUnique();

        // Truy vấn nhanh "tất cả ClassSession thuộc 1 WeeklySlot" — dùng nhiều bởi Enrollment BC
        // (GetClassSessionsByWeeklySlotIdsAsync) và khi RemoveWeeklySlot/regenerate EndDate.
        builder.HasIndex(s => s.WeeklySlotId);
    }
}