using CourseManagement.Domain.Entities;
using CourseManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseManagement.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course> {
    public void Configure(EntityTypeBuilder<Course> builder) {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("CourseId")
            .IsRequired();

        builder.Property(c => c.LecturerId)
            .IsRequired();

        // Map backing field _courseName → column CourseName.
        builder.Property<string>("_courseName")
            .HasColumnName("CourseName")
            .HasMaxLength(CourseName.MaxLength)
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(c => c.Description)
            .HasMaxLength(2000)
            .IsRequired(false);

        // DateOnly maps to SQL date column (no time component).
        builder.Property<DateOnly>("_startDate")
            .HasColumnName("StartDate")
            .HasColumnType("date")
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<DateOnly>("_endDate")
            .HasColumnName("EndDate")
            .HasColumnType("date")
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.MaxCapacity)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // WeeklySlots collection — định nghĩa lịch lặp lại hàng tuần, map qua property công khai.
        builder.HasMany(c => c.WeeklySlots)
            .WithOne()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.WeeklySlots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // ClassSessions collection — map qua property công khai, đọc/ghi qua backing field _classSessions.
        builder.HasMany(c => c.ClassSessions)
            .WithOne()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.ClassSessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}