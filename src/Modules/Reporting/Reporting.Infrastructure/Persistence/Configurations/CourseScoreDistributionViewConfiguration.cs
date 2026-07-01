using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reporting.Domain.ReadModels;

namespace Reporting.Infrastructure.Persistence.Configurations;

public class CourseScoreDistributionViewConfiguration : IEntityTypeConfiguration<CourseScoreDistributionView> {
    public void Configure(EntityTypeBuilder<CourseScoreDistributionView> builder) {
        builder.ToTable("CourseScoreDistributions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .IsRequired();

        builder.Property(v => v.CourseId)
            .IsRequired();

        builder.Property(v => v.CourseName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.ScoreGroup)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.RangeStart)
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(v => v.RangeEnd)
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(v => v.StudentCount)
            .IsRequired();

        builder.Property(v => v.Percentage)
            .HasColumnType("decimal(6,4)")
            .IsRequired();

        // Unique: mỗi Course chỉ có 1 row cho mỗi ScoreGroup.
        builder.HasIndex(v => new { v.CourseId, v.ScoreGroup })
            .IsUnique();
    }
}